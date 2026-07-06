using NSubstitute;
using Service.Events;
using Service.Interface.Events;

namespace ServiceTests
{
    public class ApplicationEventDispatcherTests
    {
        [Test]
        public async Task MustPublishEventToAvailableHandlers()
        {
            var applicationEvent = Substitute.For<IApplicationEvent>();
            var handler = Substitute.For<IApplicationEventHandler>();
            var ignoredHandler = Substitute.For<IApplicationEventHandler>();

            handler.CanHandle(applicationEvent).Returns(true);
            handler.Handle(applicationEvent).Returns(Task.CompletedTask);

            ignoredHandler.CanHandle(applicationEvent).Returns(false);
            ignoredHandler.Handle(applicationEvent).Returns(Task.CompletedTask);

            var dispatcher = new ApplicationEventDispatcher([handler, ignoredHandler]);

            await dispatcher.Publish(applicationEvent);

            await handler.Received(1).Handle(applicationEvent);
            await ignoredHandler.Received(0).Handle(applicationEvent);
        }
    }
}
