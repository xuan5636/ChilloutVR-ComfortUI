# Comfort UI 0.6.2 · Keyboard display hotfix

Fixes the keyboard rendering issues reported after the 0.6.1 startup repair: original and enhanced keyboards could appear together, some control labels used unsuitable fonts, and the actual 1520×420 game texture clipped the added controls.

- Hide only the captured original keyboard rows through explicit inline visibility management, with a simple CSS fallback. Original settings remain usable; switching back or unloading restores the saved original display values.
- Use the interface font for control labels and the selected script font for character keys. Remove the unsupported globe emoji.
- Center and narrow the panel, give character keys balanced width/height and centered text, and compact the input/candidate/language areas for the actual 420-pixel view. Original five-row key positions, proportions of function keys, settings and collapse controls are retained.
- Retain the 0.6.1 JSON startup compatibility fix, multilingual local suggestions, embedded Windows input path, settings and model caches.

**Validation:** 54 muted headless Edge/native-page fixture checks pass, including seven view sizes, actual 1520×420 composition/candidates/control bounds and key proportions, removal of the CSS hiding rule, original UI/disabled/unload restoration and script/interface fonts. 75 keyboard-engine checks and 637 typed-event checks pass again. Builds resolve all JSON references against both actual runtime libraries. These are isolated checks: actual Cohtml rendering, installed IMEs, Win+H, PC/VR and long sessions still need client testing. The 0.6.1 live game log now confirms initialization and view readiness; this does not verify the new 0.6.2 layout.

**Install:** exit ChilloutVR normally before using the EXE or replacing the single DLL in Mods. Existing preferences and model caches are retained.

**中文：0.6.2 键盘显示与比例热修复。**修复原版与增强键盘同时显示、部分标签变方框、实际420像素高度中底栏被裁切的问题。按原版五排键位收窄居中面板、调整输入区和候选栏，普通按键不再横向拉成长条，文字垂直居中。保留0.6.1启动修复、系统输入与多语言联想；请正常退出后更新。54项隔离界面检查通过，真实游戏显示/IME/Win+H/VR仍待测试。

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. Stable identifies the release channel. GitHub publication does not establish CVRMG staff approval.
