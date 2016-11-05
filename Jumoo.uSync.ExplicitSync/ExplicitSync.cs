using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.ExplicitSync
{
    public class ExplicitSync
    {
        /*
        /// <summary>
        ///  explicit clean, where items that are not on disk are deletes from the umbraco install. 
        ///  
        ///  This could if called wrong just wipe your umbraco install - but it's a good idea if you 
        ///  are swapping branches. 
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="folder"></param>
        /// <returns></returns>
        public IEnumerable<uSyncAction> CleanOrphans(string groupName, string folder, bool report = true)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var mappedFolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            foreach (var handler in handlers.Select(x => x.Value))
            {
                if (HandlerEnabled(handler.Name, "clean", groupName))
                {
                    if (handler is ISyncExplicitHandler)
                    {
                        var cleanHandler = (ISyncExplicitHandler)handler;

                        var syncFolder = System.IO.Path.Combine(mappedFolder, handler.SyncFolder);
                        LogHelper.Debug<uSyncApplicationEventHandler>("# Explicit Sync (deletes) Processing: {0}", () => handler.Name);
                        var cleanActions = cleanHandler.RemoveOrphanItems(syncFolder, report);
                        if (cleanActions != null)
                            actions.AddRange(cleanActions);
                    }
                }
            }
            return actions;
        }

        public IEnumerable<uSyncAction> RemoveOrphanItems(string folder, bool report)
        {

            var itemKeys = new List<Guid>();
            var itemAlias = new List<string>();

            // load all the keys from disk..
            var folderInfo = new DirectoryInfo(folder);

            var files = folderInfo.GetFiles("*.config", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                XElement node = XElement.Load(file.FullName);

                var key = node.KeyOrDefault();
                if (key != Guid.Empty && !itemKeys.Contains(key))
                    itemKeys.Add(key);

                var alias = node.NameFromNode();
                if (alias != string.Empty && !itemAlias.Contains(alias))
                    itemAlias.Add(alias);
            }

            return DeleteOrphans(itemKeys, itemAlias, report);
        }

        virtual public IEnumerable<uSyncAction> DeleteOrphans(List<Guid> itemKeys, List<string> itemAlias, bool report)
        {
            return new List<uSyncAction>();
        }
        */
    }
}
