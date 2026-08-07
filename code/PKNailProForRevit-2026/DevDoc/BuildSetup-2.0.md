# PKNAIL 2.0 — Build Setup

**Exactly one file must be provisioned by hand before the plugin will build: `PLUSManaged.dll`.**
Everything else a build needs is in the repository.

## 1. Revit 2026 API assemblies → `RevitAPI/` — *already committed*

`RevitAPI.dll`, `RevitAPIUI.dll` and `RevitAddInUtility.dll` are committed (see the allow-list at the
bottom of `.gitignore`), so `git clone` + `dotnet build` works on a machine with **no Revit install** —
which is what lets reviewers and CI build the plugin. They are referenced with `<Private>False</Private>`
because Revit supplies them at runtime; they are reference assemblies only, never redistributed.

If you ever need to refresh them, copy from `C:\Program Files\Autodesk\Revit 2026\`.

## 2. SoftwareKey licensing SDK → `Tools/SoftwareKey/` — **you must provide this**

```
Tools/SoftwareKey/
  PLUSManaged.dll
```

`PLUSManaged.dll` is **not redistributable** and is not on NuGet, so it stays git-ignored. Obtain it from
the PointKnown SoftwareKey SDK; on existing dev machines it is also present in prior PKNail working
copies. Without it the plugin fails to compile with `CS0246: 'com' could not be found` (the `PKLicenseSrc`
files reference the SoftwareKey namespaces).

The Revit-free `PKNail.AiImport` library and its unit tests do **not** need this file and build straight
from a clean clone.

## 3. Build

```
cd code/PKNailProForRevit-2026
dotnet build PKNailForRevit/PKNailForRevit.csproj -c Debug      # main plugin (0 errors expected)
dotnet test  PKNail.AiImport.Tests/PKNail.AiImport.Tests.csproj # fast Revit-free unit tests
```

### Build configurations

| Config | Optimized | Licensing | Use it for |
| --- | --- | --- | --- |
| `Debug` | no | **bypassed** | day-to-day development (also skips splash + update check) |
| `Evaluation` | yes | **bypassed** | **third-party review / client acceptance testing** |
| `Release` | yes | enforced + metered | shipping to customers |
| `Deploy`, `Deploy64` | yes | enforced + metered | legacy release lanes |

**`Evaluation` exists because a `Release` build cannot be exercised without a license.** The ribbon's
`CommandsAvailability` gate requires `IsLicenseValid`, so on an unlicensed machine every PKNail button —
including **Import AI Model** — is greyed out and the plugin cannot be tested at all. `Evaluation` is
byte-for-byte Release-quality (`Optimize=true`, `pdbonly`) but defines `BYPASS_LICENSE_CHECK` and
`BYPASS_UPDATE_CHECK`, so a reviewer can install it and run an import immediately. The splash screen is
deliberately left on so it still behaves like the shipping product.

```
dotnet build PKNailForRevit/PKNailForRevit.csproj -c Evaluation
# -> PKNailForRevit/bin/Evaluation/net8.0-windows/
```

`Evaluation` is intentionally **not** registered in `PKNailForRevit.sln`; build it via the `.csproj`
command above. (Half-wiring a new configuration across all 15 solution projects would let Visual Studio
silently skip projects, which is a worse failure than the config simply not appearing in the dropdown.)

> ⚠️ **Never ship an `Evaluation` build to a customer** — it performs no licensing and no usage
> accounting. `Release`, `Deploy` and `Deploy64` are untouched and remain fully licensed and metered.

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

### Opening-path strategy (Epic 11e.2)

The v2 importer has **two** opening-placement paths, by design:

1. **Primary — the 1.0 engine flow** (`ActivateWallById` → `SurveyWallHostedObjectPrep` →
   `SurveyWallHostedObject`). Applies the AI's family-def resolution + door hinge/swing + window facing.
   Requires a **PKNAIL survey family** to be loaded in the document (the shipped `PKNail *.rfa` families,
   present in the PKNail template).
2. **Fallback — `CreateOpeningOnWall`** (direct hosting). Used only when no PKNAIL survey family is
   available (e.g. a stock metric template). The opening still appears, but the AI orientation
   (hinge/facing) is **not** applied; the import report warns per opening.

**Decision:** keep the fallback — it guarantees openings are never silently lost on a non-PKNAIL template,
and it degrades honestly (a report warning, not a wrong-but-silent result). The remaining fidelity gap
(orientation on stock templates) is closed by the **optional** family auto-load (Epic 11e.3): load a
default PKNAIL window/door family when none is present, so the engine path runs everywhere. Until that
lands, use the PKNAIL template for full-fidelity openings.

## Notes

- The `.las` files under `PointCloud/` are **AI-engine sample inputs**, not plugin runtime inputs.
- The plugin's only 2.0 runtime input is `docs/ai_engine_pknail_results.json` (the AI-engine output).
- Legacy test projects (`PKNailForRevit.UnitTests`, `TestCore`, `TestRunner`) are still .NET Framework 4.6
  and the AutoIt functional harness is pinned to Revit 2017 — see Jira plan Epic 9b for modernization.
