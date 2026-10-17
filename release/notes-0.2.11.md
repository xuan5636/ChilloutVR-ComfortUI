# Comfort UI 0.2.11 · Public preview / 公开测试版

First public release. Unofficial ChilloutVR Windows/Mono UI mod.

## Changes / 更新

- 五语言单行滑块：点击、拖动、键盘选择，支持大字和关闭动效。
- 原版设置的开关、短选项、长列表、滑条、输入框与数字键盘统一风格。
- 补齐首页、快捷菜单、详情、创建房间及提示窗动效；三形式、三档速度，可关闭。
- 相同设置回传不再重建整页；复用面板和标签，保留输入、焦点、滚动及 F8 状态。
- 保留 F8 持续翻译及 Cohtml 生命周期崩溃保护；再次按 F8 关闭并显示原文。
- 保留柔和自选配色、相机功能分组、详情/简介翻译、系统或手动代理，以及五语言本地词典。

## Downloads / 下载

| Asset | Use |
| --- | --- |
| `CVRComfortUI.dll` | Standalone mod; copy to `ChilloutVR/Mods/` / 手动安装 DLL |
| `ComfortUI-Setup-0.2.11.exe` | Installer with game discovery and official loader guidance / EXE 安装程序 |
| `ComfortUI-Manual-0.2.11.zip` | DLL and instructions / 手动安装包 |
| `ComfortUI-Source-0.2.11.zip` | Mod, UI assets, installer and tests / 源码 |
| `rc.json` | CVRMG Bot listing metadata / 按截图格式整理的发布资料 |
| `SHA256SUMS.txt` | SHA256 integrity / 文件校验 |

Use either DLL installation or the EXE. Exit the game normally before installing/updating. Existing settings are preserved by the installer. The mod DLL already embeds its UI assets.

Development target: ChilloutVR **2026r182-hf4 public-nightly**, Unity 2022.3.58f1, **Mono MelonLoader 0.8.0-ci.2548**, Windows / .NET Framework 4.8. Other branches and runtimes are not certified.

## Validation / 验证

93 isolated UI checks, 12 lifetime checks, 26 chat-mode checks, 10 HTTP-logic checks, 18 camera-policy checks and 13 isolated installer checks passed. Original build had zero warnings/errors. **Full 0.2.11 real-client PC/VR, long-session performance and sustained chat translation remain unverified.** Browser timings are not game FPS measurements.

Online translation sends selected content or visible chat text to Google/MyMemory, only while enabled; chat mode starts off each launch. Public endpoints may fail or rate-limit. See [privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.11/docs/PRIVACY.md).

[中文安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.11/docs/INSTALLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.11/CHANGELOG.md) · [完整验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.11/docs/VALIDATION.md)
