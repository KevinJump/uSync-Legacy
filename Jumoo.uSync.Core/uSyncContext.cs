
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
    using Mappers;

    public class uSyncCoreContext
    {
        private static uSyncCoreContext _instance;

        private uSyncCoreContext() { }

        public static uSyncCoreContext Instance
        {
            get { return _instance ?? (_instance = new uSyncCoreContext()); }
        }

        public Dictionary<string, ISyncSerializerBase> Serailizers;

        public Dictionary<string, IContentMapper> Mappers;

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

        public ISyncSerializer<IDomain> DomainSerializer { get; private set; }

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

            LogHelper.Debug<uSyncCoreContext>("Initializing uSync.Core: [{0}]", ()=> this.Version);

            LoadSerializers();

            if (Serailizers != null)
            {
                // we load the known shortcuts here. (to maintain the backwards compatability 
                if (Serailizers[uSyncConstants.Serailization.ContentType] is ContentTypeSerializer )
                    ContentTypeSerializer = (ContentTypeSerializer)Serailizers[uSyncConstants.Serailization.ContentType];

                if (Serailizers[uSyncConstants.Serailization.MediaType] is MediaTypeSerializer)
                    MediaTypeSerializer = (MediaTypeSerializer)Serailizers[uSyncConstants.Serailization.MediaType];

                if (Serailizers[uSyncConstants.Serailization.MemberType] is MemberTypeSerializer)
                    MemberTypeSerializer = (MemberTypeSerializer)Serailizers[uSyncConstants.Serailization.MemberType];

                if (Serailizers[uSyncConstants.Serailization.Template] is TemplateSerializer)
                    TemplateSerializer = (TemplateSerializer)Serailizers[uSyncConstants.Serailization.Template];

                if (Serailizers[uSyncConstants.Serailization.Language] is LanguageSerializer)
                    LanguageSerializer = (LanguageSerializer)Serailizers[uSyncConstants.Serailization.Language];

                if (Serailizers[uSyncConstants.Serailization.Dictionary] is DictionarySerializer)
                    DictionarySerializer = (DictionarySerializer)Serailizers[uSyncConstants.Serailization.Dictionary];

                if (Serailizers[uSyncConstants.Serailization.Macro] is MacroSerializer)
                    MacroSerializer = (MacroSerializer)Serailizers[uSyncConstants.Serailization.Macro];

                if (Serailizers[uSyncConstants.Serailization.DataType] is DataTypeSerializer)
                    DataTypeSerializer = (DataTypeSerializer)Serailizers[uSyncConstants.Serailization.DataType];

                if (Serailizers[uSyncConstants.Serailization.Content] is ContentSerializer)
                    ContentSerializer = (ContentSerializer)Serailizers[uSyncConstants.Serailization.Content];

                if (Serailizers[uSyncConstants.Serailization.Media] is MediaSerializer)
                    MediaSerializer = (MediaSerializer)Serailizers[uSyncConstants.Serailization.Media];

                if (Serailizers[uSyncConstants.Serailization.Domain] is DomainSerializer)
                    DomainSerializer = (DomainSerializer)Serailizers[uSyncConstants.Serailization.Domain];
            }

            MediaFileMover = new uSyncMediaFileMover();

            LoadMappers();

            sw.Stop();
            LogHelper.Info<uSyncCoreContext>("Loading Context ({0}ms)", () => sw.ElapsedMilliseconds);
        }


        public void LoadSerializers()
        {
            Serailizers = new Dictionary<string, ISyncSerializerBase>();

            var types = TypeFinder.FindClassesOfType<ISyncSerializerBase>();
            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type) as ISyncSerializerBase;
                LogHelper.Debug<uSyncCoreContext>("Adding Serializer: {0}:{1}", ()=> instance.SerializerType, () => type.Name);

                if (!this.Serailizers.ContainsKey(instance.SerializerType))
                {
                    Serailizers.Add(instance.SerializerType, instance);
                }
                else
                {
                    // we need to see if the new serializer of the same type has a higher priority
                    // then the one we already have...
                    var currentPriority = Serailizers[instance.SerializerType].Priority;
                    LogHelper.Debug<uSyncCoreContext>("Duplicate Serializer Found: {0} comparing priorites", () => instance.SerializerType);

                    if (instance.Priority > currentPriority)
                    {
                        LogHelper.Debug<uSyncCoreContext>("Loading new Serializer for {0} {1}", () => instance.SerializerType, ()=> type.Name);
                        Serailizers.Remove(instance.SerializerType);
                        Serailizers.Add(instance.SerializerType, instance);
                    }
                }
            }
        }

        public void LoadMappers()
        {
            Mappers = new Dictionary<string, IContentMapper>();

            /*
            foreach (var mapper in Configuration.Settings.ContentMappings)
            {
                if (!Mappers.ContainsKey(mapper.EditorAlias.ToLower()))
                {
                    var mapperType = Type.GetType(mapper.CustomMappingType);
                    if (mapperType != null)
                    {
                        var instance = Activator.CreateInstance(mapperType) as IContentMapper;
                        Mappers.Add(mapper.EditorAlias.ToLower(), instance);
                    }
                }
            }
            */

            var types = TypeFinder.FindClassesOfType<IContentMapper2>();
            if (types != null && types.Any())
            {
                foreach (var type in types)
                {
                    var instance = Activator.CreateInstance(type) as IContentMapper2;
                    foreach (var alias in instance.PropertyEditorAliases)
                    {
                        if (!Mappers.ContainsKey(alias.ToLower()))
                        {
                            Mappers.Add(alias.ToLower(), instance);
                        }
                        else
                        {
                            LogHelper.Warn<uSyncCoreContext>("Multiple Mappers Found for : {0}", () => alias);
                        }
                    }
                }
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
