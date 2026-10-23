# Comfort UI for ChilloutVR

An unofficial Windows / Mono MelonLoader mod for a calmer, more consistent ChilloutVR interface.
非官方 ChilloutVR 界面模组：柔和配色、统一控件、多语言和可选翻译。

**Current release: [0.6.1 — Stable · startup compatibility hotfix](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/tag/v0.6.1).** Fixes the 0.6.0 initialization error that left the entire interface in the original game style when its older JSON library was loaded. Retains the original-layout multilingual keyboard, local suggestions and embedded system-input path. **0.6.1 启动热修复**：修复新界面未启动、仍显示原版的问题，保留 0.6.0 键盘功能和个人设置。真实客户端重启恢复、IME/Win+H/VR 验证仍待完成，详见[验证记录](docs/VALIDATION.md)。

| Download / 下载 | Use / 用途 |
| --- | --- |
| [CVRComfortUI.dll](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.6.1/CVRComfortUI.dll) | Manual installation into the game's `Mods` folder / 手动安装 |
| [ComfortUI-Setup-0.6.1.exe](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.6.1/ComfortUI-Setup-0.6.1.exe) | Standalone installer, update and removal / 独立安装程序 |
| [ComfortUI-Manual-0.6.1.zip](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.6.1/ComfortUI-Manual-0.6.1.zip) | DLL and installation instructions / DLL 与说明 |
| [Source archive](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.6.1/ComfortUI-Source-0.6.1.zip) | Mod, interface assets, installer and tests / 源码包 |
| [SHA256SUMS.txt](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.6.1/SHA256SUMS.txt) | Download integrity / 下载完整性校验 |

## Features / 功能

- Keeps the original five-row keyboard arrangement and function-key proportions, plus its settings/collapse controls. Configurable 13-language in-game keyboard, bottom language switching, enabled/default language controls, local word suggestions and Chinese/Japanese/Korean composition. Desktop system IME/Win+H input uses a child of the game window, with no separate editor dialog. See [keyboard guide and limitations](docs/KEYBOARD.md).
- Main/quick menus, settings, details, instance creation, login, virtual keyboard and HUD share a soft, configurable theme. A separate microphone control keeps navigation focused.
- First startup without a saved preference file follows the Windows display language when it maps to one of 13 supported languages; unsupported languages fall back to English. Upgrades retain the saved choice.
- 13 languages: Simplified/Traditional Chinese, English, Japanese, Korean, Spanish, French, Russian, Brazilian Portuguese, Italian, Indonesian, Polish and Turkish. The five frequent choices stay on a single-row slider; the other eight open from a secondary selector with wheel/drag scrolling and paging buttons. Some low-frequency translations are machine-drafted and need native-speaker review. Built-in labels and camera controls use local dictionaries; world/avatar/prop names remain original until translated on request.
- Smooth sliding highlights on segmented choices, including native game settings; rapid changes continue from the visible position. Three speeds and motion Off apply consistently.
- Matching switches, short-option selectors, sliders and numeric inputs. Four motion styles and three speeds; optional Orbit burst adds rotation and spring movement. All styles can be disabled in **Settings → Interface & display**.
- Actual voice-manager snapshots, a voice-input highlight and proportional level meter, native microphone cues through persistent 2D output, directly updated glyphs and bounded state transitions, compact localized HUD connection hints, notification cards and a separate microphone status card. Remote-player nameplates use rounded cards with organized portraits/names/status and a subtle border matching each player's primary colour selected in My Profile and synchronized by the game, while retaining native billboard materials and visibility; the original style is available in Interface settings.
- Translation service and source language use clickable single-row segmented controls with sliding highlights; no native HTML select is required.
- Camera controls include a direct close button, restored hover appearance and a bounded desktop drawer. Controls are grouped by frequency: zoom, focus and stabilization accessible; filters and framing grouped.
- Native in-world chat bubbles use readable, content-sized rounded cards with centered downward pointers towards their player. Matching three-dot typing hints retain native status and lifetime; motion Off makes the dots steady. The theme follows the menu palette while retaining native facing, opacity and size settings. Original chat and typing layouts are available in Settings → Interface & display → Chat bubble style.
- Chat-bubble translation retains the native viewer-facing material, updates the original text output and restores its font/material when switched off.
- Optional title/description/profile-bio translation. **F8 toggles continuous translation of visible other-player chat bubbles**; press again to restore originals. Starts off each launch and can be rebound in **Settings → Controls**.
- **Settings → Translation**: Google, MyMemory, free Bing, configurable AI or a lightweight local model; source language, encrypted keys, proxy, connection test and cache controls. Online translation sends selected/visible text to the chosen provider; see [privacy](docs/PRIVACY.md).

## Compatibility / 兼容范围

Development target: **ChilloutVR `2026r182-hf4` (`public-nightly`)**, Unity `2022.3.58f1`, **Mono MelonLoader `0.8.0-ci.2548`**, Windows, .NET Framework 4.8.
Other game branches or loader builds have not been certified. The installer opens a checksum-verified official MelonLoader installer if a Mono loader is missing; this does not certify every runtime it offers.

This is a stable-channel release, not a claim of bug-free operation. Automated managed, native Windows child-control and muted offline browser checks are documented in [validation](docs/VALIDATION.md). Actual Cohtml/PC/VR keyboard/Win+H compatibility, other computers and long sessions remain unverified. The [fixed typed data bridge](docs/SECURITY.md) is retained.

## Install / 安装

1. Exit ChilloutVR normally. Run the EXE and select the game folder; follow the official loader guide if required. Then choose **安装 / 更新模组**.
2. Or install Mono MelonLoader yourself and place **only `CVRComfortUI.dll`** into `ChilloutVR/Mods/`. Do not install both copies of the same DLL under different names.
3. Start the game normally. Interface settings are in **Settings → Interface & display** and **Settings → Translation**.

Only the first launch without `UserData/ComfortUI/preferences.json` detects the system display language. Chinese script/region variants select Simplified or Traditional Chinese; Portuguese variants select the available Brazilian pack. Other unsupported languages use English. Existing preferences are kept.

Updates preserve `UserData/ComfortUI/preferences.json`. The installer backs up prior mod files and can remove its installation; manual installations can be removed by moving the DLL out of `Mods` while the game is closed. Original game HTML and third-party files are not shipped or replaced.

[详细中文安装说明](docs/INSTALLATION.zh-CN.md) · [更新日志](CHANGELOG.md) · [构建与测试](docs/BUILDING.md)

## Source / 源码

- `src/`: C# runtime, camera, chat translation and lifecycle guards.
- `assets/`: JavaScript, CSS and local language dictionaries embedded into the DLL.
- `installer/`: standalone Windows installer source.
- `tests/`, `test.ps1`: managed regression tests using mock Unity/Cohtml/HTTP implementations.
- `release/rc.json`: current publication metadata matching the CVRMG Bot format.

Keyboard data has separate [CC BY-SA/MIT/Apache attribution](docs/KEYBOARD-DATA.md).

Language-pack maintenance and regression guidance: [Localization](docs/LOCALIZATION.md).

Build: `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\ChilloutVR"`.
Use your own game directory. This repository does not include game/Unity/Cohtml/MelonLoader binaries, personal configuration, chat logs or credentials.

Mod assembly author: **Local UI Workshop**. Repository and release publisher: **[xuan5636](https://github.com/xuan5636)**.
Unaffiliated with ChilloutVR, MelonLoader and the listed translation providers.
