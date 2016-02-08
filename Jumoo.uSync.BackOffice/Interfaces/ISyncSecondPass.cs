using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.BackOffice
{
    public interface ISyncPostImportHandler : ISyncHandlerBase
    { 
        IEnumerable<uSyncAction> ProcessPostImport(string filepath, IEnumerable<uSyncAction> actions);      
    }
}
