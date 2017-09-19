using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class ContentTemplateHandler : BaseContentHandler<IContent>,
        ISyncHandler, IPickySyncHandler
    {
        public string Name => "uSync: ContentTemplateHandler";
        public int Priority => uSyncConstants.Priority.ContentTemplate;
        public string SyncFolder => "blueprints";

        private IEntityService _entityService;
        private IContentTypeService _contentTypeService;

        bool _highEnoughUmbraco;

        public ContentTemplateHandler()
            : base("blueprint")
        {

            _entityService = ApplicationContext.Current.Services.EntityService;
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;

            _highEnoughUmbraco = false;

            if (Umbraco.Core.Configuration.UmbracoVersion.Current.Major >= 7 &&
                Umbraco.Core.Configuration.UmbracoVersion.Current.Minor >= 7)
            {
                _highEnoughUmbraco = true;
            }

            LogHelper.Info<ContentTemplateHandler>("Content Handler Enabled = {0}", () => _highEnoughUmbraco);
        }

        public IEnumerable<uSyncAction> ExportAll(string folder)
        {
            if (!_highEnoughUmbraco)
                return Enumerable.Empty<uSyncAction>();

            var entities = _entityService.GetChildren(Constants.System.Root, UmbracoObjectTypes.DocumentBlueprint).ToArray();
            var contentTypeAliases = entities.Select(x => ((UmbracoEntity)x).ContentTypeAlias);
            var contentTypeIds = _contentTypeService.GetAllContentTypeIds(contentTypeAliases.ToArray()).ToArray();
            var blueprints = _contentService.GetBlueprintsForContentTypes(contentTypeIds);

            var actions = new List<uSyncAction>();

            foreach (var blueprint in blueprints)
            {
                actions.Add(Export(blueprint, folder));
            }

            return actions;

        }

        private uSyncAction Export(IContent item, string folder)
        {
            if (!_highEnoughUmbraco)
                return uSyncAction.Fail(item.Name, typeof(IContent), ChangeType.Export, "Blueprints only work on Umbraco 7.7+");

            if (item == null)
                return uSyncAction.Fail(folder, typeof(IContent), "Item not set");

            try
            {
                var attempt = uSyncCoreContext.Instance.ContentSerializer.Serialize(item);

                string filename = string.Empty;
                if (attempt.Success)
                {
                    var savePath = item.Name.ToSafeFileName();
                    filename = uSyncIOHelper.SavePath(folder, SyncFolder, savePath, "blueprint");
                    uSyncIOHelper.SaveNode(attempt.Item, filename);
                }

                return uSyncActionHelper<XElement>.SetAction(attempt, filename);
            }
            catch (Exception ex)
            {
                return uSyncAction.Fail(item.Name, typeof(IContent), ChangeType.Export, ex);
            }
        }

        public override SyncAttempt<IContent> Import(string filePath, int parentId, bool force = false)
        {
            if (!_highEnoughUmbraco)
                return SyncAttempt<IContent>.Fail(filePath, ChangeType.Import, "Blueprints only work on Umbraco 7.7+");

            if (!System.IO.File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            var node = XElement.Load(filePath);
            return uSyncCoreContext.Instance.ContentSerializer.Deserialize(node, parentId, force);
        }

        public override void ImportSecondPass(string file, IContent item)
        {
            if (!_highEnoughUmbraco)
                return;

            XElement node = XElement.Load(file);
            uSyncCoreContext.Instance.ContentSerializer.DesearlizeSecondPass(item, node);
        }

        public void RegisterEvents()
        {
            if (!_highEnoughUmbraco)
                return;

            LogHelper.Info<ContentTemplateHandler>("Registering blueprint events {0}", () => _highEnoughUmbraco);
            InitEvents();
        }

        private void InitEvents()
        {
            ContentService.SavedBlueprint += ContentService_SavedBlueprint;
            ContentService.DeletedBlueprint += ContentService_DeletedBlueprint;

        }

        private void ContentService_DeletedBlueprint(IContentService sender, Umbraco.Core.Events.DeleteEventArgs<IContent> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.DeletedEntities)
            {
                uSyncIOHelper.ArchiveRelativeFile(SyncFolder, item.Name.ToSafeFileName(), "blueprint");
            }
        }

        private void ContentService_SavedBlueprint(IContentService sender, Umbraco.Core.Events.SaveEventArgs<IContent> e)
        {
            if (uSyncEvents.Paused)
                return;

            foreach (var item in e.SavedEntities)
            {
                var attempt = Export(item, uSyncBackOfficeContext.Instance.Configuration.Settings.Folder);
                if (attempt.Success)
                {
                    NameChecker.ManageOrphanFiles(SyncFolder, item.Key, attempt.FileName);
                }
            }

        }

        public override uSyncAction ReportItem(string file)
        {
            if (!_highEnoughUmbraco)
                return uSyncActionHelper<IContent>.ReportAction(false, file, "Blueprints only work on Umbraco 7.7+");

            var node = XElement.Load(file);
            var update = uSyncCoreContext.Instance.ContentSerializer.IsUpdate(node);
            return uSyncActionHelper<IContent>.ReportAction(update, node.NameFromNode());
        }
    }
}
