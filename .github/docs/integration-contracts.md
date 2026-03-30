# Integration Contracts

> Documento generado por el agente `integration`. Última actualización: 2026-03-29.
>
> Este documento registra los contratos verificados entre capas y los drifts detectados para cada SPEC.
> Formato de secciones: `## SPEC-XXX — <feature>` → subsecciones por endpoint.

---

## SPEC-005 — location-layout-configuration

> Verificación realizada el 2026-03-29.
> Spec: `.github/specs/location-layout-configuration.spec.md`
> Backend verificado: `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs`
> DTOs verificados: `Cotizador.Application/DTOs/LayoutConfigurationDto.cs`, `UpdateLayoutRequest.cs`
> Validator verificado: `Cotizador.Application/Validators/UpdateLayoutRequestValidator.cs`
> Middleware verificado: `Cotizador.API/Middleware/ExceptionHandlingMiddleware.cs`

---

### GET /v1/quotes/{folio}/locations/layout

**Contrato verificado:** Sí
**Fecha verificación:** 2026-03-29

#### §3.4 vs §3.5b — Spec interna

| Punto de verificación | §3.4 (Backend contract) | §3.5b (FE ↔ BE contract) | Estado |
|---|---|---|---|
| Response envelope | `{ "data": { ... } }` | `{ "data": { ... } }` | ✅ Consistente |
| Campos response | `displayMode`, `visibleColumns`, `version` | `displayMode`, `visibleColumns`, `version` | ✅ Consistente |
| Query key | N/A | `['layout', folio]` — correcto para el patrón del endpoint | ✅ Correcto |
| Error 400 (folio inválido) | ✅ Definido en §3.4 | ❌ **No listado** en §3.5b como error manejado | ⚠️ DRIFT |
| Error 401 (no autorizado) | ✅ Definido en §3.4 | ❌ **No listado** en §3.5b | ⚠️ (global, ver nota) |
| Error 404 (folio no encontrado) | ✅ Definido en §3.4 | ✅ Manejado en §3.5b | ✅ Consistente |
| Error 500 (interno) | ✅ Definido en §3.4 | ✅ Manejado en §3.5b | ✅ Consistente |

> **Nota sobre 401:** La gestión de 401 es típicamente responsabilidad de un interceptor HTTP global, no de cada endpoint. No se clasifica como drift crítico.

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.4 | Implementación real | Estado |
|---|---|---|---|
| Ruta | `GET /v1/quotes/{folio}/locations/layout` | `[HttpGet("{folio}/locations/layout")]` en `[Route("v1/quotes")]` | ✅ Coincide |
| Autorización | Basic Auth `[Authorize]` | `[Authorize]` en el controller | ✅ Coincide |
| Validación folio | `^DAN-\d{4}-\d{5}$` → 400 | `Regex.IsMatch(folio, FolioConstants.FolioPattern)` → 400 con envelope correcto | ✅ Coincide |
| Response envelope | `{ "data": { ... } }` | `Ok(new { data = dto })` | ✅ Coincide |
| Use case | `GetLayoutUseCase` | `_getLayoutUseCase.ExecuteAsync(folio, ct)` | ✅ Coincide |
| DTO campos | `displayMode`, `visibleColumns`, `version` | `LayoutConfigurationDto(string DisplayMode, List<string> VisibleColumns, int Version)` — camelCase por defecto ASP.NET Core | ✅ Coincide |
| Error 404 | `folioNotFound` | Middleware: `FolioNotFoundException` → 404 `folioNotFound` | ✅ Coincide |
| Error 401 | `unauthorized` | `[Authorize]` del esquema BasicAuth | ✅ Coincide |
| Error 500 | `internal` | Middleware: `Exception` general → 500 `internal` | ✅ Coincide |

**Status:** ⚠️ CONTRACT_DRIFT (menor — ver DRIFT-005-01)

---

### PUT /v1/quotes/{folio}/locations/layout

**Contrato verificado:** Sí
**Fecha verificación:** 2026-03-29

#### §3.4 vs §3.5b — Spec interna

