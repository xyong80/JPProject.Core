using System.Threading.Tasks;

namespace JPProject.Domain.Core.Events
{
    public interface IEventStore
    {
        Task Save<T>(T theEvent) where T : Event;
    }
}
