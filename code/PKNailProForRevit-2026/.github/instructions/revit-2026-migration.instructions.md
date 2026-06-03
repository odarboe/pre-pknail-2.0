---
applyTo:
  - "**/*.cs"
  - "**/*.csproj"
---

# PKNail Pro For Revit — Revit 2026 / .NET 8 Migration Reference

This file documents the complete API and framework changes made when migrating PKNail Pro For Revit from **Revit 2017 / .NET Framework 4.6** to **Revit 2026 / .NET 8 (net8.0-windows)**.

Use this as a reference when:
- Writing new code that touches Revit API — use Revit 2026 patterns
- Reviewing or extending any migrated file
- Troubleshooting Revit API compile or runtime errors

**Git baseline**: `11c61d9` (pre-migration, Revit 2017)  
**Migration commit**: `966f97c` (Revit 2026 working, all tests pass)  
**Gitignore commit**: `06e3e54`

---

## Project Configuration

| Item | Before (2017) | After (2026) |
|------|--------------|-------------|
| Target framework | `net4.6` (legacy-style .csproj) | `net8.0-windows` (SDK-style .csproj) |
| Json project | `net4.0` legacy | `net8.0` SDK-style |
| PKNailForRevit DLL output | `bin\Debug\PKNailForRevit.dll` | `bin\Debug\net8.0-windows\PKNailForRevit.dll` |
| RevitAPI DLLs | Revit 2017 versions in `RevitAPI/` | Revit 2026 versions in `RevitAPI/` |
| Added NuGet packages | — | `System.IO.Ports` (CommBase.cs SerialPort), `Newtonsoft.Json` (PKLicenseSrc) |

### Build Command
```
cd PKNailProForRevit-2017\PKNailProForRevit-2017\PKNailForRevit
dotnet build PKNailForRevit.csproj --configuration Debug
```

### Addin Registration
- `.addin` file: `%APPDATA%\Autodesk\Revit\Addins\2026\PKNailProForRevit.addin`
- Addin GUID: `604B1052-F742-4951-8576-C261D1893108`
- DLL path in `.addin` must point to the `net8.0-windows\PKNailForRevit.dll` output

### Test Template
- Revit template: `PKNailForRevit\RevitFiles\Imperial\PKNailProForRevit.rte`

---

## API Changes by File

### `PKNailParameter.cs` — Parameter Type Enum

Revit 2026 replaced `ParameterType` enum with `ForgeTypeId` / `SpecTypeId`:

| Old (2017) | New (2026) |
|-----------|-----------|
| `ParameterType.Text` | `SpecTypeId.String.Text` |
| `ParameterType.Integer` | `SpecTypeId.Int.Integer` |
| `ParameterType.YesNo` | `SpecTypeId.Boolean.YesNo` |
| `ParameterType.Length` | `SpecTypeId.Length` |
| `ParameterType.Number` | `SpecTypeId.Number` |
| `ParameterType m_type` (field type) | `ForgeTypeId m_type` |

---

### `WallState.cs` — ElementId.Value is now `long`

In Revit 2026, `ElementId.Value` changed from `int` to `long`. All arrays and return types that stored raw ElementId values must be `long`:

| Old (2017) | New (2026) |
|-----------|-----------|
| `int[] m_LevelOffsetDetailIdValue` | `long[] m_LevelOffsetDetailIdValue` |
| `inst.Id.IntegerValue` | `inst.Id.Value` |
| `int CreateInstance(...)` return type | `long CreateInstance(...)` |

**Rule**: Never use `.IntegerValue` — always use `.Value` (returns `long`).

---

### `RevitSurveyWall.cs` — Application parameter removed, Options constructor changed

Revit 2026 removed the need to pass `Application` for geometry options:

| Old (2017) | New (2026) |
|-----------|-----------|
| `WallEdge(Application revitApp, Wall wall, ...)` | `WallEdge(Wall wall, ...)` |
| `revitApp.Create.NewGeometryOptions()` | `new Options()` |
| `Centerline(Application revitApp, Wall w)` | `Centerline(Wall w)` |
| `WallIntersectCorner(Application revitApp, ...)` | `WallIntersectCorner(...)` |

---

### `Utilities.cs` — Unused Application parameter removed

| Old (2017) | New (2026) |
|-----------|-----------|
| `ToRevitLine(Application revitApp, G2D.Vector v)` | `ToRevitLine(G2D.Vector v)` |

---

### `RevitDocument.cs` — Multiple API changes

