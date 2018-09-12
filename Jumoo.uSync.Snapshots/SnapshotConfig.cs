using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Snapshots
{
    public class SnapshotConfig
    {
        public uSyncSnapshotSettings Settings { get; set; }

        public SnapshotConfig()
        {
            try
            {
                var configFile = IOHelper.MapPath(
                        Path.Combine(SystemDirectories.Config, "uSyncSnapshot.config")
                    );

                if (System.IO.File.Exists(configFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(uSyncSnapshotSettings));
                    string xml = File.ReadAllText(configFile);
                    using (TextReader reader = new StringReader(xml))
                    {
                        Settings = (uSyncSnapshotSettings)serializer.Deserialize(reader);
                    }
                }

            }
            catch (Exception ex)
            {
                LogHelper.Warn<SnapshotConfig>("Unable to load the settings: {0}", () => ex);
            }

            if (Settings == null)
            {
                Settings = new uSyncSnapshotSettings();
                SaveConfig();
            }

        }

        public void SaveConfig()
        {
            try
            {
                var configFile = IOHelper.MapPath(Path.Combine(SystemDirectories.Config, "uSyncSnapshot.config"));

                if (System.IO.File.Exists(configFile))
                    System.IO.File.Delete(configFile);

                XmlSerializer serializer = new XmlSerializer(typeof(uSyncSnapshotSettings));

                using (StreamWriter w = new StreamWriter(configFile))
                {
                    serializer.Serialize(w, Settings);
                }
            }
            catch(Exception ex)
            {
                LogHelper.Warn<SnapshotConfig>("Error saving config to disk: {0}", () => ex.Message);
            }
        }
    }


    public class uSyncSnapshotSettings
    {
        public string Mode { get; set; }
        public bool Upload { get; set; }

        public List<uSyncSnapshotFolderSetting> Folders { get; set; }

        public uSyncSnapshotSettings()
        {
            Folders = new List<uSyncSnapshotFolderSetting>();
            Mode = SnapshotConstants.combined;
            Upload = true;
        }
    }

    [XmlType("Folder")]
    public class uSyncSnapshotFolderSetting
    {
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }
    }

    public static class SnapshotConstants
    {
        public const string source = "source";
        public const string target = "target";
        public const string combined = "combined";
    }
}
