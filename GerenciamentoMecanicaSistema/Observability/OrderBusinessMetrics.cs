using Service.Interface.Observability;
using System.Diagnostics.Metrics;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class OrderBusinessMetrics : IOrderNotificationMetrics
{
    public const string MeterName = "Mecanica.Api.Business";
    private readonly Counter<long> created;
    private readonly Histogram<double> stageDuration;
    private readonly Histogram<double> serviceDuration;
    private readonly Counter<long> processingFailures;
    private readonly Counter<long> notifications;
    private readonly Counter<long> telemetryFailures;
    private readonly string notificationMode;
    private readonly ILogger<OrderBusinessMetrics> logger;

    public OrderBusinessMetrics(IMeterFactory factory, ILogger<OrderBusinessMetrics> logger)
    {
        var meter = factory.Create(MeterName);
        created = meter.CreateCounter<long>("mecanica.orders.created", "{order}");
        stageDuration = meter.CreateHistogram<double>("mecanica.orders.stage.duration", "s");
        serviceDuration = meter.CreateHistogram<double>("mecanica.orders.service.duration", "s");
        processingFailures = meter.CreateCounter<long>("mecanica.orders.processing.failures", "{failure}");
        notifications = meter.CreateCounter<long>("mecanica.orders.notifications", "{attempt}");
        telemetryFailures = meter.CreateCounter<long>("mecanica.orders.telemetry.failures", "{failure}");
        notificationMode = "smtp"; // SMTP4dev capture through EmailFunction in every environment.
        this.logger = logger;
    }

    public void RecordCreated() => Record(() => created.Add(1));
    public void RecordStage(string stage, double seconds, string? reason = null) => Record(() =>
    {
        if (reason is null)
            stageDuration.Record(seconds, new KeyValuePair<string, object?>("stage", stage));
        else
            stageDuration.Record(seconds, new("stage", stage), new("completion.reason", reason));
    });
    public void RecordService(double seconds) => Record(() => serviceDuration.Record(seconds));
    public void RecordProcessingFailure(string operation) => Record(() => processingFailures.Add(1, new KeyValuePair<string, object?>("operation", operation)));
    public void RecordTelemetryFailure() => Record(() => telemetryFailures.Add(1));
    public void RecordNotification(bool budget, bool succeeded) => Record(() => notifications.Add(1,
        new("notification.type", budget ? "budget_available" : "status_changed"),
        new("outcome", succeeded ? "success" : "failure"), new("mode", notificationMode)));

    private void Record(Action measurement)
    {
        try { measurement(); }
        catch (Exception exception)
        {
            logger.LogWarning("Falha ao registrar métrica de negócio. ExceptionType: {ExceptionType}", exception.GetType().FullName);
        }
    }
}
