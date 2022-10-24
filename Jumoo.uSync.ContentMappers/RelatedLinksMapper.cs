using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Mappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Web.Models;

namespace Jumoo.uSync.ContentMappers
{
    public class RelatedLinksMapper : IContentMapper2
    {
        private readonly IEntityService _entityService;
        private readonly JsonSerializerSettings _serializerSettings;

        public RelatedLinksMapper()
        {
            _entityService = ApplicationContext.Current.Services.EntityService;

            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() },
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

#pragma warning disable CS0618 // Type or member is obsolete
        public string[] PropertyEditorAliases => new[]
        {
            Constants.PropertyEditors.RelatedLinksAlias,
            Constants.PropertyEditors.RelatedLinks2Alias,
        };
#pragma warning restore CS0618 // Type or member is obsolete

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            var links = JsonConvert.DeserializeObject<List<RelatedLink>>(value, _serializerSettings);

            if (links?.Any() == true)
            {
                foreach (var link in links)
                {
                    if (link.Type == RelatedLinkType.Internal &&
                        int.TryParse(link.Link, out var id) == true &&
                        id > 0)
                    {
                        var attempt = _entityService.uSyncGetKeyForId(id);
                        if (attempt.Success == true)
                        {
                            link.Link = attempt.Result.ToString();
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(links, _serializerSettings);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            var links = JsonConvert.DeserializeObject<List<RelatedLink>>(content, _serializerSettings);

            if (links?.Any() == true)
            {
                foreach (var link in links)
                {
                    if (link.Type == RelatedLinkType.Internal &&
                        Guid.TryParse(link.Link, out var guid) == true &&
                        guid.Equals(Guid.Empty) == false)
                    {
                        var attempt = _entityService.GetIdForKey(guid, UmbracoObjectTypes.Document);
                        if (attempt.Success)
                        {
                            link.Link = attempt.Result.ToString();
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(links, _serializerSettings);
        }
    }
}
