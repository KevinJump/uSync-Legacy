using Jumoo.uSync.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;

namespace Jumoo.uSync.Core.Helpers
{
    public class uSyncContainerHelper
    {
        public static SyncAttempt<XElement> SerializeContainer(EntityContainer item)
        {
            return SyncAttempt<XElement>.Succeed(item.Name, null, typeof(EntityContainer), ChangeType.NoChange);

            /* NOT DOING THIS ANYMORE (ALREADY) - Containers are created on demand by their child items) */
            /*
            try
            {
                var node = new XElement("EntityFolder",
                        new XAttribute("Name", item.Name),
                        new XAttribute("Id", item.Id),
                        new XAttribute("Key", item.Key),
                        new XAttribute("ParentId", item.ParentId),
                        new XAttribute("Level", item.Level)
                   );

                return SyncAttempt<XElement>.Succeed(item.Name, node, typeof(EntityContainer), ChangeType.Export);
            }
            catch (Exception ex)
            {
                return SyncAttempt<XElement>.Fail(item.Name, typeof(EntityContainer), ChangeType.Export, "Failed to export folder: " + ex.ToString());
            }
            */
        }
    }
}
