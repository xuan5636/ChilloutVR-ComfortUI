# Comfort UI 0.6.3 — Keyboard fixes

Released 2026-10-30. The reporting user has tested the installed build and confirmed the reported keyboard problems are fixed. Upgrade from 0.6.0–0.6.2 to 0.6.3.

- Fixes the keyboard remaining as a thin strip after closing and preventing normal gameplay. Closing now completes the native close path, releases input/cursor locks, clears composition and removes the outer panel.
- Synchronizes native close, submit and hidden-view events. Opening the PC main/quick menu also cancels an active enhanced keyboard.
- Fixes PC keyboard centering and Chinese/Japanese/Korean control-label font selection; retains the original five-row arrangement and native arrow icons.
- Preserves preferences and local translation-model caches. Exit ChilloutVR normally before updating.

## 键盘问题已修复

用户已测试本机安装的版本并确认此前报告的键盘问题修复：关闭残留底部细条、无法恢复正常游戏、PC 居中及中日韩功能文字显示。本次发布提供与该测试版相同的 DLL 和 EXE，建议 0.6.0–0.6.2 用户升级至 0.6.3。

安装器部分文字和启动日志仍保留此前的“键盘修复测试版 / keyboard test”字样；程序版本为 0.6.3，发布时没有重新编译或更换已确认的二进制文件。

## Verification scope / 验证范围

1052 managed checks across 14 test programs, 75 keyboard-engine checks, 637 typed-event checks and 64 muted offline browser/native-HTML fixture checks passed during the repair. Production build and binary/resource checks passed. The reporter's confirmation concerns the reported keyboard problems on their PC; it is not certification of all hardware, Cohtml paths, physical IME/Win+H, VR or long-running use. See the validation and keyboard guides in the source/manual archive.

The older 0.6.0–0.6.2 files retain their known-issue warnings; this release supersedes the temporary recommendation to use 0.5.2 for those reported keyboard failures. This is an unofficial mod; publication does not establish CVRMG staff approval.
