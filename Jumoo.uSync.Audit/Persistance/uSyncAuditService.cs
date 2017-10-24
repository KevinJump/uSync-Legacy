using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Logging;

namespace Jumoo.uSync.Audit.Persistance
{
    public class uSyncAuditService
    {
        private readonly uSyncChangeGroupRepository _groupRepo;
        private readonly uSyncItemChangesRepository _itemRepo;

        public uSyncAuditService( 
            DatabaseContext dbContext,
            ILogger logger
            )
        {
            _groupRepo = new uSyncChangeGroupRepository(dbContext, logger);
            _itemRepo = new uSyncItemChangesRepository(dbContext, logger);
        }

        public IEnumerable<uSyncChangeGroup> GetAllChangeGroups(params int[] ids)
        {
            return _groupRepo.GetAll(ids);
        }

        public uSyncAuditPagedResults<uSyncChangeGroup> GetAllChangeGroups(int page, int pageSize, params int[] ids)
        {
            return _groupRepo.GetAll(page, pageSize, ids);
        }

        public uSyncChangeGroup GetGroup(int id)
        {
            return _groupRepo.Get(id);
        }

        public IEnumerable<uSyncItemChanges> GetAllChanges(params int[] ids)
        {
            return _itemRepo.GetAll(ids);
        }

        public uSyncItemChanges GetChange(int id)
        {
            return _itemRepo.Get(id);
        }

        public IEnumerable<uSyncItemChanges> GetChangesByGroup(int id)
        {
            return _itemRepo.GetByGroup(id);
        }

        public void SaveChangeGroup(uSyncChangeGroup group)
        {
            var groupDto = _groupRepo.Save(group);

            foreach(var change in group.ItemChanges)
            {
                _itemRepo.Save(change, groupDto.Id);
            }
        }
    }
}
