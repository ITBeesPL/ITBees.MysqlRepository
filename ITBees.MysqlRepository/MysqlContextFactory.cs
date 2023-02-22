using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ITBees.MysqlRepository
{
    public class MysqlContextFactory<TContext> : IDesignTimeDbContextFactory<TContext> where TContext : DbContext, new()
    {
        public TContext CreateDbContext(string[] args)
        {
            var builderOptions = new DbContextOptionsBuilder<TContext>();
            var connectionString = string.Empty;

            connectionString = args.Length == 0 ? GetConnectionStringWhenDiContainerWasNotInitialized() : args.First();

#if DEBUG
            builderOptions.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), x => x.CommandTimeout(300)).LogTo(Console.WriteLine, LogLevel.Information);
#else
                builderOptions.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), x => x.CommandTimeout(300));
#endif

            builderOptions.EnableSensitiveDataLogging(true);
            return (TContext)Activator.CreateInstance(
                typeof(TContext),
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new object[] { builderOptions.Options },
                null);
        }

        private static string GetConnectionStringWhenDiContainerWasNotInitialized()
        {
            var basePath = AppContext.BaseDirectory;

            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();

            var config = builder.Build();

            var connstr = config["ConnectionStrings:MysqlConnectionString"];
            return connstr;
        }
    }
}