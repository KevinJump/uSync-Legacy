using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.BackOffice
{
    public class uSyncEvents
    {

        /// <summary>
        ///  paused - when we are doing things in 
        ///  situte we can pause usync, this stops 
        ///  it from acting on events when they are
        ///  fired.
        /// </summary>
        public static bool Paused = false; 

        // public static event 
        public static event uSyncEventHandler Starting;
        public static event uSyncEventHandler Initilized;

        public static event uSyncEventHandler SavingFile;
        public static event uSyncEventHandler SavedFile;

        public static event uSyncEventHandler DeletingFile;
        public static event uSyncEventHandler DeletedFile;

        internal static void fireStarting(uSyncEventArgs e)
        {
            if (Starting != null)
                Starting(e);
        }

        internal static void fireInitilized(uSyncEventArgs e)
        {
            if (Initilized != null)
                Initilized(e);
        }

        // file based ones
        internal static void fireSaving(uSyncEventArgs e)
        {
            if (SavingFile != null)
                SavingFile(e);
        }

        internal static void fireSaved(uSyncEventArgs e)
        {
            if (SavedFile != null)
                SavedFile(e);

        }

        internal static void fireDeleting(uSyncEventArgs e)
        {
            if (DeletingFile != null)
                DeletingFile(e);
        }

        internal static void fireDeleted(uSyncEventArgs e)
        {
            if (DeletedFile != null)
                DeletedFile(e);
        }

    }

    public delegate void uSyncEventHandler(uSyncEventArgs e);

    public class uSyncEventArgs
    {
        public string fileName { get; set; }
    }


}
