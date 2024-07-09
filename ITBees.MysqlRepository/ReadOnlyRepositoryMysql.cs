using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ITBees.Interfaces.Repository;
using ITBees.MysqlRepository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITBees.MysqlRepository
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

        public PaginatedResult<T> GetDataPaginated(Expression<Func<T, bool>> predicate,
            int page,
            int elementsPerPage,
            string sortColumn,
            SortOrder sortOrder,
            params Expression<Func<T, object>>[] includeProperties)
        {
            var query = AllIncluding(includeProperties).Where(predicate);
            query = ApplySorting(query, sortColumn, sortOrder);
            var allElementsCount = query.Count();
            var data = query.Skip((page - 1) * elementsPerPage).Take(elementsPerPage).ToList();

            return new PaginatedResult<T>()
            {
                AllElementsCount = allElementsCount,
                CurrentPage = page,
                ElementsPerPage = elementsPerPage,
                Data = data
            };
        }

        private IQueryable<T> ApplySorting(IQueryable<T> query,
            string sortColumn, SortOrder sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                return query;
            }

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = typeof(T).GetProperty(sortColumn);
            if (property == null)
            {
                throw new ArgumentException($"Property '{sortColumn}' not found on type '{typeof(T).Name}'");
            }

            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);

            string orderMethod = sortOrder == SortOrder.Descending ? "OrderByDescending" : "OrderBy";

            MethodCallExpression resultExpression = Expression.Call(
                typeof(Queryable),
                orderMethod,
                new Type[] { typeof(T), property.PropertyType },
                query.Expression,
                Expression.Quote(orderByExpression)
            );

            return query.Provider.CreateQuery<T>(resultExpression);
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