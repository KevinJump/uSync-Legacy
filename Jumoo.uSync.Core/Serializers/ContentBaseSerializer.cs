using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Core.Models;

using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Interfaces;
using System.Text.RegularExpressions;
using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.Core.Serializers
{
    abstract public class ContentBaseSerializer<T> : SyncBaseSerializer<T>, ISyncSerializerWithParent<T>
    {
        internal IContentService _contentService;
        internal IMediaService _mediaService;

        public ContentBaseSerializer(string type) : base(type)
        {
            _contentService = ApplicationContext.Current.Services.ContentService;
            _mediaService = ApplicationContext.Current.Services.MediaService;
        }

        public SyncAttempt<T> Deserialize(XElement node, bool forceUpdate, bool onePass)
        {
            return Deserialize(node, -1, forceUpdate);
        }

        public SyncAttempt<T> Deserialize(XElement node, int parentId, bool forceUpdate = false)
        {
            // for content, we always call deserialize, because the first step will 
            // do the item lookup, and we want to return item so we can import 
            // as part of a tree. 
            return DeserializeCore(node, parentId, forceUpdate);
        }

        abstract internal SyncAttempt<T> DeserializeCore(XElement node, int parentId, bool forceUpdate);

        internal override SyncAttempt<T> DeserializeCore(XElement node)
        {
            return Deserialize(node, -1);
        }

        /// <summary>
        ///  second pass ID update in properties? 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="item"></param>
        public void DeserializeMappedIds(T baseItem, XElement node)
        {
            IContentBase item = (IContentBase)baseItem;
            var properties = node.Elements().Where(x => x.Attribute("isDoc") == null);
            foreach (var property in properties)
            {
                var propertyTypeAlias = property.Name.LocalName;
                if (item.HasProperty(propertyTypeAlias))
                {
                    string newValue = GetImportIds(GetImportXml(property));

                    var prop = item.Properties[propertyTypeAlias];

                    if (prop.PropertyType.PropertyEditorAlias == "Umbraco.RadioButtonList" || prop.PropertyType.PropertyEditorAlias == "Umbraco.DropDownList")
                    {
                        var prevalues =
                            ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(prop.PropertyType.DataTypeDefinitionId)
                                              .PreValuesAsDictionary;

                        if (prevalues != null && prevalues.Count > 0)
                        {
                            string preValue = prevalues.Where(kvp => kvp.Key.ToString() == newValue).Select(kvp => kvp.Value.Id.ToString()).SingleOrDefault();

                            if (!String.IsNullOrWhiteSpace(preValue))
                            {
                                newValue = preValue;
                            }
                        }
                    }

                    item.SetValue(propertyTypeAlias, newValue);
                }
            }
        }

        virtual public void PublishOrSave(T item, bool published, bool raiseEvents) { }


        internal int GetIdFromGuid(Guid guid)
        {
            var item = ApplicationContext.Current.Services.EntityService.GetByKey(guid);
            if (item != null)
                return item.Id;

            return -1;
        }

        internal Guid? GetGuidFromId(int id)
        {
            var item = ApplicationContext.Current.Services.EntityService.Get(id);
            if (item != null)
                return item.Key;

            return null;
        }
        

        internal string GetImportIds(string content)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            string guidRegEx = @"\b[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12}\b";

            foreach(Match m in Regex.Matches(content, guidRegEx))
            {
                var id = GetIdFromGuid(Guid.Parse(m.Value));

                if ((id != -1) && (!replacements.ContainsKey(m.Value)))
                {
                    replacements.Add(m.Value, id.ToString()); 
                }
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                content = content.Replace(pair.Key, pair.Value);
            }

            return content; 
        }


        internal string GetImportXml(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            string xml = reader.ReadInnerXml();

            if (xml.StartsWith("<![CDATA["))
                return parent.Value;
            else
                return xml.Replace("&amp;", "&");
        }

        internal SyncAttempt<XElement> SerializeBase(IContentBase item, string contentTypeAlias)
        {
            var node = new XElement(contentTypeAlias);

            node.Add(new XAttribute("guid", item.Key));
            node.Add(new XAttribute("id", item.Id));
            node.Add(new XAttribute("nodeName", item.Name));
            node.Add(new XAttribute("isDoc", ""));
            node.Add(new XAttribute("updated", item.UpdateDate));

            foreach (var prop in item.Properties.Where(p => p != null))
            {
                XElement propNode = null;

                try
                {
                    propNode = prop.ToXml();
                }
                catch
                {
                    propNode = new XElement(prop.Alias, prop.Value);
                }

                string xml = "";
                xml = GetExportIds(GetInnerXml(propNode));

                if (prop.PropertyType.PropertyEditorAlias == "Umbraco.RadioButtonList" || prop.PropertyType.PropertyEditorAlias == "Umbraco.DropDownList")
                {
                    var prevalues =
                        ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(prop.PropertyType.DataTypeDefinitionId).PreValuesAsDictionary;

                    if (prevalues != null && prevalues.Count > 0)
                    {
                        string preValue = prevalues.Where(kvp => prop.Value != null && kvp.Value.Id == (int)prop.Value).Select(kvp => kvp.Key.ToString()).SingleOrDefault();

                        if (!String.IsNullOrWhiteSpace(preValue))
                        {
                            xml = preValue;
                        }
                    }
                }

                var updatedNode = XElement.Parse(
                    string.Format("<{0}>{1}</{0}>", propNode.Name.ToString(), xml), LoadOptions.PreserveWhitespace);

                node.Add(updatedNode);
            }
            return SyncAttempt<XElement>.Succeed(item.Name, node, item.GetType(), ChangeType.Export);
        }

        private string GetExportIds(string value)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            foreach(Match m in Regex.Matches(value, @"\d{4,9}"))
            {
                int id;
                if (int.TryParse(m.Value, out id))
                {
                    Guid? itemGuid = GetGuidFromId(id);
                    if (itemGuid != null && !replacements.ContainsKey(m.Value))
                    {
                        replacements.Add(m.Value, itemGuid.ToString().ToUpper());
                    }
                }
            }

            foreach(var pair in replacements)
            {
                value = value.Replace(pair.Key, pair.Value);
            }

            return value;
        }

        private string GetInnerXml(XElement parent)
        {
            var reader = parent.CreateReader();
            reader.MoveToContent();
            return reader.ReadInnerXml();
        }

        virtual public SyncAttempt<T> DesearlizeSecondPass(T item, XElement node)
        {
            return SyncAttempt<T>.Succeed(node.NameFromNode(), ChangeType.NoChange);
        }
    }
}
