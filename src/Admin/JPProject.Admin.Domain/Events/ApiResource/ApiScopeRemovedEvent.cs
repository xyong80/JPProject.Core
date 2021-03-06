using JPProject.Domain.Core.Events;

namespace JPProject.Admin.Domain.Events.ApiResource
{
    public class ApiScopeRemovedEvent : Event
    {
        public string Name { get; }

        public ApiScopeRemovedEvent(string name, string resourceName)
        {
            AggregateId = resourceName;
            Name = name;
        }
    }
}