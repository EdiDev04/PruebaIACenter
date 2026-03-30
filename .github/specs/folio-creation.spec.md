---
id: SPEC-003
status: IMPLEMENTED
feature: folio-creation
feature_type: full-stack
requires_design_spec: true
has_calculation_logic: false
affects_database: true
consumes_core_ohs: true
created: 2026-03-29
updated: 2026-03-29
author: spec-generator
version: "1.0"
related-specs: ["SPEC-001", "SPEC-002"]
priority: alta
estimated-complexity: M
---

# Spec: Creación y Apertura de Folio

> **Estado:** `IMPLEMENTED` 
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar la creación de nuevos folios de cotización y la apertura de folios existentes. Este es el punto de entrada obligatorio al wizard del cotizador (Step 0, ADR-005). La creación es idempotente vía header `Idempotency-Key` y consume `cotizador-core-mock` para generar el número secuencial. La apertura permite retomar una cotización en progreso buscando por `folioNumber` contra MongoDB. Este feature habilita el primer flujo end-to-end visible del sistema.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-003-01**: Como usuario del cotizador, quiero crear un nuevo folio para iniciar una cotización de seguro de daños.

**Criterios de aceptación (Gherkin):**

- **Dado** que el servicio core-mock está disponible y no existe un folio con el `Idempotency-Key` enviado
  **Cuando** envío `POST /v1/folios` con header `Idempotency-Key: <uuid>` y `Authorization: Basic <base64>`
  **Entonces** el sistema genera un `folioNumber` secuencial (`DAN-YYYY-NNNNN`) consultando core-mock
  **Y** crea un documento en `property_quotes` con `quoteStatus: "draft"`, `version: 1`, `metadata.createdAt` en UTC
  **Y** retorna HTTP 201 con body `{ "data": { "folioNumber": "DAN-2026-00001", "quoteStatus": "draft", "version": 1, "metadata": {...} } }`

- **Dado** que ya existe un documento con `metadata.idempotencyKey` igual al `Idempotency-Key` enviado
  **Cuando** envío `POST /v1/folios` con el mismo `Idempotency-Key`
  **Entonces** el sistema retorna HTTP 200 con el folio existente (sin crear duplicado)

- **Dado** que la solicitud no incluye el header `Idempotency-Key`
  **Cuando** envío `POST /v1/folios`
  **Entonces** el sistema retorna HTTP 400 con body `{ "type": "validationError", "message": "El header Idempotency-Key es obligatorio", "field": "Idempotency-Key" }`

---

**HU-003-02**: Como usuario del cotizador, quiero abrir un folio existente por su número para retomar una cotización en progreso.

**Criterios de aceptación (Gherkin):**

- **Dado** que existe un folio con `folioNumber: "DAN-2026-00001"` en MongoDB
  **Cuando** envío `GET /v1/quotes/DAN-2026-00001`
  **Entonces** el sistema retorna HTTP 200 con body `{ "data": { "folioNumber": "DAN-2026-00001", "quoteStatus": "in_progress", "version": 3, "metadata": { "lastWizardStep": 1, ... } } }`

- **Dado** que no existe un folio con `folioNumber: "DAN-2026-99999"` en MongoDB
  **Cuando** envío `GET /v1/quotes/DAN-2026-99999`
  **Entonces** el sistema retorna HTTP 404 con body `{ "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }`

- **Dado** que envío un folio con formato inválido
  **Cuando** envío `GET /v1/quotes/INVALID-FORMAT`
  **Entonces** el sistema retorna HTTP 400 con body `{ "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }`

---

**HU-003-03**: Como usuario del cotizador, quiero que al crear o abrir un folio sea redirigido al wizard para continuar con la cotización.

**Criterios de aceptación (Gherkin):**

- **Dado** que creé un folio nuevo con número `DAN-2026-00001`
  **Cuando** la respuesta es exitosa (201)
  **Entonces** el frontend redirige a `/quotes/DAN-2026-00001/general-info`
  **Y** el `quoteWizardSlice` se inicializa con `currentStep: 1`, `activeFolio: "DAN-2026-00001"`

- **Dado** que abrí un folio existente cuyo `metadata.lastWizardStep` es 2
  **Cuando** la respuesta del GET es exitosa (200)
  **Entonces** el frontend redirige a `/quotes/DAN-2026-00001/locations`
  **Y** el `quoteWizardSlice` se inicializa con `currentStep: 2`, `activeFolio: "DAN-2026-00001"`

