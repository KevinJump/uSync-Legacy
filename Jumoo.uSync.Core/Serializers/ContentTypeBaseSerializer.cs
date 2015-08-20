using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using Jumoo.uSync.Core.Interfaces;
using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.Core.Serializers
{
    abstract public class ContentTypeBaseSerializer<T> : SyncBaseSerializer<T>, ISyncSerializerTwoPass<T>
    {
        internal IContentTypeService _contentTypeService;

        public ContentTypeBaseSerializer(string itemType): base(itemType)
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
        }

        #region ContentTypeBase Deserialize Helpers

        internal void DeserializeStructure(IContentTypeBase item, XElement node)
        {
            var structureNode = node.Element("Structure");
            if (structureNode == null)
                return;

            List<ContentTypeSort> allowedTypes = new List<ContentTypeSort>();
            int sortOrder = 0;

            foreach (var contentBaseNode in structureNode.Elements(_itemType))
            {
                var alias = contentBaseNode.Value;
                if (!string.IsNullOrEmpty(alias))
                {
                    IContentTypeBase contentBaseItem = default(IContentTypeBase);
                    if ( _itemType == Constants.Packaging.DocumentTypeNodeName)
                    {
                        contentBaseItem = _contentTypeService.GetContentType(alias);
                    }
                    else
                    {
                        contentBaseItem = _contentTypeService.GetMediaType(alias);
                    }

                    if (contentBaseItem != default(IContentTypeBase))
                    {
                        allowedTypes.Add(new ContentTypeSort(
                            new Lazy<int>(() => contentBaseItem.Id), sortOrder, contentBaseItem.Name));
                        sortOrder++;
                    }
                }
            }

            item.AllowedContentTypes = allowedTypes;
        }

        internal void DeserializeProperties(IContentTypeBase item, XElement node)
        {
            List<string> propertiesToRemove = new List<string>();
            Dictionary<string, string> propertiesToMove = new Dictionary<string, string>();
            Dictionary<PropertyGroup, string> tabsToBlank = new Dictionary<PropertyGroup, string>();


            var propertyNodes = node.Elements("GenericProperties").Elements("GenericProperty");

            foreach(var property in item.PropertyTypes)
            {
                XElement propertyNode = propertyNodes
                                            .Where(x => x.Element("Alias").Value == property.Alias)
                                            .SingleOrDefault();

                if (propertyNodes == null)
                {
                    propertiesToRemove.Add(property.Alias);
                }
                else
                {
                    // update existing settings.
                    if (propertyNode.Element("Name") != null)
                        property.Name = propertyNode.Element("Name").Value;

                    if (propertyNode.Element("Description") != null)
                        property.Description = propertyNode.Element("Description").Value;

                    if (propertyNode.Element("Mandatory") != null)
                        property.Mandatory = propertyNode.Element("Mandatory").Value.ToLowerInvariant().Equals("true");

                    if (propertyNode.Element("Validation") != null)
                        property.ValidationRegExp= propertyNode.Element("Validation").Value;

                    if (propertyNode.Element("SortOrder") != null)
                        property.SortOrder = int.Parse(propertyNode.Element("SortOrder").Value);

                    if (propertyNode.Element("Tab") != null)
                    {
                        var nodeTab = propertyNode.Element("Tab").Value;
                        if (!string.IsNullOrEmpty(nodeTab))
                        {
                            var propGroup = item.PropertyGroups.FirstOrDefault(x => x.Name == nodeTab);

                            if (propGroup != null)
                            {
                                if (!propGroup.PropertyTypes.Any(x => x.Alias == property.Alias))
                                {
                                    // this tab currently doesn't contain this property, to we have to
                                    // move it (later)
                                    propertiesToMove.Add(property.Alias, nodeTab);
                                }
                            }
                        }
                        else
                        {
                            // this property isn't in a tab (now!)

                            var existingTab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));
                            if (existingTab != null)
                            {
                                // this item is now not in a tab (when it was)
                                // so we have to remove it from tabs (later)
                                tabsToBlank.Add(existingTab, property.Alias);
                            }
                        }

                    }
                }
            }


            // now we have gone through all the properties, we can do the moves and removes from the groups
            if (propertiesToMove.Any())
            {
                foreach (var move in propertiesToMove)
                {
                    item.MovePropertyType(move.Key, move.Value);
                }
            }

            if (propertiesToRemove.Any())
            {
                // removing properties can cause timeouts on installs with lots of content...
                foreach(var delete in propertiesToRemove)
                {
                    item.RemovePropertyType(delete);
                }
            }

            if (tabsToBlank.Any())
            {
                foreach(var blank in tabsToBlank)
                {
                    // there might be a bug here, we need to do some cheking of if this is 
                    // possible with the public api

                    // blank.Key.PropertyTypes.Remove(blank.Value);
                }
            }

        }

        internal void DeserializeTabSortOrder(IContentTypeBase item, XElement node)
        {
            var tabNode = node.Element("Tabs");

            foreach(var tab in tabNode.Elements("Tab"))
            {
                var name = tab.Element("Caption").Value;
                var sortOrder = tab.Element("SortOrder");

                if (sortOrder != null)
                {
                    if (!string.IsNullOrEmpty(sortOrder.Value))
                    {
                        var itemTab = item.PropertyGroups.FirstOrDefault(x => x.Name == name);
                        if (itemTab != null)
                        {
                            itemTab.SortOrder = int.Parse(sortOrder.Value);
                        }
                    }
                }
            }
        }
        #endregion


        #region ContentTypeBase Serialize Helpers
        /// <summary>
        ///  So fiddling with the structure
        /// 
        ///  In an umbraco export the structure can come out in a random order
        ///  for consistancy, and better tracking of changes we export the list
        ///  in alias order, that way it should always be the same every time
        ///  regardless of the creation order of the doctypes.
        /// 
        ///  In earlier versions of umbraco, the structure export didn't always
        ///  work - so we redo the export, if it turns out this is fixed in 7.3
        ///  we shoud just do the xml sort like with properties, it will be faster
        /// </summary>
        internal XElement SerializeStructure(IContentTypeBase item, XElement node)
        {
            var structureNode = node.Element("Structure");
            structureNode.RemoveNodes();

            SortedList<string, ContentTypeSort> allowedTypes = new SortedList<string, ContentTypeSort>();
            foreach(var allowedType in item.AllowedContentTypes)
            {
                allowedTypes.Add(allowedType.Alias, allowedType);
            }

            foreach(var allowedType in allowedTypes)
            {
                structureNode.Add(new XElement(_itemType , allowedType.Value.Alias));
            }

            return node;            
        }

        /// <summary>
        ///  as with structure, we want to export properties in a consistant order
        ///  this just jiggles the order of the generic properties section, ordering by name
        /// 
        ///  at the moment we are making quite a big assumption that name is always there?
        /// </summary>
        internal XElement SerializeProperties(IContentTypeBase item, XElement node)
        {
            var sortedNode = new XElement(node);

            var sortedProperties = sortedNode.Element("GenericProperties");
            if (sortedProperties == null)
                return node;

            sortedProperties.RemoveAll();

            foreach(var property in node.Element("GenericProperties").Elements().OrderBy(x => x.Element("Name").Value))
            {
                sortedProperties.Add(property);
            }

            return sortedNode;
        }

     


        // special case for two pass, you can tell it to only first step
        public SyncAttempt<T> DeSerialize(XElement node, bool forceUpdate, bool onePass = false)
        {
            var attempt = base.DeSerialize(node);

            if (!onePass || !attempt.Success || attempt.Item == null)
                return attempt;

            return DesearlizeSecondPass(attempt.Item, node);
        }

        virtual public SyncAttempt<T> DesearlizeSecondPass(T item, XElement node)
        {
            return SyncAttempt<T>.Succeed(node.NameFromNode(), ChangeType.NoChange);
        }

        #endregion
    }
}
