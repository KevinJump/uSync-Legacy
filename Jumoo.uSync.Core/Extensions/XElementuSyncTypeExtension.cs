using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XElementuSyncTypeExtension
    {
        /// <summary>
        ///  returns the type of a XElement based on it's name
        ///  
        ///  we use this to work out just what type of thing we
        ///  have, we can then have a generic serialize/deserialize
        /// 
        ///  (but we can also work backwards from unknown xelements)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Type GetUmbracoType(this XElement node)
        {
            switch(node.Name.LocalName)
            {
                case Constants.Packaging.DocumentTypeNodeName:
                    return typeof(IContentType);
                case Constants.Packaging.DictionaryItemNodeName:
                    return typeof(IDictionaryItem);
                case Constants.Packaging.LanguagesNodeName:
                    return typeof(ILanguage);
                case Constants.Packaging.TemplateNodeName:
                    return typeof(ITemplate);
                case Constants.Packaging.MacroNodeName:
                    return typeof(IMacro);
                case Constants.Packaging.DataTypeNodeName:
                    return typeof(IDataTypeDefinition);
                case "MediaType":
                    return typeof(IMediaType);
                default:
                    return default(Type);
            }
        }

        public static bool IsArchiveFile(this XElement node)
        {
            if (node != null && node.Name != null)
                return (node.Name.LocalName.InvariantEquals("usyncarchive"));

            return false;
        }
    }
}
