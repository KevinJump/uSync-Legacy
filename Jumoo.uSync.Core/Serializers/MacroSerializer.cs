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
            var item = _packaingService.ImportMacros(node).FirstOrDefault();

            // other bits.
            if (item == null)
                return SyncAttempt<IMacro>.Fail(node.NameFromNode(), typeof(IMacro), ChangeType.Import, "Package Service import failed");

            item.Name = node.Element("name").Value;
            item.ControlType = node.Element("scriptType").Value;
            item.ControlAssembly = node.Element("scriptAssembly").Value;
            item.XsltPath = node.Element("xslt").Value;
            item.ScriptPath = node.Element("scriptingFile").Value;

            item.UseInEditor = node.Element("useInEditor").ValueOrDefault(false);
            item.CacheDuration = node.Element("refreshRate").ValueOrDefault(0);
            item.CacheByMember = node.Element("cacheByMember").ValueOrDefault(false);
            item.CacheByPage = node.Element("cacheByPage").ValueOrDefault(false);
            item.DontRender = node.Element("dontRender").ValueOrDefault(true);


            List<string> propertiesToRemove = new List<string>();

            var properties = node.Elements("properties");
            if (properties != null && properties.Any())
            {
                foreach(var property in properties)
                {
                    var alias = property.Attribute("alias").Value;
                    var itemProperty = item.Properties.FirstOrDefault(x => string.Equals(x.Alias, alias, StringComparison.OrdinalIgnoreCase));
                    if (itemProperty!= null)
                    {
                        itemProperty.Alias = alias;
                        itemProperty.Name = property.Attribute("name").Value;
                        itemProperty.EditorAlias = property.Attribute("propertyType").Value;
                    }
                    else
                    {
                        propertiesToRemove.Add(alias);
                    }
                }
            }

            if (propertiesToRemove.Any())
            {
                foreach (var alias in propertiesToRemove)
                {
                    item.Properties.Remove(alias);
                }
            }

            ApplicationContext.Current.Services.MacroService.Save(item);

            return SyncAttempt<IMacro>.Succeed(item.Name, item, typeof(IMacro), ChangeType.Import);
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
