using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface ISyncSerializer<T>
    {
        SyncAttempt<XElement> Serialize(T item);
        SyncAttempt<T> DeSerialize(XElement node, bool forceUpdate);

        bool IsUpdate(XElement node);
    }
}
