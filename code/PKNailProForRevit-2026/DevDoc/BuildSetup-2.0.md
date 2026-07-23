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

## 5. Continuous integration & the test gate (Epic 11a)

The tests-before-commit gate runs as **two CI lanes** (both required before merge to `main`):

| Lane | Workflow | Runner | Covers |
| ---- | -------- | ------ | ------ |
| Fast (Revit-free) | `.github/workflows/ci-aiimport.yml` | GitHub-hosted `windows-latest` | `PKNail.AiImport` + its 44 unit tests |
| **Revit integration** | `.github/workflows/ci-revit-integration.yml` | **self-hosted** `windows` + `revit2026` | `PKNailForRevit` plugin build + the 32 in-process integration tests |

Lane 2 cannot use a hosted runner (RevitAPI/PLUSManaged are non-redistributable), so it needs a
self-hosted runner. Both lanes call the same logic locally via **`scripts/ci-gate.ps1`**.

### Run the full gate locally (required before committing any Revit-side change)

```
# From code/PKNailProForRevit-2026 (Revit 2026 CLOSED, vendored DLLs in place):
pwsh scripts/ci-gate.ps1            # unit -> plugin build -> integration; fails fast, writes .trx
```

### Provision the self-hosted Revit runner (one-time)

1. Install Revit 2026 and the .NET 8 SDK on a Windows machine.
2. Register it as a GitHub Actions **self-hosted runner** with labels `self-hosted, windows, revit2026`.
3. Put `RevitAPI.dll`, `RevitAPIUI.dll`, `RevitAddInUtility.dll`, and `PLUSManaged.dll` in a folder and
   set the machine env var **`PKNAIL_VENDOR_LIBS`** to it — `ci-gate.ps1` copies them into `RevitAPI/`
   and `Tools/SoftwareKey/` on each run (a fresh checkout has no git-ignored vendor binaries).
4. Ensure Revit 2026 is **closed** during CI runs (the plugin output DLLs must not be locked).

### Branch protection (repo setting)

In GitHub → Settings → Branches → protect `main`: require the status checks **"PKNAIL 2.0 AiImport
tests"** and **"PKNAIL 2.0 Revit integration tests"** to pass before merging. This makes the acceptance
criterion enforceable: no Revit-side change can merge without the integration suite green.

### Analyzer baseline & remaining warnings (Epic 11d)

`.editorconfig` (solution root) suppresses **CA1416** — ~2,800 "reachable on all platforms" warnings that
are not actionable (the add-in only ever runs on Windows under Revit) and drowned out real diagnostics.
The AI-import bridge files (`AiImport/**`, `ImportAiModelCommand.cs`) opt into full `#nullable enable`
with nullability warnings escalated to **errors**, so the bridge stays null-safe; the rest of the plugin
is `<Nullable>annotations</Nullable>` (annotations without a warning flood).

**Remaining warnings — triage (11d.3), fix opportunistically when touching the owning code:**

| Warning | Where | Action |
| ------- | ----- | ------ |
| SYSLIB0041 / SYSLIB0021 / SYSLIB0014 | `PKLicenseSrc` (obsolete `Rfc2898DeriveBytes` / `AesManaged` / `WebRequest`) | Modernize the crypto/web calls when next touching licensing. |
| CS8981 (`state` type name) | `Disto.cs` (legacy rangefinder) | Rename the lower-cased type if the Disto path is revisited. |
| NETSDK1137 | `PKNailForRevit.csproj` uses `Microsoft.NET.Sdk.WindowsDesktop` | Switch to `Microsoft.NET.Sdk` (trivial, low priority). |

## Notes

- The `.las` files under `PointCloud/` are **AI-engine sample inputs**, not plugin runtime inputs.
- The plugin's only 2.0 runtime input is `docs/ai_engine_pknail_results.json` (the AI-engine output).
- Legacy test projects (`PKNailForRevit.UnitTests`, `TestCore`, `TestRunner`) are still .NET Framework 4.6
  and the AutoIt functional harness is pinned to Revit 2017 — see Jira plan Epic 9b for modernization.
