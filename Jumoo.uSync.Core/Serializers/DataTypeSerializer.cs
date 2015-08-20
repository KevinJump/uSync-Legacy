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
    public class DataTypeSerializer : SyncBaseSerializer<IDataTypeDefinition>
    {
        private IPackagingService _packagingService;
        private IDataTypeService _dataTypeService;

        public DataTypeSerializer(string type) : base (type)
        {
            _packagingService = ApplicationContext.Current.Services.PackagingService;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        internal override SyncAttempt<IDataTypeDefinition> DeserializeCore(XElement node)
        {
            LogHelper.Debug<DataTypeSerializer>("<<< DeserializeCore");
            // pre import
            var mappedNode = DeserializeGetMappedValues(node);

            var item = _packagingService.ImportDataTypeDefinitions(node).FirstOrDefault();

            if (item == null)
            {
                // Import return null, when the datatype already exists, so get it if we can
                var dataTypeDefinitionId = new Guid(node.Attribute("Definition").Value);
                item = _dataTypeService.GetDataTypeDefinitionById(dataTypeDefinitionId);

                if (item == null)
                    return SyncAttempt<IDataTypeDefinition>.Fail(node.NameFromNode(), ChangeType.Import, "package service import failed");
            }

            LogHelper.Debug<DataTypeSerializer>("<<< DeserializeCore: Post Import: {0}", ()=> item.Name);


            DeserializeUpdate(item, node);
            DeserializeUpdatePreValues(item, node);

            _dataTypeService.Save(item);
            return SyncAttempt<IDataTypeDefinition>.Succeed(item.Name, item, ChangeType.Import);

        }

        private XElement DeserializeGetMappedValues(XElement node)
        {
            LogHelper.Debug<DataTypeSerializer>("<<< Deserialize: GetMappedValues");

            XElement nodeCopy = new XElement(node);
            var id = node.Attribute("Id").Value;

            var mapper = LoadMapper(nodeCopy, id);

            var preValues = nodeCopy.Element("PreValues");

            if (mapper != null && preValues != null && preValues.HasElements)
            {
                foreach(var preValue in preValues.Descendants()
                                            .Where(x => x.Attribute("MapGuid") != null)
                                            .ToList())
                {
                    var value = mapper.MapToId(preValue);

                    if (!string.IsNullOrEmpty(value))
                    {
                        preValues.Attribute("Value").Value = value;
                    }

                    preValue.Attribute("MapGuid").Remove();
                }
            }

            if (nodeCopy.Element("Nodes") != null)
                nodeCopy.Element("Nodes").Remove();

            return node;
        }

        private void DeserializeUpdate(IDataTypeDefinition item, XElement node)
        {
            LogHelper.Debug<DataTypeSerializer>("<<< Deserialize: Update");

            if (node.Attribute("Id") != null)
            {
                var targetType = node.Attribute("Id").Value;
                if (!targetType.Equals(item.PropertyEditorAlias, StringComparison.InvariantCultureIgnoreCase))
                {
                    // try to change the type (this might need a db update, which i don't think we can do?
                    item.PropertyEditorAlias = targetType;
                }
            }
        }

        private void DeserializeUpdatePreValues(IDataTypeDefinition item, XElement node)
        {
            LogHelper.Debug<DataTypeSerializer>("<<< Deserialize: UpdatePreValues");

            var preValueRootNode = node.Element("PreValues");
            if (preValueRootNode != null)
            {
                var itemPreValues = _dataTypeService.GetPreValuesCollectionByDataTypeId(item.Id)
                                        .FormatAsDictionary();

                List<string> preValsToRemove = new List<string>();

                foreach(var preValue in itemPreValues)
                {
                    var preValNode = preValueRootNode.Elements("PreValue")
                                        .Where(x => ((string)x.Attribute("Alias").Value == preValue.Key))
                                        .FirstOrDefault();

                    if (preValNode != null)
                    {
                        // set the value of preValue value to the value of the value attribute :)
                        preValue.Value.Value = preValNode.Attribute("Value").Value;
                    }
                    else
                    {
                        preValsToRemove.Add(preValue.Key);
                    }
                }

                // remove things that we didn't find
                foreach(var key in preValsToRemove)
                {
                    itemPreValues.Remove(key);
                }

                // now add any new prevalues from the xml
                var valuesWithKeys = preValueRootNode.Elements("PreValue")
                                        .Where(x => ((string)x.Attribute("Alias")).IsNullOrWhiteSpace() == false)
                                        .ToDictionary(key => (string)key.Attribute("Alias"), val => (string)val.Attribute("Value"));

                foreach(var nodeValue in valuesWithKeys)
                {

                    if (!itemPreValues.ContainsKey(nodeValue.Key))
                    {
                        itemPreValues.Add(nodeValue.Key, new PreValue(nodeValue.Value));
                    }
                }

                _dataTypeService.SavePreValues(item, itemPreValues);


                var valuesSansKeys = preValueRootNode.Elements("PreValue")
                                        .Where(x => ((string)x.Attribute("Alias")).IsNullOrWhiteSpace() == false)
                                        .Select(x => x.Attribute("Value").Value);

                /// this is marked as obsolete? but don't some prevalues still have no keys?
                /*
                if (valuesSansKeys.Any())
                {
                    _dataTypeService.SavePreValues(item.Id, valuesSansKeys);
                }
                */
            }
        }

        internal override SyncAttempt<XElement> SerializeCore(IDataTypeDefinition item)
        {
            LogHelper.Debug<DataTypeSerializer>(">>> SerializeCore");
            try {
                var node = _packagingService.Export(item);
                if (node == null)
                    return SyncAttempt<XElement>.Fail(item.Name, ChangeType.Export, "Package service export failed");

                return SerializeUpdatePreValues(item, node);
            }
            catch(Exception ex)
            {
                return SyncAttempt<XElement>.Fail(item.Name, ChangeType.Export, "Failed to export", ex);
            }
        }

        private SyncAttempt<XElement> SerializeUpdatePreValues(IDataTypeDefinition item, XElement node)
        {

            var mapper = LoadMapper(node, item.PropertyEditorAlias);

            // we clear them out, and write them ourselves.
            var nodePreValues = node.Element("PreValues");
            if (nodePreValues != null)
                nodePreValues.RemoveNodes();

            var itemPreValues = GetPreValues(item);
            foreach(var itemPreValuePair in itemPreValues)
            {
                var preValue = itemPreValuePair.Value;
                var preValueValue = preValue.Value;

                XElement preValueNode = new XElement("PreValue",
                    new XAttribute("Id", preValue.Id.ToString()),
                    new XAttribute("Value", preValueValue));

                if (!itemPreValuePair.Key.StartsWith("zzzuSync"))
                    preValueNode.Add(new XAttribute("Alias", itemPreValuePair.Key));

                if (mapper != null)
                {
                    Guid newGuid = Guid.NewGuid();
                    if (mapper.MapToGeneric(preValueValue, newGuid))
                    {
                        preValueNode.Add(new XAttribute("MapGuid", newGuid));
                    }
                }

                nodePreValues.Add(preValueNode);
            }

            return SyncAttempt<XElement>.Succeed(item.Name, node, ChangeType.Export);
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
                        .ToDictionary(preValue => "zzzUsync" + preValue.Id.ToString(), preValue => preValue);
            }
        }


        public override bool IsUpdate(XElement node)
        {
            var nodeHash = node.GetSyncHash();
            if (string.IsNullOrEmpty(nodeHash))
                return true;

            var defNode = node.Attribute("Definition");
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

            uSyncValueMapper mapper = new uSyncValueMapper(node, settings);

            return null;
        }
    }
}
