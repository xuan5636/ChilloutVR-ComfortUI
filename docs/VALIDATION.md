# 0.2.12 validation scope

2026-10-18. Public preview; real-client validation remains incomplete. 51 isolated UI checks passed: frame-sampled segments 18, language 8, native controls 7, state reuse 3, translation settings 7, hidden-panel/numeric-editor regression 8. Main and quick native HTML use a mock bridge on loopback only.

Verified intermediate transform positions, final alignment, three speeds, rapid reversal, motion Off including mid-flight, one-save callbacks, keyboard/drag/disabled controls, changing option lists, proxy drafts/focus and an idle frame scheduler. Final build and six embedded assets/installer payload checked. Real-client Cohtml/PC/VR comfort and long-session performance remain unverified. The current public test.ps1 also passed all 66 managed mock checks: lifetime 12, chat mode 26, HTTP logic 10, camera policy 18. Historical UI/performance/installer results below belong to 0.2.11.

# 0.2.11 validation scope

Prepared 2026-10-17. This is a public preview, not a claim of complete real-client compatibility.

## Completed before publication

- Original mod build: zero compiler warnings/errors.
- 93 isolated browser UI checks: language slider 8; native controls 7; motion 7; performance behavior 2; input/focus/scroll state 3; regression 18; details 4; languages 10; translation settings 7; chat mode 6; enhancements 13; numeric-input flash/style 8. Muted headless Edge, game HTML and a simulated bridge, loopback requests only.
- Managed mock tests: Cohtml lifetime 12, continuous chat 26, HTTP translation logic 10, camera policy/localization 18.
- 13 installer checks, including Unicode paths, payload integrity, rollback, user-preference preservation and refusing modified/unrecognized files. Offscreen form rendering was used.
- Release DLL and EXE SHA256 verified; installer payload equals the standalone DLL; C# source and six embedded UI assets match the published source.

## Performance observations

In the same isolated browser fixture, 20 identical preference echoes caused 20 rebuilds per settings region before optimization and zero after. Motion-setting changes reduced selector queries from 1429 to 33, and 100 native value changes from 500 to 101, excluding unchanged native polling. Median option processing was about 63.25 → 0.3ms and language changes about 185.7 → 74.1ms. These are fixture results, not in-game FPS or headset latency measurements. Original navigation timing and background image polling remain in place.

## Still unverified

Full version-0.2.11 real-client PC/VR click/ray input, Cohtml image loading, motion comfort, camera glyphs, sustained F8 translation, long-session performance and complete installation on another computer. Earlier real-client connection-test success does not establish that this version's F8 or VR paths are validated. Public translation service availability is not guaranteed.

Do not treat successful compilation or mock tests as a substitute for these checks.
