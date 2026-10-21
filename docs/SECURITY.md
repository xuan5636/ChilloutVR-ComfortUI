# Translation bridge hardening · 0.5.2

## Change for CVRMG re-review

Earlier releases delivered a reconstructed translation result by concatenating serialized JSON into an `ExecuteScript` call. JSON encoding and `textContent` protected the tested cases, but this still mixed runtime text with JavaScript source. The previous audit did not demonstrate arbitrary code execution; 0.5.2 removes that delivery pattern instead of relying on quoting alone.

`ComfortMod` now executes only its trusted embedded bridge/initializer bundle. After the page registers its handlers, `ComfortUI_BridgeReady` acknowledges protocol 1; the host sends initialization on a later main-thread update. Translation results, initialization data, preferences, translation notices and model progress travel as **typed string arguments** to `cohtml.Net.View.TriggerEvent<string>`, using fixed event names declared in `CohtmlViewTarget`. They are not appended to script source, evaluated, or inserted into script tags.

The receiver uses `JSON.parse` and explicit type/size checks. Detail/profile translation also checks the request ID, target language, current pending request, number of fields, string field types and text lengths. Unexpected properties, nulls, invalid arrays, stale responses and language mismatches are ignored. Successful text is assigned with `textContent`; the original DOM is retained and restored by the existing toggle. Cancellation, page replacement, disabling translation and unload invalidate delivery. The native owner/wrapper/SWIG lifetime guard runs before sending events.

`UiDataJson` uses its own `JsonTextWriter` with `StringEscapeHandling.EscapeHtml`. It also isolates object serialization from process-wide `JsonConvert.DefaultSettings`, sets `TypeNameHandling.None`, and creates the response envelope itself. Raw HTTP response bodies and exception messages are never forwarded as JavaScript or diagnostic codes. Each translated field is limited to 48,000 UTF-16 code units; excessive output becomes the fixed `response` error. The event carries at most 1 MiB of JSON. Bootstrap is capped at 8 MiB; smaller data channels have separate limits.

F8 chat translation continues to replace the original Unity TMP label with rich text disabled; it does not cross the JavaScript bridge. The remaining microphone calls to `ExecuteScript` accept only a locally checked `muted`/`live`/`tts` enum or a bounded integer 0–100. The save-failure script is a fixed literal. None of these paths includes player text, provider responses, arbitrary setting strings or local dictionaries.

## Review entry points

- [CohtmlViewTarget](../src/CohtmlViewTarget.cs): fixed event names, argument limits and native lifetime guard.
- [ComfortMod](../src/ComfortMod.cs): trusted bundle injection, ready handshake, bootstrap and main-thread response delivery.
- [UiDataJson](../src/UiDataJson.cs): explicit encoding, reconstructed envelopes and output bounds.
- [native-bridge.js](../assets/native-bridge.js): fixed event registration and bootstrap/data parsing; no `eval`, `Function` or HTML insertion.
- [content-tools.js](../assets/content-tools.js): schema, pending-request/language matching and text-only rendering/restoration.
- [ChatTranslation](../src/ChatTranslation.cs), [HudMicrophoneSync](../src/HudMicrophoneSync.cs): separate TMP and constrained microphone paths.

## Reproducible checks and limits

`test.ps1` runs 11 managed projects, including 613 new serializer/schema checks over 301 boundary texts and 18 lifetime/event checks. A second run against the installed loader's .NET Framework-compatible Json.NET 13.0.4 library passes the same 613 checks. Texts cover quotes, slashes, control characters, Unicode separators, markup, script delimiters, multilingual text and deterministic generated combinations. Hostile process-wide serializer defaults are included.

The dependency-free Node check is:

```text
node tests/security-bridge052.cjs
```

For the full 637 event checks, first run `SecurityBridgeTests052.exe <absolute-output-fixtures.json>` after building the managed project, then supply that file as the Node script's second argument. The receiver runs with dynamic string code generation disabled. Both one- and two-field responses are retained as data.

Additional private, offline Edge checks exercise the actual bridge/content renderer across all five world/avatar/prop/profile entries: 1,505 rendering/restoration checks and 145 malformed-response/cancellation/language/disable/unload checks. No execution marker fires and no translated markup becomes child HTML. The native-HTML fixture also runs language, font, HUD and motion regressions through the new event bootstrap. Proprietary game HTML is not distributed.

These are standalone managed simulations and muted headless-browser checks with synthetic content, not an actual Cohtml/PC/VR session or a proof that all vulnerabilities are absent. No real player text, translation credentials or online translation services are used. CVRMG submission/acceptance and eventual staff approval are separate states; this document does not claim approval.
