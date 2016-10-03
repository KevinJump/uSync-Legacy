using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jumoo.uSync.Core
{
    public static partial class uSyncConstants
    {

        public static class Priority
        {
            public const int USYNC_RESERVED_LOWER = 1000;
            public const int USYNC_RESERVED_UPPER = 2000;

            public const int DataTypes = USYNC_RESERVED_LOWER + 10;
            public const int Templates = USYNC_RESERVED_LOWER + 20;

            public const int ContentTypes = USYNC_RESERVED_LOWER + 30;
            public const int MediaTypes = USYNC_RESERVED_LOWER + 40;
            public const int MemberTypes = USYNC_RESERVED_LOWER + 45;

            public const int Languages = USYNC_RESERVED_LOWER + 50;
            public const int DictionaryItems = USYNC_RESERVED_LOWER + 60;
            public const int Macros = USYNC_RESERVED_LOWER + 70;

            public const int Media = USYNC_RESERVED_LOWER + 200;
            public const int Content = USYNC_RESERVED_LOWER + 210;

            public const int DataTypeMappings = USYNC_RESERVED_LOWER + 220;
        }

        public static class Serailization 
        {
            /// <summary>
            ///  constants, for serailizers, anything implimenting 
            ///  Jumoo.uSync.Core.Interfaces.ISyncSerializerBase 
            ///  has to return what type of thing it serailizes. 
            ///  
            ///  these are the default values for the core serailzers
            ///  (and a couple of future/past proofing values)
            /// </summary>
            public const string ContentType = "ContentType";
            public const string MediaType = "MediaType";
            public const string DataType = "DataType";
            public const string Dictionary = "Dictionary";
            public const string Language = "Language";
            public const string Macro = "Macro";
            public const string Template = "Template";
            public const string MemberType = "MemberType";

            public const string Media = "Media";
            public const string Content = "Content";
            public const string Redirect = "Redirect";

            public const string Stylesheet = "Stylesheet";
            public const string View = "View";
            public const string Parial = "Partial";

            /// <summary>
            ///  the default priority for all serializers in the core
            ///  if you write your own serailizer and give it a higher
            ///  priority than ours, then it will run in place of the 
            ///  core serializer. 
            ///  
            ///  if you want the core serailizers to run it will be 
            ///  your responsibility to get them from the system 
            ///  and run them. 
            /// </summary>
            public const int DefaultPriority = 100;
        }

    }
}
