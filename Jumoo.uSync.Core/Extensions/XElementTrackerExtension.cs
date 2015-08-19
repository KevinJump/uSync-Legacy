using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Extensions
{
    public static class XElementTrackerExtension
    {
        public static string GetSyncHash(this XElement node)
        {
            XElement copy = new XElement(node);

            // strip ids and stuff.
            var preVals = copy.Element("PreValues");
            if (preVals != null && preVals.HasElements)
            {
                foreach(var preVal in preVals.Elements("PreValue"))
                {
                    preVals.SetAttributeValue("Id", "");
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
