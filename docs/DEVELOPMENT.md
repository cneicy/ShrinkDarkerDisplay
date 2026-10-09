# 开发说明

## 目录

| 目录 | 内容 |
| --- | --- |
| `src/ShrinkDarkerDisplay.WinUI` | 设置面板、托盘、主题、语言、窗口动画及应用生命周期 |
| `src/ShrinkDarkerDisplay.Core` | 显示器识别、原生蒙版、配置与 Windows 接口 |
| `tests/ShrinkDarkerDisplay.Tests` | 亮度、配置及原生窗口生命周期测试 |
| `installer` | Inno Setup 安装向导、中文语言文件 |
| `scripts` | 构建、签名、安装、卸载和图标生成 |

`MainWindow.xaml` 定义布局；同名 partial 文件分别管理语言、主题、面板动画和关于对话框。

## 构建与资源

运行 `scripts/Build.ps1` 编译解决方案并执行测试。`-Publish -UiAccess` 发布自包含 NativeAOT 程序；取得 UIAccess 还需签名并安装到受信任目录。

`Build-Installer.ps1` 完成构建、签名和安装器打包。安装器查找系统安装的 Inno Setup，也可通过 `-Compiler` 指定路径。`artifacts/`、`bin/`、`obj/` 为可重新生成的产物。

图标源文件位于 `Assets/ShrinkDarkerDisplay.svg`。修改后安装 Pillow，运行 `python scripts/Generate-Icon.py` 更新多尺寸 ICO；普通构建不依赖 Python。

## 签名和安装

每次打包生成专用 30 年自签名代码签名证书，使用 SHA-256 和 DigiCert RFC 3161 时间戳签署主程序、安装器。构建需要联网获取时间戳，失败则停止。签名完成后删除临时私钥，不将私钥打包。

安装器展示证书信息，将公开证书加入本机根信任存储，再验证程序签名。安装脚本维护所属文件清单；卸载仅删除清单内文件及本次添加的证书，保留用户配置。重新分发时应保留依赖库及其许可文件；MIT License 适用于本项目自有代码，第三方组件遵循各自许可。

带可信时间戳的签名支持证书过期后验证。本机已验证短期证书自然到期后签名有效且 UIAccess 可用；不同系统的应用管控和信任策略仍可能限制运行。

## 验证

`Build.ps1` 运行配置、亮度、翻译以及原生窗口回归测试。标记为 `Category=Desktop` 的测试需要交互桌面，GitHub runner 中不执行；其余测试在 CI 中运行。应用不包含临时截图、模拟显示器或 `--verify` 验收入口。

截图排除、窗口速览、输入法、HDR、多屏和睡眠恢复需在目标设备上实测；自动化测试不能替代显示效果验收。安装向导目前为简体中文；底层或系统异常的详细信息使用异常来源提供的语言。

## 翻译

文案位于 `src/ShrinkDarkerDisplay.WinUI/Localization/`：`zh-Hans.json`、`zh-Hant.json`、`en.json`、`ja.json`。文件使用 UTF-8，格式为字符串键到译文的 JSON 对象，支持 `\n` 换行。

界面根据 Windows 显示语言选择文件，不支持的语言或缺失的译文回退英文。新增文案应在四个文件中使用相同键；测试会检查键集合及语言匹配规则。JSON 文件作为独立资源随程序发布，不依赖反射序列化，兼容 NativeAOT。

## GitHub Actions 与版本

`Version.props` 是版本号的唯一来源，供 .NET 程序、关于页面和安装器使用。

- `Build`：Pull Request 或手动触发，编译、运行无需桌面的测试并验证 NativeAOT 发布。
- `Release`：推送至 `main` 自动递增 patch（纯 Markdown 修改除外）；手动执行可选择 patch、minor、major。构建通过后提交版本、原子推送提交及标签，创建草稿 Release，上传安装包后公开发布。
- 自动提交使用 `GITHUB_TOKEN`，不会再次触发 push 工作流。发布任务串行运行，构建或推送失败不会公开 Release。

仓库需要允许 Actions 使用 `contents: write`，且分支规则允许 Actions 提交版本到 `main`。无需保存签名私钥或商业证书 secret；打包在 runner 上生成专用证书并添加时间戳。

如果标签已推送但上传失败，可在失败任务的日志中找到版本，补传构建产物并发布对应草稿；重新触发 Release 会生成下一个版本。CI 使用 `windows-2025`、`global.json` 指定的 SDK 和 runner 提供的 Inno Setup。
