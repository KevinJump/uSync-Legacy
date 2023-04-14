﻿using System.Linq;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

using Newtonsoft.Json;

using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Mappers;

using Archetype.Models;

namespace Jumoo.uSync.ContentMappers
{
    public class ArchetypeContentMapper : IContentMapper
    {
        public ArchetypeContentMapper()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
        }

        private readonly IDataTypeService _dataTypeService;

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            // We need to retrieve the datatype associated with the property so that we can parse the fieldset
            // then we will go through each item in the fieldset and if there is a mapper associated with that item's datatype
            // we should map the property value

            string archetypeConfig = _dataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId).PreValuesAsDictionary["archetypeConfig"].Value;

            var config = JsonConvert.DeserializeObject<ArchetypePreValue>(archetypeConfig);

            var typedContent = JsonConvert.DeserializeObject<ArchetypeModel>(value);

            foreach (ArchetypePreValueFieldset fieldSet in config.Fieldsets)
            {
                foreach (ArchetypePreValueProperty property in fieldSet.Properties)
                {
                    IDataTypeDefinition dataType = _dataTypeService.GetDataTypeDefinitionById(property.DataTypeGuid);

                    IContentMapper mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);

                    if (mapper != null)
                    {
                        typedContent.Fieldsets.AsQueryable()
                                    .Where(fs => fs.Alias == fieldSet.Alias)
                                    .SelectMany(fs => fs.Properties)
                                    .Where(p => p.Alias == property.Alias)
                                    .ForEach(pm => pm.Value = mapper.GetExportValue(dataType.Id, pm.Value.ToString()));
                    }
                }
            }
            

            return typedContent.SerializeForPersistence();
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            // We need to retrieve the datatype associated with the property so that we can parse the fieldset
            // then we will go through each item in the fieldset and if there is a mapper associated with that item's datatype
            // we should pull out the property value and map it

            string archetypeConfig = _dataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId).PreValuesAsDictionary["archetypeConfig"].Value;

            var config = JsonConvert.DeserializeObject<ArchetypePreValue>(archetypeConfig);

            var typedContent = JsonConvert.DeserializeObject<ArchetypeModel>(content);

            foreach (ArchetypePreValueFieldset fieldSet in config.Fieldsets)
            {
                foreach (ArchetypePreValueProperty property in fieldSet.Properties)
                {
                    IDataTypeDefinition dataType = _dataTypeService.GetDataTypeDefinitionById(property.DataTypeGuid);

                    IContentMapper mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);

                    if (mapper != null)
                    {
                        typedContent.Fieldsets.AsQueryable()
                                    .SelectMany(fs => fs.Properties)
                                    .Where(p => p.Alias == property.Alias)
                                    .ForEach(pm => pm.Value = mapper.GetImportValue(dataType.Id, pm.Value.ToString()));
                    }
                }
            }

            return typedContent.SerializeForPersistence();
        }
    }
}