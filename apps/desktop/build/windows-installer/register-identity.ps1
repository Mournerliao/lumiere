param(
  [Parameter(Mandatory = $true)]
  [string] $InstallDirectory
)

$identityPath = Join-Path $InstallDirectory 'resources\windows-identity\Lumiere.Identity.msix'

try {
  Add-AppxPackage `
    -Path $identityPath `
    -ExternalLocation $InstallDirectory `
    -ForceApplicationShutdown `
    -ErrorAction Stop
}
catch {
  Write-Warning $_.Exception.Message
  exit 1
}
