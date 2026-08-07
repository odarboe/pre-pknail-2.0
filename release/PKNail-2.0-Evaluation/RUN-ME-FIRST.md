# PKNail 2.0 — Run It Yourself

This build imports an AI-engine JSON file into Revit 2026 and draws the walls, levels and
openings automatically. **No licence key needed** — this is an evaluation build.

You need: **Revit 2026** on Windows. Nothing else to install.

---

## Install (2 minutes, one time)

**1.** Copy the whole `PKNail-2.0-Evaluation` folder to `C:\` so you end up with:

```
C:\PKNail-2.0-Evaluation\PKNailForRevit.dll
```

**2.** Copy `PKNailProForRevit.addin` (in this folder) into:

```
C:\ProgramData\Autodesk\Revit\Addins\2026\
```

> That folder is hidden by default — paste the path straight into the File Explorer address bar.

**3.** Done — *if* you used `C:\PKNail-2.0-Evaluation` in step 1. If you put the folder
somewhere else, open the `.addin` file in Notepad and change the `<Assembly>` line to your
actual path to `PKNailForRevit.dll`.

---

## Run an import (5 steps)

**1.** Start **Revit 2026**. A PKNail splash appears for a few seconds — that's normal.

**2.** Create a **New Project** → choose the **Metric** template → OK.

**3.** Go to the **Add-Ins** tab → **PKNail Pro** panel → click **Import AI Model**.

**4.** Pick a file from the `sample-data` folder. Start with:

```
final_combined_pknail_v4.1.json
```

**5.** A report window appears. Click **OK**.

**To see what was built:** in the Project Browser on the left, open
**Floor Plans → L1 - Architectural**. You'll be looking down at the finished floor plan.

---

## Did it work?

After importing `final_combined_pknail_v4.1.json` you should see:

| Where | What you should see |
| --- | --- |
| The report window | `COMMITTED`, **16 walls placed**, **0 failures** |
| The L1 floor plan | A closed, square multi-room layout — no gaps at the corners |
| Doors & windows | Openings sitting inside the walls (easiest to confirm in a Window/Door **schedule**) |

That's a pass.

Everything is a **single undo step** — press `Ctrl+Z` once to remove the whole import and
try again with a different file.

---

## Test #2 — how the build handles imperfect data

Now import `final_combined_pknail_v4.0_bug.json` into a **fresh** project (File → New).

This is a deliberately imperfect export from an earlier AI-engine run. The report should
still say `COMMITTED`, but it will now list warnings like:

```
~ Levels[0].Walls[0].Endpoints: Wall is 0.113° off square;
  its far end sits 9.8 mm off axis-aligned.
```

**This is the correct behaviour.** The walls in that file are about a tenth of a degree out
of square. The plugin places them exactly as the AI engine delivered them — it does not
quietly "fix" your survey data — but it tells you which walls are off and by how much. If
you compare the two floor plans, `v4.0` will show slight gaps at some corners; `v4.1`
(the corrected export) will not.

Rule of thumb: **warnings = look at it. failures = it didn't build.**

---

## If something goes wrong

| Problem | Fix |
| --- | --- |
| No **PKNail Pro** panel on the Add-Ins tab | The `<Assembly>` path in the `.addin` file doesn't point at a real `PKNailForRevit.dll`. Check it in Notepad. |
| **Import AI Model** is greyed out | You need a project open. Create one (File → New → Metric). It stays greyed out in a *family* document. |
| Revit asks to "Always Load" the add-in on startup | Click **Always Load**. It's unsigned, which is expected for an evaluation build. |
| Report says `ROLLED BACK` | Nothing was written to your model — the whole import is atomic. Send us the report text. |
| Openings are `skipped` | The project template has no door/window family loaded. Walls still import fine. |

---

## What this build is, and isn't

- It reads **only** the AI-engine JSON. It never opens or processes a point cloud — that
  happens earlier, in the separate AI-engine repository.
- Phase 1 targets **orthogonal interior scans**. Angled and exterior walls aren't a target yet.
- This is an **evaluation build**: licensing and usage metering are switched off so you can
  test freely. It is not the build that ships to customers.

Questions or a report that doesn't look right — send us the text from the report window,
it says exactly what happened.
