---
id: SPEC-007
status: DRAFT
feature: coverage-options-configuration
feature_type: full-stack
requires_design_spec: true
has_calculation_logic: false
affects_database: true
consumes_core_ohs: true
has_fe_be_integration: true
created: 2026-03-29
updated: 2026-03-29
author: spec-generator
version: "1.0"
related-specs: ["SPEC-001", "SPEC-002", "SPEC-003"]
priority: alta
estimated-complexity: S
---

# Spec: Configuración de Opciones de Cobertura

> **Estado:** `DRAFT` → aprobar con `status: APPROVED` antes de iniciar implementación.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar la consulta y guardado de las opciones de cobertura a nivel de cotización (folio). Estas opciones definen qué garantías están habilitadas globalmente y los parámetros de deducible y coaseguro del folio. El catálogo de las 14 garantías disponibles se obtiene desde `cotizador-core-mock` a través de un endpoint proxy del backend. Las opciones se persisten como sección independiente del agregado `PropertyQuote` con versionado optimista. Corresponde al Step 3 del wizard (ADR-005), renderizado en `/quotes/{folio}/technical-info`.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-007-01**: Como usuario del cotizador, quiero configurar las opciones de cobertura del folio para definir qué garantías están habilitadas globalmente y sus condiciones de deducible y coaseguro.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio existente `DAN-2026-00001` sin opciones de cobertura configuradas
  **Cuando** envío `GET /v1/quotes/DAN-2026-00001/coverage-options`
  **Entonces** el sistema retorna HTTP 200 con valores por defecto: todas las 14 garantías habilitadas, `deductiblePercentage: 0`, `coinsurancePercentage: 0`
  **Y** el response incluye `version` actual del folio

- **Dado** que configuro las opciones habilitando solo 5 garantías y estableciendo deducible a `0.05` (5%)
  **Cuando** envío `PUT /v1/quotes/DAN-2026-00001/coverage-options` con esos datos y `version: 3`
  **Entonces** el sistema persiste solo la sección `coverageOptions`
  **Y** incrementa `version` a 4
  **Y** actualiza `metadata.updatedAt`
  **Y** actualiza `metadata.lastWizardStep` a 3
  **Y** retorna HTTP 200 con `{ "data": { ...opcionesActualizadas, "version": 4 } }`

- **Dado** que otro usuario modificó la cotización después de mi última lectura
  **Cuando** intento guardar opciones de cobertura con versión desactualizada
  **Entonces** el sistema retorna HTTP 409

**HU-007-02**: Como usuario del cotizador, quiero consultar las opciones de cobertura ya configuradas para revisarlas o modificarlas.

**Criterios de aceptación (Gherkin):**

- **Dado** que las opciones de cobertura están configuradas con 5 garantías habilitadas
  **Cuando** consulto las opciones
  **Entonces** el sistema retorna las opciones previamente guardadas con la lista exacta de guaranteeKeys

**HU-007-03**: Como usuario del cotizador, quiero ver el catálogo de garantías disponibles (14 tipos de cobertura) para seleccionar cuáles aplican a mi cotización.

**Criterios de aceptación (Gherkin):**

- **Dado** que el frontend carga la página de opciones de cobertura
  **Cuando** solicita el catálogo de garantías
  **Entonces** recibe las 14 garantías del catálogo via `GET /v1/catalogs/guarantees` (proxy del backend)
  **Y** cada garantía incluye `key`, `name`, `description`, `category`, `requiresInsuredAmount`

**HU-007-04**: Como usuario del cotizador, quiero recibir un warning cuando deshabilito una garantía que ya está seleccionada en una o más ubicaciones.

**Criterios de aceptación (Gherkin):**

- **Dado** que deshabilito la garantía `building_fire` en coverage options
  **Y** la garantía `building_fire` está seleccionada en 3 ubicaciones
  **Cuando** intento guardar
  **Entonces** el frontend muestra un warning: "Esta garantía ya está seleccionada en 3 ubicaciones. Deshabilitarla las marcará como incompletas."
  **Y** el usuario puede confirmar o cancelar

