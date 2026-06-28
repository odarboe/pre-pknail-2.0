## Point Cloud Bridge — Fields the AI Must Produce

> **Clarification (PKNAIL 2.0 boundary):** the **plugin does not import point clouds**. The point
> cloud is processed by a **separate, independent AI-engine repository**, which produces
> `ai_engine_pknail_results.json`. The Revit plugin's headless bridge — historically named
> `PointCloudImporter` below, **implemented as `AiModelImporter`** — consumes **that JSON**, not the
> `.las` file. The `.las` files under `PointCloud/` are AI-engine sample inputs kept for reference
> only. See `PKNAIL_2.0_JiraPlan.md` for the full plan and the JSON contract.

The AI engine that analyses the `.las` file must produce a `DetectedBuilding` (serialized to `ai_engine_pknail_results.json`) that covers **every field the PKNail forms collect from the rangefinder**. The headless bridge then maps that JSON onto the PKNail engine without showing any survey dialogs.

### Group 1 — Level (`LevelsDlg` / `LevelDataItem`)

| PKNail field               | `LevelDataItem` property       | AI must extract                                                         |
| -------------------------- | ------------------------------ | ----------------------------------------------------------------------- |
| Floor→Floor height         | `Floor2FloorElev`              | Distance from floor slab to floor slab above (structural storey height) |
| Floor→Ceiling height       | `Floor2CeilingElev`            | Distance from finished floor to finished ceiling                        |
| Drop ceiling present       | `IsDropCeiling`                | Whether a suspended ceiling exists below the structural ceiling         |
| Floor→Drop ceiling         | `Floor2DropCeilingElev`        | Distance from floor to suspended ceiling (only when present)            |
| Typical window sill height | `WindowHeights.SillHeight`     | Most common sill height found across all windows on this level          |
| Typical window head height | `WindowHeights.HeightWithSill` | Most common window top height found on this level                       |

**Current `DetectedLevel`** covers only `Floor2CeilingElev`. Missing: `Floor2FloorElev`, drop ceiling fields, typical window heights.

### Group 2 — First Wall only (`FirstWallDlg`)

| PKNail field                        | Property                                                                                 | AI must extract                                                |
| ----------------------------------- | ---------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| Surveyor perspective                | `CommandContext.SurveyOrientation` — `Flipped`=Outside, `NotFlipped`=Inside              | Whether the scan was taken from outside or inside the building |
| Compass direction (first wall face) | `WallSurveyParams.SurveyorCompassDirection` (`CompassDirection` enum: N/E/S/W/Undefined) | Real-world cardinal direction the first wall face points       |

`DetectedBuilding.FirstWallCompassDirection` ✅ already present. `IsScanFromInterior` (→ `SurveyOrientation.Flipped`) not yet modelled.

### Group 3 — Every Wall (`SurveyWallDlg` / `WallSurveyParams`)

| PKNail field          | Property                  | AI must extract                                                        |
| --------------------- | ------------------------- | ---------------------------------------------------------------------- |
| Length                | `Length`                  | Wall face length from near-end to far-end                              |
| Width (thickness)     | `Width`                   | Wall thickness near-face to far-face                                   |
| Height                | `Height`                  | Floor-to-ceiling height for this wall                                  |
| Level offset          | `LevelOffset`             | Elevation delta of this face above the floor datum (normally 0)        |
| Corner angle          | `CornerAngle` (`Radians`) | Interior angle from the previous wall; π/2 = 90° inside corner         |
| Connected level index | `ConnectedLevelIdx`       | Which PKNail level governs this wall's top; -1 = use `Height` directly |

`DetectedWall` has: `LengthMeters` ✅ `ThicknessMeters` ✅ `CornerAngleRad` ✅. `LevelOffset` is always `Distance.Zero`.

### Group 4 — Every Door (`SurveyDoorDlg` / `SurveyFamilyDef`)

| PKNail field        | How it is stored                                                    | AI must extract                                                       |
| ------------------- | ------------------------------------------------------------------- | --------------------------------------------------------------------- |
| Location along wall | `WallHostedObjectSurveyParams.Location`                             | Distance from near end of wall to **centre** of rough opening         |
| Width               | `SurveyFamilyDef.Width` (via `SetWidth`)                            | Rough opening width                                                   |
| Height              | `SurveyFamilyDef.Height` (via `SetHeight`)                          | Rough opening height                                                  |
| Sill height         | `SurveyFamilyDef.SillHeight` (via `SetSillHeight`)                  | Height of opening bottom above finished floor                         |
| Hinge side          | `SurveyFamilyDef.LateralOrientation` (`HingeSide`: 0=Left, 1=Right) | Which jamb carries the hinge                                          |
| Swing direction     | `SurveyFamilyDef.FacingOrientation` (`SwingDirection`: 0=Out, 1=In) | Whether door swings toward or away from surveyor                      |
| Family type         | `SurveyFamilyDefIdx`                                                | Index into `ProjectSurveyFamilyDefs` for the chosen Revit door family |

`DetectedOpening` has: `LocationFromNearEndMeters` ✅ `WidthMeters` ✅ `HeightMeters` ✅ `SillHeightMeters` ✅ `IsLikelyDoor` ✅. Missing: `HingeSideIndex`, `SwingDirectionIndex`, `FamilyDefIdx` (bridge hardcodes index 0).

### Group 5 — Every Window (`SurveyWindowDlg` / `SurveyFamilyDef`)

| PKNail field        | How it is stored                                                                 | AI must extract                                                         |
| ------------------- | -------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| Location along wall | `WallHostedObjectSurveyParams.Location`                                          | Distance from near end of wall to window opening **centre**             |
| Width               | `SurveyFamilyDef.Width` (via `SetWidth`)                                         | Rough opening width                                                     |
| Height              | `SurveyFamilyDef.Height` (via `SetHeight`)                                       | Rough opening height                                                    |
| Sill height         | `SurveyFamilyDef.SillHeight` (via `SetSillHeight`)                               | Height of sill above finished floor                                     |
| Side facing         | `SurveyFamilyDef.FacingOrientation` (`SurveyorRelation`: 0=Interior, 1=Exterior) | Which face is the interior face                                         |
| Family type         | `SurveyFamilyDefIdx`                                                             | Index into `ProjectSurveyFamilyDefs` for the chosen Revit window family |

`IsTypicalWindowHeights` and `IsStackedWindows` are deliberately `false` — the AI always provides real measured dimensions.

### Minimum Additions to `DetectedBuilding` / `DetectedOpening` to Close All Gaps

```csharp
// DetectedBuilding
public bool IsScanFromInterior = false;       // → SurveyOrientation.Flipped when true

// DetectedLevel
public double Floor2FloorMeters;              // structural storey height
public bool   HasDropCeiling = false;
public double DropCeilingHeightMeters;        // only when HasDropCeiling
public double TypicalWindowSillHeightMeters;
public double TypicalWindowHeadHeightMeters;

// DetectedOpening (additions)
public int HingeSideIndex      = 0;  // HingeSide: 0=Left, 1=Right
public int SwingDirectionIndex = 0;  // SwingDirection: 0=Out, 1=In
public int FacingOrientationIndex = 0; // SurveyorRelation: 0=Interior, 1=Exterior
public int FamilyDefIdx        = 0;  // index into ProjectSurveyFamilyDefs
```
