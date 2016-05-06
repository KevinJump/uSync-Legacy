using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core.Mappers
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

            var mapping =
                uSyncCoreContext.Instance.Configuration.Settings.ContentMappings
                .SingleOrDefault(x => x.EditorAlias == propEditor);

            if (mapping != null)
            {
                IContentMapper mapper = ContentMapperFactory.GetMapper(mapping);

                if (mapper != null)
                {
                    var dtd = _dataTypeService.GetDataTypeDefinitionById(docTypeGuid);
                    if (dtd != null)
                    {

                        LogHelper.Debug<VortoContentMapper>("Vorto: {0}", () => value);
                        // map some vorto values here.... 
                        var vorto = JsonConvert.DeserializeObject<uSyncVortoValue>(value);


                        if (vorto.Values.Any())
                        {
                            var newValue = new uSyncVortoValue();
                            newValue.DtdGuid = vorto.DtdGuid;
                            newValue.Values = new Dictionary<string, object>();

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
