# Building and testing

Use Windows, a .NET SDK capable of building .NET Framework 4.8 projects, the .NET Framework compiler installed with Windows, and your own legal ChilloutVR installation with the compatible Mono MelonLoader runtime.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\ChilloutVR"
```

Or set `CHILLOUTVR_DIR` and build `src/ComfortUI.csproj` directly. The project references game and loader assemblies from `GameDir`; they are not redistributed. The current source version is 0.5.2; outputs go to `artifacts/mod/` and `artifacts/ComfortUI-Setup-0.5.2.exe`. Building does not install to or modify the game directory.

The installer build computes the payload SHA256 and compiles a temporary copy of `Setup.cs` with the new constant. The checked-in source is not edited. This allows custom builds without breaking the installer's integrity check. Historical `app.manifest` compatibility identity is retained exactly as used in the released executable; the installer AssemblyVersion and user-facing/payload version follow the source version (currently 0.5.2).

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

The eleven managed test projects use mock Unity/Cohtml/Harmony/HTTP implementations and do not require game binaries or contact translation providers. Newtonsoft.Json is restored from NuGet. Existing test file names retain their historical version suffixes; they compile the current runtime source. UI tests cited in VALIDATION used the installed game's HTML and a private loopback harness; proprietary game fixtures are not included here.

The 0.5.2 security preview is rebuilt from this source. All seven embedded resources, the DLL/EXE payload and version are checked before packaging. Building and packaging do not install to or change the game directory. The reference to Json.NET uses the compatible `MelonLoader/net35` copy, not a .NET 6 assembly from the game's Managed directory. Different build environments may produce different binary hashes; byte-for-byte reproducible builds are not claimed.

Run `node tests/security-bridge052.cjs` for the dependency-free event receiver checks. To run all 637 cases, generate fixtures with `SecurityBridgeTests052.exe <absolute-fixtures.json>` after building that test project and pass the file to the Node script. See [security notes](SECURITY.md) for the scope of these tests. Tests that restore Json.NET use NuGet 13.0.3; an optional `-p:JsonNetPath=<absolute-compatible-dll>` reference allows a separate run against the installed loader's 13.0.4 copy.

Optional live probes (explicit network/model download): build `tests/LocalModelTests031.csproj`, then run `LocalModelTests031.exe --live <dedicated-cache-directory> [http-proxy]`. `--bing` probes public Bing; default test execution stays offline. These are synthetic standalone probes, not real-game validation. Local-model files must never be included in source or mod artifacts.
