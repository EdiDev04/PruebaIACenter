---
name: SPEC-005 Layout Config Design Status
description: Design spec for location-layout-configuration created on 2026-03-29, pending Stitch screen generation
type: project
---

SPEC-005 location-layout-configuration design spec completed (Phases 0-3).

**Why:** Feature requires 3 screens in Stitch (panel default, panel personalizado, panel error conflicto). The Stitch MCP tools were not available in the current session.

**How to apply:** When Stitch MCP is connected, execute Fase 4 using the 3 prompts in Section 6 of the design spec. Generate with GEMINI_3_FLASH first, apply design system (assetId: 4588898471681033353), then generate final versions with GEMINI_3_1_PRO. Update .stitch-config.json with resulting screenIds.

Screens pending:
1. layout-config/panel-default -- Panel with default config (grid mode, 5 columns)
2. layout-config/panel-personalizado -- Panel with custom config (list mode, 8 columns, all groups expanded)
3. layout-config/panel-error-conflicto -- Panel showing version conflict error (409)
