using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.BackOffice.Controllers;

namespace Jumoo.uSync.BackOffice.Tabs
{
    public class uSyncTabs : IuSyncTab
    {
        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab
            {
                name = "uSync",
                template = "/App_Plugins/uSync/main.html",
                sortOrder = -1                
            };
        }
    }

    public class uSyncHistoryTab : IuSyncTab
    {
        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab
            {
                name = "History",
                template = "/App_Plugins/uSync/history.html"
            };
        }
    }

    public class uSyncConfigTab: IuSyncTab
    {
        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab
            {
                name = "Config",
                template = "/App_Plugins/uSync/config.html"
            };
        }
    }
    
    public class uSyncAboutTab: IuSyncTab
    {
        public BackOfficeTab GetTabInfo()
        {
            return new BackOfficeTab
            {
                name = "About",
                template = "/App_Plugins/uSync/about.html",
                sortOrder = 100
            };
        }
    }
}
