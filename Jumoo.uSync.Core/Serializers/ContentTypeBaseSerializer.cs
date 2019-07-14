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
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;

namespace Jumoo.uSync.Core.Serializers
{
    abstract public class ContentTypeBaseSerializer<T> : SyncBaseSerializer<T>, ISyncContainerSerializerTwoPass<T>
    {
        internal IContentTypeService _contentTypeService;
        internal IDataTypeService _dataTypeService;
        internal IMemberTypeService _memberTypeService;
        internal IEntityService _entityService;

        // all content/media cached lists - they are used when 
        // creating new properties to make sure we don't YSOD the site.
        // 
        private List<IContentType> _allContentTypes;
        private List<IMediaType> _allMediaTypes;

        private UmbracoObjectTypes baseObjectType;

        /*
        [Obsolete("You should pass Object Type to base class so lookups work")]
        public ContentTypeBaseSerializer(string itemType) : 
            this(itemType, UmbracoObjectTypes.DocumentType)
        {
        }
        */

        public ContentTypeBaseSerializer(string itemType, UmbracoObjectTypes objectType): base(itemType)
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            _memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            _entityService = ApplicationContext.Current.Services.EntityService;

            baseObjectType = objectType;
        }

        #region ContentTypeBase Deserialize Helpers

        /// <summary>
        ///  does the basic deserialization, bascially the stuff in info
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        internal void DeserializeBase(IContentTypeBase item, XElement info)
        {
            var alias = info.Element("Alias").Value;
            if (item.Alias != alias)
                item.Alias = alias;

            var name = info.Element("Name").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(name) )
            {
                item.Name = name;
            }

            var icon = info.Element("Icon").ValueOrDefault("");
            if (item.Icon != icon)
                item.Icon = icon;

            var thumb = info.Element("Thumbnail").ValueOrDefault("");
            if (item.Thumbnail != thumb)
                item.Thumbnail = thumb;

            var desc = info.Element("Description").ValueOrDefault("");
            if (item.Description != desc)
                item.Description = desc;

            var allow = info.Element("AllowAtRoot").ValueOrDefault(false);
            if (item.AllowedAsRoot != allow)
                item.AllowedAsRoot = allow;

