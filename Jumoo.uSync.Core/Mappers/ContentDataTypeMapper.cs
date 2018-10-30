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
            LogHelper.Debug<ContentDataTypeMapper>("Mapping DataType: {0} {1}", () => dataTypeDefinitionId, () => content);

            var prevalues = ApplicationContext.Current.Services.
                DataTypeService.GetPreValuesCollectionByDataTypeId(dataTypeDefinitionId)
                .PreValuesAsDictionary;

            if (prevalues != null && prevalues.Count > 0)
            {
                var values = content.ToDelimitedList();
                var mapped = new List<string>();

                foreach (var value in values)
                {
                    var preValue = prevalues.Where(kvp => kvp.Value.Value.InvariantEquals(value))
                        .Select(x => x.Value).SingleOrDefault();

                    if (preValue != null)
                    {
                        LogHelper.Debug<ContentDataTypeMapper>("Matched PreValue: [{0}] {1}", () => preValue.Id, () => preValue.Value);
                        mapped.Add(preValue.Id.ToString());
                    }
                    else
                    {
                        LogHelper.Debug<ContentDataTypeMapper>("No Matched Value: {0}", () => value);
                        mapped.Add(value);
                    }
                }

                return string.Join(",", mapped);
            }

            return content;
        }
    }   
}
