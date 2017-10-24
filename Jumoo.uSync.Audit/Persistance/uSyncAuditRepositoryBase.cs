using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Jumoo.uSync.Audit.Persistance
{
    public class uSyncAuditRepositoryBase<TDTOModel, TModel>
    {
        protected DatabaseContext _dbContext;
        protected ISqlSyntaxProvider _sqlSyntax;
        protected readonly ILogger _logger;

        protected string _tableName;

        public uSyncAuditRepositoryBase(
            DatabaseContext dbContext,
            ILogger logger,
            string tableName)
        {
            _logger = logger;
            _dbContext = dbContext;
            _sqlSyntax = dbContext.SqlSyntax;

            _tableName = tableName;
        }

        protected virtual Sql GetBaseQuery()
        {
            return new Sql().Select("*")
                .From<TDTOModel>(_sqlSyntax);
        }

        protected virtual string GetBaseWhereClause()
        {
            return $"{_tableName}.Id = @Id";
        }

        public virtual TModel Get(int id)
        {
            var sql = GetBaseQuery()
                .Where(GetBaseWhereClause(), new { Id = id });

            var dto = _dbContext.Database.Fetch<TDTOModel>(sql)
                            .FirstOrDefault();

            if (dto == null)
                return default(TModel);

            return Mapper.Map<TModel>(dto);
        }
        public virtual IEnumerable<TModel> GetAll(params int[] ids)
        {
            var sql = GetBaseQuery()
                .Where($"{_tableName}.Id > 0");

            if (ids.Any())
            {
                sql.Where($"{_tableName}.Id in (@Ids)", new { Ids = ids });
            }

            return _dbContext.Database.Fetch<TDTOModel>(sql)
                .Select(x => Mapper.Map<TModel>(x));
        }

        public uSyncAuditPagedResults<TModel> GetAll(int page, int pageSize, params int[] ids)
        {
            var sql = GetBaseQuery()
                .Where($"{_tableName}.Id > 0");

            if (ids.Any())
            {
                sql.Where($"{_tableName}.Id in (@Ids)", new { Ids = ids });
            }

            sql.OrderBy("Id desc");

            var paged = _dbContext.Database.Page<TDTOModel>(page, pageSize, sql);

            var pagedResults = new uSyncAuditPagedResults<TModel>
            {
                CurrentPage = paged.CurrentPage,
                ItemsPerPage = paged.ItemsPerPage,
                TotalItems = paged.TotalItems,
                TotalPages = paged.TotalPages,
                Items = paged.Items.Select(x => Mapper.Map<TModel>(x))
            };

            return pagedResults;
        }

        public virtual TDTOModel Save(TModel entity)
        {
            var dto = Mapper.Map<TDTOModel>(entity);

            using (var transaction = _dbContext.Database.GetTransaction())
            {
                _dbContext.Database.Save(dto);
                transaction.Complete();
            }

            return dto;
        }

        public virtual void Delete(int id)
        {
            using (var transaction = _dbContext.Database.GetTransaction())
            {
                _dbContext.Database.Delete<TDTOModel>(id);
                transaction.Complete();
            }
        }
    }

    public class uSyncAuditPagedResults<TModel>
    {
        public long CurrentPage { get; set; }
        public long ItemsPerPage { get; set; }
        public long TotalItems { get; set; }
        public long TotalPages { get; set; }
        public IEnumerable<TModel> Items { get; set; }
    }


}
