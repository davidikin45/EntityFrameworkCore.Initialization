using Database.Initialization;
using EntityFrameworkCore.Initialization.NoSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace EntityFrameworkCore.Initialization
{
    public static class DbContextServiceCollectionExtensions
    {
        public static IServiceCollection AddDbContextNoSql<TContext>(this IServiceCollection services, string connectionString, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContextNoSql
        {
            if (ConnectionStringHelper.IsLiteDbInMemory(connectionString))
            {
                contextLifetime = ServiceLifetime.Singleton;
            }

            if (ConnectionStringHelper.IsLiteDbInMemory(connectionString))
            {
                services.AddDbContextNoSqlInMemory<TContext>(contextLifetime);
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(TContext), sp => ActivatorUtilities.CreateInstance(sp, typeof(TContext), new object[] { connectionString }), contextLifetime));
            }
            return services;
        }

        public static IServiceCollection AddDbContextNoSqlInMemory<TContext>(this IServiceCollection services, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContextNoSql
        {
            services.Add(new ServiceDescriptor(typeof(TContext), sp => ActivatorUtilities.CreateInstance(sp, typeof(TContext), new object[] { new MemoryStream() }), contextLifetime));
            return services;
        }

        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, string connectionString, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            if (ConnectionStringHelper.IsSQLiteInMemory(connectionString))
            {
                contextLifetime = ServiceLifetime.Singleton;
            }

            return services.AddDbContext<TContext>(options =>
            {
                options.SetConnectionString<TContext>(connectionString);
                options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }, contextLifetime);
        }

        public static DbContextOptionsBuilder SetConnectionString<TContext>(this DbContextOptionsBuilder options, string connectionString, string migrationsAssembly = "")
            where TContext : DbContext
        {
            if (connectionString == null)
            {
                return options;
            }
            else if (connectionString == string.Empty)
            {
                return options.UseInMemoryDatabase(typeof(TContext).FullName);
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                {
                    return options.UseSqlite(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        sqlOptions.UseNetTopologySuite();
                    });
                }
                return options.UseSqlite(connectionString, sqlOptions =>
                {
                    sqlOptions.UseNetTopologySuite();
                });
            }

            else if (ConnectionStringHelper.IsCosmos(connectionString))
            {
                var dbConnectionString = new CosmosDBConnectionString(connectionString);
                return options.UseCosmos(dbConnectionString.ServiceEndpoint.ToString(), dbConnectionString.AuthKey, null);
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(migrationsAssembly))
                {
                    return options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(migrationsAssembly);
                        sqlOptions.UseNetTopologySuite();
                    });
                }
                return options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.UseNetTopologySuite();
                });
            }
        }

        //https://medium.com/volosoft/asp-net-core-dependency-injection-best-practices-tips-tricks-c6e9c67f9d96
        public static IServiceCollection AddDbContextInMemory<TContext>(this IServiceCollection services, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(options =>
                    options.UseInMemoryDatabase(Guid.NewGuid().ToString()), contextLifetime);
        }

        public static IServiceCollection AddDbContextSqlServer<TContext>(this IServiceCollection services, string connectionString, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.UseNetTopologySuite();
                    }), contextLifetime);
        }

        public static IServiceCollection AddDbContextSqlite<TContext>(this IServiceCollection services, string connectionString, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(options =>
                    options.UseSqlite(connectionString, sqlOptions =>
                    {
                        sqlOptions.UseNetTopologySuite();
                    }), contextLifetime);
        }

        public static IServiceCollection AddDbContextSqliteInMemory<TContext>(this IServiceCollection services, ServiceLifetime contextLifetime = ServiceLifetime.Singleton) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(options =>
                    options.UseSqlite(":memory:", sqlOptions =>
                    {
                        sqlOptions.UseNetTopologySuite();
                    }), contextLifetime);
        }

        public static IServiceCollection AddDbContextPoolSqlServer<TContext>(this IServiceCollection services, string connectionString, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            return services.AddDbContextPool<TContext>(options =>
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.UseNetTopologySuite();
                    }));
        }

        public static IServiceCollection AddDbContextSqlServerWithRetries<TContext>(this IServiceCollection services, string connectionString, int retries = 10, ServiceLifetime contextLifetime = ServiceLifetime.Scoped) where TContext : DbContext
        {
            return services.AddDbContext<TContext>(options =>
                     options.UseSqlServer(connectionString,
                     sqlServerOptionsAction: sqlOptions =>
                     {
                         sqlOptions.EnableRetryOnFailure(
                         maxRetryCount: retries,
                         maxRetryDelay: TimeSpan.FromSeconds(30),
                         errorNumbersToAdd: null);
                         sqlOptions.UseNetTopologySuite();
                     }), contextLifetime);
        }
    }
}
