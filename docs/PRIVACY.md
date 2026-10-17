# Translation and local data

Built-in UI translations are local JSON dictionaries and do not require an online service.

Online content translation is optional. Detail translation sends selected world/avatar/prop titles and descriptions, or a profile biography, to the provider selected in Settings → Translation. Chat translation starts off each launch; when toggled on it sends currently visible and subsequently visible other-player bubble text, and turning it off restores originals. Text may itself contain names or personal information. The mod does not add separate user/account identifiers to translation requests, send chat messages, change other users' displays, or enable translation automatically on launch.

Google uses the public `translate.google.com/translate_a/single` web endpoint; MyMemory uses `api.mymemory.translated.net/get`. These are third-party services, subject to network failures, quotas and provider policies. No API key is required by this implementation. Text is included in HTTPS requests; the selected provider receives it. Proxies and their logging policies are outside this mod's control.

System proxy, direct access or a manual HTTP proxy can be selected locally. The mod does not alter Windows proxy settings and does not support embedding proxy usernames/passwords in its address field. Connection testing sends the fixed sample `Hello.` and bypasses the translation cache.

Preferences are stored in `UserData/ComfortUI/preferences.json`. Translation cache is held in memory and can be cleared in settings. The installer writes install state and rollback copies under `UserData/ComfortUI/`; these are not part of the source repository or release payload.

本地界面汉化不联网。内容和气泡翻译会把选中的内容或可见气泡正文发送给所选服务；气泡持续翻译每次启动默认关闭，按 F8 开启，再按关闭并恢复原文。仅改变本机显示。
