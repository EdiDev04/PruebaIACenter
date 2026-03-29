---
id: SPEC-005
status: DRAFT
feature: location-layout-configuration
feature_type: full-stack
requires_design_spec: true
has_calculation_logic: false
affects_database: true
consumes_core_ohs: false
has_fe_be_integration: true
created: 2026-03-29
updated: 2026-03-29
author: spec-generator
version: "1.0"
related-specs: ["SPEC-002", "SPEC-003"]
priority: media
estimated-complexity: S
---

# Spec: Configuración del Layout de Ubicaciones

> **Estado:** `DRAFT` → aprobar con `status: APPROVED` antes de iniciar implementación.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar la consulta y el guardado de la configuración del layout de la grilla de ubicaciones dentro de un folio. El layout define cómo se presenta visualmente la lista de ubicaciones de riesgo: modo de visualización (grilla o lista) y columnas visibles. Esta configuración persiste como sección independiente del agregado `PropertyQuote` en MongoDB, usando actualización parcial con versionado optimista. El layout se gestiona en la misma página de ubicaciones (Step 2 del wizard) como un panel de configuración de la grilla, no como un paso separado.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-005-01**: Como usuario del cotizador, quiero configurar el modo de visualización y las columnas visibles de la grilla de ubicaciones para organizar visualmente las propiedades del folio.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio existente `DAN-2026-00001` sin configuración de layout
  **Cuando** envío `GET /v1/quotes/DAN-2026-00001/locations/layout`
  **Entonces** el sistema retorna HTTP 200 con una configuración por defecto: `displayMode: "grid"`, `visibleColumns: ["index", "locationName", "zipCode", "businessLine", "validationStatus"]`
  **Y** el response incluye `version` actual del folio

- **Dado** que modifico el layout a `displayMode: "list"` y `visibleColumns: ["index", "locationName", "validationStatus"]`
  **Cuando** envío `PUT /v1/quotes/DAN-2026-00001/locations/layout` con esos datos y `version: 2`
  **Entonces** el sistema persiste solo la sección `layoutConfiguration`
  **Y** incrementa `version` a 3
  **Y** actualiza `metadata.updatedAt`
  **Y** actualiza `metadata.lastWizardStep` a 2
  **Y** retorna HTTP 200 con `{ "data": { ...layoutActualizado, "version": 3 } }`

- **Dado** que otro usuario modificó la cotización después de mi última lectura
  **Cuando** intento guardar el layout con una versión desactualizada (`version: 1` cuando la actual es `3`)
  **Entonces** el sistema retorna HTTP 409 con body `{ "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }`

**HU-005-02**: Como usuario del cotizador, quiero que la configuración del layout se guarde y persista entre sesiones para no tener que reconfigurar cada vez.

**Criterios de aceptación (Gherkin):**

- **Dado** que guardé un layout con `displayMode: "list"`
  **Cuando** cierro y reabro la página de ubicaciones
  **Entonces** la grilla se muestra en modo lista con las columnas que configuré

- **Dado** que modifico el layout
  **Cuando** guardo los cambios
  **Entonces** solo la sección `layoutConfiguration` se modifica en MongoDB
  **Y** las demás secciones (`insuredData`, `locations`, `coverageOptions`, etc.) permanecen intactas

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-005-01 | Layout es sección independiente | PUT layout | Solo modifica `layoutConfiguration`, no afecta ubicaciones ni otros datos | ADR-002 |
| RN-005-02 | Versionado optimista | PUT con `version` que no coincide | HTTP 409 VersionConflictException | architecture-decisions.md §Optimistic Versioning |
| RN-005-03 | Layout por defecto si no configurado | GET layout en folio sin configuración | Retorna defaults: `displayMode: "grid"`, `visibleColumns` con 5 columnas base | SUP-005-01 |
| RN-005-04 | `displayMode` solo acepta "grid" o "list" | PUT con valor fuera del enum | HTTP 400 validationError | bussines-context.md + SUP-005-01 |
| RN-005-05 | `visibleColumns` debe contener al menos una columna | PUT con array vacío | HTTP 400 validationError | Integridad de UI |
| RN-005-06 | `metadata.lastWizardStep` se actualiza a 2 | PUT layout exitoso | Automático en el `$set` del repositorio | ADR-007 |
| RN-005-07 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | Wrapper obligatorio | architecture-decisions.md §Response Format |
| RN-005-08 | Mensajes de error en español | Toda respuesta de error | Campo `message` en español; `type` en inglés | ADR-008 |
| RN-005-09 | Sort y pageSize son estado de UI transitorio | Configuración de sort/paginación | NO se persisten en MongoDB — van en Redux o `useState` local | SUP-005-01 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `displayMode` | Requerido, debe ser `"grid"` o `"list"` | "Modo de visualización inválido. Valores permitidos: grid, list" | Sí (400) |
| `visibleColumns` | Requerido, array no vacío, cada elemento debe ser un nombre de columna válido | "Debe seleccionar al menos una columna visible" | Sí (400) |
| `version` | Requerido, entero > 0, debe coincidir con versión persistida | "Conflicto de versión" | Sí (409) |

