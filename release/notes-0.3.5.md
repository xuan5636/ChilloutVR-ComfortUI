# Comfort UI 0.3.5 · Player-colour nameplate trim / 玩家颜色名牌细边框

Remote-player nameplates now have a subtle rounded border matching each player's effective in-game nameplate colour. A purple player gets a purple trim, while the quiet theme background, readable name, portrait and speaking indicator remain familiar. Native profile/colour refreshes update the existing border.

No new colour service or downloaded assets are needed. The original billboard material and native visibility remain in use. Disabling the nameplate theme restores the native appearance, including any pre-existing outline settings.

玩家名牌增加跟随各自游戏内颜色的轻薄圆角边框，保留现有底色、姓名、头像与说话提示；颜色刷新时同步更新。在「设置 → 界面与显示」关闭名牌统一样式可恢复原版。

207 managed checks passed, including 46 nameplate/startup checks. Web behavior is unchanged: the prior 172 isolated UI checks from 0.3.4 are retained as historical evidence; only web version labels changed. Actual Unity/online PC/VR outline appearance remains unverified. See [validation](https://github.com/xuan5636/ChilloutVR-ComfortUI/blob/main/docs/VALIDATION.md).

Exit the game normally before updating. Use the EXE, or put only CVRComfortUI.dll in Mods. Updates retain preferences and local-model cache and back up the previous DLL. Windows / Mono MelonLoader; development target ChilloutVR2026r182-hf4 public-nightly, loader0.8.0-ci.2548, .NET Framework4.8.

Downloads include the standalone DLL, installer, manual package and full mod source. Game/Unity/Cohtml/MelonLoader binaries, private configuration and model weights are not included. Optional online translation sends selected text to its provider; local translation requires the optional small-model download. No new translation requests are made for this release.
