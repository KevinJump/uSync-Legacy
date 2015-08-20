using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.IO;

using Jumoo.uSync.Core.Interfaces;
using System.Xml.Linq;
using Umbraco.Core.Logging;

using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.Core.Serializers
{
    /// <summary>
    ///  new behavior, we roll our own template file, and we never create the content of 
    ///  the template from the usync file. instead we assume you have copied it to the 
    ///  site, this is good, because if you work outside of umbraco, you can end up with
    ///  you're templates being way more upto date than what is in usync, and when you 
    ///  sync to something new, usync blows away any changes on disk. 
    /// 
    ///  but now, you can't just rely on usync to copy templates. 
    /// </summary>
    public class TemplateSerializer : SyncBaseSerializer<ITemplate>
    {
        IFileService _fileService;
        public TemplateSerializer(string itemType) : base(itemType)
        {
            _fileService = ApplicationContext.Current.Services.FileService;
        }

        internal override SyncAttempt<ITemplate> DeserializeCore(XElement node)
        {
            var aliasNode = node.Element("Alias");
            if (aliasNode == null)
                SyncAttempt<ITemplate>.Fail(node.NameFromNode(), ChangeType.Import, "No Alias node in xml");

            LogHelper.Debug<Events>("# Importing Template : {0}", () => aliasNode.Value);

            var nameNode = node.Element("Name");

            var item = _fileService.GetTemplate(aliasNode.Value);
            if (item == null)
            {
                var templatePath = IOHelper.MapPath(SystemDirectories.MvcViews + "/" + aliasNode.Value.ToSafeFileName() + ".cshtml");
                if (!System.IO.File.Exists(templatePath))
                {
                    templatePath = IOHelper.MapPath(SystemDirectories.Masterpages + "/" + aliasNode.Value.ToSafeFileName() + ".master");
                    if (!System.IO.File.Exists(templatePath))
                    {
                        // cannot find the master for this..
                        templatePath = string.Empty;
                        LogHelper.Warn<TemplateSerializer>("Cannot find underling template file, so we cannot create the template");
                    }
                }    
                
                if ( string.IsNullOrEmpty(templatePath))
                {
                    item = new Template(nameNode.Value, aliasNode.Value);
                    item.Path = templatePath;
                }
            }

            if (node.Element("Name").Value != item.Name)
                item.Name = node.Element("Name").Value;

            if (node.Element("Master") != null) {
                var masterName = node.Element("Master").Value;
                if (!string.IsNullOrEmpty(masterName))
                {
                    var master = _fileService.GetTemplate(masterName);
                    if (master != null)
                        item.SetMasterTemplate(master);
                }
            }

            _fileService.SaveTemplate(item);

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(ITemplate item)
        {
            var node = new XElement(Constants.Packaging.TemplateNodeName,
                new XElement("Name", item.Name),
                new XElement("Alias", item.Alias),
                new XElement("Master", item.MasterTemplateAlias));

            return SyncAttempt<XElement>.Succeed(item.Name, node, ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var aliasNode = node.Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _fileService.GetTemplate(aliasNode.Value);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            LogHelper.Debug<TemplateSerializer>(">> IsUpdated: {0} : {1}", () => !nodeHash.Equals(itemHash), () => item.Name);

            return (!nodeHash.Equals(itemHash));
        }
    }
}
