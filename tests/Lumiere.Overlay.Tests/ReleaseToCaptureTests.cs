using Lumiere.Overlay.Crop;
using Windows.Foundation;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class ReleaseToCaptureTests
{
    [Fact]
    public void CommitResult_Activated_WhenNewCropCreatedByDrag()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(0, 0, 200, 100);

        controller.BeginGesture(new Point(20, 10), bounds);
        controller.Update(new Point(120, 90), bounds);
        var result = controller.Commit(new Point(160, 95), bounds);

        Assert.Equal(CropCommitResult.Activated, result);
        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
    }

    [Fact]
    public void CommitResult_Adjusted_WhenExistingCropResized()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        controller.BeginGesture(new Point(120, 50), bounds);
        controller.Update(new Point(150, 60), bounds);
        var result = controller.Commit(new Point(150, 60), bounds);

        Assert.Equal(CropCommitResult.Adjusted, result);
        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
    }

    [Fact]
    public void CommitResult_InvalidGeometry_WhenCropTooSmall()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(0, 0, 200, 100);

        controller.BeginGesture(new Point(20, 10), bounds);
        var result = controller.Commit(new Point(21, 11), bounds);

        Assert.Equal(CropCommitResult.InvalidGeometry, result);
        Assert.Equal(CropSelectionPhase.Empty, controller.Selection.Phase);
    }

    [Fact]
    public void CommitResult_NoGesture_WhenNoGestureActive()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(0, 0, 200, 100);

        var result = controller.Commit(new Point(50, 50), bounds);

        Assert.Equal(CropCommitResult.NoGesture, result);
        Assert.Equal(CropSelectionPhase.Empty, controller.Selection.Phase);
    }

    [Fact]
    public void CommitResult_Activated_WhenReplacementGestureIsValid()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        controller.BeginGesture(new Point(140, 20), bounds);
        controller.Update(new Point(180, 70), bounds);
        var result = controller.Commit(new Point(180, 70), bounds);

        Assert.Equal(CropCommitResult.Activated, result);
        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(140, 20, 40, 50), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void CommitResult_InvalidGeometry_WhenReplacementGestureTooSmall()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        controller.BeginGesture(new Point(140, 20), bounds);
        var result = controller.Commit(new Point(141, 21), bounds);

        Assert.Equal(CropCommitResult.InvalidGeometry, result);
        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void CommitResult_InvalidGeometry_WhenAdjustmentTooSmall()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80), minimumSize: 10);
        var bounds = new Rect(0, 0, 200, 100);

        controller.BeginGesture(new Point(120, 50), bounds);
        controller.Update(new Point(22, 50), bounds);
        var result = controller.Commit(new Point(22, 50), bounds);

        Assert.Equal(CropCommitResult.InvalidGeometry, result);
        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void CanConfirm_ReturnsTrue_WhenActiveAndHdrReady()
    {
        var selection = new CropSelection(
            CropSelectionPhase.Active,
            CropGeometry.FromEdges(10, 20, 50, 60, new Rect(0, 0, 100, 100)),
            null);

        var result = ConfirmedCaptureSelection.CanConfirm(selection, OverlayDisplayStatus.HdrReady);

        Assert.True(result);
    }

    [Fact]
    public void CanConfirm_ReturnsTrue_WhenActiveAndDegradedPreview()
    {
        var selection = new CropSelection(
            CropSelectionPhase.Active,
            CropGeometry.FromEdges(10, 20, 50, 60, new Rect(0, 0, 100, 100)),
            null);

        var result = ConfirmedCaptureSelection.CanConfirm(selection, OverlayDisplayStatus.DegradedPreview);

        Assert.True(result);
    }

    [Fact]
    public void CanConfirm_ReturnsFalse_WhenCreating()
    {
        var selection = new CropSelection(
            CropSelectionPhase.Creating,
            CropGeometry.FromEdges(10, 20, 50, 60, new Rect(0, 0, 100, 100)),
            new Point(10, 20));

        var result = ConfirmedCaptureSelection.CanConfirm(selection, OverlayDisplayStatus.HdrReady);

        Assert.False(result);
    }

    [Fact]
    public void CanConfirm_ReturnsFalse_WhenEmpty()
    {
        var result = ConfirmedCaptureSelection.CanConfirm(CropSelection.Empty, OverlayDisplayStatus.HdrReady);

        Assert.False(result);
    }

    [Theory]
    [InlineData(OverlayDisplayStatus.UnsupportedCapture)]
    [InlineData(OverlayDisplayStatus.PreviewFailed)]
    [InlineData(OverlayDisplayStatus.Closing)]
    [InlineData(OverlayDisplayStatus.Disposed)]
    public void CanConfirm_ReturnsFalse_WhenStatusIsUnavailable(OverlayDisplayStatus status)
    {
        var selection = new CropSelection(
            CropSelectionPhase.Active,
            CropGeometry.FromEdges(10, 20, 50, 60, new Rect(0, 0, 100, 100)),
            null);

        var result = ConfirmedCaptureSelection.CanConfirm(selection, status);

        Assert.False(result);
    }

    private static CropController CreateActiveController(Rect region, double minimumSize = 4)
    {
        var controller = new CropController(minimumSize);
        var bounds = new Rect(0, 0, 200, 100);
        controller.Begin(new Point(region.X, region.Y), bounds);
        controller.Commit(new Point(region.X + region.Width, region.Y + region.Height), bounds);
        return controller;
    }
}
