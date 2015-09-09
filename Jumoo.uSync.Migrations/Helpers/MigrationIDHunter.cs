using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Migrations.Helpers
{
    /// <summary>
    ///  hunts for the key for a node inside the XML,
    /// 
    ///  this doesn't live in the core, only Migrations really
    ///  need to do this, it's a bit hacky, so leave it in
    ///  the migration helpers for now.
    /// </summary>
    public class MigrationIDHunter
    {
        public static string GetItemId(XElement node)
        {
            var key = string.Empty;
                
            key = node.Element("Key").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(key))
                return key;

            key = node.Attribute("Key").ValueOrDefault(string.Empty);
            if (!string.IsNullOrEmpty(key))
                return key;

            key = node.Element("Info") != null ? node.Element("Info").Element("Key").ValueOrDefault(string.Empty) : string.Empty;
            if (!string.IsNullOrEmpty(key))
                return key;
            return key;
        }

        public static bool FindInFiles(string folder, string key)
        {
            // hunts for the string inside any file in the folder.
            if (!Directory.Exists(folder))
                return false; 

            foreach(var file in Directory.GetFiles(folder, "*.config"))
            {
                // look in the file
                if (FileContains(file, key))
                    return true;
            }

            foreach(var dir in Directory.GetDirectories(folder))
            {
                if (FindInFiles(dir, key))
                    return true;
            }
                    
            return false;
        }

        private static bool FileContains(string filename, string value)
        {
            if (System.IO.File.Exists(filename))
            {
                using (StreamReader sr = new StreamReader(filename))
                {
                    string contents = sr.ReadToEnd();
                    if (contents.Contains(value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}