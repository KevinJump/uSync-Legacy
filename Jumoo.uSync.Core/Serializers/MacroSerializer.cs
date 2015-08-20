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

namespace Jumoo.uSync.Core.Serializers
{
    public class MacroSerializer : SyncBaseSerializer<IMacro>
    {
        private IPackagingService _packaingService; 
        public MacroSerializer(string itemType) : base(itemType)
        {
            _packaingService = ApplicationContext.Current.Services.PackagingService;
        }

        internal override SyncAttempt<IMacro> DeserializeCore(XElement node)
        {
            LogHelper.Debug<MacroSerializer>("<< DeserailizeCore Macro");
            var item = _packaingService.ImportMacros(node).FirstOrDefault();

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
                    }
                }
            }

            foreach(var property in item.Properties)
            {
                var nodeProp = properties.Elements("property").FirstOrDefault(x => x.Attribute("alias").Value == property.Alias);

                if (nodeProp == null)
                { 
                    propertiesToRemove.Add(property.Alias);
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

        internal override SyncAttempt<XElement> SerializeCore(IMacro item)
        {
            var node = _packaingService.Export(item);
            return SyncAttempt<XElement>.SucceedIf(
                node != null, item.Name, node, typeof(IMacro), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
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
    }
}
