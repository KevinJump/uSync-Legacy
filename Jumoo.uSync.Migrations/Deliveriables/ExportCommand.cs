using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

using Jumoo.uSync.Core;
using Umbraco.Core.Services;
using Umbraco.Core;
using System.Xml.Linq;
using System.Reflection;

namespace Jumoo.uSync.Migrations.Deliveriables
{
    public class ExportCommand
    {
        private TextReader In;
        private TextWriter Out;

        public ExportCommand(TextReader reader, TextWriter wrtier)
        {
            Out = wrtier;
            In = reader;
        }

        public async Task Process(ExportOptions options)
        {
            await Out.WriteLineAsync(
                string.Format("Exporting {0} {1} to {2}", options.Type, options.itemKey, options.fileName));
            SyncAttempt<XElement> attempt = uSync.Core.SyncAttempt<XElement>.Fail("Unknown", ChangeType.NoChange, "Not found");
                
            switch(options.Type)
            {
                case UmbracoType.ContentType:
                    attempt = ExportContentType(options.itemKey, options.fileName);
                    break;
                case UmbracoType.MediaType:
                    attempt = ExportMediaType(options.itemKey, options.fileName);
                    break;
                case UmbracoType.DataType:
                    attempt = ExportDataType(options.itemKey, options.fileName);
                    break;
                case UmbracoType.DictionaryItem:
                    attempt = ExportDictionaryItem(options.itemKey, options.fileName);
                    break;
                case UmbracoType.Language:
                    attempt = ExportLanguage(options.itemKey, options.fileName);
                    break;
                case UmbracoType.Macro:
                    attempt = ExportMacro(options.itemKey, options.fileName);
                    break;
                case UmbracoType.Template:
                    attempt = ExportTemplate(options.itemKey, options.fileName);
                    break;
            }

            if (attempt.Success)
            {
                var saveRoot = Path.Combine(
                    new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "..");

                var savePath = Path.Combine(saveRoot, options.fileName);
                if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                attempt.Item.Save(savePath);

                await Out.WriteLineAsync(string.Format("{0} exported to {1}", options.itemKey, savePath));
            }
            else
            {
                await Out.WriteLineAsync("Failed to Export " + options.itemKey + " " + attempt.Message);
            }
        }

        public SyncAttempt<XElement> ExportContentType(string alias, string file)
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var item = _contentTypeService.GetContentType(alias);
            if (item != null)
            {
                return uSyncCoreContext.Instance.ContentTypeSerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(alias, ChangeType.Export, "Item Not Found");
        }

        public SyncAttempt<XElement> ExportMediaType(string alias, string file)
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var item = _contentTypeService.GetMediaType(alias);
            if (item != null)
            {
                return uSyncCoreContext.Instance.MediaTypeSerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(alias, ChangeType.Export, "Item Not Found");
        }

        public SyncAttempt<XElement> ExportDataType(string alias, string file)
        {
            var _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            var item = _dataTypeService.GetDataTypeDefinitionByName(alias);
            if (item != null)
            {
                return uSyncCoreContext.Instance.DataTypeSerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(alias, ChangeType.Export, "Item Not Found");
        }

        public SyncAttempt<XElement> ExportDictionaryItem(string key, string file)
        {
            var _languageService = ApplicationContext.Current.Services.LocalizationService;
            var item = _languageService.GetDictionaryItemByKey(key);
            if (item != null)
            {
               return uSyncCoreContext.Instance.DictionarySerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "Item Not Found");
        }

        public SyncAttempt<XElement> ExportLanguage(string cultureCode, string file)
        {
            var _languageService = ApplicationContext.Current.Services.LocalizationService;
            var item = _languageService.GetLanguageByCultureCode(cultureCode);
            if (item != null)
            {
                return uSyncCoreContext.Instance.LanguageSerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(cultureCode, ChangeType.Export, "Item Not Found");
        }

        public SyncAttempt<XElement> ExportMacro(string alias, string file)
        {
            var _macroService = ApplicationContext.Current.Services.MacroService;
            var item = _macroService.GetByAlias(alias);
            if (item != null)
            {
                return uSyncCoreContext.Instance.MacroSerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(alias, ChangeType.Export, "Item Not Found");
        }

        public SyncAttempt<XElement> ExportTemplate(string alias, string file)
        {
            var _fileService = ApplicationContext.Current.Services.FileService;
            var item = _fileService.GetTemplate(alias);
            if (item != null)
            {
                return uSyncCoreContext.Instance.TemplateSerializer.Serialize(item);
            }
            return SyncAttempt<XElement>.Fail(alias, ChangeType.Export, "Item Not Found");
        }

    }
}