# ShrinkDarkerDisplay

[English](README.md) · [简体中文](README.zh-CN.md) · [日本語](README.ja.md)

C#、WinUI 3、.NET 10 NativeAOT で作成した Windows 11 用の画面調光ツールです。

## 機能

- モニターごとに 10%～100% の範囲で明るさを調整。
- トレイから開くコンパクトなパネル。ライト・ダーク・システムテーマに対応。
- 簡体字中国語、繁体字中国語、英語、日本語の UI。
- 調光オーバーレイを画面キャプチャから除外。**Ctrl+Alt+Shift+F12** で明るさを復元。

## インストール

[Releases](https://github.com/cneicy/ShrinkDarkerDisplay/releases) から `ShrinkDarkerDisplay-Setup-x64.exe` をダウンロードしてください。Windows 11 x64 が必要です。.NET の別途インストールは不要です。

インストーラーは自己署名証明書を使用し、UIAccess に必要なローカルの信頼設定を追加するため管理者権限を要求します。SmartScreen の警告が表示される場合があります。信頼できる配布元を確認してください。組織が管理する PC では実行できない場合があります。インストーラーは現在、簡体字中国語のみです。

トレイアイコンをクリックするとパネルが開きます。「調光」で一時停止・再開できます。終了すると元の明るさに戻ります。設定は `%LocalAppData%\ShrinkDarkerDisplay` に保存され、アンインストール後も残ります。

**既知の制限:** IME 候補ウィンドウがちらつく場合があります。HDR や異なるモニターを組み合わせた環境は検証が完了していません。マウスポインターと UAC のセキュアデスクトップは調光しません。スクリーンショットの互換性はキャプチャ方式に依存します。

## ビルド

.NET 10 SDK、Windows SDK、Visual C++ x64 ビルドツールが必要です。インストーラーの作成には Inno Setup 6.7 以降も必要です。

```powershell
.\scripts\Build.ps1
.\scripts\Build-Installer.ps1
```

出力先は `artifacts/installer/` です。翻訳は `src/ShrinkDarkerDisplay.WinUI/Localization/` の JSON ファイルで管理します。

構成、署名、GitHub Actions は [開発ガイド](docs/DEVELOPMENT.md) を参照してください。

## ライセンス

[MIT License](LICENSE)。外部ライブラリにはそれぞれのライセンスが適用されます。
