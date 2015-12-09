using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.BackOffice.Licence
{
    /// <summary>
    ///  goodwill licence checker, uSync doesn't have a licence that
    ///  restricts its use in anyway. but a goodwill licence will
    ///  make everyone fill good.
    /// </summary>
    public class GoodwillLicence
    {
        public GoodwillLicence() { }

        public bool IsLicenced()
        {
            var licencefile = IOHelper.MapPath(Path.Combine(SystemDirectories.Config, "usynclicence.config"));

            LogHelper.Debug<GoodwillLicence>("Checking : {0} for licence", () => licencefile);

            if (!System.IO.File.Exists(licencefile))
                return false;

            LogHelper.Debug<GoodwillLicence>("Loading licence file");
            var lic = XElement.Load(licencefile);
            if (lic == null)
                return false;

            var name = lic.Element("name").ValueOrDefault(string.Empty);
            var licenceHash = lic.Element("key").ValueOrDefault(string.Empty);

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(licenceHash))
                return false;

            var preHash = string.Format("usync3_yesitseasytohackthis_{0}_{1}", name,
                "but think about it - its not like we are stopping you from using it");

            string hash = "";
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(preHash);
            using (var md5 = MD5.Create())
            {
                hash = BitConverter.ToString(md5.ComputeHash(inputBytes)).Replace("-", "").ToLower();
            }
            LogHelper.Debug<GoodwillLicence>("Licence : [{0}] [{1}]", () => hash, () => licenceHash);

            return hash.Equals(licenceHash);

        }

    }
}
