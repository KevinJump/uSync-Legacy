using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jumoo.uSync.Core;
using Jumoo.uSync.Core.Interfaces;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.EntityBase;

namespace Jumoo.uSync.Audit.Persistance
{
    public class AuditCacheManager
    {
        ApplicationContext _appContext; 

        public AuditCacheManager(ApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Initialize()
        {
            try
            {
                var cacheFolder = Umbraco.Core.IO.IOHelper.MapPath("~/App_Data/uSync/Audit/");

                if (!Directory.Exists(cacheFolder))
                    InitializeEmptyCache();
            }
            catch(Exception ex)
            {
                _appContext.ProfilingLogger.Logger.Warn<AuditCacheManager>("Error when setting up the audit cache: {0}", () => ex.Message);
            }
        }


        /// <summary>
        ///  worker function that just saves everything - we call this is if the cache is empty 
        ///  because we can't track changes if we don't know what went before. 
        /// </summary>
        private void InitializeEmptyCache()
        {
            var contentTypes = _appContext.Services.ContentTypeService.GetAllContentTypes();
            SaveAllItems(contentTypes);

            var mediaTypes = _appContext.Services.ContentTypeService.GetAllMediaTypes();
            SaveAllItems(mediaTypes);

            var dataTypes = _appContext.Services.DataTypeService.GetAllDataTypeDefinitions();
            SaveAllItems(dataTypes);

            var templates = _appContext.Services.FileService.GetTemplates();
            SaveAllItems(templates);

            var macros = _appContext.Services.MacroService.GetAll();
            SaveAllItems(macros);

            var languages = _appContext.Services.LocalizationService.GetAllLanguages();
            SaveAllItems(languages);

            var dictionaryItems = _appContext.Services.LocalizationService.GetRootDictionaryItems();
            SaveAllItems(dictionaryItems);
        }

        public static void SaveAllItems<TItem>(IEnumerable<TItem> items) where TItem : IEntity
        {

            if (uSyncCoreContext.Instance.Serailizers.Any(x => x.Value is ISyncSerializer<TItem>))
            {
                var serializer = uSyncCoreContext.Instance.Serailizers.FirstOrDefault(x => x.Value is ISyncSerializer<TItem>);
                var comparitor = new uSyncComparitor<TItem>(serializer.Value as ISyncSerializer<TItem>);
                comparitor.SaveUpdates(items);
            }

        }
    }
}
