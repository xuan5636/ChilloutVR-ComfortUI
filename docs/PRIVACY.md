# Translation and local data

Built-in UI and camera translations are local dictionaries. Optional online translation sends only the selected title/description/bio or visible chat text to the explicitly selected service. It adds no account identifiers. Text itself can contain names. F8 continuous mode starts off each launch; pressing again restores originals. Only local display changes.

Five services are supported:

- Google: public `translate.googleapis.com/translate_a/single`, with one same-service fallback to `translate.google.com`. Subject to public quotas; no key.
- MyMemory: `api.mymemory.translated.net/get`; public quota, no key. Source detection or an explicitly chosen source language.
- Microsoft Azure Translator: official HTTPS Text Translation v3 endpoint, with a user-provided subscription key and optional resource region.
- AI: a user-configured Chat Completions compatible HTTPS address, explicit model and key. Local HTTP endpoints may omit a key. Sends a translation-only system prompt and the source as user content, without tools. AI/paid providers may charge fees.
- LibreTranslate: a user-configured HTTPS endpoint or local HTTP service, with an optional key. No public instance is selected automatically.

The mod does not switch to a different provider automatically. It reuses transports, bounds in-flight work, paces public calls, and pauses requests for 60 seconds after quota errors. Connection tests bypass translation cache but respect this cooldown. Tests use a fixed short sample; no game content is needed for them.

System, direct and manual HTTP proxy modes affect only this mod's requests. The system mode reads Windows proxy configuration; the mod never changes it. Proxy credentials in the address are not supported. The provider and the chosen proxy are outside the mod's control.

Preferences and non-secret service settings are in `UserData/ComfortUI/preferences.json` and browser preference storage. API keys are kept separately in `translation-credentials.dat` using Windows DPAPI for the current user, bound to the configured AI/Libre endpoint. The UI receives only saved/missing flags. Key input is cleared after submitting through a dedicated local native call; keys never enter preferences, browser storage or diagnostic logs. Changing endpoints requires explicitly saving a key for that endpoint. Delete controls remove one provider's saved key. Encrypted files are not portable to another Windows account.

Translation cache is memory-only and can be cleared. Diagnostics contain service, proxy mode, destination host and HTTP status/failure code, never content, URL query/path or keys. Source packages and installers exclude preferences, keys, chats and game dependencies.

本地界面汉化不联网。气泡与详情翻译将正文发送给所选服务；不会自动跨服务转发。F8 默认关闭、再次按下恢复原文。密钥只在当前 Windows 用户下加密保存，不进入偏好、网页缓存或日志。AI 或付费服务可能产生费用。设置方法见 [中文说明](TRANSLATION.zh-CN.md)。
