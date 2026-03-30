---
name: SPEC-010 Results Display Design Status
description: Tracking design spec and Stitch screen generation for results-display feature (Step 4 wizard)
type: project
---

Design spec for SPEC-010 (results-display) created on 2026-03-30.

**Design spec path:** `.github/design-specs/results-display.design.md`
**Design spec status:** DRAFT (complete, ready for Stitch generation)

**4 screens defined (all pending Stitch generation):**
1. `results-display/estado-no-calculado` -- Empty state (ready + not-ready variants)
2. `results-display/resumen-financiero` -- 3 financial summary cards (hero section)
3. `results-display/desglose-ubicaciones` -- Location breakdown table with coverage accordion
4. `results-display/panel-alertas-incompletas` -- Amber warning panel for incomplete locations

**Why:** This is the final visual deliverable of the quotation wizard (Step 4). It consumes endpoints from SPEC-008 (GET /state) and SPEC-009 (POST /calculate). No new backend work needed.

**How to apply:** When Stitch MCP tools are available, execute generation in 2 batches of 2 screens each using GEMINI_3_FLASH, then apply design system (assetId: 4588898471681033353), then generate final versions with GEMINI_3_1_PRO for resumen-financiero and desglose-ubicaciones.

**Generation plan:**
- Batch 1: estado-no-calculado + resumen-financiero
- Batch 2: desglose-ubicaciones + panel-alertas-incompletas
- After generation: apply_design_system, evaluate with get_screen, edit if needed
- Final versions: resumen-financiero and desglose-ubicaciones with GEMINI_3_1_PRO