**Columnas válidas:** `index`, `locationName`, `address`, `zipCode`, `state`, `municipality`, `neighborhood`, `city`, `constructionType`, `level`, `constructionYear`, `businessLine`, `guarantees`, `catZone`, `validationStatus`

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — panel de configuración en la página de ubicaciones
  Fase 1.5 (core-ohs):       NO APLICA
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — repositorio UpdateLayoutAsync ya definido (SPEC-002)
  Fase 2 backend-developer:  APLICA — Use Cases + endpoints en QuoteController
  Fase 2 frontend-developer: APLICA — widget de configuración de layout en la página de ubicaciones
  Fase 2 integration:        APLICA — valida contratos FE ↔ BE (GET/PUT layout)

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
  - integration: verificación FE↔BE requiere que backend-developer y frontend-developer completen
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/location-layout-configuration.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - LocationsPage (/quotes/{folio}/locations): Panel de configuración de layout integrado en la página de ubicaciones

Flujos de usuario a diseñar:
  - Toggle entre modo grid y list
  - Selector de columnas visibles (checkboxes o similar)
  - Guardado automático o con botón de confirmación

Inputs de comportamiento que el ux-designer debe conocer:
  - El layout es un panel auxiliar dentro de la página de ubicaciones, NO un paso separado del wizard
  - 15 columnas disponibles, 5 visibles por defecto
  - Sort y pageSize son locales a la sesión (no persisten en MongoDB)
  - Todos los strings de UI en español (ADR-008)
```

### 3.3 Modelo de dominio

**Modificar value object existente** (actualmente placeholder vacío):

```csharp
// Cotizador.Domain/ValueObjects/LayoutConfiguration.cs — MODIFICAR
public class LayoutConfiguration
{
    public string DisplayMode { get; set; } = "grid";           // "grid" | "list"
    public List<string> VisibleColumns { get; set; } = new()    // Columnas visibles en la grilla
    {
        "index", "locationName", "zipCode", "businessLine", "validationStatus"
    };
}
```

**New Application DTOs:**

```csharp
// Cotizador.Application/DTOs/LayoutConfigurationDto.cs
public record LayoutConfigurationDto(
    string DisplayMode,
    List<string> VisibleColumns,
    int Version
);

// Cotizador.Application/DTOs/UpdateLayoutRequest.cs
public record UpdateLayoutRequest(
    string DisplayMode,
    List<string> VisibleColumns,
    int Version
);
```

### 3.4 Contratos API (backend)

```
GET /v1/quotes/{folio}/locations/layout
Propósito: Consultar configuración de layout de la grilla de ubicaciones
Auth: Basic Auth ([Authorize])
Use Case: GetLayoutUseCase
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
    "displayMode": "grid",
    "visibleColumns": ["index", "locationName", "zipCode", "businessLine", "validationStatus"],
    "version": 2
  }
}

