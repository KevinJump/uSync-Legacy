using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Services;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Mappers;

namespace Jumoo.uSync.ContentMappers
{
    public class DocTypeGridMapper : IContentMapper
    {
        private IContentTypeService _contentTypeService;
        private IDataTypeService _dataTypeService;

        public DocTypeGridMapper()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        /// <summary>
        ///  doctype grid, stored the doctype as a bit of JSON inside the grid
        ///  So this is very much like the nestedContent mapper with slightly 
        ///  diffrent structure. 
        ///  
        ///  { "dtgeContentTypeAlias" : "alias",
        ///     "value" : {
        ///         "prop" : "value",    
        ///         "prop" : "value",    
        ///         "prop" : "value",    
        ///     },
        ///     "id" : "guid here" 
        ///  }
        /// 
        /// </summary>
        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            return ProcessDocTypeGridValues(value, false);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            return ProcessDocTypeGridValues(content, true);
        }

        /// <summary>
        ///  Import/Export a DocTypeGrid Value, these values are 
        ///  DocTypes-as-JSON inside Grid Values, it's very similar to how
        ///  nested content works but inside the grid. 
        /// 
        ///  import and export are near identical, (just one call) so both
        ///  functions call this - with an import = true/false and we 
        ///  traverse the tree then call the one line depending on the flag
        /// </summary>
        private string ProcessDocTypeGridValues(string content, bool import)
        {

            var jsonValue = JsonConvert.DeserializeObject<JObject>(content);
            if (jsonValue == null)
                return content;

            var docTypeAlias = jsonValue.Value<string>("dtgeContentTypeAlias");
            var docValue = jsonValue.Value<JObject>("value");

            if (docTypeAlias == null || docValue == null)
                return content;

            var docType = _contentTypeService.GetContentType(docTypeAlias);
            if (docType == null)
                return content;

            foreach (var propertyType in docType.CompositionPropertyTypes)
            {
                var prop = docValue[propertyType.Alias];
                if (prop != null)
                {
                    var dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                    if (dataType != null)
                    {
                        var mapping = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings
                            .SingleOrDefault(x => x.EditorAlias == dataType.PropertyEditorAlias);

                        if (mapping != null)
                        {
                            var mapper = ContentMapperFactory.GetMapper(mapping);
                            if (mapper != null)
                            {
                                string mappedValue = "";
                                if (import)
                                    mappedValue = mapper.GetImportValue(dataType.Id, docValue[propertyType.Alias].ToString());
                                else
                                    mappedValue = mapper.GetExportValue(dataType.Id, docValue[propertyType.Alias].ToString());

                                if (!IsJson(mappedValue))
                                    docValue[propertyType.Alias] = mappedValue;
                                else
                                {
                                    var mappedJson = JsonConvert.DeserializeObject<JObject>(mappedValue);
                                    if (mappedJson != null)
                                    {
                                        docValue[propertyType.Alias] = mappedJson;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return JsonConvert.SerializeObject(jsonValue, Formatting.None);

        }

        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
        }

    }
}
