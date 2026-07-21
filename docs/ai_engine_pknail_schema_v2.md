# `ai_engine_pknail_results.json` — Contract v2 (proposed)

**Audience:** AI-Engine team. **Status:** proposed target for the AI engine to implement.
**Why v2:** v1 encoded walls as a relative `LengthMeters` + `CornerAngleRad` chain with no coordinates.
In the Revit engine's interior-angle convention, `CornerAngleRad = 0` makes a wall fold 180° back over the
previous one, so the v1 data produced overlapping walls and a non-finite coordinate in a live Revit 2026
import (see `PKNAIL_2.0_JiraPlan.md` Appendix A). **v2 replaces the angle chain with absolute wall
endpoints**, which removes all angle ambiguity — the plugin derives lengths, corners, joins, and
orthogonal snapping itself.

- Example file: [`ai_engine_pknail_results.v2.example.json`](ai_engine_pknail_results.v2.example.json)
  (a clean 5.05 × 4.0 m room with one window and one door).

---

## 1. Conventions

- **Lengths:** meters (`double`). **Angles (if any):** radians.
- **Coordinates:** a single consistent **plan (XY) frame**, right-handed, +Z up. The origin may be
  arbitrary (the plugin re-bases to its own project origin); just keep **all walls in the same frame**.
- **Wall direction:** each wall is an ordered segment from `StartMeters` → `EndMeters`. Walls in a level
  are listed in **loop order** (each wall's `End` ≈ the next wall's `Start`).
- **Wall location line:** `LocationLine` states whether `Start`/`End` are the wall **centerline** or a
  **face**. Default and strongly preferred: **`"Centerline"`**.
- **Closed loops:** set `IsClosed: true` when the last wall's `End` returns to the first wall's `Start`.
- **Opening position:** `LocationFromStartMeters` is measured **along the wall from `StartMeters` to the
  opening centre** (removes the v1 "near end" ambiguity).
- **Units are declared explicitly** (`Units`, `AngleUnits`) and every length field keeps its `Meters`
  suffix (fixes the v1 `Floor2CeilingElev` inconsistency).

---

## 2. Field reference

### Root
| Field | Type | Required | Notes |
|---|---|---|---|
| `SchemaVersion` | string | ✅ | `"2.0"`. The plugin validates and warns on unknown versions. |
| `Units` | string | ✅ | `"meters"`. |
| `AngleUnits` | string | ✅ | `"radians"`. |
| `CoordinateSystem` | object | ✅ | See below. |
| `IsScanFromInterior` | bool | ✅ | `true` ⇒ scan taken inside ⇒ surveyor-flipped orientation. |
| `FirstWallCompassDirection` | int | ✅ | `0`=DontKnow, `1`=N, `2`=E, `3`=S, `4`=W. |
| `Levels` | array | ✅ | One entry per storey, lowest-first preferred. |

### `CoordinateSystem`
| Field | Type | Notes |
|---|---|---|
| `Frame` | string | `"plan"`. |
| `Handedness` | string | `"right"`. |
| `ZUp` | bool | `true`. |
| `XAxisRealWorld` / `YAxisRealWorld` | string | optional: real-world direction of +X/+Y (e.g. `"east"`/`"north"`) if known, else `"unknown"`. |

### `Level`
| Field | Type | Required | Notes |
|---|---|---|---|
| `Name` | string | optional | display name. |
| `ElevationMeters` | double | ✅ | base elevation of this level (0 for ground). |
| `Floor2FloorMeters` | double | ✅ | structural storey height. |
| `Floor2CeilingMeters` | double | ✅ | finished floor to ceiling (note **renamed** from v1 `Floor2CeilingElev`). |
| `HasDropCeiling` | bool | ✅ | |
| `DropCeilingHeightMeters` | double | when `HasDropCeiling` | floor to drop ceiling. |
| `TypicalWindowSillHeightMeters` | double | ✅ | most common sill height on this level. |
| `TypicalWindowHeadHeightMeters` | double | ✅ | most common window head height. |
| `IsClosed` | bool | ✅ | wall loop closes back to the start. |
| `Walls` | array | ✅ | ordered loop. |

