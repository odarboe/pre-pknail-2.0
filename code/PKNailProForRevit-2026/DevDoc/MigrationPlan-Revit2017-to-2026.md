# Migration Plan: PKNail Pro For Revit 2017 → 2026 + .NET 4.6 → .NET 8

**Status**: IDENTIFICATION PHASE (nothing changed yet)  
**Date**: 2026-04-11  
**Scope**: Revit API upgrade 2017→2026, .NET Framework 4.6 → .NET 8

---

## Legend

| Symbol | Meaning |
|--------|---------|
| ⛔ BREAKING | API removed; compile error without fix |
| ⚠️ DEPRECATED | API still compiles but produces warning or will be removed soon |
| 🔑 REQUIRED | Must change even if it compiles (e.g., version numbers) |
| ℹ️ INFO | Relevant context, not a code change |

---

## SECTION 1 — Revit API Breaking Changes

### 1.1 ⛔ `ParameterType` enum — REMOVED in Revit 2022

**Impact: HIGH — affects entire custom-parameter subsystem**

`ParameterType` (e.g., `ParameterType.Text`, `ParameterType.Length`, `ParameterType.Integer`, `ParameterType.YesNo`, `ParameterType.Number`) was deprecated in Revit 2021 and **removed in Revit 2022**. Replacement is `ForgeTypeId` with `SpecTypeId.*` constants.

**Files affected:**

| File | Lines | Old API | New API |
|------|-------|---------|---------|
| `PKNailParameter.cs` | 18 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 19 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 20 | `ParameterType.Integer` | `SpecTypeId.Int.Integer` |
| `PKNailParameter.cs` | 21 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 22 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 23 | `ParameterType.YesNo` | `SpecTypeId.Boolean.YesNo` |
| `PKNailParameter.cs` | 24 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 25 | `ParameterType.YesNo` | `SpecTypeId.Boolean.YesNo` |
| `PKNailParameter.cs` | 26 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 30, 37 | `ParameterType.Number \|\| ParameterType.Length` | `SpecTypeId.Number`, `SpecTypeId.Measurable.Length` |
| `PKNailParameter.cs` | 45, 51 | `ParameterType.Text` | `SpecTypeId.String.Text` |
| `PKNailParameter.cs` | 64, 71 | `ParameterType.Integer \|\| ParameterType.YesNo` | `SpecTypeId.Int.Integer`, `SpecTypeId.Boolean.YesNo` |
| `PKNailParameter.cs` | 79, 90 | `ParameterType.Number \|\| ParameterType.Length` | `SpecTypeId.Number`, `SpecTypeId.Measurable.Length` |

**Constructor signature change:**  
`new PKNailParameter(name, ParameterType.xxx)` → The private `m_type` field (type `ParameterType`) must become `ForgeTypeId`. All type-checking `if (m_type == ParameterType.X)` guards must also be rewritten.

**Also affects shared-parameter file loading** — when creating shared parameters via `Definitions.Create(ExternalDefinitionCreationOptions)`, `ParameterType` was the type arg; now use `ForgeTypeId`.

---

### 1.2 ⛔ `ElementId.IntegerValue` → `ElementId.Value` (long) — BREAKING in Revit 2024

**Impact: HIGH — 30+ call sites, all `Dictionary<int, ...>` keyed by element ID must change type**

In Revit 2024, `ElementId` now wraps a `long` internally. `IntegerValue` (returns `int`) **is removed**. Use `ElementId.Value` (returns `long`).

All `Dictionary<int, T>` keyed with `.IntegerValue` must become `Dictionary<long, T>`.

**Files affected:**

