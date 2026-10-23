# Keyboard data attribution and changes

`assets/keyboard-data.js` contains adapted, separately licensed datasets. These data licenses do not relicense the rest of the mod. The local composition engine and manually authored common Japanese readings are separate implementation work.

## FrequencyWords — CC BY-SA 4.0

Source: [hermitdave/FrequencyWords](https://github.com/hermitdave/FrequencyWords/tree/525f9b560de45753a5ea01069454e72e9aa541c6), content/2018, by Hermit Dave and upstream contributors. Upstream credits [OpenSubtitles 2018 / OPUS](https://opus.nlpl.eu/OpenSubtitles-v2018.php) for the source corpus. The [full license text](licenses/FrequencyWords-CC-BY-SA-4.0.txt) accompanies the download. The upstream **content** license is [Creative Commons Attribution-ShareAlike 4.0 International](https://creativecommons.org/licenses/by-sa/4.0/); its repository's MIT code license is not the word-list license.

Adaptations: select at most 6,000 common words per language, discard counts, filter tokens by length/character categories, preserve frequency order and encode as JSON. 78,000 word entries cover zh_cn, zh_tw, en, ja, ko, es, fr, ru, pt_br, it, id, pl and tr. Japanese uses ja_full.txt; the others use their _50k.txt files. These adapted frequency lists remain **CC BY-SA 4.0**, including when embedded in a DLL. Attribution, source and license links must accompany redistributions. Editable form is the `words` object in the public JS asset; no encryption or additional restriction is applied. Upstream source-file hashes are recorded in `docs/keyboard-data-sources.json`.

## phrase-pinyin-data — MIT

Source: [mozillazg/phrase-pinyin-data](https://github.com/mozillazg/phrase-pinyin-data/tree/cee0ed6e6e4898580cafd2bd5e3723e20b214aa0), pinyin.txt, MIT. See the complete [copyright and permission notice](licenses/phrase-pinyin-MIT.txt).

Adaptations: strip tone marks, index phrases by joined pinyin, cap phrase and key length and retain up to eight candidates per key. Manually authored common phrases supplement/reorder candidates. There are 41,662 lookup keys. The frequency-based ordering/combined frequency-derived portions also retain the CC BY-SA license above.

## Traditional variants — OpenCC, Apache 2.0

The `traditional` map contains 33,223 changed phrase variants generated with opencc-python-reimplemented 0.1.7, using its s2t configuration and bundled [OpenCC](https://github.com/BYVoid/OpenCC) dictionaries. [Python project](https://github.com/yichen0831/opencc-python). Changes: offline conversion into a compact simplified-to-traditional phrase map; the runtime and original full dictionaries are not shipped or required. See [Apache License 2.0](licenses/OpenCC-Apache-2.0.txt) and [NOTICE](licenses/OpenCC-NOTICE.txt). Phrase selection also reflects the source/combined-data licenses above.

Only this small generated word asset is bundled. No personal text, game dictionaries, downloaded model, Python package or proprietary game resources are included.
