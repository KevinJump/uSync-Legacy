using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XElementTrackerExtension
    {
        public static string GetSyncHash(this XElement node)
        {
            if (node == null)
                return string.Empty;

            XElement copy = new XElement(node);

            // strip ids and stuff.
            var preVals = copy.Element("PreValues");
            if (preVals != null && preVals.HasElements)
            {
                foreach(var preVal in preVals.Elements("PreValue"))
                {
                    preVal.SetAttributeValue("Id", "");
                }
            }

            // take out any keys? 
            // we might not want to do this, as 
            // keys are something we can set
            /* 
            foreach(var key in copy.Descendants("Key"))
            {
                key.Remove();
            }
            */

            // in content types we remove Definition for comparision, because for 
            // custom types it can change. 
            if (copy.Element("GenericProperties") != null)
            {
                foreach (var defId in copy.Element("GenericProperties").Descendants("Definition"))
                {
                    defId.Value = ""; 
                }
            }

            var nodes = copy.Element("Nodes");
            if (nodes != null)
                nodes.Remove();

            var tabs = copy.Element("Tab");
            if (tabs != null && tabs.HasElements)
            {
                foreach(var tab in tabs.Elements("Tab"))
                {
                    if (tab.Element("Id") != null)
                        tab.Element("Id").Remove();
                }
            }

            if (copy.Name.LocalName == "Language" && copy.Attribute("Id") != null)
            { 
                copy.Attribute("Id").Remove();
            }

            if (copy.Name.LocalName == "DictionaryItem")
            {
                foreach(var val in copy.Elements("Value"))
                {
                    if (val.Attribute("LanguageId") != null)
                        val.Attribute("LanguageId").Remove();
                }
            }

            return MakeHash(copy);
        }

        private static string MakeHash(XElement node)
        {
            string hash = "";
            MemoryStream s = new MemoryStream();
            node.Save(s);

            s.Position = 0;
            using (var md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(s)).Replace("-", "").ToLower();
            }
            s.Close();

            return hash;
        }
    }
}
