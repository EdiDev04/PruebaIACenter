---
id: SPEC-010
status: DRAFT
feature: results-display
feature_type: full-stack
requires_design_spec: true
has_calculation_logic: false
affects_database: false
consumes_core_ohs: false
has_fe_be_integration: true
created: 2026-03-29
updated: 2026-03-29
author: spec-generator
version: "1.0"
related-specs: ["SPEC-008", "SPEC-009"]
priority: alta
estimated-complexity: L
---

# Spec: Visualización de Resultados y Alertas

> **Estado:** `DRAFT` → aprobar con `status: APPROVED` antes de iniciar implementación.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar la pantalla de resultados del cálculo de primas en el frontend (Step 4 del wizard: `/quotes/{folio}/terms-and-conditions`). Muestra prima neta total, prima comercial antes de IVA, prima comercial total, desglose por ubicación, desglose por cobertura dentro de cada ubicación, y alertas de ubicaciones incompletas. Es el entregable visual principal del cotizador — completa el flujo end-to-end. El backend ya está cubierto: consume `GET /v1/quotes/{folio}/state` (SPEC-008, que incluye `calculationResult`) y `POST /v1/quotes/{folio}/calculate` (SPEC-009).

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-010-01**: Como usuario del cotizador, quiero ver la prima neta total y la prima comercial total de mi cotización después de ejecutar el cálculo.

**Criterios de aceptación (Gherkin):**

- **Dado** que ejecuté el cálculo con 2 ubicaciones calculables
  **Cuando** veo la pantalla de resultados (`/quotes/{folio}/terms-and-conditions`)
  **Entonces** veo 3 tarjetas de resumen: Prima Neta Total, Prima Comercial (sin IVA), Prima Comercial Total (con IVA)
  **Y** los montos están formateados como moneda MXN con 2 decimales (ej: `$125,430.50`)

- **Dado** que el folio no ha sido calculado (`quoteStatus != "calculated"`)
  **Cuando** navego a la pantalla de resultados
  **Entonces** veo un mensaje "Ejecute el cálculo para ver resultados"
  **Y** un botón "Calcular cotización" (si `readyForCalculation == true`)
  **Y** un mensaje adicional "No hay ubicaciones calculables" (si `readyForCalculation == false`)

---

**HU-010-02**: Como usuario del cotizador, quiero ver el desglose de prima por cada ubicación calculada.

**Criterios de aceptación (Gherkin):**

- **Dado** que hay 2 ubicaciones calculables
  **Cuando** veo la sección de desglose
  **Entonces** veo una tabla/lista con: nombre de ubicación, prima neta, estado (badge "Calculable")
  **Y** el total coincide con la prima neta total

---

**HU-010-03**: Como usuario del cotizador, quiero ver el desglose por cobertura dentro de cada ubicación.

**Criterios de aceptación (Gherkin):**

- **Dado** que la ubicación "Bodega Central CDMX" tiene 4 coberturas
  **Cuando** expando esa ubicación
  **Entonces** veo una sub-tabla con: nombre de garantía, suma asegurada, tasa, prima calculada por cobertura
  **Y** puedo verificar que tarifa se aplicó

---

**HU-010-04**: Como usuario del cotizador, quiero ver alertas de ubicaciones incompletas con los campos faltantes para poder completarlas y recalcular.

**Criterios de aceptación (Gherkin):**

- **Dado** que hay 1 ubicación incompleta con `missingFields: ["zipCode", "businessLine.fireKey"]`
  **Cuando** veo la pantalla de resultados
  **Entonces** veo un panel de alertas con la ubicación incompleta
  **Y** la alerta indica los campos faltantes en español
  **Y** puedo hacer clic en "Editar ubicación" para navegar a `/quotes/{folio}/locations`
  **Y** los resultados de las ubicaciones calculables se muestran normalmente

---

**HU-010-05**: Como usuario del cotizador, quiero poder recalcular si modifiqué ubicaciones después del primer cálculo.

**Criterios de aceptación (Gherkin):**

