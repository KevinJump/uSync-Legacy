using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Core.Mappers
{
    class ImageCropperMapper : IContentMapper
    {
        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            if (!uSyncCoreContext.Instance.Configuration.Settings.MoveMedia)
                return value;

            var cropper = JsonConvert.DeserializeObject<dynamic>(value);
            if (cropper != null)
            {
                var mediaLocation = cropper.src;
            }

            return value;
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            if (!uSyncCoreContext.Instance.Configuration.Settings.MoveMedia)
                return content;

            return content; 
        }
    }
}