| File | Line(s) | Pattern |
|------|---------|---------|
| `RevitDocument.cs` | 240 | `inst.Id.IntegerValue` → `.Value` |
| `RevitDocument.cs` | 465 | `.IntegerValue` (HashSet element) |
| `RevitDocument.cs` | 1219, 1224 | `selEl.Category.Id.IntegerValue == (int)BuiltInCategory.OST_*` → use `element.Category.EqualsBuiltInCategory(BuiltInCategory.X)` (Revit 2023+) |
| `RevitDocument.cs` | 1324 | `Dictionary<int,Wall>` key → `Dictionary<long,Wall>` |
| `RevitDocument.cs` | 1343, 1344 | Same dictionary lookup |
| `RevitDocument.cs` | 1441 | `List<int>` of IDs → `List<long>` |
| `RevitDocument.cs` | 1797, 1798, 1800 | `Dictionary<int,PreservedGeom>` key |
| `RevitDocument.cs` | 2332 | `List<int>` contains check |
| `RevitDocument.cs` | 2695, 2696 | `.Id.IntegerValue` |
| `RevitDocument.cs` | 2765 | `.Id.IntegerValue.ToString()` |
| `RevitDocument.cs` | 2776 | `nextJoinedElID.IntegerValue` |
| `RevitDocument.cs` | 3161, 3164 | `Dictionary<int,int>` restoredInstanceMap |
| `RevitDocument.cs` | 3859 | `.Id.IntegerValue` |
| `RevitDocument.cs` | 4047 | `Dictionary<int,MoveDef>` key |
| `RevitDocument.cs` | 4832, 4860, 4892, 4902, 4907, 4948, 4956, 4998, 5001, 5016, 5080, 5191, 5193 | Various `.Id.IntegerValue` |
| `RevitDocument.cs` | 5350 | `sym.Category.Id.IntegerValue` |
| `RevitDocument.cs` | 5506, 5509 | restoredInstanceMap uses |
| `RevitDocument.cs` | 5517 | `new ElementId(value.IntegerValue)` → `new ElementId(value.Value)` |
| `RevitDocument.cs` | 5519 | Same |
| `RevitDocument.cs` | 5524 | Same |
| `RevitDocument.cs` | 5555 | `.AsElementId()` (still valid) |
| `RevitDocument.cs` | 5564, 5567 | Category BuiltInCategory compare |
| `RevitDocument.cs` | 5607, 5621 | `.Id.IntegerValue` → `.Id.Value` |
| `RevitDocument.cs` | 6006, 6303, 6725, 6744 | Various |
| `WallState.cs` | 149 | `inst.Id.IntegerValue` → `.Value` |
| `RevitModelState.cs` | 330, 334 | `.AsElementId()` result (still valid) |
| `RevitSurveyWall.cs` | (various) | Any `.IntegerValue` uses |

**Special case — `RestoredInstanceMap`:**  
`Dictionary<int, int>` (maps original element ID → restored instance ID) must become `Dictionary<long, long>`.

**Special case — `OriginalElementId` field:**  
`RevitDocument.cs` inner class has `m_origElementId` stored as `int` / `m_familySymbolID` as `int`; these must become `long`.

---

### 1.3 ⛔ `Application.Create.NewGeometryOptions()` — REMOVED circa Revit 2019

**Impact: LOW — 1 call site**

| File | Line | Old | New |
|------|------|-----|-----|
| `RevitSurveyWall.cs` | 236 | `revitApp.Create.NewGeometryOptions()` | `new Options()` |

The `Autodesk.Revit.ApplicationServices.Application.Create` factory class was progressively deprecated. `NewGeometryOptions()` no longer exists; just instantiate `Options` directly.

---

### 1.4 ⛔ `Document.Create.NewDetailCurve()` — REMOVED in Revit 2022

**Impact: MEDIUM — 2 call sites**

| File | Line | Old | New |
|------|------|-----|-----|
| `RevitDocument.cs` | 845 | `doc.Create.NewDetailCurve(view, line)` | `DetailLine.Create(view, line)` |
| `RevitDocument.cs` | 973 | `doc.Create.NewDetailCurve(doc.ActiveView, ...)` | `DetailLine.Create(view, line)` |

Return type is still `DetailCurve` / `DetailLine` (same class hierarchy).

---

### 1.5 ⚠️ `Category.Id.IntegerValue == (int)BuiltInCategory.OST_*` — Awkward pattern, broken in Revit 2024

