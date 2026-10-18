# Comfort UI 0.2.14 · Translation services / 翻译服务更新

- 新增 **微软 Azure、可配置 AI、LibreTranslate**，保留 Google 和 MyMemory。详情与 F8 持续气泡翻译共用服务、源语言和代理设置。
- 改进连接复用、公共请求间隔、相同请求合并及 60 秒限流冷却；Google 只有一个同服务备用端点，不会自动转发到其他服务。
- 翻译设置提供接口地址、模型、微软区域、源语言、连接测试与密钥管理。密钥按当前 Windows 用户加密保存，不进入偏好、网页缓存或日志。更换 AI/Libre 地址后需要重新保存密钥。
- 具体错误会保留在设置页面，区分限流、认证、代理、超时、地址/模型配置与响应异常。保留 F8 持续开关、气泡朝向材质和原文恢复保护。

Adds Microsoft Azure Translator, user-configured Chat Completions compatible AI and LibreTranslate. Transport reuse, bounded requests, public-service pacing, duplicate coalescing and quota cooldown improve reliability. API keys use per-Windows-user encryption and are kept outside preferences, browser storage and logs. No automatic cross-provider fallback.

| 下载 / Download | 用途 / Use |
| --- | --- |
| `CVRComfortUI.dll` | 模组 DLL，放入游戏 Mods 文件夹 |
| `ComfortUI-Setup-0.2.14.exe` | 独立安装、更新和移除；含官方加载器引导 |
| `ComfortUI-Manual-0.2.14.zip` | DLL、安装/翻译设置说明、隐私说明与更新日志 |
| `ComfortUI-Source-0.2.14.zip` | 本版源码、界面资源、安装器与测试 |
| `SHA256SUMS.txt` | 完整性校验 |

正常退出游戏后更新。EXE 和 DLL 是两种安装方式，只保留一份模组；已有设置会保留。微软需要自己的 Azure 密钥/区域，AI 需要服务地址、模型与密钥，Libre 使用自己的服务；本模组不提供账户或额度。AI/付费接口可能计费。

**验证范围：**93 项托管检查与 34 项隔离界面检查通过；实际游戏依赖构建 0 警告/错误，六个内嵌资源及安装器载荷一致。真实代理短示例中 **MyMemory 系统/手动均成功，Google 两个公共端点均返回 HTTP429**；不能保证 Google 公共额度。微软/AI/Libre 仅模拟接口验证，真实账户、游戏 PC/VR 与长期运行仍待测试。

Development target: Windows / .NET Framework 4.8, ChilloutVR **2026r182-hf4 public-nightly**, Unity **2022.3.58f1**, Mono MelonLoader **0.8.0-ci.2548**. Managed and browser stand-ins do not establish real-client/VR or paid-provider verification. HUD/font/nameplate redesign remains subsequent work.

气泡翻译默认关闭，开启后将可见正文发送给所选服务，只改本机显示。发布不会自动安装本机游戏。CVRMG 既有 0.2.12 提交仍待审核，本次只发布 GitHub。

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Modifications may affect performance, security or stability.

[安装说明](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.14/docs/INSTALLATION.zh-CN.md) · [翻译服务设置](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.14/docs/TRANSLATION.zh-CN.md) · [更新日志](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.14/CHANGELOG.md) · [验证范围](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.14/docs/VALIDATION.md) · [Privacy](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.2.14/docs/PRIVACY.md)
