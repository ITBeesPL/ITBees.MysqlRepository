using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;

namespace ITBees.MysqlRepository
{
    public class MysqlRepositoryBase<T, TContext> where T : class where TContext : DbContext, new()
    {
        protected TContext Context;

        public MysqlRepositoryBase(DbContextOptions<TContext> options)
        {
            var connectionString = options.Extensions.OfType<MySqlOptionsExtension>().First().ConnectionString;
            Context = new MysqlContextFactory<TContext>().CreateDbContext(new[] { connectionString });
        }

        protected IQueryable<T> InternalGetData(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = Context.Set<T>();

            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query;
        }
    }
}