**Impact: MEDIUM — 4 call sites**

With `IntegerValue` removed, this comparion pattern breaks. Revit 2023 added `Category.EqualsBuiltInCategory(BuiltInCategory bic)`.

| File | Lines | Old | New |
|------|-------|-----|-----|
| `RevitDocument.cs` | 1219, 1224 | `selEl.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Doors` | `selEl.Category.EqualsBuiltInCategory(BuiltInCategory.OST_Doors)` |
| `RevitDocument.cs` | 5350 | `sym.Category.Id.IntegerValue` | `sym.Category.EqualsBuiltInCategory(...)` |
| `RevitDocument.cs` | 5564, 5567 | Category BuiltInCategory compare | Same fix |

---

### 1.6 ⚠️ `ParameterValueProvider` constructor with `ElementId(BuiltInParameter)`

| File | Line | Note |
|------|------|------|
| `RevitSurveyWall.cs` | 211 | `new ParameterValueProvider(new ElementId(BuiltInParameter.HOST_ID_PARAM))` — still valid in 2026 but verify constructor signature hasn't changed |

---

### 1.7 ℹ️ `ReferenceIntersector` — verify constructor signature

`ReferenceIntersector` was updated across versions. The 3-arg constructor `(ElementFilter, FindReferenceTarget, View3D)` is still valid in 2026.

| File | Lines |
|------|-------|
| `RevitDocument.cs` | 2326, 2882, 5061 |

**Action**: Compile-test; likely no code change needed.

---

### 1.8 ⚠️ `Document.Create.NewFamilyInstance(Curve, FamilySymbol, Level, StructuralType)` overload

| File | Line | Note |
|------|------|------|
| `WallState.cs` | 135 | This overload was deprecated in some versions; verify it still exists in Revit 2026. Alternate: use the face-based or point-based overloads. |

---

### 1.9 ℹ️ `Utilities.ToRevitLine(Application, G2D.Vector)` — takes `Application` but no longer uses it

The `Application` parameter was previously needed for `app.Create.NewLineBound()`, but in the current code `ToRevitLine` already uses `Line.CreateBound()` (static, API-agnostic). The `Application` parameter is now a dead argument.

**Action**: Remove the `Application` parameter from `ToRevitLine()` and update all 10 call sites.

| File | Lines |
|------|-------|
| `Utilities.cs` | 37 (definition) |
| `RevitDocument.cs` | 565, 630, 973, 1605, 2384, 2554, 2569, 2898, 4410 |
| `WallState.cs` | 131 |
| `RevitSurveyWall.cs` | (if any) |

---

## SECTION 2 — .NET Version Changes (4.6 → .NET 8)

### 2.1 ⛔ `WebClient` — Obsolete in .NET 5+, unavailable in .NET 8 without compatibility shim

**Impact: MEDIUM — `UpdateCheck.cs` only**

| File | Lines | Issue |
|------|-------|-------|
| `UpdateCheck.cs` | 48, 120–132 | `webClientWithTimeout : WebClient`, `wc.DownloadString(url)` |

**Fix**: Replace with `HttpClient` (or `HttpClientFactory`):

```csharp
// Old:
using (var wc = new webClientWithTimeout()) { json = wc.DownloadString(url); }

// New:
using var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(cm_updateCheckTimeout) };
string json = await httpClient.GetStringAsync(url);
// OR synchronous via .GetAwaiter().GetResult() if keeping sync (not recommended)
```

**Also see**: `System.Net.WebRequest` (also obsolete) is used indirectly via `WebClient` overrides.

---

### 2.2 ⛔ `System.Web.Services` — Not available in .NET 5+

**Impact: MEDIUM**

`PKNailForRevit.csproj` explicitly references `System.Web.Services` at the framework level. This assembly does not exist in .NET 8.

| File | Ref |
|------|-----|
| `PKNailForRevit.csproj` | `<Reference Include="System.Web.Services" />` |