- **Dado** que el folio ya fue calculado y modifiqué una ubicación
  **Cuando** veo la pantalla de resultados
  **Entonces** veo un botón "Recalcular" para re-ejecutar el cálculo
  **Y** el botón de recalcular usa la versión actual del folio

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-010-01 | Solo mostrar resultados si `quoteStatus == "calculated"` | `calculationResult != null` en QuoteStateDto | Mostrar tarjetas y desgloses | REQ-10 |
| RN-010-02 | Si no calculado, mostrar invitación a calcular | `calculationResult == null` | Mensaje + botón "Calcular" (si ready) o "No hay ubicaciones calculables" (si not ready) | REQ-10 |
| RN-010-03 | Montos formateados como MXN, 2 decimales | Formateo de moneda | `$125,430.50` con `Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })` | REQ-10 |
| RN-010-04 | Ubicaciones incompletas en sección de alertas, no junto a las calculables | Separación visual | Panel warning para incompletas, tabla principal para calculables | REQ-10 |
| RN-010-05 | Desglose por cobertura expandible (acordeón) | Click en ubicación | Sub-tabla con guaranteeKey, insuredAmount, rate, premium | REQ-10 |
| RN-010-06 | Alertas tienen link para editar ubicación | Click en "Editar" | Navega a `/quotes/{folio}/locations` | REQ-10 |
| RN-010-07 | Strings de UI en español | — | Todos los labels y mensajes | ADR-008 |
| RN-010-08 | No creación de endpoints nuevos | Backend cubierto por SPEC-008 y SPEC-009 | Consume `GET .../state` + `POST .../calculate` ya definidos | — |

### 2.3 Validaciones

N/A — esta spec no define endpoints. Las validaciones de `POST /calculate` están en SPEC-009.

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack (backend mínimo — solo consume endpoints existentes; UI es el grueso)
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — pantalla de resultados con tarjetas, desgloses y alertas
  Fase 1.5 (core-ohs):       NO APLICA
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — sin cambios de esquema
  Fase 2 backend-developer:  NO APLICA — endpoints ya definidos en SPEC-008 y SPEC-009
  Fase 2 frontend-developer: APLICA — página completa de resultados

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend: SPEC-008 y SPEC-009 deben estar implementadas (endpoints disponibles)
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/results-display.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - ResultsPage (/quotes/{folio}/terms-and-conditions): Resultados financieros + desgloses + alertas

Flujos de usuario a diseñar:
  - Estado "no calculado": mensaje de invitación + botón "Calcular" (o mensaje "no listo")
  - Estado "calculado": 3 tarjetas de resumen (netPremium, beforeTax, commercialPremium)
  - Tabla de desglose por ubicación con estado (badge calculable/incompleta)
  - Acordeón: expandir ubicación → sub-tabla de coberturas (garantía, suma, tasa, prima)
  - Panel lateral de alertas con ubicaciones incompletas
  - Botón "Recalcular" (visible si ya fue calculado)
  - Formateo de moneda MXN
  - Diseño responsive

Inputs de comportamiento que el ux-designer debe conocer:
  - Consume QuoteStateDto que incluye calculationResult cuando status == "calculated"
  - 3 valores financieros: prima neta, prima comercial sin IVA, prima comercial con IVA
  - Ubicaciones incompletas tienen alertas clicables a edición
  - La página es Step 4 del wizard
  - Strings en español
```

### 3.3 Modelo de dominio

**Sin cambios de dominio.** Esta spec es exclusivamente frontend. Consume los DTOs ya definidos en SPEC-008 (`QuoteStateDto`, `CalculationResultDto`, `LocationPremiumDto`, `CoveragePremiumDto`).

### 3.4 Contratos API (backend)

**Sin endpoints nuevos.** Esta spec consume:

| Endpoint | Definido en | Propósito en esta spec |
|---|---|---|
| `GET /v1/quotes/{folio}/state` | SPEC-008 | Leer estado + `calculationResult` al entrar a la página |
| `POST /v1/quotes/{folio}/calculate` | SPEC-009 | Trigger desde botón "Calcular" o "Recalcular" |

### 3.5 Contratos core-ohs consumidos

N/A — sin consumo directo de core-ohs.

### 3.5b Contratos FE ↔ BE

```
GET /v1/quotes/{folio}/state
Consumido por:
  Archivo FE:    entities/quote-state/api/quoteStateApi.ts (ya creado en SPEC-008)
  Hook/Query:    useQuoteStateQuery (ya creado en SPEC-008)
  Query Key:     ['quote-state', folio]

Uso en esta spec:
  - Al entrar a ResultsPage, lee QuoteStateDto
  - Si calculationResult != null → renderiza resultados
  - Si calculationResult == null → renderiza invitación a calcular
```

```
POST /v1/quotes/{folio}/calculate
Consumido por:
  Archivo FE:    features/calculate-quote/model/useCalculateQuote.ts (ya creado en SPEC-009)