- **Dado** que el usuario confirma la deshabilitación
  **Cuando** el PUT se ejecuta
  **Entonces** las opciones se persisten sin modificar las ubicaciones existentes
  **Y** la inconsistencia se detectará al visualizar el resumen o al calcular

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-007-01 | Coverage options es sección independiente | PUT coverage-options | Solo modifica `coverageOptions`, no afecta ubicaciones ni otros datos | ADR-002 |
| RN-007-02 | Versionado optimista | PUT con `version` que no coincide | HTTP 409 VersionConflictException | architecture-decisions.md §Optimistic Versioning |
| RN-007-03 | Defaults si no configurado | GET coverage-options en folio sin configuración | Retorna: todas las 14 garantías habilitadas, deducible 0, coaseguro 0 | SUP-007-02 |
| RN-007-04 | `enabledGuarantees` solo acepta keys válidas | PUT con key no existente en el catálogo | HTTP 400 validationError | Integridad referencial con catálogo |
| RN-007-05 | Deducible y coaseguro son globales | PUT coverage-options | Se aplican a todo el folio, no por garantía individual | SUP-007-02 (limitación explícita) |
| RN-007-06 | Sin eliminación retroactiva de garantías en ubicaciones | PUT coverage-options deshabilita garantía ya usada en ubicaciones | Las ubicaciones NO se modifican. Inconsistencia se detecta en resumen/cálculo | SUP-007-05 |
| RN-007-07 | `metadata.lastWizardStep` se actualiza a 3 | PUT coverage-options exitoso | Automático en el `$set` del repositorio | ADR-007 |
| RN-007-08 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | Wrapper obligatorio | architecture-decisions.md §Response Format |
| RN-007-09 | Mensajes de error en español | Toda respuesta de error | Campo `message` en español; `type` en inglés | ADR-008 |
| RN-007-10 | Frontend consume catálogo de garantías via proxy del backend | Toda consulta de catálogo | Pasa por `GET /v1/catalogs/guarantees` del backend | bussines-context.md §2 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `enabledGuarantees` | Requerido, array no vacío, cada key debe estar en `GuaranteeKeys.All` | "Debe habilitar al menos una garantía" / "Clave de garantía inválida: {key}" | Sí (400) |
| `deductiblePercentage` | Requerido, decimal >= 0 y <= 1 | "El porcentaje de deducible debe estar entre 0 y 1" | Sí (400) |
| `coinsurancePercentage` | Requerido, decimal >= 0 y <= 1 | "El porcentaje de coaseguro debe estar entre 0 y 1" | Sí (400) |
| `version` | Requerido, entero > 0, debe coincidir con versión persistida | "Conflicto de versión" | Sí (409) |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — formulario de opciones de cobertura (wizard step 3)
  Fase 1.5 (core-ohs):       NO APLICA — endpoint GET /v1/catalogs/guarantees ya implementado (SPEC-001)
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — repositorio UpdateCoverageOptionsAsync ya definido (SPEC-002)
  Fase 2 backend-developer:  APLICA — Use Cases + endpoints
  Fase 2 frontend-developer: APLICA — página, formulario con catálogo de garantías
  Fase 2 integration:        APLICA — valida contratos: BE ↔ core-ohs (guarantees proxy) Y FE ↔ BE (3 endpoints)

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
  - integration: verificación FE↔BE requiere que backend-developer y frontend-developer completen
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/coverage-options-configuration.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - TechnicalInfoPage (/quotes/{folio}/technical-info): Formulario de opciones de cobertura (Step 3)

Flujos de usuario a diseñar:
  - Carga inicial: GET opciones existentes + GET catálogo garantías → poblar formulario
  - Selección/deselección de garantías con checkboxes agrupados por categoría (fire, cat, additional, special)
  - Campos numéricos para deducible y coaseguro (como porcentaje)
  - Warning previo al deshabilitar garantía ya usada en ubicaciones
  - Guardado con botón de confirmación

