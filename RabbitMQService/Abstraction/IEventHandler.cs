
namespace RabbitMQService.Abstraction
{
    public interface IEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
