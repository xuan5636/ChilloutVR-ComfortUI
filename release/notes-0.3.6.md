# Comfort UI 0.3.6 · Selected player colours / 玩家自选颜色取色修复

Corrects0.3.5: the nameplate trim used rank/role colour. It now reads each player's primary colour selected in the game's My Profile, via PlayerColorsManager.GetPlayerColors(playerId).PrimaryColor, as clarified by the developer. Profile primary/secondary and rank colours are separate in the [referenced source](https://github.com/NotAKidoS/NAK_CVR_Mods/blob/main/CleanPlates/CleanPlates/PlateManager.Nameplates.cs#L101).

Remote colour notifications refresh the existing thin border on the main thread. A sparse cached lookup recovers joining-time unavailability or missed notifications, without additional network requests. The quiet background, portrait, name, speaking line, original billboard material and native visibility are retained. Turning off the theme restores the native appearance.

0.3.5误用了身份/等级色。本版改为读取玩家在「My Profile」里自选并由游戏同步的主色，颜色变更后自动刷新；对方无需安装本模组，不会改变对方设置。

215 managed checks passed, including54 nameplate/startup checks. A selected-purple/rank-red regression fails on frozen0.3.5. Only web version labels changed, so172 isolated UI checks from0.3.4 remain historical evidence. Actual Unity/online PC/VR appearance and colour synchronization still require in-game confirmation.

Exit the game normally before updating. Use ComfortUI-Setup-0.3.6.exe, or place only CVRComfortUI.dll in Mods. Preferences and local model cache are preserved and the previous DLL is backed up. Target: Windows / Mono MelonLoader0.8.0-ci.2548, ChilloutVR2026r182-hf4 public-nightly, .NET Framework4.8. Game binaries, private data and model weights are not included.
