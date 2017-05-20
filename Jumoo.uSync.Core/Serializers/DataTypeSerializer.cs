using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Models;

using Jumoo.uSync.Core.Interfaces;
using System.Xml.Linq;
using Umbraco.Core.Services;

using Jumoo.uSync.Core.Helpers;
using Jumoo.uSync.Core.Extensions;
using Umbraco.Core.Logging;
using System.Web;

namespace Jumoo.uSync.Core.Serializers
{
    public class DataTypeSerializer : DataTypeSyncBaseSerializer, ISyncChangeDetail
    {
        
        public DataTypeSerializer() :
            base(Constants.Packaging.DataTypeNodeName)
        { }

        public DataTypeSerializer(string type) :
            base (type)
        { }

        internal override SyncAttempt<IDataTypeDefinition> DeserializeCore(XElement node)
        {
            if (node.Name.LocalName == "EntityFolder")
                return DeserializeContainer(node);

            IDataTypeDefinition item = null;

            Guid key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key != Guid.Empty)
            {
                item = _dataTypeService.GetDataTypeDefinitionById(key);
            }

            return DeserializeItem(node, item);
        }

        private SyncAttempt<IDataTypeDefinition> DeserializeItem(XElement node, IDataTypeDefinition item)
        {
            // pre import

            Guid key = node.Attribute("Key").ValueOrDefault(Guid.Empty);

            var name = node.Attribute("Name").ValueOrDefault(string.Empty);
            var editorAlias = node.Attribute("Id").ValueOrDefault(string.Empty);
            var dbType = node.Attribute("DatabaseType").ValueOrDefault(string.Empty);
            var databaseType = !string.IsNullOrEmpty(dbType) ? dbType.EnumParse<DataTypeDatabaseType>(true) : DataTypeDatabaseType.Ntext;
            var folder = node.Attribute("Folder").ValueOrDefault(string.Empty);

            var folderId = -1;
            if (!string.IsNullOrEmpty(folder))
            {
                folderId = GetFolders(folder);
            }

            if (item == null && !string.IsNullOrEmpty(name))
            {
                // lookup by alias. 
                LogHelper.Debug<DataTypeSerializer>("Looking up datatype by name: {0}", () => name);
                item = _dataTypeService.GetDataTypeDefinitionByName(name);
            }

            if (item == null)
            {
                // create
                item = new DataTypeDefinition(editorAlias)
                {
                    Key = key,
                    Name = name,
                    DatabaseType = databaseType
                };
            }

            if (item != null)
            {
                if (item.Name != name)
                    item.Name = name;

                if (item.Key != key)
                    item.Key = key;

                if (item.PropertyEditorAlias != editorAlias)
                    item.PropertyEditorAlias = editorAlias;

                if (item.DatabaseType != databaseType)
                    item.DatabaseType = databaseType;

                if (folderId != -1 && item.ParentId != folderId)
                    item.ParentId = folderId;

                _dataTypeService.Save(item);

                DeserializeUpdatePreValues(item, node);
            }


            _dataTypeService.Save(item);

            UpdateDataTypeCache(item);

            return SyncAttempt<IDataTypeDefinition>.Succeed(item.Name, item, ChangeType.Import);

        }

        private int GetFolders(string folder)
        {
            var folders = folder.Split('/');
            var rootFolder = HttpUtility.UrlDecode(folders[0]);

            var rootId = -1;
            var root = _dataTypeService.GetContainers(rootFolder, 1).FirstOrDefault();
            if (root == null)
            {
                var attempt = _dataTypeService.CreateContainer(-1, rootFolder);
                if (attempt == false)
                {
                    LogHelper.Warn<DataTypeSerializer>("Cant' create folder: Doh!");
                    return -1;
                }
                rootId = attempt.Result.Entity.Id;
            }
            else
            {
                rootId = root.Id;
            }

            if (rootId != -1)
            {
                var current = _dataTypeService.GetContainer(rootId);
                for(int i = 1; i < folders.Length;i++)
                {
                    var name = HttpUtility.UrlDecode(folders[i]);
                    current = TryCreateContainer(name, current);
                }

                return current.Id;
            }

            return -1;
        }

