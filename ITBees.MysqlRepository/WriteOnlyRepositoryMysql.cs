using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ITBees.MysqlRepository.Interfaces;
using ITBees.Repository;
using Microsoft.EntityFrameworkCore;

namespace ITBees.MysqlRepository
{
    public class WriteOnlyRepositoryMysql<T, Tcontext> : MysqlRepositoryBase<T, Tcontext>, IWriteOnlyRepository<T, Tcontext> where T : class where Tcontext : DbContext, new()
    {
        public WriteOnlyRepositoryMysql(DbContextOptions<Tcontext> options) : base(options)
        {
        }

        public T InsertData(T entity)
        {
            var insertedEntity = Context.Add(entity);
            try
            {
                Context.SaveChanges();
            }
            catch (Exception e)
            {
                Context.ChangeTracker.Clear();
                throw e;
            }
            return insertedEntity.Entity;
        }

        public ICollection<T> InsertData(ICollection<T> includeProperty)
        {
            var insertedItems = new List<T>();

            var isContextChanged = false;

            foreach (var entity in includeProperty)
            {
                var entryToTrack = Context.Entry<T>(entity);
                entryToTrack.State = EntityState.Added;
                insertedItems.Add(entryToTrack.Entity);

                isContextChanged = true;
            }

            if (isContextChanged)
            {
                Context.SaveChanges();
            }

            return insertedItems;
        }

        public ICollection<T> UpdateData(Expression<Func<T, bool>> predicate, Action<T> updateAction, params Expression<Func<T, object>>[] includeProperties)
        {
            var changedItems = new List<T>();
            IQueryable<T> dbQuery = base.InternalGetData(predicate, includeProperties);
            var entitesToUpdate = dbQuery.Where(predicate);
            var isContextChanged = false;
            foreach (var entity in entitesToUpdate)
            {
                updateAction(entity);

                var entryToTrack = Context.Entry<T>(entity);
                entryToTrack.State = EntityState.Modified;
                changedItems.Add(entryToTrack.Entity);

                isContextChanged = true;
            }

            if (isContextChanged)
            {
                Context.SaveChanges();
            }
            return changedItems;
        }

        public int DeleteData(Expression<Func<T, bool>> predicate)
        {
            var dbSet = Context.Set<T>();
            var itemsToRemove = dbSet.Where(predicate);
            dbSet.RemoveRange(itemsToRemove);
            var deletedItemCount = Context.SaveChanges();
            return deletedItemCount;
        }

        public void DeleteData(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
        {
            var dbSet = Context.Set<T>();
            IQueryable<T> dbQuery = InternalGetData(predicate, includeProperties);
            var entitesToDelete = dbQuery.Where(predicate);
            dbSet.RemoveRange(entitesToDelete);
            Context.SaveChanges();
        }

        public void Sql(string sql)
        {
            this.Context.Database.ExecuteSqlRaw(sql);
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}