using System;
using System.Collections.Generic;
using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Mappers;

namespace Jumoo.uSync.ContentMappers
{
    public class VortoContentMapper : IContentMapper
    {
        public VortoContentMapper() {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        private readonly IDataTypeService _dataTypeService;


        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            return MapVortoValues(dataTypeDefinitionId, value, false);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            return MapVortoValues(dataTypeDefinitionId, content, true);
        }


        public string MapVortoValues(int dataTypeDefinitionId, string value, bool import = true)
        {
            string vortoDataType = _dataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId).PreValuesAsDictionary["dataType"].Value;

            var config = JsonConvert.DeserializeObject<JObject>(vortoDataType);
            var propEditor = config.Value<string>("propertyEditorAlias");
            var docTypeGuid = Guid.Parse(config.Value<string>("guid"));

            IContentMapper mapper = ContentMapperFactory.GetMapper(propEditor);

            if (mapper != null)
            {
                var dtd = _dataTypeService.GetDataTypeDefinitionById(docTypeGuid);
                if (dtd != null)
                {

                    LogHelper.Debug<VortoContentMapper>("Vorto: {0}", () => value);
                    // map some vorto values here.... 
                    var vorto = JsonConvert.DeserializeObject<uSyncVortoValue>(value);
                    var newValue = new uSyncVortoValue { DtdGuid = vorto.DtdGuid, Values = new Dictionary<string, object>() };

                    if (vorto.Values != null && vorto.Values.Any())
                    {
                        foreach (var v in vorto.Values)
                        {
                            var mapped = "";
                            if (import)
                                mapped = mapper.GetImportValue(dtd.Id, (string)v.Value);
                            else
                                mapped = mapper.GetExportValue(dtd.Id, (string)v.Value);

                            newValue.Values.Add(v.Key, mapper.GetExportValue(dtd.Id, mapped));
                        }

                        value = JsonConvert.SerializeObject(newValue, Formatting.None);
                    }
                }
            }
            return value; 
        }

    }

    public class uSyncVortoValue
    {
        [JsonProperty("values")]
        public IDictionary<string, object> Values { get; set; }

        [JsonProperty("dtdGuid")]
        public Guid DtdGuid { get; set; }
    }
}
