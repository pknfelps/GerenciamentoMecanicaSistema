using Service.Interface.Events;

namespace Service.Events
{
    public class ApplicationEventDispatcher(IEnumerable<IApplicationEventHandler> handlers) : IApplicationEventDispatcher
    {
        private IEnumerable<IApplicationEventHandler> Handlers { get; } = handlers;

        public async Task Publish(IApplicationEvent applicationEvent)
        {
            foreach (var handler in Handlers.Where(handler => handler.CanHandle(applicationEvent)))
                await handler.Handle(applicationEvent);
        }
    }
}
