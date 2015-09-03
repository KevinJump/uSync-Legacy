using Jumoo.uSync.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;

namespace Jumoo.uSync.Migrations.Chauffeur
{
    public class uSyncExporter
    {
        TextReader In;
        TextWriter Out;

        public uSyncExporter(TextReader reader, TextWriter writer)
        {
            In = reader;
            Out = writer;
        }

        /// <summary>
        ///  handles export (needs type folded by filename)
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task<bool> Export(string[] args)
        {
            if (args == null || args.Length < 3)
                return false;

            var type = args[0].ToLower().Replace("-","");
            var name = args[1].ToLower();
            var file = args[2].ToLower();

            await Out.WriteLineAsync(
                string.Format("Exporting: {0} {1} {2}", type, name, file));

            var attempt = SyncAttempt<XElement>.Fail("unknown", ChangeType.Export, "Unknown type");

            switch(type)
            {
                case "contenttype":
                    attempt = ExportContentType(name);
                    break;
                case "mediatype":
                    attempt = ExportMediaType(name);
                    break;
                case "datatype":
                    attempt = ExportDataType(name);
                    break;
                case "dictionaryitem":
                case "dictionary":
                    attempt = ExportDictionaryItem(name);
                    break;
                case "language":
                    attempt = ExportLanguage(name);
                    break;
                case "macro":
                    attempt = ExportMacro(name);
                    break;
                case "template":
                    attempt = ExportTemplate(name);
                    break;
                case "membertype":
                    attempt = ExportMemberType(name);
                    break;
            }

            if (attempt.Success)
            {
                var saveRoot = Path.Combine(
                    new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName, "..\\uSync\\Export\\");

                var savePath = Path.Combine(saveRoot, file);
                if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(savePath));


                if (File.Exists(savePath))
                    File.Delete(savePath);

                attempt.Item.Save(savePath);

                await Out.WriteLineAsync(
                    string.Format("{0} exported to {1}", name, savePath));
            }
            else
            {
                await Out.WriteLineAsync("Failed to export " + name + " " + attempt.Message);
            }

            return true;
        }

        private SyncAttempt<XElement> ExportContentType(string key)
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var item = _contentTypeService.GetContentType(key);
            if (item != null)
                return uSyncCoreContext.Instance.ContentTypeSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportMediaType(string key)
        {
            var _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var item = _contentTypeService.GetMediaType(key);
            if (item != null)
                return uSyncCoreContext.Instance.MediaTypeSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportDataType(string key)
        {
            var _typeService = ApplicationContext.Current.Services.DataTypeService;
            var item = _typeService.GetDataTypeDefinitionByName(key);
            if (item != null)
                return uSyncCoreContext.Instance.DataTypeSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportDictionaryItem(string key)
        {
            var _typeService = ApplicationContext.Current.Services.LocalizationService;
            var item = _typeService.GetDictionaryItemByKey(key);
            if (item != null)
                return uSyncCoreContext.Instance.DictionarySerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportLanguage(string key)
        {
            var _typeService = ApplicationContext.Current.Services.LocalizationService;
            var item = _typeService.GetLanguageByCultureCode(key);
            if (item != null)
                return uSyncCoreContext.Instance.LanguageSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportMacro(string key)
        {
            var _typeService = ApplicationContext.Current.Services.MacroService;
            var item = _typeService.GetByAlias(key);
            if (item != null)
                return uSyncCoreContext.Instance.MacroSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportTemplate(string key)
        {
            var _typeService = ApplicationContext.Current.Services.FileService;
            var item = _typeService.GetTemplate(key);
            if (item != null)
                return uSyncCoreContext.Instance.TemplateSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }

        private SyncAttempt<XElement> ExportMemberType(string key)
        {
            var _typeService = ApplicationContext.Current.Services.MemberTypeService;
            var item = _typeService.Get(key);
            if (item != null)
                return uSyncCoreContext.Instance.MemberTypeSerializer.Serialize(item);

            return SyncAttempt<XElement>.Fail(key, ChangeType.Export, "item not found");
        }
    }
}
