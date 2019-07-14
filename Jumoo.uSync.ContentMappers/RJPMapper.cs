using Jumoo.uSync.Core.Mappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Services;

namespace Jumoo.uSync.ContentMappers
{
    public class RJPMapper : IContentMapper
    {
        IEntityService _entityService;

        public RJPMapper()
        {
            _entityService = ApplicationContext.Current.Services.EntityService;
        }

        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            var links = JsonConvert.DeserializeObject<JArray>(value);
            if (links != null)
            {
                foreach(dynamic link in links)
                {
                    if (link.id != null)
                    {
                        var objectType = _entityService.GetObjectType((int)link.id);
                        if (objectType != UmbracoObjectTypes.Unknown)
                        {
                            var keys = _entityService.GetAll(objectType, (int)link.id);
                            if (keys != null && keys.Any() && keys.FirstOrDefault() != null)
                            {
                                link.id = keys.FirstOrDefault().Key;
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(links, Formatting.Indented);
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            var links = JsonConvert.DeserializeObject<JArray>(content);
            if (links != null)
            {
                foreach (dynamic link in links)
                {
                    if (link.id != null)
                    {
                        Guid key;
                        if (Guid.TryParse(link.id.ToString(), out key))
                        {
                            var id = GetItemIdFromGuid(key);
                            if (id > 0) {
                                link.id = id;
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(links, Formatting.Indented);
        }

        private int GetItemIdFromGuid(Guid key)
        {
            var ids = _entityService.GetAll(UmbracoObjectTypes.Document, new[] { key });
            if (ids == null || !ids.Any())
                ids = _entityService.GetAll(UmbracoObjectTypes.Media, new[] { key });

            if (ids !=null || ids.Any())
            {
                return ids.FirstOrDefault().Id;
            }

            return 0;
        }
    }
}
