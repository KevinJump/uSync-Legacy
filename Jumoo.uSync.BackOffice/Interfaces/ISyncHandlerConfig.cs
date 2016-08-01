using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.BackOffice
{
    /// <summary>
    ///  interface that allows uSync Backoffice to pass in 
    ///  config details to a handler. 
    /// </summary>
    public interface ISyncHandlerConfig
    {
        void LoadHandlerConfig(IEnumerable<uSyncHandlerSetting> settings);
    }
}
 
