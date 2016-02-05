using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface ISyncContainerSerializer<T> : ISyncSerializer<T>
    {
        SyncAttempt<T> DeserializeContainer(XElement node);
        SyncAttempt<XElement> SerializeContainer(EntityContainer item);
    }

    public interface ISyncContainerSerializerTwoPass<T> : ISyncSerializerTwoPass<T>
    {
        SyncAttempt<T> DeserializeContainer(XElement node);
        SyncAttempt<XElement> SerializeContainer(EntityContainer item);
    }
}
