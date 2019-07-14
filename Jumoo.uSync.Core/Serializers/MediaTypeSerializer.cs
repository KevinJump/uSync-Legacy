using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;
using Jumoo.uSync.Core.Helpers;
using System.Web;

namespace Jumoo.uSync.Core.Serializers
{
    public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>, ISyncChangeDetail
    {

        public override string SerializerType { get { return uSyncConstants.Serailization.MediaType; } }

        public MediaTypeSerializer() 
            : base("MediaType", UmbracoObjectTypes.MediaType) { }

        public MediaTypeSerializer(string itemType) 
            : base (itemType, UmbracoObjectTypes.MediaType) { }

        internal override SyncAttempt<IMediaType> DeserializeCore(XElement node)
        {

            if (node.Name.LocalName == "EntityFolder")
                return DeserializeContainer(node);

            // we can't use the package manager for this :(
            // we have to do it by hand.
            if (node == null || node.Element("Info") == null || node.Element("Info").Element("Alias") == null)
                throw new ArgumentException("Invalid xml");

            var info = node.Element("Info");

            IMediaType item = null;

            Guid key = Guid.Empty;
            if ((info.Element("Key") != null && Guid.TryParse(info.Element("Key").Value, out key)))
            {
                // we have key.
                try {
                    item = _contentTypeService.GetMediaType(key);
                }
                catch(Exception ex)
                {
                    LogHelper.Warn<MediaTypeSerializer>("Wobbler looking for media type: {0} {1}", () => key, ()=> ex.ToString());
                }
            }

            // you need the parent to create, so do it here...
            // var parent = default(IMediaType);
            var parentId = -1;
            // var folderId = -1;

            
            var parentAlias = info.Element("Master");
            if (parentAlias != null && !string.IsNullOrEmpty(parentAlias.Value))
            {
                // legacy master upgrade - when master is set we coming from
                // an old version of umbraco, under 7.4.x we can't have master types
                // for media, so we make the master a composition.

                // because we didn't support compostions while we where writing out
                // the master value we can 'assume' that the compositions node is 
                // empty? 
                LogHelper.Debug<MediaTypeSerializer>("Master -> Composition: {0}", () => parentAlias.Value);

                var parent = _contentTypeService.GetMediaType(parentAlias.Value);

                XElement compositionsNode = info.Element("Compositions");
                if (compositionsNode == null)
                    compositionsNode = new XElement("Compositions");

                compositionsNode.Add(new XElement("Composition", parentAlias.Value, new XAttribute("Key", parent.Key)));
                info.Add(compositionsNode);
            }

            /*
            if (parentId == -1)
            {
                folderId = GetMediaFolders(info, item);
                parentId = folderId; 
            }
            */

            parentId = GetMediaFolders(info, item);

            var alias = info.Element("Alias").Value;

            // can't find by key, lookup by alias.
            if (item == null)
            {
                LogHelper.Debug<MediaTypeSerializer>("Looking up media type by alias");
                item = _contentTypeService.GetMediaType(alias);
            }

            if (item == null)
            {
                LogHelper.Debug<MediaTypeSerializer>("Creating new Media Type");

                item = new MediaType(parentId)
                {
                    Alias = alias
                };
            }

            if (item.Key != key)
                item.Key = key;

            DeserializeBase(item, info);

            item.SetLazyParentId(new Lazy<int>(() => parentId));

            // Update Tabs before props -- allows moving props to new tabs in sync
            DeserializeTabSortOrder(item, node);

            var msg = DeserializeProperties(item, node);

            CleanUpTabs(item, node);

            // this really needs to happen in a seperate step.
            // DeserializeStructure(item, node);

            _contentTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, ChangeType.Import, msg);          
        }

