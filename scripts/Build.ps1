param([switch]$Publish, [switch]$UiAccess)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    dotnet build ShrinkDarkerDisplay.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $testArguments = @('test', 'tests/ShrinkDarkerDisplay.Tests', '-c', 'Release', '--no-build')
    if ($env:GITHUB_ACTIONS -eq 'true') { $testArguments += @('--filter', 'Category!=Desktop') }
    dotnet @testArguments
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    $flavor = if ($UiAccess) { 'uiaccess' } else { 'portable' }
    $output = Join-Path $root "artifacts\winui-$flavor"
    if ($Publish) {
        dotnet publish src/ShrinkDarkerDisplay.WinUI -c Release -r win-x64 --self-contained true `
            -p:UiAccess=$($UiAccess.IsPresent.ToString().ToLowerInvariant()) `
            -p:PublishAot=true -p:PublishTrimmed=true -p:DebugType=None -p:DebugSymbols=false -o $output
        if ($LASTEXITCODE -ne 0) { throw 'Application publish failed.' }
    }
} finally { Pop-Location }

