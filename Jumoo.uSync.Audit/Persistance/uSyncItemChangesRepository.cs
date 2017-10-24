using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Jumoo.uSync.Audit.Persistance.Model;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;

namespace Jumoo.uSync.Audit.Persistance
{
    public class uSyncItemChangesRepository : uSyncAuditRepositoryBase<uSyncItemChangesDTO, uSyncItemChanges>
    {
        public uSyncItemChangesRepository(DatabaseContext dbContext, ILogger logger) 
            : base(dbContext, logger, "uSyncAudit_ChangeItems")
        {
        }

        public IEnumerable<uSyncItemChanges> GetByGroup(int id)
        {
            var sql = GetBaseQuery()
                .Where<uSyncItemChangesDTO>(x => x.ChangeGroupId == id, _sqlSyntax);

            return _dbContext.Database.Fetch<uSyncItemChangesDTO>(sql)
                .Select(x => Mapper.Map<uSyncItemChanges>(x));
        }

        public int Save(uSyncItemChanges entity, int groupId)
        {
            var dto = Mapper.Map<uSyncItemChangesDTO>(entity);
            dto.ChangeGroupId = groupId;

            using (var transaction = _dbContext.Database.GetTransaction())
            {
                _dbContext.Database.Save(dto);
                transaction.Complete();
            }

            return dto.Id;
        }
    }
}
