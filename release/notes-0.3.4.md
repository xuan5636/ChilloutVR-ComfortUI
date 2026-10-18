# Comfort UI 0.3.4 — Voice-input feedback and microphone cues

- Actual voice input highlights the microphone and card border, with a proportional level bar. Muting, TTS and stale/disconnected capture clear the indicator.
- Native processed levels are observed read-only and paced to20Hz, with sparse recovery snapshots and no second recording device. Fixed bar height and direct visibility follow HUD scale; motion Off retains instant feedback.
- Native mute/unmute cues use a persistent 2D output while retaining the selected game/custom clip, native UI mixer/volume and both cue settings. Handled cues do not play twice; missing resources fall back to native output. Other interface sounds are unchanged.

新增说话高亮和实际音量条，补全静音/断线复位；原生麦克风开关提示音改为独立二维输出，保留原声音、音量与设置。请正常退出游戏再安装，设置及模型缓存保留。

198 managed and172 isolated UI checks passed. Real-client Cohtml, audible cues, PC/VR and other-device installation remain unverified. The original missing-sound cause has not been reproduced in the live client; the new output path is the remedy implemented here. No new online translation/model probes were run.

Download the DLL for manual installation, the EXE for guided installation, or the manual ZIP for DLL plus instructions. Source and SHA256 checksums are included.

Unofficial mod, not affiliated with, approved, verified or supported by ChilloutVR or Alpha Blend Interactive. See the installation, validation and privacy documents before use.
