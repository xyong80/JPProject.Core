using JPProject.Domain.Core.Events;

namespace JPProject.Admin.Domain.Events.Client
{
    public class NewClientSecretEvent : Event
    {
        public string Type { get; }
        public string Description { get; }

        public NewClientSecretEvent(string clientId, string type, string description)
            : base(EventTypes.Success)
        {
            AggregateId = clientId;
            Type = type;
            Description = description;
        }
    }
}