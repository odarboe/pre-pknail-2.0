# `ai_engine_pknail_results.json` — Contract v2.1

**Audience:** AI-engine team. **Status:** approved by PointKnown on 2026-10-12. Please implement.
**Example file:** [`ai_engine_pknail_results.v2.1.example.json`](ai_engine_pknail_results.v2.1.example.json)
(two rooms, a partition with a T-junction, three windows, two doors).

## Why this change

The PKNAIL Revit add-in now draws your file directly as native Revit walls, doors and windows
("Direct Build"). It no longer pushes the walls through the old surveying chain, so it no longer needs
the walls in loop order or as a closed loop. Your current v2 exports are a set of independent wall
segments meeting at corners and T-junctions. That is now exactly what we want.

v2.1 is **additive**. Every v2.0 file still imports. The new fields let us label, size and orient
elements correctly instead of guessing.

## What to change

### 1. Must do

1. **Set `"SchemaVersion": "2.1"`.**
2. **Stop ordering walls as a loop.** List walls in any order. `IsClosed` is no longer needed; we ignore it.
3. **Give every wall `StartMeters` and `EndMeters` on its centerline.** Keep `"LocationLine": "Centerline"`.
   Face-based lines are not supported yet.
4. **Make walls meet exactly where they join.** At an L-corner, both walls end at the same point. At a
   T-junction, the joining wall ends on the other wall's centerline. Your October exports already do
   this within 1 mm. Keep it that way.
5. **For every door, send `HingeSideIndex` and `SwingDirectionIndex`.** Use the definitions in
   section 3. If you truly cannot tell, omit the field. Never send a placeholder `0`.
6. **Use one wall per straight run.** Do not split a wall at its openings. Put the openings on the wall.

### 2. Should do (optional fields, all new)

| Where | Field | Type | Meaning | What the add-in does with it |
|---|---|---|---|---|
| Wall | `Id` | string | Stable id, unique in the file, e.g. `"W7"` | Revit **Mark** |
| Wall | `HeightMeters` | number | Measured wall height | Wall height. Absent ⇒ level `Floor2CeilingMeters` |
| Wall | `InteriorSide` | `"Left"` / `"Right"` | Interior side, seen walking from Start to End | Orients doors and windows (section 3) |
| Wall | `Comment` | string | e.g. `"thickness assumed (one face scanned); 87% scanned"` | Revit **Comments** |
| Opening | `Id` | string | Unique in the file, e.g. `"W7-D1"` | Revit **Mark** |
| Opening | `OpeningType` | string | Doors: `Hinged`, `DoubleHinged`, `Sliding`, `Pocket`, `Bifold`. Windows: `Fixed`, `Casement`, `Sliding`, `Awning`, `DoubleHung` | Picks the PKNAIL door/window family of that name (a type with no matching family uses the default family and is reported). Also written to Revit **Comments** |
| Opening | `Comment` | string | e.g. `"confirmed: all edges scanned"` | Revit **Comments** |
| Level | `Name` | string | e.g. `"Ground"` | Name of a level we create |
| Level | `ElevationMeters` | number | Floor elevation in your frame | Levels are re-based so the lowest is at 0 |

**Please put your confidence data into `Comment`.** Reviewers see it on the element in Revit. That is
where `thickness_source`, `scanned_pct`, `seen_from` and `confidence` from your internal output belong.

### 3. Orientation definitions (doors and windows)

- **Interior side of a wall.** Send it as `InteriorSide`: `"Left"` or `"Right"` as seen walking from
  `StartMeters` to `EndMeters`. For an outside wall it is the side inside the building. For an interior
  partition, choose a side and describe the doors relative to it. If omitted, the add-in uses the side
  facing the middle of the floor plan. That guess is fine for outside walls but unreliable for partitions.
- **`SwingDirectionIndex`.** `1` = **In**: the door leaf opens into the interior side. `0` = **Out**:
  it opens into the other side.
