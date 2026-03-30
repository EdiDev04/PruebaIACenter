---
id: SPEC-008
status: IMPLEMENTED
feature: quote-state-progress
feature_type: full-stack
requires_design_spec: true
has_calculation_logic: false
affects_database: false
consumes_core_ohs: false
has_fe_be_integration: true
created: 2026-03-29
updated: 2026-03-30
implemented: 2026-03-30
author: spec-generator
version: "1.1"
related-specs: ["SPEC-002", "SPEC-003", "SPEC-006", "SPEC-007", "SPEC-009"]
priority: alta
estimated-complexity: M
---

# Spec: Estado y Progreso de la Cotización

> **Estado:** `APPROVED` — lista para implementación.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar un endpoint de estado global del folio que consolida: progreso por sección (derivado de datos persistidos, no del step del wizard), resumen de ubicaciones (calculables vs incompletas con alertas), flag `readyForCalculation`, y resultado financiero opcional (cuando el folio ya fue calculado). El frontend consume este endpoint para mostrar una barra de progreso visible en todas las páginas del wizard y badges informativos de estado. Este feature sirve como punto de observabilidad del folio antes, durante y después del cálculo.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-008-01**: Como usuario del cotizador, quiero ver el estado actual de mi cotización (`draft`, `in_progress`, `calculated`) para saber en qué punto del flujo me encuentro.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio `DAN-2026-00001` recién creado sin datos guardados
  **Cuando** consulto `GET /v1/quotes/DAN-2026-00001/state`
  **Entonces** `quoteStatus` es `"draft"`, todos los flags de progreso son `false` excepto `layoutConfiguration` que es `true` (defaults)
  **Y** `readyForCalculation` es `false`

- **Dado** que tengo un folio con datos generales y 2 ubicaciones (1 calculable, 1 incompleta)
  **Cuando** consulto el estado
  **Entonces** `quoteStatus` es `"in_progress"`, `progress.generalInfo` es `true`, `progress.locations` es `true`
  **Y** `locations.calculable` es 1, `locations.incomplete` es 1
  **Y** `readyForCalculation` es `true` (hay al menos 1 calculable)

---

**HU-008-02**: Como usuario del cotizador, quiero ver un indicador de progreso que muestre qué secciones del folio están completas y cuáles faltan.

**Criterios de aceptación (Gherkin):**

- **Dado** que estoy en cualquier página del wizard (`/general-info`, `/locations`, `/technical-info`, `/terms-and-conditions`)
  **Cuando** se carga la página
  **Entonces** veo un indicador de progreso con checkmarks por sección: Datos Generales, Layout, Ubicaciones, Opciones de Cobertura

- **Dado** que solo completé datos generales
  **Cuando** veo el indicador
  **Entonces** `Datos Generales` tiene checkmark verde, las demás secciones están pendientes (sin checkmark)

---

**HU-008-03**: Como usuario del cotizador, quiero ver alertas de ubicaciones incompletas que indiquen qué datos faltan, sin bloquear la navegación.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo 1 ubicación incompleta con `missingFields: ["zipCode", "businessLine.fireKey"]`
  **Cuando** veo el estado
  **Entonces** la alerta muestra el nombre de la ubicación y los campos faltantes
  **Y** puedo hacer clic para navegar a la edición de esa ubicación

- **Dado** que todas las ubicaciones son calculables
  **Cuando** veo el estado
  **Entonces** no hay alertas de ubicaciones incompletas

---

**HU-008-04**: Como usuario del cotizador, quiero saber cuántas ubicaciones son calculables antes de ejecutar el cálculo.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo 3 ubicaciones: 2 calculables y 1 incompleta
  **Cuando** consulto el estado
  **Entonces** veo `calculable: 2`, `incomplete: 1`, `total: 3`
  **Y** `readyForCalculation: true`

- **Dado** que no tengo ubicaciones
  **Cuando** consulto el estado
  **Entonces** `total: 0`, `calculable: 0`, `readyForCalculation: false`

---

