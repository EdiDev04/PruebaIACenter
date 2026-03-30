---
id: SPEC-006
status: IMPLEMENTED
feature: location-management
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
related-specs: ["SPEC-001", "SPEC-002", "SPEC-003", "SPEC-005", "SPEC-007"]
priority: alta
estimated-complexity: XL
---

# Spec: Gestión de Ubicaciones de Riesgo

> **Estado:** `IN_PROGRESS` — Aprobado el 2026-03-29. Implementación en curso.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar el registro, consulta, edición y resumen de ubicaciones de riesgo dentro de un folio. Cada ubicación representa un inmueble a asegurar con datos físicos, giro comercial, coberturas seleccionadas (garantías con suma asegurada) y validación de calculabilidad. El sistema soporta múltiples ubicaciones por folio y permite edición granular de una ubicación (PATCH) sin afectar las demás. La eliminación es implícita vía PUT del array completo. El código postal resuelve automáticamente zona catastrófica, estado, municipio y colonia a través de core-ohs. El `validationStatus` se calcula en el backend. Este es el feature más crítico de la oleada 3 — es la entrada de datos para el motor de cálculo (SPEC-009).

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-006-01**: Como usuario del cotizador, quiero agregar una o varias ubicaciones de riesgo a mi folio para asegurar múltiples propiedades.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio `DAN-2026-00001` con 0 ubicaciones
  **Cuando** envío `PUT /v1/quotes/DAN-2026-00001/locations` con un array de 1 ubicación con todos los datos requeridos y `version: 2`
  **Entonces** la ubicación se persiste con `validationStatus: "calculable"`
  **Y** `version` incrementa a 3
  **Y** `metadata.updatedAt` y `metadata.lastWizardStep` (= 2) se actualizan

- **Dado** que tengo un folio con 2 ubicaciones
  **Cuando** envío PUT con 3 ubicaciones (las 2 existentes + 1 nueva)
  **Entonces** el array se reemplaza atómicamente con las 3 ubicaciones

---

**HU-006-02**: Como usuario del cotizador, quiero capturar datos físicos de cada ubicación: dirección, código postal, tipo constructivo, nivel, año de construcción.

**Criterios de aceptación (Gherkin):**

- **Dado** que agrego una ubicación con dirección `"Av. Industria 340"`, zipCode `"06600"`, constructionType `"Tipo 1 - Macizo"`, level `2`, constructionYear `1998`
  **Cuando** guardo la ubicación
  **Entonces** todos los campos de datos físicos se persisten correctamente

---

**HU-006-03**: Como usuario del cotizador, quiero seleccionar el giro comercial (`businessLine`) de cada ubicación desde el catálogo de giros (con su `fireKey`).

**Criterios de aceptación (Gherkin):**

- **Dado** que el catálogo de giros tiene el giro `BL-001` con fireKey `"B-03"`
  **Cuando** selecciono ese giro para una ubicación
  **Entonces** el campo `businessLine` se persiste con `{ "description": "Storage warehouse", "fireKey": "B-03" }`

---

**HU-006-04**: Como usuario del cotizador, quiero que al ingresar un código postal, el sistema resuelva automáticamente zona catastrófica, estado, municipio y colonia.

**Criterios de aceptación (Gherkin):**

- **Dado** que ingreso el código postal `"06600"`
  **Cuando** el sistema consulta core-ohs (`GET /v1/zip-codes/06600` via proxy del backend)
  **Entonces** resuelve automáticamente `catZone: "A"`, `state: "Ciudad de México"`, `municipality: "Cuauhtémoc"`, `neighborhood: "Doctores"`

- **Dado** que ingreso el código postal `"99999"` que no existe
  **Cuando** el sistema consulta core-ohs
  **Entonces** retorna 404 con `{ "type": "zipCodeNotFound", "message": "Código postal no encontrado" }`
  **Y** la ubicación puede guardarse sin CP resuelto pero con `validationStatus: "incomplete"`

---

**HU-006-05**: Como usuario del cotizador, quiero seleccionar las garantías activas con su suma asegurada para cada ubicación.

**Criterios de aceptación (Gherkin):**

- **Dado** que selecciono las garantías `building_fire` con insuredAmount `5000000`, `cat_tev` con insuredAmount `3000000` y `glass` con insuredAmount `0`
  **Cuando** guardo la ubicación
  **Entonces** `guarantees` se persiste como `[{ "guaranteeKey": "building_fire", "insuredAmount": 5000000 }, { "guaranteeKey": "cat_tev", "insuredAmount": 3000000 }, { "guaranteeKey": "glass", "insuredAmount": 0 }]`

- **Dado** que la garantía `glass` tiene `requiresInsuredAmount: false` en el catálogo
  **Cuando** se guarda con `insuredAmount: 0`
  **Entonces** la ubicación sigue siendo `calculable` (no genera alerta de suma asegurada)

- **Dado** que la garantía `building_fire` tiene `requiresInsuredAmount: true`
  **Cuando** se guarda con `insuredAmount: 0`
  **Entonces** la garantía genera una alerta de suma asegurada faltante
  **Y** `validationStatus` puede ser `"incomplete"` dependiendo de las demás condiciones

---

**HU-006-06**: Como usuario del cotizador, quiero editar una ubicación específica sin afectar las demás ubicaciones del folio.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio con 3 ubicaciones
  **Cuando** envío `PATCH /v1/quotes/DAN-2026-00001/locations/2` con datos actualizados de la ubicación con índice 2 y `version: 5`
  **Entonces** solo la ubicación con `index: 2` se modifica
  **Y** las ubicaciones 1 y 3 permanecen intactas
  **Y** `version` incrementa a 6

- **Dado** que envío PATCH con índice 99 pero el folio solo tiene 3 ubicaciones
  **Cuando** el backend procesa la solicitud
  **Entonces** retorna HTTP 404 con body `{ "type": "folioNotFound", "message": "La ubicación con índice 99 no existe en el folio", "field": null }`

---

**HU-006-07**: Como usuario del cotizador, quiero ver un resumen de todas las ubicaciones con su estado de validación.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio con 3 ubicaciones: 2 calculables y 1 incompleta
  **Cuando** envío `GET /v1/quotes/DAN-2026-00001/locations/summary`
  **Entonces** el sistema retorna un resumen con el estado de cada ubicación y sus alertas

