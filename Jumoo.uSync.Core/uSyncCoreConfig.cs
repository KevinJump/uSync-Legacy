using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core
{
    public class uSyncCoreConfig
    {
        public uSyncCoreSettings Settings { get; set; }

        public uSyncCoreConfig()
        {
            try
            {
                var configFile = IOHelper.MapPath(
                        Path.Combine(SystemDirectories.Config, "uSyncCore.config")
                    );

                if (System.IO.File.Exists(configFile))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(uSyncCoreSettings));
                    string xml = File.ReadAllText(configFile);
                    using (TextReader reader = new StringReader(xml))
                    {
                        Settings = (uSyncCoreSettings)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Warn<uSyncCoreConfig>("Unable to load the settings: {0}", () => ex);
            }

            if (Settings == null)
            {
                // defaults ? 
                Settings = new uSyncCoreSettings();

                Settings.Mappings.Add(new uSyncValueMapperSettings
                {
                    DataTypeId = "Umbraco.MultiNodeTreePicker",
                    MappingType = "content",
                    ValueStorageType = "json",
                    ValueAlias = "startNode"
                });

                Settings.MediaStorageFolder = "~/uSync/MediaFiles/";

                SaveSettings();
            }
        }

        public void SaveSettings()
        {
            try {
                var configFile = IOHelper.MapPath(
                    Path.Combine(SystemDirectories.Config, "uSyncCore.config"));

                if (File.Exists(configFile))
                    File.Delete(configFile);

                XmlSerializer serializer = new XmlSerializer(typeof(uSyncCoreSettings));

                using (StreamWriter w = new StreamWriter(configFile))
                {
                    serializer.Serialize(w, Settings);
                }
            }
            catch(Exception ex)
            {
                LogHelper.Warn<uSyncCoreConfig>("Unable to save settings to disk: {0}", () => ex.ToString());
            }
        }

    }

    public class uSyncCoreSettings
    {

        public uSyncCoreSettings()
        {
            Mappings = new List<uSyncValueMapperSettings>();
            ContentMappings = new List<uSyncContentMapping>();
            ContentMatch = "Newer";
        }

        public string MediaStorageFolder { get; set; }
        public bool MoveMedia { get; set; }

        [DefaultValue("Newer")]
        public string ContentMatch { get; set; }


        public List<uSyncValueMapperSettings> Mappings { get; set; }
        public List<uSyncContentMapping> ContentMappings { get; set; }
    }

    public class uSyncValueMapperSettings
    {
        public uSyncValueMapperSettings()
        {
            // default value should work but doesn't?
            // http://stackoverflow.com/questions/7290618/xmlserializer-define-default-value
            IdRegex = @"\d{4,9}";
        }
        /// <summary>
        /// used to match the mappings on import
        /// </summary>
        public string DataTypeId { get; set;}

        /// <summary>
        ///  the regex to get this type of id
        /// </summary>
        [DefaultValue(@"\d{4,9}")]
        public string IdRegex { get; set; }

        public string MappingType { get; set; }

        /// <summary>
        ///  how the value is stored (so text, number, json...)
        /// </summary>
        public string ValueStorageType { get; set; }

        /// <summary>
        ///  name of the property in the value storing our id
        ///  (usally in json)
        /// </summary>
        public string PropertyName { get; set; } 

        /// <summary>
        ///  alias of the propertyValue
        /// </summary>
        public string ValueAlias { get; set; }


        /// <summary>
        ///  splitter and position, work for when your properties
        ///  are in a string seperated (e.g by a comma).
        /// </summary>
        public char PropertySplitter { get; set; }
        public int PropertyPosistion { get; set; }
    }

    /// <summary>
    ///  basic mapping for content,
    ///  we will only map content in properties with
    ///  the relevatnt datatype
    /// 
    ///  and then we will have mapping types...
    /// 
    ///  it might make sense to have a IMapper 
    ///  or something, so we can extend this?
    /// </summary>
    public class uSyncContentMapping
    {
        [XmlAttribute(AttributeName = "Alias")]
        public string EditorAlias { get; set; }

        [XmlAttribute(AttributeName = "Mapping")]
        public ContentMappingType MappingType { get; set; }

        [XmlAttribute(AttributeName = "CustomMappingType")]
        public string CustomMappingType { get; set; }

        [XmlAttribute(AttributeName = "Settings")]
        public string Settings { get; set; }

        [XmlAttribute(AttributeName = "Regex")]
        public string RegEx { get; set; }

        [XmlAttribute(AttributeName = "View")]
        public string View { get; set; }
    }

    public enum ContentMappingType
    {
        Content,
        DataType,
        DataTypeKeys,
        Custom
    }
}
