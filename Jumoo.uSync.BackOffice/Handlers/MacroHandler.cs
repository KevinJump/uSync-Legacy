

namespace Jumoo.uSync.BackOffice.Handlers
{
    using System;
    using System.IO;
    using System.Xml.Linq;

    using Umbraco.Core;
    using Umbraco.Core.Models;
    using Umbraco.Core.Services;
    using Umbraco.Core.Logging;

    using Jumoo.uSync.Core;
    using Jumoo.uSync.BackOffice.Helpers;
    using System.Collections.Generic;

    public class MacroHandler : uSyncBaseHandler<IMacro>, ISyncHandler
    {
        public string Name { get { return "uSync: MacroHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.Macros; } }
        public string SyncFolder { get { return Constants.Packaging.MacroNodeName; } }

        public override SyncAttempt<IMacro> Import(string filePath, bool force = false)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);

            return uSyncCoreContext.Instance.MacroSerializer.DeSerialize(node, force);
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            var _macroService = ApplicationContext.Current.Services.MacroService;
            foreach (var item in _macroService.GetAll())
            {
                if (item != null)
                    actions.Add(ExportToDisk(item, folder));
            }
            return actions;
        }

        public uSyncAction ExportToDisk(IMacro item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IMacro), "item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.MacroSerializer.Serialize(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, item.Alias.ToSafeAlias());
                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }
                return uSyncActionHelper<XElement>.SetAction(attempt, filename);

            }
            catch (Exception ex)
            {
                return uSyncAction.Fail(item.Name, item.GetType(), ChangeType.Export, ex);

            }
        }

        public void RegisterEvents()
        {
            MacroService.Saved += MacroService_Saved;
            MacroService.Deleted += MacroService_Deleted;
        }

        private void MacroService_Deleted(IMacroService sender, Umbraco.Core.Events.DeleteEventArgs<IMacro> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                LogHelper.Info<MacroHandler>("Delete: Deleting uSync File for item: {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, item.Alias.ToSafeAlias());
            }
        }

        private void MacroService_Saved(IMacroService sender, Umbraco.Core.Events.SaveEventArgs<IMacro> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                LogHelper.Info<MacroHandler>("Save: Saving uSync file for item: {0}", () => item.Name);
                ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
            }
        }

    }
}
