using Amazon.SecretsManager;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using GerenciamentoMecanicaSistema.Observability;
using Infrastructure.Configuration;
using Infrastructure.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ControllerTests.Observability;

[NonParallelizable]
public sealed class ApiMetricProviderTests
{
    private TextMapPropagator originalPropagator = null!;

    [SetUp]
    public void SavePropagator() => originalPropagator = Propagators.DefaultTextMapPropagator;

    [TearDown]
    public void RestorePropagator() => Sdk.SetDefaultTextMapPropagator(originalPropagator);

    [Test]
    public void DisabledTelemetryCreatesNeitherProviderNorIntegrationMetrics()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

        using var metrics = ApiMetricProvider.Create(builder);
        builder.AddApiObservability(metrics);

        Assert.Multiple(() =>
        {
            Assert.That(metrics, Is.Null);
            Assert.That(builder.Services.Any(service => service.ServiceType == typeof(IntegrationMetrics)), Is.False);
        });
    }

    [Test]
    public void HostReusesTheProviderAndIntegrationInstrumentsCreatedBeforeStartup()
    {
        var builder = Builder();
        using var metrics = ApiMetricProvider.Create(builder)!;
        var originalProvider = metrics.Provider;
        builder.AddApiObservability(metrics);
        using var services = builder.Services.BuildServiceProvider();

        var reused = services.GetRequiredService<ApiMetricProvider>();
        using var businessMeter = services.GetRequiredService<IMeterFactory>().Create(OrderBusinessMetrics.MeterName);
        var businessCounter = businessMeter.CreateCounter<long>("mecanica.orders.created");

        Assert.Multiple(() =>
        {
            Assert.That(reused, Is.SameAs(metrics));
            Assert.That(reused.Provider, Is.SameAs(originalProvider));
            Assert.That(services.GetRequiredService<IntegrationMetrics>(), Is.SameAs(metrics.Integrations));
            Assert.That(services.GetService<MeterProvider>(), Is.Null, "The host must not create a second metric provider.");
            Assert.That(businessCounter.Enabled, Is.True, "The early provider must also collect the existing business meter.");
        });
    }

    [Test]
    public void IntegrationMeterCanBeDisabledWithoutDisablingOtherMetrics()
    {
        var builder = Builder();
        builder.Configuration[$"Metrics:EnabledMetrics:{IntegrationMetrics.MeterName}"] = "false";
        using var metrics = ApiMetricProvider.Create(builder)!;
        builder.AddApiObservability(metrics);
        using var services = builder.Services.BuildServiceProvider();

        Assert.Multiple(() =>
        {
            Assert.That(metrics.Integrations, Is.Null);
            Assert.That(services.GetService<IntegrationMetrics>(), Is.Null);
            Assert.That(metrics.Provider, Is.Not.Null);
        });
    }

    [Test]
    public void BootstrapFailureIsMeasuredBeforeBuildingTheHostAndShutdownDoesNotWaitForThePeriodicInterval()
    {
        var builder = Builder();
        var metrics = ApiMetricProvider.Create(builder)!;
        using var listener = new MeterListener();
        var outcomes = new List<object?>();
        listener.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == IntegrationMetrics.MeterName)
                owner.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) => outcomes.Add(tags.ToArray().Single(tag => tag.Key == "outcome").Value));
        listener.Start();
        using var parameters = Substitute.For<IAmazonSimpleSystemsManagement>();
        using var secrets = Substitute.For<IAmazonSecretsManager>();
        parameters.GetParametersAsync(Arg.Any<GetParametersRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetParametersResponse>(new InvalidOperationException("private-sdk-error")));
        var loader = new AwsRuntimeConfigurationLoader(parameters, secrets, metrics.Integrations);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => loader.LoadAsync("hom", "unused-certificate.pem"));
        var elapsed = Stopwatch.StartNew();
        metrics.Dispose();
        metrics.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(outcomes, Is.EqualTo(new[] { "failure" }));
            Assert.That(error!.Message, Does.Not.Contain("private-sdk-error"));
            Assert.That(elapsed.Elapsed, Is.LessThan(TimeSpan.FromSeconds(3)));
        });
    }

    private static WebApplicationBuilder Builder()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.Configuration.AddJsonFile(Path.Combine(TestContext.CurrentContext.TestDirectory, "Observability", "observability.json"));
        builder.Configuration["Telemetry:Enabled"] = "true";
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = "http://127.0.0.1:1";
        builder.Configuration["Telemetry:StartupShutdownTimeoutMilliseconds"] = "50";
        builder.Configuration["OTEL_METRIC_EXPORT_INTERVAL"] = "3600000";
        builder.Configuration["OTEL_EXPORTER_OTLP_TIMEOUT"] = "50";
        return builder;
    }
}
