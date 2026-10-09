param([ValidateSet('patch', 'minor', 'major')][string]$Part = 'patch')
$ErrorActionPreference = 'Stop'
$path = Join-Path (Split-Path $PSScriptRoot -Parent) 'Version.props'
$document = [xml](Get-Content -LiteralPath $path -Raw)
$current = [version]$document.Project.PropertyGroup.Version
$next = switch ($Part) {
    'major' { '{0}.0.0' -f ($current.Major + 1) }
    'minor' { '{0}.{1}.0' -f $current.Major, ($current.Minor + 1) }
    'patch' { '{0}.{1}.{2}' -f $current.Major, $current.Minor, ($current.Build + 1) }
}
$document.Project.PropertyGroup.Version = $next
$document.Save($path)
Write-Output $next
