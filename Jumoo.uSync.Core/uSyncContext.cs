
namespace Jumoo.uSync.Core
{
    using Helpers;
    using Jumoo.uSync.Core.Interfaces;
    using Jumoo.uSync.Core.Serializers;

    using Umbraco.Core;
    using Umbraco.Core.Models;

    public class uSyncCoreContext
    {
        private static uSyncCoreContext _instance;

        private uSyncCoreContext() { }

        public static uSyncCoreContext Instance
        {
            get { return _instance ?? (_instance = new uSyncCoreContext()); }
        }

        public ISyncSerializerTwoPass<IContentType> ContentTypeSerializer { get; private set; }
        public ISyncSerializerTwoPass<IMediaType> MediaTypeSerializer { get; private set; }

        public ISyncSerializerTwoPass<IMemberType> MemberTypeSerializer { get; private set; }

        public ISyncSerializer<ITemplate> TemplateSerializer { get; private set; }

        public ISyncSerializer<ILanguage> LanguageSerializer { get; private set; }
        public ISyncSerializer<IDictionaryItem> DictionarySerializer { get; private set; }

        public ISyncSerializer<IMacro> MacroSerializer { get; private set; }
        public ISyncSerializerTwoPass<IDataTypeDefinition> DataTypeSerializer { get; private set; }

        public ISyncSerializerWithParent<IContent> ContentSerializer { get; private set; }
        public ISyncSerializerWithParent<IMedia> MediaSerializer { get; private set; }

        public ISyncFileHandler<IMedia> MediaFileMover { get; private set; }

        public uSyncCoreConfig Configuration { get; set; }

        public void Init()
        {
            Configuration = new uSyncCoreConfig();

            ContentTypeSerializer = new ContentTypeSerializer(Constants.Packaging.DocumentTypeNodeName);
            MediaTypeSerializer = new MediaTypeSerializer("MediaType");

            MemberTypeSerializer = new MemberTypeSerializer("MemberType");

            TemplateSerializer = new TemplateSerializer(Constants.Packaging.TemplateNodeName);

            LanguageSerializer = new LanguageSerializer("Language");
            DictionarySerializer = new DictionarySerializer(Constants.Packaging.DictionaryItemNodeName);

            MacroSerializer = new MacroSerializer(Constants.Packaging.MacroNodeName);

            DataTypeSerializer = new DataTypeSerializer(Constants.Packaging.DataTypeNodeName);

            ContentSerializer = new ContentSerializer();
            MediaSerializer = new MediaSerializer();

            MediaFileMover = new uSyncMediaFileMover();
        }

        public string Version
        {
            get
            {
                return typeof(Jumoo.uSync.Core.uSyncCoreContext)
                  .Assembly.GetName().Version.ToString();
            }
        }

    }
}
