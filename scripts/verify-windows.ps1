Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-DotNet {
    param([Parameter(Mandatory = $true)][string[]]$TaskArguments)

    Write-Host "dotnet $($TaskArguments -join ' ')"
    & dotnet @TaskArguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Invoke-DotNet -TaskArguments @(
    "restore", "Lumiere.sln",
    "--disable-parallel", "--verbosity", "minimal", "/nr:false"
)
Invoke-DotNet -TaskArguments @(
    "build", "Lumiere.sln",
    "-p:Platform=x64", "--no-restore", "--verbosity", "minimal", "/nr:false"
)
Invoke-DotNet -TaskArguments @(
    "test", "tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj",
    "-p:Platform=x64", "--no-restore", "--verbosity", "minimal", "/nr:false"
)
Invoke-DotNet -TaskArguments @(
    "test", "tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj",
    "-p:Platform=x64", "--no-restore", "--verbosity", "minimal", "/nr:false"
)
Invoke-DotNet -TaskArguments @(
    "format", "Lumiere.sln",
    "--verify-no-changes", "--no-restore", "--verbosity", "minimal"
)
