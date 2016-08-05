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
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Serializers
{
    public class ContentSerializer : ContentBaseSerializer<IContent>
    {
        public ContentSerializer() : base(string.Empty)
        { }

        internal override SyncAttempt<IContent> DeserializeCore(XElement node, int parentId, bool forceUpdate = false)
        {
            var nodeGuid = node.Attribute("guid");
            if (nodeGuid == null)
                return SyncAttempt<IContent>.Fail(node.NameFromNode(), ChangeType.Import, "No Guid in XML");

            Guid guid = new Guid(nodeGuid.Value);

            var name = node.Attribute("nodeName").Value;
            var type = node.Attribute("nodeTypeAlias").Value;
            var templateAlias = node.Attribute("templateAlias").Value;

            var sortOrder = int.Parse(node.Attribute("sortOrder").Value);
            var published = bool.Parse(node.Attribute("published").Value);

            // because later set the guid, we are going for a match at this point
            var item = _contentService.GetById(guid);
            if (item == null)
            {
                item = _contentService.CreateContent(name, parentId, type);
            }
            else { 
                // update is different for content, we go on publish times..
                if (!forceUpdate)
                {
                    DateTime updateTime = node.Attribute("updated").ValueOrDefault(DateTime.Now);

                    if ((updateTime - item.UpdateDate).TotalSeconds < 0)
                    {
                        // the import is older than the content on this site;
                        return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.NoChange);
                    }
                }
            }

            if (item == null)
                return SyncAttempt<IContent>.Fail(node.NameFromNode(), ChangeType.ImportFail, "Cannot find or create content item");

            // if it's in the trash remove it. 
            if (item.Trashed)
                item.ChangeTrashedState(false);

            //
            // Change doctype if it changes, we could lose values here, but we 
            // are going to set them all later so should be fine. 
            //
            var contentType = ApplicationContext.Current.Services.ContentTypeService.GetContentType(type);
            if (contentType != null && item.ContentTypeId != contentType.Id)
            {
                item.ChangeContentType(contentType);
            }

            var template = ApplicationContext.Current.Services.FileService.GetTemplate(templateAlias);
            if (template != null)
                item.Template = template;

            item.Key = guid;

            item.SortOrder = sortOrder;
            item.Name = name;

            if (item.ParentId != parentId)
                item.ParentId = parentId;

            var properties = node.Elements().Where(x => x.Attribute("isDoc") == null);
            foreach (var property in properties)
            {
                var propertyTypeAlias = property.Name.LocalName;
                if (item.HasProperty(propertyTypeAlias))
                {
                    var propType = item.Properties[propertyTypeAlias].PropertyType;
                    var newValue = GetImportIds(propType, GetImportXml(property));

                    LogHelper.Debug<Events>("#### Setting property: [{0}] to {1}", () => propertyTypeAlias, () => newValue);
                    try
                    {
                        item.SetValue(propertyTypeAlias, newValue);
                    }
                    catch (InvalidOperationException ex)
                    {
                        LogHelper.Warn<ContentBaseSerializer<T>>(
                            "Setting a value didn't work. Tried to set value '{0}' to the property '{1}' on '{2}'. Exception: {3}",
                            () => newValue, () => propertyTypeAlias, () => item.Name, () => ex.Message);
                    }
                }

            }

            PublishOrSave(item, published);

            return SyncAttempt<IContent>.Succeed(item.Name, item, ChangeType.Import);
        }

        /// <summary>
        ///  called from teh base when things change, we need to save or publish our content
        /// </summary>
        /// <param name="item"></param>
        public override void PublishOrSave(IContent item, bool published, bool raiseEvents = false )
        {
            if (published)
            {
                var publishAttempt = _contentService.SaveAndPublishWithStatus(item, 0, raiseEvents);
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
        }

        internal override SyncAttempt<XElement> SerializeCore(IContent item)
        {
            LogHelper.Debug<ContentSerializer>("Serialize Core: {0}", () => item.Name);

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

            LogHelper.Debug<ContentSerializer>("Returning Node");
            return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IContent), ChangeType.Export);
        }

        public override bool IsUpdate(XElement node)
        {
            var key = node.Attribute("guid").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return true;

            var item = _contentService.GetById(key);
            if (item == null)
                return true;

            DateTime updateTime = node.Attribute("updated").ValueOrDefault(DateTime.Now);
            if ((updateTime - item.UpdateDate).TotalSeconds > 1)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public override SyncAttempt<IContent> DesearlizeSecondPass(IContent item, XElement node)
        {
            base.DeserializeMappedIds(item, node);

            int sortOrder = node.Attribute("sortOrder").ValueOrDefault(-1);
            if (sortOrder >= 0)
                item.SortOrder = sortOrder;

            var published = node.Attribute("published").ValueOrDefault(false);

            PublishOrSave(item, published, true);


            return SyncAttempt<IContent>.Succeed(item.Name, ChangeType.Import);
        }
   
    }
}
