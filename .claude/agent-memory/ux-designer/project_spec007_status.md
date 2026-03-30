---
name: SPEC-007 Coverage Options Design Status
description: Design spec for coverage options configuration (wizard step 3) - created, pending Stitch screen generation
type: project
---

SPEC-007 Coverage Options Configuration design spec created on 2026-03-29.

**Design spec**: `.github/design-specs/coverage-options-configuration.design.md` (status: DRAFT)

**Screens planned** (3 screens, all pending Stitch generation):
1. `formulario-principal` -- Main form with 4 grouped checkbox sections + deductible/coinsurance inputs
2. `warning-deshabilitacion` -- Confirmation dialog when disabling a guarantee used in locations
3. `error-conflicto-version` -- Version conflict (409) state with disabled form overlay

**Why:** Stitch MCP tools were not available during initial session. The design spec contains complete prompts in Section 6 ready for generation.

**How to apply:** When Stitch MCP is available, execute Phase 4 of the ux-designer workflow using the 3 prompts in Section 6. Generate in batches of 2, apply design system (assetId: 4588898471681033353), then refine. Update .stitch-config.json with actual screenIds.

**Key design decisions:**
- Single-step form (no progressive disclosure needed -- only 2 numeric inputs + grouped checkboxes)
- 14 guarantees grouped into 4 cognitive categories (Miller's Law): fire(3), cat(2), additional(4), special(5)
- Smart defaults: all 14 guarantees enabled by default (opt-out pattern)
- Amber for warnings/conflicts, never red (incomplete != error)
- Loss aversion dialog when disabling guarantee used in existing locations
