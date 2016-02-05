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

namespace Jumoo.uSync.Core.Serializers
{
    public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>
    {
        public MediaTypeSerializer(string itemType) : base (itemType)
        { }

        internal override SyncAttempt<IMediaType> DeserializeCore(XElement node)
        {

            if (node.Name.LocalName == "EntityFolder")
                return DeserializeContainer(node);

            // we can't use the package manager for this :(
            // we have to do it by hand.
            if (node == null | node.Element("Info") == null || node.Element("Info").Element("Alias") == null)
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
                    LogHelper.Warn<MediaTypeSerializer>("Wobbler looking for media type: {0}", () => key);
                }
            }

            // you need the parent to create, so do it here...
            var parent = default(IMediaType);
            var parentAlias = info.Element("Master");
            if (parentAlias != null && !string.IsNullOrEmpty(parentAlias.Value))
            {
                parent = _contentTypeService.GetMediaType(parentAlias.Value);
            }

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
                if (parent != default(IMediaType))
                {
                    item = new MediaType(parent, alias);
                }
                else
                {
                    item = new MediaType(-1);
                    item.Alias = alias;
                }
            }

            if (item.Key != key)
                item.Key = key;

            DeserializeBase(item, info);

            DeserializeProperties(item, node);

            DeserializeTabSortOrder(item, node);

            // this really needs to happen in a seperate step.
            // DeserializeStructure(item, node);

            _contentTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, ChangeType.Import);          
        }

        public override SyncAttempt<IMediaType> DeserializeContainer(XElement node)
        {
            var name = node.Attribute("Name").ValueOrDefault(string.Empty);
            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            var parentId = node.Attribute("ParentId").ValueOrDefault(-1);

            var item = _contentTypeService.GetMediaTypeContainer(key);
            if (item == null)
            {
                var attempt = _contentTypeService.CreateMediaTypeContainer(parentId, name);
                if (attempt.Success)
                    item = _contentTypeService.GetMediaTypeContainer(attempt.Result.Entity.Id);
            }

            if (item != null)
            {
                if (item.Name != name)
                    item.Name = name;

                if (item.Key != key)
                    item.Key = key;

                _contentTypeService.SaveMediaTypeContainer(item);

                return SyncAttempt<IMediaType>.Succeed(item.Name, null, ChangeType.Import);
            }

            return SyncAttempt<IMediaType>.Fail(name, ChangeType.ImportFail);
        }


        public override SyncAttempt<IMediaType> DesearlizeSecondPass(IMediaType item, XElement node)
        {
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
            var info = new XElement("Info",
                            new XElement("Name", item.Name),
                            new XElement("Alias", item.Alias),
                            new XElement("Icon", item.Icon),
                            new XElement("Thumbnail", item.Thumbnail),
                            new XElement("Description", item.Description),
                            new XElement("AllowAtRoot", item.AllowedAsRoot.ToString()));
            */

            var masterItem = item.CompositionAliases().FirstOrDefault();
            if (masterItem != null)
                info.Add(new XElement("Master", masterItem));

            var tabs = SerializeTabs(item);
            /*
            var tabs = new XElement("Tabs");
            foreach(var propertyGroup in item.PropertyGroups)
            {
                tabs.Add(new XElement("Tab",
                                new XElement("Id", propertyGroup.Id.ToString()),
                                new XElement("Caption", propertyGroup.Name),
                                new XElement("SortOrder", propertyGroup.SortOrder)));

            }
            */

            var properties = SerializeProperties(item);
            /*
            var properties = new XElement("GenericProperties");

            var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            foreach(var property in item.PropertyTypes.OrderBy(x => x.Alias))
            {
                var def = _dataTypeService.GetDataTypeDefinitionById(property.DataTypeDefinitionId);
                var tab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));

                LogHelper.Debug<MediaTypeSerializer>("Adding Property: {0}", ()=> property.Alias);

                var propNode = new XElement("GenericProperty",
                                        new XElement("Name", property.Name),
                                        new XElement("Alias", property.Alias),
                                        new XElement("Type", property.PropertyEditorAlias),
                                        new XElement("Definition", def.Key),
                                        new XElement("Tab", tab == null ? "" : tab.Name),
                                        new XElement("Mandatory", property.Mandatory.ToString()),
                                        new XElement("Validation", property.ValidationRegExp ?? ""),
                                        new XElement("Description", new XCData(property.Description ?? "")));

                properties.Add(propNode);
            }
            */

            var structure = SerializeStructure(item);
            /*
            var structure = new XElement("Structure");

            LogHelper.Debug<MediaTypeSerializer>("Content Types: {0}", () => item.AllowedContentTypes.Count());

            var node = new XElement("MediaType", 
                                        info,
                                        structure, 
                                        properties, 
                                        tabs);

            node = SerializeStructure(item, node);
            */

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
    }
}
