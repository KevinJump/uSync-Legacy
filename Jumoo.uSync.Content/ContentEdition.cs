using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.BackOffice.Controllers;

namespace Jumoo.uSync.Content
{
    public class ContentEdition : IuSyncAddOn
    {
        public string GetVersionInfo()
        {
            return string.Format("uSync.Content: {0}", typeof(Jumoo.uSync.Content.ContentEdition)
              .Assembly.GetName().Version.ToString());
        }
    }
}