Response 200 (folio sin layout configurado — retorna defaults):
{
  "data": {
    "displayMode": "grid",
    "visibleColumns": ["index", "locationName", "zipCode", "businessLine", "validationStatus"],
    "version": 1
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
PUT /v1/quotes/{folio}/locations/layout
Propósito: Guardar/actualizar configuración de layout (actualización parcial con versionado optimista)
Auth: Basic Auth ([Authorize])
Use Case: UpdateLayoutUseCase
Repositorios: IQuoteRepository.UpdateLayoutAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    Content-Type: application/json
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001
  Body:
{
  "displayMode": "list",
  "visibleColumns": ["index", "locationName", "validationStatus"],
  "version": 2
}

Response 200:
{
  "data": {
    "displayMode": "list",
    "visibleColumns": ["index", "locationName", "validationStatus"],
    "version": 3
  }
}

Response 400: { "type": "validationError", "message": "Modo de visualización inválido. Valores permitidos: grid, list", "field": "displayMode" }
Response 400: { "type": "validationError", "message": "Debe seleccionar al menos una columna visible", "field": "visibleColumns" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 409: { "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Contratos core-ohs consumidos

N/A — este feature no consume core-ohs.

### 3.5b Contratos FE ↔ BE

```
GET /v1/quotes/{folio}/locations/layout
Consumido por:
  Archivo FE:    entities/layout/api/layoutApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['layout', folio]

Response BE → FE (200):
  { "data": { "displayMode": "grid", "visibleColumns": [...], "version": 2 } }

Errores manejados por el FE:
  - 404: notificación "El folio no existe"
  - 500: notificación genérica de error

Invalidación de caché:
  - Al mutar (PUT exitoso), invalida: ['layout', folio]
```

```
PUT /v1/quotes/{folio}/locations/layout
Consumido por:
  Archivo FE:    features/save-layout/model/useSaveLayout.ts
  Hook/Query:    useMutation (TanStack Query)
  Query Key:     invalidates ['layout', folio]

Request FE → BE:
  { "displayMode": "list", "visibleColumns": [...], "version": 2 }

Response BE → FE (200):
  { "data": { "displayMode": "list", "visibleColumns": [...], "version": 3 } }

Errores manejados por el FE:
  - 400: muestra errores de validación en formulario
  - 409: alerta "El folio fue modificado, recarga para continuar"
  - 500: notificación genérica de error

Invalidación de caché:
  - Al mutar exitosamente, invalida: ['layout', folio]
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── widgets/
│   └── layout-config/
│       ├── index.ts                           # CREAR — Public API
│       └── ui/
│           └── LayoutConfigPanel.tsx           # CREAR — Panel toggle grid/list + selector columnas
├── features/
│   └── save-layout/
│       ├── index.ts                            # CREAR — Public API
│       ├── model/
│       │   └── useSaveLayout.ts                # CREAR — useMutation(PUT .../locations/layout)
│       └── strings.ts                          # CREAR — Strings en español
├── entities/
│   └── layout/
│       ├── index.ts                            # CREAR — Public API
│       ├── model/
│       │   ├── types.ts                        # CREAR — LayoutConfigurationDto, UpdateLayoutRequest
│       │   ├── useLayoutQuery.ts               # CREAR — useQuery(['layout', folio])
│       │   └── layoutSchema.ts                 # CREAR — Zod schema
│       ├── api/
│       │   └── layoutApi.ts                    # CREAR — getLayout(), updateLayout()
│       └── strings.ts                          # CREAR — Labels: "Vista de grilla", "Vista de lista", nombres de columnas
└── shared/
    └── api/
        └── endpoints.ts                        # MODIFICAR — agregar rutas de layout
```

**Props/hooks por componente:**

| Componente | Props | Hooks / queries | Acción |
|---|---|---|---|
| `LayoutConfigPanel` | `folio: string` | `useLayoutQuery`, `useSaveLayout` | Toggle grid/list, checkboxes de columnas, auto-save o botón guardar |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['layout', folio]` | `LayoutConfigurationDto` | Al mutar (PUT exitoso) |
| UI state | `useState` local | sortBy, sortDirection, pageSize | string, string, number | Local al componente — NO persiste |
| Form state | React Hook Form | layoutForm | `UpdateLayoutRequest` | On submit |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read (GET) | `property_quotes` | `Find` | `{ folioNumber }` | Full document (extraer `layoutConfiguration` + `version`) | `folioNumber_1` (existing) |
| Update (PUT) | `property_quotes` | `UpdateOne` | `{ folioNumber, version: N }` | `$set: { layoutConfiguration, version: N+1, metadata.updatedAt, metadata.lastWizardStep: 2 }` | `folioNumber_1` (existing) |

- **Versionado optimista**: filtro `{ folioNumber, version }`. Si `ModifiedCount == 0` → `VersionConflictException`.
- **Actualización parcial**: No toca `insuredData`, `locations`, `coverageOptions`, etc.
- **Repositorio**: `IQuoteRepository.UpdateLayoutAsync()` ya definido en SPEC-002.

---

## 4. LÓGICA DE CÁLCULO

N/A — este feature no involucra cálculo.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos modificados |
|---|---|---|
| `property_quotes` | Read + UpdateOne | `layoutConfiguration` (displayMode, visibleColumns), `version`, `metadata.updatedAt`, `metadata.lastWizardStep` |

### 5.2 Cambios de esquema

La entity `LayoutConfiguration` pasa de placeholder vacío a tener 2 campos:

| Campo | Tipo | Default |
|---|---|---|
| `displayMode` | `string` | `"grid"` |
| `visibleColumns` | `List<string>` | `["index", "locationName", "zipCode", "businessLine", "validationStatus"]` |

### 5.3 Índices requeridos

Ya definidos en SPEC-002. No se crean índices nuevos.

### 5.4 Datos semilla

Ninguno. El layout por defecto se define en los defaults del value object.

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-005-01 | Solo `displayMode` y `visibleColumns` se persisten en MongoDB. `sortBy`, `sortDirection` y `pageSize` son estado de UI transitorio (Redux/useState) | No tienen valor de negocio. Persistirlos infla el documento sin beneficio | Si se requiere persistencia de sort/pageSize, agregar campos al value object | usuario |
| SUP-005-02 | El layout se configura en la misma página de ubicaciones (Step 2), no como paso separado del wizard | El layout es una configuración auxiliar de la grilla, no datos de negocio propios de un paso | Si se quiere un paso dedicado, ajustar el wizard (ADR-005) | usuario |
| SUP-005-03 | Las 15 columnas válidas corresponden a los campos de la entidad `Location` definida en SPEC-002 | Alineación con el modelo de dominio existente | Si se agregan campos a Location, actualizar la lista de columnas válidas | spec-generator |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5, requires_design_spec=true)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        ├── [backend-developer]  (Fase 2, no bloqueado — SPEC-002 ya definió UpdateLayoutAsync)
        ├── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
        └── [integration]        (Fase 2, valida contratos FE ↔ BE)
                │
                ├── [test-engineer-backend]   (Fase 3, paralelo)
                └── [test-engineer-frontend]  (Fase 3, paralelo)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/location-layout-configuration.spec.md` → `status: APPROVED` |
| `integration` | `spec-generator` | `specs/location-layout-configuration.spec.md` → `status: APPROVED`. Verificación FE↔BE requiere que `backend-developer` y `frontend-developer` completen |
| `backend-developer` | `spec-generator` | `specs/location-layout-configuration.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` | `design-specs/location-layout-configuration.design.md` → `status: APPROVED` |
| `test-engineer-backend` | `backend-developer` | Implementación backend completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación frontend completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-002 | quote-data-model | depende-de (entidad LayoutConfiguration placeholder, repositorio UpdateLayoutAsync) |
| SPEC-003 | folio-creation | depende-de (el folio debe existir) |
| SPEC-006 | location-management | afecta (el layout define cómo se visualiza la grilla que SPEC-006 gestiona) |

---

## 8. LISTA DE TAREAS

### 8.0 integration

- [ ] Verificar contrato FE ↔ BE: `GET /v1/quotes/{folio}/locations/layout` — campos del response §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `PUT /v1/quotes/{folio}/locations/layout` — campos del request/response §3.4 coinciden con §3.5b
- [ ] Verificar que query keys, invalidación de caché y manejo de errores FE están alineados con los códigos HTTP del BE
- [ ] Reportar CONTRACT_DRIFT si hay discrepancias entre §3.4 y §3.5b

### 8.1 backend-developer

- [ ] Modificar `LayoutConfiguration` en `Cotizador.Domain/ValueObjects/` — agregar `DisplayMode` y `VisibleColumns` con defaults
- [ ] Crear `LayoutConfigurationDto` en `Cotizador.Application/DTOs/`
- [ ] Crear `UpdateLayoutRequest` en `Cotizador.Application/DTOs/`
- [ ] Crear validador FluentValidation `UpdateLayoutRequestValidator`
  - displayMode: requerido, debe ser "grid" o "list"
  - visibleColumns: requerido, array no vacío, cada elemento en lista de columnas válidas
  - version: requerido, > 0
- [ ] Crear `IGetLayoutUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `GetLayoutUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ILogger<GetLayoutUseCase>`
  - Flujo: buscar por folioNumber → si null throw FolioNotFoundException → mapear layoutConfiguration + version a DTO
- [ ] Crear `IUpdateLayoutUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `UpdateLayoutUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ILogger<UpdateLayoutUseCase>`
  - Flujo: construir LayoutConfiguration → llamar UpdateLayoutAsync → re-leer folio → retornar DTO
- [ ] Agregar endpoints en `QuoteController`:
  - `GET /v1/quotes/{folio}/locations/layout`
  - `PUT /v1/quotes/{folio}/locations/layout`
- [ ] Registrar Use Cases en `Program.cs`
- [ ] Mensajes de error en español (ADR-008)

### 8.2 frontend-developer

- [ ] Crear `entities/layout/` — types, schema Zod, query, api
- [ ] Crear `features/save-layout/` — `useSaveLayout` mutation
- [ ] Crear `widgets/layout-config/` — `LayoutConfigPanel` con toggle grid/list + checkboxes de columnas
- [ ] Integrar `LayoutConfigPanel` en la página de ubicaciones (`/quotes/{folio}/locations`)
- [ ] Agregar endpoints en `shared/api/endpoints.ts`
- [ ] Labels y strings en español (ADR-008)

### 8.3 test-engineer-backend

- [ ] `GetLayoutUseCaseTests` — folio con layout → retorna DTO
- [ ] `GetLayoutUseCaseTests` — folio sin layout (default) → retorna defaults
- [ ] `GetLayoutUseCaseTests` — folio inexistente → throws FolioNotFoundException
- [ ] `UpdateLayoutUseCaseTests` — datos válidos → actualización exitosa, version+1
- [ ] `UpdateLayoutUseCaseTests` — displayMode inválido → throws ValidationException
- [ ] `UpdateLayoutUseCaseTests` — visibleColumns vacío → throws ValidationException
- [ ] `UpdateLayoutUseCaseTests` — version mismatch → throws VersionConflictException
- [ ] `UpdateLayoutRequestValidatorTests` — escenarios de campos válidos e inválidos

### 8.4 test-engineer-frontend

- [ ] `LayoutConfigPanel.test.tsx` — renderiza con defaults, toggle mode, seleccionar/deseleccionar columnas
- [ ] `useLayoutQuery.test.ts` — fetch correcto mapea respuesta
- [ ] `useSaveLayout.test.ts` — mutación exitosa invalida query
- [ ] `layoutSchema.test.ts` — validaciones

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready):**
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED` (bloquea frontend)
- [ ] SPEC-002 implementada (Domain + Repository con UpdateLayoutAsync)
- [ ] SPEC-003 implementada (folio existe)

**DoD (Definition of Done):**
- [ ] `GET /v1/quotes/{folio}/locations/layout` responde según contrato §3.4
- [ ] `PUT /v1/quotes/{folio}/locations/layout` responde según contrato §3.4 (todos los códigos de error)
- [ ] Layout por defecto retornado cuando no hay configuración explícita
- [ ] Versionado optimista funcional (409 ante conflicto)
- [ ] `metadata.lastWizardStep` se actualiza a 2
- [ ] Actualización parcial — no afecta otras secciones del folio
- [ ] Mensajes de error en español (ADR-008)
- [ ] Sort y pageSize NO persisten en MongoDB
- [ ] Tests unitarios BE y FE pasando
- [ ] Sin violaciones de Clean Architecture
- [ ] Sin violaciones de reglas FSD