**HU-008-05**: Como usuario del cotizador, quiero que al consultar el estado después de calcular, el resultado financiero venga incluido para no hacer una segunda llamada.

**Criterios de aceptación (Gherkin):**

- **Dado** que el folio fue calculado (`quoteStatus: "calculated"`)
  **Cuando** consulto `GET /v1/quotes/{folio}/state`
  **Entonces** `calculationResult` no es null y contiene `netPremium`, `commercialPremiumBeforeTax`, `commercialPremium`, `premiumsByLocation`

- **Dado** que el folio NO fue calculado (`quoteStatus: "draft"` o `"in_progress"`)
  **Cuando** consulto el estado
  **Entonces** `calculationResult` es `null`

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-008-01 | Progreso derivado del dato persistido | Consulta de estado | Evalúa presencia de datos reales, no `lastWizardStep` | SUP-008-01 |
| RN-008-02 | `generalInfo` es `true` si `InsuredData.Name` no vacío | `string.IsNullOrWhiteSpace(InsuredData.Name)` | `true` / `false` | bussines-context.md §4 |
| RN-008-03 | `layoutConfiguration` es `true` si `generalInfo` es `true` | Requiere datos generales completados | `true` / `false` | AMENDMENT-008-01 |
| RN-008-04 | `locations` es `true` si `Locations.Count > 0` | Array con al menos 1 ubicación | `true` / `false` | SPEC-006 |
| RN-008-05 | `coverageOptions` es `true` si `CoverageOptions.EnabledGuarantees.Count > 0` | Whitelist no vacía | `true` / `false` | SUP-008-01 (ajustado) |
| RN-008-06 | `readyForCalculation` es `true` si hay ≥1 ubicación calculable | Evalúa `validationStatus == "calculable"` | `true` / `false` | REQ-08 |
| RN-008-07 | `quoteStatus` transiciona: `draft` → `in_progress` → `calculated` | Al guardar primera sección → `in_progress`; post-cálculo → `calculated` | Estado derivado del dato | bussines-context.md §10 |
| RN-008-08 | Alertas son informativas — nunca bloquean | Ubicación incompleta | Se muestra alerta con campos faltantes | bussines-context.md §1 |
| RN-008-09 | `calculationResult` solo se incluye si `quoteStatus == "calculated"` | Condición en el Use Case | Null o poblado | SUP-008-06 |
| RN-008-10 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | Wrapper obligatorio | architecture-decisions.md |
| RN-008-11 | Mensajes de error en español | Toda respuesta de error | Campo `message` en español | ADR-008 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `folio` (path) | Formato `DAN-YYYY-NNNNN` | "Formato de folio inválido. Use DAN-YYYY-NNNNN" | Sí (400) |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — barra de progreso visible en todas las páginas del wizard
  Fase 1.5 (core-ohs):       NO APLICA — datos derivados del folio persistido
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — sin cambios de esquema
  Fase 2 backend-developer:  APLICA — nuevo Use Case + endpoint
  Fase 2 frontend-developer: APLICA — widget de progreso, badges, alertas

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/quote-state-progress.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - WizardLayout (todas las rutas /quotes/{folio}/*): Barra de progreso persistente

Flujos de usuario a diseñar:
  - Barra con 4 secciones (Datos Generales, Layout, Ubicaciones, Opciones Cobertura) + checkmarks
  - Badge con conteo de ubicaciones calculables/incompletas
  - Click en alerta → navegar a edición de ubicación
  - Indicador de "Listo para calcular" visible antes de Step 4

Inputs de comportamiento que el ux-designer debe conocer:
  - Progreso derivado de datos, no de navegación del wizard
  - Alertas informativas (warning), no bloqueantes
  - El estado se consulta al entrar a cada página (no polling)
  - Strings en español (ADR-008)
```

### 3.3 Modelo de dominio

**No se crean ni modifican entidades de dominio.** El estado se calcula en el Use Case a partir del `PropertyQuote` existente.

**Nuevos DTOs en Application:**

```csharp
// Cotizador.Application/DTOs/QuoteStateDto.cs
public record QuoteStateDto(
    string FolioNumber,
    string QuoteStatus,
    int Version,
    ProgressDto Progress,
    LocationsStateDto Locations,
    bool ReadyForCalculation,
    CalculationResultDto? CalculationResult
);

// Cotizador.Application/DTOs/ProgressDto.cs
public record ProgressDto(
    bool GeneralInfo,
    bool LayoutConfiguration,
    bool Locations,
    bool CoverageOptions
);

// Cotizador.Application/DTOs/LocationsStateDto.cs
public record LocationsStateDto(
    int Total,
    int Calculable,
    int Incomplete,
    List<LocationAlertDto> Alerts
);

// Cotizador.Application/DTOs/LocationAlertDto.cs
public record LocationAlertDto(
    int Index,
    string LocationName,
    List<string> MissingFields
);

// Cotizador.Application/DTOs/CalculationResultDto.cs
public record CalculationResultDto(
    decimal NetPremium,
    decimal CommercialPremiumBeforeTax,
    decimal CommercialPremium,
    List<LocationPremiumDto> PremiumsByLocation
);

// Cotizador.Application/DTOs/LocationPremiumDto.cs
public record LocationPremiumDto(
    int LocationIndex,
    string LocationName,
    decimal NetPremium,
    string ValidationStatus,
    List<CoveragePremiumDto> CoveragePremiums
);

// Cotizador.Application/DTOs/CoveragePremiumDto.cs
public record CoveragePremiumDto(
    string GuaranteeKey,
    decimal InsuredAmount,
    decimal Rate,
    decimal Premium
);
```

### 3.4 Contratos API (backend)

```
GET /v1/quotes/{folio}/state
Propósito: Estado completo del folio con progreso, ubicaciones, readyForCalculation y resultado financiero opcional
Auth: Basic Auth ([Authorize])
Use Case: GetQuoteStateUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001

Response 200 (folio in_progress, sin cálculo):
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "in_progress",
    "version": 5,
    "progress": {
      "generalInfo": true,
      "layoutConfiguration": true,
      "locations": true,
      "coverageOptions": true
    },
    "locations": {
      "total": 3,
      "calculable": 2,
      "incomplete": 1,
      "alerts": [
        {
          "index": 3,
          "locationName": "Local sin CP",
          "missingFields": ["zipCode", "businessLine.fireKey"]
        }
      ]
    },
    "readyForCalculation": true,
    "calculationResult": null
  }
}

Response 200 (folio calculado):
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "calculated",
    "version": 8,
    "progress": {
      "generalInfo": true,
      "layoutConfiguration": true,
      "locations": true,
      "coverageOptions": true
    },
    "locations": {
      "total": 3,
      "calculable": 2,
      "incomplete": 1,
      "alerts": [
        { "index": 3, "locationName": "Local sin CP", "missingFields": ["zipCode", "businessLine.fireKey"] }
      ]
    },
    "readyForCalculation": true,
    "calculationResult": {
      "netPremium": 125000.50,
      "commercialPremiumBeforeTax": 150000.60,
      "commercialPremium": 174000.70,
      "premiumsByLocation": [
        {
          "locationIndex": 1,
          "locationName": "Bodega Central CDMX",
          "netPremium": 85000.30,
          "validationStatus": "calculable",
          "coveragePremiums": [
            { "guaranteeKey": "building_fire", "insuredAmount": 5000000, "rate": 0.00125, "premium": 6250.00 }
          ]
        }
      ]
    }
  }
}

Response 200 (folio draft):
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "draft",
    "version": 1,
    "progress": {
      "generalInfo": false,
      "layoutConfiguration": true,
      "locations": false,
      "coverageOptions": false
    },
    "locations": {
      "total": 0,
      "calculable": 0,
      "incomplete": 0,
      "alerts": []
    },
    "readyForCalculation": false,
    "calculationResult": null
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Contratos core-ohs consumidos

N/A — este feature no consume core-ohs. Todo se deriva del `PropertyQuote` persistido.

### 3.5b Contratos FE ↔ BE

```
GET /v1/quotes/{folio}/state
Consumido por:
  Archivo FE:    entities/quote-state/api/quoteStateApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['quote-state', folio]

Response BE → FE (200):
  { "data": { ...QuoteStateDto } }

Errores manejados por el FE:
  - 404: notificación "El folio no existe"
  - 500: notificación genérica

Invalidación de caché:
  - staleTime: 0 (siempre fresco — dato mutable)
  - Invalidar al mutar cualquier sección (PUT general-info, PUT locations, PUT coverage-options, POST calculate)
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── entities/
│   └── quote-state/
│       ├── index.ts                           # CREAR — Public API
│       ├── model/
│       │   ├── types.ts                       # CREAR — QuoteStateDto, ProgressDto, LocationAlertDto, CalculationResultDto
│       │   └── useQuoteStateQuery.ts          # CREAR — useQuery(['quote-state', folio], staleTime: 0)
│       └── api/
│           └── quoteStateApi.ts               # CREAR — getQuoteState() → GET /v1/quotes/{folio}/state
├── widgets/
│   ├── progress-bar/
│   │   ├── index.ts                           # CREAR — Public API
│   │   └── ui/
│   │       ├── ProgressBar.tsx                # CREAR — Barra horizontal con 4 secciones + checkmarks
│   │       └── ProgressBar.module.css         # CREAR
│   └── location-alerts/
│       ├── index.ts                           # CREAR — Public API
│       └── ui/
│           ├── LocationAlerts.tsx             # CREAR — Panel de alertas con links a edición
│           └── LocationAlerts.module.css      # CREAR
├── shared/
│   └── api/
│       └── endpoints.ts                       # MODIFICAR — agregar ruta de state
└── widgets/
    ├── WizardLayout.tsx                       # MODIFICAR — integrar ProgressBar
    └── WizardHeader.tsx                       # MODIFICAR — mostrar badge de calculabilidad
```

**Props/hooks por componente:**

| Componente | Props | Hooks | Acción |
|---|---|---|---|
| `ProgressBar` | `progress: ProgressDto` | — | Renderiza 4 secciones con checkmarks |
| `LocationAlerts` | `alerts: LocationAlertDto[]`, `folio: string` | `useNavigate` | Muestra alertas, click navega a ubicación |
| `WizardLayout` | — | `useQuoteStateQuery(folio)` | Integra ProgressBar + badge calculabilidad |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['quote-state', folio]` | `QuoteStateDto` | staleTime: 0; invalidar al mutar cualquier sección |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read | `property_quotes` | `Find` | `{ folioNumber }` | Full document | `folioNumber_1` (existing) |

- **Sin escrituras** — este feature es solo lectura.
- El estado se calcula dinámicamente a partir del documento completo.

---

## 4. LÓGICA DE CÁLCULO

N/A — no calcula primas. La lógica es derivación de progreso:

```
FUNCIÓN calcularProgreso(quote: PropertyQuote) → ProgressDto:
  generalInfo = !string.IsNullOrWhiteSpace(quote.InsuredData.Name)
  layoutConfiguration = generalInfo  // se activa cuando datos generales están completos (AMENDMENT-008-01)
  locations = quote.Locations.Count > 0
  coverageOptions = quote.CoverageOptions.EnabledGuarantees.Count > 0
  RETORNA { generalInfo, layoutConfiguration, locations, coverageOptions }

FUNCIÓN calcularEstadoUbicaciones(quote: PropertyQuote) → LocationsStateDto:
  total = quote.Locations.Count
  calculable = CONTAR donde loc.ValidationStatus == "calculable"
  incomplete = total - calculable
  alerts = PARA CADA loc DONDE loc.ValidationStatus == "incomplete":
    { index: loc.Index, locationName: loc.LocationName, missingFields: loc.BlockingAlerts }
  RETORNA { total, calculable, incomplete, alerts }

readyForCalculation = calculable > 0

calculationResult =
  SI quote.QuoteStatus == "calculated":
    MAPEAR quote.NetPremium, quote.CommercialPremiumBeforeTax, quote.CommercialPremium, quote.PremiumsByLocation
  SINO:
    null
```

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

Ninguna modificada. Solo lectura de `property_quotes`.

### 5.2 Cambios de esquema

Ninguno. El campo `CommercialPremiumBeforeTax` se agrega en SPEC-009 (motor de cálculo), no aquí.

### 5.3 Índices requeridos

Ninguno nuevo. Usa `folioNumber_1` existente.

### 5.4 Datos semilla

Ninguno.

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-008-01 | Progreso derivado de datos persistidos: `generalInfo` = Name no vacío, `layoutConfiguration` = siempre true, `locations` = Count > 0, `coverageOptions` = `EnabledGuarantees.Count > 0`. `LastWizardStep` es auxiliar de UX, no fuente de verdad | Derivar de datos reales es agnóstico al wizard y funciona con API directa, tests y futuros agentes | Si se requiere tracking de "visitó paso" sin guardar datos, agregar flag independiente | usuario |
| SUP-008-02 | `calculationResult` se incluye en `QuoteStateDto` cuando `quoteStatus == "calculated"`, evitando endpoint separado | Reduce llamadas HTTP al entrar a Step 4 | Si el resultado financiero crece mucho en tamaño, considerar endpoint separado con paginación | usuario |
| SUP-008-03 | `missingFields` se derivan de `BlockingAlerts` ya calculados por `LocationCalculabilityEvaluator` (SPEC-006) | Reutiliza lógica existente sin duplicar | Si las alertas cambian de formato, ambos endpoints se afectan | spec-generator |
| SUP-008-04 | El estado se consulta al navegar (no polling). Cada página del wizard hace `useQuoteStateQuery(folio)` al montar | Evita tráfico innecesario; el dato es fresco al navegar | Si dos tabs editan el mismo folio, una no verá cambios hasta que navegue | spec-generator |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        ├── [backend-developer]  (Fase 2)
        └── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
                │
                ├── [test-engineer-backend]   (Fase 3)
                └── [test-engineer-frontend]  (Fase 3)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/quote-state-progress.spec.md` → `status: APPROVED` |
| `backend-developer` | `spec-generator` | `specs/quote-state-progress.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` | `design-specs/quote-state-progress.design.md` → `status: APPROVED` |
| `test-engineer-backend` | `backend-developer` | Implementación backend completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación frontend completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-002 | quote-data-model | depende-de (PropertyQuote aggregate) |
| SPEC-003 | folio-creation | depende-de (folio debe existir) |
| SPEC-006 | location-management | depende-de (ubicaciones con ValidationStatus y BlockingAlerts) |
| SPEC-007 | coverage-options-configuration | depende-de (CoverageOptions.EnabledGuarantees) |
| SPEC-009 | premium-calculation-engine | afecta (calculationResult viene del resultado financiero de SPEC-009) |

---

## 8. LISTA DE TAREAS

### 8.1 backend-developer

- [ ] Crear DTOs: `QuoteStateDto`, `ProgressDto`, `LocationsStateDto`, `LocationAlertDto`, `CalculationResultDto`, `LocationPremiumDto`, `CoveragePremiumDto`
- [ ] Crear `IGetQuoteStateUseCase` interface en `Cotizador.Application/Interfaces/`
- [ ] Implementar `GetQuoteStateUseCase` en `Cotizador.Application/UseCases/`
  - Lee PropertyQuote completo
  - Calcula progress con RN-008-02 a RN-008-05
  - Calcula locations state con alertas
  - Evalúa readyForCalculation
  - Incluye calculationResult si status == "calculated"
- [ ] Agregar endpoint `GET /v1/quotes/{folio}/state` en `QuoteController`
- [ ] Registrar `IGetQuoteStateUseCase` → `GetQuoteStateUseCase` en `Program.cs`
- [ ] Mensajes de error en español (ADR-008)

### 8.2 frontend-developer

- [ ] Crear `entities/quote-state/` — types, query (staleTime: 0), api
- [ ] Crear `widgets/progress-bar/` — barra horizontal con 4 secciones + checkmarks
- [ ] Crear `widgets/location-alerts/` — panel de alertas con links a edición
- [ ] Modificar `WizardLayout.tsx` — integrar ProgressBar con datos de `useQuoteStateQuery`
- [ ] Modificar `WizardHeader.tsx` — badge con conteo calculables/incompletas
- [ ] Invalidar `['quote-state', folio]` en todas las mutaciones existentes (general-info, locations, layout, coverage-options)
- [ ] Agregar ruta en `shared/api/endpoints.ts`
- [ ] Labels en español (ADR-008)

### 8.3 test-engineer-backend

- [ ] `GetQuoteStateUseCaseTests` — folio draft → todos progress false excepto layout; no calculationResult
- [ ] `GetQuoteStateUseCaseTests` — folio in_progress con datos generales → generalInfo true
- [ ] `GetQuoteStateUseCaseTests` — folio con ubicaciones → locations true, conteo correcto
- [ ] `GetQuoteStateUseCaseTests` — folio con enabledGuarantees > 0 → coverageOptions true
- [ ] `GetQuoteStateUseCaseTests` — folio con enabledGuarantees vacío → coverageOptions false
- [ ] `GetQuoteStateUseCaseTests` — 1 calculable + 1 incompleta → readyForCalculation true, alertas correctas
- [ ] `GetQuoteStateUseCaseTests` — 0 calculables → readyForCalculation false
- [ ] `GetQuoteStateUseCaseTests` — folio calculado → calculationResult no null con datos financieros
- [ ] `GetQuoteStateUseCaseTests` — folio inexistente → throws FolioNotFoundException

### 8.4 test-engineer-frontend

- [ ] `ProgressBar.test.tsx` — renderiza 4 secciones con checkmarks según progress
- [ ] `LocationAlerts.test.tsx` — renderiza alertas con campos faltantes
- [ ] `LocationAlerts.test.tsx` — click en alerta navega a ubicación
- [ ] `useQuoteStateQuery.test.ts` — fetch correcto, staleTime 0
- [ ] `WizardLayout.test.tsx` — integra ProgressBar con datos del query

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready):**
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED`
- [ ] SPEC-003 implementada (folio existe)
- [ ] SPEC-006 implementada (ubicaciones con validationStatus + blockingAlerts)

**DoD (Definition of Done):**
- [ ] `GET /v1/quotes/{folio}/state` responde según contrato §3.4 (3 variantes de response)
- [ ] Progress derivado de datos persistidos (no de lastWizardStep)
- [ ] `coverageOptions` correctamente evaluado con `EnabledGuarantees.Count > 0`
- [ ] `readyForCalculation` refleja ≥1 ubicación calculable
- [ ] `calculationResult` incluido solo cuando `quoteStatus == "calculated"`
- [ ] ProgressBar visible en todas las páginas del wizard
- [ ] Location alerts clickeables navegan a ubicación
- [ ] States se invalida al mutar cualquier sección
- [ ] Mensajes de error en español
- [ ] Tests unitarios BE y FE pasando
- [ ] Sin violaciones de Clean Architecture
- [ ] Sin violaciones de reglas FSD

---

## 10. AMENDMENTS

### AMENDMENT-008-01 — Layout condicionado a Datos Generales (2026-03-30)

**Cambio:** RN-008-03 modificada. `layoutConfiguration` ya no es siempre `true`. Ahora depende de `generalInfo`.

**Razón:** En un folio recién creado, mostrar Layout como completado sin tener Datos Generales genera ruido visual en el ProgressBar. El usuario ve un checkmark verde aislado sin contexto, lo cual es confuso.

**Regla anterior:**
```
layoutConfiguration = true  // siempre, defaults desde creación
```

**Regla nueva:**
```
layoutConfiguration = generalInfo  // se activa cuando datos generales están completos
```

**Archivos modificados:**
- `Cotizador.Application/UseCases/GetQuoteStateUseCase.cs` — `CalculateProgress()`

**Impacto:** Folio draft ahora muestra 4 secciones pendientes (antes mostraba 3 pendientes + Layout completado). Ningún cambio en frontend — el ProgressBar ya reacciona al valor booleano.
