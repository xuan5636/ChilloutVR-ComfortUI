# Multilingual keyboard · 0.6.0

The enhanced keyboard adapts the live original keyboard layout in place: five rows, number keys, Tab/Caps/Shift, Backspace, right-side confirm, and Copy/Paste/Space/arrows. The clear/input/settings/collapse/close header keeps the original positions and function-key proportions. The original keyboard settings and desktop-collapsed option remain usable; candidates and language switching are compact additions. No native game HTML is distributed or replaced. It does not open a separate editor dialog. Enable it in **Settings → Interface & display → Choose keyboards**. The `+` button on the keyboard opens the same language selection. Enable any of the 13 supported languages and choose a default independently of the interface language. First use defaults to the interface language plus English. The bottom globe button cycles through enabled languages; labelled chips select them directly.

## Typing and suggestions

- Simplified/Traditional Chinese: unaccented pinyin and local phrase candidates. Tap a candidate or press 1–8; Space/Enter confirms the composition first. The bundled dictionary is bounded, without fuzzy pinyin or a complete sentence-level conversion engine.
- Japanese: romaji to hiragana, katakana and common kanji/phrase candidates. The small local kanji list is not a replacement for a complete system Japanese IME.
- Korean: two-set Hangul composition, compound vowels/finals and key-by-key deletion. A space confirms the typed syllables; longer word completion is optional via candidates.
- English, Spanish, French, Russian, Brazilian Portuguese, Italian, Indonesian, Polish and Turkish: local prefix completions and script/accent keys. French uses AZERTY, Russian Cyrillic and Korean two-set labels. Latin layouts are compact layouts rather than exact copies of every regional physical keyboard.
- Suggestions use bundled frequency words, with no learned history, network call or language-model download. They do not cover every word or every language in the world. Disable suggestions separately from the keyboard. Switching languages commits unfinished text first.
- Shift+Enter inserts a line break where the original field permits it. Enter during composition confirms text; use the explicit confirm button to submit. Password fields are masked and have no suggestions. Closing clears this session's fields; suggestions do not learn or save text. The original in-memory history of confirmed non-password single-line entries is retained for Up/Down, without writing a history file. Explicit Copy writes the selected text (or all text when there is no selection) to the Windows clipboard; passwords disable Copy.

## Windows IME and Win+H

On Windows 10/11 desktop mode, click the text area or **System input · Win+H**. Comfort UI creates a standard Windows EDIT **child inside its own game window**, positioned over the game's input area. The game UI paints the text; the native child receives the installed IME/accessibility text services. There is no external input popup or global keyboard hook. The existing game callback still decides what confirmation does; composing text must not automatically send a message.

The keyboard chooses an already-installed matching Windows input layout when available, restores the previous thread layout on close, and never installs language packs or changes global defaults. Use Win+Space to switch an installed IME if necessary. Chinese/Japanese/Korean system conversion requires the matching Windows IME. Win+H uses Windows speech settings, supported dictation languages, permissions and connectivity; speech processing belongs to Windows, not this mod. Password dictation may be blocked by Windows.

The child is cleaned up when the keyboard/session closes or the mod unloads. The voice toolbar may temporarily take foreground without destroying the edit target. Other applications' text and keystrokes are never monitored. Keyboard text travels as typed JSON events with session, revision, length and schema checks; it is never concatenated into executable JavaScript or rendered as HTML.

Native input is desktop-only. VR and unavailable window/field mappings keep the in-game virtual keyboard and paste available. Exclusive fullscreen, unusual window scaling, other IME mods and actual Win+H/VR operation still require real-client testing. If native input is unavailable, turn off **System IME and voice**; to restore the original game keyboard, disable **Comfort keyboard**.

## 中文使用方法

在「设置 → 界面与显示 → 启用的键盘」勾选语言并选择默认键盘；键盘底部可切换，`+` 可直接配置。中文默认启用中文和英文，也能加入日语、韩语等。拼音、日文罗马字和韩文两套式布局均提供本地组词，其他语言提供常用词前缀联想；复杂中日文输入建议使用已安装的系统输入法。

PC 点击游戏内输入区后可使用系统输入法或 Win+H；没有独立输入小窗。系统语言包、语音权限及网络由 Windows 管理。屏幕键盘不联网、不记录输入历史；密码无联想。真实客户端的输入法、Win+H 与 VR 尚需测试，离线检查不能证明这些全部兼容。
