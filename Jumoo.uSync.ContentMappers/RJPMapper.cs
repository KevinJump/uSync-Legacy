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
using Jumoo.uSync.Core;

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
                        var attempt = _entityService.uSyncGetKeyForId((int)link.Id);
                        if (attempt.Success)
                            link.id = attempt.Result;                        
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
                            var attempt = GetItemIdFromGuid(key);
                            if (attempt.Success) {
                                link.id = attempt.Result;
                            }
                        }
                    }
                }
            }

            return JsonConvert.SerializeObject(links, Formatting.Indented);
        }

        private Attempt<int> GetItemIdFromGuid(Guid key)
        {
            var attempt = _entityService.GetIdForKey(key, UmbracoObjectTypes.Document);
            if (attempt.Success == false)
                attempt = _entityService.GetIdForKey(key, UmbracoObjectTypes.Media);


            return attempt;
        }
    }
}
