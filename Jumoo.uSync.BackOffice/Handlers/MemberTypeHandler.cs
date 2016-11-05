using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Core;

using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Logging;

using System.IO;
using Jumoo.uSync.BackOffice.Helpers;
using System.Xml.Linq;
using Jumoo.uSync.Core.Extensions;

namespace Jumoo.uSync.BackOffice.Handlers
{
    public class MemberTypeHandler : uSyncBaseHandler<IMemberType>
    {
        public string Name { get { return "uSync: MemberTypeHandler"; } }
        public int Priority { get { return uSyncConstants.Priority.MemberTypes; } }
        public string SyncFolder { get { return "MemberType"; } }

        private IMemberTypeService _memberTypeService; 

        public MemberTypeHandler()
        {
            _memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            List<uSyncAction> actions = new List<uSyncAction>();

            foreach(var item in _memberTypeService.GetAll())
            {
                if (item != null)
                {
                    actions.Add(ExportToDisk(item, folder));
                }
            }

            return actions; 
        }

        public uSyncAction ExportToDisk(IMemberType item, string folder)
        {
            if (item == null)
                return uSyncAction.Fail(Path.GetFileName(folder), typeof(IMemberType), "Item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.MemberTypeSerializer.Serialize(item);
                var filename = string.Empty;

                if (attempt.Success)
                {
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, GetItemPath(item), "def");

                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch( Exception ex)
            {
                LogHelper.Warn<MemberTypeHandler>("Error saving membertype: {0} {1}", ()=> item.Name, ()=> ex);
                return uSyncAction.Fail(item.Name, typeof(IMemberType), ChangeType.Export);
            }
        }

        public override SyncAttempt<IMemberType> Import(string filePath, bool force = false)
        {
            if (!System.IO.File.Exists(filePath))
                throw new System.IO.FileNotFoundException();

            var node = XElement.Load(filePath);
            var attempt = uSyncCoreContext.Instance.MemberTypeSerializer.DeSerialize(node, force);
            return attempt;
        }

        public override void ImportSecondPass(string file, IMemberType item)
        {
            if (!System.IO.File.Exists(file))
                throw new System.IO.FileNotFoundException();

            var node = XElement.Load(file);

            uSyncCoreContext.Instance.MemberTypeSerializer.DesearlizeSecondPass(item, node);
        }

        public void RegisterEvents()
        {
            MemberTypeService.Saved += MemberTypeService_Saved;
            MemberTypeService.Deleted += MemberTypeService_Deleted;
        }

        private void MemberTypeService_Deleted(IMemberTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IMemberType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.DeletedEntities)
            {
                LogHelper.Info<MediaTypeHandler>("Delete: Remove usync files for {0}", () => item.Name);
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, GetItemPath(item), "def");

                uSyncBackOfficeContext.Instance.Tracker.AddAction(SyncActionType.Delete, item.Key, item.Alias, typeof(IMemberType));
            }
        }

        private void MemberTypeService_Saved(IMemberTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMemberType> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach(var item in e.SavedEntities)
            {
                LogHelper.Info<MemberTypeHandler>("Save: Saving uSync files for : {0}", () => item.Name);

                var action = ExportToDisk(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);

                if (action.Success)
                {
                    NameChecker.ManageOrphanFiles("MemberType", item.Key, action.FileName);
                }
            }
        }

        public override uSyncAction ReportItem(string file)
        {
            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.MemberTypeSerializer.IsUpdate(node);
            var action = uSyncActionHelper<IMemberType>.ReportAction(update, node.NameFromNode());
            if (action.Change > ChangeType.NoChange)
                action.Details = ((ISyncChangeDetail)uSyncCoreContext.Instance.MemberTypeSerializer).GetChanges(node);

            return action;
        }
    }
}