---

**HU-006-08**: Como usuario del cotizador, quiero ver alertas sobre ubicaciones incompletas sin que estas bloqueen el guardado del folio.

**Criterios de aceptación (Gherkin):**

- **Dado** que agrego una ubicación sin código postal
  **Cuando** guardo la ubicación
  **Entonces** se persiste con `validationStatus: "incomplete"` y `blockingAlerts: ["Código postal requerido"]`
  **Y** el guardado es exitoso (no se bloquea)
  **Y** las demás ubicaciones no se afectan

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-006-01 | Una ubicación es calculable si tiene CP válido, `businessLine.fireKey`, y al menos 1 garantía con `insuredAmount > 0` para garantías que requieren suma asegurada | Evaluado en el backend al persistir | `validationStatus: "calculable"` o `"incomplete"` + `blockingAlerts` | bussines-context.md §7, REQ-06 |
| RN-006-02 | Ubicación incompleta NO bloquea el folio | `validationStatus: "incomplete"` | Alerta generada, pero PUT/PATCH son exitosos | bussines-context.md §1 |
| RN-006-03 | PUT reemplaza el array completo atómicamente | `PUT /v1/quotes/{folio}/locations` | `$set: { locations: [...] }` en una sola operación | ADR-001, ADR-002 |
| RN-006-04 | PATCH solo modifica una ubicación por índice | `PATCH /v1/quotes/{folio}/locations/{index}` | `$set: { "locations.<idx>.*" }` — demás ubicaciones intactas | ADR-002 |
| RN-006-05 | Eliminación implícita via PUT | PUT sin una ubicación previamente existente | Array se reemplaza — la ubicación omitida desaparece | SUP-006-04 |
| RN-006-06 | Versionado optimista | PUT/PATCH con `version` que no coincide | HTTP 409 VersionConflictException | architecture-decisions.md §Optimistic Versioning |
| RN-006-07 | CP resuelve automáticamente zona/estado/municipio/colonia | Código postal ingresado | Backend consulta `GET /v1/zip-codes/{cp}` de core-ohs | bussines-context.md §7 |
| RN-006-08 | `validationStatus` se calcula en el backend | Al persistir (PUT/PATCH) | Backend evalúa las condiciones de RN-006-01 y asigna status + alertas | REQ-06 §Reglas de negocio |
| RN-006-09 | `metadata.lastWizardStep` se actualiza a 2 | PUT/PATCH locations exitoso | Automático en el `$set` del repositorio | ADR-007 |
| RN-006-10 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | Wrapper obligatorio | architecture-decisions.md §Response Format |
| RN-006-11 | Mensajes de error en español | Toda respuesta de error | Campo `message` en español; `type` en inglés | ADR-008 |
| RN-006-12 | Sin límite duro de ubicaciones | Folio con N ubicaciones | Razonable hasta 100 (BSON 16MB, ADR-001) | SUP-006-06 |
| RN-006-13 | `insuredAmount = 0` es válido para garantías con `requiresInsuredAmount: false` | Al evaluar calculabilidad | No genera alerta de suma faltante para `glass`, `illuminated_signs` | SUP-006-03 |
| RN-006-14 | Para garantías con `requiresInsuredAmount: true`, `insuredAmount > 0` es condición de calculabilidad | Al evaluar calculabilidad | Si `insuredAmount == 0` → alerta, garantía no calculable | SUP-006-03 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `index` | Requerido, entero >= 1, único dentro del array | "El índice de ubicación es obligatorio y debe ser único" | Sí (400) |
| `locationName` | Requerido, no vacío, max 200 chars | "El nombre de la ubicación es obligatorio" | Sí (400) |
| `address` | Requerido, no vacío, max 300 chars | "La dirección es obligatoria" | Sí (400) |
| `zipCode` | Opcional, si presente debe ser 5 dígitos numéricos | "El código postal debe ser de 5 dígitos" | Sí si presente (400). Si ausente, marca `incomplete` |
| `constructionType` | Opcional | — | No. Si ausente, no bloquea guardado |
| `level` | Opcional, si presente entero >= 0 | "El nivel debe ser un número positivo" | Sí si inválido (400) |
| `constructionYear` | Opcional, si presente entero entre 1800 y año actual | "El año de construcción es inválido" | Sí si inválido (400) |
| `businessLine.fireKey` | Opcional, si presente debe ser string no vacío | "La clave de incendio no puede estar vacía" | No. Si ausente, marca `incomplete` |
| `guarantees` | Opcional, array de `LocationGuarantee`. Cada key debe estar en `GuaranteeKeys.All` | "Clave de garantía inválida: {key}" | Sí si key inválida (400). Si vacío, marca `incomplete` |
| `guarantees[].insuredAmount` | Requerido si presente en array, decimal >= 0 | "La suma asegurada debe ser mayor o igual a 0" | Sí (400) |
| `version` | Requerido (en PUT/PATCH body), entero > 0 | "Conflicto de versión" | Sí (409) |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — página de ubicaciones con grilla, formulario y resumen
  Fase 1.5 (core-ohs):       NO APLICA — endpoints de zip-codes, business-lines, guarantees ya implementados (SPEC-001)
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — repositorio ya definido (SPEC-002)
  Fase 2 integration:        APLICA — valida contratos: BE ↔ core-ohs (zip-codes, business-lines, guarantees) Y FE ↔ BE (6 endpoints)
  Fase 2 backend-developer:  APLICA — Use Cases + Controller + Validadores + Lógica de calculabilidad
  Fase 2 frontend-developer: APLICA — página con grilla + formulario + resolución CP + catálogos

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
  - integration: verificación FE↔BE requiere que backend-developer y frontend-developer completen
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/location-management.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - LocationsPage (/quotes/{folio}/locations): Grilla de ubicaciones + formulario de captura/edición + panel de layout config (SPEC-005)

Flujos de usuario a diseñar:
  - Vista grilla con columnas configurables (SPEC-005 layout)
  - Botón "Agregar ubicación" → formulario modal o inline
  - Al ingresar CP → auto-resolución (zona, estado, municipio, colonia)
  - Selector de giro desde catálogo business-lines
  - Checkboxes de garantías con campo de suma asegurada por cada una
  - Indicador visual de validationStatus por ubicación (badge/chip)
  - Edición de ubicación individual → modal o inline
  - Eliminación de ubicación (con confirmación)
  - Resumen con alertas de ubicaciones incompletas
  - Warning de garantías que requieren suma asegurada > 0

