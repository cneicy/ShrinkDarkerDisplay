#define AppName "ShrinkDarkerDisplay"
[Setup]
AppId={{01D414F2-7C67-43A1-954B-199691AB14DE}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher=ShrinkDarkerDisplay
DefaultDirName={autopf}\ShrinkDarkerDisplay
DisableDirPage=yes
DisableProgramGroupPage=yes
DefaultGroupName=ShrinkDarkerDisplay
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.22000
WizardStyle=modern
DisableWelcomePage=no
Compression=lzma2/ultra64
SolidCompression=yes
OutputDir=..\artifacts\installer
OutputBaseFilename=ShrinkDarkerDisplay-Setup-x64
UninstallDisplayIcon={app}\ShrinkDarkerDisplay.App.exe
SetupIconFile=..\src\ShrinkDarkerDisplay.WinUI\Assets\ShrinkDarkerDisplay.ico
CloseApplications=yes
RestartApplications=no
InfoBeforeFile=TrustNotice.txt
LicenseFile=..\LICENSE
SetupLogging=yes
[Languages]
Name: "chinesesimp"; MessagesFile: "ChineseSimplified.isl"
[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式："; Flags: unchecked
[Files]
Source: "..\artifacts\winui-uiaccess\*"; DestDir: "{tmp}\ShrinkDarkerDisplayPayload"; Excludes: "install.log"; Flags: recursesubdirs createallsubdirs deleteafterinstall
[Icons]
Name: "{autoprograms}\ShrinkDarkerDisplay"; Filename: "{app}\ShrinkDarkerDisplay.App.exe"
Name: "{autodesktop}\ShrinkDarkerDisplay"; Filename: "{app}\ShrinkDarkerDisplay.App.exe"; Tasks: desktopicon
[Run]
Filename: "{app}\ShrinkDarkerDisplay.App.exe"; Description: "启动 ShrinkDarkerDisplay"; Flags: postinstall nowait skipifsilent runasoriginaluser
[Code]
function RunScript(Script, Extra: String): Boolean;
var Code: Integer;
begin
  Result := Exec(ExpandConstant('{sys}\WindowsPowerShell\v1.0\powershell.exe'),
    '-NoProfile -NonInteractive -ExecutionPolicy Bypass -File "' + Script + '" ' + Extra,
    '', SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Target: String;
begin
  Result := '';
  Target := ExpandConstant('{app}');
  if FileExists(Target + '\installation.json') then begin
    if not RunScript(Target + '\Uninstall-Local.ps1', '') then
      Result := '旧版本卸载失败，请关闭程序后重试。个人亮度设置会保留。';
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then begin
    WizardForm.StatusLabel.Caption := '正在部署程序并验证 UIAccess 签名…';
    if not RunScript(ExpandConstant('{tmp}\ShrinkDarkerDisplayPayload\Install-Local.ps1'), '-FromSetup') then
      RaiseException('程序部署失败。请查看安装日志；本次安装尚未成功完成。');
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep <> usUninstall then Exit;
  if FileExists(ExpandConstant('{app}\installation.json')) then begin
    if not RunScript(ExpandConstant('{app}\Uninstall-Local.ps1'), '') then
      RaiseException('程序文件清理失败，卸载已停止。请关闭程序后重试。');
  end;
end;

