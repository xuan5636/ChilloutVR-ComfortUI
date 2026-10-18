# 0.3.0 validation scope

2026-10-18. Public preview; installed locally with preferences and five protected files preserved and the prior DLL backed up. Publication retains the same verified DLL and EXE. 127 managed checks pass: lifetime 12, chat 39, HTTP/Windows key storage 24, camera 18, native-nameplate/first-run language 34. The last suite uses native stand-ins and also queries the real Windows UI-language API. It covers supported locale variants and English fallback, saved-language precedence, hook resolution, original font/atlas/billboard-material preservation, portrait/friend visibility, safe synthesized bold, contrast, restore/unload and no redundant Canvas colour writes during 120 stable state updates.

72 isolated UI checks pass: provider configuration 13, existing segmented motion 18, state reuse 3, HUD 22 (default and relative native HTML), new provider/source/nameplate controls 16 (main and quick menus). Tests use read-only native HTML, a mock bridge, muted headless Edge and loopback-only requests. The new controls receive real automated pointer clicks; all five services and six source choices save string values once, show intermediate sliding positions and honour motion Off. Five-language large-text rows remain bounded. Host preferences override stale browser storage. HUD checks cover transparent localized connection hints, bounded cards, unchanged native queue/markup, mic/TTS/levels, badge timeouts, literal scaled metrics, original mode and motion-Off cleanup.

Actual game-reference build: zero warnings/errors. All six embedded resources match source; installer version, payload and SHA256 constant match. Source/manual archives are checked byte-for-byte. This update makes no live translation calls. The earlier 0.2.14 network sample remains historical: MyMemory returned HTTP200, Google returned HTTP429. Public quota availability and real Microsoft/AI/Libre accounts are not certified.

Unverified: actual 0.3.0 Cohtml rendering/clicks, PC/VR and mirror nameplate facing, real-client font weight/long names/rank chips, HUD scaling/comfort and long-session performance, game-Mono credential storage, and clean installation/first startup on another computer. Installation was verified by matching the package and installed DLL hashes, unchanged preference bytes and five protected files, and an exact backup of the prior DLL; the game was not started automatically. Nameplate fallback: Settings → Interface & display → Player nameplates → Original. On first startup with no preferences file, supported Windows display languages select zh/en/ja/ko/es; unsupported or failed detection selects en. Existing preferences always take precedence; Chinese variants map to the supported Simplified Chinese pack.

# 0.2.14 validation scope

2026-10-18. Public preview; real-client verification remains incomplete. 93 managed checks pass: lifetime 12, chat 39, HTTP/Windows key storage 24, camera 18. 34 isolated native-HTML/mock-bridge UI checks pass: provider configuration 13, segmented motion 18, state reuse 3. Tests include bounded fallback/cooldown, transport reuse, cancellation/coalescing, provider request contracts, source/model/endpoint/key cache separation, real Windows DPAPI round-trip, key-to-endpoint binding, and no key in browser preferences/storage. Actual game-reference build and embedded resources/installer payload match.

Live short synthetic network probes from a .NET Framework process through the local proxy: MyMemory system/manual both HTTP200 with nonempty output. Google primary/fallback both HTTP429; the next attempt was stopped by cooldown. This does not certify Google availability or game/VR networking. No paid keys or real chat were used. Microsoft/AI/Libre were tested with mock HTTP only.

Unverified: real provider accounts, game-Mono credential storage, actual PC/VR rendering/mirrors/multi-atlas/long chats, long-session performance and complete installation on other devices. F8 facing-material/lifecycle protections are retained. HUD/nameplate changes are deferred to the next stage.

# 0.2.13 validation scope

2026-10-18. Public preview; live-client validation remains incomplete. The installed game's native chat output uses the BillboardFacing TMP shader. The old font setter replaced this with an ordinary SDF material. The hotfix preserves the native material's shader/properties, changing only the font atlas/SDF metrics on a per-bubble copy, and rebuilds the existing native text output.

The 0.2.12 implementation fails the new facing-material regression; the fix passes 35 chat checks. All four managed suites pass: lifetime 12, chat 35, HTTP 10, camera 18 (75 total). Tests use stand-ins, not an actual Unity renderer. Actual game-reference build: zero warnings/errors. Six embedded assets and installer payload/version/hash match. No new external service calls were made.

Live-client PC/VR, mirror views, multi-atlas glyphs, long translated messages and another computer's complete installation remain unverified. Translation backend expansion and HUD/nameplate improvements are deferred behind this user-requested hotfix.

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
