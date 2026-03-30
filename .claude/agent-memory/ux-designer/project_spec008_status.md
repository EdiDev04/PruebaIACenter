---
name: SPEC-008 Quote State Progress Design
description: Design spec for quote state progress widgets (ProgressBar + LocationAlerts) -- created 2026-03-29, pending Stitch generation
type: project
---

SPEC-008 design spec created at `.github/design-specs/quote-state-progress.design.md` on 2026-03-29.

**Widgets designed:**
1. **ProgressBar** -- horizontal bar with 4 sections (Datos Generales, Layout, Ubicaciones, Opciones de Cobertura) + checkmarks + connecting lines
2. **LocationAlerts** -- collapsible panel with amber alerts for incomplete locations, missing field chips, "Ir a editar" navigation links
3. **ReadyBanner** -- subtle banner showing calculability status
4. **QuoteStatusBadge** -- pill badge for draft/in_progress/calculated

**Stitch screens to generate (2):**
- `quote-state-progress/wizard-layout-progressbar` -- full wizard layout with integrated progress bar
- `quote-state-progress/panel-location-alerts` -- alerts panel showing 3 states (with alerts, no alerts, no locations)

**Key design decisions:**
- Amber for incomplete (never red) -- incomplete is informative, not an error
- Positive framing: "X listas para calcular" instead of "X faltan datos"
- Progress derived from persisted data, not wizard navigation
- Panel auto-expands when alerts exist, collapses when none

**Why:** This is a cross-cutting widget visible on all wizard pages. Must be non-intrusive but informative.

**How to apply:** When generating Stitch screens, use prompts from Section 6. Apply design system after generation. Two screens only, generate in single batch.
