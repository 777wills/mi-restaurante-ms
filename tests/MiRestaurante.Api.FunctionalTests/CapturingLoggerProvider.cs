using Microsoft.Extensions.Logging;

namespace MiRestaurante.Api.FunctionalTests;

public sealed record CapturedLogEntry(LogLevel Level, string Category, string Message, Exception? Exception);

public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<CapturedLogEntry> _entries = [];
    private readonly Lock _gate = new();

    public IReadOnlyList<CapturedLogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return [.. _entries];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this, categoryName);

    public void Dispose()
    {
    }

    private void Add(CapturedLogEntry entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }
    }

    private sealed class CapturingLogger(CapturingLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            provider.Add(new CapturedLogEntry(logLevel, category, formatter(state, exception), exception));
    }
}