| Punto de verificación | §3.4 (Backend contract) | §3.5b (FE ↔ BE contract) | Estado |
|---|---|---|---|
| Request campos | `displayMode`, `visibleColumns`, `version` | `displayMode`, `visibleColumns`, `version` | ✅ Consistente |
| Response envelope | `{ "data": { ... } }` | `{ "data": { ... } }` | ✅ Consistente |
| Response campos | `displayMode`, `visibleColumns`, `version` (incrementado) | `displayMode`, `visibleColumns`, `version` (incrementado) | ✅ Consistente |
| Invalidación caché | N/A | `['layout', folio]` — coincide con la query key del GET | ✅ Correcto |
| Error 400 (validación) | ✅ Definido en §3.4 | ✅ Manejado en §3.5b | ✅ Consistente |
| Error 401 (no autorizado) | ✅ Definido en §3.4 | ❌ **No listado** en §3.5b | ⚠️ (global, ver nota) |
| Error 404 (folio no encontrado) | ✅ Definido en §3.4 | ❌ **No listado** en §3.5b como error manejado | ⚠️ DRIFT |
| Error 409 (conflicto de versión) | ✅ Definido en §3.4 | ✅ Manejado en §3.5b | ✅ Consistente |
| Error 500 (interno) | ✅ Definido en §3.4 | ✅ Manejado en §3.5b | ✅ Consistente |

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.4 | Implementación real | Estado |
|---|---|---|---|
| Ruta | `PUT /v1/quotes/{folio}/locations/layout` | `[HttpPut("{folio}/locations/layout")]` en `[Route("v1/quotes")]` | ✅ Coincide |
| Autorización | Basic Auth `[Authorize]` | `[Authorize]` en el controller | ✅ Coincide |
| Validación folio | `^DAN-\d{4}-\d{5}$` → 400 | `Regex.IsMatch(folio, FolioConstants.FolioPattern)` → 400 con envelope correcto | ✅ Coincide |
| Request body campos | `displayMode`, `visibleColumns`, `version` | `UpdateLayoutRequest(string DisplayMode, List<string> VisibleColumns, int Version)` — camelCase en deserialización | ✅ Coincide |
| Validación `displayMode` | Requerido, enum: "grid"\|"list" → 400 | `UpdateLayoutRequestValidator`: `NotEmpty()` + `Must(mode => ValidDisplayModes.Contains(mode))` con mensaje correcto | ✅ Coincide |
| Validación `visibleColumns` | Requerido, array no vacío → 400 | `NotNull()` + `Must(cols => cols.Count > 0)` + `RuleForEach` para columnas válidas | ✅ Coincide |
| Validación `version` | Requerido, entero > 0 → 400 | `GreaterThan(0)` | ✅ Coincide |
| Columnas válidas (15) | `index`, `locationName`, `address`, etc. | `ValidColumns` HashSet con las 15 columnas exactas de §2.3 | ✅ Coincide |
| Response envelope | `{ "data": { ... } }` | `Ok(new { data = dto })` | ✅ Coincide |
| Use case | `UpdateLayoutUseCase` | `_updateLayoutUseCase.ExecuteAsync(folio, request, ct)` | ✅ Coincide |
| Error 400 | `validationError` | Middleware: `ValidationException` → 400 `validationError` | ✅ Coincide |
| Error 404 | `folioNotFound` | Middleware: `FolioNotFoundException` → 404 `folioNotFound` | ✅ Coincide |
| Error 409 | `versionConflict` | Middleware: `VersionConflictException` → 409 `versionConflict` | ✅ Coincide |
| Error 500 | `internal` | Middleware: `Exception` general → 500 `internal` | ✅ Coincide |

**Status:** ⚠️ CONTRACT_DRIFT (medio — ver DRIFT-005-02)

---

### Compatibilidad de tipos (§3.6 FE vs DTOs BE)

| Campo | BE C# | JSON serializado | TypeScript esperado (§3.6) | Compatible |
|---|---|---|---|---|
| `displayMode` | `string` | `"grid"` \| `"list"` | `'grid' \| 'list'` | ✅ Sí |
| `visibleColumns` | `List<string>` | `string[]` | `string[]` | ✅ Sí |
| `version` | `int` | `number` | `number` | ✅ Sí |
| `displayMode` (request) | `string` | deserializa camelCase | `string` en `UpdateLayoutRequest` TS | ✅ Sí |

> Serialización JSON: `AddControllers()` en ASP.NET Core aplica `JsonNamingPolicy.CamelCase` por defecto. `DisplayMode` → `displayMode`, `VisibleColumns` → `visibleColumns`, `Version` → `version`. ✅

---

## Drifts detectados

### DRIFT-005-01

```
Integración:       FE ↔ BE
Endpoint:          GET /v1/quotes/{folio}/locations/layout
Tipo:              Error no manejado en §3.5b
Severidad:         Menor
Detalle:           §3.4 define HTTP 400 (folio con formato inválido). §3.5b solo lista 404 y 500.
                   Si por algún motivo el `folio` llega malformado al endpoint, el BE retorna 400
                   con body `{ type: "validationError", ... }`. El FE no tiene comportamiento
                   definido para este caso.
Impacto:           Bajo — bajo condiciones normales el FE construye el folio desde el router y
                   no genera folios inválidos. Sin embargo, el contrato está incompleto.
Acción requerida:  Agregar en §3.5b el manejo de 400 para GET /v1/quotes/{folio}/locations/layout:
                   "400: notificación de error inesperado (no debería ocurrir en flujo normal)".
Responsable:       frontend-developer (actualizar useSaveLayout y/o interceptor global)
```

### DRIFT-005-02

```
Integración:       FE ↔ BE
Endpoint:          PUT /v1/quotes/{folio}/locations/layout
Tipo:              Error no manejado en §3.5b
Severidad:         Medio
Detalle:           §3.4 define HTTP 404 (folioNotFound) para el caso en que el folio no exista
                   al momento del PUT. §3.5b solo lista 400, 409 y 500. Si el folio es eliminado
                   entre el GET (que carga el layout) y el PUT (que guarda los cambios), el BE
                   retornará 404 pero el FE no tiene comportamiento definido para manejarlo.
Impacto:           Medio — el usuario vería un error genérico sin contexto. El flujo de usuario
                   quedaría bloqueado sin una notificación clara de que el folio ya no existe.
Acción requerida:  Agregar en §3.5b el manejo de 404 para PUT /v1/quotes/{folio}/locations/layout:
                   "404: notificación 'El folio no existe. Regresa a la búsqueda de folios'
                   y redirección a la página de búsqueda".
Responsable:       frontend-developer (actualizar useSaveLayout.ts para manejar 404 → redirect)
```

---

### Conclusión SPEC-005

**Resultado:** `CONTRACT_DRIFT_DETECTED`

**Discrepancias encontradas:** 2 drifts menores/medios entre §3.4 y §3.5b. La implementación real del backend es **totalmente consistente con la spec §3.4**.

| # | Drift | Endpoint afectado | Severidad | Bloquea Fase 3 |
|---|---|---|---|---|
| DRIFT-005-01 | FE no maneja HTTP 400 en GET | GET .../layout | Menor | No |
| DRIFT-005-02 | FE no maneja HTTP 404 en PUT | PUT .../layout | Medio | No |

**Implementación backend:** ✅ Sin drift — todos los endpoints, DTOs, validaciones, codes de error y envelope `{ "data": {...} }` coinciden exactamente con §3.4.

