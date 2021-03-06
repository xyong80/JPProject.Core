using FluentValidation.Results;
using IdentityServer4.Models;
using JPProject.Admin.Domain.Commands.Clients;
using JPProject.Admin.Domain.Validations.ApiResource;
using System.Collections.Generic;
using System.Linq;

namespace JPProject.Admin.Domain.Commands.ApiResource
{
    public class SaveApiScopeCommand : ApiScopeCommand
    {

        public SaveApiScopeCommand(string resourceName, string name, string description, string displayName, bool emphasize, bool showInDiscoveryDocument, IEnumerable<string> userClaims)
        {
            ResourceName = resourceName;
            Name = name;
            Description = description;
            DisplayName = displayName;
            Emphasize = emphasize;
            ShowInDiscoveryDocument = showInDiscoveryDocument;
            UserClaims = userClaims;
        }

        public override bool IsValid()
        {
            ValidationResult = new SaveApiScopeCommandValidation().Validate(this);
            return ValidationResult.IsValid;
        }

        public Scope ToModel()
        {
            return new Scope()
            {
                Description = Description,
                Required = Required,
                DisplayName = DisplayName,
                Emphasize = Emphasize,
                Name = Name,
                ShowInDiscoveryDocument = ShowInDiscoveryDocument,
                UserClaims = UserClaims.ToList(),
            };
        }
    }
}