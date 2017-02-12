using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Mappers
{
    public class GridMacroMapper : IContentMapper
    {
        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            return GetMacroValues(value, false);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            return GetMacroValues(content, true);
        }

        public string GetMacroValues(string content, bool import)
        {
            if (!IsJson(content))
                return content;

            try
            {
                LogHelper.Debug<GridMacroMapper>(">> Mapping Macro: {0}", () => content);

                var json = JToken.Parse(content);

                var macroAlias = json.Value<string>("macroAlias");

                var macro = ApplicationContext.Current.Services.MacroService.GetByAlias(macroAlias);
                var contentProperties = json.Value<JToken>("macroParamsDictionary");
                foreach(var property in macro.Properties)
                {
                    var cp = contentProperties.Value<string>(property.Alias);
                    if (!string.IsNullOrEmpty(cp)) {

                        var propMapper = ContentMapperFactory.GetMapper(property.EditorAlias);
                        if (propMapper != null)
                        {
                            var mappedValue = cp;
                            if (import)
                                mappedValue = propMapper.GetImportValue(0, cp);
                            else
                                mappedValue = propMapper.GetExportValue(0, cp);

                            contentProperties[property.Alias] = mappedValue;
                        }
                    }
                }

                LogHelper.Debug<GridMacroMapper>("<< Mapping Macro: {0}", () => JsonConvert.SerializeObject(json, Formatting.Indented));

                return JsonConvert.SerializeObject(json, Formatting.Indented);
            }
            catch (Exception ex)
            {
                LogHelper.Warn<GridMacroMapper>("Issues Getting the Macro Object to map: {0}", () => ex.Message);
            }
            return content;

        }

        private bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}"))
                || (input.StartsWith("[") && input.EndsWith("]"));
        }

    }
}
