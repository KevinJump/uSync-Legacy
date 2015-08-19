using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface ISyncSerializerWithParent<T> : ISyncSerializer<T>
    {
        SyncAttempt<T> Deserialize(XElement node, int parentId, bool forceUpdate);
    }
}