Inputs de comportamiento que el ux-designer debe conocer:
  - Catálogos: business-lines, zip-codes, guarantees — todos via proxy del backend
  - 14 garantías agrupadas por categoría; `requiresInsuredAmount` indica si necesita suma > 0
  - Ubicación incompleta genera alerta pero NO bloquea guardado
  - Sin límite duro de ubicaciones (hasta ~100 por volumen de BSON)
  - Todos los strings de UI en español (ADR-008)
```

### 3.3 Modelo de dominio

**Nuevo value object:**

```csharp
// Cotizador.Domain/ValueObjects/LocationGuarantee.cs — CREAR
public class LocationGuarantee
{
    public string GuaranteeKey { get; set; } = string.Empty;  // Key from GuaranteeKeys.All
    public decimal InsuredAmount { get; set; }                 // >= 0. 0 valid for flat-rate guarantees
}
```

**Modificar entidad `Location`** (cambiar tipo de Guarantees):

```csharp
// Cotizador.Domain/Entities/Location.cs — MODIFICAR campo Guarantees
// ANTES:  public List<string> Guarantees { get; set; } = new();
// DESPUÉS:
public List<LocationGuarantee> Guarantees { get; set; } = new(); // MODIFICADO — List<string> → List<LocationGuarantee>
```

No se crean nuevas entidades. Se reutilizan de SPEC-002:
- `PropertyQuote` — aggregate root
- `Location` — entity (con el campo `Guarantees` modificado)
- `BusinessLine` — value object
- `ValidationStatus` — constants
- `GuaranteeKeys` — constants

**New Application DTOs:**

```csharp
// Cotizador.Application/DTOs/LocationDto.cs
public record LocationDto(
    int Index,
    string LocationName,
    string Address,
    string ZipCode,
    string State,
    string Municipality,
    string Neighborhood,
    string City,
    string ConstructionType,
    int Level,
    int ConstructionYear,
    BusinessLineDto LocationBusinessLine,
    List<LocationGuaranteeDto> Guarantees,
    string CatZone,
    List<string> BlockingAlerts,
    string ValidationStatus
);

// Cotizador.Application/DTOs/LocationGuaranteeDto.cs
public record LocationGuaranteeDto(
    string GuaranteeKey,
    decimal InsuredAmount
);

// Cotizador.Application/DTOs/UpdateLocationsRequest.cs
public record UpdateLocationsRequest(
    List<LocationDto> Locations,
    int Version
);

// Cotizador.Application/DTOs/PatchLocationRequest.cs
public record PatchLocationRequest(
    string? LocationName,
    string? Address,
    string? ZipCode,
    string? State,
    string? Municipality,
    string? Neighborhood,
    string? City,
    string? ConstructionType,
    int? Level,
    int? ConstructionYear,
    BusinessLineDto? LocationBusinessLine,
    List<LocationGuaranteeDto>? Guarantees,
    string? CatZone,
    int Version
);

// Cotizador.Application/DTOs/LocationSummaryDto.cs
public record LocationSummaryDto(
    int Index,
    string LocationName,
    string ValidationStatus,
    List<string> BlockingAlerts
);

// Cotizador.Application/DTOs/LocationsSummaryResponse.cs
public record LocationsSummaryResponse(
    List<LocationSummaryDto> Locations,
    int TotalCalculable,
    int TotalIncomplete,
    int Version
);

// Cotizador.Application/DTOs/LocationsResponse.cs
public record LocationsResponse(
    List<LocationDto> Locations,
    int Version
);
```

### 3.4 Contratos API (backend)

```
GET /v1/quotes/{folio}/locations
Propósito: Listar todas las ubicaciones del folio
Auth: Basic Auth ([Authorize])
Use Case: GetLocationsUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001

Response 200:
{
  "data": {
    "locations": [
      {
        "index": 1,
        "locationName": "Bodega Central CDMX",
        "address": "Av. Industria 340",
        "zipCode": "06600",
        "state": "Ciudad de México",
        "municipality": "Cuauhtémoc",
        "neighborhood": "Doctores",
        "city": "Ciudad de México",
        "constructionType": "Tipo 1 - Macizo",
        "level": 2,
        "constructionYear": 1998,
        "locationBusinessLine": { "description": "Storage warehouse", "fireKey": "B-03" },
        "guarantees": [
          { "guaranteeKey": "building_fire", "insuredAmount": 5000000 },
          { "guaranteeKey": "glass", "insuredAmount": 0 }
        ],
        "catZone": "A",
        "blockingAlerts": [],
        "validationStatus": "calculable"
      }
    ],
    "version": 3
  }
}

