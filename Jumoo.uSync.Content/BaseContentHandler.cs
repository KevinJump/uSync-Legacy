using Jumoo.uSync.BackOffice;
using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Content
{
    abstract public class BaseContentHandler<T>
    {
        internal IContentService _contentService;
        internal IMediaService _mediaService; 

        public BaseContentHandler()
        {
            _contentService = ApplicationContext.Current.Services.ContentService;
            _mediaService = ApplicationContext.Current.Services.MediaService;
        }

        #region BaseImport

        abstract public SyncAttempt<T> Import(string file, int parentId, bool force = false);
        virtual public void ImportSecondPass(string file, T item) {}

        public IEnumerable<uSyncAction> ImportAll(string folder, bool force)
        {
            LogHelper.Debug<Logging>("Running Content Import: {0}", () => Path.GetFileName(folder));

            Dictionary<string, T> updates = new Dictionary<string, T>();

            List<uSyncAction> actions = new List<uSyncAction>();

            string mappedFolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            actions.AddRange(ImportFolder(mappedFolder, -1, force, updates));

            if (updates.Any())
            {
                foreach(var update in updates)
                {
                    ImportSecondPass(update.Key, update.Value);
                }
            }

            return actions;
        }

        private IEnumerable<uSyncAction> ImportFolder(string folder, int parentId, bool force, Dictionary<string, T> updates)
        {
            LogHelper.Debug<ContentHandler>("Import Folder: {0} {1}", () => folder, () => parentId);
            int itemId = parentId;
            List<uSyncAction> actions = new List<uSyncAction>();

            if (Directory.Exists(folder))
            {
                foreach (string file in Directory.GetFiles(folder, "*.config"))
                {
                    var attempt = Import(file, parentId, force);
                    if (attempt.Success && attempt.Item != null)
                    {
                        updates.Add(file, attempt.Item);
                    }

                    if (attempt.Item != null)
                        itemId = ((IContentBase)attempt.Item).Id;

                    actions.Add(uSyncActionHelper<T>.SetAction(attempt, file));
                }

                foreach (var child in Directory.GetDirectories(folder))
                {
                    actions.AddRange(ImportFolder(child, itemId, force, updates));
                }
            }
            return actions;
        }
        #endregion

        #region  Base Export


        #endregion

        #region Base Report
        public IEnumerable<uSyncAction> Report(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            string mappedFolder = folder; 
            if (folder.StartsWith("~"))
                mappedFolder = Umbraco.Core.IO.IOHelper.MapPath(folder);

            if (Directory.Exists(mappedFolder))
            {
                foreach(var file in Directory.GetFiles(mappedFolder, "*.config"))
                {
                    actions.Add(ReportItem(file));
                }

                foreach(var child in Directory.GetDirectories(mappedFolder))
                {
                    actions.AddRange(Report(child));
                }

            }

            return actions;
        }

        abstract public uSyncAction ReportItem(string file);
        #endregion

    }
}
