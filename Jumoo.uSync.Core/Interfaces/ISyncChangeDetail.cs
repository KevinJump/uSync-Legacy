using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Jumoo.uSync.Core.Helpers;
using System.Xml.Linq;

namespace Jumoo.uSync.Core
{
    public interface ISyncChangeDetail
    {
        IEnumerable<uSyncChange> GetChanges(XElement node);
    }
}
