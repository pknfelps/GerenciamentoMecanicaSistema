using Service.Interface.Observability;

namespace Service.Observability;

public sealed class NullOrderNotificationMetrics : IOrderNotificationMetrics
{
    public void RecordNotification(bool budget, bool succeeded)
    {
        // Notifications keep their existing behavior when business telemetry is disabled.
    }
}
