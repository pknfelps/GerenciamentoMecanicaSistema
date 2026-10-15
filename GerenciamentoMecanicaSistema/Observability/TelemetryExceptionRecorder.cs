using System.Diagnostics;

namespace GerenciamentoMecanicaSistema.Observability;

public sealed class TelemetryExceptionRecorder(TelemetryCollectionOptions collection) : IHostedService, IDisposable
{
    private readonly ActivityListener listener = new()
    {
        ShouldListenTo = source => source.Name is "Npgsql" or "Microsoft.AspNetCore"
    };

    public Task StartAsync(CancellationToken cancellationToken)
    {
        listener.ExceptionRecorder = RecordException;
        ActivitySource.AddActivityListener(listener);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose() => listener.Dispose();

    private void RecordException(Activity activity, Exception exception, ref TagList tags)
    {
        tags = default;
        tags.Add("exception.type", exception.GetType().FullName);
        tags.Add("exception.message", activity.Source.Name == "Npgsql"
            ? collection.DatabaseErrorMessage
            : collection.HttpErrorMessage);
        // Presence prevents Activity.AddException from adding exception.ToString().
        tags.Add("exception.stacktrace", null);
    }
}
