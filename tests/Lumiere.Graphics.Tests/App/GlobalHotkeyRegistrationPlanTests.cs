using Lumiere.App;
using Lumiere.Graphics.Output;
using Lumiere.Settings;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class GlobalHotkeyRegistrationPlanTests
{
    [Fact]
    public void Project_ValidShortcuts_AreRegistrableWithVirtualKeys()
    {
        var plan = GlobalHotkeyRegistrationPlan.Project(new TestSettingsProvider
        {
            FullscreenShortcut = " Ctrl + Shift + F ",
            RegionShortcut = "Alt+F12",
        });

        Assert.True(plan.Fullscreen.CanRegister);
        Assert.Equal("Ctrl+Shift+F", plan.Fullscreen.DisplayValue);
        Assert.Equal('F', plan.Fullscreen.Gesture!.VirtualKey);
        Assert.True(plan.Fullscreen.Gesture.Control);
        Assert.True(plan.Fullscreen.Gesture.Shift);
        Assert.True(plan.Region.CanRegister);
        Assert.Equal(0x7B, plan.Region.Gesture!.VirtualKey);
        Assert.True(plan.Region.Gesture.Alt);
    }

    [Fact]
    public void Project_UnassignedShortcuts_AreSkippedSafely()
    {
        var plan = GlobalHotkeyRegistrationPlan.Project(new TestSettingsProvider());

        Assert.False(plan.Fullscreen.CanRegister);
        Assert.Equal("Not assigned", plan.Fullscreen.DisplayValue);
        Assert.Contains("skipped", plan.Fullscreen.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(plan.RegistrableBindings());
    }

    [Theory]
    [InlineData("F")]
    [InlineData("Ctrl+Shift")]
    [InlineData("Ctrl+Mouse1")]
    [InlineData("Ctrl+F+R")]
    public void Project_InvalidShortcuts_AreSkippedWithDiagnostic(string shortcut)
    {
        var plan = GlobalHotkeyRegistrationPlan.Project(new TestSettingsProvider
        {
            FullscreenShortcut = shortcut,
        });

        Assert.False(plan.Fullscreen.CanRegister);
        Assert.NotEmpty(plan.Fullscreen.StatusMessage);
    }

    [Fact]
    public void Project_ConflictingShortcuts_SkipsBothRegistrations()
    {
        var plan = GlobalHotkeyRegistrationPlan.Project(new TestSettingsProvider
        {
            FullscreenShortcut = "Ctrl+Shift+F",
            RegionShortcut = "Control + Shift + F",
        });

        Assert.False(plan.Fullscreen.CanRegister);
        Assert.False(plan.Region.CanRegister);
        Assert.Contains("conflicts", plan.Fullscreen.StatusMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(plan.RegistrableBindings());
    }

    private sealed class TestSettingsProvider : ISettingsProvider
    {
        public OutputTarget OutputTarget { get; init; } = OutputTarget.Clipboard;

        public string? SavePath { get; init; }

        public bool TimestampNaming { get; init; } = true;

        public bool CopyAsImage { get; init; } = true;

        public bool HdrAlertsEnabled { get; init; } = true;

        public string FullscreenShortcut { get; init; } = string.Empty;

        public string RegionShortcut { get; init; } = string.Empty;

        public AfterCaptureBehavior AfterCaptureBehavior { get; init; } = AfterCaptureBehavior.None;
    }
}
