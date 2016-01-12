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
    ///  maps datatype values inside content, forward and back.
    /// </summary>
    public class ContentDataTypeMapper : IContentMapper
    {
        /// <summary>
        ///  takes a list of value names and returns a list 
        ///  of alias values 
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

                foreach (var val in values)
                {
                    string aliasValue = prevalues.Where(kvp => kvp.Value.Value == val)
                                            .Select(kvp => kvp.Key)
                                            .SingleOrDefault();

                    if (!String.IsNullOrWhiteSpace(aliasValue))
                    {
                        exportValue += aliasValue + ",";
                    }
                    else
                    {
                        exportValue += val + ",";
                    }
                }

                return exportValue.Trim(",");

            }

            return value;
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            LogHelper.Debug<ContentDataTypeMapper>("Mapping a datatype: {0} {1}", () => dataTypeDefinitionId, () => content);

            var prevalues =
                ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId)
                                  .PreValuesAsDictionary;

            if (prevalues != null && prevalues.Count > 0)
            {
                var importValue = "";

                var values = content.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach(var alias in values)
                {
                    string preValue = prevalues.Where(kvp => kvp.Key == alias)
                                        .Select(kvp => kvp.Value.Id.ToString())
                                        .SingleOrDefault();

                    if (!String.IsNullOrWhiteSpace(preValue))
                    {
                        importValue = string.Format("{0}{1},", importValue, preValue);
                    }
                    else
                    {
                        importValue = string.Format("{0}{1},", alias, preValue);
                    }
                }
                LogHelper.Debug<ContentDataTypeMapper>("Setting value {0} to {1}", () => content, () => importValue.Trim(","));
                return importValue.Trim(",");
            }
            return content;
        }
    }
}
