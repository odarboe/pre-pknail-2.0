# PKNAIL 2.0 — Build Setup

The repository does **not** commit vendor binaries. Before the first build, populate two local-only
reference folders under `code/PKNailProForRevit-2026/`. Both are git-ignored.

## 1. Revit 2026 API assemblies → `RevitAPI/`

Copy from your local Revit install (`C:\Program Files\Autodesk\Revit 2026\`):

```
RevitAPI/
  RevitAPI.dll
  RevitAPIUI.dll
  RevitAddInUtility.dll
```

`PKNailForRevit.csproj` references these with `<Private>False</Private>` (Revit supplies them at runtime).

## 2. SoftwareKey licensing SDK → `Tools/SoftwareKey/`

```
Tools/SoftwareKey/
  PLUSManaged.dll
```

Obtain `PLUSManaged.dll` from the PointKnown SoftwareKey SDK (it is **not** redistributable and is not on
NuGet). On existing dev machines it is also present in prior PKNail working copies.

## 3. Build

```
cd code/PKNailProForRevit-2026
dotnet build PKNailForRevit/PKNailForRevit.csproj -c Debug      # main plugin (0 errors expected)
dotnet test  PKNail.AiImport.Tests/PKNail.AiImport.Tests.csproj # fast Revit-free unit tests
```

The plugin currently emits ~1400 `CA1416` "WinForms is Windows-only" analyzer warnings; these are benign
(the add-in only ever runs on Windows under Revit). New 2.0 code in `PKNail.AiImport` builds clean with
`TreatWarningsAsErrors`.

## 4. Packaging & Release (Epic 8)

The installer lives in `Setup/` (WiX **v3**, `Product.wxs` + `Setup.wixproj`) and produces
`PKNailProForRevit2026.msi`. It was updated for 2.0: product version **2.0**, Revit **2026**, a new
`ProductUpgradeCode` (so it installs side-by-side with the 2017 product), and — importantly — it now
ships **`PKNail.AiImport.dll`** alongside the other plugin DLLs. Without that component the installed
plugin fails to load, because `PKNailForRevit` references the AI-import library.

**Toolchain (vendored, not committed).** `Setup.wixproj` uses `WixToolPath = ..\Tools\Wix\bin\` — the
WiX v3 `candle`/`light` binaries. Populate `Tools\Wix\` from the WiX v3 toolset before building the MSI
(same pattern as the `RevitAPI/` and `Tools\SoftwareKey\` vendor folders). The `.wixproj` is an
old-style MSBuild project; build it with **MSBuild** (Visual Studio or `msbuild Setup.wixproj`), not
`dotnet build`.

**Build the MSI:**

```
# 1. Build the plugin in the config you want to package (Debug or Release), so the DLLs exist in
#    PKNailForRevit\bin\<Config>\ — the .wxs sources every dependency (incl. PKNail.AiImport.dll) from there.
dotnet build PKNailForRevit/PKNailForRevit.csproj -c Release
# 2. Build the installer with MSBuild + the vendored WiX targets.
msbuild Setup/Setup.wixproj /p:Configuration=Release /p:Platform=x64
# → Setup/bin/Release/PKNailProForRevit2026.msi
```

**Signing** is a PointKnown release step (code-signing certificate, `signtool`) — it is **not**
automated in this repo and is not something a build environment without the cert can do.

**Verification (manual, on a clean Revit 2026 machine):** install the MSI, launch Revit, confirm the
**PKNail Pro** ribbon loads and **Import AI Model** runs the sample end-to-end (see the User Guide).
Regression before packaging: `dotnet test` the `PKNail.AiImport.Tests` and
`PKNail.AiImport.IntegrationTests` projects — all green (44 + 29).

> ⚠ The MSI build could not be validated in the current environment (no WiX toolset present). The
> manifest changes follow the existing component pattern; validate with a real WiX build before release.

## Notes

- The `.las` files under `PointCloud/` are **AI-engine sample inputs**, not plugin runtime inputs.
- The plugin's only 2.0 runtime input is `docs/ai_engine_pknail_results.json` (the AI-engine output).
- Legacy test projects (`PKNailForRevit.UnitTests`, `TestCore`, `TestRunner`) are still .NET Framework 4.6
  and the AutoIt functional harness is pinned to Revit 2017 — see Jira plan Epic 9b for modernization.
