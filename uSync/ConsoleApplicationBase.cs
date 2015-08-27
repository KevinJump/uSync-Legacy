using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Core;

namespace uSync
{
    public class ConsoleApplicationBase : UmbracoApplicationBase
    {
        protected override IBootManager GetBootManager()
        {
            return new CoreBootManager(this);
        }

        public void Start(object sender, EventArgs e)
        {
            base.Application_Start(sender, e);
        }


    }
}
