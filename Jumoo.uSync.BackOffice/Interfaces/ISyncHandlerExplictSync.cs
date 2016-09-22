using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.BackOffice
{
    /// <summary>
    ///  explicit sync handlers will also remove anything that is 
    ///  in umbraco but isn't on the disk in the umbraco files. 
    ///  
    ///  Explicit syncs will not be on by default, but they will
    ///  be something you can fire, on demand - in theory this 
    ///  makes branch swapping in source control something that
    ///  is doable. 
    /// </summary>
    public interface ISyncHandlerExplictSync
    {
        IEnumerable<uSyncAction> RemoveOrphanItems(string folder, bool report);
    }
}
