using Microsoft.Extensions.Logging;

namespace InvoiceFlow.UnitTests;

public sealed class TestNullLogger<T> : ILogger<T>, ILogger where T : class
{
    public static readonly TestNullLogger<T> Instance = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
