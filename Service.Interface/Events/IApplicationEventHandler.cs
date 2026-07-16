namespace Service.Interface.Events
{
    public interface IApplicationEventHandler
    {
        bool CanHandle(IApplicationEvent applicationEvent);
        Task Handle(IApplicationEvent applicationEvent);
    }
}
