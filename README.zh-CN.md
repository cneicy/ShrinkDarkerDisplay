# ShrinkDarkerDisplay

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

Windows 11 屏幕调暗工具，使用 C#、WinUI 3 和 .NET 10 NativeAOT 构建。

## 功能

- 各显示器独立调暗，亮度范围 10%～100%。
- 托盘快捷面板，支持浅色、深色及跟随系统主题。
- 主界面支持简体中文、繁体中文、英文和日文，自动匹配系统语言。
- 调暗蒙版排除截图捕获；Ctrl+Alt+Shift+F12 紧急恢复亮度。

## 使用

运行 `ShrinkDarkerDisplay-Setup-x64.exe`，按向导安装。支持 Windows 11 x64，无需另装 .NET。

安装包采用自签名证书，首次安装需要管理员权限并添加本机信任，可能出现 SmartScreen 提示。请确认下载来源；受企业应用管控限制的电脑可能无法运行。

点击托盘图标打开面板，“调暗”按钮可暂停或恢复。退出程序会恢复正常亮度。配置位于 `%LocalAppData%\ShrinkDarkerDisplay`，卸载时保留。

输入法候选窗仍可能闪烁；HDR、混合多屏尚未完整验收。鼠标指针与 UAC 安全桌面不调暗。截图兼容性取决于捕获方式。

## 开发

需要 .NET 10 SDK、Windows SDK 和 Visual C++ x64 构建工具；生成安装包另需 Inno Setup 6.7+。

```powershell
.\scripts\Build.ps1
.\scripts\Build-Installer.ps1 -Compiler 'C:\Program Files (x86)\Inno Setup 6\ISCC.exe'
```

安装包输出到 `artifacts/installer/`。翻译位于 `src/ShrinkDarkerDisplay.WinUI/Localization/*.json`。目录说明、签名及运行验证见 [开发说明](docs/DEVELOPMENT.md)。

## 许可

[MIT License](LICENSE) · 仓库地址：[cneicy/ShrinkDarkerDisplay](https://github.com/cneicy/ShrinkDarkerDisplay)
