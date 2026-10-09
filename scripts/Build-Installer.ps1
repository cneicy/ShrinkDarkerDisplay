param([string]$Compiler)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not $Compiler) {
    $Compiler = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $Compiler) { throw 'Install Inno Setup 6.7+ or pass -Compiler with the path to ISCC.exe.' }
$buildCertificate = New-SelfSignedCertificate -Type CodeSigningCert -Subject 'CN=ShrinkDarkerDisplay Local Development' -CertStoreLocation 'Cert:\CurrentUser\My' -KeyExportPolicy NonExportable -HashAlgorithm SHA256 -KeyLength 3072 -NotAfter (Get-Date).AddYears(30)
try {
    & (Join-Path $PSScriptRoot 'Prepare-UiAccess.ps1') -CertificateThumbprint $buildCertificate.Thumbprint
    $certificate = Get-Content (Join-Path $root 'artifacts\winui-uiaccess\signing.json') -Raw | ConvertFrom-Json
    $notice = @"
ShrinkDarkerDisplay · 自签名安装版

本程序使用 WinUI 3 / NativeAOT，包含必要运行文件，无需安装 .NET。
安装至 Program Files 受信任目录，并使用 UIAccess 覆盖普通桌面的系统界面。

本软件未使用商业代码签名证书。Windows 可能显示未知发布者或 SmartScreen 提示。请仅安装来自你信任来源的安装包。

继续安装表示你同意将下列专用代码签名证书加入本机“受信任的根证书颁发机构”，使 Windows 能验证主程序的 UIAccess 签名。安装需要管理员权限，不会关闭 Windows 安全策略。
证书：$($certificate.Subject)
指纹：$($certificate.Thumbprint)
有效期至：$($certificate.NotAfter)

升级保留个人亮度设置。卸载仅移除本程序安装的文件及本次添加的证书，保留 %LocalAppData%\ShrinkDarkerDisplay 中的配置。

主程序和安装包采用 SHA-256 签名及可信时间戳。此版本不要求用户定期换证或联网续期；签名有效性由 Windows 验证。企业应用控制、证书被移除或系统信任策略变化可能阻止运行。

签名私钥不会随安装包分发。
"@
    Set-Content (Join-Path $root 'installer\TrustNotice.txt') $notice -Encoding utf8
    Copy-Item (Join-Path $root 'scripts\Install-Local.ps1'),(Join-Path $root 'scripts\Uninstall-Local.ps1') (Join-Path $root 'artifacts\winui-uiaccess') -Force
    $version = ([xml](Get-Content (Join-Path $root 'Version.props') -Raw)).Project.PropertyGroup.Version
    & $Compiler /Qp "/DAppVersion=$version" (Join-Path $root 'installer\Setup.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
    $signTool = Get-ChildItem -Path "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" | Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    $setup = Join-Path $root 'artifacts\installer\ShrinkDarkerDisplay-Setup-x64.exe'
    & $signTool sign /sha1 $buildCertificate.Thumbprint /s My /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 $setup
    if ($LASTEXITCODE -ne 0) { throw 'Installer signing failed.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $setup
    if ($signature.SignerCertificate.Thumbprint -ne $buildCertificate.Thumbprint -or -not $signature.TimeStamperCertificate) { throw 'Installer signature or timestamp missing.' }
} finally {
    Remove-Item -LiteralPath "Cert:\CurrentUser\My\$($buildCertificate.Thumbprint)" -DeleteKey
}
