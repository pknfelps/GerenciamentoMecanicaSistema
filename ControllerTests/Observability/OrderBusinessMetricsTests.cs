using Domain.Interface.Order;
using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Service.Interface.Exceptions;
using Service.Interface.Observability;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class OrderBusinessMetricsTests : BaseControllerTests
{
    private IOrderMetricsHistoryReader reader = null!;
    private TelemetryWebApplicationFactory Telemetry => (TelemetryWebApplicationFactory)TestWebAppFactory;
    protected override TestWebApplicationFactory CreateFactory()
    {
        reader = Substitute.For<IOrderMetricsHistoryReader>();
        reader.ReadAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(OrderDurationMeasurementsTests.History());
        return new TelemetryWebApplicationFactory
        {
            TelemetryEnabled = "true", TelemetryEndpoint = "http://127.0.0.1:1",
            ConfigureTestServices = services => { services.RemoveAll<IOrderMetricsHistoryReader>(); services.AddSingleton(reader); }
        };
    }
    protected override void MockService() { }

    [Test]
    public async Task SuccessfulCreationCountsOnceWithoutQueryingHistoryOrDependingOnParentSampling()
    {
        TestClient.DefaultRequestHeaders.Add("traceparent", "00-11111111111111111111111111111111-2222222222222222-00");
        using var response = await TestClient.PostAsJsonAsync("/Orders", new { customerId = Guid.NewGuid(), vehicleId = Guid.NewGuid(), services = Array.Empty<object>(), materials = Array.Empty<object>() });
        Telemetry.Flush();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.created"), Is.EqualTo(1));
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(Guid.Empty, default, default);
    }

    [TestCase("diagnosis/complete", "diagnosis")]
    [TestCase("execution/complete", "execution")]
    [TestCase("delivery", "delivery_wait")]
    public async Task SuccessfulTransitionReadsOneCommittedInterval(string route, string stage)
    {
        var id = Guid.NewGuid();
        using var response = await TestClient.PatchAsync($"/Orders/{id}/{route}", null);
        Telemetry.Flush();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.stage.duration", ("stage", stage)), Is.EqualTo(1));
        await reader.Received(1).ReadAsync(id, 1, Arg.Any<CancellationToken>());
    }

    [TestCase("missing")]
    [TestCase("failure")]
    [TestCase("timeout")]
    public async Task MeasurementFailureNeverChangesCommittedSuccess(string scenario)
    {
        reader.ReadAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            if (scenario == "timeout") await Task.Delay(Timeout.Infinite, call.ArgAt<CancellationToken>(2));
            if (scenario == "missing") return (IReadOnlyList<OrderHistoryEntry>)Array.Empty<OrderHistoryEntry>();
            throw new InvalidOperationException("private-history-password");
        });
        var watch = Stopwatch.StartNew();
        using var response = await TestClient.PatchAsync($"/Orders/{Guid.NewGuid()}/execution/complete", null);
        Telemetry.Flush();
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.telemetry.failures"), Is.EqualTo(1));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.processing.failures"), Is.Zero);
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.stage.duration"), Is.Zero);
        Assert.That(string.Join(' ', Telemetry.Logs.Select(log => log.FormattedMessage)), Does.Not.Contain("private-history-password"));
        if (scenario == "timeout") Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(5)));
    }

    [TestCase(400)]
    [TestCase(404)]
    [TestCase(409)]
    [TestCase(422)]
    [TestCase(500)]
    public async Task OnlyTechnicalFailuresCountAndNeverProduceCompletedIntervals(int status)
    {
        Exception error = status switch
        {
            400 => new InvalidRequestException("invalid"), 404 => new NotFoundException("missing"),
            409 => new ConflictException("conflict"), 422 => new BusinessRuleException("rejected"),
            _ => new ApplicationFailureException("failure")
        };
        TestWebAppFactory.OrderServiceMock.CompleteExecution(Arg.Any<Guid>()).Returns(Task.FromException(error));
        using var response = await TestClient.PatchAsync($"/Orders/{Guid.NewGuid()}/execution/complete", null);
        Telemetry.Flush();
        Assert.That((int)response.StatusCode, Is.EqualTo(status));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.processing.failures", ("operation", "CompleteExecution")), Is.EqualTo(status == 500 ? 1 : 0));
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(Guid.Empty, default, default);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void NotificationsHaveBoundedOutcomeAndModeWithoutPersonalData(bool succeeded)
    {
        Telemetry.Services.GetRequiredService<IOrderNotificationMetrics>().RecordNotification(true, succeeded);
        Telemetry.Flush();
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.notifications",
            ("notification.type", "budget_available"), ("outcome", succeeded ? "success" : "failure"), ("mode", "smtp")), Is.EqualTo(1));
        foreach (var metric in Telemetry.Metrics.Where(metric => metric.MeterName == OrderBusinessMetrics.MeterName))
            foreach (ref readonly var point in metric.GetMetricPoints())
                foreach (var tag in point.Tags)
                    Assert.That(tag.Key, Is.AnyOf("operation", "stage", "completion.reason", "notification.type", "outcome", "mode"));
    }

    [TestCase(null)]
    [TestCase("Admin")]
    [TestCase("Customer")]
    public async Task AuthenticationAndAuthorizationNegativesDoNotQueryOrCountProcessingFailures(string? role)
    {
        TestClient.DefaultRequestHeaders.Authorization = null;
        if (role is not null) UseToken(role);
        using var response = await TestClient.PatchAsync($"/Orders/{Guid.NewGuid()}/execution/complete", null);
        Telemetry.Flush();
        Assert.That(response.StatusCode, Is.EqualTo(role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden));
        Assert.That(BusinessMetricAssertions.Count(Telemetry.Metrics, "mecanica.orders.processing.failures"), Is.Zero);
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(Guid.Empty, default, default);
    }
}
