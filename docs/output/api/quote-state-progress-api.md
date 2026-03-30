# API: Quote State & Progress (SPEC-008)

**Spec fuente:** `.github/specs/quote-state-progress.spec.md`  
**Status:** IMPLEMENTED — 2026-03-30  
**Audiencia:** Desarrollador técnico evaluador del reto

---

## Endpoint

### GET /v1/quotes/{folio}/state

Retorna el estado global del folio: progreso por sección, resumen de ubicaciones (calculables vs. incompletas con alertas), flag `readyForCalculation`, y resultado financiero opcional cuando el folio ya fue calculado.

**Propósito de diseño:** endpoint de observabilidad del folio. El frontend lo consulta al entrar a cada página del wizard (sin polling) para renderizar la barra de progreso y las alertas de ubicación.

---

## Detalles del contrato

| Atributo | Valor |
|---|---|
| Método | `GET` |
| Ruta | `/v1/quotes/{folio}/state` |
| Autenticación | Basic Auth (`Authorization: Basic <base64(user:pass)>`) |
| Controller | `QuoteController.GetQuoteStateAsync()` |
| Use Case | `GetQuoteStateUseCase.ExecuteAsync()` |
| Repositorio | `IQuoteRepository.GetByFolioNumberAsync()` |
| Servicios externos | Ninguno (sin llamadas a core-ohs) |

### Path Parameters

| Parámetro | Tipo | Formato | Requerido | Descripción |
|---|---|---|---|---|
| `folio` | string | `DAN-YYYY-NNNNN` | Sí | Número de folio de la cotización |

### Headers

| Header | Valor | Requerido |
|---|---|---|
| `Authorization` | `Basic dXNlcjpwYXNz` | Sí |
| `X-Correlation-Id` | UUID v4 | No (recomendado para trazabilidad) |

---

## Respuestas exitosas (200 OK)

### Variante A — Folio `draft` (recién creado, sin datos guardados)

```json
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
```

### Variante B — Folio `in_progress` (con datos, sin calcular)

```json
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
```

### Variante C — Folio `calculated` (con resultado financiero)

```json
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
      "commercialPremiumBeforeTax": 0,
      "commercialPremium": 174000.70,
      "premiumsByLocation": [
        {
          "locationIndex": 1,
          "locationName": "Bodega Central CDMX",
          "netPremium": 85000.30,
          "validationStatus": "calculable",
          "coveragePremiums": [
            {
              "guaranteeKey": "building_fire",
              "insuredAmount": 5000000.00,
              "rate": 0.00125,
              "premium": 6250.00
            }
          ]
        }
      ]
    }
  }
}
```

> **Nota — `commercialPremiumBeforeTax`:** Este campo se incluye en el DTO pero su valor es siempre `0` en SPEC-008. El motor de cálculo que popula este campo se implementa en **SPEC-009**. No usar este valor hasta que SPEC-009 esté implementado.

---

## Tabla de errores

| HTTP | `type` | `message` | `field` | Causa |
|---|---|---|---|---|
| 400 | `validationError` | `"Formato de folio inválido. Use DAN-YYYY-NNNNN"` | `"folio"` | El path param `folio` no coincide con `DAN-\d{4}-\d{5}` |
| 401 | `unauthorized` | `"Credenciales inválidas o ausentes"` | `null` | Sin header `Authorization` o credenciales incorrectas |
| 404 | `folioNotFound` | `"El folio DAN-2026-99999 no existe"` | `null` | `FolioNotFoundException` lanzado por el Use Case |
| 500 | `internal` | `"Error interno del servidor"` | `null` | Error no manejado — ver logs con `X-Correlation-Id` |

Formato de error:
```json
{ "type": "string", "message": "string", "field": "string | null" }
```

Todos los mensajes de error están en **español** (ADR-008).

---

## Schema del response

### `QuoteStateDto`

| Campo | Tipo | Nullable | Descripción |
|---|---|---|---|
| `folioNumber` | string | No | Número de folio, formato `DAN-YYYY-NNNNN` |
| `quoteStatus` | string | No | `"draft"` / `"in_progress"` / `"calculated"` |
| `version` | int | No | Versión del documento (locking optimista) |
| `progress` | `ProgressDto` | No | Progreso por sección del wizard |
| `locations` | `LocationsStateDto` | No | Resumen de ubicaciones |
| `readyForCalculation` | bool | No | `true` si `locations.calculable >= 1` |
| `calculationResult` | `CalculationResultDto` | Sí | Solo presente cuando `quoteStatus == "calculated"` |

### `ProgressDto`

| Campo | Tipo | Regla de derivación | Fuente en dominio |
|---|---|---|---|
| `generalInfo` | bool | `!string.IsNullOrWhiteSpace(InsuredData.Name)` | `PropertyQuote.InsuredData.Name` |
| `layoutConfiguration` | bool | Siempre `true` (defaults existen desde creación) | Invariante del folio (SPEC-005) |
| `locations` | bool | `Locations.Count > 0` | `PropertyQuote.Locations` |
| `coverageOptions` | bool | `CoverageOptions.EnabledGuarantees.Count > 0` | `PropertyQuote.CoverageOptions.EnabledGuarantees` |

