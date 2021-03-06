using IdentityServer4.Models;
using JPProject.Domain.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JPProject.Admin.Domain.Interfaces
{
    public interface IApiResourceRepository : IRepository<ApiResource>
    {
        Task<List<ApiResource>> GetResources();
        Task<ApiResource> GetByName(string name);
        Task UpdateWithChildrens(string oldResourceName, ApiResource irs);
        Task<ApiResource> GetResource(string resourceName);
        void RemoveSecret(string resourceName, Secret secret);
        void AddSecret(string resourceName, Secret secret);
        void RemoveScope(string resourceName, string name);
        void AddScope(string resourceName, Scope scope);
        Task<IEnumerable<Secret>> GetSecretsByApiName(string name);
        Task<IEnumerable<Scope>> GetScopesByResource(string name);
        Task<List<string>> ListResources();
    }
}