#### Detail Curve Creation
| Old (2017) | New (2026) |
|-----------|-----------|
| `DetailLine.Create(view, curve)` | `doc.Create.NewDetailCurve(view, curve)` |

#### SpecTypeId namespace change
| Old (2017) | New (2026) |
|-----------|-----------|
| `SpecTypeId.Measurable.Length` | `SpecTypeId.Length` |

#### FilterStringRule argument count
| Old (2017) | New (2026) |
|-----------|-----------|
| `FilterStringRule(p, evaluator, value, caseSensitive)` (4 args) | `FilterStringRule(p, evaluator, value)` (3 args — case sensitivity is on the evaluator) |

#### View name property
| Old (2017) | New (2026) |
|-----------|-----------|
| `view.ViewName` | `view.Name` |

#### ElementId arrays (same as WallState.cs)
All `int` fields/arrays that held `ElementId.Value` are now `long`.

---

### `UpdateCheck.cs` — HttpClient replaces WebClient

`WebClient` is obsolete in .NET 8. Pattern is now:

```csharp
// Old (2017):
using (var client = new WebClient()) { ... }

// New (2026):
using var client = new HttpClient();
var response = await client.GetStringAsync(url);
```

Also, `Process.Start(url)` requires `UseShellExecute = true` on .NET Core/.NET 8:

```csharp
// Old (2017):
Process.Start(url);

// New (2026):
Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
```

---

### `ProductInfo.cs` — Version strings updated

- Revit version string: `"2017"` → `"2026"`
- Update check URL: `http://` → `https://`

---

## Shared Parameter Storage — Important Name Mapping

PKNail stores document state in Revit `ProjectInfo` shared parameters (file: `PKNailSharedParameters.txt`). The parameter names in that file use old class names. After the class rename `Settings` → `DocumentSettings`, a mapping dictionary was added to `RevitDocument.cs`:

```csharp
// In RevitDocument.cs — maps current class name to the stored parameter name
private static readonly Dictionary<string, string> s_typeToParamName = new()
{
    { "DocumentSettings", "Settings" },   // class renamed but param name must stay "Settings"
};

private static string GetStoredParamName(Type t)
{
    return s_typeToParamName.TryGetValue(t.Name, out var name) ? name : t.Name;
}
```

The parameter names in `PKNailSharedParameters.txt` that the plugin uses:

| Stored param name (in .txt file) | Current class name |
|---|---|
| `PKNail Settings` | `DocumentSettings` |
| `PKNail LevelsData` | `LevelsData` |
| `PKNail WallState` | `WallState` |
| `PKNail documentStateContainer` | `documentStateContainer` |

**Rule**: If you rename any class that is serialized as a Revit shared parameter, add a mapping entry to `s_typeToParamName` in `RevitDocument.cs`.

---

## What Was NOT Changed (Preserved Logic)

All of the following were verified unchanged between `11c61d9` and `966f97c`:

- All measurement calculation logic (`Distance.cs`, `Geometry2D.cs`)
- All wall survey algorithms (`FirstWallDlg`, `SurveyWallDlg`, `WallState`)
- All dialog workflows (DrivableForm navigation, all Dlg classes)
- JSON serialization/deserialization (LitJson library unchanged)
- Disto serial communication (`CommBase.cs`, `Disto.cs`)
- Licensing system (`PKLicense`, `PKLicenseSrc`)
- All ribbon commands and command routing
- Hosted object placement (Door, Window, OpeningObject)
- Coordinate system operations
- Copy objects workflow
- Unknown-width wall conversion

---

## Common Compile-Error Patterns After API Changes

If you see these errors after adding new code or updating Revit API DLLs:

| Error message | Cause | Fix |
|---|---|---|
| `'ParameterType' does not contain definition for 'Text'` | Used 2017 enum | Use `SpecTypeId.String.Text` |
| `Cannot implicitly convert 'long' to 'int'` | ElementId.Value | Change field/return type to `long` |
| `'ElementId' does not contain 'IntegerValue'` | 2017 API | Use `.Value` instead |
| `'Application' does not contain 'NewGeometryOptions'` | Removed in 2026 | Use `new Options()` |
| `FilterStringRule` has N arguments... | Arg count changed | Drop the `caseSensitive` boolean arg |
| `'View' does not contain 'ViewName'` | Renamed | Use `.Name` |
| `'DetailLine' does not contain 'Create'` | Static factory removed | Use `doc.Create.NewDetailCurve(view, curve)` |
| `Parameter 'PKNail X' is undefined` (runtime) | Class renamed without updating param key | Add entry to `s_typeToParamName` in `RevitDocument.cs` |
