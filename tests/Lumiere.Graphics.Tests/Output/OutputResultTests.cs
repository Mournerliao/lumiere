using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputResultTests
{
    [Fact]
    public void ClipboardSkipped_RecordsPerTargetSkippedState()
    {
        var result = OutputResult.ClipboardSkipped("Clipboard output skipped by settings");

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Skipped, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Skipped, result.FolderOutcome);
        Assert.Single(result.Targets);
        Assert.Equal("Clipboard output skipped by settings", result.UserMessage);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, result.EffectiveProfile.Kind);
        Assert.Equal(OutputFidelityMode.SdrCompatible, result.EffectiveProfile.FidelityMode);
        Assert.False(result.EffectiveProfile.AllowsHdrPreservedClaim);
    }

    [Fact]
    public void FromTargets_ReportsPartialSuccessWithoutStringParsing()
    {
        var result = OutputResult.FromTargets(
            OutputTargetResult.Success(OutputTarget.Clipboard, "Copied to clipboard"),
            OutputTargetResult.Failed(OutputTarget.Folder, "Failed to save file", "Access denied"));

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputOutcome.Success, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Failed, result.FolderOutcome);
        Assert.Equal("Output partially complete", result.UserMessage);
        Assert.Contains("Access denied", result.TechnicalDetail);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, result.EffectiveProfile.Kind);
    }

    [Fact]
    public void FromTargets_ReportsAllSkippedAsSkipped()
    {
        var result = OutputResult.FromTargets(
            OutputTargetResult.Skipped(OutputTarget.Clipboard, "Clipboard disabled"),
            OutputTargetResult.Skipped(OutputTarget.Folder, "Folder output not implemented"));

        Assert.False(result.IsSuccess);
        Assert.Equal(OutputOutcome.Skipped, result.ClipboardOutcome);
        Assert.Equal(OutputOutcome.Skipped, result.FolderOutcome);
        Assert.Equal("Output skipped", result.UserMessage);
    }

    [Fact]
    public void FromTargets_RequiresAtLeastOneTarget()
    {
        Assert.Throws<ArgumentException>(() => OutputResult.FromTargets(Array.Empty<OutputTargetResult>()));
    }

    [Fact]
    public void WithEffectiveProfile_AttachesExecutableContractWithoutChangingArtifactOutcome()
    {
        var requested = OutputProfileContract.FromSettingsValue("HDR10");
        var result = OutputResult.ClipboardSuccess(1024)
            .WithRequestedProfile(requested);

        Assert.True(result.IsSuccess);
        Assert.Equal(OutputProfileKind.Hdr10Pq, result.RequestedProfile.Kind);
        Assert.False(result.RequestedProfile.IsExecutable);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, result.EffectiveProfile.Kind);
        Assert.True(result.UsesCompatibilityProfileFallback);
        Assert.False(result.EffectiveProfile.AllowsHdrPreservedClaim);
        Assert.DoesNotContain("HDR-preserved", result.UserMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithOutputPolicy_RecordsRuntimeEffectiveProfile()
    {
        var requested = OutputProfileContract.Hdr10Pq with
        {
            FormatContract = CompleteHdr10Contract,
        };
        var policy = OutputPolicy.Default with
        {
            RequestedProfile = requested,
            ExecutionCapabilities = OutputProfileExecutionCapabilities.Create(
                OutputProfileExecutionCapability.SrgbCompatibility,
                ImplementedHdr10PreservedCapability),
        };

        var result = OutputResult.ClipboardSuccess(1024)
            .WithOutputPolicy(policy);

        Assert.Equal(OutputProfileKind.Hdr10Pq, result.RequestedProfile.Kind);
        Assert.Equal(OutputProfileKind.Hdr10Pq, result.EffectiveProfile.Kind);
        Assert.True(result.EffectiveProfile.IsExecutable);
        Assert.Equal(OutputFidelityMode.HdrPreserved, result.EffectiveProfile.FidelityMode);
        Assert.False(result.UsesCompatibilityProfileFallback);
    }

    private static OutputFormatContract CompleteHdr10Contract { get; } =
        new(
            OutputPixelFormat.R16G16B16A16Float,
            OutputPixelFormat.R16G16B16A16Float,
            OutputTransferFunction.PqSt2084,
            OutputColorPrimaries.Bt2020,
            OutputConversionPolicy.PreserveHdrWithDefinedToneMapping,
            OutputMetadataPolicy.AttachHdr10StaticMetadata,
            OutputTargetAppAssumption.RequiresHdrViewerValidation);

    private static OutputProfileExecutionCapability ImplementedHdr10PreservedCapability { get; } =
        new(
            OutputProfileKind.Hdr10Pq,
            OutputFidelityMode.HdrPreserved,
            OutputArtifactEncoderImplementation.Implemented);
}