**Fix**: Determine what in `System.Web.Services` is actually used (likely `System.Web.Services.Protocols.SoapClient` from the `PLUSManaged` SoftwareKey SDK). The dependency may be transitive via `PLUSManaged.dll`. Requires checking SoftwareKey vendor for a .NET 8-compatible SDK.

---

### 2.3 ⛔ Project files — old-format `.csproj` → SDK-style required for .NET 8

**Impact: HIGH — all 10 projects**

All projects use the old VS 2010–2019 MSBuild format with `<TargetFrameworkVersion>`. Must be converted to SDK-style format for .NET 8.

| Project | Current TFV | Target TFV |
|---------|-------------|------------|
| `PKNailForRevit.csproj` | v4.6 | `net8.0-windows` |
| `Json.csproj` | v4.6 | `net8.0` |
| `Json.UnitTests.csproj` | v4.6 | `net8.0` |
| `PKLicense.csproj` | v4.6 | `net8.0-windows` |
| `PKLicense.UnitTests.csproj` | v4.6 | `net8.0` |
| `PKNailForRevit.TestCore.csproj` | v4.6 | `net8.0-windows` |
| `PKNailForRevit.TestRunner.csproj` | v4.6 | `net8.0-windows` |
| `PKNailForRevit.UnitTests.csproj` | v4.6 | `net8.0-windows` |
| `DistoCommTester.csproj` | v4.6 | `net8.0-windows` |
| `SetupCA.csproj` | v4.6 | `net8.0-windows` |
| `TestScriptFixer.csproj` | v3.5 | `net8.0` |
| `RevitLookup.csproj` | v4.0 | `net8.0-windows` |

**Key SDK-style additions needed for WinForms projects:**
```xml
<UseWindowsForms>true</UseWindowsForms>
```
**Key SDK-style additions needed for WPF/WPF-mixed projects:**
```xml
<UseWPF>true</UseWPF>
```
The main plugin (`PKNailForRevit.csproj`) uses both WinForms (dialogs) and WPF/PresentationCore (ribbon), so needs both.

---

### 2.4 ⚠️ WinForms (System.Windows.Forms) — Available in .NET 8 but needs opt-in

All dialog `.cs` + `.Designer.cs` files use WinForms. The 28 WinForms-using files currently import `System.Data` (likely just template-generated stub; check if actually used). WinForms itself will work in .NET 8 with `<UseWindowsForms>true</UseWindowsForms>`.

**Files using `System.Data` (check if real usage or generated stubs):**
`AddLvlDlg.cs`, `CopyObjectsDlg.cs`, `ConvertUnknownWidthWallsDlg.cs`, `FirstWallDlg.cs`, `ActivateLicenseOnlineDlg.cs`, `LevelsDlg.cs`, `PickOneDlg.cs`, `NumLvlDlg.cs`, `NewProjectSettingsDlg.cs`, `MeasureCornerAngleDlg.cs`, `SelectLicenseToActivateDlg.cs`, `SettingsAboutCtl.cs`, `SettingsAppCtl.cs`, `SettingsDlg.cs`, `SettingsLicenseCtl.cs`, `SettingsProjectGeneralCtl.cs`, `SettingsProjectRoundingCtl.cs`, `SettingsRFKeypadMapCtl.cs`, `SettingsUpdatesCtl.cs`, `SplashForm.cs`, `ShortDistanceControl.cs`, `SurveyDoorDlg.cs`, `SurveyLineDlg.cs`, `SurveyHostedObjectDlg.cs`, `SurveyWallDlg.cs`, `TransitOpeningDlg.cs`, `TypicalWindowHeightDlg.cs`, `SurveyWindowDlg.cs`, `UsageBalanceTransferDlg.cs`

---

### 2.5 ⚠️ `app.config` → `appsettings.json` or runtime config

The `DistoCommTester` project has an `app.config`. In .NET 8 this can remain as `app.config` for Windows desktop apps, but the packaging may differ.

---

### 2.6 ⚠️ T4 Text Templates (`.tt` files) — SDK-style project compatibility

