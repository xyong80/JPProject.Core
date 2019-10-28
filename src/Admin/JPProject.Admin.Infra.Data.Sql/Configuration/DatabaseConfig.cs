using JPProject.Admin.Infra.Data.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;
using JPProject.EntityFrameworkCore.Configuration;
using JPProject.EntityFrameworkCore.Context;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseConfig
    {
        public static IServiceCollection WithSqlServer(this IServiceCollection services, string connectionString, JpDatabaseOptions options = null)
        {
            var migrationsAssembly = typeof(DatabaseConfig).GetTypeInfo().Assembly.GetName().Name;
            services.AddEntityFrameworkSqlServer().AddAdminContext(opt => opt.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)), options);

            return services;
        }
        public static IServiceCollection WithSqlServer(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction, JpDatabaseOptions options = null)
        {
            services.AddAdminContext(optionsAction, options);

            return services;
        }
    }
}