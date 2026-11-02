namespace Service.Interface.Observability;

public interface IOrderNotificationMetrics
{
    void RecordNotification(bool budget, bool succeeded);
}
