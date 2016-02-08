using System.IO;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.Snapshots
{
    /// <summary>
    ///  a thing that goes of and finds ids in files
    ///  we need this when looking to see if things have
    ///  been renamed or something
    /// </summary>
    public class IDHunter
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

            key = node.Attribute("guid").ValueOrDefault(string.Empty);
            return key;
        }

        public static bool FindInFiles(string folder, string key)
        {
            if (!Directory.Exists(folder))
                return false;

            foreach(var file in Directory.GetFiles(folder, "*.config"))
            {
                if (FileContains(file, key))
                {
                    // the key is in the file, but we need to load it to see if it's
                    // actually the key in the file
                    var node = XElement.Load(file);
                    if (node != null)
                    {
                        if (key == GetItemId(node))
                            return true;
                    }
                }
            }

            foreach(var dir in Directory.GetDirectories(folder))
            {
                if (FindInFiles(dir, key))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///  does the key appear anywhere in the file
        ///  - this 'might' be quicker than loading the xml
        ///  - and looking for the actual key.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="value"></param>
        /// <returns></returns>
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
