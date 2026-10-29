# Building and testing

Use Windows, a .NET SDK capable of building .NET Framework 4.8 projects, the .NET Framework compiler installed with Windows, and your own legal ChilloutVR installation with the compatible Mono MelonLoader runtime.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\ChilloutVR"
```

Or set `CHILLOUTVR_DIR` and build `src/ComfortUI.csproj` directly. The project references game and loader assemblies from `GameDir`; they are not redistributed. The current source version is 0.6.1; outputs go to `artifacts/mod/` and `artifacts/ComfortUI-Setup-0.6.1.exe`. Building does not install to or modify the game directory.

The installer build computes the payload SHA256 and compiles a temporary copy of `Setup.cs` with the new constant. The checked-in source is not edited. This allows custom builds without breaking the installer's integrity check. Historical `app.manifest` compatibility identity is retained exactly as used in the released executable; the installer AssemblyVersion and user-facing/payload version follow the source version (currently 0.6.1).

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

The thirteen managed/native test projects use mock Unity/Cohtml/Harmony/HTTP implementations and do not require game binaries or contact translation providers. Newtonsoft.Json is restored from NuGet. Existing test file names retain their historical version suffixes; they compile the current runtime source. UI tests cited in VALIDATION used the installed game's HTML and a private loopback harness; proprietary game fixtures are not included here.

The 0.6.1 stable channel is rebuilt from this source. All ten embedded resources, the DLL/EXE payload and version are checked before packaging. Building and packaging do not install to or change the game directory. Json.NET is compiled against the game's Managed .NET Standard 2.0 API. `build.ps1` then checks every Json.NET type/member reference against both the game and Mono loader assemblies using the installed loader's Mono.Cecil; the dependency binaries are never redistributed or replaced. Both assemblies may share identity 13.0.0.0 while exposing different overloads. This fixes the 0.6.0 startup MissingMethodException. Different build environments may produce different binary hashes; byte-for-byte reproducible builds are not claimed.

Run `node tests/security-bridge052.cjs` for the dependency-free event receiver checks. To run all 637 cases, generate fixtures with `SecurityBridgeTests052.exe <absolute-fixtures.json>` after building that test project and pass the file to the Node script. See [security notes](SECURITY.md) for the scope of these tests. Tests that restore Json.NET use NuGet 13.0.3; an optional `-p:JsonNetPath=<absolute-compatible-dll>` reference allows a separate run against the installed loader's 13.0.4 copy.

Optional live probes (explicit network/model download): build `tests/LocalModelTests031.csproj`, then run `LocalModelTests031.exe --live <dedicated-cache-directory> [http-proxy]`. `--bing` probes public Bing; default test execution stays offline. These are synthetic standalone probes, not real-game validation. Local-model files must never be included in source or mod artifacts.

Run `node tests/keyboard060.cjs` for the dependency-free local composition/dictionary checks. WindowsKeyboardTests060 creates only an owned hidden fixture window, checks real EDIT child text/selection/IME-state handling and cleanup, and never starts/focuses the game or uses speech. These synthetic message checks are not real IME conversion or dictation validation. The generated keyboard-data.js is the editable bundled data; see its source hashes, transformations and separate licenses in KEYBOARD-DATA.md. No runtime download is needed for suggestions.

Run `tools/check-json-compatibility.ps1 -GameDir <game-directory> -ModPath <mod-dll>` to reproduce the binary compatibility check. The 0.6.0 binary fails against the game JSON API at the one-argument JToken.ToString/WriteTo overloads; 0.6.1 uses the common writer API and passes both. Build `tests/JsonRuntimeTests061.csproj -p:JsonNetPath=<actual-json-dll> -o <separate-test-directory>` and run `JsonRuntimeTests061.exe <built-mod-dll>` for embedded startup resource round-trips. Run the existing SecurityBridgeTests052 separately against each actual JSON DLL too. For the Unity game copy, add `-p:TestFramework=net8.0` and a distinct absolute `-p:BaseIntermediateOutputPath=<test-obj-directory>/`, then run the resulting test DLL with `dotnet` (plus the mod argument for the startup test). The desktop .NET Framework host rejects this game's JSON strong-name signature; do not disable system signature checks or change the game library. The loader's net35 copy works with the default net48 test host. Use distinct output directories; these are standalone data checks, not in-game validation.


## Keyboard lifecycle regression (0.6.3)

test.ps1 includes KeyboardLifecycleTests063: the actual KeyboardInput source compiled with isolated game/Windows stand-ins. It checks session-scoped cancellation, native unlock calls, actual menu class names, VR/disabled behavior and cleanup when UI notification fails. It does not run a game session.
