using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jumoo.uSync.Integrity.Test
{
    [TestClass]
    public class XmlIntegrity
    {
        private static string uSyncFolder { get; set; }

        [ClassInitialize]
        public static void XmlIntegrityInitialize(TestContext context)
        {
            uSyncFolder = context.Properties["uSyncFolder"].ToString();
        }

        [TestMethod]
        public void ValidateFolder()
        {
            Assert.IsTrue(Directory.Exists(uSyncFolder),
                "uSync Folder not found - either supply uSyncFolder in runsettings file, or as parameter to test",
                uSyncFolder);
        }

        [TestMethod]
        public void ValidateSyncFolder()
        {
            var files = Directory.GetFiles(uSyncFolder, "*.config", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var node = XElement.Load(file);

                    Assert.IsNotNull(node, "XML Failed to load");
                }
                catch(Exception ex)
                {
                    Assert.Fail(
                        string.Format("XML is Invalid for file {0} at {1}", file, ex.Message));
                }
            }
        }
    }
}