---

**HU-003-04**: Como sistema, quiero que la creación sea resiliente ante la indisponibilidad de core-mock.

**Criterios de aceptación (Gherkin):**

- **Dado** que `cotizador-core-mock` no responde en 10 segundos
  **Cuando** envío `POST /v1/folios`
  **Entonces** el backend reintenta 1 vez con 500ms de delay
  **Y** si falla de nuevo, retorna HTTP 503 con body `{ "type": "coreOhsUnavailable", "message": "Servicio de catálogos no disponible, intente más tarde", "field": null }`

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-003-01 | El formato del folio es `DAN-YYYY-NNNNN` | Cualquier folio generado o consultado | Validado con regex `^DAN-\d{4}-\d{5}$` | architecture-decisions.md §folioNumber Format |
| RN-003-02 | Idempotencia por `Idempotency-Key` | `POST /v1/folios` con `Idempotency-Key` ya existente en `metadata.idempotencyKey` | Retorna 200 con folio existente, no crea duplicado | architecture-decisions.md §Idempotency |
| RN-003-03 | Header `Idempotency-Key` obligatorio en POST | `POST /v1/folios` sin header `Idempotency-Key` | Retorna 400 | architecture-decisions.md §Required Headers |
| RN-003-04 | Folio nuevo se crea con estado `draft` | Creación exitosa | `quoteStatus: "draft"`, `version: 1`, `metadata.lastWizardStep: 0` | bussines-context.md §8 |
| RN-003-05 | `folioNumber` es inmutable tras la creación | Cualquier operación de escritura posterior | El campo `folioNumber` nunca se incluye en `$set` | ADR-001, SPEC-002 RN-002-01 |
| RN-003-06 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | ADR: Successful Response Format | architecture-decisions.md §Response Format |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `Idempotency-Key` (header) | Requerido en POST, formato UUID v4 | "El header Idempotency-Key es obligatorio" | Sí (400) |
| `folio` (path param en GET) | Regex `^DAN-\d{4}-\d{5}$` | "Formato de folio inválido. Use DAN-YYYY-NNNNN" | Sí (400) |
| `Authorization` (header) | Basic Auth válida | "Credenciales inválidas o ausentes" | Sí (401) |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — pantalla del wizard step 0 (crear/abrir folio)
  Fase 1.5 (core-ohs):       NO APLICA — endpoint GET /v1/folios/next ya implementado (SPEC-001)
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — entidades y repositorio ya implementados (SPEC-002)
  Fase 2 integration:        APLICA — valida contrato GET /v1/folios/next (mock ↔ cliente HTTP)
  Fase 2 backend-developer:  APLICA — Use Cases + Controller
  Fase 2 frontend-developer: APLICA — página, features y entities

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
  - integration puede iniciar en paralelo con backend-developer tras spec.status == APPROVED
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/folio-creation.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - HomePage (/cotizador): Punto de entrada del wizard — crear nuevo folio o abrir existente

Flujos de usuario a diseñar:
  - Crear folio: clic en "Crear nuevo" → loading → redirect al wizard step 1
  - Abrir folio: input de folioNumber → validación → redirect al wizard step correspondiente

Inputs de comportamiento que el ux-designer debe conocer:
  - El folioNumber tiene formato DAN-YYYY-NNNNN
  - Si el folio no existe, mostrar error inline "El folio no existe"
  - Si core-mock está caído, mostrar alerta global "Servicio no disponible"
  - Al abrir un folio, el wizard inicia en metadata.lastWizardStep (ADR-007)
```

### 3.3 Modelo de dominio

No se crean entidades nuevas. Se reutilizan las de SPEC-002:

- `PropertyQuote` (Domain/Entities) — ya implementada
- `QuoteMetadata` (Domain/ValueObjects) — ya implementada, incluye `IdempotencyKey` y `LastWizardStep`
- `FolioNotFoundException` (Domain/Exceptions) — ya implementada
- `QuoteStatus` (Domain/Constants) — ya implementada

**New Application DTOs:**

```csharp
// Cotizador.Application/DTOs/CreateFolioRequest.cs
public record CreateFolioRequest; // Body vacío — la idempotencia va en header

// Cotizador.Application/DTOs/QuoteSummaryDto.cs
public record QuoteSummaryDto(
    string FolioNumber,
    string QuoteStatus,
    int Version,
    QuoteMetadataDto Metadata
);