        private EntityContainer TryCreateContainer(string name, EntityContainer parent)
        {
            LogHelper.Debug<ContentTypeSerializer>("TryCreate: {0} under {1}", () => name, () => parent.Name);

            var children = _entityService.GetChildren(parent.Id, UmbracoObjectTypes.DataTypeContainer).ToArray();

            if (children.Any(x => x.Name.InvariantEquals(name)))
            {
                var folderId = children.Single(x => x.Name.InvariantEquals(name)).Id;
                return _dataTypeService.GetContainer(folderId);
            }

            // else - create 
            var attempt = _dataTypeService.CreateContainer(parent.Id, name);
            if (attempt == true)
                return _dataTypeService.GetContainer(attempt.Result.Entity.Id);

            LogHelper.Warn<ContentTypeSerializer>("Can't create child folders {0} you doctypes might be flat", () => name);

            return null;
        }

        internal override SyncAttempt<IDataTypeDefinition> DesearlizeSecondPassCore(IDataTypeDefinition item, XElement node)
        {
            return DeserializeItem(node, item);
        }

        private XElement DeserializeGetMappedValues(XElement node, IDictionary<string, PreValue> preValues )
        {
            XElement nodeCopy = new XElement(node);
            var id = nodeCopy.Attribute("Id").ValueOrDefault(string.Empty);

            LogHelper.Debug<DataTypeSerializer>("Mapping Guids {0}", ()=> id);

            var mapper = LoadMapper(nodeCopy, id);

            var preValuesElements = nodeCopy.Element("PreValues");

            // value use to be an attrib - now it's a value
            foreach (var legacyElement in preValuesElements.Descendants().Where(x => x.Attribute("Value") != null))
            {
                legacyElement.Value = legacyElement.Attribute("Value").Value;
            }

            if (mapper != null && preValuesElements != null && preValuesElements.HasElements)
            {
                foreach (var preValueNode in preValuesElements.Descendants()
                                            .Where(x => x.Attribute("MapGuid") != null && x.Attribute("Alias") != null)
                                            .ToList())
                {
                    var alias = preValueNode.Attribute("Alias").Value;

                    if (preValues.ContainsKey(alias))
                    {
                        var value = mapper.MapToId(preValueNode, preValues[alias] );

                        if (!string.IsNullOrEmpty(value))
                        {
                            LogHelper.Debug<DataTypeSerializer>("Setting Mapped Value: {0}", () => value);
                            preValueNode.Value = value;
                            // preValueNode.Attribute("Value").Value = value;
                        }

                        preValueNode.Attribute("MapGuid").Remove();
                    }
                }
            }

            if (nodeCopy.Element("Nodes") != null)
                nodeCopy.Element("Nodes").Remove();

            return nodeCopy;
        }

