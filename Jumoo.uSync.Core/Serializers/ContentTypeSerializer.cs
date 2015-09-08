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

namespace Jumoo.uSync.Core.Serializers
{
    public class ContentTypeSerializer : ContentTypeBaseSerializer<IContentType> 
    {
        public ContentTypeSerializer(string itemType) : base (itemType)
        {
        }

        internal override SyncAttempt<IContentType> DeserializeCore(XElement node)
        {
            // we can't use the package manager for this :(
            // we have to do it by hand.
            if (node == null | node.Element("Info") == null || node.Element("Info").Element("Alias") == null)
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
            var parentAlias = info.Element("Master");
            if (parentAlias != null && !string.IsNullOrEmpty(parentAlias.Value))
            {
                var masterKey = parentAlias.Attribute("Key").ValueOrDefault(Guid.Empty);
                if (masterKey != null)
                {
                    parent = _contentTypeService.GetContentType(masterKey);
                }

                if (parent == null)
                    parent = _contentTypeService.GetContentType(parentAlias.Value);
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
                if (parent != default(IMediaType))
                {
                    item = new ContentType(parent, alias);
                }
                else
                {
                    item = new ContentType(-1) { Alias = alias };
                }

                if (parent != null)
                    item.AddContentType(parent);
            }

            DeserializeBase(item, info);

            
            if (item.Key != key)
            {
                LogHelper.Debug<ContentTypeSerializer>("Changing Item Key: {0} -> {1}",
                    () => item.Key, () => key);
                item.Key = key;
            }

            // _contentTypeService.Save(item);

            // Update Properties
            DeserializeProperties((IContentTypeBase)item, node);

            // Update Tabs
            DeserializeTabSortOrder((IContentTypeBase)item, node);

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

            DeserializeCompositions(item, info);

            DeserializeTemplates(item, info);

            _contentTypeService.Save(item);
            // Update Structure (Happens in second pass)
            // need to consider if we also call it here
            // as that will simplify single calling apps,
            // but slow down bulk operations as we will be doing
            // structure twice.
            // DeserializeStructure((IContentTypeBase)item, node);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import);
        }

        private void DeserializeCompositions(IContentType item, XElement info)
        {
            var comps = info.Element("Compositions");
            if (comps != null && comps.HasElements)
            {
                foreach (var composistion in comps.Elements("Composition"))
                {
                    var compAlias = composistion.Value;
                    var compKey = composistion.Attribute("Key").ValueOrDefault(Guid.Empty);

                    IContentType type = null;

                    if (compKey != Guid.Empty)
                        type = _contentTypeService.GetContentType(compKey);

                    if (type == null)
                        type = _contentTypeService.GetContentType(compAlias);

                    if (type != null)
                        item.AddContentType(type);
                }
            }
        }

        private void DeserializeTemplates(IContentType item, XElement info)
        {
            var nodeTemplates = info.Element("AllowedTemplates");
            if (nodeTemplates == null || !nodeTemplates.HasElements)
                return;

            var _fileService = ApplicationContext.Current.Services.FileService;

            List<ITemplate> templates = new List<ITemplate>();

            foreach(var template in nodeTemplates.Elements("Template"))
            {
                var alias = template.Value;
                var iTemplate = _fileService.GetTemplate(alias);
                if (template != null)
                {
                    templates.Add(iTemplate);
                }
            }

            List<ITemplate> templatesToRemove = new List<ITemplate>();
            foreach(var itemTemplate in item.AllowedTemplates)
            {
                if (nodeTemplates.Elements("Template").FirstOrDefault(x => x.Value == itemTemplate.Alias) == null)
                {
                    templatesToRemove.Add(itemTemplate);
                }
            }

            foreach(var rTemplate in templatesToRemove)
            {
                item.RemoveTemplate(rTemplate);
            }
        }

        public override SyncAttempt<IContentType> DesearlizeSecondPass(IContentType item, XElement node)
        {
            DeserializeStructure((IContentTypeBase)item, node);
            _contentTypeService.Save(item);

            return SyncAttempt<IContentType>.Succeed(item.Name, item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(IContentType item)
        {
            // var node = _packagingService.Export(item);
            var info = SerializeInfo(item);

            // add content type/composistions
            var master = item.ContentTypeComposition.FirstOrDefault(x => x.Id == item.ParentId);
            if (master != null)
                info.Add(new XElement("Master", master.Alias,
                            new XAttribute("Key", master.Key)));

            var compositionsNode = new XElement("Compositions");
            var compositions = item.ContentTypeComposition;
            foreach(var composition in compositions)
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
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var aliasNode = node.Element("Info").Element("Alias");
            if (aliasNode == null)
                return true;

            var item = _contentTypeService.GetContentType(aliasNode.Value);
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
