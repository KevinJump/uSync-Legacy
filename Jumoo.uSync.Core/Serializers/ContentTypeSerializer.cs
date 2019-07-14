using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Jumoo.uSync.Core.Serializers;
using Jumoo.uSync.Core.Extensions;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;
using System.Xml.Serialization;
using System.IO;
using Umbraco.Core.IO;
using System.Globalization;
using Jumoo.uSync.Core.Helpers;
using System.Web;

namespace Jumoo.uSync.Core.Serializers
{
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType>, ISyncChangeDetail
    {
        public override string SerializerType
        {
            get { return uSyncConstants.Serailization.ContentType; }
        }

        public ContentTypeSerializer() :
            base(Constants.Packaging.DocumentTypeNodeName, UmbracoObjectTypes.DocumentType)
        { }

        public ContentTypeSerializer(string itemType) : base(itemType, UmbracoObjectTypes.DocumentType)
        {
        }

        internal override SyncAttempt<IContentType> DeserializeCore(XElement node)
        {
            if (node.Name.LocalName == "EntityFolder")
                return DeserializeContainer(node);

            // we can't use the package manager for this :(
            // we have to do it by hand.
            if (node == null || node.Element("Info") == null || node.Element("Info").Element("Alias") == null)
                throw new ArgumentException("Invalid xml");

            var info = node.Element("Info");

            IContentType item = null;

            Guid key = Guid.Empty;
            if ((info.Element("Key") != null && Guid.TryParse(info.Element("Key").Value, out key)))
            {
                // we have key.
                item = _contentTypeService.GetContentType(key);
            }

            // you need the parent to create, so do it here...
            var parent = default(IContentType);
            var parentId = -1;
            var folderId = -1;
            var parentAlias = info.Element("Master");
            if (parentAlias != null && !string.IsNullOrEmpty(parentAlias.Value))
            {
                var masterKey = parentAlias.Attribute("Key").ValueOrDefault(Guid.Empty);
                if (masterKey != Guid.Empty)
                {
                    parent = _contentTypeService.GetContentType(masterKey);
                }

                if (parent == null)
                    parent = _contentTypeService.GetContentType(parentAlias.Value);

                if (parent != null)
                    parentId = parent.Id;
            }

            if (parentId == -1)
            {
                // work out if this content item is in a folder.
                folderId  = GetContentFolders(info, item);
                parentId = folderId;
            }

            var alias = info.Element("Alias").Value;

            // can't find by key, lookup by alias.
            if (item == null)
            {
                LogHelper.Debug<ContentTypeSerializer>("Looking up ContentType by alias");
                item = _contentTypeService.GetContentType(alias);
            }

            if (item == null)
            {
                LogHelper.Debug<ContentTypeSerializer>("Creating new ContentType");
                item = new ContentType(-1) {
                    Alias = alias
                };

                if (parent != null)
                    item.AddContentType(parent);
            }

            DeserializeBase(item, info);
            if (folderId != -1)
            {
                item.SetLazyParentId(new Lazy<int>( ()=> parentId));
            }


            if (item.Key != key)
            {
                LogHelper.Debug<ContentTypeSerializer>("Changing Item Key: {0} -> {1}",
                    () => item.Key, () => key);
                item.Key = key;
            }

            // _contentTypeService.Save(item);

            // Update Tabs before props -- allows moving props to new tabs in sync
            DeserializeTabSortOrder((IContentTypeBase)item, node);

            // Update Properties
            var msg = DeserializeProperties((IContentTypeBase)item, node);

            // contenttype specifics..
            var listView = info.Element("IsListView").ValueOrDefault(false);
            if (item.IsContainer != listView)
                item.IsContainer = listView;

            var masterTemplate = info.Element("DefaultTemplate").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(masterTemplate))
            {
                var template = ApplicationContext.Current.Services.FileService.GetTemplate(masterTemplate);
                if (template != null)
                    item.SetDefaultTemplate(template);
            }

            CleanUpTabs((IContentTypeBase)item, node);

            DeserializeTemplates(item, info);

            _contentTypeService.Save(item);

            // Update Structure (Happens in second pass)
            // need to consider if we also call it here
            // as that will simplify single calling apps,
            // but slow down bulk operations as we will be doing
            // structure twice.
            // DeserializeStructure((IContentTypeBase)item, node);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import, msg);
        }

