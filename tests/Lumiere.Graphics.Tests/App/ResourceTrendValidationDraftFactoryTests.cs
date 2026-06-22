using Lumiere.App;
using Lumiere.Capture;
using Lumiere.Graphics.Hdr;
using Lumiere.Graphics.Output;
using Windows.Graphics;
using Xunit;

namespace Lumiere.Graphics.Tests.App;

public sealed class ResourceTrendValidationDraftFactoryTests
{
    [Fact]
    public void Create_PrefillsCurrentSessionContextIntoResourceTrendTemplate()
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
                "IDXGISwapChain3.SetColorSpace1 set RgbFullG2084NoneP2020; display match=DesktopBounds."));
        var request = new ResourceTrendValidationDraftRequest(
            "2.3.4+72c3be7",
            OutputTarget.Both,
            session,
            4242,
            "& \"C:\\Validation\\collect-resource-trend-samples.ps1\" -ProcessId 4242 -DurationSeconds 900 -SampleIntervalSeconds 5 -OutputDirectory \"C:\\Validation\\resource-trends\"",
            new OutputValidationCurrentSessionHint(
                "NVIDIA RTX 5080",
                ["150%"],
                "2 displays; active target HDR Display at 0,0 3840x2160"));

        var document = ResourceTrendValidationDraftFactory.Create(
            request,
            "C:\\Validation",
            new DateTimeOffset(2026, 06, 23, 11, 30, 00, TimeSpan.FromHours(8)),
            """
            # Resource Trend Validation Session Template

            ## Session Metadata

            - Date:
            - Tester:
            - Build / commit:
            - Windows version:
            - Device:
            - GPU:
            - Display setup:
            - HDR state:
            - DPI scale(s):
            - Lumiere process ID:
            - Output configuration:

            ## Sampler Configuration

            - Command:
            - Duration seconds:
            - Sample interval seconds:
            - Output directory:
            - CSV path:
            - Summary JSON path:
            - GPU counter availability:

            ## Checklist Rows Covered

            - `REL-STAB-01`:
            - `REL-STAB-02`:
            - `REL-STAB-03`:
            - `REL-STAB-04`:
            - Public gate `Long-run lifecycle evidence`:

            ## Final Result

            - Session classification: PASS / PASS with limitation / FAIL / NOT RUN
            - Release impact:
            - Known limitations:
            - Follow-up stories / issues:
            """);

        Assert.Contains("- Date: 2026-06-23", document, StringComparison.Ordinal);
        Assert.Contains("- Build / commit: 72c3be7 (app version Lumiere v2.3.4+72c3be7)", document, StringComparison.Ordinal);
        Assert.Contains("- GPU: REPLACE_WITH_GPU_MODEL_AND_DRIVER (current session: NVIDIA RTX 5080)", document, StringComparison.Ordinal);
        Assert.Contains("- Display setup: REPLACE_WITH_FULL_DISPLAY_SETUP (active target: HDR Display) (current session: 2 displays; active target HDR Display at 0,0 3840x2160)", document, StringComparison.Ordinal);
        Assert.Contains("- HDR state: REPLACE_WITH_OBSERVED_WINDOWS_HDR_STATE (current session: HDR preview path is validated.)", document, StringComparison.Ordinal);
        Assert.Contains("- DPI scale(s): REPLACE_WITH_DPI_SCALE (current session: 150%)", document, StringComparison.Ordinal);
        Assert.Contains("- Lumiere process ID: 4242 (current session)", document, StringComparison.Ordinal);
        Assert.Contains("- Output configuration: Both", document, StringComparison.Ordinal);
        Assert.Contains("- Command: & \"C:\\Validation\\collect-resource-trend-samples.ps1\" -ProcessId 4242 -DurationSeconds 900 -SampleIntervalSeconds 5 -OutputDirectory \"C:\\Validation\\resource-trends\"", document, StringComparison.Ordinal);
        Assert.Contains("- Output directory: C:\\Validation\\resource-trends", document, StringComparison.Ordinal);
        Assert.Contains("- Session classification: NOT RUN", document, StringComparison.Ordinal);
    }
}
