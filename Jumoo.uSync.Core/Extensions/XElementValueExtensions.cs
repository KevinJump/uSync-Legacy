using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XElementValueExtensions
    {
        /// <summary>
        ///  trys to get a name for something based on a node 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static string NameFromNode(this XElement node)
        {
            if (node == null)
                return "unknown";

            var name = node.Name.LocalName;

            // macro
            if (node.Element("alias") != null)
                return node.Element("alias").Value;

            // data types
            if (node.Attribute("Name") != null)
                return node.Attribute("Name").Value;

            // doc types and media types
            if (node.Element("Info") != null && node.Element("Info").Element("Alias") != null)
                return node.Element("Info").Element("Alias").Value;

            // languages
            if (node.Attribute("CultureAlias") != null)
                return node.Attribute("CultureAlias").Value;

            // dictionary items
            if (node.Attribute("Key") != null)
                return node.Attribute("Key").Value;

            // content 
            if (node.Attribute("nodeName") != null)
                return node.Attribute("nodeName").Value;

            // some catch alls, incase we've missed on.
            if (node.Element("Name") != null)
                return node.Element("Name").Value;

            if (node.Element("Alias") != null)
                return node.Element("Alias").Value;

            if (node.Element("name") != null)
                return node.Element("Name").Value;
            return name;
        }

        public static Guid KeyOrDefault(this XElement node)
        {
            if (node.Element("Info") != null && node.Element("Info").Element("Key") != null)
                return node.Element("Info").Element("Key").ValueOrDefault(Guid.Empty);

            if (node.Attribute("Key") != null)
                return node.Attribute("Key").ValueOrDefault(Guid.Empty);

            if (node.Element("Key") != null)
                return node.Element("Key").ValueOrDefault(Guid.Empty);

            return Guid.Empty;
        }

        public static string ValueOrDefault(this XElement node, string defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
                return node.Value;

            return defaultValue;
        }

        public static bool ValueOrDefault(this XElement node, bool defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                bool val;
                if (bool.TryParse(node.Value, out val))
                    return val;
            }

            return defaultValue;
        }

        public static int ValueOrDefault(this XElement node, int defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                int val;
                if (int.TryParse(node.Value, out val))
                    return val;
            }

            return defaultValue;
        }

        public static Guid ValueOrDefault(this XElement node, Guid defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                Guid val;
                if (node.Value != "00000000-0000-0000-0000-000000000000" &&
                    Guid.TryParse(node.Value, out val))
                    return val;
            }
            return defaultValue;
        }

        public static bool ValueOrDefault(this XAttribute node, bool defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                bool val;
                if (bool.TryParse(node.Value, out val))
                    return val;
            }

            return defaultValue;
        }

        public static int ValueOrDefault(this XAttribute node, int defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                int val;
                if (int.TryParse(node.Value, out val))
                    return val;
            }

            return defaultValue;
        }

        public static string ValueOrDefault(this XAttribute node, string defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
                return node.Value;

            return defaultValue;
        }

        public static DateTime ValueOrDefault(this XAttribute node, DateTime defaultValue)
        {
            if (node != null && !string.IsNullOrEmpty(node.Value))
            {
                DateTime val;
                if (DateTime.TryParse(node.Value, out val))
                    return val;
            }

            return defaultValue;
        }

        public static Guid ValueOrDefault(this XAttribute node, Guid defaultValue)
        {
            if ( node != null && !string.IsNullOrEmpty(node.Value))
            {
                Guid val;
                if (Guid.TryParse(node.Value, out val))
                    return val;
            }

            return defaultValue;
        }

        public static Type GetTypeFromElement(this XElement element)
        {
            switch(element.Name.LocalName)
            {
                case "DictionaryItem":
                    return typeof(IDictionaryItem);
                case "DataType":
                    return typeof(IDataTypeDefinition);
                case "DocumentType":
                    return typeof(IContentType);
                case "MediaType":
                    return typeof(IMediaType);
                case "Template":
                    return typeof(ITemplate);
                case "Language":
                    return typeof(ILanguage);
                case "macro":
                    return typeof(IMacro);
            }

            return null;
        }
    }
}
