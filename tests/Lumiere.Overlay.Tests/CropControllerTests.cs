using Lumiere.Overlay.Crop;
using Windows.Foundation;
using Xunit;

namespace Lumiere.Overlay.Tests;

public sealed class CropControllerTests
{
    [Fact]
    public void DragSequence_CommitsActiveCropOnRelease()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(0, 0, 200, 100);

        controller.Begin(new Point(20, 10), bounds);
        controller.Update(new Point(120, 90), bounds);
        controller.Commit(new Point(160, 95), bounds);

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 140, 85), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void DragSequence_ClampsCoordinatesLeavingPreviewBounds()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(10, 20, 100, 80);

        controller.Begin(new Point(20, 30), bounds);
        controller.Update(new Point(200, 200), bounds);

        Assert.Equal(CropSelectionPhase.Creating, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 30, 90, 70), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void Commit_ReturnsToEmptyWhenCropIsTooSmall()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(0, 0, 200, 100);

        controller.Begin(new Point(20, 10), bounds);
        controller.Commit(new Point(21, 11), bounds);

        Assert.Equal(CropSelectionPhase.Empty, controller.Selection.Phase);
        Assert.True(controller.Selection.Geometry.IsEmpty);
    }

    [Fact]
    public void CancelDuringCreation_RestoresPreviousActiveCrop()
    {
        var controller = new CropController(minimumSize: 4);
        var bounds = new Rect(0, 0, 200, 100);
        controller.Begin(new Point(10, 10), bounds);
        controller.Commit(new Point(80, 70), bounds);

        controller.Begin(new Point(100, 20), bounds);
        controller.Update(new Point(140, 60), bounds);
        controller.Cancel();

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(10, 10, 70, 60), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void UpdateWithoutCreation_DoesNotChangeSelection()
    {
        var controller = new CropController();

        controller.Update(new Point(20, 20), new Rect(0, 0, 100, 100));

        Assert.Equal(CropSelectionPhase.Empty, controller.Selection.Phase);
    }

    [Fact]
    public void HitTest_ReturnsCornerBeforeEdges()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));

        var result = controller.HitTest(new Point(22, 12));

        Assert.Equal(CropHitTestKind.Corner, result.Kind);
        Assert.Equal(CropAdjustmentHandle.TopLeft, result.Handle);
    }

    [Fact]
    public void HitTest_ReturnsEdgeWithinStableDipArea()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));

        var result = controller.HitTest(new Point(70, 13));

        Assert.Equal(CropHitTestKind.Edge, result.Kind);
        Assert.Equal(CropAdjustmentHandle.Top, result.Handle);
    }

    [Fact]
    public void BeginGesture_InsideActiveCropDoesNotStartGesture()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));

        var started = controller.BeginGesture(new Point(70, 50), new Rect(0, 0, 200, 100));

        Assert.False(started);
        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void AdjustingRightEdge_UpdatesActiveCropWithoutMovingOtherEdges()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        Assert.True(controller.BeginGesture(new Point(120, 50), bounds));
        controller.Update(new Point(150, 60), bounds);
        controller.Commit(new Point(150, 60), bounds);

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 130, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void AdjustingCornerAcrossOppositeCorner_NormalizesGeometry()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        Assert.True(controller.BeginGesture(new Point(20, 10), bounds));
        controller.Update(new Point(140, 95), bounds);
        controller.Commit(new Point(140, 95), bounds);

        Assert.Equal(new Rect(120, 90, 20, 5), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void AdjustingBeyondBounds_ClampsGeometry()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 130, 100);

        Assert.True(controller.BeginGesture(new Point(120, 50), bounds));
        controller.Update(new Point(200, 50), bounds);
        controller.Commit(new Point(200, 50), bounds);

        Assert.Equal(new Rect(20, 10, 110, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void TooSmallAdjustment_KeepsLastValidCrop()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80), minimumSize: 10);
        var bounds = new Rect(0, 0, 200, 100);

        Assert.True(controller.BeginGesture(new Point(120, 50), bounds));
        controller.Update(new Point(22, 50), bounds);
        controller.Commit(new Point(22, 50), bounds);

        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void OutsideActiveCrop_ReplacesPreviousCropOnlyAfterValidCommit()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        Assert.True(controller.BeginGesture(new Point(140, 20), bounds));
        controller.Update(new Point(180, 70), bounds);

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
        Assert.Equal(CropSelectionPhase.Creating, controller.DisplaySelection.Phase);
        Assert.Equal(new Rect(140, 20, 40, 50), controller.DisplaySelection.Geometry.Region);

        controller.Commit(new Point(180, 70), bounds);

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(140, 20, 40, 50), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void OutsideActiveCrop_InvalidRecreationRestoresPreviousCrop()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        Assert.True(controller.BeginGesture(new Point(140, 20), bounds));
        controller.Commit(new Point(141, 21), bounds);

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
    }

    [Fact]
    public void CancelDuringAdjustment_RestoresPreviousActiveCrop()
    {
        var controller = CreateActiveController(new Rect(20, 10, 100, 80));
        var bounds = new Rect(0, 0, 200, 100);

        Assert.True(controller.BeginGesture(new Point(120, 50), bounds));
        controller.Update(new Point(150, 50), bounds);
        controller.Cancel();

        Assert.Equal(CropSelectionPhase.Active, controller.Selection.Phase);
        Assert.Equal(new Rect(20, 10, 100, 80), controller.Selection.Geometry.Region);
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
