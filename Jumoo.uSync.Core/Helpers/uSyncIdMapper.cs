using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using Umbraco.Core.IO;

namespace Jumoo.uSync.Core.Helpers
{
    // dropping mapping in favor of guid changing 
    // (so making the guids the same on all installtions)
    //
    // we might include a finder - so first time we attempt to sync by name?
    //

    /*
    public class uSyncIdMapper
    {
        public static Dictionary<Guid, Guid> pairs = new Dictionary<Guid, Guid>();
        private static readonly string _pairFile;

        private static Timer _saveTimer;
        private static object _saveLock = new object();

        static uSyncIdMapper()
        {
            // load the pair file
            _pairFile = IOHelper.MapPath("~/App_Data/Temp/_usyncimport.xml");
            LoadPairFile();

            _saveTimer = new Timer(1000);
            _saveTimer.Elapsed += _saveTimer_Elapsed;

        }
        #region FileOps

        private static void _saveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_saveLock)
            {
                _saveTimer.Stop();
                SavePairFile();
            }
        }

        private static void LoadPairFile()
        {
            pairs = new Dictionary<Guid, Guid>();

            if (System.IO.File.Exists(_pairFile))
            {
                XElement source = XElement.Load(_pairFile);

                var sourcePairs = source.Descendants("pair");
                foreach (var pair in sourcePairs)
                {
                    pairs.Add(
                        Guid.Parse(pair.Attribute("id").Value), Guid.Parse(pair.Attribute("guid").Value));
                }
            }
        }

        private static void SavePairFile()
        {
            if (System.IO.File.Exists(_pairFile))
                System.IO.File.Decrypt(_pairFile);

            XElement saveNode = new XElement("content");
            foreach (var pair in pairs)
            {
                XElement pairNode = new XElement("pair",
                                        new XElement("id", pair.Key.ToString()),
                                        new XElement("guid", pair.Value.ToString()));

                saveNode.Add(pairNode);
            }

            saveNode.Save(_pairFile);
        }

        #endregion

        public static Guid GetTargetGuid(Guid source)
        {
            if (pairs.ContainsKey(source))
                return pairs[source];

            return source;
        }

        public static Guid GetSourceGuid(Guid target)
        {
            if (pairs.ContainsValue(target))
                return pairs.FirstOrDefault(x => x.Value == target).Key;

            return target;
        }

        public static void AddPair(Guid source, Guid target)
        {
            if (pairs.ContainsKey(source))
                pairs.Remove(source);

            pairs.Add(source, target);

            lock (_saveLock) { _saveTimer.Start(); } // kicks of the save process...
        }

        public static void Remove(Guid id)
        {
            bool change = false;
            if (pairs.ContainsKey(id))
            {
                pairs.Remove(id);
                change = true;
            }
            else if (pairs.ContainsValue(id))
            {
                var key = pairs.FirstOrDefault(x => x.Value == id).Key;
                pairs.Remove(id);
                change = true;
            }

            if (change)
            {
                lock (_saveLock) { _saveTimer.Start(); } // kicks of the save process...
            }
        }
    }
    */
}
