# Comfort UI 0.2.13 · Chat-bubble hotfix / 气泡显示修复

- 修复 F8 气泡译文从玩家视角反向、偏离原气泡的问题。更换语言字体时保留游戏的面向玩家材质，仅替换字体图集和必要参数。
- 译文直接更新原生文字组件并刷新网格；关闭翻译、收到新消息或销毁气泡时还原文字/字体/材质并释放临时材质。
- F8 继续作为持续翻译开关，保留过期回复、取消和页面生命周期保护。

Chat translation now keeps the game's viewer-facing bubble material instead of replacing it with an ordinary font material. It rebuilds the same native text output, and restores its original text/font/material when disabled or replaced.

| Download / 下载 | Use / 用途 |
| --- | --- |
| `CVRComfortUI.dll` | 模组 DLL，放入游戏 Mods 文件夹 |
| `ComfortUI-Setup-0.2.13.exe` | 独立安装、更新和移除；含官方加载器引导 |
| `ComfortUI-Manual-0.2.13.zip` | DLL、安装说明、隐私说明和更新日志 |
| `ComfortUI-Source-0.2.13.zip` | 本版本源码及测试 |
| `SHA256SUMS.txt` | 完整性校验 |

正常退出 ChilloutVR 后更新；EXE 与 DLL 为两种安装方式，只保留一份模组。已有个人设置会保留。Exit normally before updating. Keep only one mod DLL.

Development target: Windows / .NET Framework 4.8, ChilloutVR **2026r182-hf4 public-nightly**, Unity **2022.3.58f1**, Mono MelonLoader **0.8.0-ci.2548**. Other branches/runtimes remain unverified.

75 managed checks passed (35 chat, 12 native-page lifetime, 10 HTTP logic, 18 camera policy). The old implementation fails the new facing-material regression and the fix passes. The existing game-reference build had zero warnings/errors; all six embedded resources and installer payload match. **Managed stand-ins do not establish real-client PC/VR, mirror-view or long-session rendering validation.** No new live translation-provider verification is claimed.

本版优先修复气泡显示；AI/微软等翻译接口扩展、HUD 和名牌改版仍在后续工作中。Google/MyMemory 的连接与限流问题尚不能视为全部解决。气泡翻译每次启动默认关闭，开启后会将可见正文发给所选服务。

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Modifications may affect performance, security or stability.

[中文安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.13/docs/INSTALLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.13/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.13/docs/VALIDATION.md) · [Privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.13/docs/PRIVACY.md)
