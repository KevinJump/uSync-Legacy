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

namespace Jumoo.uSync.Core.Serializers
{
    public class DataTypeSerializer : DataTypeSyncBaseSerializer
    {
        private IDataTypeService _dataTypeService;

        public DataTypeSerializer(string type) : base(type)
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        internal override SyncAttempt<IDataTypeDefinition> DeserializeCore(XElement node)
        {
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
            var mappedNode = DeserializeGetMappedValues(node);
            Guid key = node.Attribute("Key").ValueOrDefault(Guid.Empty);

            var name = node.Attribute("Name").ValueOrDefault(string.Empty);
            var editorAlias = node.Attribute("Id").ValueOrDefault(string.Empty);
            var dbType = node.Attribute("DatabaseType").ValueOrDefault(string.Empty);
            var databaseType = !string.IsNullOrEmpty(dbType) ? dbType.EnumParse<DataTypeDatabaseType>(true) : DataTypeDatabaseType.Ntext;

            if (item == null && !string.IsNullOrEmpty(name))
            {
                // lookup by alias. 
                LogHelper.Debug<DataTypeSerializer>("Looking up datatype by name: {0}", () => name);
                item = _dataTypeService.GetDataTypeDefinitionByName(name);
            }

            if (item == null)
            {
                // create
                item = new DataTypeDefinition(editorAlias) { Key = key, Name = name, DatabaseType = databaseType };
            }

            if (item.Name != name)
            {
                item.Name = name;
            }

            if (item.Key != key)
            {
                item.Key = key;
            }

            if (item.PropertyEditorAlias != editorAlias)
            {
                item.PropertyEditorAlias = editorAlias;
            }

            if (item.DatabaseType != databaseType)
            {
                item.DatabaseType = databaseType;
            }

            _dataTypeService.Save(item);

            DeserializeUpdatePreValues(item, mappedNode);

            _dataTypeService.Save(item);
            return SyncAttempt<IDataTypeDefinition>.Succeed(item.Name, item, ChangeType.Import);
        }

        internal override SyncAttempt<IDataTypeDefinition> DesearlizeSecondPassCore(IDataTypeDefinition item, XElement node)
        {
            return DeserializeItem(node, item);
        }

        private XElement DeserializeGetMappedValues(XElement node)
        {
            XElement nodeCopy = new XElement(node);
            var id = node.Attribute("Id").ValueOrDefault(string.Empty);

            var mapper = LoadMapper(nodeCopy, id);

            var preValues = nodeCopy.Element("PreValues");

            if (mapper != null && preValues != null && preValues.HasElements)
            {
                foreach (var preValue in preValues.Descendants()
                                            .Where(x => x.Attribute("MapGuid") != null)
                                            .ToList())
                {

                    var value = mapper.MapToId(preValue);

                    if (!string.IsNullOrEmpty(value))
                    {
                        preValue.Attribute("Value").Value = value;
                        preValue.Attribute("MapGuid").Remove();
                    }
                }
            }

            if (nodeCopy.Element("Nodes") != null)
                nodeCopy.Element("Nodes").Remove();

            return nodeCopy;
        }

        private void DeserializeUpdatePreValues(IDataTypeDefinition item, XElement node)
        {
            LogHelper.Debug<DataTypeSerializer>("Deserializing DataType PreValues: {0}", () => item.Name);

            var preValueRootNode = node.Element("PreValues");
            if (preValueRootNode != null)
            {
                var itemPreValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(item.Id)
                                        .FormatAsDictionary();

                List<string> preValsToRemove = new List<string>();

                foreach (var preValue in itemPreValues)
                {

                    var preValNode = preValueRootNode.Elements("PreValue")
                                        .Where(x => x.Attribute("Alias") != null && ((string)x.Attribute("Alias").Value == preValue.Key))
                                        .FirstOrDefault();

                    if (preValNode != null)
                    {
                        // if the xml still has MapGuid on it, then the mapping didn't work
                        // so we just don't set this value, its a value that the site does have
                        // but its possible the content or whatever is set diffrently on this install
                        //                        
                        if (preValNode.Attribute("MapGuid") == null)
                        {
                            // set the value of preValue value to the value of the value attribute :)
                            preValue.Value.Value = preValNode.Attribute("Value").Value;
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
                    var value = nodeValue.Attribute("Value").ValueOrDefault(string.Empty);

                    if (!string.IsNullOrEmpty(alias))
                    {
                        if (!itemPreValues.ContainsKey(alias))
                        {
                            LogHelper.Debug<DataTypeSerializer>("Adding PreValue {0} for {1}", () => alias, () => item.Name);
                            itemPreValues.Add(alias, new PreValue(value));
                        }
                    }
                }

                _dataTypeService.SavePreValues(item, itemPreValues);

                /*
                var valuesSansKeys = preValueRootNode.Elements("PreValue")
                                        .Where(x => ((string)x.Attribute("Alias")).IsNullOrWhiteSpace() == false)
                                        .Select(x => x.Attribute("Value").Value);

                /// this is marked as obsolete? but don't some prevalues still have no keys?
                if (valuesSansKeys.Any())
                {
                    _dataTypeService.SavePreValues(item.Id, valuesSansKeys);
                }
                */
            }
        }

        internal override SyncAttempt<XElement> SerializeCore(IDataTypeDefinition item)
        {
            try
            {
                var node = new XElement(Constants.Packaging.DataTypeNodeName,
                    new XAttribute("Name", item.Name),
                    new XAttribute("Key", item.Key),
                    new XAttribute("Id", item.PropertyEditorAlias),
                    new XAttribute("DatabaseType", item.DatabaseType.ToString())
                    );

                node.Add(SerializePreValues(item, node));

                return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(IDataTypeDefinition), ChangeType.Export);
            }
            catch (Exception ex)
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
            foreach (var itemPreValuePair in itemPreValues)
            {
                var preValue = itemPreValuePair.Value;
                var preValueValue = preValue.Value;

                XElement preValueNode = new XElement("PreValue",
                    new XAttribute("Id", preValue.Id.ToString()),
                    new XAttribute("Value", String.IsNullOrEmpty(preValueValue) ? "" : preValueValue));

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
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var defNode = node.Attribute("Key");
            if (defNode == null)
                return true;

            Guid defGuid = Guid.Empty;
            if (!Guid.TryParse(defNode.Value, out defGuid))
                return true;

            var item = _dataTypeService.GetDataTypeDefinitionById(defGuid);
            if (item == null)
                return true;

            var attempt = Serialize(item);
            if (!attempt.Success)
                return true;

            var itemHash = attempt.Item.GetSyncHash();

            LogHelper.Debug<DataTypeSerializer>(">> IsUpdated: {0} : {1}", () => !nodeHash.Equals(itemHash), () => item.Name);

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
    }
}
