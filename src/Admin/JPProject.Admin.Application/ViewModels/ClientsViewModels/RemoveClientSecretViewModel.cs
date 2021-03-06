using System.ComponentModel.DataAnnotations;

namespace JPProject.Admin.Application.ViewModels.ClientsViewModels
{
    public class RemoveClientSecretViewModel
    {

        public RemoveClientSecretViewModel(string clientId, string type, string value)
        {
            ClientId = clientId;
            Type = type;
            Value = value;
        }

        [Required]
        public string Type { get; set; }
        [Required]
        public string Value { get; }

        [Required]
        public string ClientId { get; set; }
    }
}
