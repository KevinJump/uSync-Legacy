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

namespace Jumoo.uSync.Core.Serializers
{
    public class MediaTypeSerializer : ContentTypeBaseSerializer<IMediaType>
    {
        private readonly IPackagingService _packagingService;

        public MediaTypeSerializer(string itemType) : base (itemType)
        {
            _packagingService = ApplicationContext.Current.Services.PackagingService;
        }

        internal override SyncAttempt<IMediaType> DeserializeCore(XElement node)
        {
            // we can't use the package manager for this :(
            // we have to do it by hand.
            if (node == null)
                throw new ArgumentNullException("node");

            var infoNode = node.Element("Info");
            if (infoNode == null)
                throw new ArgumentException("Bad info node");

            var alias = infoNode.Element("Alias");
            if (alias == null)
                throw new ArgumentException("Bad Alias node");

            var parent = default(IMediaType);

            var parentAlias = infoNode.Element("Master");
            if (parentAlias != null && !string.IsNullOrEmpty(parentAlias.Value))
            {
                parent = _contentTypeService.GetMediaType(parentAlias.Value);
            }

            var item = _contentTypeService.GetMediaType(alias.Value);
            if (item == null)
            {
                if (parent != default(IMediaType))
                {
                    item = new MediaType(parent, alias.Value);
                }
                else
                {
                    item = new MediaType(-1);
                    item.Alias = alias.Value;
                }
            }

            if (infoNode.Element("Name") != null)
                item.Name = infoNode.Element("Name").Value;

            if (infoNode.Element("Icon") != null)
                item.Icon = infoNode.Element("Icon").Value;

            if (infoNode.Element("Thumbnail") != null)
                item.Thumbnail = infoNode.Element("Thumbnail").Value;

            if (infoNode.Element("Description") != null)
                item.Description= infoNode.Element("Description").Value;

            if (infoNode.Element("AllowAtRoot") != null)
                item.AllowedAsRoot = infoNode.Element("AllowAtRoot").Value.ToLowerInvariant().Equals("true");

            DeserializeProperties(item, node);
            DeserializeTabSortOrder(item, node);

            // this really needs to happen in a seperate step.
            // DeserializeStructure(item, node);

            _contentTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, typeof(IMedia), ChangeType.Import);          
        }

        public override SyncAttempt<IMediaType> DesearlizeSecondPass(IMediaType item, XElement node)
        {
            DeserializeStructure((IContentTypeBase)item, node);
            _contentTypeService.Save(item);

            return SyncAttempt<IMediaType>.Succeed(item.Name, item, typeof(IMedia), ChangeType.Import);
        }


        internal override SyncAttempt<XElement> SerializeCore(IMediaType item)
        {
            LogHelper.Debug<MediaTypeSerializer>("MediaType Serializer");

            if (item == null)
                throw new ArgumentNullException("item");

            var info = new XElement("Info",
                            new XElement("Name", item.Name),
                            new XElement("Alias", item.Alias),
                            new XElement("Icon", item.Icon),
                            new XElement("Thumbnail", item.Thumbnail),
                            new XElement("Description", item.Description),
                            new XElement("AllowAtRoot", item.AllowedAsRoot.ToString()));


            var masterItem = item.CompositionAliases().FirstOrDefault();
            if (masterItem != null)
                info.Add(new XElement("Master", masterItem));

            var tabs = new XElement("Tabs");
            foreach(var propertyGroup in item.PropertyGroups)
            {
                tabs.Add(new XElement("Tab",
                                new XElement("Id", propertyGroup.Id.ToString()),
                                new XElement("Caption", propertyGroup.Name),
                                new XElement("SortOrder", propertyGroup.SortOrder)));

            }

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

            var structure = new XElement("Structure");

            var node = new XElement("MediaType", 
                                        info,
                                        structure, 
                                        properties, 
                                        tabs);

            node = SerializeStructure(item, node);

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
