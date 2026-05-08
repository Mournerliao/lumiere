using Windows.Foundation;

namespace Lumiere.Overlay.Crop;

public sealed class CropController
{
    public const double DefaultMinimumSize = 4;
    public const double DefaultHitTestDipSize = 8;

    private readonly double minimumSize;
    private CropSelection previousCommittedSelection = CropSelection.Empty;
    private CropSelection replacementGestureSelection = CropSelection.Empty;
    private Rect adjustmentStartRegion;
    private CropAdjustmentHandle adjustmentHandle = CropAdjustmentHandle.None;

    public CropController(double minimumSize = DefaultMinimumSize)
    {
        this.minimumSize = minimumSize > 0
            ? minimumSize
            : throw new ArgumentOutOfRangeException(nameof(minimumSize));
    }

    public CropSelection Selection { get; private set; } = CropSelection.Empty;

    public CropSelection DisplaySelection =>
        replacementGestureSelection.IsVisible
            ? replacementGestureSelection
            : Selection;

    public bool IsGestureActive =>
        Selection.IsGestureActive || replacementGestureSelection.IsGestureActive;

    public CropHitTestResult HitTest(Point point, double hitTestDipSize = DefaultHitTestDipSize)
    {
        if (Selection.Phase is not CropSelectionPhase.Active || !Selection.Geometry.IsValid)
        {
            return CropHitTestResult.None;
        }

        var region = Selection.Geometry.Region;
        var left = region.X;
        var top = region.Y;
        var right = region.X + region.Width;
        var bottom = region.Y + region.Height;
        var tolerance = Math.Max(1, hitTestDipSize);

        if (IsNear(point, left, top, tolerance))
        {
            return new CropHitTestResult(CropHitTestKind.Corner, CropAdjustmentHandle.TopLeft);
        }

        if (IsNear(point, right, top, tolerance))
        {
            return new CropHitTestResult(CropHitTestKind.Corner, CropAdjustmentHandle.TopRight);
        }

        if (IsNear(point, right, bottom, tolerance))
        {
            return new CropHitTestResult(CropHitTestKind.Corner, CropAdjustmentHandle.BottomRight);
        }

        if (IsNear(point, left, bottom, tolerance))
        {
            return new CropHitTestResult(CropHitTestKind.Corner, CropAdjustmentHandle.BottomLeft);
        }

        if (Between(point.X, left, right) && Math.Abs(point.Y - top) <= tolerance)
        {
            return new CropHitTestResult(CropHitTestKind.Edge, CropAdjustmentHandle.Top);
        }

        if (Between(point.X, left, right) && Math.Abs(point.Y - bottom) <= tolerance)
        {
            return new CropHitTestResult(CropHitTestKind.Edge, CropAdjustmentHandle.Bottom);
        }

        if (Between(point.Y, top, bottom) && Math.Abs(point.X - left) <= tolerance)
        {
            return new CropHitTestResult(CropHitTestKind.Edge, CropAdjustmentHandle.Left);
        }

        if (Between(point.Y, top, bottom) && Math.Abs(point.X - right) <= tolerance)
        {
            return new CropHitTestResult(CropHitTestKind.Edge, CropAdjustmentHandle.Right);
        }

        return Contains(region, point)
            ? new CropHitTestResult(CropHitTestKind.Inside, CropAdjustmentHandle.None)
            : CropHitTestResult.None;
    }

    public bool BeginGesture(Point start, Rect previewBounds)
    {
        if (IsGestureActive)
        {
            return false;
        }

        // MVP: handle/edge adjustment is disabled. Release-to-capture completes on first
        // pointer release, so there is no opportunity to adjust. Post-MVP, restore the
        // full HitTest path below to enable two-step crop (create → adjust → confirm).
        //
        // var hitTest = HitTest(start);
        // if (hitTest.StartsAdjustment)
        // {
        //     BeginAdjustment(hitTest.Handle);
        //     return true;
        // }

        // Preserve: clicking inside an active crop does nothing (prevents accidental replacement).
        if (Selection.Phase is CropSelectionPhase.Active
            && Selection.Geometry.IsValid
            && Contains(Selection.Geometry.Region, start))
        {
            return false;
        }

        if (Selection.Phase is CropSelectionPhase.Active)
        {
            BeginReplacement(start, previewBounds);
        }
        else
        {
            Begin(start, previewBounds);
        }

        return true;
    }

    public void Begin(Point start, Rect previewBounds)
    {
        previousCommittedSelection = Selection.Phase is CropSelectionPhase.Active
            ? Selection
            : CropSelection.Empty;
        replacementGestureSelection = CropSelection.Empty;
        adjustmentHandle = CropAdjustmentHandle.None;

        var geometry = CropGeometry.FromDrag(start, start, previewBounds, minimumSize);
        Selection = new CropSelection(CropSelectionPhase.Creating, geometry, start);
    }

    public void Update(Point current, Rect previewBounds)
    {
        if (replacementGestureSelection.IsCreating && replacementGestureSelection.DragStart is { } replacementStart)
        {
            var geometry = CropGeometry.FromDrag(replacementStart, current, previewBounds, minimumSize);
            replacementGestureSelection = replacementGestureSelection with { Geometry = geometry };
            return;
        }

        if (Selection.IsCreating && Selection.DragStart is { } start)
        {
            var geometry = CropGeometry.FromDrag(start, current, previewBounds, minimumSize);
            Selection = Selection with { Geometry = geometry };
            return;
        }

        if (Selection.IsAdjusting)
        {
            var geometry = CreateAdjustedGeometry(current, previewBounds);
            if (geometry.IsValid)
            {
                Selection = Selection with { Geometry = geometry };
            }
        }
    }

