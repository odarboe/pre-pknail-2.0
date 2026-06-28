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

## Notes

- The `.las` files under `PointCloud/` are **AI-engine sample inputs**, not plugin runtime inputs.
- The plugin's only 2.0 runtime input is `docs/ai_engine_pknail_results.json` (the AI-engine output).
- Legacy test projects (`PKNailForRevit.UnitTests`, `TestCore`, `TestRunner`) are still .NET Framework 4.6
  and the AutoIt functional harness is pinned to Revit 2017 — see Jira plan Epic 9b for modernization.
