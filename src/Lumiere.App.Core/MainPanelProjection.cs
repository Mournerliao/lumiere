using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;

namespace Lumiere.App;

public sealed record MainPanelProjection(
    bool CanStartCapture,
    string ActionTitle,
    string TrustLabel,
    string TrustMessage,
    MainPanelTrustIcon TrustIcon,
    MainPanelTrustSeverity TrustSeverity,
    OutputProfileProjection OutputProfile,
    FidelityClaimProjection FidelityClaim,
    OutputResultProjection OutputResult,
    string ReleaseTarget,
    string AlertMessage,
    bool HasAlert)
{
    public static MainPanelProjection Project(
        CaptureSessionState state,
        OutputResult? outputResult = null,
        bool hdrAlertsEnabled = false,
        string? exportColorFormat = null,
        IEnumerable<OutputValidationSessionArtifact>? validationArtifacts = null,
        OutputProfileExecutionCapabilities? executionCapabilities = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var capabilities = executionCapabilities ?? OutputProfileExecutionCapabilities.CompatibilityOnly;
        var canStartCapture = state.Status is CaptureSessionStatus.Idle
            or CaptureSessionStatus.Unsupported
            or CaptureSessionStatus.Failed;
        var actionTitle = state.Status switch
        {
            CaptureSessionStatus.SelectingTarget => "Preparing capture...",
            CaptureSessionStatus.Initializing => "Starting preview...",
            CaptureSessionStatus.Capturing => "Capturing...",
            CaptureSessionStatus.Degraded => "Capture degraded",
            CaptureSessionStatus.Unsupported => "Capture unavailable",
            CaptureSessionStatus.Failed => "Capture failed",
            CaptureSessionStatus.Disposed => "Preview stopped",
            _ => "Ready to capture",
        };

        var trust = MapTrust(state.Readiness, outputResult);
        var alertMessage = MapAlertMessage(state.Readiness, outputResult, hdrAlertsEnabled);
        var selectedContract = OutputProfileContract.FromSettingsValue(exportColorFormat);
        var outputProfile = validationArtifacts is null
            ? PerfectHdrFidelityProjection.ProjectOutputProfile(selectedContract, state.Readiness, capabilities)
            : PerfectHdrFidelityProjection.ProjectOutputProfile(selectedContract, validationArtifacts, state.Readiness, capabilities);
        var outputResultProjection = outputResult is null
            ? OutputResultProjection.Project(outputResult, outputProfile.FidelityClaim)
            : OutputResultProjection.Project(outputResult);

        return new MainPanelProjection(
            canStartCapture,
            actionTitle,
            trust.Label,
            string.IsNullOrWhiteSpace(state.UserFacingReason)
                ? "HDR preview status is being checked."
                : state.UserFacingReason,
            trust.Icon,
            trust.Severity,
            outputProfile,
            outputProfile.FidelityClaim,
            outputResultProjection,
            PerfectHdrFidelityProjection.ReleaseTarget,
            alertMessage,
            !string.IsNullOrEmpty(alertMessage));
    }

    private static (string Label, MainPanelTrustIcon Icon, MainPanelTrustSeverity Severity) MapTrust(
        PreviewReadinessStatus readiness,
        OutputResult? outputResult)
    {
        if (outputResult is not null)
        {
            var hasFailure = outputResult.Targets.Any(t => t.Outcome == OutputOutcome.Failed);
            if (outputResult.IsSuccess && !hasFailure)
            {
                return ("Output complete", MainPanelTrustIcon.InfoCircle, MainPanelTrustSeverity.Info);
            }

            var isAllSkipped = outputResult.Targets.All(t => t.Outcome == OutputOutcome.Skipped);
            var label = isAllSkipped ? "Output skipped" : outputResult.UserMessage ?? "Output error";
            return (label, MainPanelTrustIcon.WarningCircle, MainPanelTrustSeverity.Warning);
        }

        if (readiness.Reason is PreviewReadinessReason.TargetDisplayUnresolved)
        {
            return ("HDR unvalidated", MainPanelTrustIcon.WarningCircle, MainPanelTrustSeverity.Warning);
        }

        return readiness.State switch
        {
            PreviewReadinessState.Ready => ("HDR Ready", MainPanelTrustIcon.CheckmarkCircle, MainPanelTrustSeverity.Success),
            PreviewReadinessState.Degraded => ("Enable HDR", MainPanelTrustIcon.Desktop, MainPanelTrustSeverity.Warning),
            PreviewReadinessState.Unsupported => ("HDR unavailable", MainPanelTrustIcon.ErrorCircle, MainPanelTrustSeverity.Error),
            PreviewReadinessState.Failed => ("Preview failed", MainPanelTrustIcon.ErrorBadge, MainPanelTrustSeverity.Error),
            _ => ("Checking HDR", MainPanelTrustIcon.Clock, MainPanelTrustSeverity.Neutral),
        };
    }

    private static string MapAlertMessage(
        PreviewReadinessStatus readiness,
        OutputResult? outputResult,
        bool hdrAlertsEnabled)
    {
        return AlertMapping.Classify(readiness, outputResult, hdrAlertsEnabled) switch
        {
            AlertMapping.AlertSeverity.TargetDisplayUnresolved => "HDR readiness is unvalidated for the selected capture target.",
            AlertMapping.AlertSeverity.Degraded => "Enable HDR in Windows Display settings for best capture quality.",
            AlertMapping.AlertSeverity.Unsupported => "HDR capture is not supported on this display.",
            AlertMapping.AlertSeverity.Failed => "Preview failed. Capture may not produce HDR-quality output.",
            _ => string.Empty,
        };
    }

    public static string FormatShortcut(string? shortcut) =>
        string.IsNullOrWhiteSpace(shortcut)
            ? "Not assigned"
            : shortcut.Trim();
}

public enum MainPanelTrustIcon
{
    Clock = 0,
    CheckmarkCircle,
    Desktop,
    ErrorCircle,
    ErrorBadge,
    WarningCircle,
    InfoCircle,
}

public enum MainPanelTrustSeverity
{
    Neutral = 0,
    Success,
    Warning,
    Error,
    Info,
}
