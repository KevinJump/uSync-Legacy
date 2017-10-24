using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Audit.EventHandlers
{
    public class HandlerLoader
    {
        public List<ISyncAuditHandler> LoadHandlers(ApplicationContext appContext)
        {
            List<ISyncAuditHandler> handlers = new List<ISyncAuditHandler>(); 

            var types = TypeFinder.FindClassesOfType<ISyncAuditHandler>();
            foreach(var type in types)
            {
                var instance = Activator.CreateInstance(type, appContext)
                    as ISyncAuditHandler;

                if (instance != null && instance.Activate())
                {
                    appContext.ProfilingLogger.Logger.Info<HandlerLoader>("Loaded: {0}", () => instance.ToString());
                    handlers.Add(instance);
                }
            }

            return handlers;
        }
    }
}
