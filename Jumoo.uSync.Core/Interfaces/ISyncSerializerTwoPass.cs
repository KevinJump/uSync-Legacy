using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface ISyncSerializerTwoPass<T> : ISyncSerializer<T>
    {
        SyncAttempt<T> DeSerialize(XElement node, bool forceUpdate, bool onePass);
        SyncAttempt<T> DesearlizeSecondPass(T item, XElement node);
    }
}