    public CropCommitResult Commit(Point current, Rect previewBounds)
    {
        if (replacementGestureSelection.IsCreating && replacementGestureSelection.DragStart is { } replacementStart)
        {
            var geometry = CropGeometry.FromDrag(replacementStart, current, previewBounds, minimumSize);
            if (!geometry.IsValid)
            {
                Selection = previousCommittedSelection;
                replacementGestureSelection = CropSelection.Empty;
                adjustmentHandle = CropAdjustmentHandle.None;
                return CropCommitResult.InvalidGeometry;
            }

            Selection = new CropSelection(CropSelectionPhase.Active, geometry, null);
            previousCommittedSelection = Selection;
            replacementGestureSelection = CropSelection.Empty;
            adjustmentHandle = CropAdjustmentHandle.None;
            return CropCommitResult.Activated;
        }

        if (Selection.IsCreating && Selection.DragStart is { } start)
        {
            var geometry = CropGeometry.FromDrag(start, current, previewBounds, minimumSize);
            if (!geometry.IsValid)
            {
                Selection = previousCommittedSelection;
                adjustmentHandle = CropAdjustmentHandle.None;
                return CropCommitResult.InvalidGeometry;
            }

            Selection = new CropSelection(CropSelectionPhase.Active, geometry, null);
            previousCommittedSelection = Selection;
            adjustmentHandle = CropAdjustmentHandle.None;
            return CropCommitResult.Activated;
        }

        if (!Selection.IsAdjusting)
        {
            return CropCommitResult.NoGesture;
        }

        var finalGeometry = CreateAdjustedGeometry(current, previewBounds);
        if (!finalGeometry.IsValid)
        {
            Selection = new CropSelection(
                CropSelectionPhase.Active,
                Selection.Geometry,
                null);
            previousCommittedSelection = Selection;
            adjustmentHandle = CropAdjustmentHandle.None;
            return CropCommitResult.InvalidGeometry;
        }

        Selection = new CropSelection(
            CropSelectionPhase.Active,
            finalGeometry,
            null);
        previousCommittedSelection = Selection;
        adjustmentHandle = CropAdjustmentHandle.None;
        return CropCommitResult.Adjusted;
    }

    public void Cancel()
    {
        if (Selection.IsCreating || Selection.IsAdjusting)
        {
            Selection = previousCommittedSelection;
        }

        replacementGestureSelection = CropSelection.Empty;
        adjustmentHandle = CropAdjustmentHandle.None;
    }

    public void Clear()
    {
        previousCommittedSelection = CropSelection.Empty;
        replacementGestureSelection = CropSelection.Empty;
        adjustmentHandle = CropAdjustmentHandle.None;
        Selection = CropSelection.Empty;
    }

    private void BeginReplacement(Point start, Rect previewBounds)
    {
        previousCommittedSelection = Selection;
        adjustmentHandle = CropAdjustmentHandle.None;

        var geometry = CropGeometry.FromDrag(start, start, previewBounds, minimumSize);
        replacementGestureSelection = new CropSelection(CropSelectionPhase.Creating, geometry, start);
    }

    private void BeginAdjustment(CropAdjustmentHandle handle)
    {
        previousCommittedSelection = Selection;
        adjustmentStartRegion = Selection.Geometry.Region;
        adjustmentHandle = handle;
        Selection = Selection with { Phase = CropSelectionPhase.Adjusting };
    }

    private CropGeometry CreateAdjustedGeometry(Point current, Rect previewBounds)
    {
        var left = adjustmentStartRegion.X;
        var top = adjustmentStartRegion.Y;
        var right = adjustmentStartRegion.X + adjustmentStartRegion.Width;
        var bottom = adjustmentStartRegion.Y + adjustmentStartRegion.Height;

        switch (adjustmentHandle)
        {
            case CropAdjustmentHandle.Left:
                left = current.X;
                break;
            case CropAdjustmentHandle.TopLeft:
                left = current.X;
                top = current.Y;
                break;
            case CropAdjustmentHandle.Top:
                top = current.Y;
                break;
            case CropAdjustmentHandle.TopRight:
                right = current.X;
                top = current.Y;
                break;
            case CropAdjustmentHandle.Right:
                right = current.X;
                break;
            case CropAdjustmentHandle.BottomRight:
                right = current.X;
                bottom = current.Y;
                break;
            case CropAdjustmentHandle.Bottom:
                bottom = current.Y;
                break;
            case CropAdjustmentHandle.BottomLeft:
                left = current.X;
                bottom = current.Y;
                break;
        }

        return CropGeometry.FromEdges(left, top, right, bottom, previewBounds, minimumSize);
    }

    private static bool IsNear(Point point, double x, double y, double tolerance) =>
        Math.Abs(point.X - x) <= tolerance && Math.Abs(point.Y - y) <= tolerance;

    private static bool Between(double value, double first, double second) =>
        value >= Math.Min(first, second) && value <= Math.Max(first, second);

    private static bool Contains(Rect region, Point point) =>
        point.X > region.X
        && point.X < region.X + region.Width
        && point.Y > region.Y
        && point.Y < region.Y + region.Height;
}
