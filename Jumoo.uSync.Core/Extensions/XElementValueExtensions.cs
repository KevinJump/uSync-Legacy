using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XElementValueExtensions
    {
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


    }
}