> El progreso se deriva de los **datos persistidos**, no del `lastWizardStep`. Esto lo hace agnóstico al wizard y consistente con API directa y agentes automatizados (RN-008-01).

### `LocationsStateDto`

| Campo | Tipo | Descripción |
|---|---|---|
| `total` | int | Total de ubicaciones en el folio |
| `calculable` | int | Ubicaciones con `validationStatus == "calculable"` |
| `incomplete` | int | `total - calculable` |
| `alerts` | `LocationAlertDto[]` | Alertas por ubicación incompleta — arreglo vacío si todas son calculables |

### `LocationAlertDto`

| Campo | Tipo | Descripción |
|---|---|---|
| `index` | int | Posición 1-based de la ubicación |
| `locationName` | string | Nombre legible de la ubicación |
| `missingFields` | string[] | Campos faltantes en dot-notation (e.g., `"businessLine.fireKey"`) |

> Las alertas son **informativas** — nunca bloquean la navegación del wizard (RN-008-08).

### `CalculationResultDto`

| Campo | Tipo | Descripción |
|---|---|---|
| `netPremium` | decimal | Prima neta total (suma de ubicaciones calculables) |
| `commercialPremiumBeforeTax` | decimal | **Siempre `0` hasta SPEC-009** — ver nota arriba |
| `commercialPremium` | decimal | Prima comercial total (incluye factores de gastos, comisión, financiamiento) |
| `premiumsByLocation` | `LocationPremiumDto[]` | Desglose por ubicación |

### `LocationPremiumDto`

| Campo | Tipo | Descripción |
|---|---|---|
| `locationIndex` | int | Posición 1-based |
| `locationName` | string | Nombre de la ubicación |
| `netPremium` | decimal | Prima neta de la ubicación |
| `validationStatus` | string | `"calculable"` / `"incomplete"` |
| `coveragePremiums` | `CoveragePremiumDto[]` | Desglose por garantía |

### `CoveragePremiumDto`

| Campo | Tipo | Descripción |
|---|---|---|
| `guaranteeKey` | string | Llave de la garantía (e.g., `"building_fire"`) |
| `insuredAmount` | decimal | Suma asegurada |
| `rate` | decimal | Tasa aplicada |
| `premium` | decimal | Prima resultante (`insuredAmount × rate`) |

---

## Reglas de negocio aplicadas

| ID | Regla |
|---|---|
| RN-008-01 | Progreso derivado de datos persistidos — no de `lastWizardStep` |
| RN-008-02 | `generalInfo = true` si `InsuredData.Name` no está vacío |
| RN-008-03 | `layoutConfiguration` siempre `true` — defaults existen desde creación del folio |
| RN-008-04 | `locations = true` si `Locations.Count > 0` |
| RN-008-05 | `coverageOptions = true` si `EnabledGuarantees.Count > 0` |
| RN-008-06 | `readyForCalculation = true` si hay ≥1 ubicación con `validationStatus == "calculable"` |
| RN-008-07 | `quoteStatus`: `draft` → `in_progress` al guardar primera sección; → `calculated` post-cálculo |
| RN-008-08 | Alertas informativas — nunca bloquean navegación del wizard |
| RN-008-09 | `calculationResult` es `null` a menos que `quoteStatus == "calculated"` |
| RN-008-10 | Toda respuesta 2xx usa envelope `{ "data": {...} }` |
| RN-008-11 | Mensajes de error siempre en español |

---

## Integración frontend

| Aspecto | Detalle |
|---|---|
| Archivo cliente | `cotizador-webapp/src/entities/quote-state/api/quoteStateApi.ts` |
| Hook | `useQuoteStateQuery(folio)` — TanStack Query |
| Query key | `['quote-state', folio]` |
| `staleTime` | `0` — dato siempre fresco (el folio es mutable) |
| Invalidación | Al mutar cualquier sección: `PUT general-info`, `PUT locations`, `PUT coverage-options`, `POST calculate` |
| Errores manejados por FE | 404 → notificación "El folio no existe"; 500 → notificación genérica |
| Componentes consumidores | `WizardLayout.tsx` → `ProgressBar`, banner de calculabilidad, `LocationAlerts` |

### Widgets renderizados desde el estado

| Widget | Props clave | Comportamiento |
|---|---|---|
| `ProgressBar` | `progress: ProgressDto` | 4 secciones con checkmarks; cada sección es navegable |
| `LocationAlerts` | `alerts`, `folio`, `calculable`, `total` | Panel de alertas con link "Ir a editar" → `/quotes/{folio}/locations?edit={index}` |
| Banner de calculabilidad | `readyForCalculation`, conteos | Mensaje contextual: "N ubicación(es) lista(s) para calcular" o "Complete los datos..." |

---

## Acceso a datos

| Operación | Colección | Tipo | Filtro | Proyección | Índice |
|---|---|---|---|---|---|
| Lectura | `property_quotes` | `FindOne` | `{ folioNumber }` | Documento completo | `folioNumber_1` (existente) |

- Solo lectura — este endpoint no escribe en MongoDB.
- El estado se calcula de forma dinámica a partir del `PropertyQuote` completo; no se persiste ningún campo derivado.
