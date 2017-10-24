using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Jumoo.uSync.Core.Helpers;
using System.Xml.Serialization;
using System.Xml;

namespace Jumoo.uSync.Audit.Persistance.Mappers
{
    // Give it a list of usyncChanges, and it gives you some XML of that. 
    internal class uSyncChangeListResolver : ValueResolver<List<uSyncChange>, string>
    {
        protected override string ResolveCore(List<uSyncChange> source)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<uSyncChange>));

            using(var stringWriter = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(stringWriter))
                {
                    serializer.Serialize(writer, source);
                    return stringWriter.ToString();
                }
            }
        }
    }

    internal class uSyncChangeListFromStringResolver : ValueResolver<string, List<uSyncChange>>
    {
        protected override List<uSyncChange> ResolveCore(string source)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<uSyncChange>));
            using (TextReader reader = new StringReader(source))
            {
                return (List<uSyncChange>)serializer.Deserialize(reader);
            }

        }
    }
}