**Impact: LOW-MEDIUM**

T4 template processing (`AssemblyInfo.tt`, `ProductInfo.tt`, `AssemblyVersionInfo.cs`) may not work out-of-the-box in SDK-style projects without additional tooling (`Microsoft.VisualStudio.TextTemplating.VSHost` NuGet). Verify T4 still runs and generates properly.

| File | Purpose |
|------|---------|
| `PKNailForRevit/AssemblyInfo.tt` | Generates `AssemblyInfo.cs` — product name, version |
| `PKNailForRevit/ProductInfo.tt` | Generates `ProductInfo.cs` — URLs, version strings |

---

## SECTION 3 — Third-Party Library Updates

### 3.1 🔑 `Newtonsoft.Json 6.0.0` → Update to 13.x

**Impact: LOW — straightforward NuGet update**

| File | Current |
|------|---------|
| `PKNailForRevit.csproj` | `Newtonsoft.Json 6.0.0` via `Tools\Json.NET\Net45\Newtonsoft.Json.dll` |

**Fix**: Switch to `<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />` in SDK-style project. The custom Json project in the solution (`Json.csproj`) is a local copy of a JSON library — determine if it can be retired in favor of Newtonsoft.Json 13 or `System.Text.Json`.

---

### 3.2 ⛔ `PLUSManaged.dll` (SoftwareKey SDK) — No .NET 8 guarantee

**Impact: HIGH — core to the license system**

| File | Note |
|------|------|
| `PKNailForRevit.csproj` | `PLUSManaged, Version=5.11.3.0` from `Tools\SoftwareKey\PLUSManaged.dll` |

SoftwareKey `PLUSManaged` v5.11.3.0 targets .NET Framework. It may not run under .NET 8.

**Action Required**: Contact SoftwareKey / check their website for a .NET 8-compatible version. If none, consider migrating to an alternative licensing SDK (Cryptolens, LimeLM, etc.) or build a simple custom license check.

---

### 3.3 🔑 RevitAPI DLL references — Must swap to Revit 2026 SDK

| File | Current HintPath |
|------|-----------------|
| `PKNailForRevit.csproj` | `..\RevitAPI\RevitAPI.dll` |
| `PKNailForRevit.csproj` | `..\RevitAPI\RevitAPIUI.dll` |

