using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ITBees.MysqlRepository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITBees.Repository
{
    
    public class ReadOnlyRepositoryMysql<T, TContext> : MysqlRepositoryBase<T, TContext>, IReadOnlyRepository<T, DbContext> where T : class where TContext : DbContext, new()
    {
        public ReadOnlyRepositoryMysql(DbContextOptions<TContext> options) : base(options)
        {
        }

        public bool HasData(Expression<Func<T, bool>> predicate)
        {
            return Context.Set<T>().Any(predicate);
        }

        public T GetFirst(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = Context.Set<T>().Where(predicate);
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

            return query.Any() ? query.First() : null;
        }

        public ICollection<T> GetData(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
        {
            return AllIncluding(includeProperties).Where(predicate).ToList();
        }

        public int GetDataCount(Expression<Func<T, bool>> predicate)
        {
            var query = Context.Set<T>().Count(predicate);
            return query;
        }

        public ICollection<T> GetDataFromSQL(string sql)
        {
            return Context.Set<T>().FromSqlRaw(sql).ToList();
        }

        public IQueryable<T> GetDataQueryable(Expression<Func<T, bool>> predicate,
            params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = Context.Set<T>().Where(predicate);
            query = includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));

            return query;
        }

        private IQueryable<T> AllIncluding(params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> query = Context.Set<T>();
            return includeProperties.Aggregate(query, (current, includeProperty) => current.Include(includeProperty));
        }

        public IQueryable<T> GetDataQueryable(Expression<Func<T, bool>> predicate)
        {
            var dataQueryable = Context.Set<T>().Where(predicate);
            return dataQueryable;
        }

        public ICollection<T> GetDataFromStoredProcedure(string procedureName, params object[] procedureArgument)
        {
            var parametersReplacement = string.Empty;
            var i = 0;
            foreach (var o in procedureArgument)
            {
                parametersReplacement += $"@p{i}";
                if (i != procedureArgument.Length - 1)
                {
                    parametersReplacement += ",";
                }

                i++;
            }

            return Context.Set<T>().FromSqlRaw($"CALL {procedureName} ({parametersReplacement})", procedureArgument).ToList();
        }

        public ICollection<T2> Sql<T2>(string sql) where T2 : class
        {
            return Context.Set<T2>().FromSqlRaw(sql).ToList();
        }
    }
}