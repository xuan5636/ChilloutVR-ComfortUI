# Comfort UI 0.6.0 · Stable — Multilingual in-game keyboard

**Stable release.** A new multilingual keyboard joins the cumulative menu, camera, chat, nameplate, animation, microphone and translation-security fixes. Stable is the release channel, not a guarantee of bug-free compatibility.

- Retains the original game keyboard arrangement: input/clear/settings/collapse/close header; number row, Tab/Caps/Shift and right-side confirm; Copy/Paste/Space/arrows along the bottom. Multi-language candidates and switching are compact additions, not a replacement key arrangement. Original keyboard settings remain available.
- Choose enabled keyboards and a default in **Settings → Interface & display** or the keyboard's **+** button. Switch languages directly along the bottom. All 13 interface languages are supported; German is intentionally excluded.
- Local Chinese pinyin/Traditional candidates, Japanese romaji/kana/common kanji, Korean Hangul composition, script/accent keys and prefix word suggestions. The compact dictionaries work offline with no model download or learned/saved input history; they are not a full sentence-level IME.
- Desktop system IME/Win+H input uses a child inside the game's input area, **without a separate editor popup**. Installed Windows IMEs and speech settings are required. IME confirmation is separate from game submission; password suggestions are off. Disable system input or the enhanced keyboard to fall back.
- Text remains plain text over fixed typed JSON events, with bounded session/revision/schema checks. Existing security hardening and F8 translation controls remain.

**Validation:** 1,037 .NET checks, 75 keyboard engine checks, 40 offline keyboard UI checks, 78 existing interface checks, 637 event checks, 1,505 translation rendering/restoration and 145 malformed-response/lifecycle checks pass. Native-control checks use a hidden fixture and synthetic IME messages. **Actual Cohtml, installed IME conversion, Win+H speech and PC/VR gameplay remain unverified.** See [keyboard guide](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.6.0/docs/KEYBOARD.md) and [validation](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.6.0/docs/VALIDATION.md).

**Install:** exit ChilloutVR normally, then use the standalone EXE or the manual DLL/ZIP. Choose one installation method and retain one DLL copy. Preferences/model caches are kept; no original game HTML or game/loader binaries are redistributed. Source, checksums and [keyboard-data notices](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/v0.6.0/docs/KEYBOARD-DATA.md) are included. This release task did not install or start the game.

**中文：0.6.0 稳定版**新增游戏内多语言键盘、离线联想、中日文字候选和韩文组字；底部切换、设置勾选和独立默认语言。系统输入直接接入游戏内输入区，没有外部输入小窗。本版累计修复多项菜单、字体、相机、麦克风、聊天和安全问题。实际游戏内IME、Win+H、PC/VR仍待验证；遇到问题可关闭系统输入或切回原版键盘。更新前正常退出，设置与模型缓存保留。

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. CVRMG staff review is separate from GitHub release status.
