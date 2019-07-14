using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Events;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.EntityBase;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.UnitOfWork;
using Umbraco.Core.Services;

namespace Jumoo.uSync.Core
{
    /// <summary>
    ///  Series of extensions methods to work around the change in interface for EntityService
    ///  in Umbraco 7.15.0 - we can't safely call Get(int id) or Get(Guid key) because things 
    ///  changed. but other alternatives in the service need to know types before they will 
    ///  do anything.
    /// </summary>
    public static class EntityServicePatchExtensions
    {    
        /// <summary>
        ///  gets you the Key (Guid) for an entity if all you know is the Id (int)
        /// </summary>
        public static Attempt<Guid> uSyncGetKeyForId(this IEntityService entityService, int id)
        {
            try
            {
                if (entityService.Exists(id))
                {
                    var type = entityService.GetObjectType(id);
                    if (type != UmbracoObjectTypes.Unknown)
                    {
                        return entityService.GetKeyForId(id, type);
                    }
                }
            }
            catch (Exception ex)
            {
                // it shouldn't but we might fire a ObjectNotSet exception if the type is missing
                // (but we do a check, so that is very unlikely)
                return Attempt.Fail(Guid.Empty, ex);
            }


            return Attempt.Fail(Guid.Empty);
        }

  
    }
}
