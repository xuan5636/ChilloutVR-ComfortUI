# Comfort UI 0.3.0 · Unified HUD, nameplates and language setup

0.3.0 合并翻译服务、气泡显示修复及本轮界面改进。

- **修复翻译服务和源语言无法选择**：改成可点击的单行分段按钮，带滑动高亮，关闭动效立即切换。保留5个翻译服务，切换选项只保存配置。
- **首次启动跟随系统显示语言**：没有偏好文件时匹配简中/英/日/韩/西，其他语言回退英文；中文地区使用简体中文。升级保留已有选择。
- **HUD与字体**：连接提示补译、去掉大块背景，普通通知改为紧凑卡片；麦克风独立状态卡、音量条与通知标记，常用文字增加字重。
- **其他玩家名牌**：圆角卡片，整理头像、姓名、角色、好友标记及说话提示，保留原生面向玩家材质和显隐设置。设置 → 界面与显示可切回原版名牌。
- 保留 Google/MyMemory/微软Azure/兼容AI/LibreTranslate、代理与加密密钥设置、F8持续气泡翻译、原气泡朝向和页面生命周期保护。

Clickable sliding selectors replace unusable provider/source-language lists. First startup follows a supported Windows display language; saved choices win on upgrades. The HUD gains compact localized notices, a microphone card and clearer text. Remote-player nameplates use rounded cards while retaining native font atlases, viewer-facing materials and visibility; the original style remains available in settings.

| 下载 / Download | 用途 / Use |
| --- | --- |
| `CVRComfortUI.dll` | 独立模组DLL，放入游戏Mods文件夹 |
| `ComfortUI-Setup-0.3.0.exe` | 独立安装、更新和移除；含官方加载器引导 |
| `ComfortUI-Manual-0.3.0.zip` | DLL与安装、翻译、隐私、更新说明 |
| `ComfortUI-Source-0.3.0.zip` | 本版源码、界面资源、安装器和测试 |
| `SHA256SUMS.txt` | 下载完整性校验 |

正常退出游戏后更新，EXE与DLL为两种安装方式，只保留一份模组。已有设置会保留，安装程序备份旧DLL。首次自动语言只在没有偏好文件时应用。

**验证范围：**127项托管检查与72项隔离界面检查通过；实际游戏依赖构建0警告/错误，6个内嵌资源和安装器载荷一致。隔离测试不等同真实Cohtml/PC/VR通过；名牌镜面朝向、长名、HUD舒适度、长期性能与其他电脑首次安装仍待实测。发布沿用已安装的二进制，不再次修改游戏。

Historical 0.2.14 network probes returned MyMemory HTTP200 and Google HTTP429. Google public quota availability remains unresolved; 0.3.0 does not make a new availability claim. Microsoft/AI/Libre require the user's own service configuration, with real accounts still unverified. Paid/AI providers may charge for usage. F8 translation starts off each launch; enabling it sends visible bubble text to the selected provider. No automatic cross-provider forwarding.

Development target: Windows / .NET Framework4.8, ChilloutVR **2026r182-hf4 public-nightly**, Unity **2022.3.58f1**, Mono MelonLoader **0.8.0-ci.2548**. Existing CVRMG0.2.12 submission remains pending; this update publishes to GitHub only.

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Modifications may affect performance, security or stability.

[安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.0/docs/INSTALLATION.zh-CN.md) · [翻译设置](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.0/docs/TRANSLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.0/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.0/docs/VALIDATION.md) · [Privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.0/docs/PRIVACY.md)
