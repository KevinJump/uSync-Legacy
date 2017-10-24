using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Core.Helpers;

namespace Jumoo.uSync.Audit
{
    public class uSyncChangeGroup
    {
        public int Id { get; set; } // used in the db, but not always set?

        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime ChangeTime { get; set; }

        public string ItemType { get; set; }

        public List<uSyncItemChanges> ItemChanges { get; set; }

        public uSyncChangeGroup()
        {
            ItemChanges = new List<uSyncItemChanges>();
            ChangeTime = DateTime.Now;
        }

        public uSyncChangeGroup(int userId, string userName)
        {
            UserId = userId.ToString();
            UserName = userName;
            ItemChanges = new List<uSyncItemChanges>();
            ChangeTime = DateTime.Now;
        }
    }

    public class uSyncItemChanges
    {
        public string Name { get; set; }
        public List<uSyncChange> Changes { get; set; }

        public Guid Key { get; set; }

        public string Source { get; set; }

        public uSyncItemChanges()
        {
            Changes = new List<uSyncChange>();
        }

    }
}
