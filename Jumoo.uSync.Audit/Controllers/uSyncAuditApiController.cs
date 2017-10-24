using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Jumoo.uSync.Audit.Persistance;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

using Umbraco.Core.Logging;

namespace Jumoo.uSync.Audit.Controllers
{
    [PluginController("uSync")]
    public class uSyncAuditApiController : UmbracoAuthorizedApiController
    {
        private readonly uSyncAuditService _auditService;
        public uSyncAuditApiController()
        {
            _auditService = new uSyncAuditService(
                ApplicationContext.DatabaseContext,
                ApplicationContext.ProfilingLogger.Logger);
        }

        [HttpGet]
        public uSyncAuditPagedResults<uSyncChangeGroup> GetChanges(int page)
        {
            var changes = _auditService.GetAllChangeGroups(page, 15);
            var items = changes.Items.ToList();

            foreach(var change in items)
            {
                change.ItemChanges = _auditService.GetChangesByGroup(change.Id).ToList();
            }

            changes.Items = items;

            return changes;
        }

        [HttpGet]
        public IEnumerable<uSyncItemChanges> GetItems(int id)
        {
            return _auditService.GetChangesByGroup(id);
        }
    }
}
