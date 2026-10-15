using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Service.Interface.Observability;
using System.Net;
using System.Net.Http.Json;

namespace ControllerTests.Observability;

[TestFixture(true)]
[TestFixture(false)]
[NonParallelizable]
public sealed class OrderBusinessDisabledTests(bool allTelemetryDisabled) : BaseControllerTests
{
    private readonly IOrderMetricsHistoryReader reader = Substitute.For<IOrderMetricsHistoryReader>();
    protected override TestWebApplicationFactory CreateFactory() => new TelemetryWebApplicationFactory
    {
        TelemetryEnabled = allTelemetryDisabled ? "false" : "true", TelemetryEndpoint = "http://127.0.0.1:1",
        TelemetryConfiguration = { ["Metrics:EnabledMetrics:Mecanica.Api.Business"] = "false" },
        ConfigureTestServices = services => services.AddSingleton(reader)
    };
    protected override void MockService() { }

    [Test]
    public async Task DisablingCollectionRemovesBusinessInstrumentationAndHistoryQueries()
    {
        using var creation = await TestClient.PostAsJsonAsync("/Orders", new
        { customerId = Guid.NewGuid(), vehicleId = Guid.NewGuid(), services = Array.Empty<object>(), materials = Array.Empty<object>() });
        using var completion = await TestClient.PatchAsync($"/Orders/{Guid.NewGuid()}/execution/complete", null);
        Assert.That(creation.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(completion.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
        Assert.That(TestWebAppFactory.Services.GetService<OrderBusinessMetrics>(), Is.Null);
        await reader.DidNotReceiveWithAnyArgs().ReadAsync(Guid.Empty, default, default);
        Assert.That(TestWebAppFactory.Services.GetRequiredService<IOrderNotificationMetrics>(), Is.TypeOf<Service.Observability.NullOrderNotificationMetrics>());
    }
}
