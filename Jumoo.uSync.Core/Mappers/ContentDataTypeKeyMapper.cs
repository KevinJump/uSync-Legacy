using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Mappers
{
    /// <summary>
    ///  maps datatypes where the internal ID is stored in the content, 
    ///  we map this to the alias value and back...
    /// </summary>
    public class ContentDataTypeKeyMapper : IContentMapper
    {
        /// <summary>
        ///  takes a key (or commaseperated list of keys) and 
        ///  turns it into an alias (or commaseperated list of aliases)
        /// </summary>
        public string GetExportValue(int dataTypeDefinitionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            var prevalues =
                ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId).PreValuesAsDictionary;
            if (prevalues != null && prevalues.Count > 0)
            {
                var values = value.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                var exportValue = "";

                foreach (var id in values)
                {
                    int idValue;

                    if (!int.TryParse(id, out idValue))
                        continue;

                    string aliasValue = prevalues.Where(kvp => kvp.Value.Id == idValue)
                                            .Select(kvp => kvp.Key).SingleOrDefault();

                    if (!string.IsNullOrEmpty(aliasValue))
                    {
                        exportValue += aliasValue + ",";
                    }
                    else
                    {
                        exportValue += id + ",";
                    }
                }

                return exportValue.Trim(",");
            }

            return value;
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            LogHelper.Debug<ContentDataTypeKeyMapper>("Mapping a datatype: {0} {1}", () => dataTypeDefinitionId, () => content);

            var prevalues =
                ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId)
                                  .PreValuesAsDictionary;

            if (prevalues != null && prevalues.Count > 0)
            {
                var importValue = ""; 

                var values = content.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var alias in values)
                {
                    string keyValue = prevalues.Where(kvp => kvp.Key == alias)
                        .Select(kvp => kvp.Value.Id.ToString())
                        .SingleOrDefault();

                    if (!string.IsNullOrWhiteSpace(keyValue))
                    {
                        importValue = string.Format("{0}{1},", importValue, keyValue);
                    }
                    else
                    {
                        importValue = string.Format("{0}{1},", importValue, alias);
                    }
                }

                LogHelper.Debug<ContentDataTypeKeyMapper>("Setting value {0} to {1}", () => content, () => importValue.Trim(","));
                return importValue.Trim(",");
            }

            return content;
        }
    }
}