        private void DeserializeUpdatePreValues(IDataTypeDefinition item, XElement node)
        {
            var itemPreValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(item.Id)
                                    .FormatAsDictionary();

            var mappedNode = DeserializeGetMappedValues(node, itemPreValues);

            var preValueRootNode = mappedNode.Element("PreValues");
            if (preValueRootNode != null)
            {
                List<string> preValsToRemove = new List<string>();

                foreach (var preValue in itemPreValues)
                {

                    var preValNode = preValueRootNode.Elements("PreValue")
                                        .Where(x => x.Attribute("Alias") != null && ((string)x.Attribute("Alias").Value == preValue.Key))
                                        .FirstOrDefault();

                    if (preValNode != null)
                    {
                        // set the value of preValue value to the value of the value attribute :)
                        if (preValue.Value.Value != preValNode.Value)
                        {
                            preValue.Value.Value = preValNode.Value;
                        }
                    }
                    else
                    {
                        preValsToRemove.Add(preValue.Key);
                    }


                }

                // remove things that we didn't find
                foreach (var key in preValsToRemove)
                {
                    itemPreValues.Remove(key);
                }

                // now add any new prevalues from the xml
                /*
                var valuesWithKeys = preValueRootNode.Elements("PreValue")
                                        .Where(x => (string.IsNullOrWhiteSpace((string)x.Attribute("Alias")) == false))
                                        .ToDictionary(key => (string)key.Attribute("Alias"), val => (string)val.Attribute("Value"));
                */

                foreach (var nodeValue in preValueRootNode.Elements("PreValue"))
                {
                    var alias = nodeValue.Attribute("Alias").ValueOrDefault(string.Empty);
                    var value = nodeValue.ValueOrDefault(string.Empty);

                    if (!string.IsNullOrEmpty(alias))
                    {
                        if (!itemPreValues.ContainsKey(alias))
                        {
                            itemPreValues.Add(alias, new PreValue(value));
                        }
                    }
                }

                _dataTypeService.SavePreValues(item, itemPreValues);
            }
        }

