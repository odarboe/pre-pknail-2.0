# PKNAIL 2.0 — Master Jira Project Plan (Revit side)

> **Authoritative working document.** Agents (Claude, Copilot) and developers must read this
> before making implementation decisions on PKNAIL 2.0. When something here conflicts with
> code, the brief, or `PKNAIL_Fields.md`, **stop and ask for confirmation** — do not guess.

- **Owner:** PointKnown LLC (Jim Foster) · MVP partner: Cognixlab
- **Source brief:** `docs/PKNAil 2.0 Feasilbilty and Architecture Brief.pdf`
- **Field contract:** `docs/PKNAIL_Fields.md`
- **AI-engine sample output:** `docs/ai_engine_pknail_results.json`
- **Code root:** `code/PKNailProForRevit-2026`
- **Legacy starting CSV:** `docs/jira_import_tickets.csv` (superseded by this plan for the Revit side)

---

## 1. Goal & Scope

### What changed from PKNAIL 1.0 → 2.0

PKNAIL 1.0 captured as-built geometry **live, in the field, through a Leica Disto laser
rangefinder** feeding WinForms survey dialogs (`DrivableForm` + `Disto`). Every wall, door,
and window was entered one measurement at a time.

**PKNAIL 2.0 removes the rangefinder/device step.** The **point cloud is never imported into this
plugin.** It is uploaded into an **external AI Engine — a separate, independent repository (e.g.
Cognixlab's Python pipeline)** — which reads the point-cloud data (`.las` / GeoSLAM) and emits a
single structured file: **`ai_engine_pknail_results.json`**. **The Revit plugin's only input is that
JSON.** The plugin **imports the JSON and maps it onto the existing PKNAIL parameter model**, then
drives the **proven** placement engine (`IProjectModel` / `RevitDocument`) to generate levels, walls,
and openings — with little to no manual work.

```
PKNAIL 1.0:  Laser Measurement → Survey Dialogs → PKNAIL engine → Revit Model
PKNAIL 2.0:  Point Cloud → [external AI Engine repo] → ai_engine_pknail_results.json → PKNAIL plugin → Revit Model
             └─────────── not part of this repo ───────────┘   └──────── this repo's input ────────┘
```

> **Boundary:** This repo does **not** read `.las` / point clouds at runtime. The `.las` files under
> `code/PKNailProForRevit-2026/PointCloud/` are **sample inputs for the AI-engine repo only** (kept here
> for reference/testing); the plugin consumes the JSON the AI engine produces from them.

### In scope (this plan)

- Revit 2026 / .NET 8 production-ready plugin.
- A **JSON import + validation + mapping** layer (`Detected*` DTOs → PKNAIL params). The plugin's
  sole input is `ai_engine_pknail_results.json`; it never parses point clouds.
- A **headless execution bridge** (`AiModelImporter`) that reuses the existing placement engine
  **without dialogs or the rangefinder**.
- Levels, walls (with joins/closure + architectural-intent snapping), and openings.
- Validation, logging, human-in-the-loop review, packaging.

### Out of scope (explicitly)

- The **AI Engine itself** — a **separate, independent repository**. All point-cloud handling
  (ingestion, segmentation, plane detection, JSON generation) lives there, **not here**. We
  **consume** its `ai_engine_pknail_results.json` and **own the contract** on the Revit side.
- Full automation / mesh reconstruction / visualization product (per brief §4).
- Keeping the rangefinder workflow as the primary input. The Disto path stays in the codebase
  as **legacy/optional** but is no longer the driver. **Do not delete it without sign-off.**

### Success criteria (from brief §8, §12, §16)

- Phase-1 focus: **interior SLAM datasets, orthogonal walls, limited complexity**.
- **80%+ wall accuracy**, clean alignment & joins, **walls within ~1″ tolerance**.
- Model **usable without full rebuild**; reduce modeling time **50%+**.
- Openings are a **stretch goal** for the first accepted dataset but are planned here as a full epic.

---

## 2. Working Agreement (MANDATORY — do not skip)

This is the process the user requires for every epic. **Follow it in order. Never collapse two
epics into one commit. Never proceed to the next epic before the gate is cleared.**

For each epic, in order:

1. **Implement** the epic's stories.
2. **Write full automated end-to-end / integration tests** for the epic (NUnit + TestCore/TestRunner
   where Revit is required). Tests must be runnable and must cover the epic's acceptance criteria.
3. **Provide step-by-step manual test instructions** (the "How to verify" block in each epic) so the
   user can independently confirm the behavior in Revit.
4. **User confirms** it works.
5. **Only then `git commit`** (one focused commit per epic, message format below) before moving on.
6. **If a conflicting or unclear issue arises at any point → STOP and ask for confirmation.** Record
   the question in §6 (Open Questions) and in the agent docs.

**Commit message convention** (matches existing history, e.g. `feat(parameters): …`):

```
feat(pknail-2.0/<epic-slug>): <what the epic delivered>

- <key change 1>
- <key change 2>
Tests: <what was added / how verified>
Epic: <Epic number & name>

```

Commit only when the user has confirmed. Branch off `main` for the work; do not commit straight to
`main` without the user's say-so.

---

## 3. JSON Contract (v1) — as delivered

The current `ai_engine_pknail_results.json` is the v1 contract. **All units are meters / radians.**
The plugin's internal unit is **decimal feet** (`Distance.Units.PKInternal` = `RevitInternal`), so
**every length must be converted on import.**

### Units — the one place JSON (meters) meets the plugin (feet)

| Layer | Unit | Source of truth |
|---|---|---|
| AI-engine JSON | **meters** (lengths) + **radians** (angles) | `Meters` suffix on length fields; `CornerAngleRad` |
| Plugin internal (`Distance`) | **decimal feet** (= Revit internal) | `Distance.Units` enum — `PKInternal == RevitInternal` |
| `PKNAIL_Fields.md` | states no unit for PKNAIL params (semantic); its proposed `Detected*` DTOs use the `Meters` suffix | — |

`Distance` has **native meter support**, so conversion is a single constructor call at the import
boundary — never raw `double` math, never a hand-rolled `× 0.3048`:

```csharp
// JSON meters → internal feet (do this for every length the moment it is read)
Distance length = new Distance(detectedWall.LengthMeters, Distance.Units.MetricMeter);
// internal feet → meters if ever needed back out:
double meters = length.ValueMetricMeters;
```

**Rule (refined in implementation):** the `Detected*` DTOs and the Epic 2 `Mapped*` model both hold
**meters** (`double`). Because `Distance` physically lives in the Revit-coupled `PKNailForRevit`
assembly, the **Epic 3 bridge** (not the Revit-free Epic 2 library) is the single place that wraps each
value in `new Distance(value, Distance.Units.MetricMeter)` (plugin constant `cm_feetPerMeter = 3.2808399`).
Everything from that point on is `Distance` (feet). This keeps Epic 2 Revit-free *and* drift-free (no
re-implemented conversion constant).

```jsonc
{
  "IsScanFromInterior": true, // → CommandContext.SurveyOrientation (Flipped when true)
  "FirstWallCompassDirection": 0, // CompassDirection enum: 0=DontKnow,1=N,2=E,3=S,4=W
  "Levels": [
    {
      "Floor2FloorMeters": 2.866, // structural storey height (m)
      "Floor2CeilingElev": 2.566, // ⚠ no "Meters" suffix but IS meters (naming inconsistency)
      "HasDropCeiling": false,
      "DropCeilingHeightMeters": 0.0, // only meaningful when HasDropCeiling
      "TypicalWindowSillHeightMeters": 0.9,
      "TypicalWindowHeadHeightMeters": 2.1,
      "Walls": [
        {
          "LengthMeters": 5.05,
          "ThicknessMeters": 0.2,
          "CornerAngleRad": 4.712, // ⚠ see §5 "Corner-angle / geometry" concern
          "LevelOffset": 0.0, // elevation delta of this face above floor datum (m)
          "Openings": [
            {
              "LocationFromNearEndMeters": 4.0, // near end → CENTRE of opening
              "WidthMeters": 1.0,
              "HeightMeters": 1.4,
              "SillHeightMeters": 0.4,
              "IsLikelyDoor": false, // false ⇒ window
              "HingeSideIndex": 0, // HingeSide: 0=Left, 1=Right (door)
              "SwingDirectionIndex": 0, // SwingDirection: 0=Out, 1=In (door)
              "FacingOrientationIndex": 0, // SurveyorRelation: 0=Interior, 1=Exterior (window)
              "FamilyDefIdx": 0, // index into ProjectSurveyFamilyDefs
            },
          ],
        },
      ],
    },
  ],
}
```

The JSON already matches the **extended** `DetectedBuilding` model proposed at the bottom of
`PKNAIL_Fields.md` (it includes `IsScanFromInterior`, `Floor2FloorMeters`, drop-ceiling fields,
hinge/swing/facing indices, `FamilyDefIdx`).

> **Important — two different layers (don't confuse them):**
>
> 1. **The PKNAIL parameters themselves (right-hand column of every `PKNAIL_Fields.md` table) ALREADY
>    EXIST in the code.** They are the real PKNAIL 1.0 fields, verified:
>    `LevelDataItem.Floor2FloorElev / Floor2CeilingElev / IsDropCeiling`, `WindowHeights.SillHeight`
>    ([LevelsData.cs:154](../code/PKNailProForRevit-2026/PKNailForRevit/LevelsData.cs#L154));
>    `WallSurveyParams.SurveyorCompassDirection`, `SurveyFamilyDef` with `enum HingeSide/SwingDirection/
>    SurveyorRelation`, `enum CompassDirection`
>    ([IProjectModel.cs:171](../code/PKNailProForRevit-2026/PKNailForRevit/IProjectModel.cs#L171)).
>    **We reuse these as-is — we are not re-defining PKNAIL parameters.**
> 2. **The `Detected*` DTO classes (`DetectedBuilding/Level/Wall/Opening`) do NOT exist** — a search
>    finds them only in `PKNAIL_Fields.md`, in zero `.cs` files. They are a **new, AI-facing
>    intermediate model** that mirrors the JSON shape (flat, meters, with the proposed additions).
>    `PKNAIL_Fields.md` documents them as the **contract the AI engine must satisfy**, not as existing
>    plugin code. **Building the `Detected*` DTOs is Epic 1; mapping them onto the existing PKNAIL
>    params is Epic 2.**
>
> Why the indirection: the JSON's shape ≠ the plugin's internal parameter shape (one meters-based
> building tree vs. many `Distance`-typed per-command param objects), so a thin
> **deserialize → map** layer is required.

### Field → PKNAIL parameter mapping (authoritative)

| JSON field                            | PKNAIL target                                            | Notes                                         |
| ------------------------------------- | -------------------------------------------------------- | --------------------------------------------- |
| `IsScanFromInterior`                  | `CommandContext.SurveyOrientation`                       | `true` → `Flipped` (outside-in vs inside-out) |
| `FirstWallCompassDirection`           | `WallSurveyParams.SurveyorCompassDirection`              | first wall only; `0` = DontKnow               |
| `Level.Floor2FloorMeters`             | `LevelDataItem.Floor2FloorElev`                          | convert m→ft                                  |
| `Level.Floor2CeilingElev`             | `LevelDataItem.Floor2CeilingElev`                        | convert m→ft                                  |
| `Level.HasDropCeiling`                | `LevelDataItem.IsDropCeiling`                            |                                               |
| `Level.DropCeilingHeightMeters`       | `LevelDataItem.Floor2DropCeilingElev`                    | only when HasDropCeiling                      |
| `Level.TypicalWindowSillHeightMeters` | `LevelDataItem.TypicalWindowHeights[].SillHeight`        |                                               |
| `Level.TypicalWindowHeadHeightMeters` | `LevelDataItem.TypicalWindowHeights[].HeightWithSill`    |                                               |
| `Wall.LengthMeters`                   | `WallSurveyParams.Length`                                | convert m→ft; apply `LocationRounding`        |
| `Wall.ThicknessMeters`                | `WallSurveyParams.Width`                                 | convert m→ft; apply `WallWidthRounding`       |
| `Wall.CornerAngleRad`                 | `WallSurveyParams.CornerAngle` (`Radians`)               | normalize/snap — see §5                       |
| `Wall.LevelOffset`                    | `WallSurveyParams.LevelOffset` / `WallState.LevelOffset` | convert m→ft (always 0 in sample)             |
| `Opening.LocationFromNearEndMeters`   | `WallHostedObjectSurveyParams.Location`                  | near end → centre                             |
| `Opening.WidthMeters`                 | `SurveyFamilyDef.Width` (`SetWidth`)                     | convert m→ft                                  |
| `Opening.HeightMeters`                | `SurveyFamilyDef.Height` (`SetHeight`)                   | convert m→ft                                  |
| `Opening.SillHeightMeters`            | `SurveyFamilyDef.SillHeight` (`SetSillHeight`)           | convert m→ft                                  |
| `Opening.IsLikelyDoor`                | door vs window command routing                           | `true` → Door, `false` → Window               |
| `Opening.HingeSideIndex`              | `SurveyFamilyDef.LateralOrientation`                     | door only                                     |
| `Opening.SwingDirectionIndex`         | `SurveyFamilyDef.FacingOrientation`                      | door only                                     |
| `Opening.FacingOrientationIndex`      | `SurveyFamilyDef.FacingOrientation`                      | window: SurveyorRelation                      |
| `Opening.FamilyDefIdx`                | `WallHostedObjectSurveyParams.SurveyFamilyDefIdx`        | needs a family registry — see §5              |

---

## Architecture Review (Senior C# Architect)

> Added 2026-06-28 after grounding every claim in the actual 2026/.NET 8 code. This section
> **refines** the plan; it does not re-open the §6 RESOLVED decisions. Where a recommendation needs
> the user's call, it is logged as a NEW open question (Q7+) in §6, not silently decided.

### A. Layering & where each layer lives (validates Epics 1–3, with corrections)

The `deserialize → DTO → map → headless-execute` layering is correct. Grounded specifics:

- **DTO + mapping layers can be 100% Revit-free.** `Distance`, `WallSurveyParams`,
  `WallHostedObjectSurveyParams`, `SurveyFamilyDef`, `LevelDataItem`, `WindowHeights`,
  `CommandContext`, `CompassDirection`, `SurveyOrientation` are all **plain types with no
  `Autodesk.Revit` using** — `Distance` only references `System.*` + `Json` ([Distance.cs:1-10]),
  `IProjectModel.cs` only `using System.*` ([IProjectModel.cs:1-5]), `LevelsData.cs` likewise. So the
  `Detected*` DTOs and the mapping output (param objects) compile without the Revit API. ✅
- **The testability seam is `IProjectModel` itself.** `RevitDocument : IProjectModel` is the *only*
  implementation ([RevitDocument.cs:13]). The headless bridge (Epic 3) must be coded against
  `IProjectModel`, not `RevitDocument`, so a hand-rolled fake can capture the `SurveyWall` /
  `SurveyWallHostedObject` call sequence with no Revit. **⚠ Correction to the plan:** *no
  `IProjectModel` mock exists today.* The current "integration tests" (`TestRunner`) drive a real
  `Revit.exe` via **AutoIt `.au3` UI scripts** and diff an exported `RevitModelState`
  ([TestFixture.cs:18, 50-68]; `FunctionalTests.cs` is T4-generated `Test_####`), and they still point
  at **`Revit 2017`** ([TestFixture.cs:18]). Epic 0/3 must *build* the in-process `IProjectModel` fake;
  it is not a free seam. See new story **3.5** and **Q9**.
- **Project structure for the new code.** A new **`PKNail.AiImport` class library targeting plain
  `net8.0`** (like `Json.csproj`, which is `net8.0` with no WinForms/WPF — [Json.csproj:5]) is feasible
  and **preferred** over a folder inside `PKNailForRevit`, because `PKNailForRevit.csproj` is
  `net8.0-windows` + `UseWindowsForms` + `UseWPF` ([PKNailForRevit.csproj:5-7]) and references the
  Revit API. Putting the DTO+mapping in a `net8.0` library *mechanically guarantees* the "no Revit, no
  WinForms" rule and lets a modern `net8.0` test project consume it. The headless **bridge** (`AiModelImporter`)
  *does* touch Revit, so it lives in `PKNailForRevit` (or a thin `PKNail.AiImport.Revit`). See **Q8**.

### B. Deserialization: recommend System.Text.Json for the DTOs (revises story 1.2)

- The `Json/` project is **LitJson** (`JsonMapper`, `JsonReader`, `JsonWriter`, `Lexer`,
  `JsonData` — [Json/JsonMapper.cs et al.]). It is used for PKNAIL's *internal* persistence via the
  `JsonSerializable` base ([IProjectModel.cs:68]) and custom `ExporterFunc`/`ImporterFunc` hooks on
  value types (`Distance.JsonExporter/JsonImporter` [Distance.cs:49-53], `SurveyOrientation`
  [IProjectModel.cs:636-639], `SurveyDirection` [IProjectModel.cs:559-562]). Those hooks exist so
  internal objects round-trip through *their own* shape — they are **not** the AI-engine JSON shape.
- **`PKNailForRevit.csproj` already references `Newtonsoft.Json 13.0.3`** ([PKNailForRevit.csproj:50]).
  So a modern JSON parser is **already on the build graph** — the "adds a dependency surface" objection
  is largely moot. `System.Text.Json` ships in the `net8.0` BLT with **zero added package**.
- **Recommendation:** use **`System.Text.Json`** for the `Detected*` DTOs (flat POCOs, meters/radians,
  no `Distance`/Revit types at the DTO boundary). Reserve **LitJson for internal `JsonSerializable`
  persistence only** — do not couple the AI contract to the persistence serializer. This keeps the DTO
  library plain `net8.0` and trivially unit-testable. Logged as **Q7** (recommended default = STJ).
  *Tolerant parsing* (story 1.4, the `Floor2CeilingElev` missing-suffix case) is a `[JsonPropertyName]`
  attribute in STJ — clean.

### C. Headless execution correctness (refines Epic 3)

- **Transaction strategy is already solved by the existing command harness.** Every ribbon command runs
  through `executeInTxn(...)` which: `InitializeTransaction` → `StartTransaction` (opens the
  `TransactionGroup`) → run the delegate → `CommitTransaction` (`TransactionGroup.Assimilate()`) →
  `autoSave` ([PKNailForRevitApp.cs:736-829]; group lifecycle in [RevitDocument.cs:62-146]). The import
  command should be a new `ExecuteCmd` delegate dispatched through **the same `executeInTxn` overload**
  ([PKNailForRevitApp.cs:736]) — *not* a bespoke transaction path. The whole import becomes **one
  assimilated TransactionGroup** (one Revit undo entry), which is exactly the desired semantics.
- **The dialog is NOT where the business logic lives — the engine `*Prep` methods are.** Confirmed:
  `SurveyWallPrep` computes `IsFirstWall`, `IsCreateWall`, `IsSurveyRequired`, `ConnectedLevelIdx`,
  `CornerAngleList`, `MaxLengthList`, default `Width`, `Height`, `LevelOffset`, and close-wall geometry
  ([RevitDocument.cs:340-495]); `constructSurveyWallHostedObjectData` computes `HostWallHeight/Length`
  and typical-window flags. The dialogs mostly *display Prep output and collect the user's measured
  number*. **Implication:** the headless bridge must still call `SurveyWallPrep` /
  `SurveyWallHostedObjectPrep` to populate the derived fields, then overwrite only the
  user-measured fields from the mapping layer, then call `SurveyWall` / `SurveyWallHostedObject`. This
  is the Prep→(map instead of dialog)→Execute pattern and it reuses all the geometry math. **The mapping
  layer (Epic 2) is Revit-free; the *param population from Prep* is part of the bridge (Epic 3), which
  is allowed to touch Revit.** Story 2.x should not try to reproduce `CornerAngleList`/`MaxLengthList`
  — those come from Prep. (See new story **3.6**.)
- **`*Prep` runs inside a *trial* transaction it rolls back** (`startTrialTransaction("discardChanges")`
  → `rollBackTransaction()` in the `finally` [RevitDocument.cs:350, 488-492]). So calling Prep is safe
  and non-persistent; the actual writes happen in `SurveyWall`/`SurveyWallHostedObject` which open their
  **own** inner `Transaction`s inside the surrounding group ([RevitDocument.cs:511, 541, 573]).
- **UI-thread / ExternalEvent.** The import command is a normal `IExternalCommand` and already runs on
  Revit's UI/API thread, so **no `ExternalEvent` is needed** — `ExternalEvent` is only used to marshal
  the *rangefinder keypad* callbacks ([PKNailForRevitApp.cs:858-861, 958]). The `DrivableForm`/`Disto`
  machinery is untouched: the import path instantiates **no dialog** and pushes **no** `IMeasurementProvider`/
  `IDialogNavigationProvider` context, so the Disto code never runs. ✅
- **Failure suppression pitfall.** `*Prep` uses a `failuresPreprocessor` that **`.Set()`s
  `m_txnErrorSignal`** on transaction errors ([RevitDocument.cs:3329, 491]); a background
  `transactionMonitorThreadWorker` just plays an error sound ([PKNailForRevitApp.cs:1450-1457]). In a
  long headless import this would beep per failure with no actionable record. **Epic 7 must install an
  import-specific `IFailuresPreprocessor`** that *collects* failures into the import report instead of
  only signalling. Note `CustomFailures` only defines the one "unconverted walls" warning
  ([CustomFailures.cs:24-29]); new warning definitions (skipped wall/opening) belong in Epic 7.
- **Auto-save interaction.** `autoSave` fires after the group commits and, on `NoPathSet`/`ReadOnlyFile`,
  **pops a blocking `TaskDialog`** ([PKNailForRevitApp.cs:664, 1365-1428]). For an unsaved
  metric-template session (the documented manual-test path, Epic 3 "How to verify"), the first import
  could surprise the user with a save prompt. Decide whether import suppresses auto-save until the
  project has a path. Logged as **Q11**.

### D. Reusing the engine vs bypassing it (confirms Q1 blocker, Epic 5)

- **The engine reconstructs absolute geometry from the length+angle chain — it does not take XY.** The
  first wall is built at the origin from `(SurveyorCompassDirection → azimuth) + Length`:
  `new G2D.Vector(new G2D.Point(), firstWallAz, wsParams.Length.ValueRevitInternal)`
  ([RevitDocument.cs:541-572]); the compass→azimuth switch and the `Flipped`→`+π` flip are at
  [RevitDocument.cs:544-561]. Subsequent walls chain off the activated reference wall's far corner via
  `CornerAngle` ([RevitDocument.cs:418-422, 449-452]). **So the engine *can* close an orthogonal loop
  from length+angle alone — IF the corner angles are correct and the convention is fixed.** This is
  exactly why **Q1 is a contract problem, not an engine problem**: the engine is ready; the v1 JSON's
  mostly-zero angles aren't. Appendix A's "preferred: emit XY" is still the more robust ask (the plugin
  could compute angles itself), but the **fallback (correct angles) is directly consumable** by the
  existing chain logic with *no new geometry code*. Architect's note added to Q1 in §6.
- **Headless feasibility confirmed:** `SurveyWall`/`SurveyWallHostedObject` take fully-specified params
  and require no user input ([RevitDocument.cs:498, 1148]); the dialog only gates via
  `IsSurveyRequired`/`IsCreateWall` and `RecordUsage` ([PKNailForRevitApp.cs:341-377, 439-466]). The
  bridge replicates that control flow without the WinForms layer.

### E. Persistence note (corrects story 4.1 wording)

Story 4.1 says levels persist "to extensible storage via `PKNailParameter`." In code, `PKNailParameter`
sets **Revit shared/project parameters** via `Element.LookupParameter` ([PKNailParameter.cs:103-128]),
not `ExtensibleStorage`. `LevelsData`/`WallState` round-trip through the **LitJson `JsonSerializable`**
mechanism. Keep the term accurate in story 4.1 to avoid an implementer chasing a non-existent schema.

### F. Toolchain debt that blocks the test gate (new infra work — Epic 9)

- `PKNailForRevit.UnitTests.csproj` and `TestCore`/`TestRunner` are **legacy `v4.6` MSBuild projects**
  ([PKNailForRevit.UnitTests.csproj:13]) referencing a vendored NUnit and a `2017` Revit/AutoIt harness
  ([PKNailForRevit.TestCore.csproj:113-114; TestFixture.cs:18]). The plan's "no-Revit DTO unit tests"
  need a **modern `net8.0` test project** (SDK-style, `Microsoft.NET.Test.Sdk` + `NUnit` +
  `NUnit3TestAdapter`) referencing the new `PKNail.AiImport` library. This is real setup, not free —
  pulled into new **Epic 9 (Test & CI Infrastructure)** so it doesn't silently expand Epic 0.

### G. API readiness — does the engine expose what headless import needs? (YES — verified)

**Verdict: yes.** `IProjectModel` ([IProjectModel.cs:7-62](../code/PKNailProForRevit-2026/PKNailForRevit/IProjectModel.cs#L7))
is a deliberately **dialog-independent seam** — the dialogs are one driver and the test runner is another;
`AiModelImporter` is just a **third driver** of the same interface. Every Phase-1 goal is reachable
headlessly:

| Goal | API | Headless? | Evidence |
| --- | --- | --- | --- |
| Levels | `LevelsData { get; set; }` | ✅ | `LevelsDlg` only does `proj.LevelsData = dlg.Data` ([PKNailForRevitApp.cs:264](../code/PKNailProForRevit-2026/PKNailForRevit/PKNailForRevitApp.cs#L264)) |
| Walls | `SurveyWallPrep` → `SurveyWall` | ✅ | Prep sets `IsFirstWall` from model state; next wall computed at **far end of previous wall**, geometry-driven not selection-driven ([RevitDocument.cs:579-625](../code/PKNailProForRevit-2026/PKNailForRevit/RevitDocument.cs#L579)) |
| Openings | `SurveyWallHostedObjectPrep(cat)` → `SurveyWallHostedObject` | ✅ | Same Prep→execute pattern ([PKNailForRevitApp.cs:439-453](../code/PKNailProForRevit-2026/PKNailForRevit/PKNailForRevitApp.cs#L439)); family defs via `whoParams.ProjectSurveyFamilyDefs` (resolves Q3) |
| Atomic import + rollback | `InitializeTransaction`/`SetTransactionName`/`Start`/`Commit`/`RollBackTransaction` | ✅ | Explicit txn control on the interface → one undo entry (Q12) |
| Project metadata | `DocumentSettings { get; set; }` | ✅ | name/rounding set programmatically (Epic 10) |
| Save | `Save(out SaveError)` | ✅ | — |
| Verification | `GetModelState(out state)` + `ModelState.Compare` | ✅ | Epic 7 accuracy + Epic 9 fake-driven tests |
| Lines (Phase-2) | `SurveyLinePrep` → `SurveyLine` | ✅ (out of scope) | present for roadmap |

**Three gaps — all already handled in the plan, none is a missing capability:**
1. **Coordinate system is not headless** — `SetCoordinateSystemFromWall()` ([IProjectModel.cs:18](../code/PKNailProForRevit-2026/PKNailForRevit/IProjectModel.cs#L18)) reads an interactively-picked wall. → **Epic 10 / Q13**: default CS on import, user runs *Set Coord Sys* after.
2. **Metering is in the command layer, not the engine** — `RecordUsage` is called in `surveyWall*Command_Finish` ([PKNailForRevitApp.cs:363, 453, 538](../code/PKNailProForRevit-2026/PKNailForRevit/PKNailForRevitApp.cs#L363)), **not** inside `SurveyWall`. → `AiModelImporter` (a command-layer driver) must call `RecordUsage` **per element** (**Q4**).
3. **Wall order/direction must match Prep's continuation logic** (it finds the next wall by geometry). → Feeding walls in correct sequence/direction depends on correct corner data → ties back to the **Q1 blocker** (data quality), not an API gap. Validate in the Epic 3/5 spike.

> **Implication:** there is **no plugin-side architectural blocker**. The only true external dependency is
> **Q1** (wall-geometry data quality from the AI engine), which gates Epic 5 only.

---

## 4. Epic Roadmap (overview)

**Execution order (CONFIRMED 2026-06-28):** Epic 0 → **Epic 9 (foundation)** → Epic 1 → Epic 2 → Epic 3
→ Epic 4 → Epic 5 → Epic 6 → Epic 7 → Epic 10 → Epic 8. Epic numbers below are stable IDs (not the run
order). Epic 9 was pulled to the front because the "tests-before-commit" gate has no fast path until the
net8.0 test project + in-process `IProjectModel` fake exist.

| Run | #   | Epic                                          | Outcome                                                                   | Gate   |
| --- | --- | --------------------------------------------- | ------------------------------------------------------------------------- | ------ |
| 1   | 0   | Baseline, Contract Lock & Setup               | Build green, JSON contract frozen, discrepancies logged                   | commit |
| 2   | 9   | **Test & CI Infrastructure (FOUNDATION — do first)** | Modern net8.0 test project, in-process `IProjectModel` fake, CI wiring | commit |
| 3   | 1   | JSON Contract & `Detected*` Domain Model      | Deserialize sample JSON into validated C# DTOs                            | commit |
| 4   | 2   | Mapping Layer (Detected\* → PKNAIL params)    | Pure mapping + unit conversion + intent snapping, unit-tested             | commit |
| 5   | 3   | Headless Execution Bridge (`AiModelImporter`) | Dialog-free, rangefinder-free JSON-driven placement path + ribbon command | commit |
| 6   | 4   | Levels Generation                             | Levels created from `DetectedLevel`                                       | commit |
| 7   | 5   | Wall Generation (polyline + joins)            | Closed orthogonal walls within ~1″, clean joins (**Q1-gated**)            | commit |
| 8   | 6   | Openings (doors/windows)                      | Hosted openings placed with correct family/dims/orientation               | commit |
| 9   | 7   | Validation, Logging & Human-in-Loop           | Confidence flags, halt/skip, import report, accuracy metrics              | commit |
| 10  | 10  | Coordinate System & Project Origin            | Import origin/orientation handling; `Set Coord Sys` interaction defined   | commit |
| 11  | 8   | Packaging & Handoff                           | Installer, docs, sample I/O, limitations & roadmap                        | commit |
| 12  | 11a | **Revit-integration CI lane** (hardening)     | Integration suite + plugin build run in CI; path filters fixed (**release-blocking**) | commit |
| 13  | 11b | **Testable placement geometry** (hardening)   | Absolute-wall activation geometry extracted + unit-tested (**release-blocking**)      | commit |
| 14  | 11d | Nullable + analyzer baseline (hardening)       | Bridge null-annotated; ~1,400-warning noise baselined                     | commit |
| 15  | 11c | Transaction scope + RevitDocument decomposition | RAII transaction scope; AI-import surface isolated from the God class    | commit |
| 16  | 11e | Interface segregation + opening-path consolidation | Segregated import interface; opening-path strategy documented          | commit |
| 17  | 12a | **Opening position fix** (Bug 1)              | Paired windows/doors land at the JSON centers (±1 mm); pairs stay adjacent | commit |
| 18  | 12b | Opening data validation (Bugs 1+2)            | Overlap + missing-door-orientation warnings in the import report          | commit |
| 19  | 12c | Door type & orientation (Bug 2)               | Sliding / swing-in / hinge honored; family chosen by type, not index 0 (**Q16-gated**) | commit |
| 20  | 12d | Fallback-path fidelity (Bugs 1+2)             | Stock-template openings get correct size + hinge/swing (or auto-load PKNAIL families) | commit |
| 21  | 12e | Face/interior side on free-direction v2 walls | Swing/facing correct regardless of wall drawing direction                 | commit |
| 22  | 13  | **Direct Build import** (v2/v2.1 files)        | AI walls/openings drawn natively, no survey engine; nothing dropped; contract v2.1 to the AI team | commit |

Each epic below has: **Goal · Stories · Deliverables · Automated tests · How to verify (manual) ·
Acceptance criteria · Commit gate.**

> **Post-2.0 sequencing (architect review 2026-07-23).** The 2.0 import *design* is accepted; **Epic 11
> (Production Hardening)** closes the production-readiness gaps. Run order: **11a → 11b** (both
> release-blocking) **→ 11d → 11c → 11e** (incremental, may land post-release). The same working
> agreement applies — implement, test, manual-verify, confirm, then one focused commit per sub-epic.

> ### Delivery status (as of 2026-07-23)
>
> The end-to-end AI-JSON import path is **built, tested, and verified in live Revit 2026**
> (`new_final_combined_pknail.json` → 9 walls + 3 windows, 0 failures). Test suites: **44 unit +
> 32 integration**, all green.
>
> | Epic | Status | Notes |
> | --- | --- | --- |
> | 0 Baseline | ✅ committed | |
> | 9 Test/CI foundation | ✅ committed | net8.0 lib + NUnit + in-process `IProjectModel` fake |
> | 1 `Detected*` model | ✅ committed | System.Text.Json DTOs + validation |
> | 2 Mapping | ✅ committed | `Mapped*` + intent snapping |
> | 3 Bridge | ✅ committed | `AiModelImporter` + ribbon + .NET 8 runtime hardening |
> | 4 Levels | ✅ done | in the bridge (merge-into-existing-levels); verified in Revit |
> | 5 Walls | ✅ done | **v2 absolute endpoints** resolved the Q1 blocker; direct placement |
> | 6 Openings | ✅ committed | hosted via the **1.0 engine flow** (`ActivateWallById` → `SurveyWallHostedObject`, hinge/swing/facing + family-def); direct-hosting fallback when no PKNAIL family loaded (R4). Verified in Revit. |
> | 7 Validation/logging | ✅ committed | warnings collected into the report; duplicate Type Mark fixed. **Deferred: 7.3 visual highlight/zoom, 7.4 accuracy-metrics harness.** |
> | 10 Coord system/origin | ✅ committed | deterministic re-based origin (documented in report) + default project name; Set Coord Sys stays a manual step (Q13) |
> | 8 Packaging | ✅ committed | manifest updated for 2.0 (Revit 2026, new upgrade code, ships `PKNail.AiImport.dll`) + user/dev docs. **MSI build + code-signing + clean-VM install remain PointKnown release steps** (no WiX toolset in the dev env). |
> | 11 Production Hardening | 📋 planned | Senior-architect review 2026-07-23. **11a (Revit CI) + 11b (activation-geometry tests) are release-blocking**; 11c–11e are incremental. |
> | 12 Opening bug fixes | 📋 planned (2026-10-03) | Bug 1 double windows misaligned; Bug 2 all doors use the default type. Root causes + plan in **Epic 12**. Repro data: commit `83b6afd` (September AI-engine JSONs). |
> | 13 Direct Build | 🔧 built + tested, **awaiting manual Revit confirmation** (2026-10-13) | v2/v2.1 files bypass the survey engine; 137 unit + 71 integration tests green. First Revit run found the door-size dialog problem; fixed in 13.8, re-test pending. Supersedes 12a/12b/12d for v2 files (see Epic 13). Contract v2.1 sent to the AI team (Q16). |
> | 13b Revit-check fixes | 🔧 built + tested, **awaiting manual Revit confirmation** (2026-11-06) | Family chosen from `OpeningType` / `FamilyDefIdx` (Q20), hinge convention flipped (Q19), no duplicate PKNail parameters, parameters bound before any command. 157 unit + 72 integration tests green. |
>
> Epics 4/5/6 were delivered as part of the v2-contract and bridge commits (the real data required
> absolute placement) rather than as separate commits. The audit gaps #1–#5 (opening-engine reuse,
> re-import guard, metering, out-of-bounds, WallState tagging) landed in the openings commit.

> **Sequencing (CONFIRMED).** **Epic 9 runs first** (after the Epic 0 baseline) — the no-Revit test
> project + in-process `IProjectModel` fake are prerequisites for honoring the test gate on every later
> epic. The **Q1 wall-geometry blocker gates only Epic 5 (and the wall-loop portion of Epic 7)**; Epics
> 0, 9, 1, 2, 3, 4, 6 (logic), and 10 are **independent of Q1** and proceed while Q1 is open with the
> AI-engine team. Treat **Epic 5 as the long-pole** that unblocks the Revit-dependent halves of Epics 6/7.

---

## EPIC 0 — Baseline, Contract Lock & Setup

**Goal:** Prove the current Revit 2026/.NET 8 codebase builds and tests pass; freeze the v1 JSON
contract; record every known discrepancy so later epics don't re-litigate them.

**Stories**

- 0.1 Verify solution builds under `net8.0-windows`, x64; run existing NUnit suites (Distance,
  Geometry2D, Disto, DrivableForm, PKLicense, Json).
- 0.2 Freeze `ai_engine_pknail_results.json` as **contract v1**; copy into a test-fixtures folder
  (e.g. `PKNailForRevit.UnitTests/Fixtures/ai_engine_pknail_results.json`).
- 0.3 Add the field-mapping table (§3) and discrepancy log (§5) to repo docs; wire pointers into
  README / CLAUDE.md / copilot-instructions / agent file.
- 0.4 Create the working branch and confirm the commit-gate process with the user.

**Automated tests**

- CI/local: `dotnet build` succeeds; existing unit tests green (no regressions).

**How to verify (manual)**

1. Open `PKNailForRevit.sln`; build Release x64 → 0 errors.
2. Run the NUnit test projects → all existing tests pass.
3. Confirm `docs/PKNAIL_2.0_JiraPlan.md` (this file), `PKNAIL_Fields.md`, and the frozen JSON
   fixture are present and referenced from README + agent docs.

**Acceptance criteria:** build green, baseline tests pass, contract + discrepancies documented.
**Commit:** `chore(pknail-2.0/baseline): freeze JSON contract v1 and document the 2.0 plan`.

---

## EPIC 1 — JSON Contract & `Detected*` Domain Model

**Goal:** Turn `ai_engine_pknail_results.json` into strongly-typed, validated C# objects.

**Stories**

- 1.1 Create DTOs mirroring the JSON exactly: `DetectedBuilding`, `DetectedLevel`, `DetectedWall`,
  `DetectedOpening` (raw meters/radians, no Revit dependency). **Architect recommendation (see §A + Q8):
  put them in a new `PKNail.AiImport` class library targeting plain `net8.0`** (modelled on
  `Json.csproj` — [Json.csproj:5]), NOT a folder inside the `net8.0-windows`/WinForms `PKNailForRevit`
  project; the separate library *mechanically enforces* the Revit-free rule and is consumable by a
  modern test project (Epic 9). Add a **`SchemaVersion`/`ContractVersion`** field to `DetectedBuilding`
  (see Q10) — the v1 JSON has none.
- 1.2 Deserialize the AI-engine JSON. **Architect recommendation (see §B + Q7): use `System.Text.Json`
  for the `Detected*` DTOs** (flat POCOs; `Newtonsoft.Json 13.0.3` is already on the graph and STJ adds
  no package — [PKNailForRevit.csproj:50]; the `Json/` LitJson lib stays reserved for internal
  `JsonSerializable` persistence — [IProjectModel.cs:68], [Distance.cs:49-53]). Confirm Q7 before coding.
- 1.3 Schema validation: required fields present, numeric ranges sane (positive lengths, angle in
  `[0, 2π)`, sill ≥ 0), enum indices in range. Validation produces a structured list of errors/warnings
  (no exceptions for "soft" issues).
- 1.4 Tolerant parsing of the `Floor2CeilingElev` naming inconsistency (no "Meters" suffix).

**Automated tests** (`PKNailForRevit.UnitTests`, no Revit needed)

- Deserialize the frozen fixture → assert level count = 1, wall count = 9, one opening on wall #3
  (`LengthMeters ≈ 5.25`), opening `WidthMeters = 1.0`, `IsLikelyDoor = false`.
- Validation flags: a doctored fixture with a negative length / out-of-range enum produces the
  expected error entries.
- Round-trip: serialize DTO back and re-read → structurally equal.

**How to verify (manual)**

1. Run the new `AiImportDeserializeTest` fixture → all green.
2. Temporarily corrupt a copy of the JSON (negative width) → test asserts a validation error, not a crash.

**Acceptance criteria:** sample JSON deserializes losslessly; validation catches malformed input;
zero Revit dependency in the DTO layer.
**Commit:** `feat(pknail-2.0/json-model): add Detected* DTOs + schema validation`.

---

## EPIC 2 — Mapping Layer (Detected\* → normalized `Mapped*` model) ✅ DONE & VERIFIED (2026-06-28)

**Goal:** Pure, Revit-free **normalization** from `Detected*` DTOs to a `Mapped*` model: resolve
semantics and snap corner angles to architectural intent. **Refined boundary (per Q8):** because
`Distance` lives in the Revit-coupled assembly, this layer keeps lengths in **meters** and does **not**
produce `WallSurveyParams`/`LevelDataItem`; the meters→`Distance` conversion + `DocumentSettings`
rounding + PKNAIL-param population move to the **Epic 3 bridge**. This keeps Epic 2 Revit-free and drift-free.

**Stories (as built, in `PKNail.AiImport/Mapping/`)**

- 2.1 ✅ `MappedBuilding/Level/Wall/Opening` records + Revit-free enums (`SurveyOrientationKind`,
  `CompassDirectionKind`, `OpeningKind`, `HingeSideKind`, `SwingDirectionKind`, `FacingRelationKind`)
  that mirror the plugin enums **by integer value** (trivial cast in Epic 3). Lengths stay in meters.
- 2.2 ✅ Orientation: `IsScanFromInterior` → `Flipped`/`NotFlipped`; `FirstWallCompassDirection` →
  `CompassDirectionKind` (DontKnow→Undefined).
- 2.3 ✅ **Architectural-intent angle snapping** (`AiModelMapper.SnapCornerAngle`): normalize to
  `[0,2π)`, snap to nearest 90° multiple within `MappingOptions.OrthogonalSnapToleranceRad` (default 5°);
  floating-point noise (`2.22e-16`) collapses to 0. First wall's angle is dropped to 0 (ignored, Q2).
- 2.4 ✅ Openings: door vs window via `IsLikelyDoor` → `OpeningKind`; hinge/swing/facing indices → enums;
  `FamilyDefIdx` passed through.
- 2.5 ✅ **Length rounding & meters→`Distance` are explicitly NOT here** — deferred to Epic 3 (they need
  `DocumentSettings` + `Distance`, both Revit-side). Engine-derived lists (`CornerAngleList`,
  `MaxLengthList`, `IsCloseWall`) are produced by `SurveyWallPrep` at execute time, not here.

**Automated tests (13, pure unit tests, no Revit)**

- Frozen contract → `Flipped`, compass `Undefined`, 1 level, 9 ordered walls, first wall flagged.
- First-wall `4.712` → 0; `1.5707963` → exactly π/2; `2.22e-16` → 0 (`CornerAngleSnapped == true`).
- 89° snaps to 90°; 45° does **not** snap; `-90°` normalizes to 270°.
- Opening on wall #3 → `Window`, dims preserved in meters; exterior/interior facing from index.
- Synthetic door → `Door` with hinge=Right, swing=In.

**How to verify (manual)**

```
dotnet test PKNail.AiImport.Tests/PKNail.AiImport.Tests.csproj
```
→ all tests pass (31 total across Epics 9a/1/2).

**Acceptance criteria:** deterministic, Revit-free normalization; semantics resolved; corner angles
snapped to intent; lengths preserved in meters (conversion deferred to Epic 3). **(Met.)**
**Commit:** `feat(pknail-2.0/mapping): Detected* → normalized Mapped model + intent angle snapping`.

---

## EPIC 3 — Headless Execution Bridge (`AiModelImporter`)

**Goal:** Drive the existing `IProjectModel` placement engine **without any WinForms dialog and
without the rangefinder**, plus expose a ribbon entry point to import the AI-engine **JSON** file.
(Named `AiModelImporter`, not `PointCloudImporter` — the plugin imports the JSON, never the point cloud.)

**Stories**

- 3.1 `AiModelImporter`: takes a `DetectedBuilding` + mapped params and drives the engine via the
  **`IProjectModel` interface** (not the concrete `RevitDocument` — keeps the bridge testable, §A).
  Reuse the existing **committed-`TransactionGroup`** lifecycle: dispatch the importer as a new
  `ExecuteCmd` delegate through the existing **`executeInTxn` harness** ([PKNailForRevitApp.cs:736-829],
  group lifecycle [RevitDocument.cs:62-146]) so the whole import is **one assimilated group = one undo
  entry**. **No** `DrivableForm`, **no** `Disto`, **no** `ExternalEvent`, **no** point-cloud parsing.
- 3.2 New ribbon command **"Import AI Model (JSON)"** (`ImportAiModelCommand_Txn` + an `IExternalCommand`
  class `ImportAiModelCommand` registered in `populateRibbon` next to `Capture` —
  [PKNailForRevitApp.cs:1204-1241, 1532-1539]): file picker → deserialize (Epic 1) → validate → map
  (Epic 2) → execute (3.1/3.6) inside one group.
- 3.3 **Licensing/metering — RESOLVED (Q4): per element.** Call `m_license.RecordUsage(...)` before each
  placed wall/opening exactly as the 1.0 finish methods do ([PKNailForRevitApp.cs:363, 453, 538]); if
  the balance is exhausted mid-import, **stop and surface the partial result in the import report**
  (Epic 7), don't crash. Debug builds keep `BYPASS_LICENSE_CHECK` ([PKNailForRevit.csproj:23]).
- 3.4 Idempotency / re-import — RESOLVED (Q5): **warn + require an empty PKNAIL context** (don't silently
  append). Detect existing PKNAIL elements before importing.
- 3.5 **Define partial-import rollback semantics (architect-added):** decide whether a mid-import failure
  rolls the *whole* group back (atomic, clean re-run) or commits the elements placed so far (resumable).
  Recommended default = **atomic rollback** to keep re-import simple and idempotent. Logged as **Q12**.
- 3.6 **Prep→map→execute per element (architect-added):** for each wall/opening, call the engine's
  `SurveyWallPrep` / `SurveyWallHostedObjectPrep` to populate engine-derived fields
  (`CornerAngleList`, `MaxLengthList`, default `Width`/`Height`, `IsFirstWall`/`IsCreateWall`,
  `HostWallHeight/Length`, typical-window flags — [RevitDocument.cs:340-495]), then overwrite only the
  AI-measured fields from the Epic-2 `Mapped*` output — **this is where the meters→`Distance` conversion
  happens** (`new Distance(mapped.LengthMeters, Distance.Units.MetricMeter)` etc.) plus `DocumentSettings`
  rounding (`Distance.Round`) and the trivial `Mapped*`-enum → plugin-enum cast — then call `SurveyWall` /
  `SurveyWallHostedObject` ([RevitDocument.cs:498, 1148]). This mirrors the dialog flow with the mapping
  layer substituted for the WinForms dialog (the dialogs mostly display Prep output — §C).
- 3.7 **Auto-save / unsaved-project interaction (architect-added):** after group commit, `autoSave` may
  pop a blocking save `TaskDialog` on an unsaved/new project ([PKNailForRevitApp.cs:664, 1365-1428]).
  Decide whether import suppresses auto-save until the project has a path (**Q11**).

**Automated tests** (uses the **in-process `IProjectModel` fake built in Epic 9** — note: this fake does
not exist yet; the legacy `TestRunner` drives a real `Revit.exe` via AutoIt and is **not** a mock —
[TestFixture.cs:18, 50-68])

- With the fake `IProjectModel`, importing the sample calls `SurveyWallPrep`+`SurveyWall` 9 times and
  `SurveyWallHostedObjectPrep`+`SurveyWallHostedObject` once, in order, with the mapped params —
  verified without launching Revit.
- No dialog types are instantiated and no `IMeasurementProvider`/`IDialogNavigationProvider` context is
  pushed during import (assert via the fake/seam).

**How to verify (manual, in Revit)**

1. Launch Revit 2026 with the add-in (Debug, license bypassed).
2. New project from the metric template; click **Import AI Model (JSON)**; pick
   `docs/ai_engine_pknail_results.json`.
3. Confirm the command runs to completion with **no survey dialogs and no rangefinder prompts**, and
   reports how many levels/walls/openings it processed.

**Acceptance criteria:** end-to-end import path executes headlessly through the existing engine; ribbon
command present; metering behavior agreed and documented.
**Commit:** `feat(pknail-2.0/bridge): headless AiModelImporter + Import AI Model ribbon command`.

---

## EPIC 4 — Levels Generation

**Goal:** Create Revit levels and PKNAIL level data from `DetectedLevel`.

**Stories**

- 4.1 Generate levels (Floor2Floor stack → Project elevations), set Floor2Ceiling, drop ceiling, and
  typical window heights into `LevelsData`. **Persistence (architect correction):** `LevelsData`
  round-trips via the LitJson `JsonSerializable` mechanism ([LevelsData.cs:7]); `PKNailParameter`
  writes **Revit shared/project parameters** via `LookupParameter` ([PKNailParameter.cs:103-128]) — it
  is *not* `ExtensibleStorage`. Use the existing persistence path; don't invent a new schema.
- 4.2 Single-level happy path first (the sample), then verify multi-level arrays don't break.
- 4.3 **Floor2Floor ↔ ProjectElev interaction (architect-added).** Set `Floor2FloorElev` per level from
  the imported `Floor2FloorMeters`, then let the existing `RecalculateProjectElevations()` derive
  `ProjectElev` from the reference level down/up ([LevelsData.cs:78-132]); do NOT also hand-set
  `ProjectElev` (the recalc would overwrite or conflict). Note `RecalculateFloor2FloorElevations()`
  ([LevelsData.cs:64-76]) is the *inverse* and must not be called after import, or it would clobber the
  imported storey heights. The single-level sample has `Floor2FloorElev = NoValue` for level 0 by design
  ([LevelsData.cs:75]) — assert the importer does not treat that as an error.

**Automated tests** (TestCore/TestRunner with `RevitModelState`)

- After import of the sample, exactly **one** level exists with Floor2Ceiling ≈ 2.566 m (in feet) and
  typical window sill/head = 0.9 / 2.1 m.
- `RevitModelState.Compare` matches the expected level snapshot.

**How to verify (manual)**

1. Import sample JSON; open **Capture Levels** → the level shows the imported Floor→Floor, Floor→Ceiling,
   drop-ceiling = none, and typical window heights 0.9 / 2.1 m.

**Acceptance criteria:** levels match the JSON; values readable in the Levels dialog.
**Commit:** `feat(pknail-2.0/levels): generate levels from DetectedLevel`.

---

## EPIC 5 — Wall Generation (polyline walk + joins)

**Goal:** Generate clean, joined, orthogonal walls from the ordered wall sequence.

> **⚠ Hard dependency on §5 "Corner-angle / geometry" concern.** The v1 JSON encodes walls as
> `(LengthMeters, CornerAngleRad)` only — **no endpoint coordinates** — and most angles are `0`
> with one `90°` and one `270°`, which **cannot** reconstruct a closed orthogonal loop on its own.
> **This epic cannot be finished until the geometry representation is resolved with the user.**

**Stories**

- 5.1 Walk the wall sequence, reconstructing absolute corner points from start + lengths + (resolved)
  angles, mirroring how the legacy survey flow chained walls from corners.
- 5.2 Place walls via the engine; resolve corner **joins** and **close the loop** within tolerance.
- 5.3 Orthogonal snap + thickness normalization (architectural intent).
- 5.4 Confidence/skip: if a wall can't be reconstructed unambiguously, **flag and skip** rather than
  guess (brief core principle "halt/skip rather than force guesses").

**Automated tests** (TestCore/TestRunner)

- Sample import yields the expected wall count; adjacent walls are **joined**; the polyline **closes**;
  each wall face within **~1″ (25 mm)** of the intended position.
- Orthogonal walls report 90° joins; near-zero angles collapse to collinear/handled per the agreed rule.

**How to verify (manual)**

1. Import sample JSON; in plan view confirm the walls form a clean closed room, joins are clean (no
   gaps/overlaps), walls read as **Existing** phase, thickness uniform (0.2 m).
2. Dimension a couple of walls → within ~1″ of expected lengths (5.05, 6.0, 5.25, 8.35 m …).

**Acceptance criteria:** **80%+ wall accuracy**, clean joins, closed loop, usable without rebuild.
**Commit:** `feat(pknail-2.0/walls): polyline wall generation with joins + orthogonal snap`.

---

## EPIC 6 — Openings (Doors/Windows)

**Goal:** Place hosted openings on the correct walls with correct family, dimensions, and orientation.

**Stories**

- 6.1 Family registry: resolve `FamilyDefIdx` to a real `SurveyFamilyDef` / Revit family type. Define a
  default door family and default window family; **flag** that the AI engine hardcodes index 0.
- 6.2 Place via `SurveyWallHostedObject`: location (near-end → centre), width/height/sill, door vs
  window routing (`IsLikelyDoor`), hinge/swing (door), facing (window).
- 6.3 Reject impossible hosts (e.g. opening longer than wall) → flag/skip.

**Automated tests** (TestCore/TestRunner)

- Sample import places **1 window** on wall #3 at 4.0 m from near end, width 1.0 m, height 1.4 m,
  sill 0.4 m; hosted on the correct wall element.
- A door fixture (synthetic) routes to the door family with hinge/swing applied.

**How to verify (manual)**

1. Import sample JSON; confirm a single window appears on the correct wall, centred 4.0 m from the
   near end, with the right rough-opening size and sill height.

**Acceptance criteria:** openings hosted on correct walls with correct dims/orientation/family.
**Commit:** `feat(pknail-2.0/openings): hosted door/window placement from DetectedOpening`.

---

## EPIC 7 — Validation, Logging & Human-in-the-Loop

**Goal:** Make the import trustworthy and reviewable (brief core principles: validate in Revit, visual
debugging, human-in-loop first).

**Stories**

- 7.1 Import report dialog/log: counts, snapped values, skipped items, all flagged uncertainties.
- 7.2 Explicit "halt/skip rather than guess" enforcement across levels/walls/openings.
- 7.3 Visual debugging aid (e.g. highlight/zoom skipped or low-confidence elements).
- 7.4 Accuracy metrics harness: compare generated model vs a manual reference model (offsets, joins).
- 7.5 **Import-specific `IFailuresPreprocessor` (architect-added).** Today the trial-txn preprocessor
  only `.Set()`s `m_txnErrorSignal`, which a monitor thread turns into an error *beep*
  ([RevitDocument.cs:3329, 491]; [PKNailForRevitApp.cs:1450-1457]) — useless for a batch import. Install
  a preprocessor that **collects** Revit warnings/failures into the import report (and decides
  resolve/rollback per the Q12 policy). Register any new skip warnings alongside the existing
  `CustomFailures.UnconvertedWallsWarningId` ([CustomFailures.cs:24-29]).

**Automated tests**

- Import of a deliberately ambiguous fixture produces a report listing the skipped items (no silent
  drops, no crashes).
- Accuracy harness computes per-wall offset on the sample and asserts ≤ ~1″.

**How to verify (manual)**

1. Import sample → review the report; numbers match what's in the model.
2. Import a broken fixture → report clearly lists what was skipped and why; model still opens cleanly.

**Acceptance criteria:** no silent failures; reviewer can see exactly what happened and what to fix.
**Commit:** `feat(pknail-2.0/validation): import report, halt/skip, accuracy metrics`.

---

## EPIC 8 — Packaging & Handoff

**Goal:** Production-ready delivery.

**Stories**

- 8.1 WiX installer / build configs updated for the 2.0 feature set; signed setup per Release procedure.
- 8.2 User + developer docs: how to run the import, JSON contract reference, sample I/O datasets.
- 8.3 Known limitations + Phase-2 roadmap (exterior scans, non-orthogonal walls, multi-level, lines,
  generic hosted objects, ML-assisted segmentation).

**Automated tests**

- Full regression: all NUnit + integration suites green; clean install runs the sample end-to-end.

**How to verify (manual)**

1. Install from the signed setup on a clean Revit 2026 VM; run the sample import; confirm the model.

**Acceptance criteria:** installable, documented, sample reproducible; limitations/roadmap written.
**Commit:** `chore(pknail-2.0/release): package 2.0 import pipeline + handoff docs`.

---

## EPIC 9 — Test & CI Infrastructure (architect-added; **run first**)

**Goal:** Provide the *no-Revit* test seam the tests-before-commit gate depends on.

> **✅ Build provisioning RESOLVED (2026-06-28).** The plugin originally failed to build because two
> **local reference folders were missing from this checkout**: `RevitAPI/` (Revit 2026 `RevitAPI.dll` /
> `RevitAPIUI.dll` / `RevitAddInUtility.dll`) and `Tools/SoftwareKey/PLUSManaged.dll` (SoftwareKey
> licensing SDK). Both were sourced locally (from the sibling `…\NAIL_2\PKNailProForRevit-2017` working
> copy) and dropped into the expected paths under `code/PKNailProForRevit-2026/`. **These are local-only
> vendor DLLs — git-ignored, never committed.** With them in place, **`dotnet build
> PKNailForRevit/PKNailForRevit.csproj` succeeds (0 errors)** — this satisfies the deferred **Epic 0.1
> "build green"** for the main plugin. **Consequence:** the full plugin (and the Epic 9b in-process fake
> that references it) **can be built and tested in this environment** — 9b is no longer blocked.
>
> **New developer setup (add to `DevDoc`):** before first build, populate `code/PKNailProForRevit-2026/RevitAPI/`
> from `C:\Program Files\Autodesk\Revit 2026\` (RevitAPI.dll, RevitAPIUI.dll, RevitAddInUtility.dll) and place
> the licensing `PLUSManaged.dll` in `code/PKNailProForRevit-2026/Tools/SoftwareKey/`.
>
> **Why a separate `PKNail.AiImport` library is still worthwhile (not because Revit is unavailable):** the
> DTO + mapping logic is pure (JSON → `Distance` values) and gets **millisecond unit tests** with no Revit
> load; the separate net8.0 project just *enforces* that purity. The real plugin still builds and the
> import still runs through the live Revit API — this is a layering choice, not a workaround.

### Epic 9a — Revit-free unit-test foundation ✅ DONE & VERIFIED (2026-06-28)

- 9a.1 ✅ New SDK-style **`net8.0` Revit-free library `PKNail.AiImport`** (`Nullable enable`, implicit
  usings, `TreatWarningsAsErrors`) — home of the `Detected*` DTOs (Epic 1) + mapping (Epic 2). No
  reference to `PKNailForRevit`/Revit (mechanically enforces Q8).
- 9a.2 ✅ New **`net8.0` NUnit test project `PKNail.AiImport.Tests`** (`Microsoft.NET.Test.Sdk` +
  `NUnit 4` + `NUnit3TestAdapter`) referencing the library. **3 smoke tests pass under `dotnet test`.**
- 9a.3 ✅ **Frozen JSON contract v1** linked as a copied test fixture
  (`docs/ai_engine_pknail_results.json` → `Fixtures/…`, single source of truth) for Epic 1+ tests.
- 9a.4 ✅ Both projects added to `PKNailForRevit.sln`; `bin/obj` confirmed git-ignored.
- 9a.5 ✅ **CI**: `.github/workflows/ci-aiimport.yml` builds + tests the Revit-free projects on
  `windows-latest` (no Revit needed); uploads `.trx` results.

### Epic 9b — In-process `IProjectModel` fake + integration lane (do before Epic 3; now UNBLOCKED)

- 9b.1 ✅ Provision the build: `RevitAPI/` + `Tools/SoftwareKey/PLUSManaged.dll` in place; `PKNailForRevit`
  builds (0 errors) — deferred Epic 0.1 "build green" satisfied. **Still TODO: write the `DevDoc` setup note.**
- 9b.2 Build the hand-rolled **in-process `IProjectModel` fake** (`RecordingProjectModel`) that records
  the ordered call sequence (`SurveyWallPrep`/`SurveyWall`/`SurveyWallHostedObject*`/`LevelsData` set)
  and the params passed — the seam Epic 3 tests assume. **Architecture choice (flag for the spike):**
  either reference the plugin assembly directly, or first extract the Revit-free contract
  (`IProjectModel` + POCO param types) into a shared library so the fake compiles without Revit.
- 9b.3 A net8.0(-windows) integration test project hosting the fake-driven Epic 3 tests.
- 9b.4 Add a **self-hosted (Revit) CI lane** for 9b + the legacy AutoIt suite; repoint/quarantine the
  `TestRunner` constants hardcoded to **Revit 2017** ([TestFixture.cs:18]).

**Automated tests (9a):** `dotnet test PKNail.AiImport.Tests` → 3/3 green; CI green on a clean checkout
with no Revit installed.
**Automated tests (9b):** fake-driven Epic 3 tests run green under `dotnet test` on a provisioned machine.

**How to verify (manual, 9a):** from `code/PKNailProForRevit-2026`, run
`dotnet test PKNail.AiImport.Tests/PKNail.AiImport.Tests.csproj` on any machine **without Revit** → all
tests pass.

**Acceptance criteria:** 9a — no-Revit test path exists, wired into CI, ready for Epics 1–2 (**met**).
9b — fake exists and Epic 3 tests run, once the plugin build is provisioned.
**Commit (9a):** `chore(pknail-2.0/test-infra): net8.0 PKNail.AiImport library + NUnit project + CI`.
**Commit (9b):** `chore(pknail-2.0/test-infra): in-process IProjectModel fake + integration lane`.

---

## EPIC 10 — Coordinate System & Project Origin (architect-added)

**Goal:** Define, deterministically, where the imported building lands and how it is oriented — the v1
JSON carries **no georeference/origin/project metadata** (§5 item 7).

**Stories**

- 10.1 Origin & orientation: the engine builds the **first wall at the world origin**
  (`new G2D.Point()`) with azimuth from `SurveyorCompassDirection` and the `Flipped` inside/outside flip
  ([RevitDocument.cs:541-572]). Document that the import inherits this; decide whether to expose an
  optional offset/rotation.
- 10.2 `Set Coord Sys` interaction: the interactive `SetCoordinateSystemFromWall` calls
  `uidoc.Selection.PickObject` ([RevitDocument.cs:173-202]) — **user-interactive, unusable headlessly.**
  Decide: (a) run import in the default coordinate system and let the user run `Set Coord Sys`
  afterward, or (b) auto-set the coordinate system from the first imported wall. Recommended default =
  **(a)** for Phase 1. Logged as **Q13**.
- 10.3 Project metadata defaults: project name from the JSON file name; unit system default (metric per
  test S.6) — coordinate with §5 item 7 and request these fields in contract v2.

**Automated tests:** importing the sample twice yields byte-identical geometry (deterministic origin);
first-wall azimuth matches `FirstWallCompassDirection` + `IsScanFromInterior`.

**How to verify (manual):** after import the room sits at a predictable, documented location/orientation;
running `Set Coord Sys` realigns as expected.

**Acceptance criteria:** import placement is deterministic and documented; `Set Coord Sys` behavior
defined.
**Commit:** `feat(pknail-2.0/coordsys): deterministic origin + Set Coord Sys interaction`.

---

## EPIC 11 — Production Hardening (Senior C# Architect review, 2026-07-23)

**Goal:** Close the production-readiness gaps found in the post-2.0 architecture review so the AI-import
pipeline is **regression-protected and maintainable**, not merely "manually verified once." The 2.0
import *design* is accepted (clean layering, testable seam, faithful engine reuse); Epic 11 is the set of
**conditions** to call the codebase production-ready. Verdict: *architecturally sound at the 2.0 layer;
not yet production-hardened at the plugin/infra level.*

**Evidence baseline (as reviewed):** `RevitDocument.cs` is **7,497 lines** (single implementation of a
**41-member** `IProjectModel`); CI ([.github/workflows/ci-aiimport.yml](../.github/workflows/ci-aiimport.yml))
builds/tests **only** the 44 Revit-free unit tests and its `paths:` filters **exclude `PKNailForRevit/**`**;
the 32 integration tests + plugin build never run in CI; the absolute-wall **activation geometry**
(`ActivateWallById` in `RevitDocument.cs`) is verified **only by manual Revit clicks**; `PKNailForRevit`
has `<Nullable>disable</Nullable>` and ~**1,400** analyzer warnings; two opening paths now coexist
(engine primary + `CreateOpeningOnWall` fallback).

**Run order:** **11a → 11b** (release-blocking) **→ 11d → 11c → 11e** (incremental). Each sub-epic is one
focused commit under the standard gate.

---

### Epic 11a — Revit-integration CI lane (review #1 → R11) · release-blocking

**Goal:** Run the 32 in-process integration tests **and** the `PKNailForRevit` plugin build in CI so
bridge/engine regressions are caught automatically, not just by a human importing in Revit.

**Stories**
- 11a.1 Add a **self-hosted (Revit 2026) Actions lane** (or a documented, required local gate on a
  provisioned dev machine) that restores the vendored `RevitAPI/` + `Tools/SoftwareKey/PLUSManaged.dll`,
  builds `PKNailForRevit` (Debug), and runs `PKNail.AiImport.IntegrationTests`; upload the `.trx`.
- 11a.2 **Fix `ci-aiimport.yml` `paths:` filters** so changes under `PKNailForRevit/**` and
  `PKNail.AiImport.IntegrationTests/**` trigger the relevant lane — today only `PKNail.AiImport/**`
  changes trigger CI, so a bridge/engine regression is invisible to CI.
- 11a.3 Require both lanes green in branch protection before merge to `main`.

**Automated tests:** the 32-test integration suite runs in CI on every PR touching the plugin or bridge.
**How to verify (manual):** open a PR that edits `PKNailForRevit/AiImport/AiModelImporter.cs`; confirm the
integration lane runs and blocks the PR when red.
**Acceptance criteria:** no Revit-side change can merge without the integration suite passing.
**Commit:** `ci(pknail-2.0/hardening): Revit integration lane + corrected path filters`.

---

### Epic 11b — Testable placement geometry (review #2 → R12) · release-blocking

**Goal:** Put the **riskiest new logic** under automated test: the absolute-wall activation geometry
(front-face selection, `IsScanFromInterior` → `SurveyOrientation`, near-end → `SurveyDirection`, face
point) that currently has **no** automated coverage — the integration fake asserts call sequence/params,
not geometric correctness.

**Stories**
- 11b.1 Extract the pure geometry of `ActivateWallById` into a **Revit-free helper** in `PKNail.AiImport`
  (input: wall start/end points + thickness + `IsScanFromInterior` + near-end flag; output: chosen face
  point / orientation-kind / direction-kind) — no `Autodesk.Revit` types.
- 11b.2 Keep `ActivateWallById` a **thin wrapper** that reads Revit wall geometry, calls the helper, and
  writes the `refElementState`. No geometry *decisions* remain in the Revit method.
- 11b.3 **Unit-test the convention** (from the engine investigation): near corner == wall start; interior
  scan → interior face; each `(SO, dir)` combination; degenerate / near-collinear / reversed-endpoint cases.

**Automated tests:** net8.0 unit tests (no Revit) covering the convention + edge cases; a single Revit
smoke test still confirms one door hinge/side end-to-end.
**How to verify (manual):** the helper's tests run in the fast unit lane; place one door in Revit and
confirm hinge/side match the JSON.
**Acceptance criteria:** the activation convention is guarded by unit tests; the Revit wrapper contains no
untested geometry decisions.
**Commit:** `refactor(pknail-2.0/hardening): extract + unit-test absolute-wall activation geometry`.

---

### Epic 11c — Transaction scope + RevitDocument decomposition (review #3/#1 → R13)

**Goal:** Make transaction lifetime **RAII-safe** and begin carving the AI-import surface out of the
7,497-line `RevitDocument` God class — incrementally, never big-bang.

**Stories**
- 11c.1 Introduce an **`IDisposable` transaction scope** (Start → Commit, rollback-on-dispose) wrapping
  `m_txn`/`m_txnGrp`, so a dangling inner transaction is structurally impossible — this replaces the
  manual `AbortPendingTransaction` workaround. Migrate the **AI-import methods first**; leave legacy
  call-sites untouched until they are test-covered.
- 11c.2 Extract the AI-import additions (`CreateWallAbsolute`, `ActivateWallById`, `EnsureDefault3DView`,
  `HasPKNailWalls`, `AbortPendingTransaction`, import-warning capture) into a focused
  `RevitDocument.AiImport.cs` **partial** (or an `AiImportDocumentService`), so the new surface is
  isolated and reviewable.
- 11c.3 (Stretch) File tickets to extract the next 1–2 cohesive services (wall placement, opening
  hosting); do **not** rewrite the God class in one pass.

**Automated tests:** the 32 integration tests stay green through the refactor (behavior-preserving); add a
fake-driven test that a scope disposed without commit rolls back.
**How to verify (manual):** re-run the live import — identical result, no behavior change.
**Acceptance criteria:** transaction lifetime is scope-managed for the import path; AI-import code is
isolated from the bulk of `RevitDocument`.
**Commit:** `refactor(pknail-2.0/hardening): IDisposable transaction scope + isolate AI-import surface`.

---

### Epic 11d — Nullable + analyzer baseline on PKNailForRevit (review #4/#10 → R14)

**Goal:** Give the bridge code null-safety and surface real defects hidden in ~1,400 analyzer warnings.

**Stories**
- 11d.1 Enable **`<Nullable>annotations</Nullable>`** on `PKNailForRevit` (annotations-only first to avoid
  a warning flood), fully annotate the AI-import files, and set `PKNail.AiImport.IntegrationTests` to
  `enable` (it is currently `disable`).
- 11d.2 Add an **`.editorconfig` / analyzer ruleset** that suppresses the known-benign **CA1416**
  (WinForms-is-Windows-only) with justification and treats **new** warnings on the AI-import files as
  errors (mirroring `PKNail.AiImport`'s existing `TreatWarningsAsErrors`).
- 11d.3 Triage the remaining top warning categories into follow-up tickets — do not fix all 1,400.

**Automated tests:** build stays green; the Revit-free lib already builds warnings-as-errors.
**How to verify (manual):** the build shows a small, clean warning set for the AI-import files; CA1416 no
longer masks real warnings.
**Acceptance criteria:** the bridge is null-annotated; analyzer noise is baselined; new warnings on 2.0
code fail the build.
**Commit:** `chore(pknail-2.0/hardening): nullable annotations + analyzer baseline on the bridge`.

---

### Epic 11e — Interface segregation + opening-path consolidation (review #5/#11 → R15)

**Goal:** Shrink the 41-member `IProjectModel` surface the import depends on, and document/consolidate the
two opening paths.

**Stories**
- 11e.1 Define a focused import-facing interface (e.g. `IAiImportTarget`) exposing only what
  `AiModelImporter` uses (transaction control, `LevelsData`, `CreateWallAbsolute`, `ActivateWallById`,
  `SurveyWallHostedObject*`, warning capture, `EnsureDefault3DView`). `RevitDocument` implements it; the
  `RecordingProjectModel` fake shrinks to match.
- 11e.2 Document the **two opening paths** (engine-hosting primary + `CreateOpeningOnWall` fallback) with a
  decision note; decide whether to keep the fallback long-term.
- 11e.3 (Optional, closes R4) **Auto-load the shipped PKNAIL window/door families** when none are present,
  so the engine path (correct AI orientation) works on **any** template and the fallback's fidelity gap
  disappears. Resolve family paths for both dev (`RevitFiles/`) and installed (`Libraries/`) layouts.

**Automated tests:** importer tests retarget to the smaller interface; fallback + engine-path tests
unchanged; a test asserts family auto-load routes openings to the engine (not the fallback).
**How to verify (manual):** import on a stock metric template with family auto-load → windows show correct
AI orientation with **no** fallback warning.
**Acceptance criteria:** the import depends on a segregated interface; opening-path strategy is documented;
(optional) family auto-load removes the stock-template fidelity gap.
**Commit:** `refactor(pknail-2.0/hardening): segregate import interface + consolidate opening paths`.

---

## EPIC 12 — Opening Bug Fixes (double windows, door types) — 2026-10-03

**Goal:** Fix the two opening defects reported against the September AI-engine output (committed in
`83b6afd`: `docs/Sept17/`, `docs/September15/`, `docs/September_jsons/`, `docs/final_combined_pknail_v5.json`).

- **Bug 1:** double windows (two openings side by side on one wall) don't land next to each other.
- **Bug 2:** every door uses the same default door type, even when the real door is sliding or swings inward.

**Two opening paths (from Epic 6/11e).** Which one runs decides which root causes apply. The import
report shows the fallback warning *"hosted with a default family — no PKNAIL survey family loaded"*
whenever the fallback ran.

- **Engine path:** used when PKNAIL door/window families are loaded. It runs `ActivateWallById` →
  `SurveyWallHostedObjectPrep` → `SurveyWallHostedObject`.
- **Fallback path:** used on stock templates. It runs `CreateOpeningOnWall`.

### Root-cause analysis (verified in code)

**Bug 1 — positions**

| # | Path | Cause | Evidence | Effect on a pair |
| -- | ---- | ----- | -------- | ---------------- |
| 1A | Engine | **The AI sends the center; the engine expects the near edge.** v2 `LocationFromStartMeters` is the opening **centre** ([schema v2:82](ai_engine_pknail_schema_v2.md)). The 1.0 engine treats `Location` as the **near edge** and adds `width/2` unless the family has *Family Measure Location Centered = Yes* ([RevitDocument.cs:5776-5779]). The importer passes the center straight through ([AiModelImporter.cs:457]). | `September15` wall[1]: 0.65 m + 2.151 m windows meant to touch end up **~0.70 m apart**. `v5` wall[1]: an equal-width pair stays 0.149 m apart but the whole pair moves **+0.344 m**. | Each opening moves by its **own** half-width, so pairs of unequal width split apart. |
| 1B | Fallback | **The size is never applied.** Width and height are only set if the family exposes them as *instance* parameters ([RevitDocument.AiImport.cs:228-237]); standard Revit families use *type* parameters. Every opening gets the first type's default size. | All windows have the same default width. | Centers are correct, but default widths make pairs overlap or leave gaps. |
| 1C | Engine | **Measured from a different point.** The engine measures from the **wall-face corner**; the JSON measures from the **centerline start**. Where a perpendicular wall joins, the face corner is trimmed by about half that wall's thickness (6–10 cm). | Same offset for every opening on the wall. | The pair moves together; secondary, but it counts against the ~1″ accuracy goal. |
| 1D | Data | **The AI's own openings overlap.** | `September15` wall[0]: a door and a window overlap by 0.33 m. Wall[1]: two windows overlap by 5 cm. | The plugin can't fix bad input; it should warn. |

**Bug 2 — door type**

| # | Path | Cause | Evidence |
| -- | ---- | ----- | -------- |
| 2A | Contract | **The JSON can't say which door type.** There's no door-type field, and `FamilyDefIdx` is always 0 (§5 item 4, Q3). Every door resolves to the first PKNAIL door family in Family Order ([AiModelImporter.cs:445-455]). The engine already supports `LateralOrientationType.Sliding`; it just never receives it. | All files: `FamilyDefIdx=0`, `Kind="Door"` only. |
| 2B | Data | **Orientation fields are missing, so doors default to Left / swing Out.** `Sept17`, `September15` and `v5` have no `HingeSideIndex` / `SwingDirectionIndex`. The mapper defaults them to 0 ([AiModelMapper.cs:100-101]), so a door that swings inward imports swinging **out**. The DTO uses `int` rather than `int?`, so a missing field can't be detected. | Only some `September_jsons` models emit `SwingDirectionIndex=1`. |
| 2C | Fallback | **The fallback ignores type and orientation.** It places the first door type in the document for every door and never applies hinge or swing ([RevitDocument.AiImport.cs:195-237]). | Same door everywhere on stock templates. |
| 2D | Engine (unconfirmed) | **The interior side may be wrong on some walls.** v2 walls run in arbitrary directions, but `ActivateWallById` gets the same interior/exterior face choice for every wall (`IsScanFromInterior`). On walls drawn the "other" way round, swing and facing may come out mirrored. Opposite parallel walls in `Sept17` (wall[1] vs wall[3]) both run west→east. | Needs a Revit check (12e). |

### Run order

**12a → 12b → 12d → 12e**, plus **12c once Q16 is answered**.

- 12a and 12b have no outside dependencies.
- 12c needs the AI-engine contract change (Q16).
- 12d may be replaced by 11e.3 (auto-load the PKNAIL families), depending on Q14.

Each sub-epic is one focused commit under the standard §2 gate.

---

### Epic 12a — Opening position fix (Bug 1A + 1C)

**Stories**
- 12a.1 **Convert center to near edge in the bridge.** In `AiModelImporter.PlaceOpening`, after the
  family def is chosen:
  - If the def is *not* `IsMeasureLocationCentered`, pass `hp.Location = center − width/2`.
  - Otherwise pass the center unchanged.

  This happens at the bridge boundary only; the 1.0 engine is not touched. The v1 survey-chain path
  keeps today's near-end semantics; check that against the v1 contract before changing it.
- 12a.2 **Measure from the face corner.** Extend `ActivateWallById` to return the signed distance along
  the wall from the **centerline start** to the **near face corner**. The importer subtracts it so the
  location is measured from the same point the engine uses. Put the projection math in the Revit-free
  `WallActivationGeometry` helper so it is unit-tested (follows 11b).
- 12a.3 **Fixed-width families.** When the selected family has a fixed width
  (`FixedDimensions.Width`), use the family's width for the half-width offset and record a warning
  that the AI width was not applied.

**Automated tests**
- Unit: center→edge conversion for centered and non-centered defs; the face-corner projection
  (perpendicular join, no join, reversed endpoints).
- Integration (`RecordingProjectModel`): import `September15/step7_pknail_with_windows.json` and
  `final_combined_pknail_v5.json`. For every wall with two or more openings, assert that the
  reconstructed center (recorded `hp.Location` + `width/2` + corner offset) equals the JSON center
  within 1 mm, and that the gaps between neighbours are preserved.

**How to verify (manual)**
1. Import `final_combined_pknail_v5.json` on the PKNAIL template.
2. In plan view, dimension wall[1]'s two windows. The gap should be **0.149 m** and the first window's
   center **2.282 m** from the wall start.
3. Repeat with `September15/step7_pknail_with_windows.json` wall[1]. The windows should touch (the JSON
   overlaps them by 5 cm, which 12b reports).

**Acceptance criteria:** every opening center is within 1 mm of the JSON; pairs keep their JSON
spacing.

**Commit:** `fix(pknail-2.0/openings): place v2 openings at the JSON centre (centre→near-edge + face offset)`.

---

### Epic 12b — Opening data validation (Bug 1D + 2B)

**Stories**
- 12b.1 **Overlap warnings.** `AiModelValidator` warns when two openings on the same wall overlap
  (edge gap < 0), naming the wall, both indices and the overlap in mm. This is a warning, not an
  error. The policy (skip / clamp / warn only) is decided in Q17.
- 12b.2 **Make absence detectable.** Change `HingeSideIndex`, `SwingDirectionIndex` and
  `FacingOrientationIndex` to `int?` in `DetectedOpening`. The mapper still defaults to 0.
- 12b.3 **Missing-orientation warnings.** When a door has no hinge or swing value, the validator warns
  *"door orientation not provided — defaulted to Left/Out"*.
- 12b.4 Extend `ImportReport` to show these warnings next to the Revit warnings.

**Automated tests:** validator unit tests (overlap, touching, gap, and a missing swing field); a
deserialize test confirming that a missing field gives `null`; a regression run over every committed
September JSON with the expected warning counts.

**How to verify (manual):** import `September15/step7_pknail_with_windows.json`. The report lists the
wall[0] door/window overlap, the wall[1] window overlap, and the doors whose orientation defaulted.

**Acceptance criteria:** bad or missing opening data is never silent.

**Commit:** `feat(pknail-2.0/validation): warn on overlapping openings + missing door orientation`.

---

### Epic 12c — Door type & orientation (Bug 2A + 2B) · **Q16-gated**

**Stories**
- 12c.1 **Contract v2.1 (needs AI-engine team sign-off).**
  - Add an optional per-opening `OpeningType` string:
    - doors: `"Hinged"`, `"DoubleHinged"`, `"Sliding"`, `"Pocket"`, `"Bifold"`
    - windows: `"Fixed"`, `"Casement"`, `"Sliding"`, …
  - Make `HingeSideIndex` and `SwingDirectionIndex` **required for doors**.
  - Deprecate `FamilyDefIdx`. It's a document-specific index the AI can't know.
  - Update `ai_engine_pknail_schema_v2.md` and add a fixture.
- 12c.2 **Choose the family by type.** Map `OpeningType` to a family def using the PKNAIL family
  parameters the engine already reads (*Family Lateral Orientation* = `Hinged` / `Sliding`) plus a
  settings name map (for example `Sliding` → "PK Sliding Door").
  - Order: an exact name-map match, then the first def with a matching `LateralOrientationTypeVal`,
    then index 0 with a warning.
  - `FamilyDefIdx` is used only when `OpeningType` is missing (keeps v2.0 behaviour).
- 12c.3 **Report which family was used.** The import report lists the family chosen for each door,
  so a reviewer can spot defaults.
- 12c.4 Ship or confirm PKNAIL sliding and double-door families in the PKNAIL template (coordinate
  with 11e.3).

**Automated tests**
- Unit: mapper and resolver (each type; unknown type → default with a warning).
- Integration: the fake exposes several `ProjectSurveyFamilyDefs`. Assert the chosen
  `SurveyFamilyDefIdx`, and that a door with `SwingDirectionIndex=1` reaches `SetFacingOrientation(1)`.

**How to verify (manual):** import a v2.1 fixture with one hinged swing-in door and one sliding door.
In Revit, check the family/type of each and the swing side.

**Acceptance criteria:** door family and swing match the JSON; defaults are reported, never silent.

**Commit:** `feat(pknail-2.0/openings): resolve door family by OpeningType + honour swing/hinge`.

---

### Epic 12d — Fallback-path fidelity (Bug 1B + 2C)

**Decision (Q14):** either (a) implement 11e.3, auto-loading the shipped PKNAIL families so the engine
path always runs and the fallback becomes rare (**recommended**), or (b) fix the fallback in place.

**Stories (option b, or as a backstop under a)**
- 12d.1 **Apply the size.** In `CreateOpeningOnWall`, select or duplicate a family **type** whose
  Width/Height match the AI values, the way the engine's `getFamilySymbol` does
  ([RevitDocument.cs:6349]), instead of setting instance parameters.
- 12d.2 **Apply orientation.** Apply hinge and swing with `flipHand` / `flipFacing`, using the same
  rules as `wallHostedInstanceDefinition.construct` ([RevitDocument.cs:6069-6083]).
- 12d.3 Choose the door family by `OpeningType` name match when 12c has landed.

**Automated tests:** integration tests asserting the fake's `CreateOpeningOnWall` receives the
width/height/hinge/swing values. The Revit lane (11a) checks the type's width.

**How to verify (manual):** on a stock metric template, import `final_combined_pknail_v5.json`. The
windows have the JSON widths and the wall[10] doors are mirrored correctly.

**Commit:** `fix(pknail-2.0/openings): fallback hosting applies size + hinge/swing`.

---

### Epic 12e — Face/interior side on free-direction v2 walls (Bug 2D)

**Stories**
- 12e.1 **Reproduce.** In Revit, import `Sept17/step7_pknail_with_windows_v2.json` and record the
  door swing and window facing on walls drawn in opposite directions.
- 12e.2 If the bug is confirmed, **work out the interior side per wall from geometry** (the side facing
  the building / room centroid, or the side matching the AI's `FacingOrientationIndex`) rather than one
  constant for the whole import. Implement it as a Revit-free helper in `WallActivationGeometry`.

**Automated tests:** unit tests for the interior-side helper on a rectangle with walls drawn both ways.

**How to verify (manual):** all inward-swinging doors swing into the room on every wall.

**Commit:** `fix(pknail-2.0/openings): derive the interior face per wall for v2 walls`.

---

## EPIC 13 — Direct Build import (v2/v2.1 files) — 2026-10-12

**Goal:** draw the AI-engine JSON in Revit exactly as delivered — every wall and opening, as native
Revit elements — without forcing the data through the 1.0 survey engine, and without anyone editing
the JSON or deleting walls to make an import succeed.

**Why (decided with the user 2026-10-12, Q18).** The v2 engine path (`CreateWallAbsolute` →
`ActivateWallById` → `SurveyWallHostedObject`) assumed a connected survey chain and PKNAIL survey
families. The real October exports (`docs/October5/New/`) are 13–19 independent segments with
T-junctions. Their ends already meet within 1 mm and no file closes a loop. The trouble was the engine
path, not the data:

- the opening flow expects the near edge, measures from the wall face and needs PKNAIL families (Epic 12 Bugs 1A/1B/1C/2C);
- one element-level validation error blocked the whole file, and one failed wall rolled everything back;
- every wall went on level index 0 at the level ceiling height; the AI's level elevation and per-wall data were dropped.

Direct Build plans everything Revit-free, then calls plain Revit API methods. The `v3test/` Dynamo
scripts proved the approach. This brings it inside the add-in so licensing, metering, one-undo import
and the import report still apply.

**Stories (all delivered)**
- 13.1 **Contract v2.1 (additive).** Optional wall `Id`, `HeightMeters`, `InteriorSide`, `Comment`;
  opening `Id`, `OpeningType`, `Comment`; level `Name`. `LevelOffsetMeters` (the v2 key) is now read.
  `HingeSideIndex`/`SwingDirectionIndex`/`FacingOrientationIndex` are `int?` so "not provided" is
  detectable (covers 12b.2). Supported versions: 1.0, 2.0, 2.1. AI-team instructions:
  [ai_engine_pknail_schema_v2.1.md](ai_engine_pknail_schema_v2.1.md) + example JSON.
- 13.2 **Lenient validation policy.** `AiModelValidator.Validate(b, elementIssuesAreWarnings: true)`:
  a bad wall/opening becomes a "will be skipped" warning; a bad optional value is "ignored"; file-level
  problems still block.
- 13.3 **`WallNetworkCleaner`** (Revit-free): square up walls within 2° of an axis; extend/trim a loose
  end ≤ 10 cm along its own line onto a neighbour's centerline (L-corners, T-junctions); flag collinear
  overlaps as possible duplicates. Never deletes, never moves a wall sideways, reports every change.
- 13.4 **`DirectBuildPlanner`** (Revit-free): levels re-based so the lowest sits at 0 (explicit
  `ElevationMeters`, else stacked storey heights); plan re-based to the building min X/Y (Epic 10.1);
  openings re-anchored to the tidied walls, slid inside if they overhang, overlaps flagged (Q17: warn
  only); interior side from `InteriorSide`, else toward the plan centroid; labels = AI ids, else
  `W{n}` / `W{n}-D{k}` / `W{n}-N{k}`.
- 13.5 **`OpeningOrientation`** (Revit-free): windows face the exterior; door swing/hinge follow the AI
  when provided, else the family default. The two Revit family conventions are constants to confirm in
  the manual test (Q19).
- 13.6 **Revit bridge.** `IAiImportTarget.CreateWallDirect` / `CreateOpeningDirect`
  (`RevitDocument.DirectBuild.cs`) and `DirectBuildImporter`: levels matched by elevation or created
  through the 1.0 `LevelsData` mechanism (level + floor plan); walls on their centerline with Mark,
  Comments and the PKNAIL `WallState` marker; openings at their centre using a duplicated type of the
  measured size (`PKNail AI Door 760 x 1990`) from the first wall-hosted family, or a plain rectangular
  opening when no family is loaded. One transaction group, per-element metering (Q4), atomic rollback
  on a wall failure or metering veto (Q12), openings best-effort.
- 13.7 **Routing + report.** `ImportAiModelCommand` sends any file with wall coordinates to Direct
  Build; v1 files keep the survey chain. `ImportReport` gains `Mode` and `Adjustments` (`*` lines).
- 13.8 **No Revit dialogs; honest counts (fix from the 2026-10-12 Revit run).** The first manual run of
  `October5/1_ai_pknail_v2 (1).json` showed four doors (571–678 mm) that the default
  "Door-Passage-Single-Flush" family cannot be built at ("Profile sketch is empty", "Can't make type").
  Revit raised a blocking dialog and "Delete Type" had to be pressed, and the report could still count
  those doors as placed. Now every Direct Build element uses its own failure preprocessor: Revit errors
  are recorded and only that element is rolled back silently (`ClearAfterRollback`). A door/window
  falls back to the nearest existing type within 10 cm (`OpeningFallback`), else a plain opening of the
  exact size, and the report says which. Success is only counted when the element exists after commit.
  A wall Revit rejects now rolls the import back with Revit's error text instead of a dialog (Q12).

**Effect on Epic 12.** For v2/v2.1 files, 12a (centre vs near edge, face offset), 12b (overlap and
missing-orientation warnings) and 12d (fallback size/handing) are **superseded**: Direct Build places
at the centre, sizes the type, and reports both cases. 12c (choose the family by `OpeningType`) and the
door-handing part of 12e remain, now on the Direct Build path. The old v2 engine path
(`AiModelImporter.ImportAbsolute`) is kept in code and tests but no longer called by the command.

**Automated tests**
- Unit (`PKNail.AiImport.Tests`): `WallNetworkCleanerTests`, `DirectBuildPlannerTests` (every October
  export: nothing dropped, openings inside walls, re-based), `OpeningOrientationTests`, `ContractV21Tests`
  (v2.1 fields, nullable orientation, lenient policy, the AI-team example file).
- Integration (`PKNail.AiImport.IntegrationTests`): `DirectBuildImportTests` — every October export
  imports whole in one undo step with no survey-engine calls; coordinates and units; openings on the
  right wall at their centre; metering once per element; veto, wall failure and exception roll back; an
  unhostable opening is skipped while the import commits; multi-level creation with a name clash; the
  example file's ids, types and door handing reach Revit.
- `OpeningFallbackTests` (nearest-type rule), and the two `October5/1_*.json` files from the failed
  Revit run added to the October regression set. The Revit-only part of 13.8 (failure preprocessor,
  fallback chain) cannot run headless; it is covered by manual step 8.

**How to verify (manual)**
1. Close Revit, then build: `dotnet build PKNailForRevit/PKNailForRevit.csproj -c Debug`.
2. Start Revit 2026, new project (metric template). Add-Ins → PKNail Pro → Import AI Model →
   `docs/October5/New/Orleans_ai_pknail_v2.json`. Report: `AI import (Direct Build): COMMITTED`,
   walls placed = 19, openings placed = 13, failures = 0.
3. Floor plan: walls meet cleanly at corners and T-junctions; no wall missing compared with the JSON.
4. Select a window: Mark = `W1-N1` style label; its type is `PKNail AI Window <w> x <h>` matching the
   JSON size in mm. Dimension its centre from the wall start: it matches `LocationFromStartMeters`.
5. Press Undo once: the whole import disappears.
6. New project, import `docs/ai_engine_pknail_results.v2.1.example.json`. Door `W2-D1` (east wall) must
   swing **into** the room with the hinge on the **left** as seen from inside; door `W5-D1` (partition)
   swings into the west room with the hinge on the **right** as seen from the west room. Windows face
   outward. This confirms Q19.
7. Repeat step 2 with `Carlisle_manual_check_pknail_v2.json`: the report lists the W18 squaring
   (`*` line) and the W15/W18 possible duplicate (`~` line); both walls are drawn.
8. New project, import `docs/October5/1_ai_pknail_v2 (1).json`. **No Revit error dialog appears.** The
   report has a `~` line for each of W7-D1, W7-D2, W7-D3 and W10-D1 saying Revit could not build the
   door family at that size and what was placed instead (nearest type or a plain opening). Each of
   those four positions shows a door or an opening in the plan.

**Acceptance criteria:** every October export imports with 0 failures and walls/openings equal to the
JSON counts; every change appears in the report; a single Undo removes the import; Q19 confirmed.

**Commit:** `feat(pknail-2.0/direct-build): draw v2/v2.1 AI files natively + contract v2.1 for the AI team`.

---

## EPIC 13b — Fixes from the 2026-11-06 Revit check

**Source:** `BUG_SUMMARY.md` from the Revit check of `docs/October31/1_v2_ai_pknail_v2_doortypes.json`
(Revit 2026.4, PKNAIL template). All 13 openings were within 1 mm of their expected centres. Five
problems were reported; each was checked against the code and the two model dumps before fixing.

| # | Reported | Verified cause | Fix |
|---|---|---|---|
| 1 | All doors `M_Single-Flush`, all windows `M_Fixed`; the 4 sliding doors are swing doors | `pickOpeningSymbol` took the first wall-hosted type; `FamilyDefIdx` / `OpeningType` never reached Revit (Q18) | 13b.1 |
| 2 | Capture family list empty after an import | `EnsureDocumentParameters` used `bindings.Contains(def)`, which does not see the template's own (not shared) parameters of the same name, so it bound a second "PKNail Family Category" etc. `LookupParameter` then returned the empty one | 13b.2 |
| 3 | Capture / Wall fail without an import: `Parameter "PKNail Settings" is undefined` | Only the import command bound the missing parameters; the 2026 template carries 6 of the 18 names in the shared parameter file | 13b.3 |
| 4 | Hinges mirrored on the 3 hinged doors, swing sides right | `HandPointsAwayFromHinge` had the wrong value (Q19) | 13b.4 |
| 5 | Door trim overlaps wall 11 by about 25 mm | Family geometry, not placement: the door is on its measured centre, 49 mm from the wall face | No code change. May differ once the door is the PKNAIL "Single" family (13b.1) |

Not reproduced and not changed: the door offsets and empty passages seen on the other PC. Those
symptoms match the old survey path (`Cannot locate point in face wall`), so that PC is probably
running an older build.

**Stories**
- 13b.1 **Choose the family (12c.2 on the Direct Build path, Q20).** `OpeningFamilyResolver`
  (Revit-free): `OpeningType` first (same name ignoring case and spaces, then the aliases
  `Hinged` = "Single" and `DoubleHinged` = "Double", then the first hinged / sliding family), else
  `FamilyDefIdx`; an index outside the list uses the first family and says so. The list is built by the
  1.0 `ReconcileSurveyFamilyTypeDefs`, so it is the Capture list. A project without PKNAIL survey
  families keeps the first wall-hosted family and reports once if the file asked for a specific one. A
  survey family that has no hinge or no swing (sliding, pocket, opening) keeps its default handing; a
  size it cannot be built at falls back to its own nearest type, never to another family.
- 13b.2 **No duplicate parameters.** `EnsureDocumentParameters` skips a definition when a parameter of
  that name is already bound, and opens no transaction when nothing is missing. Protection for
  documents that already hold duplicates: `PKNailParameter` uses the namesake that holds a value when
  `LookupParameter` returns an empty one, and the family-category filter matches on every parameter of
  that name. Existing duplicates are not removed.
- 13b.3 **Bind before any PKNAIL command.** `executeInTxn` calls `EnsureDocumentParameters` first.
- 13b.4 **Hinge convention.** `HandPointsAwayFromHinge = false`.
- 13b.5 **Report clean-up (from the first re-run, 2026-11-06, on `Default-Multi-discipline.rte`).** That
  run showed 19 warnings for 5 facts. Revit warnings raised by an attempt that is rolled back are now
  dropped (the attempt is reported once with its error); multi-line Revit texts are one line. The
  failure line names the wall thickness, and says the family does not fit the wall when a type placed
  elsewhere in the same import fails there. This corrects 13.8: "Door-Passage-Single-Flush" does not
  fail at 571–678 mm widths, it fails in 100 mm walls. In both runs every failed door was on W7 or W10
  (100 mm), every door on a 110/115 mm wall was built, and the 530 mm type built on W4 failed on W7.
- 13b.6 **Verify the cut.** After a door/window is placed, the wall's solid is probed at the middle of
  the opening. If the family left the wall solid, the measured opening is cut behind it and the report
  says so. Added because two windows on W2 looked uncut in the 3D view of that run. **Confirmed by the
  second re-run:** the report said both W2 windows had not cut the wall, and the cut behind them fixed
  the view. They were the only two instances that needed no flip after creation, so the instance is now
  hosted after regenerating its (freshly resized) type and directly at its final height; the probe stays
  as the backstop. If the next report has no "did not cut the wall" line, that was the cause.
- 13b.7 **Load the PKNAIL families when the project has none (11e.3, Q14).** Both re-runs were made on
  Revit's `Default-Multi-discipline.rte`, which has one door family and no sliding door, so the four
  sliding doors could not be sliding and four doors on 100 mm walls became plain openings. The import
  now loads the shipped PKNAIL door families when the project has no PKNAIL door family, and likewise
  for windows (`RevitDocument.EnsureSurveyFamilies`), and tags their types with the template's
  `PKNail Family Category` / `PKNail Family Order`, so `FamilyDefIdx` means the same on any template.
  Family files are found next to or above the add-in (`RevitFiles\Imperial|Metric`) or in the installed
  library (`%ProgramData%\PointKnown\PKNail Pro For Revit <year>\Libraries\US Imperial|US Metric`),
  chosen by the project's unit system. A project that already has PKNAIL families of a kind is not
  touched. The report lists what was loaded (`*` line); if the files are missing it says so (`~` line)
  and the project's own families are used.

**Automated tests:** `OpeningFamilyResolverTests`; `OpeningOrientationTests` (the three doors of the
check file); `DirectBuildPlannerTests` and `DirectBuildImportTests` on the check file (family index,
handing and centres reach the plan and Revit). 157 unit + 72 integration tests green. 13b.2, 13b.3 and
the Revit side of 13b.1 touch the Revit API and cannot run headless; they are covered by the manual
steps.

**How to verify (manual)**
1. Close Revit, build `dotnet build PKNailForRevit/PKNailForRevit.csproj -c Debug`, start Revit 2026.
2. New project from `PKNailProForRevit.rte`. **Before importing**, click PKNail Pro → **Wall**. It must
   start instead of failing with `Parameter "PKNail Settings" is undefined`. Cancel it.
3. Import `docs/October31/1_v2_ai_pknail_v2_doortypes.json`. Walls placed = 14, openings placed = 13.
   The 4 sliding doors (walls 3, 5, 6, 7) are `PKNail Sliding`; the 3 hinged doors are `PKNail Single`;
   the windows are `PKNail Fixed`. Compare the hinged doors with `expected_plan.png`: wall 9 door hinged
   on the east jamb swinging north; wall 6 doors hinged at the jambs nearest wall 5, swinging east.
4. Activate a wall (**Wall**), then **Capture → Door**: the family list shows Single, Double, Opening,
   Sliding Glass, Sliding, Pocket.
5. Run the RevitDump add-in: the binding list has no name twice.
6. Open the project saved from the earlier check (the one with duplicate parameters): **Capture → Door**
   lists the families there too.
7. New project from a stock metric template, same import: doors are `M_Single-Flush`, windows `M_Fixed`,
   and the report has one `~` line saying the project has no PKNAIL door families (the file asks for
   door family 4; the windows ask for the default, so they add no line).
8. New project from the imperial `Default-Multi-discipline.rte`, same import: the report has a `*` line
   "Loaded the PKNAIL families Single, Double, Opening, Sliding Glass, Sliding, Pocket, Fixed, Double
   Hung". The 4 sliding doors are `PKNail Sliding`, the 3 hinged doors `PKNail Single`, the windows
   `PKNail Fixed`; no door is a plain opening and there is no "did not cut the wall" line. If a PKNAIL
   family cannot be built in a 100 mm wall either, its `~` line says so and that is the next thing to
   fix.

**Commit:** `fix(pknail-2.0/direct-build): choose the door/window family, fix hinge side + duplicate PKNail parameters`.

---

## 4b. Risk Register (Senior C# Architect)

| #  | Risk | Likelihood × Impact | Mitigation |
| -- | ---- | ------------------- | ---------- |
| R1 | **Wall geometry unreconstructable (Q1).** v1 JSON gives length+mostly-zero angles, no XY; cannot close an orthogonal loop ([ai_engine_pknail_results.json:13-87]; Appendix A). | High × High | Block Epic 5 only; pursue XY (preferred) or guaranteed-correct angles from the AI team; the engine's chain logic is ready either way ([RevitDocument.cs:541-572]). Build Epics 1–4, 6, 9, 10 in parallel. |
| R2 | **No in-process test seam exists.** Plan assumed an `IProjectModel` mock; reality is AutoIt-on-Revit-2017 ([TestFixture.cs:18, 50-80]) — the tests-before-commit gate has no fast path. | High × High | Epic 9: build the `IProjectModel` fake + `net8.0` test project *first*, before Epics 1–2 hit the gate. |
| R3 | **Revit API version drift.** Code references Revit 2017 paths in test harness ([TestFixture.cs:18]) and `2017` addin copy ([PKNailForRevit.TestCore.csproj:113]); 2026 API differences in failure handling / transactions could surface only at runtime. | Med × High | Story 9.5 repoints/scopes the harness; smoke-test the import inside Revit 2026 early in Epic 3; pin the RevitAPI HintPath ([PKNailForRevit.csproj:56-63]). |
| R4 | **Family-def availability in the target template.** `FamilyDefIdx` resolves against `ProjectSurveyFamilyDefs` ([IProjectModel.cs:276, 416-421]); a metric template (test S.6) may lack the expected default door/window families, so `getSelectedFamilyDef()` returns null → `HardException` on `SetWidth` ([IProjectModel.cs:308-314]). | Med × High | Epic 6.1: validate family registry up front; flag/skip openings with no resolvable family rather than throw; ship/require known default families. |
| R5 | **Float precision & tolerance.** Meters→feet is `× 3.2808399` ([Distance.cs:17, 35-37]); `Distance ==` uses a 0.0001 ft tolerance ([Distance.cs:20, 73]); `CornerAngleRad` arrives as `2.22e-16` ([ai_engine_pknail_results.json:70]). Naïve comparisons or un-snapped angles drift past the ~1″ goal. | Med × Med | Always go through `Distance` (never raw double past the DTO boundary); snap epsilon angles to 0/90° in Epic 2.4; assert the ~1″ (25 mm) budget in Epic 5/7 accuracy tests. |
| R6 | **License exhaustion mid-import (Q4).** Per-element `RecordUsage` ([PKNailForRevitApp.cs:363, 453, 538]) means a low-balance metered license stops placement partway. | Med × Med | Epic 3.3 + 3.5: surface partial result in the import report; decide atomic-rollback vs resumable (Q12); optionally pre-check balance ≥ element count before starting. |
| R7 | **Auto-save TaskDialog interrupts batch import.** `autoSave` pops a blocking dialog on unsaved/read-only projects ([PKNailForRevitApp.cs:664, 1365-1428]). | Med × Low | Q11: suppress auto-save during import until the project has a path. |
| R8 | **Failure noise / silent suppression.** Trial-txn preprocessor only beeps via `m_txnErrorSignal` ([RevitDocument.cs:3329]; [PKNailForRevitApp.cs:1450-1457]); batch failures are neither logged nor surfaced. | Med × Med | Epic 7.5: import-specific `IFailuresPreprocessor` that collects to the report. |
| R9 | **Contract drift without a version field.** v1 JSON has no `SchemaVersion` (§5 item 7); the inconsistent `Floor2CeilingElev` suffix ([ai_engine_pknail_results.json:7]) shows the contract is informal. | Med × Med | Q10: add `SchemaVersion` to the DTO + a version check on import; request suffix normalization in v2. |
| R10 | **Extensible-storage / parameter schema growth.** New PKNAIL params or `JsonSerializable` shape changes can break round-trip of older saved files ([PKNailParameter.cs]; [LevelsData.cs:7]). | Low × High | Version the persisted schema; add round-trip tests (Epic 1.4 + 4) before changing any serialized type. |
| R11 | **CI does not cover the Revit side.** `ci-aiimport.yml` builds/tests only the 44 Revit-free unit tests and its `paths:` exclude `PKNailForRevit/**`; the 32 integration tests + plugin build never run in CI, so a bridge/engine regression is invisible. | High × High | **Epic 11a**: self-hosted Revit lane running the integration suite + plugin build; fix path filters; require green before merge. **Release-blocking.** |
| R12 | **Riskiest new logic is untested.** `ActivateWallById` geometry (face selection, interior/exterior→SO, near-end→dir) is verified only by manual Revit clicks; the integration fake asserts call sequence, not geometric correctness. | High × High | **Epic 11b**: extract the pure geometry into a Revit-free helper and unit-test the convention + edge cases. **Release-blocking.** |
| R13 | **God class + fragile manual transactions.** `RevitDocument.cs` is 7,497 lines implementing a 41-member interface; `m_txn`/`m_txnGrp` are mutable fields, nesting throws, and `AbortPendingTransaction` is a manual patch for dangling transactions (we hit exactly that in the openings work). | Med × High | **Epic 11c**: `IDisposable` transaction scope (rollback-on-dispose); incrementally isolate the AI-import surface into a partial/service. Do not big-bang. |
| R14 | **No null-safety on the bridge + warning noise.** `PKNailForRevit` is `<Nullable>disable</Nullable>` (so `AiModelImporter` gets no null-safety) and emits ~1,400 CA1416/SYSLIB warnings that mask real ones. | Med × Med | **Epic 11d**: nullable annotations on the bridge; analyzer baseline that suppresses benign CA1416 and treats new 2.0-code warnings as errors. |
| R15 | **Fat interface + dual opening paths.** The import depends on a 41-member `IProjectModel` (ISP violation; the fake must implement all of it), and two opening paths now coexist (engine primary + `CreateOpeningOnWall` fallback), with the fallback dropping AI orientation on stock templates. | Med × Med | **Epic 11e**: segregate an `IAiImportTarget`; document the opening-path strategy; optionally auto-load PKNAIL families to remove the fallback's fidelity gap (also closes R4 on stock templates). |
| R16 | **Opening location semantics differ by layer.** The v2 JSON gives the centre from the centerline start; the 1.0 engine expects the near edge from the face corner. Wrong conversion puts each opening off by its own half-width (Bug 1). | High × Med | **Epic 12a**: convert at the bridge boundary only; a regression test on every committed fixture asserts centres within 1 mm. |
| R17 | **The contract can't express door type or required orientation.** `FamilyDefIdx` is a document-specific index the AI can't know, and missing orientation fields silently become Left/Out (Bug 2). | High × Med | **Epic 12b** makes absence visible now; **Epic 12c** adds `OpeningType` in contract v2.1 (Q16). |

---

## 5. Discrepancies & Concerns (flag early — read before Epics 1, 2, 5, 6)

These are real mismatches between the delivered JSON and what the plugin needs. **Items marked
🔴 BLOCKER must be resolved with the user before the dependent epic can complete.**

1. **🔴 Wall geometry has no coordinates (Epic 5 blocker). → SENT TO AI-ENGINE TEAM. Full write-up in
   [Appendix A](#appendix-a--blocker-shareable-write-up-for-the-ai-engine-team).** The CSV task said the
   schema would model _"Wall start/end points, width, angle"_, but the delivered JSON gives only
   `LengthMeters` + `CornerAngleRad` per wall — **no endpoints**. And the angle data is unreliable: 7 of 9
   walls have `CornerAngleRad = 0` (or `2.2e-16`), one is `90°`, one is `270°` (`4.712`). A length+angle
   chain with mostly-zero angles **cannot reconstruct a closed orthogonal room**. **Decision needed:**
   either (a) the AI engine adds absolute wall **start/end XY points** (preferred, matches the original
   schema intent), or (b) we lock down exact corner-angle semantics (turn vs interior angle, reference
   frame, first-wall rule) and the engine produces correct angles. **Recommend (a).**

2. **✅ First wall `CornerAngleRad = 4.712` (270°) — RESOLVED (Q2).** The first wall has no predecessor, so
   a corner angle is meaningless. **Decision: follow PKNAIL 1.0** — the first wall has no corner angle
   (`IsFirstWall`); the JSON's `4.712` is **ignored**, and orientation comes from `FirstWallCompassDirection`
   + `IsScanFromInterior` only.

3. **🟠 Units & naming.** All lengths are meters but `Floor2CeilingElev` lacks the `Meters` suffix that
   every other field has. Internally PKNAIL is **decimal feet**. We convert on import and parse the
   inconsistent field tolerantly — but request the AI engine standardize suffixes in v2.

4. **✅ `FamilyDefIdx` hardcoded to 0 — RESOLVED (Q3).** The engine always emits `FamilyDefIdx = 0`.
   **Decision: use PKNAIL 1.0 behavior** — `FamilyDefIdx` is the index into the project's
   `ProjectSurveyFamilyDefs` list; resolve it and pick the default family exactly as 1.0 does. (Index 0 =
   first/default family-def, which is the 1.0 default.) Still flag to the AI team that index 0 is always
   emitted, so non-default families can't yet be requested.

5. **🟠 Door-only vs window-only fields always present.** Each opening carries `HingeSideIndex`,
   `SwingDirectionIndex` **and** `FacingOrientationIndex` regardless of `IsLikelyDoor`. Confirm the
   plugin ignores door fields for windows and vice-versa.

6. **🟡 Single level / no closure flag.** Sample has one level and no explicit `IsClosed` flag for the
   wall loop. Confirm whether the wall list is always a single closed loop per level, and how multiple
   disconnected wall runs would be represented.

7. **🟡 No project metadata.** No project name, unit system, georeference/origin, **or `SchemaVersion`**
   in the JSON. Decide defaults (e.g. metric, project name from file name) or request them in v2.
   **→ Now tracked as Q10 (schema version) and Epic 10 (origin/coordinate-system & metadata defaults).**

8. **🟡 Collinear split walls.** Walls #3/#4/#5 are collinear segments (angle 0) — likely a single
   physical wall split around an opening. Decide whether to merge collinear segments or keep them split
   (keeping split is simpler for hosting openings; document the choice).

9. **🟡 Lines & generic hosted objects unmodeled.** The JSON has no `Lines` or generic wall-hosted
   objects. Out of scope for Phase 1; note for the roadmap.

10. **✅ Metering at scale — RESOLVED (Q4).** One import = many placements. **Decision: mirror PKNAIL
    1.0 exactly — call `RecordUsage` once per placed element** (each wall/door/window = 1 usage unit).
    Importing the sample (9 walls + 1 window) consumes ~10 units; a low-balance metered license can be
    exhausted **mid-import** and placement stops when the balance hits 0 (same as 1.0). Implement in Epic 3.3.

---

## 6. Open Questions — RESOLVED (2026-06-28)

| #   | Question | Blocks | **Resolution** |
| --- | --- | --- | --- |
| Q1  | Add absolute wall start/end XY points to the JSON, or fix corner-angle semantics? | Epic 5 | **OPEN — sent to AI-engine team** (Appendix A). Preferred: emit start/end XY points. Until answered, Epic 5 cannot complete. **Architect's note (do not re-open):** the engine *already* reconstructs absolute geometry from the length+angle chain — first wall at origin from compass azimuth + length ([RevitDocument.cs:541-572]), subsequent walls via `CornerAngle` ([RevitDocument.cs:418-422]). So the **fallback (correct angles) is directly consumable with no new geometry code**; XY remains the more robust ask because the plugin could then derive angles itself. Either fix unblocks Epic 5; the blocker is the *contract*, not the engine. |
| Q2  | First-wall corner angle: ignore it? | Epic 2/5 | **Follow PKNAIL 1.0** — the first wall has no corner angle in 1.0 (`IsFirstWall` ⇒ no predecessor); orientation comes from `FirstWallCompassDirection` + `IsScanFromInterior`. The JSON's `4.712` is ignored. |
| Q3  | Default door & window families + `FamilyDefIdx` map? | Epic 6 | **Use PKNAIL 1.0 behavior** — resolve `FamilyDefIdx` against `ProjectSurveyFamilyDefs` exactly as 1.0 does (the index into the project's family-def list); follow 1.0's default-family selection. |
| Q4  | Metering: per-import or per-element? | Epic 3 | **Per element (exact 1.0 behavior)** — `RecordUsage` before each placed wall/opening; import stops if balance is exhausted mid-run. |
| Q5  | Re-import behavior: append, warn, or require empty context? | Epic 3 | **Default** — warn + require an empty PKNAIL context (don't silently append). |
| Q6  | Keep the rangefinder/Disto path in the build? | all | **Yes** — keep as legacy/optional; do not delete without sign-off. |

### New open questions (architect-added) — RESOLVED 2026-06-28

| #   | Question | Blocks | **Resolution** |
| --- | --- | --- | --- |
| Q7  | Deserializer for the `Detected*` DTOs: `System.Text.Json` vs LitJson (`Json/`)? | Epic 1 | **✅ System.Text.Json.** Ships in `net8.0`, no added package; keeps the AI contract decoupled from the LitJson serializer used only for internal `JsonSerializable` persistence. |
| Q8  | Where do the DTO + mapping layers live: new `PKNail.AiImport` `net8.0` library vs folder in `PKNailForRevit`? | Epics 1–2 | **✅ New `PKNail.AiImport` `net8.0` class library** (modelled on `Json.csproj`). Mechanically enforces the Revit-free rule. The Revit-touching bridge stays in `PKNailForRevit`. |
| Q9  | Build a hand-rolled in-process `IProjectModel` fake (no mocking framework)? | Epic 3/9 tests | **✅ Yes** — `IProjectModel` is the only seam; a simple recording fake (no Moq/NSubstitute) is enough. Built in **Epic 9 (run first)**. |
| Q10 | Add a `SchemaVersion`/`ContractVersion` field and version check? | Epic 1 | **✅ Yes** — add to `DetectedBuilding`; warn on unknown versions. Request the AI engine emit it in contract v2. |
| Q11 | Suppress auto-save during headless import on an unsaved/new project? | Epic 3 | **✅ Yes** — suppress until the project has a path (auto-save can pop a blocking `TaskDialog` mid/after import — [PKNailForRevitApp.cs:664, 1365-1428]). |
| Q12 | Partial-import failure policy: atomic rollback of the whole group, or commit-so-far/resumable? | Epic 3 | **✅ Atomic rollback** of the single import `TransactionGroup` (one undo entry). Simpler + idempotent re-import (Q5). Revisit only if large imports make full re-run too costly. |
| Q13 | Coordinate system on import: default CS + user runs `Set Coord Sys` after, or auto-set from first wall? | Epic 10 | **✅ Default CS for Phase 1**, user runs `Set Coord Sys` after (interactive `PickObject` can't run headlessly). Auto-set from first wall is a Phase-2 enhancement. |

### Epic 12 open questions — OPEN (2026-10-03)

| #   | Question | Blocks | Status |
| --- | --- | --- | --- |
| Q14 | Which opening path does the reported Revit run hit: the engine path (PKNAIL families loaded) or the fallback (report shows *"hosted with a default family"*)? Fix the fallback in place, or auto-load the PKNAIL families (11e.3)? | 12d priority | **Auto-load implemented (13b.7, 2026-11-06), awaiting user confirmation in Revit.** |
| Q15 | Is a "double window" two separate windows (as the JSON sends today) or one double-window family? Should touching or overlapping same-height windows be merged? | 12a scope | **OPEN.** Plan assumes two separate windows, no merge. |
| Q16 | Approve the contract v2.1 request to the AI-engine team: `OpeningType` per opening, and `HingeSideIndex` / `SwingDirectionIndex` required for doors? | 12c | **✅ APPROVED (user, 2026-10-12)** and widened: v2.1 also drops the loop-order rule and adds wall `Id`/`HeightMeters`/`InteriorSide`/`Comment`, opening `Id`/`Comment`, level `Name`. Spec: [ai_engine_pknail_schema_v2.1.md](ai_engine_pknail_schema_v2.1.md). |
| Q17 | Policy for openings that overlap in the JSON: warn only, skip the second, or clamp? | 12b | **Implemented as warn only** on the Direct Build path (both placed, reported). Confirm. |

### Epic 13 decisions (2026-10-12)

| #   | Question | Blocks | Status |
| --- | --- | --- | --- |
| Q18 | Replace the v2 engine path with a Direct Build path (native Revit API, no survey engine) as the default for files with wall coordinates? | Epic 13 | **✅ YES (user, 2026-10-12).** v1 keeps the survey chain. Direct Build ignores `FamilyDefIdx` (deprecated) and sizes a type from the first wall-hosted family: a deliberate departure from Q3 for v2 files until 12c selects families by `OpeningType`. |
| Q19 | Revit family conventions used to apply AI door handing: the leaf swings toward `FacingOrientation`; `HandOrientation` points from hinge to latch. | Epic 13 gate | **Half answered by the 2026-11-06 Revit check.** Swing: confirmed (`DoorSwingsTowardFacing = true`). Hinge: all three hinged doors came out mirrored, so `HandPointsAwayFromHinge` is now `false` (the hand points at the hinge, which is also what the 1.0 flip rule implies). The hinge was judged by eye on `M_Single-Flush`; **re-confirm in Revit on the PKNAIL "Single" family** (Epic 13b manual step 3). |
| Q20 | Should Direct Build honour `FamilyDefIdx` after all? Q18 ignored it on purpose; the 2026-11-06 Revit check reported it as a bug (4 sliding doors built as swing doors). | Epic 13b | **Implemented as 12c.2 says, awaiting user confirmation:** `OpeningType` chooses the PKNAIL family first; `FamilyDefIdx` is used when `OpeningType` is absent or matches nothing; a project with no PKNAIL survey families keeps the Q18 behaviour (first wall-hosted family). Side effect to confirm: older v2 files (all `FamilyDefIdx = 0`) now get the first PKNAIL door/window family ("Single" / "Fixed") in a PKNAIL template instead of the stock `M_Single-Flush` / `M_Fixed`. The v2.1 note to the AI team still says "do not send `FamilyDefIdx`"; prefer `OpeningType`. |

> **Q2/Q3 action:** before coding Epics 2/6, trace the exact PKNAIL 1.0 code paths (`SurveyWallPrep`
> first-wall handling; `SurveyFamilyDefIdx` / `ProjectSurveyFamilyDefs` resolution and default family
> selection in `RevitDocument`) and mirror them. If the 1.0 behavior is ambiguous in code, **stop and
> confirm** rather than guessing.

---

## Appendix A — BLOCKER: shareable write-up for the AI-engine team

> **PKNAIL 2.0 — Wall geometry representation is insufficient to reconstruct a floor plan**
>
> **Context.** The Revit plugin consumes `ai_engine_pknail_results.json` and must place walls as real
> 2D geometry (each wall = a start point → end point line in the plan). To draw and *join* walls and
> *close* the room, the plugin needs unambiguous wall endpoints.
>
> **Problem.** In v1 each wall is encoded only as `{ LengthMeters, ThicknessMeters, CornerAngleRad,
> LevelOffset }` in an ordered list — a **relative chain** (a length plus an angle relative to the
> previous wall), with **no absolute coordinates**. Reconstruction requires "walking" the chain: place
> wall 1, turn by the next `CornerAngleRad`, draw wall 2, and so on. That only works if every corner
> angle is correct and the convention is fixed. In the sample neither holds:
> - The 9 walls have `CornerAngleRad` = `[4.712, 0, 0, 0, 0, 1.5708, ~0, 0, 0]`.
> - **Seven of nine are 0** (one is `2.22e-16`, i.e. floating-point zero). If 0 means "no turn /
>   collinear," walking the chain yields an open polyline that does **not** form a closed room.
> - **Wall 1's angle is `4.712 rad` (270°)**, but wall 1 has **no previous wall** — a corner angle there
>   is undefined/meaningless.
>
> **Two unresolved ambiguities** (even when the angle is non-zero):
> 1. **Angle semantics:** is `CornerAngleRad` the **interior angle** at the corner (PKNAIL convention:
>    π/2 = a 90° inside corner) or the **exterior/turn angle** (heading change)? Different numbers.
> 2. **Meaning of 0:** "collinear continuation" or "not computed / unknown"? The mostly-zero values look
>    like placeholders rather than measured corners.
>
> **Why this blocks us.** With no coordinates and unreliable angles, the plugin cannot deterministically
> rebuild the plan. Per our core principle ("flag/skip rather than guess"), we will not invent geometry —
> so wall generation cannot complete until this is resolved.
>
> **Preferred fix (recommended).** Emit **absolute wall geometry**: for each wall a **start point (X, Y)**
> and **end point (X, Y)** in meters, in one documented plan coordinate system (state the origin and the
> +X/+Y axis directions). The plugin then computes lengths, thickness, corner angles, joins, and
> orthogonal snapping itself, robustly. Matches the original schema intent ("Wall start/end points").
>
> **Acceptable alternative.** Keep length + angle but: (a) define `CornerAngleRad` precisely — interior
> vs. turn angle, reference direction, and that it is the angle between **this** wall and the **previous**
> wall; (b) set the **first wall's angle to an explicit null/sentinel** (not 270°); (c) guarantee the
> angles actually close the loop; (d) add an explicit **`IsClosed`** boolean for the wall loop.
>
> **Please also specify** (needed either way): (e) is `LengthMeters` the **centerline** length or a
> **face** length? (f) which wall end is the **"near end"** that `Opening.LocationFromNearEndMeters` is
> measured from, given the wall ordering? (g) confirm walls are always a single ordered loop per level
> (and how multiple disconnected runs would be represented).
>
> ---
>
> ### Empirical confirmation — live Revit 2026 import of the v1 JSON (2026-06-29)
>
> The full import pipeline is built and was run against `ai_engine_pknail_results.json` in Revit 2026.
> It **bound parameters, created/linked the level, resolved wall types, set wall heights, and began
> placing walls** — then **placed 5 walls and aborted** with:
> `XYZ..ctor: x must be a finite number` (a non-finite wall coordinate), after several Revit
> **"Highlighted walls overlap"** warnings. The import rolled back atomically (no partial model left).
> This is the predicted failure, now reproduced.
>
> **Exact engine convention (verified in code — please match it).** The engine computes each wall's
> heading as `newAzimuth = previousAzimuth + (π if surveyed right-to-left) + CornerAngleRad`
> ([RevitDocument.cs:2356-2362](../code/PKNailProForRevit-2026/PKNailForRevit/RevitDocument.cs#L2356)).
> `CornerAngleRad` is therefore the **interior angle** at the corner, and:
> - **`π/2` (90°)** = a normal **90° inside corner**.
> - **`π` (180°)** = a **straight, collinear continuation** (no corner).
> - **`0`** = the next wall folds **180° back over** the previous one ⇒ **overlapping/degenerate walls**
>   (exactly the overlaps + the eventual non-finite coordinate we hit).
>
> So the v1 sample's seven `0` angles are interpreted as fold-backs, and `270°`/`90°` don't close the
> loop — the data cannot describe the room. **Bottom line: the pipeline is proven; it needs valid
> geometry.** Preferred: emit **wall start/end XY points**. If staying with angles: use **`π` for straight
> runs, `π/2` for inside corners**, a **null first-wall angle**, an **`IsClosed`** flag, and verify the
> turn angles actually close the polygon.

---

## 7. References

- `docs/PKNAIL_Fields.md` — every PKNAIL field the AI must produce + the proposed `Detected*` additions.
- `docs/ai_engine_pknail_results.json` — frozen v1 contract.
- `docs/PKNAil 2.0 Feasilbilty and Architecture Brief.pdf` — brief (executive summary, phases, success criteria).
- `code/PKNailProForRevit-2026/.github/copilot-instructions.md` — full plugin architecture & data model.
- `docs/PKNAIL_1.0.legacy.md` — legacy 1.0 rangefinder workflow reference.
