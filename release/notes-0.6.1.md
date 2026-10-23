# Comfort UI 0.6.1 · Startup compatibility hotfix

Fixes the 0.6.0 startup failure that could leave the entire interface in the original game style. The loader log reported `MissingMethodException: JToken.ToString(Formatting)`. The game and Mono loader ship JSON libraries with the same assembly identity but different available overloads; an additional one-argument `WriteTo` call was also incompatible.

- Startup settings/language resources now use the existing HTML-safe typed-event JSON writer with an explicit converter array, supported by both libraries.
- Builds compile against the game's shared API and automatically check every referenced Json.NET type and method against both installed game/loader libraries. No dependency DLL is replaced or included in the download.
- Retains the 0.6.0 original five-row keyboard layout, local multilingual suggestions, language configuration and embedded system-input path. Preferences and model caches are retained.

**Validation:** the released 0.6.0 binary reproduces the two missing API references; the 0.6.1 binary passes all type/member checks against both actual libraries. Startup embedded-resource/serialization checks and 613 security checks are run separately against each library. Existing keyboard/UI fixture checks remain evidence for the unchanged 0.6.0 features; actual game restart, Cohtml/IME/Win+H/PC/VR testing is still pending. See [validation](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.6.1/docs/VALIDATION.md).

**Install:** exit ChilloutVR normally before running the EXE or replacing the single DLL in Mods. A running process keeps the previously loaded DLL until restart.

**中文：0.6.1 启动兼容热修复。**修复 0.6.0 初始化报错导致整个界面仍是原版的问题；根因是游戏与加载器的 JSON 库版本标识相同、方法重载不同。改用两者兼容的写入接口，并新增构建时的双依赖 API 检查和启动资源回归。保留原版布局的新键盘、语言和联想功能，不改个人设置或模型缓存。请先正常退出游戏再更新；实际重启游戏后的恢复效果仍待验证。

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. GitHub publication does not establish CVRMG staff approval.
