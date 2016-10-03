using Microsoft.Web.XmlTransform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using umbraco.cms.businesslogic.packager.standardPackageActions;
using umbraco.interfaces;

using Umbraco.Core.IO;

namespace Jumoo.uSync.ContentMappers
{
    public class PackageActions
    {
        public class TransformConfig : IPackageAction
        {
            public string Alias()
            {
                return "UCM.TransformConfig";
            }

            private bool Transform(string packageName, System.Xml.XmlNode xmlData, bool uninstall = false)
            {
                //The config file we want to modify
                var file = xmlData.Attributes.GetNamedItem("file").Value;

                string sourceDocFileName = IOHelper.MapPath(file);

                //The xdt file used for tranformation 
                //var xdtfile = xmlData.Attributes.GetNamedItem("xdtfile").Value;
                var fileEnd = "install.xdt";
                if (uninstall)
                {
                    fileEnd = string.Format("un{0}", fileEnd);
                }

                var xdtfile = string.Format("{0}.{1}", xmlData.Attributes.GetNamedItem("xdtfile").Value, fileEnd);
                string xdtFileName = IOHelper.MapPath(xdtfile);

                // The translation at-hand
                using (var xmlDoc = new XmlTransformableDocument())
                {
                    xmlDoc.PreserveWhitespace = true;
                    xmlDoc.Load(sourceDocFileName);

                    using (var xmlTrans = new XmlTransformation(xdtFileName))
                    {
                        if (xmlTrans.Apply(xmlDoc))
                        {
                            // If we made it here, sourceDoc now has transDoc's changes
                            // applied. So, we're going to save the final result off to
                            // destDoc.
                            xmlDoc.Save(sourceDocFileName);
                        }
                    }
                }
                return true;
            }

            public bool Execute(string packageName, System.Xml.XmlNode xmlData)
            {
                return Transform(packageName, xmlData);
            }

            public System.Xml.XmlNode SampleXml()
            {
                string str = "<Action runat=\"install\" undo=\"true\" alias=\"UCM.TransformConfig\" file=\"~/web.config\" xdtfile=\"~/app_plugins/demo/web.config\">" +
                         "</Action>";
                return helper.parseStringToXmlNode(str);
            }

            public bool Undo(string packageName, System.Xml.XmlNode xmlData)
            {
                return Transform(packageName, xmlData, true);
            }
        }
    }
}