### `Wall`
| Field | Type | Required | Notes |
|---|---|---|---|
| `StartMeters` | `{X,Y}` | ✅ | wall start point (per `LocationLine`). |
| `EndMeters` | `{X,Y}` | ✅ | wall end point. |
| `ThicknessMeters` | double | ✅ | wall thickness, near-face to far-face. |
| `LocationLine` | string | ✅ | `"Centerline"` (preferred), `"InteriorFace"`, or `"ExteriorFace"`. |
| `LevelOffsetMeters` | double | ✅ | elevation delta of this wall above the floor datum (usually 0). |
| `LengthMeters` | double | optional | derived; if present must equal `|End−Start|` (the plugin recomputes). |
| `Openings` | array | ✅ | may be empty. |

### `Opening`
| Field | Type | Required | Notes |
|---|---|---|---|
| `LocationFromStartMeters` | double | ✅ | distance from `StartMeters` to the opening **centre**, along the wall. |
| `WidthMeters` | double | ✅ | rough opening width. |
| `HeightMeters` | double | ✅ | rough opening height. |
| `SillHeightMeters` | double | ✅ | bottom of opening above finished floor (`0` for most doors). |
| `Kind` | string | ✅ | `"Door"` or `"Window"` (replaces v1 `IsLikelyDoor`). |
| `HingeSideIndex` | int | door only | `0`=Left, `1`=Right. |
| `SwingDirectionIndex` | int | door only | `0`=Out, `1`=In. |
| `FacingOrientationIndex` | int | window only | `0`=Interior, `1`=Exterior. |
| `FamilyDefIdx` | int | optional | index into the project family list; default `0`. |

---

