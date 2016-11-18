
namespace Jumoo.uSync.BackOffice
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.IO;
    using System.Xml.Serialization;

    using Umbraco.Core.IO;
    using Umbraco.Core.Logging;

    public class uSyncBackOfficeConfig
    {
        private uSyncBackOfficeSettings _settings;
        public uSyncBackOfficeSettings Settings
        {
            get { return _settings; }
        }

        public uSyncBackOfficeConfig()
        {
            Init();
        }

        private void Init()
        {
            if (_settings != null) return;

            try
            {
                var configFile = IOHelper.MapPath(
                    Path.Combine(SystemDirectories.Config, "uSyncBackOffice.Config"));

                if (System.IO.File.Exists(configFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(uSyncBackOfficeSettings));
                    string xml = File.ReadAllText(configFile);
                    using (TextReader reader = new StringReader(xml))
                    {
                        _settings = (uSyncBackOfficeSettings)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn<uSyncBackOfficeConfig>("Unable to load the settings: {0}", () => ex);
            }

            if (_settings == null)
            {
                // default settings...
                _settings = new uSyncBackOfficeSettings
                {
                    Import = true,
                    ExportAtStartup = false,
                    ExportOnSave = true,
                    WatchForFileChanges = false,
                    ArchiveVersions = false,

                    Folder = "~/uSync/data/",
                    ArchiveFolder = "~/uSync/Archive/",
                    BackupFolder = "~/uSync/Backup/",
                    DontThrowErrors = false, 

                    Handlers = new List<HandlerGroup>()
                    {
                        new HandlerGroup()
                    }
                };

                foreach(var handler in uSyncBackOfficeContext.Instance.Handlers)
                {
                    _settings.Handlers[0].Handlers.Add(new HandlerConfig
                    {
                        Name = handler.Name,
                        Enabled = true,
                        Settings = new List<uSyncHandlerSetting>()
                    });
                }
         
                // save the defaults to disk...
                SaveSettings(_settings);
            }
        }

        public void SaveSettings(uSyncBackOfficeSettings settings)
        {
            try
            {
                var configFile = IOHelper.MapPath(
                    Path.Combine(SystemDirectories.Config, "uSyncBackOffice.Config"));

                if (File.Exists(configFile))
                    File.Delete(configFile);

                XmlSerializer serializer = new XmlSerializer(typeof(uSyncBackOfficeSettings));

                using (StreamWriter w = new StreamWriter(configFile))
                {
                    serializer.Serialize(w, settings);
                }
            }
            catch(Exception ex)
            {
                LogHelper.Warn<uSyncBackOfficeConfig>("Unable to save settings to disk: {0}", () => ex.ToString());
            }
        }
    }

    public class HandlerGroup
    {
        public HandlerGroup() 
        {
            Group = "Default";
            EnableMissing = true;
            Handlers = new List<HandlerConfig>();
        }

        [XmlAttribute("Group")]
        public string Group { get; set; }

        [XmlAttribute("EnableMissing")]
        public bool EnableMissing { get; set; }

        [XmlElement("HandlerConfig")]
        public List<HandlerConfig> Handlers { get; set; }
    }


    public class HandlerConfig
    {
        public HandlerConfig()
        {
            Actions = "All";
        }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        ///  Actions, what the handler will do
        ///  CommaSeperated list of options
        ///
        ///  For BackOffice these values can be
        /// 
        ///   All - Do everything
        ///   Import - Do imports
        ///   Export - Do Exports
        ///   Events - Listen for the saves and deletes
        ///  
        /// </summary>

        [XmlAttribute(AttributeName = "Actions")]
        public string Actions { get; set; }

        // handler modes ? 
        // Syncronize (So new, copies, deletes, renames)
        // Contribute (adds only, nothing else is pushed)

        [XmlElement("Setting")]

        public List<uSyncHandlerSetting> Settings { get; set; }


    }

    [XmlType("Setting")]
    public class uSyncHandlerSetting
    {
        [XmlAttribute(AttributeName = "Key")]
        public string Key { get; set; }

        [XmlAttribute(AttributeName = "Value")]
        public string Value { get; set; }
    }


    public class uSyncBackOfficeSettings
    {
        public uSyncBackOfficeSettings()
        {
            HandlerGroup = "default";

            // get it from web.config if it's there (but you will have to remove the one in 
            // the usync settings to get that one to work) 
            if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["uSync.HandlerGroup"]))
                HandlerGroup = ConfigurationManager.AppSettings["uSync.HandlerGroup"];
        }

        public string MappedFolder()
        {
            return IOHelper.MapPath(Folder);
        }

        public bool Import { get; set; }
        public bool ExportAtStartup { get; set; }
        public bool ExportOnSave { get; set; }
        public bool WatchForFileChanges { get; set; }

        public bool ArchiveVersions { get; set; }

        public string Folder { get; set; }
        public string ArchiveFolder { get; set; }
        public string BackupFolder { get; set; }
        public int MaxArchiveVersionCount { get; set; }

        public bool DontThrowErrors { get; set; }

        public bool UseShortIdNames { get; set; }

        public string HandlerGroup { get; set; }

        [XmlElement("Handlers")]
        public List<HandlerGroup> Handlers {get;set;}

        public bool PreserveAllFiles { get; set; }
    }
}