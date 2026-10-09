#Requires -RunAsAdministrator
param([switch]$FromSetup)
$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
Start-Transcript -Path (Join-Path $source 'install.log') -Append | Out-Null
$target = Join-Path $env:ProgramFiles 'ShrinkDarkerDisplay'
$certificatePath = Join-Path $source 'ShrinkDarkerDisplay.Local.cer'
$certificate = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new($certificatePath)
$signing = Get-Content -LiteralPath (Join-Path $source 'signing.json') -Raw | ConvertFrom-Json
if ($certificate.Subject -ne 'CN=ShrinkDarkerDisplay Local Development' -or $certificate.Thumbprint -ne $signing.Thumbprint) { throw 'Unexpected local certificate.' }
if (Test-Path -LiteralPath $target) {
    if ((Get-Item -LiteralPath $target).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing an installation directory reparse point.' }
    $remaining = @(Get-ChildItem -LiteralPath $target -Force)
    if (-not $FromSetup -or @($remaining | Where-Object { $_.PSIsContainer -or $_.Name -notmatch '^unins[0-9]{3}\.(exe|dat|msg)$' }).Count -gt 0) {
        throw "Installation directory contains existing files: $target"
    }
}
foreach ($name in @('ShrinkDarkerDisplay.App.exe')) {
    $signature = Get-AuthenticodeSignature -LiteralPath (Join-Path $source $name)
    if (-not $signature.TimeStamperCertificate) { throw "Timestamp missing: $name" }
    # Untrusted local roots may be reported as UnknownError; full verification follows the trust import.
    if ($signature.SignerCertificate.Thumbprint -ne $certificate.Thumbprint -or $signature.Status -notin @('Valid','NotTrusted','UnknownError')) { throw "Invalid signed executable: $name" }
}
Write-Output "Install destination: $target"
Write-Output "Self-signed certificate: $($certificate.Thumbprint)"
Write-Output 'Adds this dedicated certificate to LocalMachine Root for UIAccess validation. Does not disable Windows security policies.'
$addedRoot = -not (Test-Path -LiteralPath "Cert:\LocalMachine\Root\$($certificate.Thumbprint)")
$createdTarget = $false
$copiedPaths = @()
try {
    if ($addedRoot) { Import-Certificate -FilePath $certificatePath -CertStoreLocation 'Cert:\LocalMachine\Root' | Out-Null }
    foreach ($name in @('ShrinkDarkerDisplay.App.exe')) {
        if ((Get-AuthenticodeSignature -LiteralPath (Join-Path $source $name)).Status -ne 'Valid') { throw "Signature verification failed: $name" }
    }
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $createdTarget = $true
    $files = Get-ChildItem -LiteralPath $source -File -Recurse | Where-Object Name -ne 'install.log'
    $relativeFiles = @()
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($source.TrimEnd('\').Length + 1)
        $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
        if (-not $destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'File escapes install directory.' }
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
        $copiedPaths += $destination
        $relativeFiles += $relative
    }
    @{ Thumbprint = $certificate.Thumbprint; AddedRoot = $addedRoot; Files = $relativeFiles } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $target 'installation.json') -Encoding utf8
    Write-Output "Installed. Launch as your normal user: $target\ShrinkDarkerDisplay.App.exe"
} catch {
    if ($createdTarget) {
        # This invocation created the previously absent directory; rollback only its copied files.
        foreach ($copied in $copiedPaths) {
            if (Test-Path -LiteralPath $copied) { Remove-Item -LiteralPath $copied }
        }
        Get-ChildItem -LiteralPath $target -Directory -Recurse | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
            if (@(Get-ChildItem -LiteralPath $_.FullName -Force).Count -eq 0) { Remove-Item -LiteralPath $_.FullName }
        }
        if (@(Get-ChildItem -LiteralPath $target -Force).Count -eq 0) { Remove-Item -LiteralPath $target }
    }
    if ($addedRoot -and (Test-Path -LiteralPath "Cert:\LocalMachine\Root\$($certificate.Thumbprint)")) { Remove-Item -LiteralPath "Cert:\LocalMachine\Root\$($certificate.Thumbprint)" }
    throw
}

