using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Xunit;

namespace Lumiere.Graphics.Tests.Capture;

public sealed class CaptureLifecycleValidationTests
{
    [Fact]
    public void SummaryAcceptsRepeatedRecoverableFinalStates()
    {
        var summary = CaptureLifecycleValidationSummary.Create(
            new[]
            {
                CaptureLifecycleValidationRecord.Create(
                    attemptNumber: 1,
                    CaptureLifecycleAttemptKind.Start,
                    CaptureSessionStatus.Disposed,
                    captureTeardownCompleted: true,
                    previewDetachedBeforeRelease: true),
                CaptureLifecycleValidationRecord.Create(
                    attemptNumber: 2,
                    CaptureLifecycleAttemptKind.Restart,
                    CaptureSessionStatus.Idle,
                    captureTeardownCompleted: true,
                    previewDetachedBeforeRelease: true),
                CaptureLifecycleValidationRecord.Create(
                    attemptNumber: 3,
                    CaptureLifecycleAttemptKind.FailedInitialization,
                    CaptureSessionStatus.Failed,
                    captureTeardownCompleted: true,
                    previewDetachedBeforeRelease: true,
                    PreviewReadinessStatus.Failed(
                        PreviewReadinessStage.Capture,
                        "Preview failed",
                        "Synthetic initialization failure for lifecycle validation.")),
            });

        Assert.Equal(3, summary.AttemptCount);
        Assert.Equal(CaptureSessionStatus.Failed, summary.FinalStatus);
        Assert.True(summary.AllAttemptsEndedRecoverably);
        Assert.True(summary.AllCaptureTeardownsCompleted);
        Assert.True(summary.AllPreviewResourcesDetachedBeforeRelease);
        Assert.True(summary.HasNoUnboundedResourceGrowthEvidence);
        Assert.Equal("Synthetic initialization failure for lifecycle validation.", summary.FinalReadinessTechnicalDetail);
    }

    [Theory]
    [InlineData(CaptureSessionStatus.SelectingTarget)]
    [InlineData(CaptureSessionStatus.Initializing)]
    [InlineData(CaptureSessionStatus.Capturing)]
    public void SummaryFlagsStuckFinalStates(CaptureSessionStatus finalStatus)
    {
        var summary = CaptureLifecycleValidationSummary.Create(
            new[]
            {
                CaptureLifecycleValidationRecord.Create(
                    attemptNumber: 1,
                    CaptureLifecycleAttemptKind.Start,
                    finalStatus,
                    captureTeardownCompleted: false,
                    previewDetachedBeforeRelease: false,
                    resourceGrowthEvidence: CaptureResourceGrowthEvidence.NotMeasured),
            });

        Assert.False(summary.AllAttemptsEndedRecoverably);
        Assert.Contains(finalStatus, summary.StuckFinalStates);
    }

    [Fact]
    public void SummaryFlagsResourceGrowthEvidence()
    {
        var summary = CaptureLifecycleValidationSummary.Create(
            new[]
            {
                CaptureLifecycleValidationRecord.Create(
                    attemptNumber: 1,
                    CaptureLifecycleAttemptKind.Restart,
                    CaptureSessionStatus.Disposed,
                    captureTeardownCompleted: true,
                    previewDetachedBeforeRelease: true,
                    resourceGrowthEvidence: CaptureResourceGrowthEvidence.GrowthObserved),
            });

        Assert.False(summary.HasNoUnboundedResourceGrowthEvidence);
    }

    [Fact]
    public void SummaryRequiresAtLeastOneAttemptForAggregateSuccess()
    {
        var summary = CaptureLifecycleValidationSummary.Create(Array.Empty<CaptureLifecycleValidationRecord>());

        Assert.Equal(0, summary.AttemptCount);
        Assert.False(summary.AllAttemptsEndedRecoverably);
        Assert.False(summary.AllCaptureTeardownsCompleted);
        Assert.False(summary.AllPreviewResourcesDetachedBeforeRelease);
        Assert.True(summary.HasNoUnboundedResourceGrowthEvidence);
    }

    [Fact]
    public void SummaryDoesNotTreatUnmeasuredResourceGrowthAsNoGrowthEvidence()
    {
        var summary = CaptureLifecycleValidationSummary.Create(
            new[]
            {
                CaptureLifecycleValidationRecord.Create(
                    attemptNumber: 1,
                    CaptureLifecycleAttemptKind.Restart,
                    CaptureSessionStatus.Disposed,
                    captureTeardownCompleted: true,
                    previewDetachedBeforeRelease: true,
                    resourceGrowthEvidence: CaptureResourceGrowthEvidence.NotMeasured),
            });

        Assert.False(summary.HasNoUnboundedResourceGrowthEvidence);
    }

    [Fact]
    public void RecordRejectsInvalidAttemptNumber()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            CaptureLifecycleValidationRecord.Create(
                attemptNumber: 0,
                CaptureLifecycleAttemptKind.Start,
                CaptureSessionStatus.Idle,
                captureTeardownCompleted: true,
                previewDetachedBeforeRelease: true));

        Assert.Equal("attemptNumber", exception.ParamName);
    }
}
