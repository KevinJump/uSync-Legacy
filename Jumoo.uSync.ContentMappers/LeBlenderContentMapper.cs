using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Mappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;
using Umbraco.Core.Services;

namespace Jumoo.uSync.ContentMappers
{
    public class LeBlenderContentMapper : IContentMapper
    {
        IDataTypeService _dataTypeService;

        public LeBlenderContentMapper()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            return MapLeBlenderValue(dataTypeDefinitionId, value, false);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            return MapLeBlenderValue(dataTypeDefinitionId, content, true);
        }

        public string MapLeBlenderValue(int dataTypeDefinitionId, string value, bool import = true)
        {
            if (!IsJson(value))
                return value;


            var valueArray = JsonConvert.DeserializeObject<IEnumerable<Dictionary<string, uSyncLeBlenderGridModel>>>(value);
            foreach (var leblenderItems in valueArray)
            {
                foreach (var kvp in leblenderItems)
                {
                    var item = kvp.Value;

                    Guid dtdGuid = Guid.Empty;
                    if (Guid.TryParse(item.DataTypeGuid, out dtdGuid))
                    {
                        var dataType = _dataTypeService.GetDataTypeDefinitionById(dtdGuid);
                        if (dataType != null)
                        {
                            var mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);
                            if (mapper != null)
                            {
                                var mappedValue = "";
                                if (import)
                                    mappedValue = mapper.GetImportValue(dataType.Id, (string)item.Value);
                                else
                                    mappedValue = mapper.GetExportValue(dataType.Id, (string)item.Value);

                                item.Value = mappedValue;
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(valueArray, Formatting.Indented);
        }

        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
        }

        internal class uSyncLeBlenderGridModel
        {
            [JsonProperty("dataTypeGuid")]
            public String DataTypeGuid { get; set; }

            [JsonProperty("editorName")]
            public String Name { get; set; }

            [JsonProperty("editorAlias")]
            public String Alias { get; set; }

            [JsonProperty("value")]
            public object Value { get; set; }
        }
    }
}
