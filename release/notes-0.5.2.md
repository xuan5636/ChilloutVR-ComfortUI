# Comfort UI 0.5.2 · Translation bridge hardening

This preview addresses the CVRMG review concern about translation data entering `ExecuteScript`.

- Translation responses now use fixed, typed Cohtml events. Runtime text is parsed as JSON and never concatenated into executable script.
- Initialization, preferences, notices and local-model progress use the same code/data separation, with a page-ready handshake and native lifetime checks.
- Explicit HTML-safe JSON encoding, strict response types/lengths, request/language matching, cancellation and unload checks. Translations remain text-only and the toggle restores original content.
- Retains the 13-language interface, camera controls, chat bubbles, nameplates, motion choices and microphone fixes from 0.5.1. F8 remains a continuous translation toggle.

更新：翻译结果、启动数据和设置改用固定事件传递，不再把外部文字拼进脚本；补齐显式转义、响应格式/长度、请求与语言匹配、取消和页面关闭检查。保留0.5.1的语言、相机、聊天、名牌及麦克风功能。

Validation: 986 managed checks, 637 offline event checks, 1,505 isolated translation rendering/restoration checks and 145 malformed-response/lifecycle checks. The serializer is additionally tested against the installed Mono loader's Json.NET 13.0.4. See [security review notes](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.5.2/docs/SECURITY.md) and [validation limits](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.5.2/docs/VALIDATION.md).

These checks use stand-ins, synthetic content and muted headless Edge. Actual Cohtml/PC/VR behavior, other computers and long sessions remain unverified. This release does not claim that arbitrary script execution was reproduced in earlier versions, or that CVRMG staff have approved the update.

Download either `ComfortUI-Setup-0.5.2.exe` or `CVRComfortUI.dll`/the manual ZIP. Exit the game normally before installing; existing preferences and model caches are retained. Source, SHA256 checksums and `rc.json` are provided. The local development build does not install itself.
