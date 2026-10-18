# Comfort UI 0.3.2 · Microphone display hotfix

修复用户报告的HUD麦克风卡片仅剩「已静音」文字、图标不显示的问题。

- **直接绘制麦克风图形**：用普通HTML/CSS绘制胶囊、支架、底座和静音斜线，不再依赖编码SVG图片、内联SVG或图像解码；另有TTS标识。
- **保留状态与动画**：读取原生麦克风/TTS真实状态，文字、图形、音量和显隐保持同步。短交叉淡化/缩放跟随三档速度，快速切换可接续，关闭动效立即切换。
- **缩放与文案**：明确图形尺寸和线宽并跟随HUD缩放；五语言长状态文字在卡片内居中换行。
- **通知图标**：引用游戏已有的本地图片路径，移除HUD编码图片依赖；不复制或分发游戏图像文件。
- 保留0.3.1的免费Bing、小型CPU/可选Vulkan本地模型、名牌、多语言及已有设置/模型缓存。

The microphone is now drawn using basic HTML/CSS boxes rather than an encoded SVG image. Muted/live/TTS symbols, native state, the volume meter and visibility are retained. Five-language labels wrap within the card; the glyph scales with the native HUD. Notification badges refer to existing game files. No proprietary image is bundled.

**验证范围：**140项托管、118项隔离UI检查通过；新增16项覆盖图像解码不可用、三状态实际栅格差异、配色、五语言边界与缩放。冻结0.3.1在新的图像独立绘制回归中失败，0.3.2通过。这是模拟已报告的失败路径，不代表已证实Cohtml内部确切原因。真实0.3.2游戏Cohtml/PC/VR/镜面、其他电脑与长期运行仍需确认。

This release does not run new online translation probes. The genuine standalone Bing/CPU/Vulkan tests belong to0.3.1; Google's prior public429 remains unresolved. Small local model translations may be inaccurate. Existing CVRMG0.2.12 submission remains pending; no Discord update was sent.

| 下载 / Download | 用途 |
| --- | --- |
| `CVRComfortUI.dll` | 独立模组DLL，放入游戏Mods文件夹 |
| `ComfortUI-Setup-0.3.2.exe` | 独立安装、更新、移除及官方加载器引导 |
| `ComfortUI-Manual-0.3.2.zip` | DLL及安装、翻译、隐私、验证、许可和更新说明 |
| `ComfortUI-Source-0.3.2.zip` | 对应本版的源代码、界面、安装器与离线测试 |
| `SHA256SUMS.txt` | 下载完整性校验 |

正常退出游戏后更新；EXE与DLL为两种安装方式，只保留一份同模组DLL。安装会备份旧DLL、保留设置与模型缓存。本次发布直接沿用已安装并校验的0.3.2文件，不重建或再次安装。此前0.3.1和更早附件保持不变。

Development target: Windows x64 / .NET Framework4.8, ChilloutVR2026r182-hf4 public-nightly, Unity2022.3.58f1, Mono MelonLoader0.8.0-ci.2548.

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Modifications may affect performance, security or stability.

[安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.2/docs/INSTALLATION.zh-CN.md) · [翻译设置](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.2/docs/TRANSLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.2/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.2/docs/VALIDATION.md) · [Privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.2/docs/PRIVACY.md)
