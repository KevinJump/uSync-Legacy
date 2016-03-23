using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface ISyncFileHandler<T>
    {
        bool ImportFile(T item, string folder);
        bool ExportFile(T item, string folder);
    }

    public interface ISyncFileHander2<T> : ISyncFileHandler<T>
    {
        bool ImportFileValue(XElement node, T item, string folder);
    }
}
