using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.Core;

namespace Jumoo.uSync.BackOffice
{
    public interface ISyncHandler
    {
        int Priority { get; }
        string Name { get; }
        string SyncFolder { get; }

        void RegisterEvents();

        IEnumerable<uSyncAction> ImportAll(string folder, bool force);
        IEnumerable<uSyncAction> ExportAll(string folder);

        IEnumerable<uSyncAction> Report(string folder);
    }
}
