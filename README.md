# Comfort UI for ChilloutVR

An unofficial Windows / Mono MelonLoader mod for a calmer, more consistent ChilloutVR interface.
非官方 ChilloutVR 界面模组：柔和配色、统一控件、多语言和可选翻译。

**Current release: [0.2.13 — public preview](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/tag/v0.2.13).**

| Download / 下载 | Use / 用途 |
| --- | --- |
| [CVRComfortUI.dll](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.2.13/CVRComfortUI.dll) | Manual installation into the game's `Mods` folder / 手动安装 |
| [ComfortUI-Setup-0.2.13.exe](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.2.13/ComfortUI-Setup-0.2.13.exe) | Standalone installer, update and removal / 独立安装程序 |
| [ComfortUI-Manual-0.2.13.zip](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.2.13/ComfortUI-Manual-0.2.13.zip) | DLL and installation instructions / DLL 与说明 |
| [Source archive](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.2.13/ComfortUI-Source-0.2.13.zip) | Mod, interface assets, installer and tests / 源码包 |
| [SHA256SUMS.txt](https://github.com/xuan5636/ChilloutVR-ComfortUI/releases/download/v0.2.13/SHA256SUMS.txt) | Download integrity / 下载完整性校验 |

## Features / 功能

- Main/quick menus, settings, details, instance creation, login, virtual keyboard and HUD share a soft, configurable theme. A separate microphone control keeps navigation focused.
- 简体中文 / English / 日本語 / 한국어 / Español in a single-row language selector. Built-in labels and camera controls use local dictionaries; world/avatar/prop names remain original until translated on request.
- Smooth sliding highlights on segmented choices, including native game settings; rapid changes continue from the visible position. Three speeds and motion Off apply consistently.
- Matching switches, short-option selectors, sliders and numeric inputs. Three motion styles and three speeds; animations can be disabled in **Settings → Interface & display**.
- Camera controls grouped by frequency: zoom, focus and stabilization accessible; filters and framing grouped.
- Chat-bubble translation retains the native viewer-facing material, updates the original text output and restores its font/material when switched off.
- Optional title/description/profile-bio translation. **F8 toggles continuous translation of visible other-player chat bubbles**; press again to restore originals. Starts off each launch and can be rebound in **Settings → Controls**.
- **Settings → Translation**: Google or MyMemory, system/direct/manual HTTP proxy, connection test, cache and feature controls. Online translation sends selected/visible text to the chosen provider; see [privacy](docs/PRIVACY.md).

## Compatibility / 兼容范围

Development target: **ChilloutVR `2026r182-hf4` (`public-nightly`)**, Unity `2022.3.58f1`, **Mono MelonLoader `0.8.0-ci.2548`**, Windows, .NET Framework 4.8.
Other game branches or loader builds have not been certified. The installer opens a checksum-verified official MelonLoader installer if a Mono loader is missing; this does not certify every runtime it offers.

This release is a **public preview**. Build and 75 managed regression checks passed; version 0.2.13 has not completed a full real-client PC/VR and long-session test. See [validation](docs/VALIDATION.md) for the distinction.

## Install / 安装

1. Exit ChilloutVR normally. Run the EXE and select the game folder; follow the official loader guide if required. Then choose **安装 / 更新模组**.
2. Or install Mono MelonLoader yourself and place **only `CVRComfortUI.dll`** into `ChilloutVR/Mods/`. Do not install both copies of the same DLL under different names.
3. Start the game normally. Interface settings are in **Settings → Interface & display** and **Settings → Translation**.

Updates preserve `UserData/ComfortUI/preferences.json`. The installer backs up prior mod files and can remove its installation; manual installations can be removed by moving the DLL out of `Mods` while the game is closed. Original game HTML and third-party files are not shipped or replaced.

[详细中文安装说明](docs/INSTALLATION.zh-CN.md) · [更新日志](CHANGELOG.md) · [构建与测试](docs/BUILDING.md)

## Source / 源码

- `src/`: C# runtime, camera, chat translation and lifecycle guards.
- `assets/`: JavaScript, CSS and local language dictionaries embedded into the DLL.
- `installer/`: standalone Windows installer source.
- `tests/`, `test.ps1`: managed regression tests using mock Unity/Cohtml/HTTP implementations.
- `release/rc.json`: publication metadata matching the CVRMG Bot format.

Build: `powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\ChilloutVR"`.
Use your own game directory. This repository does not include game/Unity/Cohtml/MelonLoader binaries, personal configuration, chat logs or credentials.

Mod assembly author: **Local UI Workshop**. Repository and release publisher: **[xuan5636](https://github.com/xuan5636)**.
Unaffiliated with ChilloutVR, MelonLoader, Google and MyMemory.