**Bloqueo a Fase 3:** No bloqueado. Los drifts detectados son de cobertura de errores en el FE, no de incompatibilidad estructural de request/response. El contrato funcional (campos, tipos, rutas, query keys, invalidación de caché) es completamente consistente entre §3.4 y §3.5b. Se recomienda que `frontend-developer` corrija los drifts antes de finalizar la implementación.

---

## SPEC-006 — location-management

> Verificación realizada el 2026-03-29.
> Spec: `.github/specs/location-management.spec.md`
> Backend verificado: `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs` · `CatalogController.cs`
> DTOs verificados: `Cotizador.Application/DTOs/LocationDto.cs` · `UpdateLocationsRequest.cs` · `PatchLocationRequest.cs` · `SingleLocationResponse.cs` · `LocationsSummaryResponse.cs` · `LocationSummaryDto.cs` · `LocationGuaranteeDto.cs` · `ZipCodeDto.cs` · `BusinessLineDto.cs`
> Use cases verificados: `GetLocationsUseCase.cs` · `UpdateLocationsUseCase.cs` · `PatchLocationUseCase.cs` · `GetLocationsSummaryUseCase.cs` · `GetZipCodeUseCase.cs` · `GetBusinessLinesUseCase.cs`
> Frontend verificado: `entities/location/api/locationApi.ts` · `entities/location/model/types.ts` · `entities/zip-code/model/types.ts` · `entities/business-line/model/types.ts` · `features/save-locations/model/useSaveLocations.ts` · `shared/api/endpoints.ts`
> Mock verificado: `cotizador-core-mock/src/routes/zipCodeRoutes.ts` · `cotizador-core-mock/src/fixtures/zipCodes.json` · `cotizador-core-mock/src/fixtures/businessLines.json`

---

## Contratos BE ↔ core-ohs

### GET /v1/zip-codes/{zipCode}

**Contrato verificado:** Sí (con drift menor — ver DRIFT-006-06)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

| Componente | Valor |
|---|---|
| Ruta en core-ohs mock | `GET /v1/zip-codes/:zipCode` |
| Consumido por (BE) | `GetZipCodeUseCase` → `ICoreOhsClient.GetZipCodeAsync()` |
| Propósito | Resolver datos geográficos (estado, municipio, colonia, zona cat.) a partir de un CP de 5 dígitos |

**Request:**
```
Parámetro de ruta: zipCode (string, 5 dígitos numéricos)
```

**Response 200:**
```json
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
```

**Response 404:**
```json
{ "type": "zipCodeNotFound", "message": "Código postal no encontrado" }
```
> ⚠️ La spec define `"field": null` en la respuesta de error. El mock no incluye la propiedad `field`. Ver DRIFT-006-06.

#### Verificación contra mock (`cotizador-core-mock/`)

| Punto de verificación | Spec §3.5 | Mock real | Estado |
|---|---|---|---|
| Ruta | `GET /v1/zip-codes/{zipCode}` | `router.get('/:zipCode', ...)` (montado en `/v1/zip-codes`) | ✅ Coincide |
| Envelope response 200 | `{ "data": { ... } }` | `{ data: found }` con `ApiResponse<ZipCodeData>` | ✅ Coincide |
| Campo `zipCode` | `string` | `"06600"` en fixture | ✅ Presente |
| Campo `state` | `string` | `"Ciudad de México"` en fixture | ✅ Presente |
| Campo `municipality` | `string` | `"Cuauhtémoc"` en fixture | ✅ Presente |
| Campo `neighborhood` | `string` | `"Doctores"` en fixture | ✅ Presente |
| Campo `city` | `string` | `"Ciudad de México"` en fixture | ✅ Presente |
| Campo `catZone` | `string` ("A"\|"B"\|"C") | `"A"` en fixture | ✅ Presente |
| Campo `technicalLevel` | `number` | `2` en fixture | ✅ Presente |
| Response 404 type | `zipCodeNotFound` | `"zipCodeNotFound"` | ✅ Coincide |
| Response 404 message | `"Código postal no encontrado"` | `"Código postal no encontrado"` | ✅ Coincide |
| Response 404 `field: null` | Definido en §3.5 | **No presente en mock** | ⚠️ DRIFT |

#### Verificación contra BE DTO (`ZipCodeDto.cs`)

