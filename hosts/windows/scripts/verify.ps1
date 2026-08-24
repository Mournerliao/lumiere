Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$WindowsRoot = Split-Path -Parent $PSScriptRoot
$Solution = Join-Path $WindowsRoot "Lumiere.Windows.sln"

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$TaskArguments)

    Write-Host "dotnet $($TaskArguments -join ' ')"
    & dotnet @TaskArguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Invoke-DotNet -TaskArguments @(
    "restore", $Solution,
    "--disable-parallel", "--verbosity", "minimal", "/nr:false"
)
Invoke-DotNet -TaskArguments @(
    "build", $Solution,
    "--configuration", "Release", "-p:Platform=x64", "--no-restore", "--verbosity", "minimal", "/nr:false"
)

foreach ($Project in @(
    "Lumiere.Windows.Capture.Tests",
    "Lumiere.Windows.Graphics.Tests",
    "Lumiere.Windows.Interop.Tests"
)) {
    $ProjectPath = Join-Path $WindowsRoot "tests/$Project/$Project.csproj"
    Invoke-DotNet -TaskArguments @(
        "test", $ProjectPath,
        "--configuration", "Release", "-p:Platform=x64", "--no-build", "--no-restore", "--verbosity", "minimal", "/nr:false"
    )
}

Invoke-DotNet -TaskArguments @(
    "format", $Solution,
    "--verify-no-changes", "--no-restore", "--verbosity", "minimal"
)
