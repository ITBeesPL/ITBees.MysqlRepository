using ITBees.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace ITBees.MysqlRepository.Interfaces
{
    public interface IReadOnlyRepository<T, TContext> : IReadOnlyRepository<T> where TContext : DbContext
    {
    }
}