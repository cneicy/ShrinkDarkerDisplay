param([string]$CertificateThumbprint)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'Build.ps1') -Publish -UiAccess
$output = Join-Path $root 'artifacts\winui-uiaccess'
$signTool = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
if (-not $signTool) { throw 'Windows SDK signtool.exe is required.' }
$subject = 'CN=ShrinkDarkerDisplay Local Development'
$ownsCertificate = -not $CertificateThumbprint
$certificate = if ($ownsCertificate) {
    New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy NonExportable -HashAlgorithm SHA256 -KeyLength 3072 -NotAfter (Get-Date).AddYears(30)
} else { Get-Item -LiteralPath "Cert:\CurrentUser\My\$CertificateThumbprint" }
if ($certificate.Subject -ne $subject -or -not $certificate.HasPrivateKey) { throw 'Unexpected signing certificate.' }
try {
    foreach ($name in @('ShrinkDarkerDisplay.App.exe')) {
        & $signTool sign /sha1 $certificate.Thumbprint /s My /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 (Join-Path $output $name)
        if ($LASTEXITCODE -ne 0) { throw "Signing failed: $name" }
        if (-not (Get-AuthenticodeSignature -LiteralPath (Join-Path $output $name)).TimeStamperCertificate) { throw "Timestamp missing: $name" }
    }
    Export-Certificate -Cert $certificate -FilePath (Join-Path $output 'ShrinkDarkerDisplay.Local.cer') | Out-Null
    @{ Thumbprint = $certificate.Thumbprint; Subject = $certificate.Subject; NotAfter = $certificate.NotAfter.ToString('O'); Purpose = 'Self-signed UIAccess distribution with explicit local trust'; TimestampServer = 'http://timestamp.digicert.com' } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'signing.json') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Install-Local.ps1'),(Join-Path $PSScriptRoot 'Uninstall-Local.ps1') -Destination $output
    Write-Output "Prepared signed validation package: $output"
    Write-Output 'No trust store or Program Files installation has been changed.'
} finally {
    # Only the newly generated certificate/key is removed; the public certificate travels with the build.
    if ($ownsCertificate) { Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($certificate.Thumbprint)" -DeleteKey }
}