// Cotizador.Application/DTOs/QuoteMetadataDto.cs
public record QuoteMetadataDto(
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string CreatedBy,
    int LastWizardStep
);
```

### 3.4 Contratos API (backend)

```
POST /v1/folios
Propósito: Crear un nuevo folio de cotización (idempotente)
Auth: Basic Auth ([Authorize])
Use Case: CreateFolioUseCase
Repositorios: IQuoteRepository.GetByIdempotencyKeyAsync(), IQuoteRepository.CreateAsync()
Servicios externos: ICoreOhsClient.GenerateFolioAsync()

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    Idempotency-Key: 550e8400-e29b-41d4-a716-446655440000
    Content-Type: application/json
    X-Correlation-Id: (opcional, UUID v4)
  Body: {} (vacío)

Response 201:
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "draft",
    "version": 1,
    "metadata": {
      "createdAt": "2026-03-29T10:00:00Z",
      "updatedAt": "2026-03-29T10:00:00Z",
      "createdBy": "user",
      "lastWizardStep": 0
    }
  }
}

Response 200 (idempotente — folio ya existía con mismo Idempotency-Key):
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "in_progress",
    "version": 3,
    "metadata": {
      "createdAt": "2026-03-28T15:00:00Z",
      "updatedAt": "2026-03-29T09:00:00Z",
      "createdBy": "user",
      "lastWizardStep": 2
    }
  }
}

