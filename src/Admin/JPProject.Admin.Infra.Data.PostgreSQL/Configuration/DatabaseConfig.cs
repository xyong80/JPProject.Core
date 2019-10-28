using JPProject.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using JPProject.Admin.Infra.Data.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DatabaseConfig
    {
        public static IServiceCollection WithPostgreSql(this IServiceCollection services, string connectionString, JpDatabaseOptions options = null)
        {
            var migrationsAssembly = typeof(DatabaseConfig).GetTypeInfo().Assembly.GetName().Name;
            services.AddEntityFrameworkNpgsql().AddAdminContext(opt => opt.UseNpgsql(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly)), options);

            return services;
        }

    }
}
