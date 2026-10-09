#Requires -RunAsAdministrator
$ErrorActionPreference = 'Stop'
$target = [IO.Path]::GetFullPath((Join-Path $env:ProgramFiles 'ShrinkDarkerDisplay'))
if (-not (Test-Path -LiteralPath (Join-Path $target 'installation.json'))) { throw 'No ShrinkDarkerDisplay installation record was found.' }
if ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing to uninstall through a reparse point.' }
$record = Get-Content -LiteralPath (Join-Path $target 'installation.json') -Raw | ConvertFrom-Json
if ($record.Thumbprint -notmatch '^[A-Fa-f0-9]{40}$') { throw 'Invalid certificate thumbprint.' }
Get-Process -Name ShrinkDarkerDisplay.App -ErrorAction SilentlyContinue | Where-Object Path -eq (Join-Path $target 'ShrinkDarkerDisplay.App.exe') | Stop-Process -Force
$ownedPaths = foreach ($name in $record.Files) {
    if ([IO.Path]::IsPathRooted($name)) { throw 'Invalid installation file name.' }
    $filePath = [IO.Path]::GetFullPath((Join-Path $target $name))
    if (-not $filePath.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'File escapes installation directory.' }
    $parent = Split-Path $filePath -Parent
    while ($parent.Length -gt $target.Length) {
        if ((Test-Path -LiteralPath $parent) -and ((Get-Item -LiteralPath $parent).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Refusing to uninstall through a reparse point.' }
        $parent = Split-Path $parent -Parent
    }
    $filePath
}
foreach ($filePath in $ownedPaths) { if (Test-Path -LiteralPath $filePath) { Remove-Item -LiteralPath $filePath } }
$ownedDirectories = $ownedPaths | ForEach-Object { Split-Path $_ -Parent } | Sort-Object -Unique | Sort-Object Length -Descending
foreach ($directory in $ownedDirectories) {
    if ($directory -ne $target -and (Test-Path -LiteralPath $directory) -and @(Get-ChildItem -LiteralPath $directory -Force).Count -eq 0) { Remove-Item -LiteralPath $directory }
}
if ($record.AddedRoot) {
    $certPath = "Cert:\LocalMachine\Root\$($record.Thumbprint)"
    if (Test-Path -LiteralPath $certPath) {
        if ((Get-Item -LiteralPath $certPath).Subject -ne 'CN=ShrinkDarkerDisplay Local Development') { throw 'Certificate subject mismatch.' }
        Remove-Item -LiteralPath $certPath
    }
}
Remove-Item -LiteralPath (Join-Path $target 'installation.json')
Get-ChildItem -LiteralPath $target -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
    if (-not ($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -and @(Get-ChildItem -LiteralPath $_.FullName -Force).Count -eq 0) { Remove-Item -LiteralPath $_.FullName }
}
if (@(Get-ChildItem -LiteralPath $target -Force).Count -eq 0) { Remove-Item -LiteralPath $target }
Write-Output 'Uninstalled. Per-user brightness settings have been preserved.'