| Campo spec §3.5 | Campo `ZipCodeDto` (C#) | JSON serializado | Estado |
|---|---|---|---|
| `zipCode` | `ZipCode` (string) | `zipCode` | ✅ Coincide |
| `state` | `State` (string) | `state` | ✅ Coincide |
| `municipality` | `Municipality` (string) | `municipality` | ✅ Coincide |
| `neighborhood` | `Neighborhood` (string) | `neighborhood` | ✅ Coincide |
| `city` | `City` (string) | `city` | ✅ Coincide |
| `catZone` | `CatZone` (string) | `catZone` | ✅ Coincide |
| `technicalLevel` | `TechnicalLevel` (int) | `technicalLevel` | ✅ Coincide |

**Status:** ⚠️ CONTRACT_DRIFT menor (ver DRIFT-006-06)

---

### GET /v1/business-lines

**Contrato verificado:** Sí
**Fecha verificación:** 2026-03-29

#### Definición del contrato

| Componente | Valor |
|---|---|
| Ruta en core-ohs mock | `GET /v1/business-lines` (montado en raíz) |
| Consumido por (BE) | `GetBusinessLinesUseCase` → `ICoreOhsClient.GetBusinessLinesAsync()` |
| Propósito | Obtener catálogo de giros comerciales con su `fireKey` para clasificación de tarifa de incendio |

**Response 200:**
```json
{
  "data": [
    { "code": "BL-001", "description": "Bodega de almacenamiento", "fireKey": "B-03", "riskLevel": "medium" },
    { "code": "BL-002", "description": "Tienda de retail", "fireKey": "C-01", "riskLevel": "low" }
  ]
}
```

#### Verificación contra mock

| Punto de verificación | Spec §3.5 | Mock real | Estado |
|---|---|---|---|
| Ruta | `GET /v1/business-lines` | `router.get('/', ...)` montado en `/v1/business-lines` | ✅ Coincide |
| Envelope response 200 | `{ "data": [...] }` | `{ data: businessLines }` con `ApiResponse<BusinessLine[]>` | ✅ Coincide |
| Campo `code` | `string` | `"BL-001"` en fixture | ✅ Presente |
| Campo `description` | `string` | `"Bodega de almacenamiento"` (contenido en español, spec muestra ejemplo en inglés) | ✅ Estructura correcta |
| Campo `fireKey` | `string` | `"B-03"` en fixture | ✅ Presente |
| Campo `riskLevel` | `string` | `"medium"` en fixture | ✅ Presente |
| Manejo 503 | No simulado en mock | No implementado | ℹ️ Esperado (mock simula uptime) |

**Status:** ✅ Sin drift estructural

---

## Contratos FE ↔ BE

### GET /v1/quotes/{folio}/locations

**Contrato verificado:** Sí (con drift crítico — ver DRIFT-006-01)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

**Consumido por (FE):**
- Archivo: `entities/location/api/locationApi.ts` → `getLocations(folio)`
- Hook/Query: `useLocationsQuery` (TanStack Query `useQuery`)
- Query Key: `['locations', folio]`
- Endpoint resuelto: `endpoints.locations.list(folio)` → `/v1/quotes/${folio}/locations`

**Response 200 esperado por el FE:**
```json
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
```

**Errores manejados por el FE (§3.5b):**
- 404: notificación "El folio no existe"
- 500: notificación genérica

**Invalidación de caché:** Al mutar (PUT/PATCH exitoso), invalida `['locations', folio]` y `['locations-summary', folio]`

#### §3.4 vs §3.5b — Spec interna

| Punto de verificación | §3.4 | §3.5b | Estado |
|---|---|---|---|
| Ruta | `GET /v1/quotes/{folio}/locations` | `/v1/quotes/${folio}/locations` | ✅ Coincide |
| Response envelope | `{ "data": { locations, version } }` | `{ "data": { locations, version } }` | ✅ Coincide |
| Query Key | N/A | `['locations', folio]` | ✅ Correcto |
| Error 400 (folio inválido) | ✅ Definido | ❌ No listado en §3.5b | ⚠️ Drift menor |
| Error 401 | ✅ Definido | ❌ No listado (global) | ℹ️ Interceptor global |
| Error 404 | ✅ Definido | ✅ Manejado | ✅ Coincide |
| Error 500 | ✅ Definido | ✅ Manejado | ✅ Coincide |

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.4 | Implementación real | Estado |
|---|---|---|---|
| Endpoint en controller | `GET /v1/quotes/{folio}/locations` | **No existe en QuoteController** | ❌ DRIFT CRÍTICO |
| Use case | `GetLocationsUseCase` | Implementado en `GetLocationsUseCase.cs` | ✅ Use case OK |
| DTO response | `LocationsResponse(locations, version)` | `LocationsResponse.cs` tiene `List<LocationDto> Locations, int Version` | ✅ DTO OK |
| Mapper | `LocationMapper.ToDto` | **`LocationMapper` no existe en código fuente** | ❌ DRIFT CRÍTICO |

**Status:** ❌ CONTRACT_DRIFT CRÍTICO (ver DRIFT-006-01 y DRIFT-006-03)

---

### PUT /v1/quotes/{folio}/locations

**Contrato verificado:** Sí (con drifts — ver DRIFT-006-01, DRIFT-006-03, DRIFT-006-04, DRIFT-006-05)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

**Consumido por (FE):**
- Archivo: `features/save-locations/model/useSaveLocations.ts` → `useMutation` → `updateLocations(folio, body)`
- Endpoint resuelto: `endpoints.locations.update(folio)` → `/v1/quotes/${folio}/locations`

**Request FE → BE:**
```json
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
      "guarantees": [{ "guaranteeKey": "building_fire", "insuredAmount": 5000000 }],
      "catZone": "A"
    }
  ],
  "version": 2
}
```
> Nota: FE envía `Omit<LocationDto, 'blockingAlerts' | 'validationStatus'>[]`. Los campos `blockingAlerts` y `validationStatus` se omiten (son computados por el BE).

**Response 200 esperado por el FE:**
```json
{
  "data": {
    "locations": [ { ...LocationDto completo con blockingAlerts y validationStatus... } ],
    "version": 3
  }
}
```

**Errores manejados por el FE (§3.5b):**
- 400: errores de validación en formulario
- 409: alerta "El folio fue modificado, recarga para continuar"
- 500: notificación genérica

**Invalidación de caché:** Invalida `['locations', folio]` y `['locations-summary', folio]`

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.4 | Implementación real | Estado |
|---|---|---|---|
| Endpoint en controller | `PUT /v1/quotes/{folio}/locations` | **No existe en QuoteController** | ❌ DRIFT CRÍTICO |
| Use case | `UpdateLocationsUseCase` | Implementado, lógica correcta | ✅ Use case OK |
| DTO request | `UpdateLocationsRequest(List<LocationDto>, int Version)` | Reusa `LocationDto` (incluye campos output `blockingAlerts`, `validationStatus`) | ⚠️ DRIFT MEDIO |
| DTO response | `LocationsResponse(locations, version)` | `LocationsResponse.cs` correcto | ✅ DTO OK |
| Mapper | `LocationMapper.ToEntity` / `ToDto` | **`LocationMapper` no existe** | ❌ DRIFT CRÍTICO |

**Status:** ❌ CONTRACT_DRIFT CRÍTICO (ver DRIFT-006-01, DRIFT-006-03, DRIFT-006-04, DRIFT-006-05)

---

### PATCH /v1/quotes/{folio}/locations/{index}

**Contrato verificado:** Sí (con drift crítico — ver DRIFT-006-01, DRIFT-006-03)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

**Consumido por (FE):**
- Archivo: `entities/location/api/locationApi.ts` → `patchLocation(folio, index, body)`
- Hook/Query: `useEditLocation` → `useMutation` (TanStack Query)
- Endpoint resuelto: `endpoints.locations.patch(folio, index)` → `/v1/quotes/${folio}/locations/${index}`

**Request FE → BE (parcial — solo campos que cambian):**
```json
{
  "zipCode": "03100",
  "state": "Ciudad de México",
  "municipality": "Benito Juárez",
  "neighborhood": "Del Valle",
  "city": "Ciudad de México",
  "catZone": "B",
  "version": 5
}
```

**Response 200 esperado por el FE (`PatchLocationResponse`):**
```json
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
    "guarantees": [{ "guaranteeKey": "building_fire", "insuredAmount": 3000000 }],
    "catZone": "B",
    "blockingAlerts": [],
    "validationStatus": "calculable",
    "version": 6
  }
}
```

**Errores manejados por el FE (§3.5b):**
- 400: errores de validación
- 404: "Ubicación no encontrada"
- 409: alerta de conflicto de versión
- 500: notificación genérica

**Invalidación de caché:** Invalida `['locations', folio]` y `['locations-summary', folio]`

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.4 | Implementación real | Estado |
|---|---|---|---|
| Endpoint en controller | `PATCH /v1/quotes/{folio}/locations/{index}` | **No existe en QuoteController** | ❌ DRIFT CRÍTICO |
| Use case | `PatchLocationUseCase` | Implementado — lógica de merge correcta | ✅ Use case OK |
| DTO request | `PatchLocationRequest` (todos los campos opcionales + `version` requerido) | `PatchLocationRequest.cs` — todos los campos nullable + `int Version` | ✅ DTO OK |
| DTO response | `SingleLocationResponse` + `version` en el mismo objeto | `SingleLocationResponse.cs` tiene todos los campos de `LocationDto` + `Version` | ✅ DTO OK |
| FE type `PatchLocationResponse` | `{ data: LocationDto & { version: number } }` | `SingleLocationResponse` serializado = `{ ...locationFields, version }` | ✅ Compatibles |
| Mapper | `LocationMapper.ToSingleResponse` | **`LocationMapper` no existe** | ❌ DRIFT CRÍTICO |
| Error 404 ubicación missing | `{ type: "folioNotFound", message: "La ubicación con índice N no existe..." }` | Use case lanza `FolioNotFoundException(...)` — middleware mapea a 404 | ✅ Coincide |

**Status:** ❌ CONTRACT_DRIFT CRÍTICO (ver DRIFT-006-01, DRIFT-006-03)

---

### GET /v1/quotes/{folio}/locations/summary

**Contrato verificado:** Sí (con drift crítico — ver DRIFT-006-01)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

**Consumido por (FE):**
- Archivo: `entities/location/model/useLocationsSummaryQuery.ts`
- Hook/Query: `useQuery` (TanStack Query)
- Query Key: `['locations-summary', folio]`
- Endpoint resuelto: `endpoints.locations.summary(folio)` → `/v1/quotes/${folio}/locations/summary`

**Response 200 esperado por el FE:**
```json
{
  "data": {
    "locations": [
      { "index": 1, "locationName": "Bodega Central CDMX", "validationStatus": "calculable", "blockingAlerts": [] },
      { "index": 3, "locationName": "Almacén Norte", "validationStatus": "incomplete", "blockingAlerts": ["Código postal requerido", "Giro comercial requerido"] }
    ],
    "totalCalculable": 1,
    "totalIncomplete": 1,
    "version": 5
  }
}
```

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.4 | Implementación real | Estado |
|---|---|---|---|
| Endpoint en controller | `GET /v1/quotes/{folio}/locations/summary` | **No existe en QuoteController** | ❌ DRIFT CRÍTICO |
| Use case | `GetLocationsSummaryUseCase` | Archivo existe en UseCases/ | ✅ Use case existe |
| DTO response | `LocationsSummaryResponse(locations, totalCalculable, totalIncomplete, version)` | `LocationsSummaryResponse.cs` tiene exactamente esos campos | ✅ DTO OK |
| Sub-DTO | `LocationSummaryDto(index, locationName, validationStatus, blockingAlerts)` | `LocationSummaryDto.cs` tiene exactamente esos campos | ✅ DTO OK |

**Status:** ❌ CONTRACT_DRIFT CRÍTICO (ver DRIFT-006-01)

---

### GET /v1/zip-codes/{zipCode} (proxy BE → core-ohs → FE)

**Contrato verificado:** Sí (con drift crítico — ver DRIFT-006-02)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

**Consumido por (FE):**
- Archivo: `entities/zip-code/api/zipCodeApi.ts` → `getZipCode(cp)`
- Hook/Query: `useZipCodeQuery` → `useQuery(['zip-code', cp], enabled: cp?.length === 5)`
- Query Key: `['zip-code', zipCode]`
- Endpoint resuelto: `endpoints.zipCode.get(cp)` → `/v1/zip-codes/${cp}`
- staleTime: 30 min

**Response 200 esperado por el FE (`ZipCodeResponse`):**
```json
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
```

**Errores manejados por el FE (§3.5b):**
- 404: muestra "Código postal no encontrado" en el campo CP del formulario
- 503: alerta global de servicio no disponible

#### Compatibilidad de tipos FE ↔ BE

| Campo | BE `ZipCodeDto` (C#) | JSON serializado | FE `ZipCodeDto` (TypeScript) | Compatible |
|---|---|---|---|---|
| `zipCode` | `string ZipCode` | `zipCode` | `string` | ✅ Sí |
| `state` | `string State` | `state` | `string` | ✅ Sí |
| `municipality` | `string Municipality` | `municipality` | `string` | ✅ Sí |
| `neighborhood` | `string Neighborhood` | `neighborhood` | `string` | ✅ Sí |
| `city` | `string City` | `city` | `string` | ✅ Sí |
| `catZone` | `string CatZone` | `catZone` | `string` | ✅ Sí |
| `technicalLevel` | `int TechnicalLevel` | `technicalLevel` | `number` | ✅ Sí |

#### §3.4 vs Implementación real del backend

| Punto de verificación | Spec §3.5 | Implementación real | Estado |
|---|---|---|---|
| Endpoint en controller | `GET /v1/zip-codes/{zipCode}` | **No existe en CatalogController ni en otro controller** | ❌ DRIFT CRÍTICO |
| Use case | `GetZipCodeUseCase` | Archivo `GetZipCodeUseCase.cs` existe | ✅ Use case existe |
| DTO response | `ZipCodeDto` con 7 campos | `ZipCodeDto.cs` tiene exactamente los 7 campos | ✅ DTO OK |

**Status:** ❌ CONTRACT_DRIFT CRÍTICO (ver DRIFT-006-02)

---

### GET /v1/business-lines (proxy BE → core-ohs → FE)

**Contrato verificado:** Sí (con drift crítico — ver DRIFT-006-02)
**Fecha verificación:** 2026-03-29

#### Definición del contrato

**Consumido por (FE):**
- Archivo: `entities/business-line/api/businessLineApi.ts` → `getBusinessLines()`
- Hook/Query: `useBusinessLinesQuery` → `useQuery(['business-lines'], staleTime: 30min)`
- Query Key: `['business-lines']`
- Endpoint resuelto: `endpoints.businessLines` → `/v1/business-lines`

**Response 200 esperado por el FE (`BusinessLinesResponse`):**
```json
{
  "data": [
    { "code": "BL-001", "description": "Bodega de almacenamiento", "fireKey": "B-03", "riskLevel": "medium" }
  ]
}
```

#### Compatibilidad de tipos FE ↔ BE

| Campo | BE `BusinessLineDto` (C#) | JSON serializado | FE `BusinessLineDto` (TypeScript) | Compatible |
|---|---|---|---|---|
| `code` | `string Code` | `code` | `string` | ✅ Sí |
| `description` | `string Description` | `description` | `string` | ✅ Sí |
| `fireKey` | `string FireKey` | `fireKey` | `string` | ✅ Sí |
| `riskLevel` | `string RiskLevel` | `riskLevel` | `string` | ✅ Sí |

#### §3.5 vs Implementación real del backend

| Punto de verificación | Spec §3.5 | Implementación real | Estado |
|---|---|---|---|
| Endpoint en controller | `GET /v1/business-lines` | **No existe en CatalogController ni en otro controller** | ❌ DRIFT CRÍTICO |
| Use case | `GetBusinessLinesUseCase` | Archivo `GetBusinessLinesUseCase.cs` existe | ✅ Use case existe |
| DTO response | `BusinessLineDto` con 4 campos | `BusinessLineDto.cs` tiene `Code, Description, FireKey, RiskLevel` | ✅ DTO OK |

**Status:** ❌ CONTRACT_DRIFT CRÍTICO (ver DRIFT-006-02)

---

### Compatibilidad de tipos FE ↔ BE (ubicaciones)

| Campo | BE C# | JSON serializado | TypeScript esperado (§3.6) | Compatible |
|---|---|---|---|---|
| `index` | `int Index` | `index` | `number` | ✅ Sí |
| `locationName` | `string LocationName` | `locationName` | `string` | ✅ Sí |
| `address` | `string Address` | `address` | `string` | ✅ Sí |
| `zipCode` | `string ZipCode` | `zipCode` | `string` | ✅ Sí |
| `state` | `string State` | `state` | `string` | ✅ Sí |
| `municipality` | `string Municipality` | `municipality` | `string` | ✅ Sí |
| `neighborhood` | `string Neighborhood` | `neighborhood` | `string` | ✅ Sí |
| `city` | `string City` | `city` | `string` | ✅ Sí |
| `constructionType` | `string ConstructionType` | `constructionType` | `string` | ✅ Sí |
| `level` | `int Level` | `level` | `number` | ✅ Sí |
| `constructionYear` | `int ConstructionYear` | `constructionYear` | `number` | ✅ Sí |
| `locationBusinessLine` | `BusinessLineDto? LocationBusinessLine` | `locationBusinessLine` (nullable) | `BusinessLineDto \| null` | ✅ Sí |
| `locationBusinessLine.description` | `string Description` | `description` | `string` | ✅ Sí |
| `locationBusinessLine.fireKey` | `string FireKey` | `fireKey` | `string` | ✅ Sí |
| `locationBusinessLine.code` | `string Code` (en `BusinessLineDto`) | `code` (presente en JSON cuando se serialice) | **No en FE location `BusinessLineDto`** | ⚠️ Ver DRIFT-006-07 |
| `guarantees` | `List<LocationGuaranteeDto> Guarantees` | `guarantees` | `LocationGuaranteeDto[]` | ✅ Sí |
| `guarantees[].guaranteeKey` | `string GuaranteeKey` | `guaranteeKey` | `string` | ✅ Sí |
| `guarantees[].insuredAmount` | `decimal InsuredAmount` | `insuredAmount` | `number` | ✅ Sí |
| `catZone` | `string CatZone` | `catZone` | `string` | ✅ Sí |
| `blockingAlerts` | `List<string> BlockingAlerts` | `blockingAlerts` | `string[]` | ✅ Sí |
| `validationStatus` | `string ValidationStatus` | `validationStatus` | `'calculable' \| 'incomplete'` | ✅ Sí |

---

## Drifts detectados

### DRIFT-006-01

```
Integración:       FE ↔ BE
Endpoints:         GET /v1/quotes/{folio}/locations
                   PUT /v1/quotes/{folio}/locations
                   PATCH /v1/quotes/{folio}/locations/{index}
                   GET /v1/quotes/{folio}/locations/summary
Tipo:              Endpoints ausentes en controller
Severidad:         CRÍTICO
Detalle:           Los cuatro endpoints de gestión de ubicaciones definidos en §3.4 NO existen en
                   QuoteController.cs. Los use cases (GetLocationsUseCase, UpdateLocationsUseCase,
                   PatchLocationUseCase, GetLocationsSummaryUseCase) están implementados y los DTOs
                   son correctos, pero ninguno está expuesto por el controller.
                   El FE ya tiene locationApi.ts con las llamadas definidas, pero todas fallarán
                   con HTTP 404 (no route matched) al ejecutarse.
Impacto:           Bloqueo total del feature. Ninguna operación CRUD de ubicaciones funciona.
Acción requerida:  backend-developer debe agregar los 4 action methods a QuoteController.cs:
                   - [HttpGet("{folio}/locations")]
                   - [HttpPut("{folio}/locations")]
                   - [HttpPatch("{folio}/locations/{index}")]
                   - [HttpGet("{folio}/locations/summary")]
                   con inyección de los use cases correspondientes, validación de folio pattern y
                   envelope { data = ... }.
Responsable:       backend-developer
Bloquea Fase 3:    Sí
```

### DRIFT-006-02

```
Integración:       FE ↔ BE (proxy BE → core-ohs)
Endpoints:         GET /v1/zip-codes/{zipCode}
                   GET /v1/business-lines
Tipo:              Endpoints proxy ausentes en controller
Severidad:         CRÍTICO
Detalle:           Los dos endpoints proxy de catálogos definidos en §3.5 NO existen en
                   CatalogController.cs ni en ningún otro controller. Los use cases
                   (GetZipCodeUseCase, GetBusinessLinesUseCase) están implementados y los DTOs
                   son correctos, pero no están expuestos.
                   El FE llama a /v1/zip-codes/{cp} desde useZipCodeQuery y a /v1/business-lines
                   desde useBusinessLinesQuery. Ambas llamadas fallarán con HTTP 404.
Impacto:           El formulario de ubicaciones no puede resolver CP (auto-resolución de zona/estado/
                   municipio/colonia) ni consultar el catálogo de giros. El feature de captura de
                   ubicaciones es inutilizable sin estos endpoints.
Acción requerida:  backend-developer debe agregar a CatalogController.cs:
                   - [HttpGet("zip-codes/{zipCode}")] → GetZipCodeUseCase
                   - [HttpGet("business-lines")] → GetBusinessLinesUseCase
Responsable:       backend-developer
Bloquea Fase 3:    Sí
```

### DRIFT-006-03

```
Integración:       BE (interno)
Artefacto:         LocationMapper (clase estática con métodos ToDto, ToEntity, ToSingleResponse)
Tipo:              Clase ausente en código fuente
Severidad:         CRÍTICO
Detalle:           Los use cases GetLocationsUseCase, UpdateLocationsUseCase y PatchLocationUseCase
                   referencian LocationMapper.ToDto, LocationMapper.ToEntity y
                   LocationMapper.ToSingleResponse. La clase LocationMapper no existe en ningún
                   archivo del proyecto — su búsqueda en toda la solución no retorna resultados.
                   Esto genera un error de compilación: CS0103 - The name 'LocationMapper' does not
                   exist in the current context.
Impacto:           El proyecto no compila. Ningún endpoint puede servir peticiones.
Acción requerida:  backend-developer debe crear LocationMapper.cs en
                   Cotizador.Application/UseCases/ con los tres métodos estáticos:
                   - ToDto(Location) → LocationDto
                   - ToEntity(LocationDto) → Location
                   - ToSingleResponse(Location, int version) → SingleLocationResponse
Responsable:       backend-developer
Bloquea Fase 3:    Sí
```

### DRIFT-006-04

```
Integración:       BE (interno — DTO semántico)
Endpoint:          PUT /v1/quotes/{folio}/locations
Tipo:              DTO de request incluye campos de output (computed fields)
Severidad:         Medio
Detalle:           UpdateLocationsRequest usa List<LocationDto> para el array de ubicaciones.
                   LocationDto incluye BlockingAlerts y ValidationStatus, que son campos calculados
                   por el backend (no deben enviarse en el request).
                   La spec §3.4 muestra el request body sin blockingAlerts ni validationStatus.
                   El FE lo maneja correctamente con Omit<LocationDto, 'blockingAlerts' | 'validationStatus'>
                   pero el DTO de BE acepta (y silenciosamente ignora) esos campos si se envían.
Impacto:           Semántico — no rompe funcionalidad en tiempo de ejecución porque ASP.NET
                   deserializa con valores por defecto para campos faltantes. Sin embargo el contrato
                   del DTO es incorrecto y puede confundir a consumidores futuros.
Acción requerida:  Crear un DTO de request específico (LocationRequestDto o LocationInputDto)
                   sin BlockingAlerts ni ValidationStatus, y actualizar UpdateLocationsRequest
                   para usarlo: List<LocationRequestDto> Locations.
Responsable:       backend-developer
Bloquea Fase 3:    No (funciona, pero debe corregirse)
```

### DRIFT-006-05

```
Integración:       FE ↔ BE
Endpoint:          PUT /v1/quotes/{folio}/locations
Tipo:              Error no manejado en §3.5b
Severidad:         Medio
Detalle:           §3.4 define HTTP 404 (folioNotFound) para el caso en que el folio no exista
                   al momento del PUT. §3.5b solo lista 400, 409 y 500 como errores manejados.
                   useSaveLocations.ts maneja explícitamente versionConflict (409) y cae en el
                   handler genérico para todos los demás errores, incluyendo 404.
                   Si el folio se elimina entre la carga de la página y el envío del PUT, el usuario
                   recibirá el mensaje genérico de error sin contexto sobre qué ocurrió.
Impacto:           UX — el usuario ve error genérico sin saber que el folio ya no existe ni
                   tiene opción de redirigirse a búsqueda.
Acción requerida:  Agregar en useSaveLocations.ts (y en useSaveLayout.ts según DRIFT-005-02):
                   if (apiErr?.type === 'folioNotFound') { redirect('/quotes') + notificación }
Responsable:       frontend-developer
Bloquea Fase 3:    No
```

### DRIFT-006-06

```
Integración:       BE ↔ core-ohs
Endpoint:          GET /v1/zip-codes/{zipCode}
Tipo:              Campo faltante en respuesta de error del mock
Severidad:         Menor
Detalle:           Spec §3.5 define respuesta 404 como:
                     { "type": "zipCodeNotFound", "message": "Código postal no encontrado", "field": null }
                   El mock (zipCodeRoutes.ts) retorna:
                     { "type": "zipCodeNotFound", "message": "Código postal no encontrado" }
                   La propiedad "field" está ausente.
                   Si el backend proxy-ea directamente el body del error de core-ohs al FE, el FE
                   recibirá un error sin el campo "field".
Impacto:           Bajo — el FE no usa el campo "field" del error de zip-code en su manejo actual.
                   Sin embargo el contrato está incompleto según la spec.
Acción requerida:  Opción A: Actualizar zipCodeRoutes.ts para incluir field: null en el error.
                   Opción B: El backend proxy puede normalizar el error antes de re-retornarlo.
Responsable:       core-ohs mock maintainer (Opción A) o backend-developer (Opción B)
Bloquea Fase 3:    No
```

### DRIFT-006-07

```
Integración:       FE ↔ BE
Endpoint:          GET /v1/quotes/{folio}/locations (y PUT/PATCH respuestas)
Tipo:              Tipo DTO compartido entre contextos con diferente estructura esperada
Severidad:         Menor
Detalle:           El BE usa BusinessLineDto (4 campos: Code, Description, FireKey, RiskLevel)
                   tanto para el catálogo GET /v1/business-lines como para el sub-objeto
                   locationBusinessLine dentro de LocationDto.
                   El domain value object BusinessLine solo tiene Description y FireKey.
                   Al serializar LocationDto.LocationBusinessLine, el mapper (cuando se cree)
                   deberá crear BusinessLineDto con Code y RiskLevel vacíos/nulos para las
                   ubicaciones (ya que Business​Line VO no los tiene).
                   El FE en entities/location/model/types.ts define BusinessLineDto con solo
                   description y fireKey — esto es correcto para el contexto de ubicación, pero
                   los campos code y riskLevel aparecerán en el JSON (como "" o null) sin ser
                   consumidos.
Impacto:           Bajo — no rompe funcionalidad. El FE ignora campos extra. Sin embargo
                   el DTO reutilizado es semánticamente incorrecto para el contexto de ubicación.
Acción requerida:  Cuando se cree LocationMapper.ToDto, usar solo Description y FireKey al
                   mapear el business line de la ubicación. Considerar crear un DTO específico
                   LocationBusinessLineDto(string Description, string FireKey) para claridad.
Responsable:       backend-developer
Bloquea Fase 3:    No
```

---

### Conclusión SPEC-006

**Resultado:** `CONTRACT_DRIFT_DETECTED — BLOQUEO A FASE 3`

**Discrepancias encontradas:** 7 drifts (3 críticos, 2 medios, 2 menores).

| # | Drift | Integración | Severidad | Bloquea Fase 3 |
|---|---|---|---|---|
| DRIFT-006-01 | 4 endpoints de ubicaciones ausentes en QuoteController | FE ↔ BE | CRÍTICO | ✅ Sí |
| DRIFT-006-02 | 2 endpoints proxy ausentes en CatalogController | FE ↔ BE | CRÍTICO | ✅ Sí |
| DRIFT-006-03 | `LocationMapper` clase ausente — error de compilación | BE interno | CRÍTICO | ✅ Sí |
| DRIFT-006-04 | `UpdateLocationsRequest` reutiliza DTO con campos de output | BE interno | Medio | No |
| DRIFT-006-05 | FE no maneja 404 en PUT locations | FE ↔ BE | Medio | No |
| DRIFT-006-06 | 404 de core-mock sin propiedad `field` | BE ↔ core-ohs | Menor | No |
| DRIFT-006-07 | `BusinessLineDto` compartido entre contextos con diferente estructura | FE ↔ BE | Menor | No |

**Contratos verificados sin drift:** `GET /v1/business-lines` (BE ↔ core-ohs) ✅

**Bloqueo a Fase 3:** **SÍ — bloqueado.** Los drifts CRÍTICOS (DRIFT-006-01, DRIFT-006-02, DRIFT-006-03) impiden que el backend sea funcional: el proyecto no compila (`LocationMapper` faltante) y aunque compilara, los 6 endpoints requeridos por el FE no están expuestos en ningún controller. El `backend-developer` debe resolver estos tres drifts antes de pasar a Fase 3 (tests).

**Acción inmediata requerida:** Notificar a `backend-developer` — prioridad bloqueante:
1. Crear `LocationMapper.cs` con `ToDto`, `ToEntity` y `ToSingleResponse`
2. Agregar 4 action methods de ubicaciones a `QuoteController.cs`
3. Agregar 2 action methods de catálogos a `CatalogController.cs`
