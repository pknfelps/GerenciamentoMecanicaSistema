using Microsoft.Extensions.Logging;

namespace ControllerTests.Logging;

internal sealed record CapturedLogEntry(
    LogLevel Level,
    string Message,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, IReadOnlyDictionary<string, object?>>> Scopes);
