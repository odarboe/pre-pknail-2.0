# CLAUDE.md — PKNAIL 2.0 agent quick-start

This repo is **PKNAIL 2.0**: a Revit 2026 / .NET 8 plugin that imports an **AI-engine-produced JSON
file** (`docs/ai_engine_pknail_results.json`) and maps it onto the proven PKNAIL placement engine to
generate levels, walls, and openings. PKNAIL 1.0's **Leica Disto rangefinder** input is replaced by
this JSON; the rangefinder code remains as **legacy/optional** (don't delete without sign-off).

**The plugin never imports point clouds.** The point cloud is uploaded into a **separate, independent
AI-engine repository** which produces `ai_engine_pknail_results.json`. **That JSON is this plugin's
only input.** The `.las` files under `code/.../PointCloud/` are AI-engine sample inputs kept here for
reference only — the plugin does not parse them.

## Read before doing anything on 2.0

1. **`docs/PKNAIL_2.0_JiraPlan.md`** — the master plan: epics, tests, the commit-gate process,
   discrepancies, and open questions. **This is the authoritative source when unclear.**
2. `docs/PKNAIL_Fields.md` — the field contract (what the AI must produce + the `Detected*` additions).
3. `docs/ai_engine_pknail_results.json` — frozen v1 JSON contract.
4. `code/PKNailProForRevit-2026/.github/copilot-instructions.md` — full plugin architecture & data model.

## Architecture decisions (confirmed — senior C# architect review)

- **Run order:** Epic 0 → **Epic 9 (test/CI foundation) FIRST** → Epics 1–7 → 10 → 8. The existing test
  projects are still **.NET Framework 4.6** and the only integration harness drives **real Revit 2017 via
  AutoIt** — so a net8.0 test project + an **in-process `IProjectModel` fake** must exist before the
  tests-before-commit gate is real.
- **Deserializer:** **System.Text.Json** for the `Detected*` DTOs (keep LitJson only for internal
  `JsonSerializable` persistence).
- **Structure:** DTOs + mapping live in a **new Revit-free `PKNail.AiImport` net8.0 library**; the
  Revit-touching `AiModelImporter` bridge stays in `PKNailForRevit`.
- **Headless execution:** business logic lives in the engine's `*Prep` methods (not the dialogs), so the
  bridge calls `Prep` then overwrites the measured fields and commits via the existing `executeInTxn`
  `TransactionGroup` — no `ExternalEvent` needed. Import = **atomic rollback** on failure; metering is
  **per-element** (1.0 behavior); suppress auto-save until the project has a path.
- `PKNailParameter` uses Revit shared/project params (`LookupParameter`), **not** extensible storage.

## Code location

- Solution: `code/PKNailProForRevit-2026/PKNailForRevit.sln`
- Target: `net8.0-windows`, x64, Revit 2026. The 2017→2026 migration of the **main plugin** is done; the
  **test projects are not yet modernized** (Epic 9).
- The `Detected*` DTOs / `AiModelImporter` bridge **do not exist yet** — building them is the 2.0
  work (Epics 1–3). (`PKNAIL_Fields.md` calls the bridge "PointCloudImporter"; we use `AiModelImporter`
  because it consumes the AI-engine JSON, not the point cloud.)

## Working agreement (MANDATORY)

Proceed **one epic at a time** per the Jira plan. For each epic:
1. Implement the stories.
2. Write **full automated end-to-end / integration tests** (NUnit + TestCore/TestRunner).
3. Give **step-by-step manual test instructions** so the user can verify in Revit.
4. **Wait for the user to confirm it works.**
5. **Only then `git commit`** (one focused commit per epic; format in the Jira plan §2). Branch off
   `main`; don't commit to `main` without sign-off.
6. **If anything is unclear or conflicting, STOP and ask.** Log it in the plan's Open Questions.

Do **not** skip tests, do **not** batch epics into one commit, do **not** proceed past an unconfirmed gate.

## Coding conventions (from the plugin)

- All measurements use the `Distance` type — **never raw `double`**; `Distance.NoValue` is the "unset" sentinel.
- Revit internal units are **decimal feet**; the JSON is in **meters** — convert on import.
- `Prep` methods use trial transactions; execution happens in a committed `TransactionGroup`.
- Document data is JSON-serialized into Revit extensible storage via `PKNailParameter`.
- The headless import path must **not** instantiate `DrivableForm` dialogs, touch `Disto`, or parse
  point clouds — its only input is the AI-engine JSON.

## Known discrepancies to respect (details in Jira plan §5)

- 🔴 Wall geometry has **no coordinates** — only `LengthMeters` + unreliable `CornerAngleRad`
  (mostly 0, one 90°, one 270°). Cannot close an orthogonal loop alone. **Resolve with the user
  before finishing Epic 5** (recommend the AI engine emit wall start/end points).
- `FamilyDefIdx` is always 0 — resolve it via 1.0's `ProjectSurveyFamilyDefs` indexing (Q3 resolved).
- `Floor2CeilingElev` lacks the `Meters` suffix other fields have (parse tolerantly); it's still meters.
- Metering: **per-element, exactly like 1.0** — `RecordUsage` before each placed wall/opening (Q4 resolved).

## Units & the Detected* layer (common confusion)

- **Units:** JSON is **meters/radians**; the plugin is **decimal feet** internally (`Distance`). Convert at
  the import boundary only: `new Distance(meters, Distance.Units.MetricMeter)`. Never raw `double` math.
- **The PKNAIL parameters already exist** (`LevelDataItem`, `WallSurveyParams`, `SurveyFamilyDef`, the
  `HingeSide/SwingDirection/SurveyorRelation/CompassDirection` enums). We **reuse** them. What's new is the
  `Detected*` DTO layer (the JSON deserialization target) + `AiModelImporter`. Don't re-define PKNAIL params.
