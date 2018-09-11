using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Core.Mappers;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Newtonsoft.Json.Linq;

namespace Jumoo.uSync.ContentMappers
{
    public class InnerContentMapper : IContentMapper2
    {
        private const string GuidPropertyKey = "icContentTypeGuid";
        private const string AliasPropertyKey = "icContentTypeAlias";

        IContentTypeService _contentTypeService;
        IDataTypeService _dataTypeService;

        public InnerContentMapper()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        public virtual string[] PropertyEditorAliases => new[] { "Our.Umbraco.InnerContent" };

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !IsJson(value))
                return null;

            LogHelper.Debug<InnerContentMapper>("InnerContent : {0}", () => value);

            var token = JsonConvert.DeserializeObject<JToken>(value);
            if (token == null)
                return value;

            RecurseInnerValues(token, true);

            LogHelper.Debug<InnerContentMapper>("InnerContent Export: {0}", () => JsonConvert.SerializeObject(token, Formatting.Indented));

            return JsonConvert.SerializeObject(token);
        }

        public string GetImportValue(int dataTypeDefinitionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !IsJson(value))
                return null;

            LogHelper.Debug<InnerContentMapper>("InnerContent : {0}", () => value);

            var token = JsonConvert.DeserializeObject<JToken>(value);
            if (token == null)
                return value;

            RecurseInnerValues(token, true);

            LogHelper.Debug<InnerContentMapper>("InnerContent Import: {0}", () => JsonConvert.SerializeObject(token, Formatting.Indented));

            return JsonConvert.SerializeObject(token);
        }


        private void RecurseInnerValues(JToken token, bool isExport)
        {
            if (token is JArray)
            {
                var jArr = token as JArray;
                foreach (var item in jArr)
                {
                    RecurseInnerValues(item, isExport);
                }
            }

            if (token is JObject)
            {
                var obj = token as JObject;

                if (obj[GuidPropertyKey] != null || obj[AliasPropertyKey] != null)
                {
                    GetInnerValue(obj, isExport);
                }
                else
                {
                    foreach(var kvp in obj)
                    {
                        if (kvp.Value is JArray || kvp.Value is JObject)
                        {
                            RecurseInnerValues(kvp.Value, isExport);
                        }
                    }
                }
            }
        }

        private void GetInnerValue(JObject item, bool isExport)
        {
            if (item == null) return;

            var contentType = GetContentType(item);
            if (contentType == null)
                return;

            var propValueKey = item.Properties().Select(x => x.Name).ToArray();

            foreach(var propKey in propValueKey)
            {
                var propType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias.InvariantEquals(propKey));
                if (propType != null)
                {
                    var dataType = _dataTypeService.GetDataTypeDefinitionById(propType.DataTypeDefinitionId);
                    if (dataType != null)
                    {
                        var mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);
                        if (mapper != null)
                        {
                            if (isExport)
                            {
                                item[propKey] = mapper.GetExportValue(
                                    dataType.Id, item[propKey].ToString());
                            }
                            else 
                            {
                                item[propKey] = mapper.GetImportValue(
                                    dataType.Id, item[propKey].ToString());
                            }
                        }
                    }
                }
            }
        }


        // we can't use DetectIsJson - as we target a version prior to it 
        // been made public in umbraco        
        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
        }

        private IContentType GetContentType(JObject value)
        {
            var guid = GetContentGuid(value);
            if(guid.HasValue && guid.Value != Guid.Empty)
            {
                return _contentTypeService.GetContentType(guid.Value);
            }

            var alias = GetContentAlias(value);
            if (!string.IsNullOrWhiteSpace(alias))
            {
                return _contentTypeService.GetContentType(alias);
            }

            return null;
        }

        private Guid? GetContentGuid(JObject item)
        {
            var guid = item?[GuidPropertyKey];
            return guid?.ToObject<Guid?>();
        }

        private string GetContentAlias(JObject item)
        {
            var alias = item?[AliasPropertyKey];
            return alias?.ToObject<string>();
        }

        private bool IsSystemPropertyKey(string propKey)
        {
            return propKey == "name"
                            || propKey == "children"
                            || propKey == "key"
                            || propKey == "icon"
                            || propKey == GuidPropertyKey
                            || propKey == AliasPropertyKey;
        }
    }

    public class InnerContentValue
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("icContentTypeAlias")]
        public string IcContentTypeAlias { get; set; }

        [JsonProperty("icContentTypeGuid")]
        public Guid? IcContentTypeGuid { get; set; }

        /// <summary>
        /// The remaining properties will be serialized to a dictionary
        /// </summary>
        /// <remarks>
        /// The JsonExtensionDataAttribute is used to put the non-typed properties into a bucket
        /// http://www.newtonsoft.com/json/help/html/DeserializeExtensionData.htm
        /// </remarks>
        [JsonExtensionData]
        public IDictionary<string, object> PropertyValues { get; set; }
    }
}
