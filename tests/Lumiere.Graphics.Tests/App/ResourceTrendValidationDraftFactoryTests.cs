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

    [Fact]
    public void Create_ImportsLatestSamplerSummaryWhileKeepingHumanClassificationExplicit()
    {
        var request = new ResourceTrendValidationDraftRequest(
            "2.3.4+72c3be7",
            OutputTarget.Folder,
            CaptureSessionState.Idle(),
            4242);
        var summary = ResourceTrendSummaryArtifact.FromJson(
            CreateSummaryJson(),
            "C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000-summary.json") with
        {
            CsvPathStatus = ResourceTrendEvidencePathStatus.Present,
        };

        var document = ResourceTrendValidationDraftFactory.Create(
            request,
            "C:\\Validation",
            new DateTimeOffset(2026, 06, 24, 12, 30, 00, TimeSpan.FromHours(8)),
            """
            # Resource Trend Validation Session Template

            ## Sampler Configuration

            - Lumiere process ID:
            - Duration seconds:
            - Sample interval seconds:
            - Output directory:
            - CSV path:
            - Summary JSON path:
            - GPU counter availability:

            ## Checklist Rows Covered

            - Public gate `Long-run lifecycle evidence`:

            ## Metric Summary

            | Metric | Baseline | Final | Delta | Min | Max | Classification | Notes |
            |---|---|---|---|---|---|---|---|
            | Handles |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | Private bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | Threads |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | Working set bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | Paged memory bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | GPU dedicated usage bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | GPU shared usage bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
            | GPU total committed bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |

            ## Final Result

            - Session classification: PASS / PASS with limitation / FAIL / NOT RUN
            """,
            summary);

        Assert.Contains("- Duration seconds: 900", document, StringComparison.Ordinal);
        Assert.Contains("- Sample interval seconds: 5", document, StringComparison.Ordinal);
        Assert.Contains("- CSV path: C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000.csv", document, StringComparison.Ordinal);
        Assert.Contains("- Summary JSON path: C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000-summary.json", document, StringComparison.Ordinal);
        Assert.Contains("- GPU counter availability: GPU counters present in latest sampler summary", document, StringComparison.Ordinal);
        Assert.Contains("- Lumiere process ID: 4242 (current session); imported summary matches PID 4242", document, StringComparison.Ordinal);
        Assert.Contains("| Handles | 100 | 104 | 4 | 99 | 105 | REPLACE_WITH_PASS_FAIL_LIMITATION | Imported from sampler summary. |", document, StringComparison.Ordinal);
        Assert.Contains("| Private bytes | 1000000 | 1200000 | 200000 | 950000 | 1250000 | REPLACE_WITH_PASS_FAIL_LIMITATION | Imported from sampler summary. |", document, StringComparison.Ordinal);
        Assert.Contains("| GPU total committed bytes | 500000 | 550000 | 50000 | 490000 | 560000 | REPLACE_WITH_PASS_FAIL_LIMITATION | Imported from sampler summary. |", document, StringComparison.Ordinal);
        Assert.Contains("REPLACE_WITH_PASS_FAIL_LIMITATION after reviewing 180 imported sampler samples", document, StringComparison.Ordinal);
        Assert.Contains("- Session classification: REPLACE_WITH_PASS_FAIL_LIMITATION (sampler summary imported; human review required)", document, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_WarnsWhenImportedSamplerSummaryDoesNotMatchCurrentProcess()
    {
        var request = new ResourceTrendValidationDraftRequest(
            "2.3.4+72c3be7",
            OutputTarget.Folder,
            CaptureSessionState.Idle(),
            4242);
        var summary = ResourceTrendSummaryArtifact.FromJson(
            CreateSummaryJson(processId: 7777),
            "C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid7777-20260624-120000-summary.json") with
        {
            CsvPathStatus = ResourceTrendEvidencePathStatus.Present,
        };

        var document = ResourceTrendValidationDraftFactory.Create(
            request,
            "C:\\Validation",
            new DateTimeOffset(2026, 06, 24, 12, 30, 00, TimeSpan.FromHours(8)),
            """
            - Lumiere process ID:
            - Warm-up or stabilization notes:
            """,
            summary);

        Assert.Contains(
            "- Lumiere process ID: 4242 (current session); scope warning: imported summary PID 7777 does not match current PID 4242",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "Scope warning: imported sampler summary PID 7777 does not match current PID 4242",
            document,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Create_KeepsSessionNotRunWhenImportedSamplerCsvIsMissing()
    {
        var request = new ResourceTrendValidationDraftRequest(
            "2.3.4+72c3be7",
            OutputTarget.Folder,
            CaptureSessionState.Idle(),
            4242);
        var summary = ResourceTrendSummaryArtifact.FromJson(
            CreateSummaryJson(),
            "C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000-summary.json") with
        {
            CsvPathStatus = ResourceTrendEvidencePathStatus.Missing,
        };

        var document = ResourceTrendValidationDraftFactory.Create(
            request,
            "C:\\Validation",
            new DateTimeOffset(2026, 06, 24, 12, 30, 00, TimeSpan.FromHours(8)),
            """
            - Public gate `Long-run lifecycle evidence`:
            - Warm-up or stabilization notes:
            - Session classification: PASS / PASS with limitation / FAIL / NOT RUN
            - Known limitations:
            """,
            summary);

        Assert.Contains(
            "Public gate `Long-run lifecycle evidence`: NOT RUN until imported sampler evidence is complete",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "CSV path is missing or unreadable: C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000.csv",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "- Session classification: NOT RUN (imported sampler summary is incomplete:",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "Imported sampler summary is not yet countable release evidence",
            document,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Create_KeepsSessionNotRunWhenImportedSamplerCsvWasNotVerified()
    {
        var request = new ResourceTrendValidationDraftRequest(
            "2.3.4+72c3be7",
            OutputTarget.Folder,
            CaptureSessionState.Idle(),
            4242);
        var summary = ResourceTrendSummaryArtifact.FromJson(
            CreateSummaryJson(),
            "C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000-summary.json");

        var document = ResourceTrendValidationDraftFactory.Create(
            request,
            "C:\\Validation",
            new DateTimeOffset(2026, 06, 24, 12, 30, 00, TimeSpan.FromHours(8)),
            """
            - Public gate `Long-run lifecycle evidence`:
            - Session classification: PASS / PASS with limitation / FAIL / NOT RUN
            """,
            summary);

        Assert.Contains(
            "Public gate `Long-run lifecycle evidence`: NOT RUN until imported sampler evidence is complete",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "CSV path must be manually verified: C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid4242-20260624-120000.csv",
            document,
            StringComparison.Ordinal);
        Assert.Contains(
            "- Session classification: NOT RUN (imported sampler summary is incomplete:",
            document,
            StringComparison.Ordinal);
    }

    private static string CreateSummaryJson(int processId = 4242) =>
        $$"""
        {
          "processId": {{processId}},
          "processName": "Lumiere.App",
          "durationSeconds": 900,
          "sampleIntervalSeconds": 5,
          "sampleCount": 180,
          "csvPath": "C:\\Validation\\resource-trends\\resource-trend-Lumiere.App-pid{{processId}}-20260624-120000.csv",
          "metrics": {
            "handles": { "baseline": 100, "final": 104, "delta": 4, "min": 99, "max": 105 },
            "privateBytes": { "baseline": 1000000, "final": 1200000, "delta": 200000, "min": 950000, "max": 1250000 },
            "threads": { "baseline": 12, "final": 12, "delta": 0, "min": 12, "max": 13 },
            "workingSetBytes": { "baseline": 2000000, "final": 2100000, "delta": 100000, "min": 1900000, "max": 2200000 },
            "pagedMemoryBytes": { "baseline": 300000, "final": 310000, "delta": 10000, "min": 290000, "max": 320000 },
            "gpuDedicatedUsageBytes": { "baseline": 100000, "final": 110000, "delta": 10000, "min": 90000, "max": 120000 },
            "gpuSharedUsageBytes": { "baseline": 200000, "final": 210000, "delta": 10000, "min": 190000, "max": 220000 },
            "gpuTotalCommittedBytes": { "baseline": 500000, "final": 550000, "delta": 50000, "min": 490000, "max": 560000 }
          }
        }
        """;
}
