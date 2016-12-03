using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jumoo.uSync.BackOffice.Handlers.Deploy
{
    public class uSyncDeployNode
    {
        public Guid Key { get; set; }
        public Guid? Master { get; set; }

        public XElement Node { get; set; }
    }

    public class uSyncDeployTreeNode
    {
        public uSyncDeployNode Node { get; set; }
        public List<uSyncDeployTreeNode> Children { get; set; }
        public uSyncDeployTreeNode()
        {
            Children = new List<uSyncDeployTreeNode>();
        }
    }
}
