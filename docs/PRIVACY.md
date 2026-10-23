# Translation and local data · 0.6.0

UI, camera and system-notification localization uses local dictionaries. Optional Google, MyMemory, free Bing web translation or a configured AI service sends the selected description/profile text or visible chat text only to that chosen service. F8 starts off each launch; pressing again restores originals. No automatic cross-provider fallback occurs.

Bing uses its public web translator and temporary in-memory session tokens. It never receives old Azure subscription keys. Public services may throttle, change their pages or require verification; the mod reports failure and does not bypass challenges. AI keys remain Windows-user DPAPI protected and endpoint bound, outside preferences, browser storage and logs. Legacy Azure/Libre encrypted entries remain untouched and unused.

The optional small local model runs on this PC. The explicit Prepare button downloads a pinned official Qwen model and pinned llama.cpp Windows runtime through the chosen proxy, verifies SHA256 and extracts only inside `UserData/ComfortUI/LocalTranslation/`. Inference uses an authenticated random loopback port, bypassing remote proxies; chat is never included in download requests or process arguments. No prompt/response logs are retained. CPU is the baseline; optional Vulkan uses a supported GPU with CPU fallback. Idle timeout, unload and a Windows kill-on-close job clean up only the owned server. Model files remain after uninstall so the user can retain or delete the dedicated cache.

Translation cache is memory-only. Diagnostics include only service, proxy mode, destination host and HTTP status/failure code. Source, DLL and EXE packages contain no game assemblies, model binaries, preferences, credentials or chats. Synthetic tests do not use real players' content. See [settings and download sizes](TRANSLATION.zh-CN.md).

0.6.0 separates code from data at the native/web boundary. Translation, settings and initialization payloads use fixed typed events, with explicit HTML-safe JSON encoding and text-only translated output. No telemetry or additional provider requests are added by the bridge. See [security notes](SECURITY.md).

## Keyboard input in 0.6.0

Local suggestions use bundled word lists; Comfort UI does not learn, log, persist or send typed text for completion. Input/session JSON is data only. PC system input uses a native child control in this process's game window, with no global key hook or observation of other apps. It is removed on keyboard close/unload. Win+H is Windows dictation and follows Microsoft's speech permissions/privacy/network settings; the mod does not capture audio. Password fields have no suggestions. Explicit game confirmation and existing optional chat translation retain their separate behavior. See [keyboard controls and limits](KEYBOARD.md).

The original in-memory, non-password single-line submission history is retained for the native-position Up/Down buttons; no history file or learning database is created. Explicit Copy writes only the selected/current text to the system clipboard; it is disabled for password fields. The mod does not poll or upload the clipboard.
