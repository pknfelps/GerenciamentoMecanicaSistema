using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ControllerTests.Logging;

internal sealed class CapturingLoggerProvider : ILoggerProvider, ILogger, ISupportExternalScope
{
    private IExternalScopeProvider externalScopeProvider = new LoggerExternalScopeProvider();
    public ConcurrentQueue<CapturedLogEntry> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => this;
    public bool IsEnabled(LogLevel logLevel) => true;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => externalScopeProvider.Push(state);
    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => externalScopeProvider = scopeProvider;
    public void Dispose() { }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var scopes = new List<KeyValuePair<string, IReadOnlyDictionary<string, object?>>>();
        externalScopeProvider.ForEachScope((scope, target) => target.Add(new(scope?.ToString() ?? "", Properties(scope))), scopes);
        Entries.Enqueue(new(logLevel, formatter(state, exception), Properties(state), exception, scopes));
    }

    private static IReadOnlyDictionary<string, object?> Properties(object? state) =>
        state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.ToDictionary(pair => pair.Key, pair => pair.Value)
            : new Dictionary<string, object?>();
}