Inputs de comportamiento que el ux-designer debe conocer:
  - 14 garantías agrupadas en 4 categorías: fire (3), cat (2), additional (4), special (5)
  - Deducible y coaseguro son globales — en el dominio real varían por garantía, pero se simplifica para este alcance
  - Al deshabilitar garantía usada en ubicaciones: warning con count de ubicaciones afectadas
  - Todos los strings de UI en español (ADR-008)
```

### 3.3 Modelo de dominio

**Modificar value object existente** (actualmente placeholder vacío):

```csharp
// Cotizador.Domain/ValueObjects/CoverageOptions.cs — MODIFICAR
public class CoverageOptions
{
    public List<string> EnabledGuarantees { get; set; } = new(GuaranteeKeys.All); // Default: todas habilitadas
    public decimal DeductiblePercentage { get; set; }                              // Default: 0 (0%)
    public decimal CoinsurancePercentage { get; set; }                             // Default: 0 (0%)
}
```

**New Application DTOs:**

```csharp
// Cotizador.Application/DTOs/CoverageOptionsDto.cs
public record CoverageOptionsDto(
    List<string> EnabledGuarantees,
    decimal DeductiblePercentage,
    decimal CoinsurancePercentage,
    int Version
);

// Cotizador.Application/DTOs/UpdateCoverageOptionsRequest.cs
public record UpdateCoverageOptionsRequest(
    List<string> EnabledGuarantees,
    decimal DeductiblePercentage,
    decimal CoinsurancePercentage,
    int Version
);
```

### 3.4 Contratos API (backend)

```
GET /v1/quotes/{folio}/coverage-options
Propósito: Consultar las opciones de cobertura configuradas de la cotización
Auth: Basic Auth ([Authorize])
Use Case: GetCoverageOptionsUseCase
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
    "enabledGuarantees": ["building_fire", "contents_fire", "coverage_extension", "cat_tev", "cat_fhm", "debris_removal", "extraordinary_expenses", "rent_loss", "business_interruption", "electronic_equipment", "theft", "cash_and_securities", "glass", "illuminated_signs"],
    "deductiblePercentage": 0.05,
    "coinsurancePercentage": 0.10,
    "version": 4
  }
}

