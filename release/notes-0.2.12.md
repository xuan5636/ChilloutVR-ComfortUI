# Comfort UI 0.2.12 · Public preview / 公开测试版

## Changes / 更新

- 分段选项加入真实滑动高亮：语言、界面/翻译设置、原版开关与短选项组统一使用。
- 高亮柔和减速到位，连续切换从当前显示位置继续；快速/标准/舒缓约160/220/320ms，关闭动效立即切换，中途关闭也立即停止。
- 翻译面板切换服务、开关与代理时保留控件、代理地址草稿、焦点和持续翻译状态；动画空闲时停止请求帧，逐帧不读取布局。
- 保留原生回调、禁用/键盘/语言拖动、一次保存、原版恢复和数值键盘防闪现，以及既有F8持续翻译/生命周期保护。

Segmented controls now share a persistent sliding highlight, three speeds and instant motion Off. Rapid changes continue from the visible position. Translation settings reuse controls and preserve pending proxy edits and chat state. The shared frame scheduler stops when idle.

## Downloads / 下载

| File | 用途 / Use |
| --- | --- |
| `CVRComfortUI.dll` | 模组本体，放入 `ChilloutVR/Mods/` / standalone mod |
| `ComfortUI-Setup-0.2.12.exe` | 自动查找游戏、安装/更新/移除，官方加载器引导 / installer |
| `ComfortUI-Manual-0.2.12.zip` | DLL与安装说明 / manual installation |
| `ComfortUI-Source-0.2.12.zip` | 本版本完整源码与测试 / source and tests |
| `rc.json` | CVRMG格式发布资料 / listing metadata |
| `SHA256SUMS.txt` | 下载校验 / integrity hashes |

正常退出游戏后更新，已有个人设置保留。DLL和EXE是同一个模组的两种安装方式。Exit normally before updating; do not install duplicate DLL copies.

Development target: ChilloutVR **2026r182-hf4 public-nightly**, Unity **2022.3.58f1**, Mono MelonLoader **0.8.0-ci.2548**, Windows / .NET Framework 4.8. Other branches/runtimes are unverified.

## Validation / 验证

本轮51项隔离UI检查和66项托管替身检查通过，构建0警告/错误；六项内嵌资源及安装器载荷一致。真实客户端Cohtml、PC/VR手感和长期性能仍待实测；浏览器数据不代表游戏FPS。

51 isolated UI checks and 66 managed mock checks passed, build had zero warnings/errors, and embedded assets/installer payload match. **Real-client Cohtml behavior, PC/VR comfort and long-session performance remain unverified.**

在线翻译会发送所选/可见正文给所选服务，气泡翻译每次启动默认关闭。Public translation services may fail or rate-limit. See [privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.12/docs/PRIVACY.md).

[中文安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.12/docs/INSTALLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.12/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.12/docs/VALIDATION.md)