        private int GetMediaFolders(XElement info, IMediaType item)
        {
            var path = info.Element("Folder").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                // create the folders at each level, and then return the topmost folder as the id (we set it as parent)
                var folders = path.Split('/');
                var rootFolder = HttpUtility.UrlDecode(folders[0]);

                var rootId = -1;
                var root = _contentTypeService.GetMediaTypeContainers(rootFolder, 1).FirstOrDefault();
                if (root == null)
                {
                    var attempt = _contentTypeService.CreateMediaTypeContainer(-1, rootFolder);
                    if (attempt == false)
                    {
                        // something amis
                        LogHelper.Warn<MediaTypeSerializer>("Can't create the root folder something is not right - you doc types might be a little flat");
                        return -1;
                    }
                    rootId = attempt.Result.Entity.Id;
                }
                else {
                    rootId = root.Id;
                }

                if (rootId != -1)
                {
                    var current = _contentTypeService.GetMediaTypeContainer(rootId);

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

            var children = _entityService.GetChildren(parent.Id, UmbracoObjectTypes.MediaTypeContainer).ToArray();

            if (children.Any(x => x.Name.InvariantEquals(name)))
            {
                var folderId = children.Single(x => x.Name.InvariantEquals(name)).Id;
                return _contentTypeService.GetMediaTypeContainer(folderId);
            }

            // else - create 
            var attempt = _contentTypeService.CreateMediaTypeContainer(parent.Id, name);
            if (attempt == true)
                return _contentTypeService.GetMediaTypeContainer(attempt.Result.Entity.Id);

            LogHelper.Warn<ContentTypeSerializer>("Can't create child folders {0} you doctypes might be flat", () => name);

            return null;
        }



        public override SyncAttempt<IMediaType> DeserializeContainer(XElement node)
        {
            return SyncAttempt<IMediaType>.Succeed(node.Name.LocalName, ChangeType.NoChange);
        }

        private void DeserializeCompositions(IMediaType item, XElement node)
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
                    IMediaType type = null;
                    if (compKey != Guid.Empty)
                        type = _contentTypeService.GetMediaType(compKey);
                    if (type == null)
                        type = _contentTypeService.GetMediaType(compAlias);
                    if (type != null)
                        compositions.Add(type);
                    else
                        LogHelper.Warn<MediaTypeSerializer>("Unable to find type for composition: " + compAlias);
                }
            }
            LogHelper.Debug<MediaTypeSerializer>("Setting {0} compositions for element", () => item.ContentTypeComposition.Count());
            item.ContentTypeComposition = compositions;
        }

        public override SyncAttempt<IMediaType> DesearlizeSecondPass(IMediaType item, XElement node)
        {
            DeserializeCompositions(item, node);
            DeserializeStructure((IContentTypeBase)item, node);
            _contentTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, ChangeType.Import);
        }

        public override SyncAttempt<XElement> SerializeContainer(EntityContainer item)
        {
            return uSyncContainerHelper.SerializeContainer(item);
        }
      

        internal override SyncAttempt<XElement> SerializeCore(IMediaType item)
        {
            LogHelper.Debug<MediaTypeSerializer>("MediaType Serializer");

            if (item == null)
                throw new ArgumentNullException("item");

            var info = SerializeInfo(item);

            /*
            var masterItem = item.CompositionAliases().FirstOrDefault();
            if (masterItem != null)
                info.Add(new XElement("Master", masterItem));

            // in v7.4.x media can't have a parent (it's compositons only)
            */

            if (item.Level != 1)
            {
                var folders = _contentTypeService.GetMediaTypeContainers(item)
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

            var tabs = SerializeTabs(item);

            var properties = SerializeProperties(item);

            var structure = SerializeStructure(item);

            var node = new XElement("MediaType",
                                        info,
                                        structure,
                                        properties,
                                        tabs);

            
            LogHelper.Debug<MediaTypeSerializer>("Media Serializer Complete");

            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IMedia), ChangeType.Export);
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

            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _contentTypeService.GetMediaType(aliasNode.Value);
            if (item == null)
                return true;

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

            var item = _contentTypeService.GetMediaTypeContainer(key);
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

            var item = _contentTypeService.GetMediaType(itemGuid);
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

            var item = _contentTypeService.GetMediaTypeContainer(key);
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
