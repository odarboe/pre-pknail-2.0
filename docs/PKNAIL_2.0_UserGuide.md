# PKNAIL 2.0 — User & Handoff Guide

PKNAIL 2.0 imports an **AI-engine-produced JSON file** into Revit 2026 and generates levels, walls,
and openings automatically — replacing PKNAIL 1.0's manual Leica Disto rangefinder survey. The point
cloud is processed by a **separate AI-engine repository**; this plugin's only input is the JSON that
engine emits.

> Companion docs: [PKNAIL_2.0_JiraPlan.md](PKNAIL_2.0_JiraPlan.md) (master plan / decisions),
> [ai_engine_pknail_schema_v2.1.md](ai_engine_pknail_schema_v2.1.md) (JSON contract, current),
> [ai_engine_pknail_schema_v2.md](ai_engine_pknail_schema_v2.md) (v2 background), and
> [../code/PKNailProForRevit-2026/DevDoc/BuildSetup-2.0.md](../code/PKNailProForRevit-2026/DevDoc/BuildSetup-2.0.md)
> (build & packaging).

---

## 1. What you need

- **Revit 2026** (64-bit).
- The **PKNail Pro** add-in installed (see §6) or a developer build deployed (see the DevDoc).
- An **AI-engine results JSON** file. Bundled samples live in `docs/`:
  - `October5/New/*.json` — the October 2026 AI-engine and manual-check exports (v2, 13–19 walls each).
  - `ai_engine_pknail_results.v2.1.example.json` — two rooms, a partition, doors with hinge/swing (v2.1).
  - `new_final_combined_pknail.json` — a real 9-wall building with 3 windows (v2, absolute endpoints).
  - `ai_engine_pknail_results.v2.example.json` — a clean 5.05 × 4.0 m room (v2).
  - `ai_engine_pknail_results.demo.json` — the known-good demo room (v1, survey-chain).

---

## 2. Run an import (step by step)

1. Launch **Revit 2026** and start a **new project** (the metric template is the tested path).
2. Go to **Add-Ins → PKNail Pro → Import AI Model**.
3. In the file picker, choose your AI-engine JSON (e.g. `docs\new_final_combined_pknail.json`).
4. The import runs as a **single atomic operation** (one undo entry). For files whose walls carry
   coordinates (v2/v2.1) it uses **Direct Build** (Epic 13). It:
   - binds the PKNail parameters into the document (works on any project, not just a PKNail template);
   - matches each level to an existing Revit level by elevation (the lowest AI level becomes elevation 0),
     or creates the level and its floor plan;
   - tidies the wall network: squares walls within 2° of an axis and closes gaps of up to 10 cm at corners
     and T-junctions — never deletes a wall;
   - draws every wall as a native Revit wall on its centerline, with its AI id in **Mark** and the AI's
     note in **Comments**;
   - places every door/window at its centre, using a type of the measured size (e.g.
     `PKNail AI Door 760 x 1990`) duplicated from the PKNAIL family the file asks for (`OpeningType`,
     else `FamilyDefIdx`), or from the first wall-hosted family when the project has no PKNAIL families;
   - collects any Revit warnings into the report instead of interrupting you with pop-ups.

   v1 files (no wall coordinates) still use the original survey-chain path.
5. Read the **Import AI Model** report dialog (see §3), then **OK**.

To see the result: open the **`L1 - Architectural`** floor plan (top-down) or a **Window/Door schedule** —
interior openings are easiest to confirm there rather than in a 3D view.

---

## 3. Reading the import report

The report summarizes exactly what happened — nothing is silently dropped:

```
Import committed with 13 warning(s) — review below.

AI import (Direct Build): COMMITTED; walls placed=19, openings placed=18, skipped=0, adjustments=1, warnings=13, failures=0
  origin re-based to project 0,0 (subtracted 3.232, -61.475 m from survey coords)
  * W18: squared up (0.457° off the Y axis); each end moved 27.7 mm.
  ~ W15 and W18 overlap for 5.48 m on the same line (28 mm apart) — possibly a duplicate wall. ...
  ~ W1-D1: door swing and hinge side not provided by the AI; Revit default kept.
  ~ ... (one line per door; Revit's own warnings are added here too)
```