            var masterNode = info.Element("Master");
            if (masterNode != null)
            {
                var masterId = 0;

                var masterKey = masterNode.Attribute("Key").ValueOrDefault(Guid.Empty);
                if (masterKey != Guid.Empty)
                {
                    var attempt = ApplicationContext.Current.Services.EntityService.GetIdForKey(masterKey, baseObjectType);
                    if (attempt.Success)
                        masterId = attempt.Result;
                }

                if (masterId == 0)
                {
                    // old school alias lookup
                    var master = default(IContentTypeBase);

                    LogHelper.Debug<Events>("Looking up Content Master by Alias");
                    var masterAlias = masterNode.Value;
                    master = LookupByAlias(masterAlias);
                    if (master != null)
                        masterId = master.Id;
                }

                if (masterId > 0)
                {
                    item.SetLazyParentId(new Lazy<int>(() => masterId));                        
                }
            }
        }

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
                var key = contentBaseNode.Attribute("Key").ValueOrDefault(Guid.Empty);

                IContentTypeBase contentBaseItem = default(IContentTypeBase);
                IUmbracoEntity baseItem = default(IUmbracoEntity);

                var _entityService = ApplicationContext.Current.Services.EntityService;

                if (key != Guid.Empty)
                {
                    LogHelper.Debug<uSync.Core.Events>("Using key to find structure element");
                    contentBaseItem = LookupByKey(key);
                }

                if (baseItem == null && !string.IsNullOrEmpty(alias))
                {
                    LogHelper.Debug<uSync.Core.Events>("Fallback Alias lookup");
                    contentBaseItem = LookupByAlias(alias);
                }

                if (contentBaseItem != default(IContentTypeBase))
                {
                    allowedTypes.Add(new ContentTypeSort(
                        new Lazy<int>(() => contentBaseItem.Id), sortOrder, contentBaseItem.Name));
                    sortOrder++;
                }
            }

            item.AllowedContentTypes = allowedTypes;
        }

        internal string DeserializeProperties(IContentTypeBase item, XElement node)
        {
            string message = ""; 
            // clear our type cache, we use it to check for clashes in compistions.
            _allMediaTypes = null;
            _allContentTypes = null;

            List<string> propertiesToRemove = new List<string>();
            Dictionary<string, string> propertiesToMove = new Dictionary<string, string>();
            Dictionary<PropertyGroup, string> tabsToBlank = new Dictionary<PropertyGroup, string>();

            var genericPropertyNode = node.Element("GenericProperties");
            if (genericPropertyNode != null)
            {
                // add or update properties
                foreach (var propertyNode in genericPropertyNode.Elements("GenericProperty"))
                {
                    bool newProperty = false; 

                    var property = default(PropertyType);
                    var propKey = propertyNode.Element("Key").ValueOrDefault(Guid.Empty);
                    if (propKey != Guid.Empty)
                    {
                        LogHelper.Debug<ContentTypeSerializer>("Looking up Property Key: {0}", () => propKey);
                        property = item.PropertyTypes.SingleOrDefault(x => x.Key == propKey);



                    }

                    var alias = propertyNode.Element("Alias").ValueOrDefault(string.Empty);

                    if (property == null)
                    {
                        LogHelper.Debug<ContentTypeSerializer>("Looking up Property Alias: {0}", () => alias);
                        // look up via alias?
                        property = item.PropertyTypes.SingleOrDefault(x => x.Alias == alias);
                    }

                    // we need to get element stuff now before we can create or update

                    var defGuid = propertyNode.Element("Definition").ValueOrDefault(Guid.Empty);
                    var dataTypeDefinition = _dataTypeService.GetDataTypeDefinitionById(defGuid);

                    if (dataTypeDefinition == null)
                    {
                        var propEditorAlias = propertyNode.Element("Type").ValueOrDefault(string.Empty);
                        if (!string.IsNullOrEmpty(propEditorAlias))
                        {
                            dataTypeDefinition = _dataTypeService
                                            .GetDataTypeDefinitionByPropertyEditorAlias(propEditorAlias)
                                            .FirstOrDefault();
                        }
                    }

                    if (dataTypeDefinition == null)
                    { 
                        LogHelper.Warn<Events>("Failed to get Definition for property type");
                        continue;
                    }

                    if (property == null)
                    {
                        // for doctypes we need to check the compositions, we cant create a property here
                        // that exists further down the composition tree. 
                        
                        if (CanCreateProperty(item, alias))
                        {
                            LogHelper.Debug<Events>("Creating new Property: {0} {1}", () => item.Alias, () => alias);
                            property = new PropertyType(dataTypeDefinition, alias);
                            newProperty = true;
                        }
                        else
                        {
                            LogHelper.Warn<Events>("Can't create {0} : already used by a composed doctype - a second import might fix this", ()=> alias);
                            message = string.Format("Property: {0} was not created because of clash (try running import again)", alias);
                        }

                    }

                    if (property != null)
                    {
                        LogHelper.Debug<Events>("Updating Property :{0} {1}", ()=> item.Alias, ()=> alias);

                        var key = propertyNode.Element("Key").ValueOrDefault(Guid.Empty);
                        if (key != Guid.Empty)
                        {
                            LogHelper.Debug<Events>("Setting Key :{0}", () => key);
                            property.Key = key;
                        }

                        LogHelper.Debug<Events>("Item Key    :{0}", () => property.Key);

                        // update settings.
                        property.Name = propertyNode.Element("Name").ValueOrDefault("unnamed" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

                        if (property.Alias != alias)
                            property.Alias = alias; 

                        if (propertyNode.Element("Description") != null)
                            property.Description = propertyNode.Element("Description").Value;

                        if (propertyNode.Element("Mandatory") != null)
                            property.Mandatory = propertyNode.Element("Mandatory").Value.ToLowerInvariant().Equals("true");

                        if (propertyNode.Element("Validation") != null)
                            property.ValidationRegExp = propertyNode.Element("Validation").Value;

                        if (propertyNode.Element("SortOrder") != null)
                            property.SortOrder = int.Parse(propertyNode.Element("SortOrder").Value);

                        if (propertyNode.Element("Type") != null)
                        {
                            LogHelper.Debug<Events>("Setting Property Type : {0}", () => propertyNode.Element("Type").Value);
                            property.PropertyEditorAlias = propertyNode.Element("Type").Value;
                        }

                        if (property.DataTypeDefinitionId != dataTypeDefinition.Id)
                        {
                            property.DataTypeDefinitionId = dataTypeDefinition.Id;
                        }

                        var tabName = propertyNode.Element("Tab").ValueOrDefault(string.Empty);

                        if (_itemType == "MemberType")
                        {
                            ((IMemberType)item).SetMemberCanEditProperty(alias,
                                propertyNode.Element("CanEdit").ValueOrDefault(false));

                            ((IMemberType)item).SetMemberCanViewProperty(alias,
                                propertyNode.Element("CanView").ValueOrDefault(false));
                        }

                        if (!newProperty)
                        {
                            if (!string.IsNullOrEmpty(tabName))
                            {
                                var propGroup = item.PropertyGroups.FirstOrDefault(x => x.Name == tabName);
                                if (propGroup != null)
                                {
                                    if (!propGroup.PropertyTypes.Any(x => x.Alias == property.Alias))
                                    {
                                        // this tab currently doesn't contain this property, to we have to
                                        // move it (later)
                                        if (!propertiesToMove.ContainsKey(property.Alias))
                                            propertiesToMove.Add(property.Alias, tabName);
                                    }
                                }
                            }
                            else
                            {
                                // this property isn't in a tab (now!)
                                if (!newProperty)
                                {
                                    var existingTab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));
                                    if (existingTab != null)
                                    {
                                        // this item is now not in a tab (when it was)
                                        // so we have to remove it from tabs (later)
                                        if (!tabsToBlank.ContainsKey(existingTab))
                                            tabsToBlank.Add(existingTab, property.Alias);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // new propert needs to be added to content type..
                            if (string.IsNullOrEmpty(tabName))
                            {
                                item.AddPropertyType(property);
                            }
                            else
                            {
                                item.AddPropertyType(property, tabName);
                            }

                            // setting the key before here doesn't seem to work for new types.
                            if (key != Guid.Empty)
                                property.Key = key; 
                        }
                    }
                } // end foreach property
            } // end generic properties 


            // look at what properties we need to remove. 
            var propertyNodes = node.Elements("GenericProperties").Elements("GenericProperty");
            foreach(var property in item.PropertyTypes)
            {
                XElement propertyNode = propertyNodes
                                            .FirstOrDefault(x=> x.Element("key") != null && x.Element("Key").Value == property.Key.ToString());

                if (propertyNode == null)
                {
                    LogHelper.Debug<uSync.Core.Events>("Remove Check: Looking up property type by alias {0} to stop accedental removal", ()=> property.Alias);
                    propertyNode = propertyNodes
                        .SingleOrDefault(x => x.Element("Alias").Value == property.Alias);
                }

                if (propertyNode == null)
                {
                    LogHelper.Debug<uSync.Core.Events>("Removing Property: (no match on system) {0}", () => property.Alias);
                    propertiesToRemove.Add(property.Alias);
                }
            }


            // now we have gone through all the properties, we can do the moves and removes from the groups
            if (propertiesToMove.Any())
            {
                foreach (var move in propertiesToMove)
                {
                    LogHelper.Debug<Events>("Moving Property: {0} {1}", () => move.Key, () => move.Value);
                    item.MovePropertyType(move.Key, move.Value);
                }
            }

            if (propertiesToRemove.Any())
            {
                // removing properties can cause timeouts on installs with lots of content...
                foreach(var delete in propertiesToRemove)
                {
                    LogHelper.Debug<Events>("Removing Property: {0}", () => delete);
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

            return message; 

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
                        else
                        {
                            LogHelper.Debug<Events>("Adding new Tab? {0}", ()=> name);
                            // at this point we might have a missing tab. 
                            if (item.AddPropertyGroup(name))
                            {
                                itemTab = item.PropertyGroups.FirstOrDefault(x => x.Name == name);
                                if (itemTab != null)
                                    itemTab.SortOrder = int.Parse(sortOrder.Value);
                            }
                        }
                    }
                }
            }

        }

        internal void CleanUpTabs(IContentTypeBase item, XElement node)
        {
            LogHelper.Debug<Events>("Cleaning up Tabs");
            var tabNode = node.Element("Tabs");

            if (tabNode == null || !tabNode.HasElements)
                return;

            // remove tabs 
            List<string> tabsToRemove = new List<string>();
            foreach (var tab in item.PropertyGroups)
            {
                if (tabNode.Elements("Tab").FirstOrDefault(x => x.Element("Caption").Value == tab.Name) == null)
                {
                    // no tab of this name in the import... remove it.
                    LogHelper.Debug<ContentTypeSerializer>("Removing Tab {0}", () => tab.Name);
                    tabsToRemove.Add(tab.Name);
                }
            }

            foreach (var name in tabsToRemove)
            {
                item.PropertyGroups.Remove(name);
            }

        }

        #endregion

        #region ContentTypeBase Serialize Helpers

        public virtual SyncAttempt<XElement> SerializeContainer(EntityContainer item)
        {
            return SyncAttempt<XElement>.Succeed(item.Name, ChangeType.NoChange);
        }

        internal XElement SerializeInfo(IContentTypeBase item)
        {
            var info = new XElement("Info",
                            new XElement("Key", item.Key),
                            new XElement("Name", item.Name),
                            new XElement("Alias", item.Alias),
                            new XElement("Icon", item.Icon),
                            new XElement("Thumbnail", item.Thumbnail),
                            new XElement("Description", string.IsNullOrEmpty(item.Description) ? "" : item.Description ),
                            new XElement("AllowAtRoot", item.AllowedAsRoot.ToString()),
                            new XElement("IsListView", item.IsContainer.ToString()));

            return info;
        }

        internal XElement SerializeTabs(IContentTypeBase item)
        {
            var tabs = new XElement("Tabs");
            foreach (var tab in item.PropertyGroups.OrderBy(x => x.SortOrder))
            {
                tabs.Add(new XElement("Tab",
                        // new XElement("Key", tab.Key),
                        new XElement("Caption", tab.Name),
                        new XElement("SortOrder", tab.SortOrder)));
            }

            return tabs;
        }

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
        internal XElement SerializeStructure(IContentTypeBase item)
        {
            var structureNode = new XElement("Structure");

            SortedList<string, Guid> allowedAliases = new SortedList<string, Guid>();
            foreach(var allowedType in item.AllowedContentTypes.OrderBy(x => x.Alias))
            {
                IContentTypeBase allowed = LookupById(allowedType.Id.Value);
                if (allowed != null)
                    allowedAliases.Add(allowed.Alias, allowed.Key);
            }


            foreach (var alias in allowedAliases)
            {
                structureNode.Add(new XElement(_itemType, alias.Key,
                    new XAttribute("Key", alias.Value.ToString()))
                    );
            }
            return structureNode;            
        }

        // private Dictionary<int, IDataTypeDefinition> _dtdCache; 

        /// <summary>
        ///  as with structure, we want to export properties in a consistant order
        ///  this just jiggles the order of the generic properties section, ordering by name
        /// 
        ///  at the moment we are making quite a big assumption that name is always there?
        /// </summary>
        internal XElement SerializeProperties(IContentTypeBase item)
        {
            var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;

            var properties = new XElement("GenericProperties");

            foreach(var property in item.PropertyTypes.OrderBy(x => x.Alias))
            {
                var propNode = new XElement("GenericProperty");

                propNode.Add(new XElement("Key", property.Key));
                propNode.Add(new XElement("Name", property.Name));
                propNode.Add(new XElement("Alias", property.Alias));
                /*
                if (_dtdCache == null)
                    _dtdCache = new Dictionary<int, IDataTypeDefinition>();

                IDataTypeDefinition def = default(IDataTypeDefinition);
                if (_dtdCache.ContainsKey(property.DataTypeDefinitionId))
                    def = _dtdCache[property.DataTypeDefinitionId];
                else 
                */
                var def = _dataTypeService.GetDataTypeDefinitionById(property.DataTypeDefinitionId);

                if (def != null)
                {
                    propNode.Add(new XElement("Definition", def.Key));
                    propNode.Add(new XElement("Type", def.PropertyEditorAlias));
                }
                else
                {
                    propNode.Add(new XElement("Type", property.PropertyEditorAlias));
                }


                propNode.Add(new XElement("Mandatory", property.Mandatory));

                propNode.Add(new XElement("Validation", property.ValidationRegExp != null ? property.ValidationRegExp : "" ));

                var description = String.IsNullOrEmpty(property.Description) ? "" : property.Description;
                propNode.Add(new XElement("Description", new XCData(description)));

                propNode.Add(new XElement("SortOrder", property.SortOrder));

                var tab = item.PropertyGroups.FirstOrDefault(x => x.PropertyTypes.Contains(property));
                propNode.Add(new XElement("Tab", tab != null ? tab.Name : ""));

                if (_itemType == "MemberType")
                {
                    var canEdit = ((IMemberType)item).MemberCanEditProperty(property.Alias);
                    var canView = ((IMemberType)item).MemberCanViewProperty(property.Alias);

                    propNode.Add(new XElement("CanEdit", canEdit));
                    propNode.Add(new XElement("CanView", canView));
                }

                properties.Add(propNode);
            }

            return properties;
        }

   
        // special case for two pass, you can tell it to only first step
        public SyncAttempt<T> Deserialize(XElement node, bool forceUpdate, bool onePass = false)
        {
            var attempt = base.DeSerialize(node);

            if (!onePass || !attempt.Success || attempt.Item == null)
                return attempt;

            return DesearlizeSecondPass(attempt.Item, node);
        }

        virtual public SyncAttempt<T> DeserializeContainer(XElement node)
        {
            return SyncAttempt<T>.Succeed(node.NameFromNode(), ChangeType.NoChange);
        }

        virtual public SyncAttempt<T> DesearlizeSecondPass(T item, XElement node)
        {
            return SyncAttempt<T>.Succeed(node.NameFromNode(), ChangeType.NoChange);
        }

        #endregion

        #region Lookup Helpers
        /// <summary>
        ///  these shoud be doable with the entity service, but for now, we 
        ///  are grouping these making it eaiser should we add another 
        /// contentTypeBased type to it. 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private IContentTypeBase LookupByKey(Guid key)
        {
            IContentTypeBase item = default(IContentTypeBase);
            switch (_itemType)
            {
                case Constants.Packaging.DocumentTypeNodeName:
                    item = _contentTypeService.GetContentType(key);
                    break;
                case "MediaType":
                    item = _contentTypeService.GetMediaType(key);
                    break;
                case "MemberType":
                    item = _memberTypeService.Get(key);
                    break;
            }

            return item;
        }

        // private Dictionary<int, IContentTypeBase> _lookupCache; 

        private IContentTypeBase LookupById(int id)
        {
            IContentTypeBase item = default(IContentTypeBase);
            /*
            if (_lookupCache == null)
                _lookupCache = new Dictionary<int, IContentTypeBase>();

            if (_lookupCache.ContainsKey(id))
                return _lookupCache[id];
            */
            switch (_itemType)
            {
                case Constants.Packaging.DocumentTypeNodeName:
                    item = _contentTypeService.GetContentType(id);
                    break;
                case "MediaType":
                    item = _contentTypeService.GetMediaType(id);
                    break;
                case "MemberType":
                    item = _memberTypeService.Get(id);
                    break;
            }

            return item;
        }
        private IContentTypeBase LookupByAlias(string alias)
        {
            IContentTypeBase item = default(IContentTypeBase);
            switch (_itemType)
            {
                case Constants.Packaging.DocumentTypeNodeName:
                    item = _contentTypeService.GetContentType(alias);
                    break;
                case "MediaType":
                    item = _contentTypeService.GetMediaType(alias);
                    break;
                case "MemberType":
                    item = _memberTypeService.Get(alias);
                    break;
            }

            return item;
        }


        /// <summary>
        ///  does a check to see that no doctype/media type below 
        ///  what we are looking at has the property we want to 
        ///  create, if it doesn, we warn and carry on. a double
        ///  import fixes this 
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        private bool CanCreateProperty(IContentTypeBase item, string alias)
        {
            bool canCreate = true;

            switch (_itemType)
            {
                case Constants.Packaging.DocumentTypeNodeName:
                    if (_allContentTypes == null)
                        _allContentTypes = _contentTypeService.GetAllContentTypes().ToList();

                    var allProperties = _allContentTypes.Where(x => x.ContentTypeComposition.Any(y => y.Id == item.Id)).Select(x => x.PropertyTypes);
                    if (allProperties.Any(x => x.Any(y => y.Alias == alias)))
                        canCreate = false;
                    break;
                case "MediaType":
                    if (_allMediaTypes == null)
                        _allMediaTypes = _contentTypeService.GetAllMediaTypes().ToList();

                    var allMediaProperties = _allMediaTypes.Where(x => x.ContentTypeComposition.Any(y => y.Id == item.Id)).Select(x => x.PropertyTypes);
                    if (allMediaProperties.Any(x => x.Any(y => y.Alias == alias)))
                        canCreate = false;
                    break;
            }
            return canCreate;

        }
        #endregion

    }
}
