
namespace Jumoo.uSync.Core
{
    using Helpers;
    using Jumoo.uSync.Core.Interfaces;
    using Jumoo.uSync.Core.Serializers;
    using System.Collections.Generic;
    using Umbraco.Core;
    using Umbraco.Core.Logging;
    using Umbraco.Core.Models;
    using System.Linq;
    using System;
    using System.Diagnostics;

    public class uSyncCoreContext
    {
        private static uSyncCoreContext _instance;

        private uSyncCoreContext() { }

        public static uSyncCoreContext Instance
        {
            get { return _instance ?? (_instance = new uSyncCoreContext()); }
        }

        public Dictionary<string, ISyncSerializerBase> Serailizers;

        public ISyncContainerSerializerTwoPass<IContentType> ContentTypeSerializer { get; private set; }
        public ISyncContainerSerializerTwoPass<IMediaType> MediaTypeSerializer { get; private set; }

        public ISyncSerializerTwoPass<IMemberType> MemberTypeSerializer { get; private set; }

        public ISyncSerializer<ITemplate> TemplateSerializer { get; private set; }

        public ISyncSerializer<ILanguage> LanguageSerializer { get; private set; }
        public ISyncSerializer<IDictionaryItem> DictionarySerializer { get; private set; }

        public ISyncSerializer<IMacro> MacroSerializer { get; private set; }
        public ISyncContainerSerializerTwoPass<IDataTypeDefinition> DataTypeSerializer { get; private set; }

        public ISyncSerializerWithParent<IContent> ContentSerializer { get; private set; }
        public ISyncSerializerWithParent<IMedia> MediaSerializer { get; private set; }

        public ISyncFileHander2<IMedia> MediaFileMover { get; private set; }

        public uSyncCoreConfig Configuration { get; set; }

        public void Init()
        {
            Configuration = new uSyncCoreConfig();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            /*
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
            */


            LoadSerializers();

            if (Serailizers != null)
            {
                // we load the known shortcuts here. (to maintain the backwards compatability 
                if (Serailizers["ContentTypeSerializer"] is ContentTypeSerializer )
                    ContentTypeSerializer = (ContentTypeSerializer)Serailizers["ContentTypeSerializer"];

                if (Serailizers["MediaTypeSerializer"] is MediaTypeSerializer)
                    MediaTypeSerializer = (MediaTypeSerializer)Serailizers["MediaTypeSerializer"];

                if (Serailizers["MemberTypeSerializer"] is MemberTypeSerializer)
                    MemberTypeSerializer = (MemberTypeSerializer)Serailizers["MemberTypeSerializer"];

                if (Serailizers["TemplateSerializer"] is TemplateSerializer)
                    TemplateSerializer = (TemplateSerializer)Serailizers["TemplateSerializer"];

                if (Serailizers["LanguageSerializer"] is LanguageSerializer)
                    LanguageSerializer = (LanguageSerializer)Serailizers["LanguageSerializer"];

                if (Serailizers["DictionarySerializer"] is DictionarySerializer)
                    DictionarySerializer = (DictionarySerializer)Serailizers["DictionarySerializer"];

                if (Serailizers["MacroSerializer"] is MacroSerializer)
                    MacroSerializer = (MacroSerializer)Serailizers["MacroSerializer"];

                if (Serailizers["DataTypeSerializer"] is DataTypeSerializer)
                    DataTypeSerializer = (DataTypeSerializer)Serailizers["DataTypeSerializer"];

                if (Serailizers["ContentSerializer"] is ContentSerializer)
                    ContentSerializer = (ContentSerializer)Serailizers["ContentSerializer"];

                if (Serailizers["MediaSerializer"] is MediaSerializer)
                    MediaSerializer = (MediaSerializer)Serailizers["MediaSerializer"];
            }

            MediaFileMover = new uSyncMediaFileMover();

            sw.Stop();
            LogHelper.Info<uSyncCoreContext>("Loading Context ({0}ms)", () => sw.ElapsedMilliseconds);
        }


        public void LoadSerializers()
        {
            Serailizers = new Dictionary<string, ISyncSerializerBase>();

            var types = TypeFinder.FindClassesOfType<ISyncSerializerBase>();
            foreach(var type in types)
            {
                var instance = Activator.CreateInstance(type) as ISyncSerializerBase;
                LogHelper.Debug<uSyncCoreContext>("Adding Serializer: {0}", () => type.Name);
                Serailizers.Add(type.Name, instance);
            }
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
