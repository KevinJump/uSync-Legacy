namespace Jumoo.uSync.BackOffice.Handlers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;

    using Jumoo.uSync.Core;

    using Umbraco.Core.Logging;
    using Umbraco.Core.Models.EntityBase;

    abstract public class uSyncBaseHandler<T>
    {
        abstract public SyncAttempt<T> Import(string filePath, bool force = false);

        public IEnumerable<uSyncAction> ImportAll(string folder, bool force)
        {
            LogHelper.Debug<uSyncApplicationEventHandler>("Running Import: {0}", () => folder);
            Dictionary<string, T> updates = new Dictionary<string, T>();

            List<uSyncAction> actions = new List<uSyncAction>();

            string mappedfolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            if (Directory.Exists(mappedfolder))
            {
                foreach (string file in Directory.GetFiles(mappedfolder, "*.config"))
                {
                    var attempt = Import(file, force);
                    if (attempt.Success && attempt.Item != null)
                    {
                        updates.Add(file, attempt.Item);
                    }

                    actions.Add(uSyncActionHelper<T>.SetAction(attempt, file));
                }

                foreach (var children in Directory.GetDirectories(mappedfolder))
                {
                    actions.AddRange(ImportAll(children, force));
                }
            }

            if (updates.Any())
            {
                foreach (var update in updates)
                {
                    ImportSecondPass(update.Key, update.Value);
                }
            }

            return actions; 
        }


        virtual public string GetItemPath(T item)
        {
            return ((IUmbracoEntity)item).Name;
        }

        /// <summary>
        ///  second pass placeholder, some things require a second pass
        ///  (doctypes for structures to be in place)
        /// 
        ///  they just override this function to do their thing.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="item"></param>
        virtual public void ImportSecondPass(string file, T item)
        {

        }
    }
}
