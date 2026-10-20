# Comfort UI 0.5.1 · Languages, camera & HUD

This public preview combines the previously local 0.5.0 language update with the 0.5.1 repairs. / 本公开预览版合并此前0.5.0语言更新及0.5.1修复。

- **13 languages:** adds Traditional Chinese, French, Russian, Brazilian Portuguese, Italian, Indonesian, Polish and Turkish to the existing five. First startup follows a supported Windows display language; saved choices take precedence. German is not included. / 共13种语言，首启匹配支持的系统显示语言，升级保留选择。
- **Language selection:** the common five stay in one sliding row; other languages and the translation source list support native wheel/drag scrolling, paging buttons and keyboard navigation. / 修复更多语言及源语言列表无法滚动的问题。
- **Korean:** explicit language and font selection use the game's existing Korean font, covering all 590 Hangul characters used in the pack. / 修复韩语字体选择，无需额外下载字体。
- **Camera:** a direct close button uses the native close flow; hover backgrounds restore, and desktop drawers stay within the preview. VR retains larger external controls. / 相机新增关闭按钮，修复悬停背景和横屏分类越界。
- **Motion:** optional fourth style **Orbit burst / 跃迁弹入**, with three speeds. Rotation, scale and spring movement play only when selected; motion Off disables them. / 新增第四种可选动效，保留三档速度及关闭选项。
- **Microphone:** deferred layout and small HUD scale no longer cause a permanent fallback to the native icon; the original remains visible until a complete replacement can be laid out. Voice state, speech highlight, level meter and native cues are retained. / 修复麦克风样式误回退，保留真实状态与语音反馈。

**Downloads:** use `ComfortUI-Setup-0.5.1.exe` for guided installation, or install only `CVRComfortUI.dll` into `Mods` after setting up Mono MelonLoader. Exit the game before updating. `ComfortUI-Manual-0.5.1.zip` includes the DLL and instructions; `ComfortUI-Source-0.5.1.zip` includes source, assets, installer and tests. Compare downloads with `SHA256SUMS.txt`. Personal settings and model caches are preserved. / EXE与DLL为两种安装方式，更新前正常退出游戏，保留设置与模型缓存。

**Validation:** 367 managed checks, 78 isolated browser checks and 32 native-camera static contracts passed. The public DLL/EXE are the verified locally installed binaries. Publication is authorized by the user; these checks do not certify actual Cohtml/PC/VR behavior, other computers, online translation availability or long sessions. Some low-frequency translations remain machine-drafted and need native-speaker review. See [validation](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.5.1/docs/VALIDATION.md) and the [changelog](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.5.1/CHANGELOG.md).

Development target: Windows, ChilloutVR `2026r182-hf4` public-nightly, Mono MelonLoader `0.8.0-ci.2548`, .NET Framework 4.8. Other branches and loaders need separate verification. Unofficial mod; not affiliated with or supported by ChilloutVR or Alpha Blend Interactive.
