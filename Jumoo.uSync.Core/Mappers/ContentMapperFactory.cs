using System;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Core.Mappers
{
    public class ContentMapperFactory
    {
        public static IContentMapper GetCustomMapper(string typeDefinition)
        {
            Type mapperType = Type.GetType(typeDefinition);
            if (mapperType == null)
            {
                return null;
            }

            LogHelper.Debug<ContentMapperFactory>("Custom Mapper: {0}", () => mapperType.ToString());

            return Activator.CreateInstance(mapperType) as IContentMapper;
        }

        public static IContentMapper GetMapper(uSyncContentMapping mapping)
        {
            LogHelper.Debug<ContentMapperFactory>("Mapping: {0} {1}", () => mapping.EditorAlias, ()=> mapping.MappingType);
            switch (mapping.MappingType)
            {
                case ContentMappingType.Content:
                    return new ContentIdMapper(mapping.RegEx);
                case ContentMappingType.DataType:
                    return new ContentDataTypeMapper();
                case ContentMappingType.DataTypeKeys:
                    return new ContentDataTypeKeyMapper();
                case ContentMappingType.Media:
                    return new MediaIdMapper("");
                case ContentMappingType.Custom:
                    return ContentMapperFactory.GetCustomMapper(mapping.CustomMappingType);
                default:
                    return null;
            }
        }

        public static IContentMapper GetMapper(string alias)
        {
            LogHelper.Debug<ContentMapperFactory>("Looking for {0} in loaded mappers", () => alias);

            var mapping = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings
                .SingleOrDefault(x => x.EditorAlias.InvariantEquals(alias));

            if (mapping == null)
            {
                LogHelper.Debug<ContentMapperFactory>("Looking for a dynamic mapper");

                // look for a dynamic mappings
                if (uSyncCoreContext.Instance.Mappers
                    .Any(x => x.Key.InvariantEquals(alias)))
                {
                    var mapper = uSyncCoreContext.Instance.Mappers
                        .FirstOrDefault(x => x.Key.InvariantEquals(alias));

                    LogHelper.Debug<ContentMapperFactory>("Returning Mapper (dynamic): {0}", () => mapper.Key);

                    return mapper.Value as IContentMapper;
                }

                return null;
            }
            else
            {
                return GetMapper(mapping);
            }
        }

        public static IContentMapper GetByViewName(string view)
        {
            var mapping = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings
                    .SingleOrDefault(x => !string.IsNullOrEmpty(x.View) && view.IndexOf(x.View, StringComparison.InvariantCultureIgnoreCase) > -1);

            if (mapping != null)
                return GetMapper(mapping);

            return null;
        }
    }
}