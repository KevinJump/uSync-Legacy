using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Mappers
{
    class ContentIdMapper : IContentMapper
    {
        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            foreach (Match m in Regex.Matches(value, @"\d{4,9}"))
            {
                int id;
                if (int.TryParse(m.Value, out id))
                {
                    Guid? itemGuid = GetGuidFromId(id);
                    if (itemGuid != null && !replacements.ContainsKey(m.Value))
                    {
                        replacements.Add(m.Value, itemGuid.ToString().ToLower());
                    }
                }
            }

            foreach (var pair in replacements)
            {
                value = value.Replace(pair.Key, pair.Value);
            }

            return value;

        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();

            string guidRegEx = @"\b[A-Fa-f0-9]{8}(?:-[A-Fa-f0-9]{4}){3}-[A-Fa-f0-9]{12}\b";

            foreach (Match m in Regex.Matches(content, guidRegEx))
            {
                var id = GetIdFromGuid(Guid.Parse(m.Value));

                if ((id != -1) && (!replacements.ContainsKey(m.Value)))
                {
                    replacements.Add(m.Value, id.ToString());
                }
            }

            foreach (KeyValuePair<string, string> pair in replacements)
            {
                content = content.Replace(pair.Key, pair.Value);
            }

            return content;
        }


        internal int GetIdFromGuid(Guid guid)
        {
            var item = ApplicationContext.Current.Services.EntityService.GetByKey(guid);
            if (item != null)
                return item.Id;

            return -1;
        }

        internal Guid? GetGuidFromId(int id)
        {
            var item = ApplicationContext.Current.Services.EntityService.Get(id);
            if (item != null)
                return item.Key;

            return null;
        }

    }
}
