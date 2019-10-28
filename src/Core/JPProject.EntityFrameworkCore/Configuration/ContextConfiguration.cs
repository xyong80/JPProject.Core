using JPProject.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace JPProject.EntityFrameworkCore.Configuration
{
    public static class ContextConfiguration
    {
        public static IServiceCollection AddEventStoreContext(this IServiceCollection services, Action<DbContextOptionsBuilder> optionsAction)
        {
            services.AddDbContext<EventStoreContext>(optionsAction);

            DbMigrationHelpers.CheckDatabases(services.BuildServiceProvider()).Wait();

            return services;
        }
    }
}
