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

            // Pinned server version instead of ServerVersion.AutoDetect(...) — AutoDetect opens a
            // connection on every context creation, which throws (and bubbles up as an unhandled 500)
            // whenever MySQL is briefly unavailable, e.g. during an automatic package upgrade/restart.
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 45));

#if DEBUG
            builderOptions.UseMySql(connectionString, serverVersion, x => x.CommandTimeout(300).EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null)).LogTo(Console.WriteLine, LogLevel.Information);
#else
                builderOptions.UseMySql(connectionString, serverVersion, x => x.CommandTimeout(300).EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null));
#endif

            builderOptions.EnableSensitiveDataLogging(true);
            return (TContext)Activator.CreateInstance(
                typeof(TContext),
                BindingFlags.Instance | BindingFlags.Public,
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