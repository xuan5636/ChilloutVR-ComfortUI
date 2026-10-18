# Comfort UI 0.3.7 · Microphone sync after connection notices / 连接后麦克风同步修复

Fixes the microphone card remaining muted after joining an online room. The real client log identified an unsupported document.createTreeWalker call in notification translation, which aborted the microphone refresh. System-notice localization now uses basic DOM links and preserves native markup/original text records. Actual microphone state, glyph and level refresh first; a failed optional notice can no longer interrupt them or native event delivery.

修复进在线房间后已经开麦、底部仍显示「已静音」的问题。移除真实Cohtml报错的不兼容DOM调用；提示翻译单独容错，不再拖住状态、图标和音量刷新。保留实际语音状态、开关动画、输入高亮与原版提示音，不改变语音传输或个人设置。

215 managed and132 isolated HUD checks passed, including14 missing-API/fault-isolation checks on default/relative native HTML. Frozen0.3.6 fails the new regression.54 other menu checks from0.3.4 remain historical evidence. Real online PC/VR rendering and audio after update still require in-game confirmation.

Exit the game normally before updating. Run ComfortUI-Setup-0.3.7.exe or place only CVRComfortUI.dll in Mods. The installer backs up the previous DLL and preserves preferences/local model cache. Windows / Mono MelonLoader0.8.0-ci.2548, ChilloutVR2026r182-hf4 public-nightly, .NET Framework4.8. Game files, logs, personal data and model weights are not included.
