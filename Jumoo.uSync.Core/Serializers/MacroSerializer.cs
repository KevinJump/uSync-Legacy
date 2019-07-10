using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using Jumoo.uSync.Core.Extensions;
using Umbraco.Core.Logging;
using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Serializers
{
    public class MacroSerializer : SyncBaseSerializer<IMacro>, ISyncChangeDetail
    {
        private IPackagingService _packaingService;
        private IEntityService _entityService;
        private IMacroService _macroService;

        public override string SerializerType { get { return uSyncConstants.Serailization.Macro; } }

        public MacroSerializer() : 
            base(Constants.Packaging.MacroNodeName)
        {
            _packaingService = ApplicationContext.Current.Services.PackagingService;
            _entityService = ApplicationContext.Current.Services.EntityService;
            _macroService = ApplicationContext.Current.Services.MacroService;
        }

        public MacroSerializer(string itemType) : base(itemType)
        {
            _packaingService = ApplicationContext.Current.Services.PackagingService;
            _entityService = ApplicationContext.Current.Services.EntityService;
            _macroService = ApplicationContext.Current.Services.MacroService;
        }

        internal override SyncAttempt<IMacro> DeserializeCore(XElement node)
        {
            LogHelper.Debug<MacroSerializer>("<< DeserailizeCore Macro");

            IMacro item = null;

            // find by key. doesn't actully work at the moment
            //   -  beause entity does seem to return macros .
            if (node.Element("Key") != null)
            {
                var key = node.Element("Key").ValueOrDefault(Guid.Empty);
                if (key != Guid.Empty)
                {
                    var macros = _macroService.GetAll(key);
                    if (macros != null && macros.Any())
                        item = macros.FirstOrDefault();
                }
            }

            if (item == null)
            { 
                item = _packaingService.ImportMacros(node).FirstOrDefault();
            }
            // other bits.
            if (item == null)
                return SyncAttempt<IMacro>.Fail(node.NameFromNode(), ChangeType.Import, "Package Service import failed");

            LogHelper.Debug<MacroSerializer>("<<< DeserailizeCore - General properties");

            if (node.Element("name") != null)
                item.Name = node.Element("name").Value;

            if (node.Element("scriptType") != null)
                item.ControlType = node.Element("scriptType").Value;

            if (node.Element("scriptAssembly") != null)
                item.ControlAssembly = node.Element("scriptAssembly").Value;

            if (node.Element("xslt") != null)
                item.XsltPath = node.Element("xslt").Value;

            if (node.Element("Key") != null)
                item.Key = node.Element("Key").ValueOrDefault(Guid.Empty);

            if (node.Element("scriptingFile") != null)
                item.ScriptPath = node.Element("scriptingFile").Value;

            LogHelper.Debug<MacroSerializer>("<<< DeserailizeCore - Defaults");

            item.UseInEditor = node.Element("useInEditor").ValueOrDefault(false);
            item.CacheDuration = node.Element("refreshRate").ValueOrDefault(0);
            item.CacheByMember = node.Element("cacheByMember").ValueOrDefault(false);
            item.CacheByPage = node.Element("cacheByPage").ValueOrDefault(false);
            item.DontRender = node.Element("dontRender").ValueOrDefault(true);


            LogHelper.Debug<MacroSerializer>("<<< DeserailizeCore - Properties");
            List<string> propertiesToRemove = new List<string>();

            var properties = node.Element("properties");
            if (properties != null && properties.HasElements)
            {
                foreach (var property in properties.Elements("property"))
                {
                    var alias = property.Attribute("alias").Value;
                    var itemProperty = item.Properties.FirstOrDefault(x => string.Equals(x.Alias, alias, StringComparison.OrdinalIgnoreCase) == true);
                    if (itemProperty != null)
                    {
                        LogHelper.Debug<MacroSerializer>("<<< Updating Property: {0}", () => alias);
                        itemProperty.Alias = alias;
                        itemProperty.Name = property.Attribute("name").Value;
                        itemProperty.EditorAlias = property.Attribute("propertyType").Value;
                        itemProperty.SortOrder = property.Attribute("sortOrder").ValueOrDefault(0);
                    }
                }
            }

            if (properties != null)
            {
                foreach (var property in item.Properties)
                {
                    var nodeProp = properties.Elements("property").FirstOrDefault(x => x.Attribute("alias").Value == property.Alias);

                    if (nodeProp == null)
                    {
                        propertiesToRemove.Add(property.Alias);
                    }
                }
            }

            if (propertiesToRemove.Any())
            {
                foreach (var alias in propertiesToRemove)
                {
                    LogHelper.Debug<MacroSerializer>("<<< Removing Property : {0}", () => alias);
                    item.Properties.Remove(alias);
                }
            }

            LogHelper.Debug<MacroSerializer>("<<< DeserailizeCore - Saving");
            ApplicationContext.Current.Services.MacroService.Save(item);

            LogHelper.Debug<MacroSerializer>("<<< DeserailizeCore - Return");
            return SyncAttempt<IMacro>.Succeed(item.Name, item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(IMacro macro)
        {

            // TODO: this doesn't export consistantly. 
            // var node = _packaingService.Export(item);

            // Basically a copy of Serialize function, 
            // but wit the properties sorted
            var xml = new XElement("macro");
            xml.Add(new XElement("name", macro.Name));
            xml.Add(new XElement("alias", macro.Alias));
            xml.Add(new XElement("scriptType", macro.ControlType));
            xml.Add(new XElement("scriptAssembly", macro.ControlAssembly));
            xml.Add(new XElement("scriptingFile", macro.ScriptPath));
            xml.Add(new XElement("xslt", macro.XsltPath));
            xml.Add(new XElement("useInEditor", macro.UseInEditor.ToString()));
            xml.Add(new XElement("dontRender", macro.DontRender.ToString()));
            xml.Add(new XElement("refreshRate", macro.CacheDuration.ToString()));
            xml.Add(new XElement("cacheByMember", macro.CacheByMember.ToString()));
            xml.Add(new XElement("cacheByPage", macro.CacheByPage.ToString()));
            xml.Add(new XElement("Key", macro.Key.ToString()));

            var properties = new XElement("properties");
            foreach (var property in macro.Properties.OrderBy(x => x.Alias))
            {
                properties.Add(new XElement("property",
                    new XAttribute("name", property.Name),
                    new XAttribute("alias", property.Alias),
                    new XAttribute("sortOrder", property.SortOrder),
                    new XAttribute("propertyType", property.EditorAlias)));
            }
            xml.Add(properties);

            return SyncAttempt<XElement>.SucceedIf(
                xml != null, macro.Name, xml, typeof(IMacro), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            if (node.IsArchiveFile())
                return false;

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var aliasNode = node.Element("alias");
            if (aliasNode == null)
                return true;

            var item = ApplicationContext.Current.Services.MacroService.GetByAlias(aliasNode.Value);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));
        }

        #region ISyncChangeDetail : Support for detailed change reports
        public IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var aliasNode = node.Element("alias");
            if (aliasNode == null)
                return null;

            var item = ApplicationContext.Current.Services.MacroService.GetByAlias(aliasNode.Value);
            if (item == null)
            {
                return uSyncChangeTracker.NewItem(aliasNode.Value);
            }

            var attempt = Serialize(item);
            if (attempt.Success)
            {
                return uSyncChangeTracker.GetChanges(node, attempt.Item, "");
            }
            else
            {
                return uSyncChangeTracker.ChangeError(aliasNode.Value);
            }
        }
        #endregion
    }
}
