using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class OutputValidationDraftFactoryTests
{
    [Fact]
    public void Create_PrefillsCurrentSessionContextForHdr10FolderValidation()
    {
        var session = CaptureSessionState.Capturing(
            CaptureTarget.CreateForTest(
                new SizeInt32
                {
                    Width = 3840,
                    Height = 2160,
                },
                "HDR Display",
                CaptureTargetKind.Display,
                new DisplayOutputIdentity("\\\\.\\DISPLAY1", left: 0, top: 0, width: 3840, height: 2160)),
            PreviewReadinessStatus.Ready(
                "HDR preview path is validated.",
                "IDXGISwapChain3.CheckColorSpaceSupport returned Present; IDXGISwapChain3.SetColorSpace1 set RgbFullG2084NoneP2020; display match=DesktopBounds."));
        var request = new OutputValidationDraftRequest(
            "0.1.0",
            OutputTarget.Folder,
            OutputProfileContract.Hdr10Pq,
            session);

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)));

        Assert.Equal("output-validation-draft-2026-06-22-hdr10-folder", document.FileNameStem);
        Assert.Equal("2026-06-22", document.Artifact.Date);
        Assert.Equal(["Folder"], document.Artifact.OutputTargetsTested);
        Assert.Contains("REL-OUT-04", document.Artifact.ChecklistIdsCovered);
        Assert.Contains("REL-HDR-04", document.Artifact.ChecklistIdsCovered);
        Assert.Contains("Microsoft Paint", document.Artifact.TargetAppsTested);
        Assert.Contains("Windows Photos", document.Artifact.TargetAppsTested);
        Assert.Contains("Chromium browsers", document.Artifact.TargetAppsTested);
        Assert.Contains("Lumiere v0.1.0", document.Artifact.BuildCommit, StringComparison.Ordinal);
        Assert.Equal("HDR Display", document.Artifact.TargetHdrEvidence!.TargetDisplayName);
        Assert.Equal(0, document.Artifact.TargetHdrEvidence.Left);
        Assert.Equal(0, document.Artifact.TargetHdrEvidence.Top);
        Assert.Equal(3840, document.Artifact.TargetHdrEvidence.Width);
        Assert.Equal(2160, document.Artifact.TargetHdrEvidence.Height);
        Assert.Equal("DesktopBounds", document.Artifact.TargetHdrEvidence.MatchKind);
        Assert.Equal("RgbFullG2084NoneP2020", document.Artifact.TargetHdrEvidence.ColorSpace);
        Assert.Contains("REPLACE_WITH_OBSERVED_TARGET_HDR_STATE", document.Artifact.TargetHdrEvidence.HdrState, StringComparison.Ordinal);

        var profileRecord = Assert.Single(document.Artifact.OutputProfileRecords);
        Assert.Equal(OutputProfileKind.Hdr10Pq, profileRecord.ProfileKind);
        Assert.Equal(["Folder"], profileRecord.OutputTargetsCovered);
        Assert.NotNull(profileRecord.FormatContract);
        Assert.Equal(OutputMetadataPolicy.AttachHdr10StaticMetadata, profileRecord.FormatContract!.MetadataPolicy);
        Assert.Equal(OutputTransferFunction.PqSt2084, profileRecord.FormatContract.TransferFunction);
        Assert.All(
            profileRecord.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.HdrPreservationStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.Hdr10MetadataStatus);
            });
    }

    [Fact]
    public void Create_KeepsClipboardSrgbDraftScopedToCompatibilityOutput()
    {
        var request = new OutputValidationDraftRequest(
            "v0.1.0",
            OutputTarget.Clipboard,
            OutputProfileContract.SrgbCompatibilityPng,
            CaptureSessionState.Idle());

        var document = OutputValidationDraftFactory.Create(
            request,
            new DateTimeOffset(2026, 06, 22, 10, 30, 00, TimeSpan.FromHours(8)));

        Assert.Equal(["Clipboard"], document.Artifact.OutputTargetsTested);
        Assert.Contains("REL-OUT-01", document.Artifact.ChecklistIdsCovered);
        Assert.DoesNotContain("REL-HDR-04", document.Artifact.ChecklistIdsCovered);

        var profileRecord = Assert.Single(document.Artifact.OutputProfileRecords);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, profileRecord.ProfileKind);
        Assert.Equal(OutputMetadataPolicy.NoHdrMetadata, profileRecord.FormatContract!.MetadataPolicy);
        Assert.All(
            profileRecord.ViewerEvidence,
            viewer =>
            {
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.ArtifactHandlingStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotRun, viewer.VisualMatchStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotApplicable, viewer.HdrPreservationStatus);
                Assert.Equal(OutputCompatibilityEvidenceStatus.NotApplicable, viewer.Hdr10MetadataStatus);
            });
    }
}
