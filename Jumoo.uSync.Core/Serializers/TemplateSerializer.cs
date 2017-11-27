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
using Jumoo.uSync.Core.Helpers;
using System.IO;

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
    public class TemplateSerializer : SyncBaseSerializer<ITemplate>, ISyncChangeDetail
    {
        IFileService _fileService;
        public override string SerializerType { get { return uSyncConstants.Serailization.Template; } }

        public TemplateSerializer() :
            base(Constants.Packaging.TemplateNodeName)
        {
            _fileService = ApplicationContext.Current.Services.FileService;
        }

        public TemplateSerializer(string itemType) : base(itemType)
        {
            _fileService = ApplicationContext.Current.Services.FileService;
        }

        internal override SyncAttempt<ITemplate> DeserializeCore(XElement node)
        {
            if (node == null || node.Element("Alias") == null || node.Element("Name") == null)
                throw new ArgumentException("Bad xml import");

            var alias = node.Element("Alias").ValueOrDefault(string.Empty);
            if (string.IsNullOrEmpty(alias))
                SyncAttempt<ITemplate>.Fail(node.NameFromNode(), ChangeType.Import, "No Alias node in xml");

            var name = node.Element("Name").ValueOrDefault(string.Empty);

            var item = default(ITemplate);
            var key = node.Element("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
                item = _fileService.GetTemplate(key);

            if (item == default(ITemplate))
                item = _fileService.GetTemplate(alias);

            if (item == null)
            {
                //var templatePath = IOHelper.MapPath(SystemDirectories.MvcViews + "/" + alias.ToSafeFileName() + ".cshtml");
                var templatePath = FindTemplate(alias);
                if (!System.IO.File.Exists(templatePath))
                {
                    templatePath = IOHelper.MapPath(SystemDirectories.Masterpages + "/" + alias.ToSafeFileName() + ".master");
                    if (!System.IO.File.Exists(templatePath))
                    {
                        // cannot find the master for this..
                        templatePath = string.Empty;
                        LogHelper.Warn<TemplateSerializer>("Cannot find underling template file, so we cannot create the template");
                    }
                }

                if (!string.IsNullOrEmpty(templatePath))
                {
                    var content = System.IO.File.ReadAllText(templatePath);

                    item = new Template(name, alias);
                    item.Path = templatePath;
                    item.Content = content;

                }
                else 
                {
                    LogHelper.Warn<TemplateSerializer>("Can't get a template path?");
                    return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "Can't get template path (are template files missing?)");
                }
            }

            if (item == null)
            {
                LogHelper.Warn<TemplateSerializer>("Cannot create the template, something missing?");
                return SyncAttempt<ITemplate>.Fail(name, ChangeType.Import, "Item create fail");
            }

            if (node.Element("Name").Value != item.Name)
                item.Name = node.Element("Name").Value;

            if (node.Element("Alias") != null && node.Element("Alias").Value != item.Alias)
                item.Alias = node.Element("Alias").Value;

            if (node.Element("Master") != null) {
                var masterName = node.Element("Master").Value;
                if (!string.IsNullOrEmpty(masterName))
                {
                    var master = _fileService.GetTemplate(masterName);
                    if (master != null)
                        item.SetMasterTemplate(master);
                }
            }

            if (key != Guid.Empty)
                item.Key = key;

            _fileService.SaveTemplate(item);

            return SyncAttempt<ITemplate>.Succeed(item.Name, item, ChangeType.Import);
        }
		
		private String FindTemplate(String alias) {
            var templatePath = IOHelper.MapPath(SystemDirectories.MvcViews + "/" + alias.ToSafeFileName() + ".cshtml");
            if (!System.IO.File.Exists(templatePath)) {
                var viewsPath = IOHelper.MapPath(SystemDirectories.MvcViews);
                var directories = Directory.GetDirectories(viewsPath);

                foreach (var directory in directories.Where(x => !x.ToLower().Contains("partials"))) {
                    var folder = Path.GetFileName(directory);
                    String relativeFileUrl = string.Format(SystemDirectories.MvcViews + "/{0}/{1}.cshtml", folder, alias.ToSafeFileName());
                    if (System.IO.File.Exists(IOHelper.MapPath(relativeFileUrl))) {
                        templatePath = IOHelper.MapPath(relativeFileUrl);
                    }
                }
            }

            return templatePath;
        }

        internal override SyncAttempt<XElement> SerializeCore(ITemplate item)
        {
            var node = new XElement(Constants.Packaging.TemplateNodeName,
                new XElement("Name", item.Name),
                new XElement("Key", item.Key),
                new XElement("Alias", item.Alias),
                new XElement("Master", item.MasterTemplateAlias));

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(ITemplate), ChangeType.Export);
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

            // LogHelper.Debug<TemplateSerializer>(">> IsUpdated: {0} : {1}", () => !nodeHash.Equals(itemHash), () => item.Name);

            return (!nodeHash.Equals(itemHash));
        }


        #region ISyncChangeDetail : Support for detailed change reports
        public IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var key = node.Element("Key");
            if (key == null)
                return null;

            Guid templateGuid = Guid.Empty;
            if (!Guid.TryParse(key.Value, out templateGuid))
                return null;

            var item = _fileService.GetTemplate(templateGuid);
            if (item == null)
            {
                return uSyncChangeTracker.NewItem( node.NameFromNode());
            }

            var attempt = Serialize(item);
            if (attempt.Success)
            {
                return uSyncChangeTracker.GetChanges(node, attempt.Item, "");
            }
            else
            {
                return uSyncChangeTracker.ChangeError(key.Value);
            }
        }
        #endregion


    }
}
