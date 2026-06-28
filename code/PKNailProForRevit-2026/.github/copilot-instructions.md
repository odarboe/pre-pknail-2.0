# PKNail Pro For Revit 2026 — Copilot Instructions

## Project Overview

**PKNail Pro For Revit** is an Autodesk Revit 2026 plugin by PointKnown LLC (support@pointknown.com, (617) 575-2222). It enables field surveyors to capture as-built measurements from a **Leica Disto laser rangefinder** directly into a Revit model in real time. The surveyor walks through a building measuring walls, doors, windows, and other elements; the plugin places corresponding Revit family instances at the measured locations with the correct dimensions.

**Solution**: `PKNailForRevit.sln`  
**Namespace root**: `PKNailForRevit`  
**Entry point**: `PKNailForRevitApp` (implements `IExternalApplication`)  
**Target**: Autodesk Revit 2026, .NET 8 (`net8.0-windows`)  
**Licensing**: PointKnown SOLO license server + custom PayGo metered usage  
**Source control**: Git

---

## PKNAIL 2.0 — AI-Engine JSON Import (CURRENT DIRECTION)

> Most of this document describes the **PKNAIL 1.0 rangefinder workflow**, which is still in the
> codebase. **The active 2.0 effort changes the input, not the placement engine.** Read
> **`docs/PKNAIL_2.0_JiraPlan.md`** (master plan) and **`docs/PKNAIL_Fields.md`** before doing 2.0 work.

