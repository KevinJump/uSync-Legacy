using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Complete.Models;
using Umbraco.Core;
using Umbraco.Web.WebApi;

namespace Jumoo.uSync.Complete.Complete
{
    public class UsyncCompleteApiController : UmbracoAuthorizedApiController
    {
        public bool GetApiRoot()
        {
            return true;
        }

        public IEnumerable<IUsyncCompleteTab> GetTabs()
        {
            var tabs = new List<IUsyncCompleteTab>();
            var tabTypes = TypeFinder.FindClassesOfType<IUsyncCompleteTab>();
            foreach (var t in tabTypes)
            {
                var inst = Activator.CreateInstance(t) as IUsyncCompleteTab;
                if (inst != null)
                {
                    tabs.Add(inst);
                }
            }

            return tabs;
        }

    }
}
