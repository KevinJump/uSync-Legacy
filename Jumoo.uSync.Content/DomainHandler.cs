using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

using Jumoo.uSync.BackOffice;
using Jumoo.uSync.BackOffice.Handlers;
using Jumoo.uSync.BackOffice.Helpers;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Extensions;

using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Content
{
    public class DomainHandler : uSyncBaseHandler<IDomain>, ISyncHandler
    {
        public string Name => "uSync: DomainHandler";
        public int Priority => uSyncConstants.Priority.DomainSettings;
        public string SyncFolder => "Domains";

        private readonly IDomainService domainService;

        public DomainHandler()
        {
            domainService = ApplicationContext.Current.Services.DomainService;
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            LogHelper.Info<DomainHandler>("Exporting Domain Settings");

            List<uSyncAction> actions = new List<uSyncAction>();

            foreach(var domain in domainService.GetAll(true))
            {
                if (domain != null)
                    actions.Add(ExportItem(domain, folder));
            }

            return actions;
        }

        private uSyncAction ExportItem(IDomain item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail("item", typeof(IDomain), "item not set");


            var attempt = uSyncCoreContext.Instance.DomainSerializer.Serialize(item);

            string filename = string.Empty;
            if (attempt.Success)
            {
                filename = uSyncIOHelper.SavePath(folder, SyncFolder, item.DomainName.ToSafeAlias());
                uSyncIOHelper.SaveNode(attempt.Item, filename);
            }

            return uSyncActionHelper<XElement>.SetAction(attempt, filename);

        }

        public override SyncAttempt<IDomain> Import(string filePath, bool force = false)
        {
            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);
            var attempt = uSyncCoreContext.Instance.DomainSerializer.DeSerialize(node, force);

            if (attempt.Success && attempt.Item != null)
            {
                uSyncBackOfficeContext.Instance.Tracker.RemoveActions(attempt.Item.DomainName, typeof(IDomain));
            }

            return attempt;
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.DomainSerializer.IsUpdate(node);
            var action = uSyncActionHelper<IDomain>.ReportAction(update, node.NameFromNode());
            if (action.Change > ChangeType.NoChange)
                action.Details = ((ISyncChangeDetail)uSyncCoreContext.Instance.DomainSerializer).GetChanges(node);

            return action;
        }

        public void RegisterEvents()
        {
            DomainService.Saved += DomainService_Saved;
            DomainService.Deleted += DomainService_Deleted;
        }

        private void DomainService_Deleted(IDomainService sender, Umbraco.Core.Events.DeleteEventArgs<IDomain> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.DeletedEntities)
            {
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, item.DomainName.ToSafeAlias());

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.DomainName, typeof(IDomain));
            }
        }

        private void DomainService_Saved(IDomainService sender, Umbraco.Core.Events.SaveEventArgs<IDomain> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.SavedEntities)
            {
                ExportItem(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                uSyncBackOfficeContext.Instance.Tracker.RemoveActions(item.DomainName, typeof(IDomain));
            }
        }
    }
}
