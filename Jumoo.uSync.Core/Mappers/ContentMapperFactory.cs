using System;
using System.Linq;
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
            var mapping = uSyncCoreContext.Instance.Configuration.Settings.ContentMappings
                .SingleOrDefault(x => x.EditorAlias == alias);

            if (mapping == null)
                return null;
            else
                return GetMapper(mapping);
        }
    }
}