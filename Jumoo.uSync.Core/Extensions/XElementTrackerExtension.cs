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
        internal static XElement GetLocalizeduSyncElement(this XElement node)
        {
            if (node == null)
                return null;
            XElement copy = new XElement(node);

            // strip ids and stuff.
            var preVals = copy.Element("PreValues");
            if (preVals != null && preVals.HasElements)
            {
                foreach (var preVal in preVals.Elements("PreValue"))
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

            // 7.3.3. update - don't do this now, as it means you can miss some changes.
            // and everything is guids so less faulse positives. in later umbraco v's
            // 
            //if (copy.Element("GenericProperties") != null)
            //{
            //    foreach (var defId in copy.Element("GenericProperties").Descendants("Definition"))
            //    {
            //        defId.Value = "";
            //    }
            //}

            var nodes = copy.Element("Nodes");
            if (nodes != null)
                nodes.Remove();

            var tabs = copy.Element("Tab");
            if (tabs != null && tabs.HasElements)
            {
                foreach (var tab in tabs.Elements("Tab"))
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
                if (copy.Attribute("guid") != null)
                    copy.Attribute("guid").Remove();

                foreach (var val in copy.Elements("Value"))
                {
                    if (val.Attribute("LanguageId") != null)
                        val.Attribute("LanguageId").Remove();
                }
            }

            if (copy.Name.LocalName == "Macro" && copy.Attribute("Key") != null)
                copy.Attribute("Key").Remove();


            if (copy.Attribute("updated") != null)
            {
                copy.Attribute("updated").Remove();
            }

            return copy; 
        }

        public static string GetSyncHash(this XElement node)
        {
            if (node == null)
                return string.Empty;

            return MakeHash(node.GetLocalizeduSyncElement());
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