**What changes:** PKNAIL 2.0 **removes the Leica Disto rangefinder/device step.** The **point cloud is
never imported into this plugin** — it is uploaded into a **separate, independent AI-engine repository**
(e.g. Cognixlab's Python pipeline) which reads the `.las` / GeoSLAM data and emits a single JSON file,
`ai_engine_pknail_results.json`. **That JSON is the plugin's only 2.0 input.** The plugin deserializes it
into `Detected*` DTOs, maps them onto the existing PKNAIL parameter objects, and drives the **same proven
placement engine** (`IProjectModel` / `RevitDocument`) — **headlessly, with no survey dialogs and no Disto**.

```
PKNAIL 1.0:  Laser → Survey Dialogs → PKNAIL engine → Revit Model
PKNAIL 2.0:  Point Cloud → [external AI-engine repo] → ai_engine_pknail_results.json → PKNAIL plugin → Revit Model
```

**Boundary:** this repo does **not** parse point clouds at runtime. The `.las` files under `PointCloud/`
are AI-engine sample inputs kept for reference only. The headless bridge is **`AiModelImporter`**
(`PKNAIL_Fields.md` calls it `PointCloudImporter`; it consumes the JSON, not the cloud). The `Detected*`
DTOs and `AiModelImporter` **do not exist in the code yet** — building them is the 2.0 work (Epics 1–3 of
the Jira plan). The rangefinder/`Disto` path stays as **legacy/optional** — do not delete without sign-off.

**Working agreement:** implement **one epic at a time** → write full automated + step-by-step manual tests
→ user confirms → **only then `git commit`** (one focused commit per epic) → next epic. **If anything is
unclear or conflicting, STOP and ask.** Known discrepancies (no wall coordinates in the JSON, hardcoded
`FamilyDefIdx`, unit/naming mismatches, metering at scale) are tracked in the Jira plan §5–§6.

---

## Repository Structure

```
PKNailProForRevit-2017/
├── .github/
│   ├── copilot-instructions.md        ← this file
│   ├── instructions/
│   │   └── revit-2026-migration.instructions.md  ← full Revit 2026 migration API reference
│   └── agents/
│       └── pknail-revit.agent.md
├── PKNailForRevit/                    ← main plugin project
│   ├── PKNailForRevitApp.cs           ← singleton TheApp, startup/shutdown, ribbon, command dispatch
│   ├── RevitDocument.cs               ← IProjectModel impl, all Revit API calls
│   ├── IProjectModel.cs               ← interface + all domain parameter classes
│   ├── Distance.cs                    ← value type for all measurements
│   ├── Geometry2D.cs                  ← 2D geometry (Point, Vector, Radians)
│   ├── Disto.cs                       ← Leica Disto serial comm + keypad events
│   ├── CommBase.cs / CommLine.cs      ← serial port base comm
│   ├── LevelsData.cs                  ← floor/level data model
│   ├── WallState.cs                   ← per-wall survey state (face surveyed, offsets)
│   ├── RevitSurveyWall.cs             ← Revit wall helpers (azimuth, placement)
│   ├── RevitModelState.cs             ← model snapshot for automated test comparison
│   ├── Settings.cs                    ← PKNailAppSettings (user) + DocumentSettings (project)
│   ├── UpdateCheck.cs                 ← periodic version check against pointknown.com/esd
│   ├── CustomFailures.cs              ← register Revit failure suppressors
│   ├── *Dlg.cs                        ← WinForms dialog for each survey command
│   └── DrivableForm.cs                ← base form supporting rangefinder-driven navigation
├── PKLicenseSrc/                      ← license logic (SOLO & PayGo)
│   ├── PKLicense.cs
│   ├── PKLicenseData.cs
│   ├── StringEncryption.cs
│   ├── ConsumedUsageData.cs
│   └── UsageRecord.cs
├── PKNailForRevit.UnitTests/          ← NUnit unit tests (Distance, Geometry2D, DrivableForm, Disto)
├── PKNailForRevit.TestCore/           ← Revit-based integration test scaffolding
├── PKNailForRevit.TestRunner/         ← test runner harness
├── Json/                              ← custom LitJson-based JSON serializer
├── PKLicense/                         ← compiled PKLicense assembly
├── PKLicense.UnitTests/               ← NUnit tests for license + encryption
├── DevDoc/
│   ├── ManualTests.rtf                ← full manual test case list (see Testing section)
│   ├── ReleaseProcedure.rtf           ← build/deploy steps
│   └── UpgradeRevitVersionProcedure.rtf ← Revit version upgrade checklist
├── Setup/                             ← WiX installer
├── SetupCA/                           ← custom installer actions
└── RevitAPI/                          ← RevitAPI.dll, RevitAPIUI.dll (2017)
```

---

## Architecture & Key Design Patterns

### Singleton Application (`TheApp`)
`TheApp` (in `PKNailForRevitApp.cs`) is a private-constructor singleton that manages:
- All active `IMeasurementProvider`, `IKeypadCommandProvider`, and `IDialogNavigationProvider` instances
- The `Disto` rangefinder connection
- The `PKLicense` instance
- The Revit ribbon panel
- The `ExternalEvent` / `AutoResetEvent` for rangefinder-driven command execution on the Revit UI thread

### `IProjectModel` Interface
All operations are exposed through `IProjectModel`. The concrete implementation is `RevitDocument`. The split enables the integration test framework to substitute a mock model. Key method pairs:
- `Prep` methods → open a **trial transaction** (roll-back at end) to read Revit data and populate parameter objects
- Non-`Prep` methods → execute the command in a committed `TransactionGroup`

### Dialog Navigation ("`DrivableForm`")
Every survey dialog inherits `DrivableForm`. The form can be controlled by rangefinder keypad input (via `IDialogNavigationProvider`) so the surveyor need not touch a keyboard. `DriveAndShowDialog(mps, dnps)` shows the form and wires up rangefinder measurement events.

### Distance Value Type
`Distance` stores all measurements internally in **decimal feet** (same as Revit internal units). It supports:
- Imperial (feet + fractional inches)
- MetricMeter, MetricMillimeter
- PKInternal / RevitInternal (decimal feet)
- Sentinel value `Distance.NoValue` (stored as -10000001.0001)

### JSON Serialization
`JsonSerializable` base class uses a custom LitJson-based serializer in the `Json/` project. All data stored in the Revit document (levels, wall state, document settings) is JSON-serialized as Revit extensible storage data parameters (`PKNailParameter`).

---

## Ribbon Commands

| Ribbon Label | Command Method | Transaction Name |
|---|---|---|
| Connect | `ConnectRFCommand_Txn` | "Connect" |
| Disconnect | `DisconnectRFCommand_Txn` | "Disconnect" |
| Change Key Map | `ChangeKeymapCommand_Txn` | "Change Key Map" |
| Capture Levels | `LevelsDlgCommand_Txn` | "Capture Levels" |
| Activate Wall | `ActivateWallCommand_Txn` | "Activate Wall" |
| Set Coord Sys | `SetCoordinateSystemCommand_Txn` | "Set Coord Sys" |
| Wall (L→R) | `SurveyWallCommand_Txn` (LR) | "Wall" |
| Wall (R→L) | `SurveyWallCommand_Txn` (RL) | "Wall" |
| Line | `SurveyLineCommand_Txn` | "Line" |
| Door | `SurveyDoorCommand_Txn` | "Door" |
| Window | `SurveyWindowCommand_Txn` | "Window" |
| Wall-Hosted Object | `SurveyWallHostedObjectCommand_Txn` | "Wall-Hosted Object" |
| Copy Level-to-Level | `CopyObjectsCommand_Txn` | "Copy Level-to-Level" |
| Convert Walls | `ConvertUnknownWidthWallsCommand_Txn` | "Convert Walls" |
| Settings | Settings dialog | — |

---

## Step-by-Step Workflow

### 1. Plugin Startup (`OnStartup`)
1. Show `SplashForm` ("Checking License…")
2. Register `CustomFailures` (suppress Revit failure dialogs)
3. Load `PKNailAppSettings` (user-scoped)
4. Call `populateRibbon` → add ribbon panel with all commands
5. Call `getLicense` → load or activate license; validate system date/time via NTP
6. If license invalid → show TaskDialog; all ribbon buttons except Settings disabled
7. If license valid → show "Loading…" splash, call `initializeApp`, register document event handlers
8. Run `UpdateCheck` (weekly, against `pointknown.com/esd/latestVersion.php`)
9. Hide splash

### 2. Project Initialization (`RevitDocument.Initialize`)
Called when a Revit document opens. Reads `PKNailVersionInfo` from extensible storage, updates `CurrentRun` version stamp, commits transaction.

### 3. Capture Levels (`LevelsDlgCommand_Txn`)
1. If no levels defined: `NumLvlDlg` → user enters number of floors + whether drop ceilings exist
2. Open `LevelsDlg` (Levels Wizard)  
   - Left list: all levels (top = highest floor)
   - Per level fields: **Name**, **Is Ref Elev**, **Project Elev**, **Floor→Floor**, **Floor→Ceiling**, **Drop Ceiling** (None or measured), **Typical Window Heights**
   - Undo, Move Up/Down, Add/Remove level controls
3. On close: `proj.LevelsData = dlg.Data` (saved to Revit doc extensible storage)

**`LevelDataItem` fields tracked**:

| Field | Type | Notes |
|---|---|---|
| `Name` | string | Level display name |
| `ElementId` | string | Revit Level element ID |
| `IsRefElev` | bool | Reference elevation anchor |
| `ProjectElev` | Distance | Absolute elevation (calculated from Floor2Floor) |
| `Floor2FloorElev` | Distance | Height from this level to next level up |
| `Floor2CeilingElev` | Distance | Floor to finished ceiling height |
| `IsDropCeiling` | bool | Whether a drop ceiling exists |
| `Floor2DropCeilingElev` | Distance | Floor to drop ceiling height |
| `TypicalWindowHeights` | List\<WindowHeights\> | Typical sill + window heights for this level |

### 4. Survey Wall (`SurveyWallCommand_Txn`)

**Direction variants**: Left→Right, Right→Left, From Model (reads from active element).

#### 4a. Prep Phase (`SurveyWallPrep`)
- Opens trial transaction (rolls back at end)
- Builds `CommandContext` (current level, survey direction/orientation, display units)
- Populates `WallSurveyParams`:
  - `IsFirstWall` = true if no walls exist in model
  - `IsCreateWall` = true if a new wall will be placed
  - `IsSurveyRequired` = true if dialog input is needed
  - `ProjectWallWidthList` → all distinct wall widths already in model
  - `ProjectLevelVisibleHeightList` → available floor-to-floor heights per level
  - `CornerAngleList`, `MaxLengthList` → available corner angles from adjacent walls
  - `ConnectedLevelIdx`, `CloseLength`, `CloseCornerAngle`, `LevelOffset`, `ReferenceFaceLength`

#### 4b. First Wall Dialog (`FirstWallDlg`)
Shown only for the very first wall in the project.

| Field | Values | Effect |
|---|---|---|
| Surveyor Perspective | Outside / Inside | Sets `SurveyOrientation` (Flipped/NotFlipped) |
| Compass Direction | Don't know / N / E / S / W | Sets `SurveyorCompassDirection` on `WallSurveyParams` |

#### 4c. Survey Wall Dialog (`SurveyWallDlg`)

| Field | Type | Notes |
|---|---|---|
| **Length** | Distance | Wall length; defaults from `MaxLengthList` matching selected corner angle |
| **Width** | Distance or CB | Combo with existing widths + `<measure>`; `WallWidthRounding` applied |
| **Height / Connected Level** | Distance or CB | Select connected level or measure; `LocationRounding` applied |
| **Level Offset** | Distance | Height offset for this face (stored in `WallState`) |
| **Corner Angle** | Degrees CB | From `CornerAngleList` or measured via `MeasureCornerAngleDlg` |
| **Unknown Width** | Checkbox | Creates wall with placeholder width; disabled after face is surveyed |
| **Close Wall** | Checkbox | Switches length to `CloseLength`, angle to `CloseCornerAngle` |

**Corner Angle Measurement** (`MeasureCornerAngleDlg`): Three measurements — reference face length, survey wall length, and diagonal — compute the corner angle from a scalene triangle.

#### 4d. License Usage Recording
Before `proj.SurveyWall(...)`:
```csharp
bool ok = m_license.RecordUsage(proj.DocumentSettings.ProjectName, m_appSettings.UserName);
```
MeteredDaily/MeteredHourly/Subscription licenses check balance; `cm_insufficientUsageMsg` returned if balance zero.

#### 4e. Revit Model Update
`SurveyWall` commits a `TransactionGroup` that:
- Places wall using Revit `Wall.Create`
- Sets wall type, height, connected level
- Stores `WallState` into extensible storage
- Places a "survey sprite" (arrow indicating survey direction)
- Auto-saves if interval elapsed

### 5. Activate Wall (`ActivateWallCommand_Txn`)
Used to **reference** an existing wall (already in model) so subsequent surveys continue from its corner.

1. `ActivateWallPrep`: user picks wall face via `uidoc.Selection.PickObject` (filtered to wall faces)
2. If picked face has hosted openings (doors/windows) → offer `TransitOpeningDlg` to use opening as reference
3. Otherwise → `SurveyWallDlg` with `IsSurveyRequired=true` for the active face length / level offset
4. `ActivateWall` modifies the Revit model: adjusts wall corners and saves survey face state

**Transit Opening Dialog (`TransitOpeningDlg`) fields**:

| Field | Direction Variant | Notes |
|---|---|---|
| **Wall Extent** | Both | Overall wall length |
| **Corner To Opening** | L→R: "Left Corner To Opening" / R→L: "Right Corner To Opening" | Distance from near corner to opening center |
| **Level Offset** | Both | Elevation delta for this wall face |

Diagram panel renders different bitmap based on direction and focused control.

### 6. Set Coordinate System (`SetCoordinateSystemCommand_Txn`)
- User picks a wall face
- Plugin reads wall azimuth, normalizes to within [-π/4, +π/4]
- Stores as coordinate system rotation angle in document extensible storage

### 7. Survey Door (`SurveyDoorCommand_Txn`)

Prep → `SurveyWallHostedObjectPrep` with `FamilyCategory.Doors`.
Dialog → `SurveyDoorDlg` (extends `SurveyWallHostedObjectForm`).

**`SurveyDoorDlg` fields**:

| Field | Notes |
|---|---|
| **Location** | Distance along host wall |
| **Width** | Door rough opening width; may be fixed per family def |
| **Height** | Door rough opening height; may be fixed |
| **Sill Height** | Height of opening bottom from floor; may be fixed |
| **Center** checkbox | Sets Location to wall midpoint |
| **Type** (CB) | From `ProjectSurveyFamilyDefs` list |
| **Hinge Side / Slide Direction** | Left/Right (hinged) or Right→Left / Left→Right (sliding) |
| **Swing Direction / Side Facing** | Out/In (swinging) or Interior/Exterior (facing) |

`SurveyFamilyDef.FixedDimensions` flags control which dimension fields are editable.

### 8. Survey Window (`SurveyWindowCommand_Txn`)

Dialog → `SurveyWindowDlg` (extends `SurveyWallHostedObjectForm`).

**`SurveyWindowDlg` fields**:

| Field | Notes |
|---|---|
| **Location** | Distance along host wall |
| **Width** | Window rough opening width |
| **Height** | Window rough opening height |
| **Sill Height** | Distance from floor to sill |
| **Center** checkbox | Sets Location to wall midpoint |
| **Type** (CB) | From family definitions |
| **Typical Heights** checkbox | Locks Height and Sill Height to level's typical window heights |
| **Stack Windows** checkbox | Only enabled when Typical Heights checked; stacks this window above typical |
| **Side Facing** | Interior / Exterior (if family supports it) |

State logic (case S.5): when unchecking Typical Heights, the previously entered custom values are restored (saved in `m_savedHeight`, `m_savedSillHeight`).

### 9. Survey Wall-Hosted Object (`SurveyWallHostedObjectCommand_Txn`)
Generic version: user first picks family category from `PickOneDlg`, then `SurveyHostedObjectDlg` is shown. Same base fields as Door/Window but without Door/Window-specific orientation options.

### 10. Survey Line (`SurveyLineCommand_Txn`)

**`SurveyLineDlg` fields**:

| Field | Notes |
|---|---|
| **Location** | Distance along reference wall face (disabled if no reference face wall) |
| **Length** | Line length; defaults from `MaxLengthList[cornerAngleIdx]` |
| **Center** checkbox | Sets Location to reference wall midpoint |
| **Corner Angle** (CB) | Available angles from adjacent walls |
| **Type** (CB) | Line style from project line styles |

If `IsParallel`: Length and Corner Angle are disabled (line is drawn parallel to reference).

**Rangefinder keypad workflow on a line** (case S.3): when "dude" sprite is on a line and right-arrow is pressed, `SurveyLineDlg` opens again to extend/add another line.

### 11. Copy Level-to-Level (`CopyObjectsCommand_Txn`)

`CopyObjectsDlg` fields:

| Field | Notes |
|---|---|
| **From** label | Current active level (read-only) |
| **Upper Level** (CB) | Target level above current |
| **Lower Level** (CB) | Target level below current |
| **Walls** | Not configurable (always from selection) |
| **Windows** | Selected / All / None |
| **Doors** | Selected / All / None |
| **Other Wall-Hosted Objects** | Selected / All / None |

### 12. Convert Unknown-Width Walls (`ConvertUnknownWidthWallsCommand_Txn`)

`ConvertUnknownWidthWallsDlg`: User selects unknown-width walls, chooses target wall type/width.

---

## Data Model — All Tracked Fields

### `PKNailAppSettings` (user-scoped, persisted in .NET user settings)

| Field | Default | Notes |
|---|---|---|
| `UserName` | "" | Used in license usage recording |
| `RangefinderComPort` | "" | COM port name (e.g. "COM3") |
| `IsAutoDetectComPort` | true | Auto-detect COM port on connect |
| `AutoSaveInterval` | 5 | Commands between auto-saves |
| `IsPeriodicUpdateCheck` | true | Weekly update check enabled |
| `LastUpdateCheck` | 1/1/0001 | Date of last update check |
| `SkipVersion` | "" | Skip notification for this version |
| `IsOpenProjectSettingsOnCreateDocument` | true | Open settings on new doc |
| `LastProjectName` | "" | Last project for display purposes |

### `DocumentSettings` (project-scoped, JSON in Revit extensible storage)

| Field | Imperial Default | Metric Default | Notes |
|---|---|---|---|
| `ProjectName` | "" | "" | |
| `LocationRounding` | 1/4" | 10mm | Rounding for wall/line lengths |
| `WallWidthRounding` | 1/8" | 5mm | Rounding for wall widths |
| `FeatureDimensionRounding` | 1/4" | 10mm | Rounding for door/window dims |
| `MeasurementTolerance` | 1/4" | 10mm | Snap tolerance |
| `WallIntersectFloorElevationOffset` | 3" | 80mm | Height above floor for wall-floor intersect |
| `FullOpeningTopElevationOffset` | 1/16" | 2mm | Clearance above opening top |
| `CutPlaneElevation` | 4' | 1500mm | Revit view cut plane height |
| `RangefinderKeypadMaps` | default map | | Keypad button → command mapping |
| `IsStructuralWalls` | false | | Place walls as structural walls |

### `CommandContext` (per-command, not persisted)

| Field | Type | Notes |
|---|---|---|
| `ProjectLevelNames` | List\<string\> | All level names (top to bottom) |
| `CurrentLevelIdx` | int | Active level index |
| `SurveyDirection` | enum | LeftToRight / RightToLeft / FromModel |
| `SurveyOrientation` | enum | NotFlipped / Flipped |
| `DisplayUnitSystem` | enum | Imperial / Metric |
| `ModelSettings` | DocumentSettings | Current project settings |
| `ZoomElements` | List\<string\> | Element IDs for post-command zoom |

### `WallSurveyParams` (per wall survey command)

| Field | Type | Notes |
|---|---|---|
| `IsSurveyRequired` | bool | Dialog must be shown |
| `IsCreateWall` | bool | A wall will be placed |
| `IsFirstWall` | bool | No walls exist; FirstWallDlg shown |
| `IsCloseWall` | bool | Close-wall option available |
| `Length` | Distance | Wall length input |
| `Width` | Distance | Wall width input |
| `Height` | Distance | Wall height input |
| `LevelOffset` | Distance | Elevation offset for this wall face |
| `IsUnknownWidth` | bool | Width is not yet known |
| `ConnectedLevelIdx` | int | Connected-to level (-1 = measure) |
| `CornerAngleList` | List\<Radians\> | Available corner angles |
| `MaxLengthList` | List\<Distance\> | Default lengths per corner angle |
| `ProjectWallWidthList` | List\<Distance\> | Available wall widths |
| `ProjectLevelVisibleHeightList` | List\<Distance\> | Available heights per level |
| `CornerAngle` | Radians | Selected corner angle |
| `CloseLength` | Distance | Auto-calculated close length |
| `CloseCornerAngle` | Radians | Corner angle for close operation |
| `ReferenceFaceLength` | Distance | Length of reference wall face |
| `ReferenceOpeningParams` | object | Set if TransitOpening flow |
| `SurveyorCompassDirection` | enum | N/E/S/W/Unknown (first wall only) |

### `WallHostedObjectSurveyParams` (doors, windows, generic hosted objects)

| Field | Type | Notes |
|---|---|---|
| `Location` | Distance | Distance along host wall |
| `HostWallHeight` | Distance | Height of the host wall |
| `HostWallLength` | Distance | Length of the host wall |
| `TypicalHeight` | Distance | Level's typical window height |
| `TypicalSillHeight` | Distance | Level's typical sill height |
| `IsTypicalWindowHeights` | bool | Use typical heights |
| `IsStackedWindows` | bool | Stack above typical height |
| `ProjectSurveyFamilyDefs` | List\<SurveyFamilyDef\> | Available family types |
| `SurveyFamilyDefIdx` | int | Selected family index |

### `SurveyFamilyDef` (per Revit family type)

| Field | Type | Notes |
|---|---|---|
| `Name` | string | Family display name |
| `Width` | Distance | Rough opening width |
| `Height` | Distance | Rough opening height |
| `Depth` | Distance | Wall depth (for embedded objects) |
| `SillHeight` | Distance | Sill height from floor |
| `FixedDimensions` | Flags | Which dims are fixed (Width, Height, Depth, SillHeight) |
| `LateralOrientationType` | enum | None / Hinged / Sliding |
| `FacingOrientationType` | enum | None / Swinging / Facing |
| `LateralOrientation` | int | HingeSide or SlideDirection index |
| `FacingOrientation` | int | SwingDirection or SurveyorRelation index |
| `IsMeasureLocationCentered` | bool | Default location = center |

### `ReferenceOpeningParams` (TransitOpening activate-wall flow)

| Field | Type | Notes |
|---|---|---|
| `ReferenceOpeningID` | string | Revit ElementId of the reference opening |
| `CornerToOpening` | Distance | Near corner to opening center |
| `ContinuationCornerToOpening` | Distance | Far corner to opening center |
| `WallExtent` | Distance | Total wall extent |
| `ContinuationWallExtent` | Distance | Wall extent for continuation segment |
| `LevelOffset` | Distance | Level offset for this activation |

### `WallState` (per Revit wall, stored in extensible storage)

| Field | Size | Notes |
|---|---|---|
| `IsFaceSurveyed[2]` | bool[2] | Index 0 = NotFlipped, 1 = Flipped |
| `LevelOffset[2]` | Distance[2] | Per-face level offset |
| `RefPtOffset[4]` | Distance[4] | Reference point offset per (SurveySide × SurveyOrientation) |
| `LevelOffsetDetailIdValue[2]` | int[2] | ElementId of level-offset detail element |

### `LineSurveyParams`

| Field | Type | Notes |
|---|---|---|
| `Location` | Distance | Distance along reference wall |
| `Length` | Distance | Line length |
| `CornerAngle` | Radians | Angle relative to reference |
| `LineStyles` | List\<string\> | Available line styles |
| `LineStyleIdx` | int | Selected line style |
| `IsParallel` | bool | Parallel to reference; no length/angle input |
| `IsReferenceFaceWall` | bool | Whether a reference face wall exists |
| `ReferenceFaceLength` | Distance | Length of host reference face |
| `MaxLengthList` | List\<Distance\> | Default length per corner angle |

---

## Rangefinder Integration (Disto)

`Disto` implements `IMeasurementProvider`, `IKeypadCommandProvider`, and `IDialogNavigationProvider`.

**Connection**: Detects COM port automatically or uses `PKNailAppSettings.RangefinderComPort`.  
**State machine**: `Disconnected → ConnectPending → Connected → DisconnectRequested / ReconnectRequested / SerialPortError`

**Keypad contexts**:
- `m_dialogNavigationKeypadContext` (2 short presses, 1 long = navigation)
- `m_commandKeypadContext` (1 short press, 2 long = command trigger)

**Events emitted**:
- `OnMeasurement` → distance measured
- `OnKeypadCommand` → keypad button phrase (e.g., arrow key sequence)
- `OnDialogNavigation` → navigate within a dialog (up/down/confirm/cancel)

A `Stack<Delegate>` is maintained for measurement and keypad handlers so that dialog stacks correctly restore context on close.

**Keypad map** (`RangefinderKeypadMaps`, stored in `DocumentSettings`): maps keypad button phrases to survey commands (Wall, Line, Door, Window, generic objects). Selected map page stored in `m_currentRFKeymapPage`.

**Key behavioral rules**:
- Rangefinder commands fire on the Revit UI thread via `ExternalEvent` + `AutoResetEvent`
- If Revit is busy (in edit mode), the command fails with an audible warning (bong) — see test R.1
- Wall-hosted objects cannot be placed on a detail line (test S.4)

---

## Licensing System

**License file**: `%AppData%\PointKnown\license\PKNailProLicense.xml`  
**License data file**: `PKNailProLicenseData.json`

**License types** (SOLO trigger codes):

| Code | Type | Behavior |
|---|---|---|
| 1 | Perpetual | No usage recording; never expires |
| 10 | NotForResale (INVALID/legacy) | Deprecated; disabled |
| 11 | Subscription (Legacy) | Usage recorded; server-checked |
| 14 | MeteredDaily | Daily usage unit deducted per command |
| 15 | MeteredHourly | Hourly metered session |
| 16 | Subscription | Usage recorded per command with balance check |

**Usage recording**: `PKLicense.RecordUsage(projectName, userName)` is called before every wall, line, door, window, hosted-object placement. Returns `false` if balance is exhausted.

**System date/time validation**: NTP servers (`0.pool.ntp.org`, `1.pool.ntp.org`, `time.windows.com`). Tolerance: 1.5 hours. If clock is off by more than 2.5 hours, licensing is disabled (test L.1).

**License refresh**: After `cm_licenseRefreshIntervalDays` (1 day in Release, 0 in Debug), the plugin refreshes the license file from SOLO.

**Background usage posting**: A background thread (`m_postUsageRecordsThread`) posts accumulated usage records to `paygo.pointknown.com/webapi/license/` every 10 minutes (Release) or 10 seconds (Debug).

---

## Settings Dialog

`SettingsDlg` hosts multiple tab pages as user controls:

| Tab | Control | Key Settings |
|---|---|---|
| App | `SettingsAppCtl` | UserName, AutoSaveInterval, ComPort, IsAutoDetect |
| Project General | `SettingsProjectGeneralCtl` | ProjectName, IsStructuralWalls, CutPlaneElevation, WallIntersectFloorOffset |
| Project Rounding | `SettingsProjectRoundingCtl` | LocationRounding, WallWidthRounding, FeatureDimensionRounding, MeasurementTolerance |
| RF Keypad Map | `SettingsRFKeypadMapCtl` | Keypad button → command mapping per page |
| Updates | `SettingsUpdatesCtl` | IsPeriodicUpdateCheck, force update check |
| License | `SettingsLicenseCtl` | View license info, activate new license |
| About | `SettingsAboutCtl` | Version, copyright |

---

## RevitModelState (Automated Test Snapshots)

Used in integration tests (`PKNailForRevit.TestCore`/`TestRunner`) to capture and compare Revit model state before/after each test command.

```
RevitModelState
├── RevitWallModelStateList        → all Wall elements
├── RevitDetailCurveModelStateList → all DetailCurve elements
└── RevitFamilyInstanceModelStateList → all FamilyInstance elements
```

`ModelState.Compare(other, out diffMsg)` returns `false` with a diff message if models diverge. Used to verify that exactly the expected elements were created.

---

## Build & Test

### Automated Unit Tests (NUnit)
- `PKNailForRevit.UnitTests/DistanceTest.cs` — Distance parsing, arithmetic, unit conversions
- `PKNailForRevit.UnitTests/Geometry2DTest.cs` — Vector azimuth, intersection, parallel/perpendicular
- `PKNailForRevit.UnitTests/DistoTest.cs` — Disto serial communication
- `PKNailForRevit.UnitTests/DrivableFormTest.cs` — DrivableForm behavior
- `PKLicense.UnitTests/StringEncryptionTest.cs` — StringEncryption round-trip
- `PKLicense.UnitTests/UsageDataTest.cs` — Usage recording logic
- `Json.UnitTests/` — JSON serializer tests

### Integration Tests (Revit in-process)
`PKNailForRevit.TestRunner` drives Revit via `IExternalApplication` and compares `RevitModelState` snapshots. Test `.rvt` files are source-controlled in `RevitFiles/`.

### Build Configurations
| Config | Notes |
|---|---|
| Debug | `BYPASS_LICENSE_CHECK`, `BYPASS_UPDATE_CHECK` defined; license intervals shortened |
| Release | Full license/update check; assembly signed |
| Deploy64 | Produces setup bootstrapper |

### Key Compiler Symbols
| Symbol | Effect |
|---|---|
| `BYPASS_LICENSE_CHECK` | Skip SOLO license; always valid |
| `BYPASS_UPDATE_CHECK` | Skip update check |
| `IS_SELECT_REMOTE_OR_LOCAL_SERVER` | Point PayGo at localhost:8080 |

---

## Manual Test Cases (from `DevDoc/ManualTests.rtf`)

### Installation
| ID | Description | Expected Result |
|---|---|---|
| I.1 (323) | Install from setup file | UAC dialog shows "signed by PointKnown LLC" |
| I.2 (345) | Upgrade from previous version | New version runs; license files untouched |
| I.3 (417) | Force update check; click Download | Warning shown before download starts |

### Licensing
| ID | Description | Expected Result |
|---|---|---|
| L.1 (414) | Clock back 1:30 → trial activate; clock back 2:30 → trial activate | 1:30 ok; 2:30 error "system time invalid" |
| L.2 | Clock back 2:00 after NFR activate | Error "system time invalid"; PK disabled |
| L.3 (418,420) | Expired subscription license | Expired dialog shown; ribbon disabled except Settings; past date shown in red |

### Rangefinder
| ID | Description | Expected Result |
|---|---|---|
| R.1 (337) | Start manual Revit edit, trigger Survey Wall from RF | Fails with audible warning (bong) |

### Survey Operations
| ID | Description | Expected Result |
|---|---|---|
| S.1 (367) | Activate un-surveyed face of unknown-width wall | Selection filter rejects; impossible to activate |
| S.2 (368) | Unknown-width wall corner activation scenarios (5 sub-steps) | Various activation/dialog behaviors — see RTF for full detail |
| S.3 (394) | RF creates wall then line; press right arrow on line | Survey Line dialog re-opens for additional line |
| S.4 (398) | Try to place door/window/object on a line (RF or ribbon) | Command rejected with failure sound; no error message |
| S.5 (397) | Typical window heights state management (7 sub-steps) | Non-typical values preserved when un-checking Typical Heights |
| S.6 (443,444) | Create walls from imperial/metric template | Walls have phase "Existing"; dimensions align to faces |

---

## Release Procedure Summary (from `DevDoc/ReleaseProcedure.rtf`)

1. Commit all changes; merge to each Revit version branch
2. Update `VersionInfo/.template` (`AssemblyVersion`, `AssemblyFileVersion` to vA.B.0)
3. Run custom tool on `AssemblyInfo.tt` in Json and PKNailForRevit projects
4. Update `VersionInfo/.xml` ProductThisYear → run `ProductInfo.tt`
5. Update `Setup/.wxs` `ProductDisplayVersion` to vA.B
6. Commit, push, merge; tag `PKNailForRevit20XX_vA.B`
7. TeamCity: create VCS roots `PKNailForRevit_20XX_vA.B`; run Debug + Release build configs (automated tests must pass)
8. Run Deployment Build configs → `PKNailProForRevit20XX_vA.B.VCS.BN_setup.exe`
9. Sign setup with `signSetup.bat` using `PKCodeSigningCert.pfx` from USB drive

---

## Migration History

### Revit 2017 → Revit 2026 (completed)

The codebase was migrated from Revit 2017/.NET Framework 4.6 to Revit 2026/.NET 8 (SDK-style project).  
**Full API change reference**: see [`.github/instructions/revit-2026-migration.instructions.md`](.github/instructions/revit-2026-migration.instructions.md)

**Key changes summary**:
- `ParameterType` enum → `ForgeTypeId` / `SpecTypeId.*`
- `ElementId.IntegerValue` (int) → `ElementId.Value` (long) — affects all ElementId storage
- `Application.Create.NewGeometryOptions()` → `new Options()`
- `DetailLine.Create(view, curve)` → `doc.Create.NewDetailCurve(view, curve)`
- `FilterStringRule` dropped the `caseSensitive` 4th argument
- `view.ViewName` → `view.Name`
- `WebClient` → `HttpClient` (obsolete in .NET 8)
- `Process.Start(url)` → `Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })`
- `s_typeToParamName` dictionary added to `RevitDocument.cs` to handle the `Settings`→`DocumentSettings` class rename without breaking shared parameter lookups

**Git refs**: pre-migration baseline `11c61d9`, migration commit `966f97c`
10. FTP signed installers to `pointknown.com/esd`
11. Update changelog HTML, `latestVersion.php`, and website download links

## Revit Version Upgrade Procedure Summary (from `DevDoc/UpgradeRevitVersionProcedure.rtf`)

1. Install new Revit version on dev machine
2. Copy new `RevitAPI.dll` / `RevitAPIUI.dll` to `RevitAPI/`
3. Edit `PKNailForRevit.csproj`: replace prior Revit version number (e.g., 2016→2017)
4. Edit `PKNailForRevit.TestCore.csproj`: same replacement
5. Edit `Setup.wixproj`: same replacement
6. Edit `Setup/.wxs`: update `ProductRevitVersion`; generate new `ProductUpgradeCode` GUID
7. Global search/replace all occurrences of prior version number in solution
8. Update Debug start external program path to new Revit exe
9. Resolve all compiler errors/warnings
10. Update dotNetInstaller config (Tools/.exe + Setup/.xml)
11. Build Deploy64; verify bootstrapper
12. Open all source-controlled `.rvt`, `.rte`, `.rfa` files in new Revit; save (upgrades format)
13. Install new Revit on all build/test VMs; configure automated test keyboard shortcuts

---

## Coding Conventions

- **All measurement values use `Distance`** — never raw `double` for measurements
- **`Distance.NoValue`** sentinel (not null) represents "not set"
- **Trial transactions**: `Prep` methods always wrap in `startTrialTransaction`/`rollBackTransaction`
- **Committed transactions**: only in the non-`Prep` execution method, inside a `TransactionGroup`
- **JSON serialization**: inherit from `JsonSerializable`; use `JsonExporter`/`JsonImporter` delegates on `Distance`  
- **Extensible storage**: all document-level data stored via `PKNailParameter` helper
- **Revit internal units**: decimal feet (same as `Distance.Units.PKInternal`)
- **Unit tests**: NUnit 2.x; test class suffix `Test`; `[TestFixture]` / `[Test]` attributes
- **No direct `double` comparisons for Distance** — use `Distance.operator==` (tolerance-based)
