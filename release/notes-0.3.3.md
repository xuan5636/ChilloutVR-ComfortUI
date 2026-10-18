# Comfort UI 0.3.3 · Microphone state and animation hotfix

修复进入在线房间后麦克风卡片仍停在「已静音」，以及开启后图标仍有静音斜线的问题。

- **读取实际语音状态**：直接从游戏语音管理器读取静音/TTS，不再仅根据原版网页节点推断。HUD新建、重载时提供初始快照，状态变化即时同步，每秒补发一次快照；不修改实际麦克风开关和语音传输。
- **图形和文字保持一致**：斜线/TTS直接设置元素显隐，与文案共用状态；延迟到来的旧HUD节点或事件不能覆盖实际状态。
- **动画可可靠收尾**：保留柔和交叉淡化与轻缩放，并给暂停/限速的动画帧增加收尾保护；快速切换接续，关闭动效立即完成。
- 只向HUD传递状态，保留现有主线程Cohtml生命周期保护；稳定帧不反复执行JS，相同快照不写DOM。保留配色、五语言、名牌、翻译以及设置和模型缓存。

Microphone labels and glyphs now follow the game voice manager rather than stale HUD DOM after joining an online room or reloading the HUD. The muted slash and TTS mark are updated directly, and animation cleanup still completes if animation frames stop. Native mute/TTS behavior is read only; audio and input behavior are unchanged.

**验证：156项托管、142项隔离UI检查通过。** 新增16项覆盖实际状态与传递策略，24项覆盖过期HUD节点、错误显隐样式、暂停动画帧、快速切换、初始化与五语言。0.3.2缺少实际状态快照路径，在新增回归中失败。使用原版只读HTML、模拟桥接和静音Edge；实际Cohtml内部故障原因没有自动复现，真实在线房间、PC/VR、其他电脑与长期运行仍需确认。

This is a preview, not a real-client certification. No new online translation probes were run. Google's historical public429 remains unresolved; local model translations may be inaccurate. Existing CVRMG0.2.12 review is unchanged; no Discord message was sent.

| 下载 / Download | 用途 |
| --- | --- |
| `CVRComfortUI.dll` | 独立模组DLL，放入游戏Mods文件夹 |
| `ComfortUI-Setup-0.3.3.exe` | 独立安装、更新、移除及官方加载器引导 |
| `ComfortUI-Manual-0.3.3.zip` | DLL与安装/翻译/隐私/验证/许可/更新说明 |
| `ComfortUI-Source-0.3.3.zip` | 对应源代码、界面、安装器与离线测试 |
| `SHA256SUMS.txt` | 下载完整性校验 |

正常退出游戏后更新。EXE与DLL为两种安装方式，只保留一份本模组DLL。更新备份旧DLL并保留设置、模型缓存。本次发布沿用本机已经安装并校验的0.3.3二进制，不重建或再次安装；0.3.2及更早发布不变。

Development target: Windows x64 / .NET Framework4.8, ChilloutVR2026r182-hf4 public-nightly, Unity2022.3.58f1, Mono MelonLoader0.8.0-ci.2548.

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Modifications may affect performance, security or stability.

[安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.3/docs/INSTALLATION.zh-CN.md) · [翻译设置](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.3/docs/TRANSLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.3/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.3/docs/VALIDATION.md)
