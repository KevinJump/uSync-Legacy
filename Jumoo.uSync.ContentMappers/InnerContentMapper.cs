using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Core.Mappers;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

namespace Jumoo.uSync.ContentMappers
{
    public class InnerContentMapper : IContentMapper2
    {
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
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!IsJson(value))
                return null;

            var innerContent = JsonConvert.DeserializeObject<InnerContentValue[]>(value);

            if (innerContent == null)
                return null;

            var allContentTypes = innerContent.Select(x => x.IcContentTypeAlias)
                .Distinct()
                .ToDictionary(a => a, a => _contentTypeService.GetContentType(a));

            //Ensure all of these content types are found
            if (allContentTypes.Values.Any(contentType => contentType == null))
            {
                throw new InvalidOperationException($"Could not resolve these content types for the Inner Content property: {string.Join(",", allContentTypes.Where(x => x.Value == null).Select(x => x.Key))}");
            }

            foreach (var row in innerContent)
            {
                var contentType = allContentTypes[row.IcContentTypeAlias];

                foreach (var key in row.PropertyValues.Keys.ToArray())
                {
                    var propertyType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == key);
                    if (propertyType == null)
                    {
                        LogHelper.Debug<InnerContentMapper>($"No Property Type found with alias {key} on Content Type {contentType.Alias}");
                        continue;
                    }

                    // (can we just use ? ) propertyType.PropertyEditorAlias
                    //  -  i think for legacy (pre v7 it was harder)
                    var dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                    if (dataType != null)
                    {
                        var mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);
                        if (mapper != null)
                        {
                            row.PropertyValues[key] = mapper.GetExportValue(
                                dataType.Id, row.PropertyValues[key].ToString());
                        }
                    }
                }
            }

            value = JsonConvert.SerializeObject(innerContent);
            return value;
        }

        public string GetImportValue(int dataTypeDefinitionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            if (!IsJson(value))
                return null;

            var innerContent = JsonConvert.DeserializeObject<InnerContentValue[]>(value);

            if (innerContent == null)
                return null;

            var allContentTypes = innerContent.Select(x => x.IcContentTypeAlias)
                .Distinct()
                .ToDictionary(a => a, a => _contentTypeService.GetContentType(a));

            //Ensure all of these content types are found
            if (allContentTypes.Values.Any(contentType => contentType == null))
            {
                throw new InvalidOperationException($"Could not resolve these content types for the Inner Content property: {string.Join(",", allContentTypes.Where(x => x.Value == null).Select(x => x.Key))}");
            }

            foreach (var row in innerContent)
            {
                var contentType = allContentTypes[row.IcContentTypeAlias];

                foreach (var key in row.PropertyValues.Keys.ToArray())
                {
                    var propertyType = contentType.CompositionPropertyTypes.FirstOrDefault(x => x.Alias == key);
                    if (propertyType == null)
                    {
                        LogHelper.Debug<InnerContentMapper>($"No Property Type found with alias {key} on Content Type {contentType.Alias}");
                        continue;
                    }

                    // (can we just use ? ) propertyType.PropertyEditorAlias
                    //  -  i think for legacy (pre v7 it was harder)
                    var dataType = _dataTypeService.GetDataTypeDefinitionById(propertyType.DataTypeDefinitionId);
                    if (dataType != null)
                    {
                        var mapper = ContentMapperFactory.GetMapper(dataType.PropertyEditorAlias);
                        if (mapper != null)
                        {
                            row.PropertyValues[key] = mapper.GetImportValue(
                                dataType.Id, row.PropertyValues[key].ToString());
                        }
                    }
                }
            }

            value = JsonConvert.SerializeObject(innerContent);
            return value;
        }

        // we can't use DetectIsJson - as we target a version prior to it 
        // been made public in umbraco        
        private bool IsJson(string val)
        {
            val = val.Trim();
            return (val.StartsWith("{") && val.EndsWith("}"))
                || (val.StartsWith("[") && val.EndsWith("]"));
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
