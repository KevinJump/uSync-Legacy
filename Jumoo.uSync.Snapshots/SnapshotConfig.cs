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
                    using (FileStream fs = new FileStream(configFile, FileMode.Open))
                    {
                        Settings = (uSyncSnapshotSettings)serializer.Deserialize(fs);
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
            }
        }
    }


    public class uSyncSnapshotSettings
    {
        public List<uSyncSnapshotFolderSetting> Folders { get; set; }

        public uSyncSnapshotSettings()
        {
            Folders = new List<uSyncSnapshotFolderSetting>();
        }
    }

    public class uSyncSnapshotFolderSetting
    {
        public string Path { get; set; }
    }
}
