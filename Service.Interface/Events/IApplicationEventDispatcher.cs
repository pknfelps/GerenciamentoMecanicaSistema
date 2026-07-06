namespace Service.Interface.Events
{
    public interface IApplicationEventDispatcher
    {
        Task Publish(IApplicationEvent applicationEvent);
    }
}