**Action**: Replace the DLLs in the `RevitAPI/` folder with those from the Revit 2026 SDK (typically at `C:\Program Files\Autodesk\Revit 2026\`), OR switch to the NuGet package `Autodesk.Revit.RevitAPI` (version 2026.*) if Autodesk publishes it.

---

### 3.4 ℹ️ NUnit

Unit test projects use NUnit. Verify the NUnit version is compatible with .NET 8. NUnit 3.x fully supports .NET 8. Update to latest NUnit 3 or NUnit 4 via NuGet.

---

## SECTION 4 — Version Number / String Updates

### 4.1 🔑 `ProductInfo.cs` / `ProductInfo.tt`

| File | Line | Change |
|------|------|--------|
| `ProductInfo.cs` | 13 | `ProductRevitVersion = "2017"` → `"2026"` |
| `ProductInfo.tt` | — | The T4 source drives this from `VersionInfo.xml` |

Also: `http://` URLs in `ProductInfo.cs` (line 9) must become `https://`.

| File | Old | New |
|------|-----|-----|
| `ProductInfo.cs` | `http://www.pointknown.com` | `https://www.pointknown.com` |

---

### 4.2 🔑 `AssemblyInfo.cs` / `AssemblyInfo.tt`

| File | Line | Change |
|------|------|--------|
| `AssemblyInfo.cs` | 18 | `AssemblyProduct("PKNail Pro For Revit 2017")` → `"PKNail Pro For Revit 2026"` |

---

### 4.3 🔑 `VersionInfo/` — Source XML driving T4 templates

Locate the `VersionInfo.xml` (or similar) that feeds `AssemblyInfo.tt` and `ProductInfo.tt` and update `<ProductRevitVersion>` from `2017` to `2026`.

---

### 4.4 🔑 `Setup/Product.wxs` — WiX installer

| Line | Change |
|------|--------|
| 8 | `ProductRevitVersion = "2017"` → `"2026"` |
| 12 | `ProductUpgradeCode` **MUST be a new GUID** (different Revit version) |

Generate a new GUID via `New-Guid` in PowerShell or Visual Studio's GUID generator.

---

### 4.5 🔑 `.addin` file Revit version

The `.addin` template at `RevitFiles/PKNailProForRevit.addin.template` does not contain the version number (it's auto-placed by the installer), but the **install folder path** is `PKNailProForRevit2017` — the installer must place the `.addin` file in Revit 2026's addins location:

- Old: `%APPDATA%\Autodesk\Revit\Addins\2017\`
- New: `%APPDATA%\Autodesk\Revit\Addins\2026\`

This is controlled by the WiX installer (`Product.wxs`). Search for `2017` in all `.wxs` files.

---

### 4.6 🔑 `ccnet.config` — TeamCity / CruiseControl build config

| File | Action |
|------|--------|
| `ccnet.config` | Review for any hardcoded Revit 2017 paths or build output paths |

---

## SECTION 5 — Security / HTTP Changes

### 5.1 ⚠️ HTTP → HTTPS for all external URLs

All hardcoded `http://` URLs must become `https://`. TLS 1.2+ required.

| File | Line | URL |
|------|------|-----|
| `ProductInfo.cs` | 9 | `http://www.pointknown.com` → `https://www.pointknown.com` |
| `PKLicenseSrc/PKLicense.cs` | (PayGo calls) | Any `http://paygo.pointknown.com` → `https://` |

### 5.2 ⚠️ NTP UDP Socket (PKLicense.cs)

The license system makes raw UDP calls to NTP servers for tamper-resistant time validation. In .NET 8 this still works but the UDP socket code should be reviewed for any `SocketException` handling changes.

---

## SECTION 6 — RevitLookup Debug Tool

`RevitLookup/` is an open-source Autodesk debug/browsing tool bundled with the solution. It uses:
- Very old Revit API (`Autodesk.Revit.Collections` — removed long ago)
- `TargetFrameworkVersion = v4.0`

**Recommendation**: Replace the entire `RevitLookup/` folder with the current official [RevitLookup](https://github.com/jeremytammik/RevitLookup) release for Revit 2026. Do not migrate the old code.

---

## SECTION 7 — Summary Checklist (Ordered by Risk)

### Phase 1 — New RevitAPI DLLs (prerequisite for everything else)
- [ ] **7.1** Replace `RevitAPI/RevitAPI.dll` and `RevitAPI/RevitAPIUI.dll` with Revit 2026 SDK copies
- [ ] **7.2** Attempt a compile; enumerate all new errors

### Phase 2 — .NET 8 Project Migration
- [ ] **7.3** Convert all `.csproj` files to SDK-style format
- [ ] **7.4** Set `<TargetFramework>net8.0-windows</TargetFramework>` on all plugin projects
- [ ] **7.5** Add `<UseWindowsForms>true</UseWindowsForms>` to dialog-bearing projects
- [ ] **7.6** Remove `<Reference Include="System.Web.Services" />` from `PKNailForRevit.csproj`
- [ ] **7.7** Switch `Newtonsoft.Json` to NuGet PackageReference v13.x
- [ ] **7.8** Verify T4 templates still execute (may need `dotnet-t4` or VS extension)

### Phase 3 — ParameterType → ForgeTypeId (most isolated change)
- [ ] **7.9** Rewrite `PKNailParameter.cs` field from `ParameterType m_type` → `ForgeTypeId m_type`
- [ ] **7.10** Replace all `ParameterType.Text/Length/Integer/YesNo/Number` constants with `SpecTypeId.*` equivalents
- [ ] **7.11** Update all `if (m_type == ParameterType.X)` comparisons to `m_type == SpecTypeId.X`

### Phase 4 — ElementId.IntegerValue → ElementId.Value
- [ ] **7.12** Find all `Dictionary<int, T>` keyed by element ID; change to `Dictionary<long, T>`
- [ ] **7.13** Find all `List<int>` storing element IDs; change to `List<long>`
- [ ] **7.14** Change `m_origElementId` / `m_familySymbolID` fields from `int` to `long` in `RevitDocument.cs`
- [ ] **7.15** Replace all `.IntegerValue` with `.Value` (30+ sites across `RevitDocument.cs`, `WallState.cs`, `RevitModelState.cs`, `RevitSurveyWall.cs`)
- [ ] **7.16** Replace `new ElementId(value.IntegerValue)` with `new ElementId(value.Value)` (3 sites in `RevitDocument.cs`)
- [ ] **7.17** Replace Category BuiltInCategory comparisons (5 sites) with `Category.EqualsBuiltInCategory()`

### Phase 5 — Removed API replacements
- [ ] **7.18** `RevitSurveyWall.cs:236` — `revitApp.Create.NewGeometryOptions()` → `new Options()`
- [ ] **7.19** `RevitDocument.cs:845` — `doc.Create.NewDetailCurve()` → `DetailLine.Create()`
- [ ] **7.20** `RevitDocument.cs:973` — same as above
- [ ] **7.21** `WallState.cs:135` — Verify `doc.Create.NewFamilyInstance(Curve, ...)` overload still exists; fix if removed
- [ ] **7.22** Remove dead `Application` parameter from `Utilities.ToRevitLine()` and its 10 call sites

### Phase 6 — WebClient → HttpClient
- [ ] **7.23** Rewrite `UpdateCheck.cs` `webClientWithTimeout` class to use `HttpClient`
- [ ] **7.24** Check if `PKLicenseSrc/PKLicense.cs` PayGo HTTP calls also use `WebClient`

### Phase 7 — Version strings and installer
- [ ] **7.25** Update `VersionInfo.xml` `<ProductRevitVersion>` to `2026`
- [ ] **7.26** `Setup/Product.wxs`: change `ProductRevitVersion` and generate new `ProductUpgradeCode` GUID
- [ ] **7.27** Update Revit addins folder path from `\2017\` to `\2026\` in WiX installer
- [ ] **7.28** Change `http://` → `https://` in `ProductInfo.cs`

### Phase 8 — Third-party library verification
- [ ] **7.29** Confirm `PLUSManaged.dll` (SoftwareKey) is compatible with .NET 8 — escalate if not
- [ ] **7.30** Replace `RevitLookup/` with official Revit 2026 RevitLookup release
- [ ] **7.31** Update NUnit to latest NUnit 3.x or 4.x in unit test projects

### Phase 9 — Test and validate
- [ ] **7.32** Full solution compile with zero errors
- [ ] **7.33** Run all NUnit unit tests (Distance, Geometry2D, PKLicense)
- [ ] **7.34** Load plugin in Revit 2026 and run manual test suite I/L/R/S from ManualTests.rtf
- [ ] **7.35** Verify shared parameters still bind correctly (ParameterType → ForgeTypeId change)

---

## SECTION 8 — Files NOT Requiring Changes

The following areas are pure business logic or UI layout and require no revision:

- `Geometry2D.cs` — pure math, no Revit API
- `Distance.cs` — value type, no Revit API
- `WallState.cs` (except `.IntegerValue` fix) — state machine logic
- `LevelsData.cs` — data holder
- `Settings.cs` — serialization, no Revit API
- All `*.Designer.cs` dialog layout files — WinForms Designer output, compatible with .NET 8 WinForms
- `DrivableForm.cs` — WinForms base, compatible
- `CommBase.cs`, `Disto.cs` — serial port communication, `System.IO.Ports` available in .NET 8
- `Geometry2D.cs`, `IProjectModel.cs` interfaces — no API dependencies

---

*Document auto-generated by analysis — do not edit the "Files affected" tables manually; always trace back to source.*