Response 200 (folio sin opciones configuradas — retorna defaults):
{
  "data": {
    "enabledGuarantees": ["building_fire", "contents_fire", "coverage_extension", "cat_tev", "cat_fhm", "debris_removal", "extraordinary_expenses", "rent_loss", "business_interruption", "electronic_equipment", "theft", "cash_and_securities", "glass", "illuminated_signs"],
    "deductiblePercentage": 0,
    "coinsurancePercentage": 0,
    "version": 1
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
PUT /v1/quotes/{folio}/coverage-options
Propósito: Guardar/actualizar opciones de cobertura (actualización parcial con versionado optimista)
Auth: Basic Auth ([Authorize])
Use Case: UpdateCoverageOptionsUseCase
Repositorios: IQuoteRepository.UpdateCoverageOptionsAsync()
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
  "enabledGuarantees": ["building_fire", "contents_fire", "cat_tev", "theft", "glass"],
  "deductiblePercentage": 0.05,
  "coinsurancePercentage": 0.10,
  "version": 3
}

Response 200:
{
  "data": {
    "enabledGuarantees": ["building_fire", "contents_fire", "cat_tev", "theft", "glass"],
    "deductiblePercentage": 0.05,
    "coinsurancePercentage": 0.10,
    "version": 4
  }
}

Response 400: { "type": "validationError", "message": "Debe habilitar al menos una garantía", "field": "enabledGuarantees" }
Response 400: { "type": "validationError", "message": "Clave de garantía inválida: invalid_key", "field": "enabledGuarantees" }
Response 400: { "type": "validationError", "message": "El porcentaje de deducible debe estar entre 0 y 1", "field": "deductiblePercentage" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 409: { "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Endpoint proxy de catálogo de garantías (backend → core-ohs → frontend)

```
GET /v1/catalogs/guarantees
Propósito: Proxy — obtener catálogo de garantías desde core-ohs para el frontend
Auth: Basic Auth ([Authorize])
Use Case: GetGuaranteesUseCase (passthrough)
Repositorios: Ninguno
Servicios externos: ICoreOhsClient.GetGuaranteesAsync()

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    X-Correlation-Id: (opcional, UUID v4)

Response 200:
{
  "data": [
    { "key": "building_fire", "name": "Incendio Edificios", "description": "Cobertura base sobre la construcción contra incendio", "category": "fire", "requiresInsuredAmount": true },
    { "key": "contents_fire", "name": "Incendio Contenidos", "description": "Cobertura sobre bienes muebles e inventarios contra incendio", "category": "fire", "requiresInsuredAmount": true },
    { "key": "glass", "name": "Vidrios", "description": "Rotura accidental de cristales", "category": "special", "requiresInsuredAmount": false },
    { "key": "illuminated_signs", "name": "Anuncios Luminosos", "description": "Daño a letreros y señalética iluminada", "category": "special", "requiresInsuredAmount": false }
  ]
}

Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 503: { "type": "coreOhsUnavailable", "message": "Servicio de catálogos no disponible, intente más tarde", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5.1 Contrato core-ohs subyacente

```
GET /v1/catalogs/guarantees (core-mock)
Response 200: { "data": [{ "key": "building_fire", "name": "Incendio Edificios", ... }, ...] }
Fixture: cotizador-core-mock/src/fixtures/guarantees.json
Datos extraídos: key, name, description, category, requiresInsuredAmount
Manejo de error: Timeout/5xx → CoreOhsUnavailableException → 503 al frontend
```

### 3.5b Contratos FE ↔ BE

```
GET /v1/quotes/{folio}/coverage-options
Consumido por:
  Archivo FE:    entities/coverage-options/api/coverageOptionsApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['coverage-options', folio]

Response BE → FE (200):
  { "data": { "enabledGuarantees": [...], "deductiblePercentage": 0.05, "coinsurancePercentage": 0.10, "version": 4 } }

Errores manejados por el FE:
  - 404: notificación "El folio no existe"
  - 500: notificación genérica de error

Invalidación de caché:
  - Al mutar (PUT exitoso), invalida: ['coverage-options', folio]
```

```
PUT /v1/quotes/{folio}/coverage-options
Consumido por:
  Archivo FE:    features/save-coverage-options/model/useSaveCoverageOptions.ts
  Hook/Query:    useMutation (TanStack Query)
  Query Key:     invalidates ['coverage-options', folio]

Request FE → BE:
  { "enabledGuarantees": [...], "deductiblePercentage": 0.05, "coinsurancePercentage": 0.10, "version": 3 }

Response BE → FE (200):
  { "data": { "enabledGuarantees": [...], "deductiblePercentage": 0.05, "coinsurancePercentage": 0.10, "version": 4 } }

Errores manejados por el FE:
  - 400: muestra errores de validación en formulario
  - 409: alerta "El folio fue modificado, recarga para continuar"
  - 500: notificación genérica de error

Invalidación de caché:
  - Al mutar exitosamente, invalida: ['coverage-options', folio]
```

```
GET /v1/catalogs/guarantees
Consumido por:
  Archivo FE:    entities/guarantee/api/guaranteeApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['guarantees']

Response BE → FE (200):
  { "data": [{ "key": "building_fire", "name": "Incendio Edificios", ... }, ...] }

Errores manejados por el FE:
  - 503: alerta global "Servicio no disponible"
  - 500: notificación genérica de error

Invalidación de caché:
  - staleTime: 30min, no invalida manualmente
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── pages/
│   └── technical-info/
│       ├── index.ts                                # CREAR — Public API
│       └── ui/
│           └── TechnicalInfoPage.tsx               # CREAR — Ensamblado: CoverageOptionsForm widget
├── widgets/
│   └── coverage-options-form/
│       ├── index.ts                                # CREAR — Public API
│       └── ui/
│           └── CoverageOptionsForm.tsx             # CREAR — Formulario con checkboxes de garantías + inputs deducible/coaseguro
├── features/
│   └── save-coverage-options/
│       ├── index.ts                                # CREAR — Public API
│       ├── model/
│       │   └── useSaveCoverageOptions.ts           # CREAR — useMutation(PUT .../coverage-options)
│       └── strings.ts                              # CREAR — Strings en español
├── entities/
│   ├── coverage-options/
│   │   ├── index.ts                                # CREAR — Public API
│   │   ├── model/
│   │   │   ├── types.ts                            # CREAR — CoverageOptionsDto, UpdateCoverageOptionsRequest
│   │   │   ├── useCoverageOptionsQuery.ts          # CREAR — useQuery(['coverage-options', folio])
│   │   │   └── coverageOptionsSchema.ts            # CREAR — Zod schema
│   │   ├── api/
│   │   │   └── coverageOptionsApi.ts               # CREAR — getCoverageOptions(), updateCoverageOptions()
│   │   └── strings.ts                              # CREAR — Labels: "Opciones de cobertura", "Deducible", etc.
│   └── guarantee/
│       ├── index.ts                                # CREAR — Public API
│       ├── model/
│       │   ├── types.ts                            # CREAR — GuaranteeDto
│       │   └── useGuaranteesQuery.ts               # CREAR — useQuery(['guarantees'], staleTime: 30min)
│       └── api/
│           └── guaranteeApi.ts                     # CREAR — getGuarantees() → GET /v1/catalogs/guarantees (backend proxy)
└── shared/
    └── api/
        └── endpoints.ts                            # MODIFICAR — agregar rutas de coverage-options, guarantees
```

**Props/hooks por componente:**

| Componente | Props | Hooks / queries | Acción |
|---|---|---|---|
| `TechnicalInfoPage` | — | `useParams()` para `folio` | Ensambla `CoverageOptionsForm` |
| `CoverageOptionsForm` | `folio: string` | `useCoverageOptionsQuery`, `useGuaranteesQuery`, `useLocationsQuery` (para contar afectadas), React Hook Form + Zod | Checkboxes agrupados por categoría, inputs numéricos, warning dialog |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['coverage-options', folio]` | `CoverageOptionsDto` | Al mutar (PUT exitoso) |
| Server state | TanStack Query | `['guarantees']` | `GuaranteeDto[]` | staleTime: 30min, no invalida |
| Server state | TanStack Query | `['locations', folio]` | `LocationDto[]` | Necesario para contar ubicaciones afectadas por warning (SPEC-006 query) |
| UI state | Redux | `quoteWizardSlice.stepsCompleted[3]` | `boolean` | `markComplete(3)` tras PUT exitoso |
| Form state | React Hook Form | coverageOptionsForm | `CoverageOptionsFormValues` | On submit + `useFormPersist` (ADR-007) |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read (GET) | `property_quotes` | `Find` | `{ folioNumber }` | Full document (extraer `coverageOptions` + `version`) | `folioNumber_1` (existing) |
| Update (PUT) | `property_quotes` | `UpdateOne` | `{ folioNumber, version: N }` | `$set: { coverageOptions, version: N+1, metadata.updatedAt, metadata.lastWizardStep: 3 }` | `folioNumber_1` (existing) |

- **Versionado optimista**: filtro `{ folioNumber, version }`. Si `ModifiedCount == 0` → `VersionConflictException`.
- **Actualización parcial**: No toca `insuredData`, `locations`, `layoutConfiguration`, etc.
- **Repositorio**: `IQuoteRepository.UpdateCoverageOptionsAsync()` ya definido en SPEC-002.

---

## 4. LÓGICA DE CÁLCULO

N/A — este feature no involucra cálculo. Las opciones de cobertura son input para el motor de cálculo (SPEC-009).

**Limitación documentada**: En el dominio real de seguros de daños, deducible y coaseguro pueden variar por tipo de garantía (ej. incendio tiene condiciones distintas a CAT). En este alcance se simplifican como parámetros globales del folio. La implementación per-guarantee queda fuera de scope y se documenta como extensión futura.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos modificados |
|---|---|---|
| `property_quotes` | Read + UpdateOne | `coverageOptions` (enabledGuarantees, deductiblePercentage, coinsurancePercentage), `version`, `metadata.updatedAt`, `metadata.lastWizardStep` |

### 5.2 Cambios de esquema

La entity `CoverageOptions` pasa de placeholder vacío a tener 3 campos:

| Campo | Tipo | Default |
|---|---|---|
| `enabledGuarantees` | `List<string>` | Todas las 14 keys de `GuaranteeKeys.All` |
| `deductiblePercentage` | `decimal` | `0` |
| `coinsurancePercentage` | `decimal` | `0` |

### 5.3 Índices requeridos

Ya definidos en SPEC-002. No se crean índices nuevos.

### 5.4 Datos semilla

Ninguno. El catálogo de garantías ya existe como fixture en `cotizador-core-mock/src/fixtures/guarantees.json` (SPEC-001).

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-007-01 | Coverage options se renderiza en `/quotes/{folio}/technical-info` como Step 3 del wizard | Alineado con ADR-005 step mapping | Si se quiere en otra ruta, ajustar router | usuario |
| SUP-007-02 | Deducible y coaseguro son porcentajes globales del folio (no per-guarantee) | Simplificación para el alcance del reto. En el dominio real varían por garantía | Si se requiere per-guarantee, agregar `List<GuaranteeCondition>` y refactorizar | usuario |
| SUP-007-03 | Defaults: todas las 14 garantías habilitadas, deducible 0%, coaseguro 0% | Empieza con todo habilitado para que el usuario reduzca | Si se prefiere vacío, cambiar el default del value object | usuario |
| SUP-007-04 | El backend valida `enabledGuarantees` contra `GuaranteeKeys.All` (constantes C#), no contra el catálogo de core-mock en runtime | Evita llamada HTTP en cada PUT. Las keys son constantes del dominio | Si el catálogo cambia en core-mock sin actualizar las constantes, habría desincronización | spec-generator |
| SUP-007-05 | Al deshabilitar una garantía usada en ubicaciones, NO se eliminan retroactivamente de las ubicaciones. El frontend muestra warning; la inconsistencia se refleja en resumen y cálculo | Evita cascada de escrituras complejas. El backend escribe solo `coverageOptions` | Si se requiere eliminación retroactiva, agregar lógica de cascada en el Use Case | usuario |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5, requires_design_spec=true)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        ├── [backend-developer]  (Fase 2, no bloqueado — SPEC-002 ya definió UpdateCoverageOptionsAsync)
        ├── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
        └── [integration]        (Fase 2, valida contratos BE ↔ core-ohs Y FE ↔ BE)
                │
                ├── [test-engineer-backend]   (Fase 3, paralelo)
                └── [test-engineer-frontend]  (Fase 3, paralelo)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/coverage-options-configuration.spec.md` → `status: APPROVED` |
| `integration` | `spec-generator` | `specs/coverage-options-configuration.spec.md` → `status: APPROVED`. Verificación FE↔BE requiere que `backend-developer` y `frontend-developer` completen |
| `backend-developer` | `spec-generator` | `specs/coverage-options-configuration.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` | `design-specs/coverage-options-configuration.design.md` → `status: APPROVED` |
| `test-engineer-backend` | `backend-developer` | Implementación backend completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación frontend completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-001 | core-reference-service | depende-de (catálogo de garantías GET /v1/catalogs/guarantees) |
| SPEC-002 | quote-data-model | depende-de (entidad CoverageOptions placeholder, repositorio UpdateCoverageOptionsAsync, GuaranteeKeys) |
| SPEC-003 | folio-creation | depende-de (el folio debe existir) |
| SPEC-006 | location-management | afecta (enabledGuarantees actúa como whitelist para las garantías seleccionables por ubicación) |
| SPEC-009 | premium-calculation-engine | afecta (deducible y coaseguro son inputs del motor de cálculo) |

---

## 8. LISTA DE TAREAS

### 8.0 integration

- [ ] Verificar contrato BE ↔ core-ohs: `GET /v1/catalogs/guarantees` — response del mock coincide con lo que `ICoreOhsClient.GetGuaranteesAsync()` mapea
- [ ] Verificar contrato FE ↔ BE: `GET /v1/quotes/{folio}/coverage-options` — campos del response §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `PUT /v1/quotes/{folio}/coverage-options` — campos request/response §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `GET /v1/catalogs/guarantees` — proxy response §3.5 coincide con §3.5b
- [ ] Verificar que query keys, invalidación de caché y manejo de errores FE están alineados con los códigos HTTP del BE
- [ ] Reportar CONTRACT_DRIFT si hay discrepancias entre §3.4/§3.5 y §3.5b

### 8.1 backend-developer

- [ ] Modificar `CoverageOptions` en `Cotizador.Domain/ValueObjects/` — agregar `EnabledGuarantees`, `DeductiblePercentage`, `CoinsurancePercentage` con defaults
- [ ] Crear `CoverageOptionsDto` en `Cotizador.Application/DTOs/`
- [ ] Crear `UpdateCoverageOptionsRequest` en `Cotizador.Application/DTOs/`
- [ ] Crear validador FluentValidation `UpdateCoverageOptionsRequestValidator`
  - enabledGuarantees: requerido, no vacío, cada key en `GuaranteeKeys.All`
  - deductiblePercentage: requerido, >= 0, <= 1
  - coinsurancePercentage: requerido, >= 0, <= 1
  - version: requerido, > 0
- [ ] Crear `IGetCoverageOptionsUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `GetCoverageOptionsUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ILogger<GetCoverageOptionsUseCase>`
  - Flujo: buscar por folioNumber → si null throw FolioNotFoundException → mapear coverageOptions + version a DTO
- [ ] Crear `IUpdateCoverageOptionsUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `UpdateCoverageOptionsUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ILogger<UpdateCoverageOptionsUseCase>`
  - Flujo: construir CoverageOptions → llamar UpdateCoverageOptionsAsync → re-leer folio → retornar DTO
- [ ] Crear `IGetGuaranteesUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `GetGuaranteesUseCase` en `Cotizador.Application/UseCases/` (passthrough a `ICoreOhsClient.GetGuaranteesAsync()`)
- [ ] Agregar endpoints en `QuoteController`:
  - `GET /v1/quotes/{folio}/coverage-options`
  - `PUT /v1/quotes/{folio}/coverage-options`
- [ ] Agregar endpoint proxy en `CatalogController`:
  - `GET /v1/catalogs/guarantees` → `GetGuaranteesUseCase`
- [ ] Registrar Use Cases en `Program.cs`
- [ ] Mensajes de error en español (ADR-008)

### 8.2 frontend-developer

- [ ] Crear `entities/coverage-options/` — types, schema Zod, query, api
- [ ] Crear `entities/guarantee/` — types, query (staleTime 30min), api → `GET /v1/catalogs/guarantees` (backend proxy)
- [ ] Crear `features/save-coverage-options/` — `useSaveCoverageOptions` mutation
- [ ] Crear `widgets/coverage-options-form/` — `CoverageOptionsForm` con checkboxes agrupados por categoría + inputs deducible/coaseguro
- [ ] Implementar warning al deshabilitar garantía usada en ubicaciones: consultar `['locations', folio]`, contar afectadas, mostrar dialog
- [ ] Crear `pages/technical-info/` — `TechnicalInfoPage` ensambla el widget
- [ ] Integrar `useFormPersist` con key `wizard:{folio}:step:3` (ADR-007)
- [ ] Agregar ruta `/quotes/:folio/technical-info` en `app/router/router.tsx`
- [ ] Agregar endpoints en `shared/api/endpoints.ts`
- [ ] Labels y strings en español (ADR-008)

### 8.3 test-engineer-backend

- [ ] `GetCoverageOptionsUseCaseTests` — folio con opciones → retorna DTO
- [ ] `GetCoverageOptionsUseCaseTests` — folio sin opciones (default) → retorna defaults (14 garantías, 0, 0)
- [ ] `GetCoverageOptionsUseCaseTests` — folio inexistente → throws FolioNotFoundException
- [ ] `UpdateCoverageOptionsUseCaseTests` — datos válidos → actualización exitosa, version+1
- [ ] `UpdateCoverageOptionsUseCaseTests` — guaranteeKey inválida → throws ValidationException
- [ ] `UpdateCoverageOptionsUseCaseTests` — enabledGuarantees vacío → throws ValidationException
- [ ] `UpdateCoverageOptionsUseCaseTests` — deductible fuera de rango → throws ValidationException
- [ ] `UpdateCoverageOptionsUseCaseTests` — version mismatch → throws VersionConflictException
- [ ] `GetGuaranteesUseCaseTests` — core-ohs disponible → retorna 14 garantías
- [ ] `GetGuaranteesUseCaseTests` — core-ohs no disponible → throws CoreOhsUnavailableException

### 8.4 test-engineer-frontend

- [ ] `CoverageOptionsForm.test.tsx` — renderiza checkboxes por categoría, campos deducible/coaseguro
- [ ] `CoverageOptionsForm.test.tsx` — submit exitoso invoca mutación y marca step complete
- [ ] `CoverageOptionsForm.test.tsx` — warning dialog al deshabilitar garantía usada en ubicaciones
- [ ] `useCoverageOptionsQuery.test.ts` — fetch correcto mapea respuesta
- [ ] `useSaveCoverageOptions.test.ts` — mutación exitosa invalida query
- [ ] `coverageOptionsSchema.test.ts` — validaciones

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready):**
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED` (bloquea frontend)
- [ ] SPEC-001 implementada (catálogo de garantías en core-mock)
- [ ] SPEC-002 implementada (Domain + Repository con UpdateCoverageOptionsAsync)
- [ ] SPEC-003 implementada (folio existe)

**DoD (Definition of Done):**
- [ ] `GET /v1/quotes/{folio}/coverage-options` responde según contrato §3.4
- [ ] `PUT /v1/quotes/{folio}/coverage-options` responde según contrato §3.4 (todos los códigos de error)
- [ ] `GET /v1/catalogs/guarantees` responde como proxy según contrato §3.5
- [ ] Frontend consume catálogo de garantías exclusivamente a través del backend
- [ ] Defaults retornados cuando no hay configuración explícita (14 garantías, deducible 0, coaseguro 0)
- [ ] Validación de guaranteeKeys contra `GuaranteeKeys.All`
- [ ] Warning en frontend al deshabilitar garantía usada en ubicaciones
- [ ] Sin eliminación retroactiva de garantías en ubicaciones
- [ ] Versionado optimista funcional (409 ante conflicto)
- [ ] `metadata.lastWizardStep` se actualiza a 3
- [ ] Mensajes de error en español (ADR-008)
- [ ] Tests unitarios BE y FE pasando
- [ ] Sin violaciones de Clean Architecture
- [ ] Sin violaciones de reglas FSD