        internal override SyncAttempt<XElement> SerializeCore(IDataTypeDefinition item)
        {
            try {
                var node = new XElement(Constants.Packaging.DataTypeNodeName,
                    new XAttribute("Name", item.Name),
                    new XAttribute("Key", item.Key),
                    new XAttribute("Id", item.PropertyEditorAlias),
                    new XAttribute("DatabaseType", item.DatabaseType.ToString())                    
                    );


                if (item.Level != 1)
                {
                    var folders = _dataTypeService.GetContainers(item)
                        .OrderBy(x => x.Level)
                        .Select(x => HttpUtility.UrlEncode(x.Name));

                    if (folders.Any())
                        node.Add(new XAttribute("Folder", string.Join("/", folders.ToArray())));

                }

                node.Add(SerializePreValues(item, node));
                UpdateDataTypeCache(item);

                return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IDataTypeDefinition), ChangeType.Export); 
            }
            catch(Exception ex)
            {
                LogHelper.Warn<DataTypeSerializer>("Error Serializing {0}", () => ex.ToString());
                return SyncAttempt<XElement>.Fail(item.Name, typeof(IDataTypeDefinition), ChangeType.Export, "Failed to export", ex);
            }
        }


        private XElement SerializePreValues(IDataTypeDefinition item, XElement node)
        {
            var mapper = LoadMapper(node, item.PropertyEditorAlias);
            var mapId = 1; 

            // we clear them out, and write them ourselves.
            var nodePreValues = new XElement("PreValues");

            var itemPreValues = GetPreValues(item);
            foreach(var itemPreValuePair in itemPreValues)
            {
                var preValue = itemPreValuePair.Value;
                var preValueValue = preValue.Value;

                XElement preValueNode = new XElement("PreValue",
                    new XAttribute("Id", preValue.Id.ToString()));
                preValueNode.Add(new XCData(String.IsNullOrEmpty(preValueValue) ? "" : preValueValue));

                if (!itemPreValuePair.Key.StartsWith("zzzuSync"))
                    preValueNode.Add(new XAttribute("Alias", itemPreValuePair.Key));

                if (mapper != null)
                {
                    if (itemPreValuePair.Key.StartsWith("zzzuSync") || mapper.ValueAlias == itemPreValuePair.Key)
                    {
                        // Guid newGuid = Guid.NewGuid();
                        if (mapper.MapToGeneric(preValueValue, mapId))
                        {
                            preValueNode.Add(new XAttribute("MapGuid", mapId));
                            mapId++;
                        }
                    }
                }

                nodePreValues.Add(preValueNode);
            }
            return nodePreValues;
        }

        private Dictionary<string, PreValue> GetPreValues(IDataTypeDefinition dataType)
        {
            var preValuesCollection = ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dataType.Id);

            if (preValuesCollection.IsDictionaryBased)
            {
                return preValuesCollection
                        .PreValuesAsDictionary
                        .OrderBy(p => p.Value.SortOrder)
                        .ThenBy(p => p.Value.Id)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            else
            {
                // return an array, just make it a dictionary on the way out.
                return preValuesCollection
                        .PreValuesAsArray
                        .OrderBy(p => p.SortOrder)
                        .ThenBy(p => p.Id)
                        .ToDictionary(preValue => "zzzuSync" + preValue.Id.ToString(), preValue => preValue);
            }
        }


        public override bool IsUpdate(XElement node)
        {
            if (node.Name.LocalName == "EntityFolder")
                return IsContainerUpdated(node);

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var defNode = node.Attribute("Key");
            if (defNode == null)
                return true;

            Guid defGuid = Guid.Empty;
            if (!Guid.TryParse(defNode.Value, out defGuid))
                return true;

            // var item = _dataTypeService.GetDataTypeDefinitionById(defGuid);
            var item = GetDataTypeFromCache(defGuid);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));
        }

        /// <summary>
        ///   Datatype cache. we can use this to do the lookups, 
        ///   pre-warming the datatypes on import saves, a few seconds on import.
        /// </summary>
        private IDataTypeDefinition GetDataTypeFromCache(Guid guid)
        {
            if (_dtdCache != null)
            {
                var i = _dtdCache.FirstOrDefault(x => x.Key == guid);
                if (i != null)
                    return i;
            }

            return _dataTypeService.GetDataTypeDefinitionById(guid);
        }

        private void UpdateDataTypeCache(IDataTypeDefinition item)
        {
            if (_dtdCache != null)
            {
                if (_dtdCache.Any(x => x.Key == item.Key))
                {
                    var i = _dtdCache.FindIndex(x => x.Key == item.Key);

                    if (_dtdCache[i].UpdateDate != item.UpdateDate)
                    {
                        _dtdCache[i] = item;
                    }
                }
                else
                {
                    _dtdCache.Add(item);
                }
            }
        }        

        private bool IsContainerUpdated(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var key = node.Attribute("Key").ValueOrDefault(Guid.Empty);
            if (key == Guid.Empty)
                return true;

            var item = _dataTypeService.GetContainer(key);
            if (item == null)
                return true;

            var attempt = SerializeContainer(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            return (!nodeHash.Equals(itemHash));
        }


        private uSyncValueMapper LoadMapper(XElement node, string dataTypeId)
        {
            var settings = uSyncCoreContext.Instance.Configuration.Settings.Mappings
                                .Where(x => x.DataTypeId == dataTypeId.ToString())
                                .FirstOrDefault();


            if (settings != null)
            {
                LogHelper.Debug<DataTypeSerializer>("Loading Mapper for : {0}", () => settings.DataTypeId);
                return new uSyncValueMapper(node, settings);
            }

            return null;
        }

        public IEnumerable<uSyncChange> GetChanges(XElement node)
        {
            if (node.Name.LocalName == "EntityFolder")
                return GetContainerChanges(node);

            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return null;

            var defNode = node.Attribute("Key");
            if (defNode == null)
                return null;

            Guid defGuid = Guid.Empty;
            if (!Guid.TryParse(defNode.Value, out defGuid))
                return null;

            var item = _dataTypeService.GetDataTypeDefinitionById(defGuid);
            if (item == null)
                return uSyncChangeTracker.NewItem(defNode.Value);

            var attempt = Serialize(item);
            if (attempt.Success)
            {
                return uSyncChangeTracker.GetChanges(node, attempt.Item, "");
            }
            else
            {
                return uSyncChangeTracker.ChangeError(defNode.Value);
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

            var item = _dataTypeService.GetContainer(key);
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
    }
}