- **`HingeSideIndex`.** Stand on the interior side, facing the wall. `0` = **Left**: hinges on your
  left. `1` = **Right**: hinges on your right.
- **Windows** are always placed facing the exterior. `FacingOrientationIndex` is not needed.

### 4. Do not

- Do not send `FamilyDefIdx`. It is deprecated: it indexes a list inside the Revit document that you
  cannot see. Send `OpeningType` instead. (The add-in still reads `FamilyDefIdx` when an opening has no
  usable `OpeningType`, so existing v2.0 files keep working.)
- Do not send zero-length walls, walls with zero or negative thickness, or openings wider than their wall.
- Do not repeat a wall. Two walls on the same line that share length are reported as a likely duplicate.

## How the add-in treats imperfect data

Nothing is deleted silently. Every change and every skip is listed in the import report.

| Situation in the JSON | Result in Revit |
|---|---|
| Wall within 2° of the X or Y axis | Squared to the axis, kept in place. Reported. |
| Wall end within 10 cm of where it should meet another wall | Extended or trimmed along its own line to meet it. Reported. |
| Larger gap at a join | Left as sent. |
| Two walls overlapping on the same line | Both drawn. Reported as a possible duplicate. |
| Opening sticking out past the wall end | Slid inside the wall. Reported. |
| Two openings overlapping on one wall | Both placed. Reported. |
| Zero-length wall, bad thickness, opening wider than its wall, opening centre outside its wall | That element is skipped and reported. The rest of the file still imports. |
| Door without `HingeSideIndex` / `SwingDirectionIndex` | Placed with the family's default handing. Reported. |
| Door or window the Revit family cannot be built at that size or in that wall | The nearest existing type within 10 cm, else a plain opening of the exact size. Reported. |
| Problem with the file itself: no levels, non-positive storey or ceiling height | The import stops with a message. |

## Field reference (complete v2.1)

Unchanged from v2.0 unless marked **new** or **changed**.

**Root:** `SchemaVersion` (`"2.1"`, **changed**), `Units` (`"meters"`), `AngleUnits` (`"radians"`),
`CoordinateSystem`, `IsScanFromInterior`, `FirstWallCompassDirection`, `Levels`.

**Level:** `Name` (optional), `ElevationMeters`, `Floor2FloorMeters`, `Floor2CeilingMeters`,
`HasDropCeiling`, `DropCeilingHeightMeters`, `TypicalWindowSillHeightMeters`,
`TypicalWindowHeadHeightMeters`, `Walls`. `IsClosed` is optional and ignored (**changed**).

**Wall:** `StartMeters {X,Y}`, `EndMeters {X,Y}`, `ThicknessMeters`, `LocationLine` (`"Centerline"`),
`LevelOffsetMeters`, `Openings`, plus optional `Id`, `HeightMeters`, `InteriorSide`, `Comment` (**new**).
`LengthMeters` is optional; we recompute it.

**Opening:** `LocationFromStartMeters` (Start to the opening **centre**), `WidthMeters`, `HeightMeters`,
`SillHeightMeters`, `Kind` (`"Door"`/`"Window"`). Doors: `HingeSideIndex`, `SwingDirectionIndex`
(**send for every door**). Optional `Id`, `OpeningType`, `Comment` (**new**). `FamilyDefIdx` is
deprecated.

## Checklist before sending a file

- [ ] `SchemaVersion` is `"2.1"` and all lengths are meters.
- [ ] Every wall has `StartMeters` and `EndMeters` on its centerline.
- [ ] Walls that join share an end point, or the end lies on the other wall's centerline.
- [ ] Every door has `HingeSideIndex` and `SwingDirectionIndex`, defined as in section 3.
- [ ] `Id` values are unique within the file.
- [ ] No duplicate walls, no zero-length walls, no opening wider than its wall.
- [ ] Opening centres lie on their wall: `0 ≤ LocationFromStartMeters ≤ wall length`.