Uso en esta spec:
  - Botón "Calcular" ejecuta mutation
  - Botón "Recalcular" ejecuta la misma mutation con version actual
  - Al éxito, invalida ['quote-state', folio] → re-renderiza con resultados
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── pages/
│   └── ResultsPage.tsx                             # CREAR — Step 4: /quotes/{folio}/terms-and-conditions
│   └── ResultsPage.module.css                      # CREAR
├── widgets/
│   ├── financial-summary/
│   │   ├── index.ts                                # CREAR — Public API
│   │   └── ui/
│   │       ├── FinancialSummary.tsx                # CREAR — 3 tarjetas: netPremium, beforeTax, commercialPremium
│   │       └── FinancialSummary.module.css         # CREAR
│   ├── location-breakdown/
│   │   ├── index.ts                                # CREAR — Public API
│   │   └── ui/
│   │       ├── LocationBreakdown.tsx               # CREAR — Tabla de ubicaciones con prima neta y badge de estado
│   │       ├── LocationBreakdown.module.css        # CREAR
│   │       ├── CoverageAccordion.tsx               # CREAR — Sub-tabla de coberturas expandible
│   │       └── CoverageAccordion.module.css        # CREAR
│   └── incomplete-alerts/
│       ├── index.ts                                # CREAR — Public API
│       └── ui/
│           ├── IncompleteAlerts.tsx                 # CREAR — Panel de alertas con links a edición
│           └── IncompleteAlerts.module.css          # CREAR
├── shared/
│   └── lib/
│       └── formatCurrency.ts                       # CREAR — Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })
└── app/
    └── router/
        └── router.tsx                              # MODIFICAR — agregar ruta /quotes/:folio/terms-and-conditions
```

**Props/hooks por componente:**

| Componente | Props | Hooks | Acción |
|---|---|---|---|
| `ResultsPage` | — | `useParams()`, `useQuoteStateQuery(folio)`, `useCalculateQuote()` | Ensambla: FinancialSummary + LocationBreakdown + IncompleteAlerts + CalculateButton |
| `FinancialSummary` | `netPremium: number`, `commercialPremiumBeforeTax: number`, `commercialPremium: number` | — | Renderiza 3 tarjetas con `formatCurrency()` |
| `LocationBreakdown` | `premiumsByLocation: LocationPremiumDto[]` | — | Tabla con nombre, prima, badge de estado; cada fila expandible |
| `CoverageAccordion` | `coveragePremiums: CoveragePremiumDto[]` | — | Sub-tabla: garantía, suma asegurada, tasa, prima |
| `IncompleteAlerts` | `alerts: LocationAlertDto[]`, `folio: string` | `useNavigate` | Panel warning con campos faltantes + link "Editar ubicación" |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['quote-state', folio]` | `QuoteStateDto` (reutiliza SPEC-008 query) | staleTime: 0; invalida tras POST calculate |

No se crean queries ni mutations nuevas — se reutilizan `useQuoteStateQuery` (SPEC-008) y `useCalculateQuote` (SPEC-009).

### 3.8 Persistencia MongoDB

N/A — sin escrituras. Todo se lee vía `GET /v1/quotes/{folio}/state`.

---

## 4. LÓGICA DE CÁLCULO

N/A — no calcula primas. La lógica de presentación es:

```
SI quoteState.calculationResult != null:
  MOSTRAR FinancialSummary(calculationResult.netPremium, .commercialPremiumBeforeTax, .commercialPremium)
  MOSTRAR LocationBreakdown(calculationResult.premiumsByLocation WHERE validationStatus == "calculable")
  MOSTRAR IncompleteAlerts(quoteState.locations.alerts)
  MOSTRAR botón "Recalcular" con version: quoteState.version
SINO:
  SI quoteState.readyForCalculation:
    MOSTRAR mensaje "Ejecute el cálculo para ver resultados"
    MOSTRAR botón "Calcular cotización" con version: quoteState.version
  SINO:
    MOSTRAR mensaje "No hay ubicaciones calculables. Complete al menos una ubicación."
    MOSTRAR link "Ir a ubicaciones" → /quotes/{folio}/locations
```

---

## 5. MODELO DE DATOS

