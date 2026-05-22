using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class SettingsPanelProjectionTests
{
    [Fact]
    public void Project_UsesNotAssignedFallbackForShortcutRows()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.Equal("Not assigned", projection.FullscreenShortcut.DisplayValue);
        Assert.Equal("Not assigned", projection.RegionShortcut.DisplayValue);
    }

    [Fact]
    public void Project_UsesConfiguredShortcutValuesSeparately()
    {
        var settings = new TestSettingsProvider
        {
            FullscreenShortcut = " Ctrl+Shift+F ",
            RegionShortcut = "Ctrl+Shift+R",
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.Equal("Ctrl+Shift+F", projection.FullscreenShortcut.DisplayValue);
        Assert.Equal("Ctrl+Shift+R", projection.RegionShortcut.DisplayValue);
    }

    [Fact]
    public void Project_MarksShortcutRowsReadOnlyAndPendingRegistration()
    {
        var projection = SettingsPanelProjection.Project(new TestSettingsProvider(), CreateState());

        Assert.True(projection.FullscreenShortcut.IsReadOnly);
        Assert.True(projection.FullscreenShortcut.IsPendingRegistration);
        Assert.Equal("Global registration arrives in Epic 7", projection.FullscreenShortcut.PendingReason);
        Assert.True(projection.RegionShortcut.IsReadOnly);
        Assert.True(projection.RegionShortcut.IsPendingRegistration);
        Assert.Equal("Global registration arrives in Epic 7", projection.RegionShortcut.PendingReason);
    }

    [Fact]
    public void Project_ReflectsHdrAlertPreference()
    {
        var settings = new TestSettingsProvider
        {
            HdrAlertsEnabled = false,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.HdrAlertsEnabled);
        Assert.False(projection.OptionalHdrAlertChromeEnabled);
    }

    [Theory]
    [InlineData(OutputTarget.Clipboard, "Clipboard", true, false, false)]
    [InlineData(OutputTarget.Folder, "Folder", false, true, false)]
    [InlineData(OutputTarget.Both, "Both", false, false, true)]
    public void Project_ReflectsReadOnlyOutputTargetDisplay(
        OutputTarget target,
        string displayValue,
        bool clipboardSelected,
        bool folderSelected,
        bool bothSelected)
    {
        var settings = new TestSettingsProvider
        {
            OutputTarget = target,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.True(projection.Output.IsReadOnly);
        Assert.Equal(displayValue, projection.Output.DisplayValue);
        Assert.Equal(clipboardSelected, projection.Output.IsClipboardSelected);
        Assert.Equal(folderSelected, projection.Output.IsFolderSelected);
        Assert.Equal(bothSelected, projection.Output.IsBothSelected);
    }

    [Fact]
    public void Project_ReflectsReadOnlyTimestampAndCopyAsImageDisplayState()
    {
        var settings = new TestSettingsProvider
        {
            TimestampNaming = false,
            CopyAsImage = false,
        };

        var projection = SettingsPanelProjection.Project(settings, CreateState());

        Assert.False(projection.TimestampNaming);
        Assert.False(projection.CopyAsImage);
    }

    [Fact]
    public void Project_DisabledHdrAlertsPreserveTypedTrustProjection()
    {
        var settings = new TestSettingsProvider
        {
            HdrAlertsEnabled = false,
        };
        var state = CaptureSessionState.Failed(
            CreateTarget(),
            PreviewReadinessStatus.Failed(
                PreviewReadinessStage.Presentation,
                "Preview failed",
                "Presentation failure remains diagnostic state."));

        var projection = SettingsPanelProjection.Project(settings, state);

        Assert.Equal("HDR unavailable", projection.MainPanel.TrustLabel);
        Assert.Equal(MainPanelTrustSeverity.Error, projection.MainPanel.TrustSeverity);
        Assert.False(projection.OptionalHdrAlertChromeEnabled);
    }

    private static CaptureSessionState CreateState() =>
        CaptureSessionState.Capturing(
            CreateTarget(),
            PreviewReadinessStatus.Ready("HDR-ready", "Test readiness."));

    private static CaptureTarget CreateTarget() =>
        CaptureTarget.CreateForTest(
            new SizeInt32
            {
                Width = 1920,
                Height = 1080,
            },
            "Test Display",
            CaptureTargetKind.Display);

    private sealed class TestSettingsProvider : ISettingsProvider
    {
        public OutputTarget OutputTarget { get; init; } = OutputTarget.Clipboard;

        public string? SavePath { get; init; }

        public bool TimestampNaming { get; init; } = true;

        public bool CopyAsImage { get; init; } = true;

        public bool HdrAlertsEnabled { get; init; } = true;

        public string FullscreenShortcut { get; init; } = string.Empty;

        public string RegionShortcut { get; init; } = string.Empty;
    }
}