| Field | Meaning |
| --- | --- |
| `COMMITTED` / `ROLLED BACK` | Whether the model was written. A failure rolls the **whole** import back — you never get a half-built model. |
| `walls placed` / `openings placed` | Elements successfully created. |
| `skipped` (`-` lines) | Best-effort items that were **skipped, not failed** (e.g. an opening whose family isn't loaded). The import continues. |
| `adjustments` (`*` lines) | Direct Build only: every change made to the AI geometry (squared walls, closed gaps, openings slid inside their wall, levels created). |
| `warnings` (`~` lines) | Things to check that were still placed: possible duplicate walls, overlapping openings, doors without hinge/swing data, fixed-size families, and Revit warnings absorbed during placement. |
| `failures` (`!` lines) | Hard errors that rolled the import back. |
| `origin re-based …` | How far the building was shifted from its raw survey coordinates to sit at the project origin (deterministic; documented here). |

After import, run **Set Coord Sys** (PKNail Pro) if you want to re-anchor the model to a chosen wall —
this is an interactive step by design.

---

## 4. The JSON contract (what the AI engine must emit)

Current spec: **[ai_engine_pknail_schema_v2.1.md](ai_engine_pknail_schema_v2.1.md)** (the AI-team
instructions). In brief, v2/v2.1 carries **absolute wall endpoints** (`Start`/`End` in meters, any order,
no closed loop needed), and v2.1 adds optional ids, wall heights, interior side, opening type and
comments. All lengths are **meters**, all angles **radians**; the plugin converts to Revit's internal
feet at the import boundary. The older v1 contract (relative `LengthMeters` + `CornerAngleRad`) is still
read for backward compatibility but cannot reliably close an orthogonal room on its own — prefer v2.

---

## 5. Known limitations (Phase 1)

- **Orthogonal, interior scans.** Tuned for interior SLAM datasets with orthogonal walls and limited
  complexity. Non-orthogonal / exterior scans are not a Phase-1 target.
- **Near-square walls are squared up (Direct Build).** A wall within 2° of an axis is straightened and
  the move is listed under adjustments. Deliberately angled walls, more than 2° off, are drawn as sent.
- **Door and window families.** In a project with PKNAIL survey families (the PKNAIL template), each
  opening is built from the family the file asks for: `OpeningType` first (`Sliding`, `Pocket`,
  `Hinged` = "Single", `DoubleHinged` = "Double", `DoubleHung`, …), else `FamilyDefIdx`, which is the
  position in the **Capture** family list (doors: Single 0, Double 1, Opening 2, Sliding Glass 3,
  Sliding 4, Pocket 5). A type or index that matches nothing uses a default family and the report says
  so. A project without PKNAIL families (any stock Revit template) gets the shipped PKNAIL door and
  window families loaded by the import, and the report lists them. Only if those family files cannot
  be found does every opening use the first wall-hosted family of its category; with no family at all,
  a plain rectangular opening of the measured size is cut instead.
- **Some families cannot be built in some walls.** In testing, the imperial default door family
  "Door-Passage-Single-Flush" could not be built in 100 mm thick walls at any width (first read as a
  571–678 mm width limit; the 2026-11-06 run showed a type that builds in a 115 mm wall failing in a
  100 mm one). The PKNAIL template's door families do not have this limit.
  When that happens no dialog appears: the import places the nearest existing type within 10 cm of the
  measured width, or else cuts a plain opening of the exact size, and the report says which.
- **Door handing needs AI data.** Without `HingeSideIndex`/`SwingDirectionIndex` the family's default
  handing is kept and the report says so. Sliding, pocket and plain-opening families have no hinge or
  swing, so they keep their default handing. The hinge convention was corrected after the 2026-11-06
  Revit check and is to be re-confirmed on the PKNAIL "Single" family (Q19).
- **Single closed loop assumed per run** for the survey-chain (v1) path; the v2 absolute path places
  independent walls and has no such assumption.
- **Coordinate system:** import lands at a deterministic default origin; re-anchoring is a manual
  **Set Coord Sys** step (no headless coordinate pick).
- **Not yet built (deferred from Epic 7):** visual highlight/zoom of skipped items, and an automated
  accuracy-metrics harness comparing the import against a manual reference model.

## 5b. Phase-2 roadmap

- Exterior scans and non-orthogonal walls.
- Multi-level buildings beyond the single-level happy path.
- Lines and generic wall-hosted objects (electrical, plumbing fixtures, etc.).
- A project-level name map from `OpeningType` to family (today: name match plus two built-in aliases).
- ML-assisted segmentation improvements upstream in the AI engine.
- Accuracy-metrics harness + visual review aids (Epic 7 stories 7.3 / 7.4).

---

## 6. Installation & packaging (release)

The WiX installer under `code/PKNailProForRevit-2026/Setup/` produces
`PKNailProForRevit2026.msi`. Building and **signing** it requires the WiX v3 toolset and PointKnown's
code-signing certificate — see the **Packaging & Release** section of
[DevDoc/BuildSetup-2.0.md](../code/PKNailProForRevit-2026/DevDoc/BuildSetup-2.0.md). The manifest
installs the plugin DLLs (including `PKNail.AiImport.dll`), the `.addin`, the PKNail families and
templates, and the EULA/manual, and writes the Revit 2026 add-in manifest.

For development (no installer), deploy by pointing a Revit 2026 `.addin` manifest at the built
`PKNailForRevit.dll` — the DevDoc covers this.
