using JPProject.Admin.Application.Configuration;
using JPProject.Domain.Core.Bus;
using JPProject.Domain.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class AdminUiBootstrapper
    {
        public static IServiceCollection ConfigureJpAdmin<T>(this IServiceCollection services) where T : class, ISystemUser
        {
            // Domain Bus (Mediator)
            services.TryAddScoped<IMediatorHandler, InMemoryBus>();

            services.TryAddScoped<ISystemUser, T>();

            // Application
            ApplicationBootStrapper.RegisterServices(services);

            // Domain - Events
            DomainEventsBootStrapper.RegisterServices(services);

            // Domain - Commands
            DomainCommandsBootStrapper.RegisterServices(services);

            // Infra - Data
            RepositoryBootStrapper.RegisterServices(services);

            return services;
        }
    }

}