N/A — sin cambios de esquema ni colecciones afectadas.

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-010-01 | Resultados viven en Step 4 (`/quotes/{folio}/terms-and-conditions`), no en `/technical-info` (Step 3) | ADR-005 asigna Step 3 a coverage-options y Step 4 a terms/result | Si se cambia el mapping de steps, ajustar la ruta | usuario |
| SUP-010-02 | No se crea endpoint backend nuevo — consume `GET .../state` (SPEC-008) y `POST .../calculate` (SPEC-009) | Evita duplicación de endpoints; QuoteStateDto ya incluye calculationResult optionalmente | Si se necesita un response shape diferente, considerar DTO wrapper en FE | usuario |
| SUP-010-03 | `formatCurrency` usa `Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })` | Formato estándar para pesos mexicanos | Si el reto requiere otro formato, ajustar locale | spec-generator |
| SUP-010-04 | Desglose por cobertura muestra `rate` además de `premium` para trazabilidad | El usuario puede verificar que tarifa se aplicó | Si no se quiere mostrar la tasa, el campo existe pero el diseño puede ocultarlo | spec-generator |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        └── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
                │
                └── [test-engineer-frontend]  (Fase 3)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/results-display.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` + `backend-developer` (SPEC-008 + SPEC-009) | Design spec APPROVED + endpoints de state y calculate disponibles |
| `test-engineer-frontend` | `frontend-developer` | Implementación completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-008 | quote-state-progress | depende-de (QuoteStateDto con calculationResult, useQuoteStateQuery) |
| SPEC-009 | premium-calculation-engine | depende-de (POST calculate endpoint, useCalculateQuote mutation) |

---

## 8. LISTA DE TAREAS

### 8.1 backend-developer

No hay tareas backend — endpoints cubiertos en SPEC-008 y SPEC-009.

### 8.2 frontend-developer

- [ ] Crear `pages/ResultsPage.tsx` — ensambla FinancialSummary + LocationBreakdown + IncompleteAlerts + CalculateButton (SPEC-009)
- [ ] Crear `pages/ResultsPage.module.css`
- [ ] Crear `widgets/financial-summary/` — 3 tarjetas (prima neta, antes de IVA, comercial total) con formatCurrency
- [ ] Crear `widgets/location-breakdown/` — tabla de ubicaciones con prima neta y badge de estado
- [ ] Crear `widgets/location-breakdown/CoverageAccordion.tsx` — sub-tabla expandible de coberturas (garantía, suma, tasa, prima)
- [ ] Crear `widgets/incomplete-alerts/` — panel warning con campos faltantes + link "Editar ubicación"
- [ ] Crear `shared/lib/formatCurrency.ts` — `Intl.NumberFormat('es-MX', { style: 'currency', currency: 'MXN' })`
- [ ] Agregar ruta `/quotes/:folio/terms-and-conditions` en `app/router/router.tsx` → `ResultsPage`
- [ ] Lógica de estados:
  - `calculationResult != null` → mostrar resultados + botón "Recalcular"
  - `calculationResult == null && readyForCalculation` → mensaje + botón "Calcular"
  - `calculationResult == null && !readyForCalculation` → mensaje + link a ubicaciones
- [ ] Labels y strings en español (ADR-008)
- [ ] Garantizar que tras `POST calculate` exitoso, invalidar `['quote-state', folio]` → UI se actualiza automáticamente

### 8.3 test-engineer-frontend

- [ ] `ResultsPage.test.tsx` — folio no calculado + readyForCalculation true → muestra botón "Calcular"
- [ ] `ResultsPage.test.tsx` — folio no calculado + readyForCalculation false → muestra "No hay ubicaciones calculables" + link a ubicaciones
- [ ] `ResultsPage.test.tsx` — folio calculado → muestra 3 tarjetas, desglose, alertas
- [ ] `FinancialSummary.test.tsx` — renderiza 3 tarjetas con formato MXN correcto
- [ ] `LocationBreakdown.test.tsx` — renderiza tabla con ubicaciones calculables
- [ ] `CoverageAccordion.test.tsx` — expandir ubicación muestra sub-tabla de coberturas
- [ ] `IncompleteAlerts.test.tsx` — renderiza alertas con campos faltantes
- [ ] `IncompleteAlerts.test.tsx` — click en "Editar" navega a ubicaciones
- [ ] `formatCurrency.test.ts` — formatea 125430.50 → "$125,430.50"

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready):**
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED`
- [ ] SPEC-008 implementada (GET /state con calculationResult)
- [ ] SPEC-009 implementada (POST /calculate funcional)

**DoD (Definition of Done):**
- [ ] Página `/quotes/{folio}/terms-and-conditions` funcional (Step 4 del wizard)
- [ ] 3 estados de la página correctamente renderizados:
  - No calculado + ready → botón "Calcular"
  - No calculado + not ready → mensaje + link a ubicaciones
  - Calculado → tarjetas + desgloses + alertas + botón "Recalcular"
- [ ] 3 tarjetas de resumen financiero con formato MXN
- [ ] Tabla de desglose por ubicación con prima neta y badge de estado
- [ ] Acordeón de coberturas expandible con garantía, suma, tasa, prima
- [ ] Panel de alertas con ubicaciones incompletas y link "Editar"
- [ ] Botón "Recalcular" funcional (ejecuta POST calculate con version actual)
- [ ] Tras calcular, resultados se actualizan automáticamente (invalidación de cache)
- [ ] Strings en español (ADR-008)
- [ ] Tests unitarios FE pasando
- [ ] Sin violaciones de reglas FSD
