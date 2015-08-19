using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;

using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Extensions;
using Umbraco.Core;
using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Core.Serializers
{
    public class ContentSerializer : ContentBaseSerializer<IContent>
    {
        public ContentSerializer() : base(string.Empty)
        { }

        internal override SyncAttempt<IContent> DeserializeCore(XElement node, int parentId, bool forceUpdate = false)
        {
            var newItem = false; 

            var nodeGuid = node.Attribute("guid");
            if (nodeGuid == null)
                return SyncAttempt<IContent>.Fail(ChangeType.Import, "No Guid in XML");

            Guid sourceGuid = new Guid(nodeGuid.Value);
            Guid targetGuid = uSyncIdMapper.GetTargetGuid(sourceGuid);

            var name = node.Attribute("nodeName").Value;
            var type = node.Attribute("nodeTypeAlias").Value;
            var templateAlias = node.Attribute("templateAlias").Value;

            var sortOrder = int.Parse(node.Attribute("sortOrder").Value);
            var published = bool.Parse(node.Attribute("published").Value);

            var item = _contentService.GetById(targetGuid);
            if (item == null || item.Trashed)
            {
                item = _contentService.CreateContent(name, parentId, type);
                newItem = true;
            }
            else
            {
                // update is different for content, we go on publish times..
                if (!forceUpdate)
                {
                    DateTime updateTime = node.Attribute("updated").ValueOrDefault(DateTime.Now);

                    if (DateTime.Compare(updateTime, item.UpdateDate.ToLocalTime()) <= 0)
                    {
                        // the import is older than the content on this site;
                        return SyncAttempt<IContent>.Succeed(item, ChangeType.NoChange);
                    }
                }
            }

            if (item == null)
                return SyncAttempt<IContent>.Fail(ChangeType.ImportFail, "Cannot find or create content item");

            var template = ApplicationContext.Current.Services.FileService.GetTemplate(templateAlias);
            if (template != null)
                item.Template = template;

            item.SortOrder = sortOrder;
            item.Name = name;

            if (item.ParentId != parentId)
                item.ParentId = parentId;

            var properties = node.Elements().Where(x => x.Attribute("isDoc") == null);
            foreach(var property in properties)
            {
                var propertyTypeAlias = property.Name.LocalName;
                if (item.HasProperty(propertyTypeAlias))
                {
                    item.SetValue(propertyTypeAlias, GetImportIds(GetImportXml(property)));
                }
            }

            if (published)
            {
                var publishAttempt = _contentService.SaveAndPublishWithStatus(item, 0, false);
                if (!publishAttempt.Success)
                {
                    // publish didn't work :(
                }
            }
            else
            {
                _contentService.Save(item, 0, false);
                if (item.Published)
                    _contentService.UnPublish(item);
            }

            if (newItem)
                uSyncIdMapper.AddPair(sourceGuid, item.Key);

            return SyncAttempt<IContent>.Succeed(item, ChangeType.Import);
        }

        internal override SyncAttempt<XElement> SerializeCore(IContent item)
        {
            var ContentTypeAlias = item.ContentType.Alias;
            var attempt = base.SerializeBase(item, ContentTypeAlias);

            if (!attempt.Success)
                return attempt;

            var node = attempt.Item;

            // content specifics..
            node.Add(new XAttribute("parentGUID", item.Level > 1 ? item.Parent().Key : new Guid("00000000-0000-0000-0000-000000000000")));
            node.Add(new XAttribute("nodeTypeAlias", item.ContentType.Alias));
            node.Add(new XAttribute("templateAlias", item.Template == null ? "" : item.Template.Alias));

            node.Add(new XAttribute("sortOrder", item.SortOrder));
            node.Add(new XAttribute("published", item.Published));

            return SyncAttempt<XElement>.Succeed(node, ChangeType.Export);
        }
    }
}