Response 400: { "type": "validationError", "message": "El header Idempotency-Key es obligatorio", "field": "Idempotency-Key" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 503: { "type": "coreOhsUnavailable", "message": "Servicio de catálogos no disponible, intente más tarde", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
GET /v1/quotes/{folio}
Propósito: Obtener datos resumidos de un folio existente (para abrir en wizard)
Auth: Basic Auth ([Authorize])
Use Case: GetQuoteSummaryUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001 (validado con regex ^DAN-\d{4}-\d{5}$)

Response 200:
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "in_progress",
    "version": 5,
    "metadata": {
      "createdAt": "2026-03-28T15:00:00Z",
      "updatedAt": "2026-03-29T12:00:00Z",
      "createdBy": "user",
      "lastWizardStep": 2
    }
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Contratos core-ohs consumidos

```
GET /v1/folios/next
Propósito: Generar el siguiente folio secuencial
Response 200: { "data": { "folioNumber": "DAN-2026-00001" } }
Fixture: cotizador-core-mock/src/routes/folioRoutes.ts (in-memory counter)
Datos extraídos: folioNumber
Mapeado a: PropertyQuote.FolioNumber
Manejo de error:
  - Timeout/5xx: 1 retry con 500ms delay → CoreOhsUnavailableException (503)
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── app/
│   ├── router/
│   │   └── router.tsx                      # MODIFICAR — agregar ruta /cotizador
│   ├── store/
│   │   └── store.ts                        # MODIFICAR — registrar quoteWizardSlice + alertsSlice
│   └── providers/
│       └── AppProviders.tsx                 # CREAR — QueryClient, Redux Provider, ErrorBoundary global
├── pages/
│   └── home/
│       ├── index.ts                        # CREAR — Public API
│       └── ui/
│           └── HomePage.tsx                # CREAR — Ensamblado: CreateFolioCard + OpenFolioCard
├── widgets/
│   └── alert-container/
│       ├── index.ts                        # CREAR — Public API
│       └── ui/
│           └── AlertContainer.tsx          # CREAR — Lee alertsSlice, renderiza alertas globales
├── features/
│   ├── create-folio/
│   │   ├── index.ts                        # CREAR — Public API
│   │   ├── model/
│   │   │   └── useCreateFolio.ts           # CREAR — useMutation(POST /v1/folios)
│   │   ├── ui/
│   │   │   └── CreateFolioCard.tsx         # CREAR — Botón "Crear nuevo folio" con loading
│   │   └── strings.ts                      # CREAR — Strings en español
│   ├── open-folio/
│   │   ├── index.ts                        # CREAR — Public API
│   │   ├── model/
│   │   │   └── useOpenFolio.ts             # CREAR — lógica: validar formato + GET /v1/quotes/{folio}
│   │   └── ui/
│   │       ├── OpenFolioCard.tsx           # CREAR — Input + botón "Abrir folio"
│   │       └── openFolioSchema.ts          # CREAR — Zod: regex DAN-YYYY-NNNNN
│   ├── quote-wizard/
│   │   ├── index.ts                        # CREAR — Public API
│   │   └── model/
│   │       └── quoteWizardSlice.ts         # CREAR — Redux slice: currentStep, activeFolio, stepsCompleted
│   └── alerts/
│       ├── index.ts                        # CREAR — Public API
│       └── model/
│           └── alertsSlice.ts              # CREAR — Redux slice: alerts array
├── entities/
│   └── folio/
│       ├── index.ts                        # CREAR — Public API
│       ├── model/
│       │   └── types.ts                    # CREAR — QuoteSummaryDto, QuoteMetadataDto
│       ├── api/
│       │   └── folioApi.ts                 # CREAR — createFolio(), getQuoteSummary()
│       └── strings.ts                      # CREAR — Strings: estados, etiquetas de folio
└── shared/
    ├── api/
    │   ├── apiClient.ts                    # CREAR — fetch wrapper con Basic Auth, X-Correlation-Id
    │   └── endpoints.ts                    # CREAR — constantes de rutas API
    ├── lib/
    │   ├── strings.ts                      # CREAR — Mensajes globales: errores de red, alertas genéricas
    │   └── generateUuid.ts                 # CREAR — genera UUID v4 para Idempotency-Key
    └── ui/
        ├── Button.tsx                      # CREAR — Componente botón primitivo
        ├── Input.tsx                       # CREAR — Componente input primitivo
        ├── Card.tsx                        # CREAR — Componente card contenedor
        └── Spinner.tsx                     # CREAR — Loading spinner
```

**Props/hooks por componente:**

| Componente | Props | Hooks / queries | Acción |
|---|---|---|---|
| `HomePage` | — | — | Ensambla `CreateFolioCard` + `OpenFolioCard` |
| `CreateFolioCard` | — | `useCreateFolio()` | Genera UUID, invoca `POST /v1/folios`, redirect en onSuccess |
| `OpenFolioCard` | — | `useOpenFolio()`, React Hook Form + Zod | Valida formato, invoca `GET /v1/quotes/{folio}`, redirect en onSuccess |
| `AlertContainer` | — | `useAppSelector(alertsSlice)` | Renderiza alertas globales, auto-dismiss 8s |
| `quoteWizardSlice` | — | Redux Toolkit | `setFolio(folioNumber)`, `goToStep(n)`, `nextStep()`, `markComplete(step)` |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['quote', folioNumber]` | `QuoteSummaryDto` | staleTime: 0, refetch on mount |
| UI state | Redux | `quoteWizardSlice.currentStep` | `number` | `goToStep(n)` |
| UI state | Redux | `quoteWizardSlice.activeFolio` | `string \| null` | `setFolio(folioNumber)` |
| UI state | Redux | `alertsSlice.alerts` | `Alert[]` | `addAlert()`, `removeAlert()` |
| Form state | React Hook Form | `openFolioForm` | `{ folioNumber: string }` | On submit |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read by idempotency | `property_quotes` | `Find` | `{ "metadata.idempotencyKey": <uuid> }` | Full document | `metadata.idempotencyKey_1` (unique sparse) — ya definido en SPEC-002 |
| Create | `property_quotes` | `InsertOne` | — | — | `folioNumber_1` (unique) — ya definido en SPEC-002 |
| Read by folio | `property_quotes` | `Find` | `{ folioNumber: <folio> }` | Full document | `folioNumber_1` — ya definido en SPEC-002 |

- No se crean índices nuevos. SPEC-002 ya definió los necesarios.
- La creación **no** usa versionado optimista (es InsertOne, no UpdateOne).

---

## 4. LÓGICA DE CÁLCULO

N/A — este feature no involucra cálculo.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos modificados |
|---|---|---|
| `property_quotes` | InsertOne (crear) / Find (leer) | Documento completo al crear; lectura completa al abrir |

### 5.2 Cambios de esquema

Ninguno. Se usa la estructura ya implementada en SPEC-002.

### 5.3 Índices requeridos

Ya definidos en SPEC-002:
- `folioNumber_1` (unique)
- `metadata.idempotencyKey_1` (unique sparse)

### 5.4 Datos semilla

Ninguno requerido. El primer folio se genera al usar el sistema.

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-003-01 | Se agrega `GET /v1/quotes/{folio}` para que el frontend pueda abrir un folio existente (REQ-08 state endpoint está en Oleada 4) | Sin un GET no hay forma de validar existencia ni abrir un folio en Oleada 2 | Si REQ-08 se implementa antes, el GET podría ser redundante; pero son complementarios (este retorna datos resumidos, state retorna progreso) | usuario |
| SUP-003-02 | La ruta real del core-mock para generar folio es `GET /v1/folios/next` (no `GET /v1/folios` como indica REQ-01) | Código implementado en SPEC-001 usa `/v1/folios/next` | Si la ruta cambia en core-mock, ajustar `CoreOhsClient` | usuario |
| SUP-003-03 | El header `Idempotency-Key` es obligatorio en `POST /v1/folios`. Si falta → HTTP 400 | ADR de idempotencia lo requiere; alineado con guideline LIN-DEV-010 | Si se hace opcional, la idempotencia no es garantizada | usuario |
| SUP-003-04 | Al abrir un folio existente, el frontend usa un text input con validación de formato `DAN-YYYY-NNNNN` y verifica existencia contra `GET /v1/quotes/{folio}` del backend | El requerimiento dice "solicitar número de folio, validar existencia" | Si se prefiere un dropdown con folios recientes, se necesita un endpoint de listado no previsto en esta oleada | usuario |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5, requires_design_spec=true)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        ├── [integration]        (Fase 2, paralelo — valida contratos core-ohs)
        ├── [backend-developer]  (Fase 2, no bloqueado — SPEC-002 ya implementó Domain + Repository)
        └── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
                │
                ├── [test-engineer-backend]   (Fase 3, paralelo)
                └── [test-engineer-frontend]  (Fase 3, paralelo)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/folio-creation.spec.md` → `status: APPROVED` |
| `integration` | `spec-generator` | `specs/folio-creation.spec.md` → `status: APPROVED` |
| `backend-developer` | `spec-generator` | `specs/folio-creation.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` | `design-specs/folio-creation.design.md` → `status: APPROVED` |
| `test-engineer-backend` | `backend-developer` | Implementación backend completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación frontend completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-001 | core-reference-service | depende-de (endpoint `GET /v1/folios/next`) |
| SPEC-002 | quote-data-model | depende-de (entidades, repositorio, excepciones, middleware) |
| SPEC-004 | general-info-management | extiende (step 0 → step 1 del wizard) |

---

## 8. LISTA DE TAREAS

### 8.1 integration

- [ ] Documentar contrato `GET /v1/folios/next` en `.github/docs/integration-contracts.md`: request, response 200, manejo de errores
- [ ] Verificar que `cotizador-core-mock/src/routes/folioRoutes.ts` expone `GET /v1/folios/next` con response `{ "data": { "folioNumber": "DAN-YYYY-NNNNN" } }`
- [ ] Verificar que `CoreOhsClient.GenerateFolioAsync()` mapea correctamente a `FolioDto`
- [ ] Verificar que `CoreOhsClient` transforma timeout/5xx en `CoreOhsUnavailableException`
- [ ] Reportar CONTRACT_DRIFT si hay discrepancias entre mock, cliente HTTP y spec §3.5

### 8.2 backend-developer

- [ ] Crear `ICreateFolioUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `CreateFolioUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ICoreOhsClient`, `ILogger<CreateFolioUseCase>`
  - Flujo: verificar idempotencia → llamar core-mock → crear PropertyQuote → retornar DTO
- [ ] Crear `IGetQuoteSummaryUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `GetQuoteSummaryUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ILogger<GetQuoteSummaryUseCase>`
  - Flujo: buscar por folioNumber → si null throw FolioNotFoundException → mapear a DTO
- [ ] Crear `QuoteSummaryDto` y `QuoteMetadataDto` en `Cotizador.Application/DTOs/`
- [ ] Crear `FolioController` en `Cotizador.API/Controllers/`
  - `POST /v1/folios` — extrae `Idempotency-Key` del header, invoca `CreateFolioUseCase`
  - `GET /v1/quotes/{folio}` — valida formato, invoca `GetQuoteSummaryUseCase`
- [ ] Validación: `Idempotency-Key` requerido en POST (FluentValidation o inline en controller)
- [ ] Validación: `folio` path param regex `^DAN-\d{4}-\d{5}$`
- [ ] Registrar Use Cases en `Program.cs`: `AddScoped<ICreateFolioUseCase, CreateFolioUseCase>()`, `AddScoped<IGetQuoteSummaryUseCase, GetQuoteSummaryUseCase>()`
- [ ] Mensajes de error en español (ADR-008)

### 8.3 frontend-developer

- [ ] Inicializar proyecto `cotizador-webapp` con Vite + React 18 + TypeScript
- [ ] Configurar `app/providers/AppProviders.tsx` (QueryClient, Redux store, Router, ErrorBoundary)
- [ ] Configurar `app/store/store.ts` con `quoteWizardSlice` + `alertsSlice`
- [ ] Crear `shared/api/apiClient.ts` — fetch wrapper con Basic Auth + X-Correlation-Id
- [ ] Crear `shared/api/endpoints.ts` — constantes de rutas
- [ ] Crear `shared/lib/strings.ts` — mensajes globales en español
- [ ] Crear `shared/lib/generateUuid.ts`
- [ ] Crear componentes primitivos en `shared/ui/`: `Button`, `Input`, `Card`, `Spinner`
- [ ] Crear `entities/folio/model/types.ts` — DTOs TypeScript
- [ ] Crear `entities/folio/api/folioApi.ts` — `createFolio()`, `getQuoteSummary()`
- [ ] Crear `entities/folio/strings.ts` — etiquetas de folio en español
- [ ] Crear `features/create-folio/` — `useCreateFolio` hook + `CreateFolioCard` component
- [ ] Crear `features/open-folio/` — `useOpenFolio` hook + `OpenFolioCard` + `openFolioSchema` (Zod)
- [ ] Crear `features/quote-wizard/model/quoteWizardSlice.ts`
- [ ] Crear `features/alerts/model/alertsSlice.ts`
- [ ] Crear `widgets/alert-container/` — `AlertContainer` component
- [ ] Crear `pages/home/ui/HomePage.tsx` — ensambla CreateFolioCard + OpenFolioCard
- [ ] Configurar `app/router/router.tsx` con ruta `/cotizador`
- [ ] Strings UI en español (ADR-008)

### 8.4 test-engineer-backend

- [ ] `CreateFolioUseCaseTests` — creación exitosa (verify Create + GenerateFolio calls)
- [ ] `CreateFolioUseCaseTests` — idempotencia (verify GetByIdempotencyKey retorna existente)
- [ ] `CreateFolioUseCaseTests` — core-ohs indisponible (mock throws CoreOhsUnavailableException)
- [ ] `GetQuoteSummaryUseCaseTests` — folio encontrado (verify mapping a DTO)
- [ ] `GetQuoteSummaryUseCaseTests` — folio no encontrado (verify throws FolioNotFoundException)
- [ ] `FolioControllerTests` — POST sin Idempotency-Key → 400
- [ ] `FolioControllerTests` — GET con formato inválido → 400
- [ ] `FolioControllerTests` — POST exitoso → 201 con envelope `{ data: {...} }`
- [ ] `FolioControllerTests` — GET existente → 200 con envelope `{ data: {...} }`

### 8.5 test-engineer-frontend

- [ ] `CreateFolioCard.test.tsx` — renderiza botón, invoca mutación en clic
- [ ] `OpenFolioCard.test.tsx` — validación de formato (Zod), error al ingresar formato inválido
- [ ] `OpenFolioCard.test.tsx` — folio no encontrado muestra error inline
- [ ] `quoteWizardSlice.test.ts` — acciones setFolio, goToStep, markComplete
- [ ] `alertsSlice.test.ts` — acciones addAlert, removeAlert

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready)** — antes de iniciar implementación:
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED` (bloquea frontend)
- [ ] Todos los supuestos aprobados por el usuario
- [ ] SPEC-001 implementada (core-mock disponible)
- [ ] SPEC-002 implementada (Domain + Repository + Middleware)

**DoD (Definition of Done)** — para considerar el feature terminado:
- [ ] `POST /v1/folios` responde 201 (crear) y 200 (idempotente) según contrato §3.4
- [ ] `GET /v1/quotes/{folio}` responde 200/404 según contrato §3.4
- [ ] Idempotencia verificada: doble llamada con mismo Idempotency-Key no duplica
- [ ] Mensajes de error en español (ADR-008)
- [ ] Frontend: crear folio y abrir folio funcionan con redirect al wizard
- [ ] Redux slices (`quoteWizardSlice`, `alertsSlice`) inicializados y funcionales
- [ ] Tests unitarios BE y FE pasando
- [ ] Sin violaciones de Clean Architecture (`API → Application → Domain ← Infrastructure`)
- [ ] Sin violaciones de reglas FSD
