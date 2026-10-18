# Building and testing

Use Windows, a .NET SDK capable of building .NET Framework 4.8 projects, the .NET Framework compiler installed with Windows, and your own legal ChilloutVR installation with the compatible Mono MelonLoader runtime.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "D:\SteamLibrary\steamapps\common\ChilloutVR"
```

Or set `CHILLOUTVR_DIR` and build `src/ComfortUI.csproj` directly. The project references game and loader assemblies from `GameDir`; they are not redistributed. Outputs go to `artifacts/mod/` and `artifacts/ComfortUI-Setup-0.3.1.exe`. Building does not install to or modify the game directory.

The installer build computes the payload SHA256 and compiles a temporary copy of `Setup.cs` with the new constant. The checked-in source is not edited. This allows custom builds without breaking the installer's integrity check. Historical `app.manifest` compatibility identity is retained exactly as used in the released executable; the current assembly and installer payload versions are 0.3.1.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\test.ps1
```

The six managed test projects use mock Unity/Cohtml/Harmony/HTTP implementations and do not require game binaries or contact translation providers. Newtonsoft.Json is restored from NuGet. Existing test file names retain their historical version suffixes; they compile the current runtime source. UI tests cited in VALIDATION used the installed game's HTML and a private loopback harness; proprietary game fixtures are not included here.

The 0.3.1 release uses the already packaged DLL and EXE, matching the locally installed and verified payloads; publication does not rebuild or install the mod. Building and packaging do not install to or change the game directory. Different checkout paths/compiler environments may produce different binary hashes; source and all six embedded UI resources are checked against the package. Rebuilding is not claimed to be byte-for-byte reproducible across environments.

Optional live probes (explicit network/model download): build `tests/LocalModelTests031.csproj`, then run `LocalModelTests031.exe --live <dedicated-cache-directory> [http-proxy]`. `--bing` probes public Bing; default test execution stays offline. These are synthetic standalone probes, not real-game validation. Local-model files must never be included in source or mod artifacts.
