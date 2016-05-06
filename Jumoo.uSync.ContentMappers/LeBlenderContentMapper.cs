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

            var jsonValue = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(value);
            if (jsonValue == null)
                return value;


            foreach(var control in jsonValue)
            {
                Guid dtdGuid = Guid.Empty;
                if (Guid.TryParse(control.Value.Value<string>("dataTypeGuid"), out dtdGuid))
                {
                    var dataType = _dataTypeService.GetDataTypeDefinitionById(dtdGuid);
                    if (dataType != null)
                    {
                        var mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);
                        if (mapper != null)
                        {
                            var mappedValue = "";
                            if (import)
                                mappedValue = mapper.GetImportValue(dataType.Id, control.Value.Value<string>("value"));
                            else
                                mappedValue = mapper.GetExportValue(dataType.Id, control.Value.Value<string>("value"));

                            control.Value["value"] = mappedValue;
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(jsonValue, Formatting.Indented);
        }

        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
        }

    }
}
