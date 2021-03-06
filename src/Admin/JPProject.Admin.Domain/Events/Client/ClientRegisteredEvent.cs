using JPProject.Domain.Core.Events;

namespace JPProject.Admin.Domain.Events.Client
{
    public class ClientRemovedEvent : Event
    {
        public ClientRemovedEvent(string clientId)
            : base(EventTypes.Success)
        {
            AggregateId = clientId;
        }
    }
}