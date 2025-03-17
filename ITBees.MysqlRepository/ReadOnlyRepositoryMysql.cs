using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ITBees.Interfaces.Repository;
using ITBees.MysqlRepository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ITBees.MysqlRepository
{
    public class ReadOnlyRepositoryMysql<T, TContext> : MysqlRepositoryBase<T, TContext>,
        IReadOnlyRepository<T, DbContext>
        where T : class
        where TContext : DbContext, new()
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

        public PaginatedResult<T> GetDataPaginated(
            Expression<Func<T, bool>> predicate,
            int page,
            int elementsPerPage,
            string sortColumn,
            SortOrder sortOrder,
            params Expression<Func<T, object>>[] includeProperties)
        {
            var query = AllIncluding(includeProperties).Where(predicate);

            query = ApplySorting(query, sortColumn, sortOrder);

            var allElementsCount = query.Count();
            var data = query
                .Skip((page - 1) * elementsPerPage)
                .Take(elementsPerPage)
                .ToList();

            return new PaginatedResult<T>()
            {
                AllElementsCount = allElementsCount,
                CurrentPage = page,
                ElementsPerPage = elementsPerPage,
                Data = data
            };
        }

        public PaginatedResult<T> GetDataPaginated(
            Expression<Func<T, bool>> predicate,
            SortOptions sortOptions,
            params Expression<Func<T, object>>[] includeProperties)
        {
            return GetDataPaginated(
                predicate,
                sortOptions.Page,
                sortOptions.ElementsPerPage,
                sortOptions.SortColumn,
                sortOptions.SortOrder,
                includeProperties);
        }

        /// <summary>
        /// Applies sorting to the given query, supporting nested properties using a dot notation (e.g. "UserAccount.SetupTime").
        /// </summary>
        private IQueryable<T> ApplySorting(IQueryable<T> query, string sortColumn, SortOrder sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
            {
                // If no sort column is provided, just return the query as-is.
                return query;
            }

            try
            {
                // Create a parameter expression: x =>
                var parameter = Expression.Parameter(typeof(T), "x");
                // Build up the property expression for e.g. "UserAccount.SetupTime"
                // by splitting on '.' and drilling down each property.
                Expression propertyExpression = parameter;

                // Split by dot to handle nested properties
                foreach (var member in sortColumn.Split('.'))
                {
                    propertyExpression = Expression.PropertyOrField(propertyExpression, member);
                }

                // Lambda: x => x.UserAccount.SetupTime (for example)
                var lambda = Expression.Lambda(propertyExpression, parameter);

                // Use "OrderBy" or "OrderByDescending" based on sortOrder
                string methodName = sortOrder == SortOrder.Descending ? "OrderByDescending" : "OrderBy";

                var resultExpression = Expression.Call(
                    typeof(Queryable),
                    methodName,
                    new Type[] { typeof(T), propertyExpression.Type },
                    query.Expression,
                    Expression.Quote(lambda)
                );

                return query.Provider.CreateQuery<T>(resultExpression);
            }
            catch (ArgumentException ex)
            {
                // This is thrown if the property is not found or doesn't exist in the chain
                throw new ArgumentException(
                    $"Property '{sortColumn}' is invalid or not found on type '{typeof(T).Name}'. Check nested properties or spelling.",
                    ex
                );
            }
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

        public IQueryable<T> GetDataQueryable(
            Expression<Func<T, bool>> predicate,
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

            return Context.Set<T>()
                .FromSqlRaw($"CALL {procedureName} ({parametersReplacement})", procedureArgument)
                .ToList();
        }

        public ICollection<T2> Sql<T2>(string sql) where T2 : class
        {
            return Context.Set<T2>().FromSqlRaw(sql).ToList();
        }
    }
}