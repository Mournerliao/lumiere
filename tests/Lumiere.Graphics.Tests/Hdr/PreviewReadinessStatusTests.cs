using Lumiere.Graphics.Hdr;
using Xunit;

namespace Lumiere.Graphics.Tests.Hdr;

public sealed class PreviewReadinessStatusTests
{
    [Fact]
    public void InitializingStatusIsNotReadyBeforeValidation()
    {
        var status = PreviewReadinessStatus.Initializing();

        Assert.Equal(PreviewReadinessState.Initializing, status.State);
        Assert.False(status.IsReady);
        Assert.False(status.RequiresUserAttention);
    }

    [Fact]
    public void CannotEstablishHdrReadinessReportsDegradedState()
    {
        var status = PreviewReadinessStatus.Degraded(
            PreviewReadinessStage.Presentation,
            "HDR preview readiness could not be fully established.",
            "DXGI color-space validation has not completed.");

        Assert.Equal(PreviewReadinessState.Degraded, status.State);
        Assert.Equal(PreviewReadinessStage.Presentation, status.Stage);
        Assert.False(status.IsReady);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("HDR preview", status.UserMessage, StringComparison.Ordinal);
        Assert.Contains("DXGI", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsupportedStatusCarriesUserAndTechnicalDiagnostics()
    {
        var status = PreviewReadinessStatus.Unsupported(
            PreviewReadinessStage.Graphics,
            "HDR preview is unsupported on this graphics configuration.",
            "Required FP16 scRGB swap-chain color space is unavailable.");

        Assert.Equal(PreviewReadinessState.Unsupported, status.State);
        Assert.Equal(PreviewReadinessStage.Graphics, status.Stage);
        Assert.False(status.IsReady);
        Assert.True(status.RequiresUserAttention);
        Assert.Contains("unsupported", status.UserMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FP16", status.TechnicalDetail ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadyStatusIsOnlyReportedByExplicitReadyFactory()
    {
        var status = PreviewReadinessStatus.Ready("HDR preview path is validated.");

        Assert.Equal(PreviewReadinessState.Ready, status.State);
        Assert.True(status.IsReady);
        Assert.False(status.RequiresUserAttention);
    }

    [Fact]
    public void StatusCannotBeCreatedWithPublicReadyBypass()
    {
        var publicConstructors = typeof(PreviewReadinessStatus).GetConstructors();

        Assert.DoesNotContain(publicConstructors, constructor => constructor.IsPublic);
    }
}
