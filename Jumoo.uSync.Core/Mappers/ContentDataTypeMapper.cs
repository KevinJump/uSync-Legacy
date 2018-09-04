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
            return value;
            /*
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
            */
        }

        public string GetImportValue(int dataTypeDefinitionId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return content;

            var prevalues =
                ApplicationContext.Current.Services.DataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId).PreValuesAsDictionary;

            if (prevalues != null && prevalues.Count > 0)
            {

                var values = content.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                var importValue = "";

                foreach (var val in values)
                {
                    // ? needs to be set to the key value . 
                    int inValue = prevalues.Where(kvp => kvp.Value.Value == val)
                                            .Select(kvp => kvp.Value.Id)
                                            .SingleOrDefault();

                    if (inValue >= 0)
                    {
                        importValue += inValue + ",";
                    }
                    else
                    {
                        importValue += val + ",";
                    }
                }

                return importValue.Trim(",");
            }

            return content;
        }
    }
}
