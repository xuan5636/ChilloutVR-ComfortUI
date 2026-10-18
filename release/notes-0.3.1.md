# Comfort UI 0.3.1 · HUD fixes, free Bing and a small local model

按照用户安排，先发布已经安装的0.3.1，随后另做修复版；本次发布沿用现有DLL/EXE，不修改游戏。

- 麦克风状态读取改为跟随原生DOM，增加状态过渡动画，关闭动效立即切换。
- 调整世界空间名牌圆角密度、姓名宽度和角色行布局，保留原生材质与显隐。
- 补齐加入、退出、连接成功/失败和语音连接相关五语言提示。
- 微软改为 **Bing 免费网页翻译**，无需 Azure 密钥/区域。
- LibreTranslate 改为 **轻量本地模型**：Qwen2.5-0.5B Q4_K_M约491MB，默认CPU；首次另下载约20MB可移植CPU运行文件，可选Vulkan再约32MB。显式准备、进度/取消、固定SHA256；不需Python/Ollama/CUDA。复杂语句可能误译，本地每字段上限1200字符。
- 保留F8持续翻译开关、气泡原位/材质保护、代理、AI加密密钥、语言和配色设置。

**已知问题：用户安装后报告HUD麦克风卡片仅显示状态文字、没有图标。0.3.1保留当前二进制供版本追溯，将在下一修复版处理。**

140 managed checks and 102 isolated browser checks passed. Standalone synthetic probes returned text from free Bing in five languages and from the small model on CPU/Vulkan. These are not real-client PC/VR or other-computer certifications; a Korean model sample mistranslated a phrase. Google public429 remains unresolved. The microphone icon issue above was reported in the real client after the isolated checks.

| 下载 / Download | 用途 |
| --- | --- |
| `CVRComfortUI.dll` | 独立模组，放入游戏Mods目录 |
| `ComfortUI-Setup-0.3.1.exe` | 独立安装、更新、移除及官方加载器引导 |
| `ComfortUI-Manual-0.3.1.zip` | DLL及安装、翻译、隐私、验证、许可和更新说明 |
| `ComfortUI-Source-0.3.1.zip` | 本版源代码、界面、安装器和离线测试 |
| `SHA256SUMS.txt` | 下载校验 |

正常退出游戏后更新，EXE与DLL只选一种安装方式；已有设置保留并备份旧DLL。其他电脑的本地模型在「设置 → 翻译」显式下载准备，下载遵循所选代理，推理仅在本机。首次语言仅在没有偏好文件时跟随支持的系统显示语言。

Development target: Windows x64 / .NET Framework4.8, ChilloutVR2026r182-hf4 public-nightly, Unity2022.3.58f1, Mono MelonLoader0.8.0-ci.2548. Existing CVRMG0.2.12 submission remains pending; this is a GitHub update only.

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Modifications may affect performance, security or stability.

[安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.1/docs/INSTALLATION.zh-CN.md) · [翻译设置](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.1/docs/TRANSLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.1/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.1/docs/VALIDATION.md) · [Privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.3.1/docs/PRIVACY.md)