        private int GetContentFolders(XElement info, IContentType item)
        {
            var path = info.Element("Folder").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                // create the folders at each level, and then return the topmost folder as the id (we set it as parent)
                var folders = path.Split('/');
                var rootFolder = HttpUtility.UrlDecode(folders[0]);

                var rootId = -1;
                var root = _contentTypeService.GetContentTypeContainers(rootFolder, 1).FirstOrDefault();
                if (root == null)
                {
                    var attempt = _contentTypeService.CreateContentTypeContainer(-1, rootFolder);
                    if (attempt == false)
                    {
                        // something amis
                        LogHelper.Warn<ContentTypeSerializer>("Can't create the root folder something is not right - you doc types might be a little flat");
                        return -1;
                    }
                    rootId = attempt.Result.Entity.Id;
                }
                else {
                    rootId = root.Id;
                }

                if (rootId != -1)
                {
                    var current = _contentTypeService.GetContentTypeContainer(rootId);

                    for (int i = 1; i < folders.Length; i++)
                    {
                        var name = HttpUtility.UrlDecode(folders[i]);
                        current = TryCreateContainer(name, current);
                    }

                    return current.Id;
                }
            }

            return -1;
        }

        private EntityContainer TryCreateContainer(string name, EntityContainer parent)
        {
            LogHelper.Debug<ContentTypeSerializer>("TryCreate: {0} under {1}", () => name, () => parent.Name);

            var children = _entityService.GetChildren(parent.Id, UmbracoObjectTypes.DocumentTypeContainer).ToArray();

            if (children.Any(x => x.Name.InvariantEquals(name)))
            {
                var folderId = children.Single(x => x.Name.InvariantEquals(name)).Id;
                return _contentTypeService.GetContentTypeContainer(folderId);
            }

            // else - create 
            var attempt = _contentTypeService.CreateContentTypeContainer(parent.Id, name);
            if (attempt == true)
                return _contentTypeService.GetContentTypeContainer(attempt.Result.Entity.Id);

            LogHelper.Warn<ContentTypeSerializer>("Can't create child folders {0} you doctypes might be flat", () => name);

            return null;
        }

        public override SyncAttempt<IContentType> DeserializeContainer(XElement node)
        {
            /* NOT DOING THIS - FOLDERS ARE CREATED BY THE DOCTYPES ON A AS NEEDED BASIS */
            return SyncAttempt<IContentType>.Succeed(node.Name.LocalName, ChangeType.NoChange);
        }

        private void DeserializeCompositions(IContentType item, XElement node)
        {
            var info = node.Element("Info");
            var comps = info.Element("Compositions");
            List<IContentTypeComposition> compositions = new List<IContentTypeComposition>();
            if (comps != null && comps.HasElements)
            {
                foreach (var composition in comps.Elements("Composition"))
                {
                    var compAlias = composition.Value;

                    LogHelper.Debug<MediaTypeSerializer>("Composition: {0}", () => compAlias);
                    var compKey = composition.Attribute("Key").ValueOrDefault(Guid.Empty);
                    IContentType type = null;
                    if (compKey != Guid.Empty)
                        type = _contentTypeService.GetContentType(compKey);
                    if (type == null)
                        type = _contentTypeService.GetContentType(compAlias);
                    if (type != null)
                        // item.AddContentType(type);
                        compositions.Add(type);
                    else
                        LogHelper.Warn<ContentTypeSerializer>("Unable to find type for composition: " + compAlias);
                }
            }

            /*
            if (item.ParentId != -1 && compositions.Count == 0)
            {
                // it might be that the item was created Umbraco 7.13 - 7.13.2 as a collection and it doesn't have it's 
                // parent - we can fix that here (but should we?)
                var parent = _contentTypeService.GetContentType(item.ParentId);
                if (parent != null)
                {
                    // not a folder :) 
                    compositions.Add(parent);
                }
            }
            */

            LogHelper.Debug<ContentTypeSerializer>("Setting {0} compositions for element", () => item.ContentTypeComposition.Count());
            item.ContentTypeComposition = compositions;

        }

        private XElement SerializeTemplates(IContentType item)
        {
            var templatesNode = new XElement("AllowedTemplates");

            if (item.AllowedTemplates.Any())
            {
                foreach (var template in item.AllowedTemplates.OrderBy(x => x.Alias))
                {
                    templatesNode.Add(new XElement("Template", template.Alias));
                }
            }

            return templatesNode;
        }


        private void DeserializeTemplates(IContentType item, XElement info)
        {
            var nodeTemplates = info.Element("AllowedTemplates");
            if (nodeTemplates == null || !nodeTemplates.HasElements)
                return;

            var _fileService = ApplicationContext.Current.Services.FileService;

            List<ITemplate> templates = new List<ITemplate>();

            foreach (var template in nodeTemplates.Elements("Template"))
            {
                var alias = template.Value;
                var iTemplate = _fileService.GetTemplate(alias);
                if (iTemplate != null)
                {
                    templates.Add(iTemplate);
                }
            }
            /*
            List<ITemplate> templatesToRemove = new List<ITemplate>();
            foreach (var itemTemplate in item.AllowedTemplates)
            {
                if (nodeTemplates.Elements("Template").FirstOrDefault(x => x.Value == itemTemplate.Alias) == null)
                {
                    templatesToRemove.Add(itemTemplate);
                }
            }

            foreach (var rTemplate in templatesToRemove)
            {
                item.RemoveTemplate(rTemplate);
            }
            */

            item.AllowedTemplates = templates;
        }

        public override SyncAttempt<IContentType> DesearlizeSecondPass(IContentType item, XElement node)
        {
            DeserializeCompositions(item, node);
            DeserializeStructure((IContentTypeBase)item, node);
            _contentTypeService.Save(item);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import);
        }


        public override SyncAttempt<XElement> SerializeContainer(EntityContainer item)
        {
            return uSyncContainerHelper.SerializeContainer(item);
        }


        internal override SyncAttempt<XElement> SerializeCore(IContentType item)
        {
            // var node = _packagingService.Export(item);
            var info = SerializeInfo(item);

            // add content type/composistions
            var master = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);

            if (item.Level != 1 && master == null)
            {
                // it is possible that a doctype collection has done something here where
                // it does have a parent but its not a composition 
                master = _contentTypeService.GetContentType(item.ParentId);
            }

            if (master != null)
                info.Add(new XElement("Master", master.Alias,
                            new XAttribute("Key", master.Key)));


            if (item.Level != 1 && master == null)
            { 
                // we must be in a folder. 
                var folders = _contentTypeService.GetContentTypeContainers(item)
                    .OrderBy(x => x.Level)
                    .Select(x => HttpUtility.UrlEncode(x.Name));

                if (folders.Any())
                {
                    string path = string.Join("/", folders.ToArray());
                    info.Add(new XElement("Folder", path)); 
                }
            }

            var compositionsNode = new XElement("Compositions");
            var compositions = item.ContentTypeComposition;
            foreach (var composition in compositions.OrderBy(x => x.Key))
            {
                compositionsNode.Add(new XElement("Composition", composition.Alias,
                    new XAttribute("Key", composition.Key))
                    );
            }
            info.Add(compositionsNode);

            // Templates
            if (item.DefaultTemplate != null && item.DefaultTemplate.Id != 0)
                info.Add(new XElement("DefaultTemplate", item.DefaultTemplate.Alias));
            else
                info.Add(new XElement("DefaultTemplate", ""));


            var templates = SerializeTemplates(item);
            if (templates != null)
                info.Add(templates);

            // Structure
            var structure = SerializeStructure(item);

            // Properties
            var properties = SerializeProperties(item);

            // Tabs
            var tabs = SerializeTabs(item);

            var node = new XElement(Constants.Packaging.DocumentTypeNodeName,
                                        info,                                        
                                        structure,
                                        properties,
                                        tabs);


            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContentType), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            if (node.IsArchiveFile())
                return false;

            if (node.Name.LocalName == "EntityFolder")
                return IsContainerUpdated(node);

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var keyNode = node.Element("Info").Element("Key");
            if (keyNode == null)
                return true;

            var keyGuid = keyNode.ValueOrDefault(Guid.Empty);
            if (keyGuid == Guid.Empty)
                return true;

            var item = _contentTypeService.GetContentType(keyGuid);
            if (item == null)
                return true;

            /*
            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _contentTypeService.GetContentType(aliasNode.Value);
            if (item == null)
                return true;
            */

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));
        }

        private bool IsContainerUpdated(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return true;

            var item = _contentTypeService.GetContentTypeContainer(key);
            if (item == null)
                return true;

            var attempt = SerializeContainer(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));            
        }


        #region ISyncChangeDetail : Support for detailed change reports
        public IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            if (node.Name.LocalName == "EntityFolder")
                return GetContainerChanges(node);

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var key = node.Element("Info").Element("Key");
            if (key == null)
                return null;

            Guid itemGuid = Guid.Empty;
            if (!Guid.TryParse(key.Value, out itemGuid))
                return null;

            var item = _contentTypeService.GetContentType(itemGuid);
            if (item == null)
            {
                return uSyncChangeTracker.NewItem(key.Value);
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

        private IEnumerable<uSyncChange> GetContainerChanges(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return null;

            var item = _contentTypeService.GetContentTypeContainer(key);
            if (item == null)
                return uSyncChangeTracker.NewItem(node.Attribute("Name").ValueOrDefault("unknown"));

            var attempt = SerializeContainer(item);
            if (attempt.Success)
            {
                return uSyncChangeTracker.GetChanges(node, attempt.Item, "");
            }
            else
            {
                return uSyncChangeTracker.ChangeError(node.Attribute("Name").ValueOrDefault("unknown"));
            }
        }
        #endregion
    }
}
