# PKNAIL 2.0 — User & Handoff Guide

PKNAIL 2.0 imports an **AI-engine-produced JSON file** into Revit 2026 and generates levels, walls,
and openings automatically — replacing PKNAIL 1.0's manual Leica Disto rangefinder survey. The point
cloud is processed by a **separate AI-engine repository**; this plugin's only input is the JSON that
engine emits.

> Companion docs: [PKNAIL_2.0_JiraPlan.md](PKNAIL_2.0_JiraPlan.md) (master plan / decisions),
> [ai_engine_pknail_schema_v2.md](ai_engine_pknail_schema_v2.md) (JSON contract), and
> [../code/PKNailProForRevit-2026/DevDoc/BuildSetup-2.0.md](../code/PKNailProForRevit-2026/DevDoc/BuildSetup-2.0.md)
> (build & packaging).

---

## 1. What you need

- **Revit 2026** (64-bit).
- The **PKNail Pro** add-in installed (see §6) or a developer build deployed (see the DevDoc).
- An **AI-engine results JSON** file. Bundled samples live in `docs/`:
  - `new_final_combined_pknail.json` — a real 9-wall building with 3 windows (v2, absolute endpoints).
  - `ai_engine_pknail_results.v2.example.json` — a clean 5.05 × 4.0 m room (v2).
  - `ai_engine_pknail_results.demo.json` — the known-good demo room (v1, survey-chain).

---

## 2. Run an import (step by step)

1. Launch **Revit 2026** and start a **new project** (the metric template is the tested path).
2. Go to **Add-Ins → PKNail Pro → Import AI Model**.
3. In the file picker, choose your AI-engine JSON (e.g. `docs\new_final_combined_pknail.json`).
4. The import runs as a **single atomic operation** (one undo entry). It:
   - binds the PKNail parameters into the document (works on any project, not just a PKNail template);
   - creates/links the level(s) and sets floor-to-ceiling / typical window heights;
   - places every wall at its real plan coordinates, re-based to the project origin;
   - hosts each door/window on its wall;
   - collects any Revit warnings into the report instead of interrupting you with pop-ups.
5. Read the **Import AI Model** report dialog (see §3), then **OK**.

To see the result: open the **`L1 - Architectural`** floor plan (top-down) or a **Window/Door schedule** —
interior openings are easiest to confirm there rather than in a 3D view.

---

## 3. Reading the import report

The report summarizes exactly what happened — nothing is silently dropped:

```
Import committed with 1 warning(s) — review below.

AI import: COMMITTED; walls placed=9, openings placed=3, skipped=0, warnings=1, failures=0
  origin re-based to project 0,0 (subtracted 4.419, -106.091 m from survey coords)
  ~ Insert conflicts with joined Wall.
```

| Field | Meaning |
| --- | --- |
| `COMMITTED` / `ROLLED BACK` | Whether the model was written. A failure rolls the **whole** import back — you never get a half-built model. |
| `walls placed` / `openings placed` | Elements successfully created. |
| `skipped` (`-` lines) | Best-effort items that were **skipped, not failed** (e.g. an opening whose family isn't loaded). The import continues. |
| `warnings` (`~` lines) | Revit warnings absorbed during placement (e.g. *insert conflicts with joined wall*). Informational — the elements were still placed. |
| `failures` (`!` lines) | Hard errors that rolled the import back. |
| `origin re-based …` | How far the building was shifted from its raw survey coordinates to sit at the project origin (deterministic; documented here). |

After import, run **Set Coord Sys** (PKNail Pro) if you want to re-anchor the model to a chosen wall —
this is an interactive step by design.

---

## 4. The JSON contract (what the AI engine must emit)

Full spec: **[ai_engine_pknail_schema_v2.md](ai_engine_pknail_schema_v2.md)**. In brief, v2 carries
**absolute wall endpoints** (`Start`/`End` in meters) so the plugin can reconstruct the floor plan
robustly. All lengths are **meters**, all angles **radians**; the plugin converts to Revit's internal
feet at the import boundary. The older v1 contract (relative `LengthMeters` + `CornerAngleRad`) is still
read for backward compatibility but cannot reliably close an orthogonal room on its own — prefer v2.

---

## 5. Known limitations (Phase 1)

- **Orthogonal, interior scans.** Tuned for interior SLAM datasets with orthogonal walls and limited
  complexity. Non-orthogonal / exterior scans are not a Phase-1 target.
- **Near-square walls are placed exactly as delivered.** On the v2 absolute path the plugin places
  `Start`/`End` verbatim — it does **not** square them up. If the AI engine exports a wall that is
  *almost* axis-aligned (say 0.1° off), it stays that way in Revit, so corners may not meet and joins
  can fail. The import report now flags this per wall, e.g.
  `~ Levels[0].Walls[3].Endpoints: Wall is 0.113° off square; its far end sits 13.6 mm off axis-aligned.`
  It is a **warning, not an error** — the import still runs. The fix belongs upstream: ask the AI engine
  for axis-aligned coordinates. (Deliberately angled walls, more than 2° off square, are not reported.)
- **Openings need a loaded family.** Doors/windows host against the first loadable family of the right
  category in the document. With no such family loaded, the opening is **skipped** (reported), not forced.
- **`FamilyDefIdx` is always 0** from the AI engine, so non-default families can't yet be requested per
  opening.
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
- Per-opening family selection (real `FamilyDefIdx` → family registry).
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
