using JPProject.Admin.Domain.Commands.ApiResource;
using JPProject.Admin.Domain.Validations.Client;

namespace JPProject.Admin.Domain.Validations.ApiResource
{
    public class SaveApiSecretCommandValidation : ApiSecretValidation<SaveApiSecretCommand>
    {
        public SaveApiSecretCommandValidation()
        {
            ValidateResourceName();
            ValidateType();
            ValidateValue();
            ValidateHashType();
        }
    }
}