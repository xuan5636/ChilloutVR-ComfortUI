# Language packs and regression checks

The 0.5.1 public preview supports `zh`, `en`, `ja`, `ko`, `es`, `zh-Hant`, `fr`, `ru`, `pt-BR`, `it`, `id`, `pl` and `tr`. The first five share a single-row sliding control; the other eight use an expandable row. Both write the same saved `language` field. The source-language selector additionally provides `auto`. German is intentionally not included.

## Sources and quality

`assets/locales.json` holds mod labels, native static/system templates, quick-menu keys, camera labels, detail prefixes and colour-channel names. `assets/settings-zh.json` and `camera-zh.json` retain their established Chinese roles. `comfort.js` uses the mod-owned quick-menu map before a native fallback and never registers Chinese under the native `en` slot. Native and third-party English registrations must remain intact.

Traditional Chinese is converted from the Simplified pack with regional corrections. The other seven new packs combine hand-authored core labels/camera/notification templates and machine-drafted low-frequency strings. They have not been reviewed by native speakers. Unsafe machine drafts retain the source text; a key existing in the JSON does not establish translation completeness or natural phrasing.

## Rules for future changes

1. Audit static labels, tooltips and runtime templates separately. Preserve player names, user-generated titles/descriptions, numeric values, URLs, sprite tags and substitution tokens such as `{value}` and `[playername]`. Do not machine-translate a whole HTML fragment or let it rewrite tokens.
2. Maintain the codes in `src/LanguageSupport.cs`, startup mapping, provider-specific codes, font faces and both selectors together. First launch may detect Windows display language; existing preferences win. Verify Simplified/Traditional Chinese and the Brazilian Portuguese mapping explicitly.
3. Replace native visuals only after their substitute is complete and can lay out. A missing script, stylesheet, node or glyph must leave the native microphone visible. Do not change the user's native HUD/reticle visibility settings to hide a display failure.
4. Run `test.ps1`. Verify actual picker clicks, keyboard navigation, save/reload, all text sizes, main/quick/login/keyboard/HUD, round trips to English, placeholder integrity and another component registering English both before and after Comfort UI. Browser fixtures use read-only game HTML and synthetic content; their results do not certify Cohtml/PC/VR rendering.
5. Build DLL and installer from the same source and compare all six embedded resources, payload SHA256 and version. Do not patch an old DLL while distributing a different installer payload. Preserve preferences/cache and keep an exact prior DLL backup when installing.

Before public release, have players test the real menus/HUD in PC and VR and ask native speakers to review the new languages. A native page fixture currently emits a baseline login `inputs.forEach` error in headless Edge; keep it distinct from injection errors and confirm login in the real client rather than treating the fixture as complete login validation.

## 0.5.1 interaction and rendering regressions

- Additional language lists must participate in the game's capture-phase ScrollHandler. Keep the nearest `list-content`/`scroll-content` with `nohorizontal`, a bounded viewport, and paging buttons. Test actual wheel and drag with the native handler loaded; default Chromium scrolling alone misses the reported failure. Keyboard navigation must explicitly reveal the active option.
- Set `lang` and `data-comfort-language` with `setAttribute`. Resolve the selected font into compiled CSS as well as the native font variable. Test a nonreflecting HTML `lang` property and check Korean font cmap coverage; a JSON translation existing does not mean its glyphs render.
- Probe the complete HUD replacement invisibly while retaining the native microphone. Allow only bounded layout retries; do not impose fixed pixel minimums that reject small render textures or saved HUD scales. Recheck when native HUD visibility or scale changes. Missing styling or parts must leave the original graphic intact.
- Camera controls use a native Unity Canvas. Keep persistent tooltip/click handlers; suppress only the themed background's sprite/colour callbacks and restore them when the layout is disabled. Close via the game's PortableCameraController.Disable, not GameObject deactivation. Keep PC controls within preview bounds and preserve VR hit areas.
