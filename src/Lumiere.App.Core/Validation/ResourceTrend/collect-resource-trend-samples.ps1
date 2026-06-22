<#
.SYNOPSIS
Collects repeated resource trend samples for a running Lumiere process.

.DESCRIPTION
Records CPU-process and GPU-process-memory counters at a fixed interval and
emits both raw CSV samples and a JSON summary. This script is intended for the
Windows manual validation workflow in harness/validation/resource-trend-validation.md.

.PARAMETER ProcessName
Process name to monitor when ProcessId is not provided. Defaults to Lumiere.App.

.PARAMETER ProcessId
Exact process ID to monitor. Prefer this when multiple Lumiere instances may exist.

.PARAMETER DurationSeconds
How long to sample for. Public-fidelity long-run validation usually uses a
duration that covers 50+ or 100+ capture/output cycles.

.PARAMETER SampleIntervalSeconds
Seconds between samples.

.PARAMETER OutputDirectory
Directory where the CSV and summary JSON will be written.
#>
param(
    [string]$ProcessName = "Lumiere.App",
    [int]$ProcessId,
    [ValidateRange(10, 86400)]
    [int]$DurationSeconds = 600,
    [ValidateRange(1, 300)]
    [int]$SampleIntervalSeconds = 5,
    [string]$OutputDirectory = "."
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:gpuCounterWarningShown = $false

function Resolve-TargetProcess {
    param(
        [string]$Name,
        [int]$Id
    )

    if ($Id -gt 0) {
        return Get-Process -Id $Id -ErrorAction Stop
    }

    $matches = Get-Process -Name $Name -ErrorAction Stop | Sort-Object StartTime -Descending
    if ($matches.Count -gt 1) {
        Write-Warning "Multiple processes matched '$Name'. Using the newest process Id=$($matches[0].Id)."
    }

    return $matches[0]
}

function Get-GpuUsageSample {
    param(
        [int]$Id
    )

    try {
        $counterPaths = @(
            "\\GPU Process Memory(*)\\Dedicated Usage",
            "\\GPU Process Memory(*)\\Shared Usage",
            "\\GPU Process Memory(*)\\Total Committed"
        )
        $samples = Get-Counter -Counter $counterPaths -MaxSamples 1
        $matching = $samples.CounterSamples | Where-Object { $_.InstanceName -like "pid_${Id}_*" }

        if (-not $matching) {
            return [pscustomobject]@{
                DedicatedUsageBytes = 0L
                SharedUsageBytes    = 0L
                TotalCommittedBytes = 0L
            }
        }

        $sumMetric = {
            param(
                [string]$Suffix
            )

            [long](($matching |
                Where-Object { $_.Path -like "*$Suffix" } |
                Measure-Object -Property CookedValue -Sum).Sum)
        }

        return [pscustomobject]@{
            DedicatedUsageBytes = (& $sumMetric "Dedicated Usage")
            SharedUsageBytes    = (& $sumMetric "Shared Usage")
            TotalCommittedBytes = (& $sumMetric "Total Committed")
        }
    }
    catch {
        if (-not $script:gpuCounterWarningShown) {
            Write-Warning "GPU Process Memory counters were unavailable; GPU fields will be recorded as 0. $($_.Exception.Message)"
            $script:gpuCounterWarningShown = $true
        }

        return [pscustomobject]@{
            DedicatedUsageBytes = 0L
            SharedUsageBytes    = 0L
            TotalCommittedBytes = 0L
        }
    }
}

function Measure-MetricSummary {
    param(
        [System.Collections.Generic.List[object]]$Samples,
        [string]$PropertyName
    )

    $baseline = [long]$Samples[0].$PropertyName
    $final = [long]$Samples[$Samples.Count - 1].$PropertyName
    $values = $Samples | ForEach-Object { [long]$_.$PropertyName }

    return [ordered]@{
        baseline = $baseline
        final    = $final
        delta    = $final - $baseline
        min      = ($values | Measure-Object -Minimum).Minimum
        max      = ($values | Measure-Object -Maximum).Maximum
    }
}

$targetProcess = Resolve-TargetProcess -Name $ProcessName -Id $ProcessId
$resolvedProcessId = $targetProcess.Id

if (-not (Test-Path -LiteralPath $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$baseName = "resource-trend-$($targetProcess.ProcessName)-pid$resolvedProcessId-$stamp"
$csvPath = Join-Path $OutputDirectory "$baseName.csv"
$summaryPath = Join-Path $OutputDirectory "$baseName-summary.json"

$records = [System.Collections.Generic.List[object]]::new()
$deadline = (Get-Date).AddSeconds($DurationSeconds)

Write-Host "Sampling process '$($targetProcess.ProcessName)' (PID $resolvedProcessId) for $DurationSeconds seconds every $SampleIntervalSeconds seconds..."
Write-Host "CSV output: $csvPath"
Write-Host "Summary output: $summaryPath"

while ((Get-Date) -lt $deadline) {
    try {
        $targetProcess = Get-Process -Id $resolvedProcessId -ErrorAction Stop
    }
    catch {
        Write-Warning "The target process exited before sampling completed."
        break
    }

    $gpuSample = Get-GpuUsageSample -Id $resolvedProcessId
    $timestampUtc = (Get-Date).ToUniversalTime().ToString("o")

    $records.Add([pscustomobject]@{
            TimestampUtc            = $timestampUtc
            ProcessId               = $resolvedProcessId
            ProcessName             = $targetProcess.ProcessName
            Handles                 = [int]$targetProcess.Handles
            PrivateBytes            = [long]$targetProcess.PrivateMemorySize64
            WorkingSetBytes         = [long]$targetProcess.WorkingSet64
            PagedMemoryBytes        = [long]$targetProcess.PagedMemorySize64
            Threads                 = [int]$targetProcess.Threads.Count
            GpuDedicatedUsageBytes  = [long]$gpuSample.DedicatedUsageBytes
            GpuSharedUsageBytes     = [long]$gpuSample.SharedUsageBytes
            GpuTotalCommittedBytes  = [long]$gpuSample.TotalCommittedBytes
        })

    Start-Sleep -Seconds $SampleIntervalSeconds
}

if ($records.Count -eq 0) {
    throw "No samples were collected."
}

$records | Export-Csv -NoTypeInformation -Encoding UTF8 -Path $csvPath

$summary = [ordered]@{
    generatedAtUtc         = (Get-Date).ToUniversalTime().ToString("o")
    processId              = $resolvedProcessId
    processName            = $records[0].ProcessName
    durationSeconds        = $DurationSeconds
    sampleIntervalSeconds  = $SampleIntervalSeconds
    sampleCount            = $records.Count
    csvPath                = $csvPath
    metrics                = [ordered]@{
        handles                = (Measure-MetricSummary -Samples $records -PropertyName "Handles")
        privateBytes           = (Measure-MetricSummary -Samples $records -PropertyName "PrivateBytes")
        threads                = (Measure-MetricSummary -Samples $records -PropertyName "Threads")
        workingSetBytes        = (Measure-MetricSummary -Samples $records -PropertyName "WorkingSetBytes")
        pagedMemoryBytes       = (Measure-MetricSummary -Samples $records -PropertyName "PagedMemoryBytes")
        gpuDedicatedUsageBytes = (Measure-MetricSummary -Samples $records -PropertyName "GpuDedicatedUsageBytes")
        gpuSharedUsageBytes    = (Measure-MetricSummary -Samples $records -PropertyName "GpuSharedUsageBytes")
        gpuTotalCommittedBytes = (Measure-MetricSummary -Samples $records -PropertyName "GpuTotalCommittedBytes")
    }
}

$summary | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 -Path $summaryPath

Write-Host ""
Write-Host "Sampling complete."
Write-Host "Samples collected: $($records.Count)"
Write-Host "Private bytes delta: $($summary.metrics.privateBytes.delta)"
Write-Host "Handle delta: $($summary.metrics.handles.delta)"
Write-Host "Thread delta: $($summary.metrics.threads.delta)"
Write-Host "GPU total committed delta: $($summary.metrics.gpuTotalCommittedBytes.delta)"
