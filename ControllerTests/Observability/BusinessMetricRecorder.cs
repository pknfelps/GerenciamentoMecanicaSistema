using GerenciamentoMecanicaSistema.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics.Metrics;

namespace ControllerTests.Observability;

internal sealed class BusinessMetricRecorder : IDisposable
{
    private readonly ServiceProvider services;
    private readonly MeterListener listener = new();

    public BusinessMetricRecorder()
    {
        services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        var factory = services.GetRequiredService<IMeterFactory>();
        Meter = factory.Create(OrderBusinessMetrics.MeterName);
        listener.InstrumentPublished = (instrument, owner) =>
        {
            // Capture only this recorder's meter, not other tests or HTTP hosts.
            if (ReferenceEquals(instrument.Meter, Meter))
                owner.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            Measurements.Add(new(instrument.Name, value, tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value))));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
            Measurements.Add(new(instrument.Name, value, tags.ToArray().ToDictionary(tag => tag.Key, tag => tag.Value))));
        listener.Start();
        Metrics = new OrderBusinessMetrics(factory, NullLogger<OrderBusinessMetrics>.Instance);
    }

    public Meter Meter { get; }
    public OrderBusinessMetrics Metrics { get; }
    public List<BusinessMeasurement> Measurements { get; } = [];

    public void Dispose()
    {
        listener.Dispose();
        services.Dispose();
    }
}

internal sealed record BusinessMeasurement(string Name, double Value, Dictionary<string, object?> Tags);
