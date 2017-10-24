using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Audit.EventHandlers;
using Jumoo.uSync.Audit.Persistance.Mappers;
using Jumoo.uSync.Core;
using Semver;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence.Migrations;
using Umbraco.Core.Services;
using Umbraco.Web;

namespace Jumoo.uSync.Audit
{
    public class uSyncAuditEventHandler : ApplicationEventHandler
    {
        private List<ISyncAuditHandler> _handlers; 

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            ApplyMigrations(applicationContext, "uSyncAudits", new SemVersion(1, 0, 0));

            ModelMappings mappings = new ModelMappings();
            mappings.Initialzie();

            uSyncCoreContext.Instance.Init();

            DataTypeService.Saved += DataTypeService_Saved;

            ContentTypeService.SavedContentType += ContentTypeService_SavedContentType;
            ContentTypeService.SavedMediaType += ContentTypeService_SavedMediaType;
            ContentTypeService.DeletedContentType += ContentTypeService_DeletedContentType;

            MemberTypeService.Saved += MemberTypeService_Saved;

            MacroService.Saved += MacroService_Saved;

            FileService.SavedTemplate += FileService_SavedTemplate;

            LocalizationService.SavedDictionaryItem += LocalizationService_SavedDictionaryItem;
            LocalizationService.SavedLanguage += LocalizationService_SavedLanguage;

            // load up the handlers (any thing that inhetits ISyncAuditHandler)
            HandlerLoader loader = new HandlerLoader();
            _handlers = loader.LoadHandlers(applicationContext);

        }

        private void ContentTypeService_DeletedContentType(IContentTypeService sender, Umbraco.Core.Events.DeleteEventArgs<IContentType> e)
        {
            var auditor = new uSyncComparitor<IContentType>(uSyncCoreContext.Instance.ContentTypeSerializer);
        }

        private void MemberTypeService_Saved(IMemberTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMemberType> e)
        {
            var auditor = new uSyncComparitor<IMemberType>(uSyncCoreContext.Instance.MemberTypeSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void MacroService_Saved(IMacroService sender, Umbraco.Core.Events.SaveEventArgs<IMacro> e)
        {
            var auditor = new uSyncComparitor<IMacro>(uSyncCoreContext.Instance.MacroSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void DataTypeService_Saved(IDataTypeService sender, Umbraco.Core.Events.SaveEventArgs<IDataTypeDefinition> e)
        {
            var auditor = new uSyncComparitor<IDataTypeDefinition>(uSyncCoreContext.Instance.DataTypeSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void LocalizationService_SavedLanguage(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<ILanguage> e)
        {
            var auditor = new uSyncComparitor<ILanguage>(uSyncCoreContext.Instance.LanguageSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void LocalizationService_SavedDictionaryItem(ILocalizationService sender, Umbraco.Core.Events.SaveEventArgs<IDictionaryItem> e)
        {
            var auditor = new uSyncComparitor<IDictionaryItem>(uSyncCoreContext.Instance.DictionarySerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void FileService_SavedTemplate(IFileService sender, Umbraco.Core.Events.SaveEventArgs<ITemplate> e)
        {
            var auditor = new uSyncComparitor<ITemplate>(uSyncCoreContext.Instance.TemplateSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void ContentTypeService_SavedMediaType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<IMediaType> e)
        {
            var auditor = new uSyncComparitor<IMediaType>(uSyncCoreContext.Instance.MediaTypeSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }

        private void ContentTypeService_SavedContentType(IContentTypeService sender, Umbraco.Core.Events.SaveEventArgs<Umbraco.Core.Models.IContentType> e)
        {
            var auditor = new uSyncComparitor<IContentType>(uSyncCoreContext.Instance.ContentTypeSerializer);
            auditor.ProcessChanges(e.SavedEntities);
        }




        private void ApplyMigrations(ApplicationContext applicationContext,
         string productName, SemVersion targetVersion)
        {
            var currentVersion = new SemVersion(0);

            var migrations = applicationContext.Services.MigrationEntryService.GetAll(productName);
            var latest = migrations.OrderByDescending(x => x.Version).FirstOrDefault();
            if (latest != null)
                currentVersion = latest.Version;

            applicationContext.ProfilingLogger
                .Logger.Debug<uSyncAudit>("Current: {0}, Target: {1}",
                () => currentVersion.ToString(), () => targetVersion.ToString());

            if (targetVersion == currentVersion)
                return;

            var migrationRunner = new MigrationRunner(
                applicationContext.Services.MigrationEntryService,
                applicationContext.ProfilingLogger.Logger,
                currentVersion,
                targetVersion,
                productName);

            try
            {
                migrationRunner.Execute(applicationContext.DatabaseContext.Database);
            }
            catch (Exception ex)
            {
                applicationContext.ProfilingLogger
                    .Logger.Error<uSyncAudit>("Error running migration", ex);
            }
        }

    }
}
