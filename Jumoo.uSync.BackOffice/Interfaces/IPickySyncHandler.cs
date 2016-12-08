using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.BackOffice
{
    /// <summary>
    ///  A pickly sync handler will only load when usync really wants it to 
    ///  (so when includeIfMissing) is true. 
    ///  
    ///  There is nothing to impliment, you just have to inherit
    ///  the type to be picky. 
    /// </summary>
    public interface IPickySyncHandler
    {
    }
}
