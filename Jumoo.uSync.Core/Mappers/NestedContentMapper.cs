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

namespace Jumoo.uSync.NestedContent
{
    public class NestedContentMapper : IContentMapper
    {
        private IContentTypeService _contentTypeService;
        private IDataTypeService _dataTypeService;

        public NestedContentMapper()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            var array = JsonConvert.DeserializeObject<JArray>(value);
            if (array == null || !array.Any())
                return value; 

            foreach(var nestedObject in array)
            {
                var doctype = _contentTypeService.GetContentType(nestedObject["ncContentTypeAlias"].ToString());
                if (doctype == null)
                    continue;

                foreach(var propertyType in doctype.PropertyTypes)
                {
                    object alias = nestedObject[propertyType.Alias];
                    if (alias != null)
                    {
                        var dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                        if (dataType != null)
                        {
                            uSyncContentMapping mapping =
                                uSyncCoreContext.Instance.Configuration.Settings.ContentMappings.SingleOrDefault(x => x.EditorAlias == dataType.PropertyEditorAlias);

                            if (mapping != null)
                            {
                                IContentMapper mapper = ContentMapperFactory.GetMapper(mapping);
                                if (mapper != null)
                                {
                                    nestedObject[propertyType.Alias] =
                                        mapper.GetExportValue(dataType.Id, nestedObject[propertyType.Alias].ToString());
                                }
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(array);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            var array = JsonConvert.DeserializeObject<JArray>(content);
            if (array == null || !array.Any())
                return content;

            foreach (var nestedObject in array)
            {
                var doctype = _contentTypeService.GetContentType(nestedObject["ncContentTypeAlias"].ToString());
                if (doctype == null)
                    continue;

                foreach (var propertyType in doctype.PropertyTypes)
                {
                    object alias = nestedObject[propertyType.Alias];
                    if (alias != null)
                    {
                        var dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                        if (dataType != null)
                        {
                            uSyncContentMapping mapping =
                                uSyncCoreContext.Instance.Configuration.Settings.ContentMappings.SingleOrDefault(x => x.EditorAlias == dataType.PropertyEditorAlias);

                            if (mapping != null)
                            {
                                IContentMapper mapper = ContentMapperFactory.GetMapper(mapping);
                                if (mapper != null)
                                {
                                    nestedObject[propertyType.Alias] =
                                        mapper.GetImportValue(dataType.Id, nestedObject[propertyType.Alias].ToString());
                                }
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(array);

        }
    }
}