## 3. JSON Schema (Draft 2020-12)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://pointknown.com/schemas/ai_engine_pknail_results.v2.json",
  "title": "PKNAIL AI engine results (v2)",
  "type": "object",
  "required": ["SchemaVersion", "Units", "AngleUnits", "CoordinateSystem",
               "IsScanFromInterior", "FirstWallCompassDirection", "Levels"],
  "additionalProperties": false,
  "properties": {
    "SchemaVersion": { "const": "2.0" },
    "Units": { "const": "meters" },
    "AngleUnits": { "const": "radians" },
    "CoordinateSystem": {
      "type": "object",
      "required": ["Frame", "Handedness", "ZUp"],
      "additionalProperties": false,
      "properties": {
        "Frame": { "const": "plan" },
        "Handedness": { "enum": ["right", "left"] },
        "ZUp": { "type": "boolean" },
        "OriginNote": { "type": "string" },
        "XAxisRealWorld": { "type": "string" },
        "YAxisRealWorld": { "type": "string" }
      }
    },
    "IsScanFromInterior": { "type": "boolean" },
    "FirstWallCompassDirection": { "type": "integer", "minimum": 0, "maximum": 4 },
    "Levels": {
      "type": "array",
      "minItems": 1,
      "items": { "$ref": "#/$defs/level" }
    }
  },
  "$defs": {
    "point2d": {
      "type": "object",
      "required": ["X", "Y"],
      "additionalProperties": false,
      "properties": { "X": { "type": "number" }, "Y": { "type": "number" } }
    },
    "level": {
      "type": "object",
      "required": ["ElevationMeters", "Floor2FloorMeters", "Floor2CeilingMeters",
                   "HasDropCeiling", "TypicalWindowSillHeightMeters",
                   "TypicalWindowHeadHeightMeters", "IsClosed", "Walls"],
      "additionalProperties": false,
      "properties": {
        "Name": { "type": "string" },
        "ElevationMeters": { "type": "number" },
        "Floor2FloorMeters": { "type": "number", "exclusiveMinimum": 0 },
        "Floor2CeilingMeters": { "type": "number", "exclusiveMinimum": 0 },
        "HasDropCeiling": { "type": "boolean" },
        "DropCeilingHeightMeters": { "type": "number", "minimum": 0 },
        "TypicalWindowSillHeightMeters": { "type": "number", "minimum": 0 },
        "TypicalWindowHeadHeightMeters": { "type": "number", "minimum": 0 },
        "IsClosed": { "type": "boolean" },
        "Walls": { "type": "array", "minItems": 1, "items": { "$ref": "#/$defs/wall" } }
      }
    },
    "wall": {
      "type": "object",
      "required": ["StartMeters", "EndMeters", "ThicknessMeters", "LocationLine",
                   "LevelOffsetMeters", "Openings"],
      "additionalProperties": false,
      "properties": {
        "StartMeters": { "$ref": "#/$defs/point2d" },
        "EndMeters": { "$ref": "#/$defs/point2d" },
        "ThicknessMeters": { "type": "number", "exclusiveMinimum": 0 },
        "LocationLine": { "enum": ["Centerline", "InteriorFace", "ExteriorFace"] },
        "LevelOffsetMeters": { "type": "number" },
        "LengthMeters": { "type": "number", "exclusiveMinimum": 0 },
        "Openings": { "type": "array", "items": { "$ref": "#/$defs/opening" } }
      }
    },
    "opening": {
      "type": "object",
      "required": ["LocationFromStartMeters", "WidthMeters", "HeightMeters",
                   "SillHeightMeters", "Kind"],
      "additionalProperties": false,
      "properties": {
        "LocationFromStartMeters": { "type": "number", "minimum": 0 },
        "WidthMeters": { "type": "number", "exclusiveMinimum": 0 },
        "HeightMeters": { "type": "number", "exclusiveMinimum": 0 },
        "SillHeightMeters": { "type": "number", "minimum": 0 },
        "Kind": { "enum": ["Door", "Window"] },
        "HingeSideIndex": { "type": "integer", "enum": [0, 1] },
        "SwingDirectionIndex": { "type": "integer", "enum": [0, 1] },
        "FacingOrientationIndex": { "type": "integer", "enum": [0, 1] },
        "FamilyDefIdx": { "type": "integer", "minimum": 0 }
      }
    }
  }
}
```

---

## 4. v1 → v2 change summary

| v1 | v2 | Reason |
|---|---|---|
| Wall `LengthMeters` + `CornerAngleRad` (relative chain) | Wall `StartMeters` + `EndMeters` (absolute XY) | Eliminates the angle-convention ambiguity that produced overlapping/degenerate walls. |
| (none) | `LocationLine` per wall | Defines whether Start/End are centerline or a face. |
| (none) | `Level.IsClosed` | Lets the plugin close the loop deterministically. |
| `Opening.LocationFromNearEndMeters` | `Opening.LocationFromStartMeters` | Measured from the wall's `Start`, removing "near end" ambiguity. |
| `Opening.IsLikelyDoor` (bool) | `Opening.Kind` (`"Door"`/`"Window"`) | Explicit and extensible. |
| `Floor2CeilingElev` (no suffix) | `Floor2CeilingMeters` | Consistent suffixing. |
| (none) | `SchemaVersion`, `Units`, `AngleUnits`, `CoordinateSystem`, `Level.ElevationMeters` | Self-describing + multi-level support. |

**Status:** the plugin's import layer **already consumes v2** (auto-detected when walls carry
`StartMeters`/`EndMeters`). The mapper derives each wall's length and **interior corner angle** from the
endpoints (engine convention: `π/2` = 90° inside corner), winding-independent, then drives the same proven
survey engine as v1. Verified by `V2ContractTests` / `V2ContractImportTests` — the v2 example maps to the
exact corner angles (`[0, π/2, π/2, π/2]`) of the demo that produced a clean room in Revit 2026. v1 remains
fully supported for regression.
