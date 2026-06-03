using Lumiere.Infrastructure.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lumiere.Graphics.Tests.Diagnostics;

public sealed class SessionDiagnosticScopeTests
{
    [Fact]
    public void Begin_WithExplicitSessionId_PreservesId()
    {
        using var logger = new TestLogger();
        using var scope = SessionDiagnosticScope.Begin(logger, sessionId: "test-session");

        Assert.Equal("test-session", scope.SessionId);
    }

    [Fact]
    public void Begin_WithExplicitCorrelationId_PreservesId()
    {
        using var logger = new TestLogger();
        using var scope = SessionDiagnosticScope.Begin(logger, sessionId: "s1", correlationId: "c1");

        Assert.Equal("s1", scope.SessionId);
        Assert.Equal("c1", scope.CorrelationId);
    }

    [Fact]
    public void Begin_WithoutIds_GeneratesIds()
    {
        using var logger = new TestLogger();
        using var scope = SessionDiagnosticScope.Begin(logger);

        Assert.NotNull(scope.SessionId);
        Assert.NotEmpty(scope.SessionId);
        Assert.NotNull(scope.CorrelationId);
        Assert.NotEmpty(scope.CorrelationId);
    }

    [Fact]
    public void Begin_CreatesLogScope()
    {
        using var logger = new TestLogger();
        using var scope = SessionDiagnosticScope.Begin(logger, sessionId: "s1", correlationId: "c1");

        Assert.True(logger.ScopeCreated);
    }

    [Fact]
    public void Dispose_DisposesLogScope()
    {
        using var logger = new TestLogger();
        var scope = SessionDiagnosticScope.Begin(logger, sessionId: "s1");
        scope.Dispose();

        Assert.True(logger.ScopeDisposed);
    }

    private sealed class TestLogger : ILogger, IDisposable
    {
        public bool ScopeCreated { get; private set; }
        public bool ScopeDisposed { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            ScopeCreated = true;
            return new TestScope(this);
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
        }

        public void Dispose()
        {
        }

        private sealed class TestScope : IDisposable
        {
            private readonly TestLogger owner;
            public TestScope(TestLogger owner) => this.owner = owner;
            public void Dispose() => owner.ScopeDisposed = true;
        }
    }
}
