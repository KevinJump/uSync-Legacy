using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.BackOffice
{
    public interface ISyncHandlerBase
    {
        int Priority { get; }
        string Name { get; }
        string SyncFolder { get; }
    }
}
