using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Mappers
{
    class MediaIdMapper : ContentIdMapper
    {
        public MediaIdMapper(string regex)
            : base(regex, UmbracoObjectTypes.Media) { }

        public override string GetExportValue(int dataTypeDefinitionId, string value)
        {
            LogHelper.Debug<MediaIdMapper>("GetExportValue");
            var id = GetIdValue(value);
            LogHelper.Debug<MediaIdMapper>("ID {0}", () => id);
            int intId;
            if (int.TryParse(id, out intId))
            {
                var guid = GetGuidFromId(intId);
                LogHelper.Debug<MediaIdMapper>("Guid: {0}", () => guid.ToString());

                if (guid != null)
                {
                    LogHelper.Debug<MediaIdMapper>("Value {0}", () => SetValue(value, guid.ToString()));
                    return SetValue(value, guid.ToString());
                }
            }

            LogHelper.Debug<MediaIdMapper>("Unalterd Value: {0}", ()=> value);
            return value;
        }

        public override string GetImportValue(int dataTypeDefinitionId, string content)
        {
            var guid = GetIdValue(content);
            Guid guidVal;
            if (Guid.TryParse(guid, out guidVal))
            {
                var intVal = GetIdFromGuid(guidVal);
                if (intVal > 0)
                {
                    return SetValue(content, intVal.ToString());
                }
            }

            return content; 
        }

        private string GetIdValue(string content)
        {
            if (IsJson(content))
            {
                var cropperVal = JToken.Parse(content);
                return cropperVal.Value<string>("id").ToString();
            }
            else
            {
                return content;
            }
        }

        private string SetValue(string value, string replacement)
        {
            if (IsJson(value))
            {
                var cropperVal = JToken.Parse(value);
                cropperVal["id"] = replacement;
                return cropperVal.ToString();
            }

            return replacement;
        }


        private bool IsJson(string input)
        {
            input = input.Trim();
            return (input.StartsWith("{") && input.EndsWith("}"))
                || (input.StartsWith("[") && input.EndsWith("]"));
        }


    }
}
