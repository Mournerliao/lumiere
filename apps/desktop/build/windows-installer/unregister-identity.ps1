Get-AppxPackage `
  -Name 'io.github.sousouliao.lumiere' `
  -ErrorAction SilentlyContinue |
  Remove-AppxPackage -ErrorAction SilentlyContinue
