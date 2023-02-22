using ITBees.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace ITBees.MysqlRepository.Interfaces
{
    public interface IWriteOnlyRepository<T, TContext> : IWriteOnlyRepository<T> where TContext : DbContext
    {
        void Dispose();
    }
}