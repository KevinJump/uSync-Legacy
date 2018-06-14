using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Complete.Models
{
    public interface IUsyncCompleteTab
    {
        string Name { get; }
        string View { get; }
        int sortOrder { get; }
    }
}