Response 200 (folio sin ubicaciones):
{
  "data": {
    "locations": [],
    "version": 1
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
PUT /v1/quotes/{folio}/locations
Propósito: Guardar/reemplazar el array completo de ubicaciones (operación atómica con versionado optimista)
Auth: Basic Auth ([Authorize])
Use Case: UpdateLocationsUseCase
Repositorios: IQuoteRepository.UpdateLocationsAsync()
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
  "locations": [
    {
      "index": 1,
      "locationName": "Bodega Central CDMX",
      "address": "Av. Industria 340",
      "zipCode": "06600",
      "state": "Ciudad de México",
      "municipality": "Cuauhtémoc",
      "neighborhood": "Doctores",
      "city": "Ciudad de México",
      "constructionType": "Tipo 1 - Macizo",
      "level": 2,
      "constructionYear": 1998,
      "locationBusinessLine": { "description": "Storage warehouse", "fireKey": "B-03" },
      "guarantees": [
        { "guaranteeKey": "building_fire", "insuredAmount": 5000000 },
        { "guaranteeKey": "glass", "insuredAmount": 0 }
      ],
      "catZone": "A"
    }
  ],
  "version": 2
}

Response 200:
{
  "data": {
    "locations": [
      {
        "index": 1,
        "locationName": "Bodega Central CDMX",
        "address": "Av. Industria 340",
        "zipCode": "06600",
        "state": "Ciudad de México",
        "municipality": "Cuauhtémoc",
        "neighborhood": "Doctores",
        "city": "Ciudad de México",
        "constructionType": "Tipo 1 - Macizo",
        "level": 2,
        "constructionYear": 1998,
        "locationBusinessLine": { "description": "Storage warehouse", "fireKey": "B-03" },
        "guarantees": [
          { "guaranteeKey": "building_fire", "insuredAmount": 5000000 },
          { "guaranteeKey": "glass", "insuredAmount": 0 }
        ],
        "catZone": "A",
        "blockingAlerts": [],
        "validationStatus": "calculable"
      }
    ],
    "version": 3
  }
}

Response 400: { "type": "validationError", "message": "El nombre de la ubicación es obligatorio", "field": "locations[0].locationName" }
Response 400: { "type": "validationError", "message": "Clave de garantía inválida: invalid_key", "field": "locations[0].guarantees" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 409: { "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
PATCH /v1/quotes/{folio}/locations/{index}
Propósito: Editar una ubicación puntual por índice (solo los campos enviados)
Auth: Basic Auth ([Authorize])
Use Case: PatchLocationUseCase
Repositorios: IQuoteRepository.PatchLocationAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    Content-Type: application/json
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001
    index: 2 (1-based)
  Body (parcial — solo campos que cambian):
{
  "zipCode": "03100",
  "state": "Ciudad de México",
  "municipality": "Benito Juárez",
  "neighborhood": "Del Valle",
  "city": "Ciudad de México",
  "catZone": "B",
  "version": 5
}

Response 200:
{
  "data": {
    "index": 2,
    "locationName": "Sucursal Del Valle",
    "address": "Av. División del Norte 1500",
    "zipCode": "03100",
    "state": "Ciudad de México",
    "municipality": "Benito Juárez",
    "neighborhood": "Del Valle",
    "city": "Ciudad de México",
    "constructionType": "Tipo 2 - Mixto",
    "level": 3,
    "constructionYear": 2005,
    "locationBusinessLine": { "description": "Retail store", "fireKey": "C-01" },
    "guarantees": [
      { "guaranteeKey": "building_fire", "insuredAmount": 3000000 },
      { "guaranteeKey": "contents_fire", "insuredAmount": 1500000 }
    ],
    "catZone": "B",
    "blockingAlerts": [],
    "validationStatus": "calculable",
    "version": 6
  }
}

Response 400: { "type": "validationError", "message": "El código postal debe ser de 5 dígitos", "field": "zipCode" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 404: { "type": "folioNotFound", "message": "La ubicación con índice 99 no existe en el folio", "field": null }
Response 409: { "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
GET /v1/quotes/{folio}/locations/summary
Propósito: Resumen de ubicaciones con estado de validación y alertas
Auth: Basic Auth ([Authorize])
Use Case: GetLocationsSummaryUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001

Response 200:
{
  "data": {
    "locations": [
      { "index": 1, "locationName": "Bodega Central CDMX", "validationStatus": "calculable", "blockingAlerts": [] },
      { "index": 2, "locationName": "Sucursal Del Valle", "validationStatus": "calculable", "blockingAlerts": [] },
      { "index": 3, "locationName": "Almacén Norte", "validationStatus": "incomplete", "blockingAlerts": ["Código postal requerido", "Giro comercial requerido"] }
    ],
    "totalCalculable": 2,
    "totalIncomplete": 1,
    "version": 5
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Endpoints proxy de catálogos (backend → core-ohs → frontend)

```
GET /v1/zip-codes/{zipCode}
Propósito: Proxy — resolver datos geográficos de un CP desde core-ohs
Auth: Basic Auth ([Authorize])
Use Case: GetZipCodeUseCase (passthrough)
Repositorios: Ninguno
Servicios externos: ICoreOhsClient.GetZipCodeAsync()

Response 200:
{
  "data": {
    "zipCode": "06600",
    "state": "Ciudad de México",
    "municipality": "Cuauhtémoc",
    "neighborhood": "Doctores",
    "city": "Ciudad de México",
    "catZone": "A",
    "technicalLevel": 2
  }
}

Response 404: { "type": "zipCodeNotFound", "message": "Código postal no encontrado", "field": null }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 503: { "type": "coreOhsUnavailable", "message": "Servicio de catálogos no disponible, intente más tarde", "field": null }
```

```
GET /v1/business-lines
Propósito: Proxy — obtener catálogo de giros con fireKey desde core-ohs
Auth: Basic Auth ([Authorize])
Use Case: GetBusinessLinesUseCase (passthrough)
Repositorios: Ninguno
Servicios externos: ICoreOhsClient.GetBusinessLinesAsync()

Response 200:
{
  "data": [
    { "code": "BL-001", "description": "Storage warehouse", "fireKey": "B-03", "riskLevel": "medium" },
    { "code": "BL-002", "description": "Retail store", "fireKey": "C-01", "riskLevel": "low" }
  ]
}

Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 503: { "type": "coreOhsUnavailable", "message": "Servicio de catálogos no disponible, intente más tarde", "field": null }
```

> **Nota**: `GET /v1/catalogs/guarantees` ya definido como proxy en SPEC-007.

### 3.5b Contratos FE ↔ BE

```
GET /v1/quotes/{folio}/locations
Consumido por:
  Archivo FE:    entities/location/api/locationApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['locations', folio]

Response BE → FE (200):
  { "data": { "locations": [...], "version": 3 } }

Errores manejados por el FE:
  - 404: notificación "El folio no existe"
  - 500: notificación genérica

Invalidación de caché:
  - Al mutar (PUT/PATCH exitoso), invalida: ['locations', folio], ['locations-summary', folio]
```

```
PUT /v1/quotes/{folio}/locations
Consumido por:
  Archivo FE:    features/save-locations/model/useSaveLocations.ts
  Hook/Query:    useMutation (TanStack Query)

Request FE → BE:
  { "locations": [...], "version": 3 }

Errores manejados por el FE:
  - 400: errores de validación en formulario
  - 409: alerta "El folio fue modificado, recarga para continuar"
  - 500: notificación genérica

Invalidación de caché:
  - Invalida: ['locations', folio], ['locations-summary', folio]
```

```
PATCH /v1/quotes/{folio}/locations/{index}
Consumido por:
  Archivo FE:    features/edit-location/model/useEditLocation.ts
  Hook/Query:    useMutation (TanStack Query)

Request FE → BE:
  { ...campos parciales, "version": 5 }

Errores manejados por el FE:
  - 400: errores de validación
  - 404: "Ubicación no encontrada"
  - 409: alerta versión
  - 500: notificación genérica

Invalidación de caché:
  - Invalida: ['locations', folio], ['locations-summary', folio]
```

```
GET /v1/quotes/{folio}/locations/summary
Consumido por:
  Archivo FE:    entities/location/model/useLocationsSummaryQuery.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['locations-summary', folio]

Response BE → FE (200):
  { "data": { "locations": [...], "totalCalculable": 2, "totalIncomplete": 1, "version": 5 } }

Invalidación de caché:
  - Al mutar ubicaciones (PUT/PATCH), invalida: ['locations-summary', folio]
```

```
GET /v1/zip-codes/{zipCode}
Consumido por:
  Archivo FE:    entities/zip-code/api/zipCodeApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['zip-code', zipCode]

Response BE → FE (200):
  { "data": { "zipCode": "06600", "state": "...", "municipality": "...", "neighborhood": "...", "city": "...", "catZone": "A", "technicalLevel": 2 } }

Errores manejados por el FE:
  - 404: muestra "Código postal no encontrado" en el campo CP
  - 503: alerta global

Invalidación de caché:
  - staleTime: 30min (CPs no cambian frecuentemente)
```

```
GET /v1/business-lines
Consumido por:
  Archivo FE:    entities/business-line/api/businessLineApi.ts
  Hook/Query:    useQuery (TanStack Query)
  Query Key:     ['business-lines']

Response BE → FE (200):
  { "data": [{ "code": "BL-001", "description": "...", "fireKey": "...", "riskLevel": "..." }, ...] }

Invalidación de caché:
  - staleTime: 30min (catálogo estático)
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── pages/
│   └── locations/
│       ├── index.ts                              # CREAR — Public API
│       └── ui/
│           └── LocationsPage.tsx                  # CREAR — Ensamblado: LocationsGrid + LayoutConfigPanel (SPEC-005)
├── widgets/
│   ├── locations-grid/
│   │   ├── index.ts                               # CREAR — Public API
│   │   └── ui/
│   │       └── LocationsGrid.tsx                  # CREAR — Grilla/lista de ubicaciones con badges de validación
│   └── location-form/
│       ├── index.ts                               # CREAR — Public API
│       └── ui/
│           └── LocationForm.tsx                   # CREAR — Formulario de captura/edición de una ubicación
├── features/
│   ├── save-locations/
│   │   ├── index.ts                               # CREAR — Public API
│   │   ├── model/
│   │   │   └── useSaveLocations.ts                # CREAR — useMutation(PUT .../locations)
│   │   └── strings.ts                             # CREAR
│   ├── edit-location/
│   │   ├── index.ts                               # CREAR — Public API
│   │   ├── model/
│   │   │   └── useEditLocation.ts                 # CREAR — useMutation(PATCH .../locations/{index})
│   │   └── strings.ts                             # CREAR
│   └── add-location/
│       ├── index.ts                               # CREAR — Public API
│       ├── model/
│       │   └── useAddLocation.ts                  # CREAR — Lógica local: agrega al array, trigger useSaveLocations
│       └── strings.ts                             # CREAR
├── entities/
│   ├── location/
│   │   ├── index.ts                               # CREAR — Public API
│   │   ├── model/
│   │   │   ├── types.ts                           # CREAR — LocationDto, LocationGuaranteeDto, UpdateLocationsRequest, PatchLocationRequest, etc.
│   │   │   ├── useLocationsQuery.ts               # CREAR — useQuery(['locations', folio])
│   │   │   ├── useLocationsSummaryQuery.ts        # CREAR — useQuery(['locations-summary', folio])
│   │   │   └── locationSchema.ts                  # CREAR — Zod schema para validación de una ubicación
│   │   ├── api/
│   │   │   └── locationApi.ts                     # CREAR — getLocations(), updateLocations(), patchLocation(), getSummary()
│   │   └── strings.ts                             # CREAR — Labels: "Ubicación", "Dirección", "Código Postal", etc.
│   ├── zip-code/
│   │   ├── index.ts                               # CREAR — Public API
│   │   ├── model/
│   │   │   ├── types.ts                           # CREAR — ZipCodeDto
│   │   │   └── useZipCodeQuery.ts                 # CREAR — useQuery(['zip-code', cp], enabled: cp?.length === 5)
│   │   └── api/
│   │       └── zipCodeApi.ts                      # CREAR — getZipCode() → GET /v1/zip-codes/{cp} (backend proxy)
│   └── business-line/
│       ├── index.ts                               # CREAR — Public API
│       ├── model/
│       │   ├── types.ts                           # CREAR — BusinessLineDto
│       │   └── useBusinessLinesQuery.ts           # CREAR — useQuery(['business-lines'], staleTime: 30min)
│       └── api/
│           └── businessLineApi.ts                 # CREAR — getBusinessLines() → GET /v1/business-lines (backend proxy)
└── shared/
    └── api/
        └── endpoints.ts                           # MODIFICAR — agregar rutas de locations, zip-codes, business-lines
```

**Props/hooks por componente:**

| Componente | Props | Hooks / queries | Acción |
|---|---|---|---|
| `LocationsPage` | — | `useParams()` para `folio` | Ensambla `LocationsGrid` + `LayoutConfigPanel` (SPEC-005) |
| `LocationsGrid` | `folio: string` | `useLocationsQuery`, `useLocationsSummaryQuery` | Renderiza grilla configurable, badges de estado, botón agregar/editar/eliminar |
| `LocationForm` | `folio: string`, `location?: LocationDto`, `onSave` | `useZipCodeQuery`, `useBusinessLinesQuery`, `useGuaranteesQuery` (SPEC-007), React Hook Form + Zod | Formulario completo de una ubicación |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['locations', folio]` | `LocationsResponse` | Al mutar (PUT/PATCH exitoso) |
| Server state | TanStack Query | `['locations-summary', folio]` | `LocationsSummaryResponse` | Al mutar ubicaciones |
| Server state | TanStack Query | `['zip-code', zipCode]` | `ZipCodeDto` | staleTime: 30min |
| Server state | TanStack Query | `['business-lines']` | `BusinessLineDto[]` | staleTime: 30min |
| Server state | TanStack Query | `['guarantees']` | `GuaranteeDto[]` | staleTime: 30min (SPEC-007 query) |
| UI state | Redux | `quoteWizardSlice.stepsCompleted[2]` | `boolean` | `markComplete(2)` tras PUT/PATCH exitoso |
| Form state | React Hook Form | locationForm | `LocationFormValues` | On submit + `useFormPersist` (ADR-007) |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read (GET locations) | `property_quotes` | `Find` | `{ folioNumber }` | Full document (extraer `locations` + `version`) | `folioNumber_1` (existing) |
| Read (GET summary) | `property_quotes` | `Find` | `{ folioNumber }` | Full document (extraer resumen) | `folioNumber_1` (existing) |
| Update (PUT locations) | `property_quotes` | `UpdateOne` | `{ folioNumber, version: N }` | `$set: { locations: [...], version: N+1, metadata.updatedAt, metadata.lastWizardStep: 2 }` | `folioNumber_1` (existing) |
| Patch (PATCH location) | `property_quotes` | `UpdateOne` | `{ folioNumber, version: N }` | `$set: { "locations.<idx>.field": val, version: N+1, ... }` | `folioNumber_1` (existing) |

- **Versionado optimista**: filtro `{ folioNumber, version }`. Si `ModifiedCount == 0` → `VersionConflictException`.
- **PUT es atómico**: reemplaza el array completo en una sola operación `$set`.
- **PATCH es parcial**: solo toca los campos de la ubicación en el índice dado.
- **Concurrencia (SUP-006-04)**: el versionado optimista es la única salvaguarda. Si dos sesiones editan simultáneamente y una hace PUT sin la ubicación X, la otra pierde cambios al recargar (409).

---

## 4. LÓGICA DE CÁLCULO

N/A — este feature no calcula primas. Pero define el `validationStatus` que determina qué ubicaciones son calculables para SPEC-009.

### Lógica de calculabilidad (ejecutada en el backend al persistir)

```
PARA CADA ubicacion EN request.locations:
  alertas = []

  SI ubicacion.zipCode está vacío O no tiene 5 dígitos:
    alertas.ADD("Código postal requerido")

  SI ubicacion.businessLine es null O businessLine.fireKey está vacío:
    alertas.ADD("Giro comercial requerido")

  SI ubicacion.guarantees está vacío (array vacío o null):
    alertas.ADD("Al menos una garantía es requerida")
  SINO:
    // Consultar catálogo de garantías (en memoria, constantes)
    PARA CADA guarantee EN ubicacion.guarantees:
      SI guarantee.requiresInsuredAmount == true Y guarantee.insuredAmount <= 0:
        alertas.ADD($"Suma asegurada requerida para {guarantee.guaranteeKey}")

  SI alertas.Count == 0:
    ubicacion.validationStatus = "calculable"
    ubicacion.blockingAlerts = []
  SINO:
    ubicacion.validationStatus = "incomplete"
    ubicacion.blockingAlerts = alertas
```

> **Nota**: La metadata de `requiresInsuredAmount` por garantía se obtiene de `GuaranteeKeys` + una constante o lookup en memoria (no requiere llamada HTTP en cada persistencia). Se pueden definir los keys que NO requieren suma como constantes: `glass`, `illuminated_signs`.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos modificados |
|---|---|---|
| `property_quotes` | Read + UpdateOne + PatchOne | `locations` (array completo o parcial), `version`, `metadata.updatedAt`, `metadata.lastWizardStep` |

### 5.2 Cambios de esquema

El campo `Location.Guarantees` cambia de `List<string>` a `List<LocationGuarantee>`:

| Campo | Tipo ANTES | Tipo DESPUÉS |
|---|---|---|
| `locations[].guarantees` | `List<string>` | `List<LocationGuarantee>` con `{ guaranteeKey, insuredAmount }` |

**Impacto migración**: Los folios existentes (creados en oleada 2) no tienen ubicaciones, así que no hay datos que migrar. El cambio es retrocompatible porque `Location.Guarantees` inicia como lista vacía.

### 5.3 Índices requeridos

Ya definidos en SPEC-002. No se crean índices nuevos.

### 5.4 Datos semilla

Ninguno. Los catálogos (zip-codes, business-lines, guarantees) ya existen como fixtures en `cotizador-core-mock` (SPEC-001).

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-006-01 | `Location.Guarantees` cambia de `List<string>` a `List<LocationGuarantee>` con `guaranteeKey` + `insuredAmount` | El motor de cálculo (SPEC-009) necesita `insuredAmount` por garantía por ubicación | Si no se captura aquí, se necesitaría un paso adicional antes del cálculo | usuario |
| SUP-006-02 | `insuredAmount = 0` es válido para garantías con `requiresInsuredAmount: false` (glass, illuminated_signs) | Son coberturas de tarifa plana que no se basan en suma asegurada | Si cambian a requerir suma, ajustar la lógica de calculabilidad | usuario |
| SUP-006-03 | Para garantías con `requiresInsuredAmount: true`, `insuredAmount > 0` es condición de calculabilidad | El motor necesita un valor positivo para calcular prima = suma × tarifa | Si se permite insuredAmount=0 con default, ajustar motor | usuario |
| SUP-006-04 | La eliminación de ubicaciones es implícita vía PUT del array completo. El PUT valida `version`, retorna 409 si difiere, y reemplaza atómicamente. Si dos sesiones editan simultáneamente, el versionado optimista detecta el conflicto pero no hace merge | Evita crear endpoint DELETE y simplifica la API. ADR-001 opera sobre un solo documento atómico | Si se requiere merge de conflictos, se necesitaría lógica de reconciliación | usuario |
| SUP-006-05 | La lógica de calculabilidad (`validationStatus`) se define con constantes en memoria, no consulta core-ohs en cada persistencia | Evita llamada HTTP por cada PUT/PATCH. Las garantías que requieren suma se conocen estáticamente | Si el catálogo cambia dinámicamente, agregar caché o lookup | spec-generator |
| SUP-006-06 | Sin límite duro de ubicaciones. Razonable hasta 100 (BSON 16MB, ADR-001) | El reto opera con folios de decenas de ubicaciones, no miles | Si un folio supera 100 ubicaciones, considerar paginación o normalización | usuario |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5, requires_design_spec=true)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        ├── [integration]        (Fase 2, valida contratos: BE ↔ core-ohs Y FE ↔ BE)
        ├── [backend-developer]  (Fase 2, no bloqueado — SPEC-002 ya definió UpdateLocationsAsync + PatchLocationAsync)
        └── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
                │
                ├── [test-engineer-backend]   (Fase 3, paralelo)
                └── [test-engineer-frontend]  (Fase 3, paralelo)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/location-management.spec.md` → `status: APPROVED` |
| `integration` | `spec-generator` | `specs/location-management.spec.md` → `status: APPROVED`. Verificación FE↔BE requiere que `backend-developer` y `frontend-developer` completen |
| `backend-developer` | `spec-generator` | `specs/location-management.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` | `design-specs/location-management.design.md` → `status: APPROVED` |
| `test-engineer-backend` | `backend-developer` | Implementación backend completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación frontend completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-001 | core-reference-service | depende-de (endpoints: zip-codes, business-lines, guarantees) |
| SPEC-002 | quote-data-model | depende-de (entidades Location, BusinessLine, repositorio UpdateLocationsAsync + PatchLocationAsync) |
| SPEC-003 | folio-creation | depende-de (el folio debe existir) |
| SPEC-005 | location-layout-configuration | depende-de (layout configura la visualización de la grilla de ubicaciones) |
| SPEC-007 | coverage-options-configuration | afecta (enabledGuarantees es whitelist para las garantías seleccionables) |
| SPEC-009 | premium-calculation-engine | afecta (ubicaciones calculables son input del motor) |

---

## 8. LISTA DE TAREAS

### 8.1 integration

- [ ] Documentar contratos BE ↔ core-ohs: `GET /v1/zip-codes/{cp}`, `GET /v1/business-lines`
- [ ] Verificar que `CoreOhsClient.GetZipCodeAsync()` mapea response 200 → `ZipCodeDto` y response 404 → `null`
- [ ] Verificar que `CoreOhsClient.GetBusinessLinesAsync()` mapea a `List<BusinessLineDto>`
- [ ] Verificar contrato FE ↔ BE: `GET /v1/quotes/{folio}/locations` — campos §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `PUT /v1/quotes/{folio}/locations` — campos request/response §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `PATCH /v1/quotes/{folio}/locations/{index}` — campos §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `GET /v1/quotes/{folio}/locations/summary` — campos §3.4 coinciden con §3.5b
- [ ] Verificar contrato FE ↔ BE: `GET /v1/zip-codes/{zipCode}` — proxy response §3.5 coincide con §3.5b
- [ ] Verificar contrato FE ↔ BE: `GET /v1/business-lines` — proxy response §3.5 coincide con §3.5b
- [ ] Verificar que query keys, invalidación de caché y manejo de errores FE están alineados con los códigos HTTP del BE
- [ ] Reportar CONTRACT_DRIFT si hay discrepancias

### 8.2 backend-developer

- [ ] Crear `LocationGuarantee` en `Cotizador.Domain/ValueObjects/`
- [ ] Modificar `Location.Guarantees` en `Cotizador.Domain/Entities/Location.cs` — cambiar `List<string>` → `List<LocationGuarantee>`
- [ ] Crear constante `GuaranteesNotRequiringInsuredAmount` en `Cotizador.Domain/Constants/GuaranteeKeys.cs` (glass, illuminated_signs)
- [ ] Crear DTOs: `LocationDto`, `LocationGuaranteeDto`, `UpdateLocationsRequest`, `PatchLocationRequest`, `LocationSummaryDto`, `LocationsSummaryResponse`, `LocationsResponse`
- [ ] Crear validador FluentValidation `UpdateLocationsRequestValidator`
  - locations: array, each with index unique/>=1, locationName required, address required
  - guarantees[].guaranteeKey in GuaranteeKeys.All, insuredAmount >= 0
  - version: required, > 0
- [ ] Crear validador FluentValidation `PatchLocationRequestValidator`
  - version: required, > 0
  - campos opcionales con validación condicional
- [ ] Implementar lógica de calculabilidad como método en Domain o helper en Application
  - Evalúa: zipCode presente, businessLine.fireKey presente, ≥1 garantía, insuredAmount > 0 para guarantees que lo requieren
  - Retorna: `validationStatus` + `blockingAlerts`
- [ ] Crear `IGetLocationsUseCase` + implementar `GetLocationsUseCase`
- [ ] Crear `IUpdateLocationsUseCase` + implementar `UpdateLocationsUseCase`
  - Flujo: validar → evaluar calculabilidad por ubicación → llamar UpdateLocationsAsync → re-leer → retornar
- [ ] Crear `IPatchLocationUseCase` + implementar `PatchLocationUseCase`
  - Flujo: validar → verificar que índice existe → evaluar calculabilidad → llamar PatchLocationAsync → re-leer → retornar
- [ ] Crear `IGetLocationsSummaryUseCase` + implementar `GetLocationsSummaryUseCase`
- [ ] Crear `IGetZipCodeUseCase` + implementar `GetZipCodeUseCase` (passthrough a `ICoreOhsClient.GetZipCodeAsync()`)
- [ ] Crear `IGetBusinessLinesUseCase` + implementar `GetBusinessLinesUseCase` (passthrough a `ICoreOhsClient.GetBusinessLinesAsync()`)
- [ ] Agregar endpoints en `QuoteController`:
  - `GET /v1/quotes/{folio}/locations`
  - `PUT /v1/quotes/{folio}/locations`
  - `PATCH /v1/quotes/{folio}/locations/{index}`
  - `GET /v1/quotes/{folio}/locations/summary`
- [ ] Agregar endpoints proxy en `CatalogController`:
  - `GET /v1/zip-codes/{zipCode}` → `GetZipCodeUseCase`
  - `GET /v1/business-lines` → `GetBusinessLinesUseCase`
- [ ] Registrar todos los Use Cases en `Program.cs`
- [ ] Mensajes de error en español (ADR-008)

### 8.3 frontend-developer

- [ ] Crear `entities/location/` — types, schema Zod, queries (locations + summary), api
- [ ] Crear `entities/zip-code/` — types, query (enabled: cp.length===5, staleTime 30min), api → `GET /v1/zip-codes/{cp}` (backend proxy)
- [ ] Crear `entities/business-line/` — types, query (staleTime 30min), api → `GET /v1/business-lines` (backend proxy)
- [ ] Crear `features/save-locations/` — `useSaveLocations` mutation (PUT completo)
- [ ] Crear `features/edit-location/` — `useEditLocation` mutation (PATCH)
- [ ] Crear `features/add-location/` — lógica local que agrega al array
- [ ] Crear `widgets/locations-grid/` — grilla configurable con badges de estado, botones agregar/editar/eliminar
- [ ] Crear `widgets/location-form/` — formulario completo: datos físicos, selector giro, auto-resolución CP, checkboxes garantías con input de suma asegurada
- [ ] Crear `pages/locations/` — `LocationsPage` ensambla grilla + layout config (SPEC-005)
- [ ] Integrar `useFormPersist` con key `wizard:{folio}:step:2` (ADR-007)
- [ ] Agregar ruta `/quotes/:folio/locations` en `app/router/router.tsx`
- [ ] Agregar endpoints en `shared/api/endpoints.ts`
- [ ] Labels y strings en español (ADR-008)

### 8.4 test-engineer-backend

- [ ] `GetLocationsUseCaseTests` — folio con ubicaciones → retorna array mapeado
- [ ] `GetLocationsUseCaseTests` — folio sin ubicaciones → retorna array vacío
- [ ] `GetLocationsUseCaseTests` — folio inexistente → throws FolioNotFoundException
- [ ] `UpdateLocationsUseCaseTests` — array válido con ubicación calculable → persiste con status "calculable"
- [ ] `UpdateLocationsUseCaseTests` — ubicación sin CP → persiste con status "incomplete" + alerta
- [ ] `UpdateLocationsUseCaseTests` — ubicación sin fireKey → persiste con status "incomplete" + alerta
- [ ] `UpdateLocationsUseCaseTests` — ubicación sin garantías → persiste con status "incomplete" + alerta
- [ ] `UpdateLocationsUseCaseTests` — garantía con requiresInsuredAmount=true y insuredAmount=0 → alerta
- [ ] `UpdateLocationsUseCaseTests` — garantía glass con insuredAmount=0 → NO genera alerta
- [ ] `UpdateLocationsUseCaseTests` — version mismatch → throws VersionConflictException
- [ ] `UpdateLocationsUseCaseTests` — guaranteeKey inválida → throws ValidationException
- [ ] `PatchLocationUseCaseTests` — ubicación existente → actualiza solo esa ubicación
- [ ] `PatchLocationUseCaseTests` — índice inexistente → throws (404 wrapped)
- [ ] `PatchLocationUseCaseTests` — version mismatch → throws VersionConflictException
- [ ] `PatchLocationUseCaseTests` — recalcula validationStatus tras patch
- [ ] `GetLocationsSummaryUseCaseTests` — 2 calculables + 1 incomplete → retorna totales correctos
- [ ] `GetZipCodeUseCaseTests` — CP existe → retorna ZipCodeDto
- [ ] `GetZipCodeUseCaseTests` — CP no existe → retorna null / 404
- [ ] `GetBusinessLinesUseCaseTests` — retorna lista de giros

### 8.5 test-engineer-frontend

- [ ] `LocationsGrid.test.tsx` — renderiza ubicaciones con badges de estado
- [ ] `LocationForm.test.tsx` — renderiza campos, auto-resolución CP mock
- [ ] `LocationForm.test.tsx` — seleccionar giro desde catálogo
- [ ] `LocationForm.test.tsx` — seleccionar garantías con suma asegurada
- [ ] `LocationForm.test.tsx` — validación Zod: nombre vacío muestra error
- [ ] `LocationForm.test.tsx` — submit exitoso invoca mutación
- [ ] `useLocationsQuery.test.ts` — fetch correcto mapea respuesta
- [ ] `useSaveLocations.test.ts` — mutación exitosa invalida queries
- [ ] `useEditLocation.test.ts` — PATCH exitoso invalida queries
- [ ] `useZipCodeQuery.test.ts` — CP de 5 dígitos dispara query, menos de 5 no
- [ ] `locationSchema.test.ts` — validaciones

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready):**
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED` (bloquea frontend)
- [ ] SPEC-001 implementada (core-mock: zip-codes, business-lines, guarantees)
- [ ] SPEC-002 implementada (Domain + Repository con UpdateLocationsAsync + PatchLocationAsync)
- [ ] SPEC-003 implementada (folio existe)
- [ ] SPEC-005 implementada (layout para la grilla)

**DoD (Definition of Done):**
- [ ] `GET /v1/quotes/{folio}/locations` responde según contrato §3.4
- [ ] `PUT /v1/quotes/{folio}/locations` responde según contrato §3.4 (reemplazo atómico + versionado)
- [ ] `PATCH /v1/quotes/{folio}/locations/{index}` responde según contrato §3.4 (edición granular)
- [ ] `GET /v1/quotes/{folio}/locations/summary` responde según contrato §3.4
- [ ] `GET /v1/zip-codes/{zipCode}` responde como proxy según contrato §3.5
- [ ] `GET /v1/business-lines` responde como proxy según contrato §3.5
- [ ] Frontend consume catálogos exclusivamente a través del backend
- [ ] `validationStatus` calculado en el backend (no confiar solo en el frontend)
- [ ] `insuredAmount = 0` aceptado para garantías con `requiresInsuredAmount: false`
- [ ] `insuredAmount > 0` requerido como condición de calculabilidad para garantías que lo necesitan
- [ ] Ubicaciones incompletas generan alertas pero NO bloquean el guardado
- [ ] PATCH modifica solo la ubicación indicada, las demás permanecen intactas
- [ ] Versionado optimista funcional (409 ante conflicto, reemplazo atómico en PUT)
- [ ] `metadata.lastWizardStep` se actualiza a 2
- [ ] Mensajes de error en español (ADR-008)
- [ ] Tests unitarios BE y FE pasando
- [ ] Sin violaciones de Clean Architecture
- [ ] Sin violaciones de reglas FSD
