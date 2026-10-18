# 翻译服务设置 · 0.2.14

打开游戏 **设置 → 翻译**。所有服务都会把译文转为当前界面语言。详情页按翻译按钮显示译文，再按恢复原文。F8 开启后自动处理当前及随后可见的他人气泡；再按 F8 关闭并还原。每次启动默认关闭。

| 服务 | 配置方法 |
| --- | --- |
| Google | 使用公共接口，无需密钥。可选择系统、直连或手动代理；公共额度可能限流。 |
| MyMemory | 无需密钥，使用公共额度。通常自动检测源语言；无法识别时指定源语言。 |
| 微软 Azure | 填自己的 Translator 资源密钥，按资源填写区域，例如 eastasia；全局资源区域可留空。 |
| AI 翻译 | 填兼容 Chat Completions 的基础地址（例如 https://api.openai.com/v1）或完整接口地址、服务支持的模型名称及密钥。本模组不提供账户/额度，也不自动选择模型。 |
| LibreTranslate | 填自己使用的服务地址；例如本机 http://127.0.0.1:5000。服务如要求密钥，再填写。 |

填好地址、模型或区域后按「应用服务配置」。密钥填入单独的密码框，按「保存密钥」；保存时也会先应用当前服务配置。保存后密码框清空，页面仅显示是否已保存。更换 AI/Libre 服务地址后需重新保存密钥，旧地址的密钥不会自动发往新地址。「删除已存密钥」只删除当前服务的密钥。

密钥保存在 `UserData/ComfortUI/translation-credentials.dat`，由 Windows 当前用户加密；不会进入 `preferences.json`、网页缓存、日志、源码包或 EXE。其他电脑应各自填写密钥。不能直接复制这份加密文件让别的 Windows 用户解密。

本机代理支持「跟随系统代理」或「手动代理」。手动填写 HTTP 代理地址，例如 `http://127.0.0.1:7890`；端口以代理软件实际配置为准。不会改动 Windows 的系统代理。远程 AI/Libre 地址要求 HTTPS，本机服务可用 HTTP。

点击「测试翻译连接」会用短示例实际联网，不开启气泡模式。页面会保留结果。若提示限流/额度不足，先等待 60 秒，或主动选择其他已配置服务；清缓存不会绕过冷却。密钥错误时检查密钥、服务区域和账户权限。AI 或付费接口按所选服务规则计费。

2026-10-18 本机短示例测试：MyMemory 经系统/手动代理均返回译文；Google 两个公共端点均为 HTTP 429，仍可能不可用。微软、AI 与 Libre 的请求格式和错误处理已通过模拟检查，没有付费服务实测。真实游戏 PC/VR 需要继续确认。

接口参考：[微软 Translate](https://learn.microsoft.com/en-us/azure/ai-services/translator/text-translation/reference/v3/translate)、[OpenAI Chat Completions](https://developers.openai.com/api/reference/resources/chat/subresources/completions/methods/create)、[LibreTranslate](https://docs.libretranslate.com/api/operations/translate/)。
