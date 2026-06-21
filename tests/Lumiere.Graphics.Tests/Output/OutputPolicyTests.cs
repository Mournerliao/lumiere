using Lumiere.Graphics.Output;
using Xunit;

namespace Lumiere.Graphics.Tests.Output;

public sealed class OutputPolicyTests
{
    [Fact]
    public void Default_AttemptsClipboardOnly()
    {
        var policy = OutputPolicy.Default;

        Assert.True(policy.ShouldAttemptClipboard);
        Assert.False(policy.ShouldAttemptFolder);
    }

    [Theory]
    [InlineData(OutputTarget.Clipboard, true, true, false)]
    [InlineData(OutputTarget.Clipboard, false, false, false)]
    [InlineData(OutputTarget.Folder, true, false, true)]
    [InlineData(OutputTarget.Folder, false, false, true)]
    [InlineData(OutputTarget.Both, true, true, true)]
    [InlineData(OutputTarget.Both, false, false, true)]
    public void FromSettings_DerivesAttemptPolicy(
        OutputTarget target,
        bool copyAsImage,
        bool shouldAttemptClipboard,
        bool shouldAttemptFolder)
    {
        var policy = OutputPolicy.FromSettings(
            target,
            copyAsImage,
            " C:\\Captures ",
            timestampNaming: true,
            afterCaptureBehavior: "RevealInFolder",
            exportColorFormat: "sRGB");

        Assert.Equal(shouldAttemptClipboard, policy.ShouldAttemptClipboard);
        Assert.Equal(shouldAttemptFolder, policy.ShouldAttemptFolder);
        Assert.Equal("C:\\Captures", policy.SavePath);
        Assert.Equal("RevealInFolder", policy.AfterCaptureBehavior);
        Assert.Equal(OutputAfterCaptureAction.Reveal, policy.AfterCaptureAction);
        Assert.Equal("sRGB", policy.RequestedProfile.Label);
        Assert.Equal("sRGB", policy.EffectiveProfile.Label);
        Assert.False(policy.UsesCompatibilityProfileFallback);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromSettings_NormalizesBlankOptionalValues(string? value)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            value,
            timestampNaming: false,
            afterCaptureBehavior: value);

        Assert.Null(policy.SavePath);
        Assert.Null(policy.AfterCaptureBehavior);
        Assert.Equal(OutputAfterCaptureAction.None, policy.AfterCaptureAction);
    }

    [Theory]
    [InlineData("Open", OutputAfterCaptureAction.Open)]
    [InlineData("Reveal", OutputAfterCaptureAction.Reveal)]
    [InlineData("RevealInFolder", OutputAfterCaptureAction.Reveal)]
    [InlineData("Unsupported", OutputAfterCaptureAction.None)]
    public void FromSettings_MapsSupportedAfterCaptureActions(
        string value,
        OutputAfterCaptureAction expectedAction)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Folder,
            copyAsImage: true,
            savePath: "C:\\Captures",
            timestampNaming: true,
            afterCaptureBehavior: value);

        Assert.Equal(expectedAction, policy.AfterCaptureAction);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sRGB")]
    [InlineData("unknown")]
    public void FromSettings_UsesSrgbCompatibilityProfileForExecutableOutput(string? exportColorFormat)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Clipboard,
            copyAsImage: true,
            savePath: null,
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat);

        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.RequestedProfile.Kind);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.Equal(OutputFidelityMode.SdrCompatible, policy.EffectiveProfile.FidelityMode);
        Assert.True(policy.EffectiveProfile.IsExecutable);
        Assert.Contains("No HDR metadata", policy.EffectiveProfile.MetadataPolicy, StringComparison.OrdinalIgnoreCase);
        Assert.False(policy.EffectiveProfile.AllowsHdrPreservedClaim);
    }

    [Theory]
    [InlineData("HDR10", OutputProfileKind.Hdr10Pq)]
    [InlineData("P3", OutputProfileKind.DisplayP3)]
    [InlineData("wide", OutputProfileKind.DisplayP3)]
    public void FromSettings_KeepsUnsupportedProfilesNonExecutableAndFallsBackToSrgb(
        string exportColorFormat,
        OutputProfileKind requestedKind)
    {
        var policy = OutputPolicy.FromSettings(
            OutputTarget.Clipboard,
            copyAsImage: true,
            savePath: null,
            timestampNaming: true,
            afterCaptureBehavior: null,
            exportColorFormat);

        Assert.Equal(requestedKind, policy.RequestedProfile.Kind);
        Assert.False(policy.RequestedProfile.IsExecutable);
        Assert.Equal(OutputFidelityMode.Unvalidated, policy.RequestedProfile.FidelityMode);
        Assert.False(policy.RequestedProfile.AllowsHdrPreservedClaim);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
        Assert.True(policy.ShouldAttemptClipboard);
    }

    [Fact]
    public void UsesCompatibilityProfileFallback_WhenRequestedProfileIsExecutableButFormatContractIsIncomplete()
    {
        var policy = OutputPolicy.Default with
        {
            RequestedProfile = OutputProfileContract.Hdr10Pq with
            {
                IsExecutable = true,
                FidelityMode = OutputFidelityMode.HdrPreserved,
            },
        };

        Assert.False(policy.RequestedProfile.HasCompleteFormatContract);
        Assert.Equal(OutputProfileKind.SrgbCompatibilityPng, policy.EffectiveProfile.Kind);
        Assert.True(policy.UsesCompatibilityProfileFallback);
    }
}
