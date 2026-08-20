using Lumiere.Windows.Graphics.Hdr;
using Xunit;

namespace Lumiere.Windows.Graphics.Tests.Hdr;

public sealed class EngineReadinessStatusTests
{
    [Fact]
    public void InitializingStatusIsNotReadyBeforeTheProbeCompletes()
    {
        var status = EngineReadinessStatus.Initializing();

        Assert.Equal(EngineReadinessState.Initializing, status.State);
        Assert.False(status.IsReady);
        Assert.False(status.RequiresUserAttention);
    }

    [Fact]
    public void CannotEstablishHdrReadinessReportsDegradedState()
    {
        var status = EngineReadinessStatus.Degraded(
            EngineReadinessStage.Graphics,
            "HDR capture readiness could not be fully established.",
            "DXGI display capability probe has not completed.");

        Assert.Equal(EngineReadinessState.Degraded, status.State);
        Assert.Equal(EngineReadinessStage.Graphics, status.Stage);
        Assert.False(status.IsReady);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("HDR capture", status.UserMessage, StringComparison.Ordinal);
        Assert.Contains("DXGI", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedStatusCarriesUserAndTechnicalDiagnostics()
    {
        var status = EngineReadinessStatus.Unsupported(
            EngineReadinessStage.Graphics,
            "HDR capture is unsupported on this graphics configuration.",
            "Required FP16 scRGB swap-chain color space is unavailable.");

        Assert.Equal(EngineReadinessState.Unsupported, status.State);
        Assert.Equal(EngineReadinessStage.Graphics, status.Stage);
        Assert.False(status.IsReady);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("unsupported", status.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FP16", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadyStatusIsOnlyReportedByExplicitReadyFactory()
    {
        var status = EngineReadinessStatus.Ready("HDR-aware engine is ready.");

        Assert.Equal(EngineReadinessState.Ready, status.State);
        Assert.True(status.IsReady);
        Assert.False(status.RequiresUserAttention);
    }

    [Fact]
    public void StatusCannotBeCreatedWithPublicReadyBypass()
    {
        var publicConstructors = typeof(EngineReadinessStatus).GetConstructors();

        Assert.DoesNotContain(publicConstructors, constructor => constructor.IsPublic);
    }
}
