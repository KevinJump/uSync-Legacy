using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Core.Interfaces
{
    public interface ISyncFileHandler<T>
    {
        bool ImportFile(T item, string folder);
        bool ExportFile(T item, string folder);
    }
}
