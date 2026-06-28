# PKNail 2.0

Pointknown's internal geometry-to-BIM engine for converting **point-cloud-derived features into
structured Revit elements** — an *Architectural Intent* engine, not a literal point-cloud overfit.

## Current Phase

MVP development with Cognixlab. Phase 1: interior SLAM datasets, orthogonal walls, levels and walls
first; openings as a stretch goal. Target: **80%+ wall accuracy, walls within ~1″, usable without
full rebuild** (see the brief in `docs/`).

## What changed: PKNAIL 1.0 → 2.0

PKNAIL 1.0 captured as-built geometry **live in the field through a Leica Disto laser rangefinder**
feeding survey dialogs. PKNAIL 2.0 **removes the device step**:

```
PKNAIL 1.0:  Laser Measurement → Survey Dialogs → PKNAIL engine → Revit Model
PKNAIL 2.0:  Point Cloud → [external AI Engine repo] → ai_engine_pknail_results.json → PKNAIL plugin → Revit Model
             └────────── separate, independent repo ──────────┘   └──────── this plugin's only input ───────┘
```

**This plugin never imports point clouds.** The point cloud is uploaded into an **AI Engine — a
separate, independent repository** — which reads it (`.las` / GeoSLAM) and emits
**`docs/ai_engine_pknail_results.json`**. **That JSON is the plugin's sole input.** This Revit 2026
plugin **imports the JSON, maps it onto the existing PKNAIL parameter model, and drives the proven
placement engine** to generate levels, walls, and openings — with little to no manual work. The
rangefinder path remains in the codebase as **legacy/optional**.

## Core Components

- **Revit 2026 / .NET 8 add-in** — placement engine (`IProjectModel` / `RevitDocument`).
- **AI-model JSON import + mapping layer** — `Detected*` DTOs → PKNAIL params (units, intent snapping).
- **Headless execution bridge (`AiModelImporter`)** — places elements through the engine **without
  dialogs, the rangefinder, or any point-cloud parsing**; its only input is the AI-engine JSON.
- Sample `.las` rooms (`code/PKNailProForRevit-2026/PointCloud/*.las`) — **inputs for the external AI
  engine, kept here for reference/testing only**; the plugin does not read them.
- Documentation and MVP acceptance criteria.

> **Point-cloud preprocessing, segmentation, and JSON generation all live in the separate AI-engine
> repository — not in this repo.** This repo's responsibility starts at the JSON.

## Documentation map (read these first)

| Doc | Purpose |
|---|---|
| **`docs/PKNAIL_2.0_JiraPlan.md`** | **Master plan** — epics, tests, commit-gate process, discrepancies, open questions. **Read before any 2.0 work.** |
| `docs/PKNAIL_Fields.md` | Every PKNAIL field the AI must produce + the `Detected*` model additions |
| `docs/ai_engine_pknail_results.json` | Frozen v1 JSON contract (AI-engine output) |
| `docs/PKNAil 2.0 Feasilbilty and Architecture Brief.pdf` | Feasibility & architecture brief (Jim Foster) |
| `code/PKNailProForRevit-2026/.github/copilot-instructions.md` | Full plugin architecture, data model, workflows |
| `code/PKNailProForRevit-2026/.github/agents/pknail-revit.agent.md` | Shared agent instructions (Claude + Copilot) |
| `CLAUDE.md` | Agent quick-start + working agreement |
| `docs/PKNAIL_1.0.legacy.md` | Legacy 1.0 rangefinder workflow reference |

## Working agreement (for contributors & agents)

Implementation proceeds **epic by epic** per `docs/PKNAIL_2.0_JiraPlan.md`. For each epic:
**implement → write full automated + step-by-step manual tests → user confirms → only then
`git commit`** before moving to the next epic. **If anything is unclear or conflicting, stop and
ask for confirmation.** Do not skip these steps.

## Ownership and Confidentiality

This repository contains proprietary Pointknown LLC materials, concepts, documentation, source code,
and development work product.

Do not copy, distribute, publish, reuse, or share any part of this repository outside the authorized
PKNail 2.0 development effort without written permission from Pointknown LLC.
