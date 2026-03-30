# Escenarios Gherkin — Estado y Progreso de la Cotización (SPEC-008)

> **Feature:** `quote-state-progress`
> **Endpoint:** `GET /v1/quotes/{folio}/state`
> **Spec origen:** `.github/specs/quote-state-progress.spec.md`
> **HU cubiertas:** HU-008-01 a HU-008-05
> **Reglas de negocio cubiertas:** RN-008-01 a RN-008-11
> **Generado:** 2026-03-30 | **Agente:** qa-agent

---

## Datos de prueba

| Fixture ID | Folio | Estado | Descripción |
|------------|-------|--------|-------------|
| FX-001 | `DAN-2026-00001` | `draft` | Folio recién creado, sin datos. Sólo defaults. |
| FX-002 | `DAN-2026-00002` | `in_progress` | Datos generales completos + 2 ubicaciones (1 calculable, 1 incompleta) |
| FX-003 | `DAN-2026-00003` | `calculated` | Folio calculado con resultado financiero completo |
| FX-004 | `DAN-2026-00004` | `in_progress` | Solo datos generales, sin ubicaciones ni garantías |
| FX-005 | `DAN-2026-00005` | `in_progress` | 3 ubicaciones todas "calculable", garantías habilitadas |
| FX-006 | `DAN-2026-00006` | `in_progress` | 2 ubicaciones ambas "incomplete", sin calculables |
| FX-007 | `DAN-2026-00007` | `in_progress` | 3 ubicaciones: 2 calculables + 1 incompleta |
| FX-ERR | `DAN-2026-99999` | — | No existe en base de datos |

---

```gherkin
#language: es
Característica: Estado y Progreso de la Cotización
  Como usuario del cotizador
  Quiero consultar el estado completo de mi folio
  Para conocer el progreso por sección, la calculabilidad de ubicaciones
  y el resultado financiero (cuando el folio ya fue calculado)

  Antecedentes:
    Dado que el sistema tiene los fixtures de prueba cargados
    Y el endpoint base es "http://localhost:5000/v1/quotes"

  # =========================================================
  # HU-008-01 — Estado global del folio (@smoke @critico)
  # =========================================================

  @smoke @critico @HU-008-01
  Escenario: Folio draft recién creado retorna estado inicial con defaults correctos
    Dado que existe el folio "DAN-2026-00001" en estado "draft" sin datos guardados
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state" con header "Authorization: Basic dXNlcjpwYXNz"
    Entonces la respuesta tiene código de estado 200
    Y la respuesta contiene el envelope "data"
    Y "data.folioNumber" es "DAN-2026-00001"
    Y "data.quoteStatus" es "draft"
    Y "data.version" es 1
    Y "data.progress.generalInfo" es false
    Y "data.progress.layoutConfiguration" es true
    Y "data.progress.locations" es false
    Y "data.progress.coverageOptions" es false
    Y "data.readyForCalculation" es false
    Y "data.calculationResult" es null
    Y "data.locations.total" es 0
    Y "data.locations.calculable" es 0
    Y "data.locations.incomplete" es 0
    Y "data.locations.alerts" es una lista vacía

  @smoke @critico @HU-008-01 @HU-008-03 @HU-008-04
  Escenario: Folio in_progress con datos generales y ubicaciones mixtas retorna estado enriquecido
    Dado que existe el folio "DAN-2026-00002" en estado "in_progress"
    Y el folio tiene InsuredData.Name "Aseguradora Nacional S.A. de C.V."
    Y el folio tiene 2 ubicaciones: "Bodega Central CDMX" (calculable) y "Local sin CP" (incompleta con missingFields ["zipCode", "businessLine.fireKey"])
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00002/state" con header "Authorization: Basic dXNlcjpwYXNz"
    Entonces la respuesta tiene código de estado 200
    Y "data.quoteStatus" es "in_progress"
    Y "data.progress.generalInfo" es true
    Y "data.progress.layoutConfiguration" es true
    Y "data.progress.locations" es true
    Y "data.readyForCalculation" es true
    Y "data.locations.total" es 2
    Y "data.locations.calculable" es 1
    Y "data.locations.incomplete" es 1
    Y "data.locations.alerts" tiene 1 elemento
    Y "data.locations.alerts[0].locationName" es "Local sin CP"
    Y "data.locations.alerts[0].missingFields" contiene "zipCode"
    Y "data.locations.alerts[0].missingFields" contiene "businessLine.fireKey"
    Y "data.calculationResult" es null

  @smoke @critico @HU-008-05
  Escenario: Folio calculado retorna calculationResult con datos financieros completos
    Dado que existe el folio "DAN-2026-00003" en estado "calculated"
    Y el folio tiene netPremium 125000.50, commercialPremium 174000.70
    Y el folio tiene 1 ubicación calculable "Bodega Central CDMX" con netPremium 85000.30
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00003/state" con header "Authorization: Basic dXNlcjpwYXNz"
    Entonces la respuesta tiene código de estado 200
    Y "data.quoteStatus" es "calculated"
    Y "data.calculationResult" no es null
    Y "data.calculationResult.netPremium" es 125000.50
    Y "data.calculationResult.commercialPremium" es 174000.70
    Y "data.calculationResult.premiumsByLocation" tiene al menos 1 elemento
    Y "data.calculationResult.premiumsByLocation[0].locationName" es "Bodega Central CDMX"
    Y "data.calculationResult.premiumsByLocation[0].netPremium" es 85000.30
    Y "data.calculationResult.premiumsByLocation[0].validationStatus" es "calculable"

  # =========================================================
  # HU-008-02 — Indicador de progreso por sección
  # =========================================================

  @smoke @critico @HU-008-02
  Escenario: Solo sección de datos generales completa muestra un checkmark en esa sección
    Dado que existe el folio "DAN-2026-00004" en estado "in_progress"
    Y el folio tiene InsuredData.Name "Seguros del Centro S.A."
    Y el folio no tiene ubicaciones
    Y el folio no tiene EnabledGuarantees
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00004/state"
    Entonces "data.progress.generalInfo" es true
    Y "data.progress.layoutConfiguration" es true
    Y "data.progress.locations" es false
    Y "data.progress.coverageOptions" es false

  @smoke @critico @HU-008-02 @RN-008-05
  Escenario: Folio con opciones de cobertura habilitadas muestra coverageOptions en true
    Dado que existe el folio "DAN-2026-00005" con EnabledGuarantees ["building_fire", "cat_tev", "electronic_equipment"]
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00005/state"
    Entonces "data.progress.coverageOptions" es true

  # =========================================================
  # HU-008-03 — Alertas informativas (sin bloqueo)
  # =========================================================

  @smoke @critico @HU-008-03 @RN-008-08
  Escenario: Ubicación incompleta genera alerta informativa sin bloquear navegación
    Dado que el folio "DAN-2026-00002" tiene 1 ubicación incompleta "Local sin CP"
    Y la ubicación incompleta tiene missingFields ["zipCode", "businessLine.fireKey"]
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00002/state"
    Entonces "data.locations.alerts[0].locationName" es "Local sin CP"
    Y "data.locations.alerts[0].missingFields[0]" es "zipCode"
    Y "data.locations.alerts[0].missingFields[1]" es "businessLine.fireKey"
    Y la respuesta tiene código de estado 200 (no 4xx — la alerta no bloquea)

  @edge-case @HU-008-03
  Escenario: Todas las ubicaciones calculables eliminan las alertas por completo
    Dado que el folio "DAN-2026-00005" tiene 3 ubicaciones todas con validationStatus "calculable"
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00005/state"
    Entonces "data.locations.alerts" es una lista vacía
    Y "data.locations.incomplete" es 0

  # =========================================================
  # HU-008-04 — Conteo de ubicaciones calculables
  # =========================================================

  @smoke @critico @HU-008-04 @RN-008-06
  Escenario: Folio con 3 ubicaciones mixtas muestra conteo correcto y readyForCalculation true
    Dado que existe el folio "DAN-2026-00007" con 3 ubicaciones
    Y 2 de ellas tienen validationStatus "calculable"
    Y 1 de ellas tiene validationStatus "incomplete" con missingFields ["businessLine.fireKey"]
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00007/state"
    Entonces "data.locations.total" es 3
    Y "data.locations.calculable" es 2
    Y "data.locations.incomplete" es 1
    Y "data.readyForCalculation" es true

  # =========================================================
  # HU-008-05 — calculationResult null cuando no hay cálculo
  # =========================================================

  @smoke @critico @HU-008-05 @RN-008-09
  Escenario: Folio in_progress retorna calculationResult null
    Dado que el folio "DAN-2026-00002" tiene quoteStatus "in_progress"
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00002/state"
    Entonces "data.calculationResult" es null

  @smoke @critico @HU-008-05 @RN-008-09
  Escenario: Folio draft retorna calculationResult null
    Dado que el folio "DAN-2026-00001" tiene quoteStatus "draft"
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces "data.calculationResult" es null

  # =========================================================
  # Rutas de error (@error-path)
  # =========================================================

  @error-path @seguridad @HU-008-01
  Escenario: Request sin autenticación devuelve 401 con mensaje en español
    Dado que NO tengo header "Authorization" en mi request
    Y el folio "DAN-2026-00001" existe en base de datos
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state" sin cabecera de autenticación
    Entonces la respuesta tiene código de estado 401
    Y "type" es "unauthorized"
    Y "message" es "Credenciales inválidas o ausentes" (en español, RN-008-11)
    Y "field" es null

  @error-path @seguridad @HU-008-01
  Escenario: Request con credenciales inválidas devuelve 401
    Dado que tengo credenciales inválidas "dXNlcjpXUk9ORw=="
    Y el folio "DAN-2026-00001" existe en base de datos
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state" con header "Authorization: Basic dXNlcjpXUk9ORw=="
    Entonces la respuesta tiene código de estado 401
    Y "type" es "unauthorized"

  @error-path @HU-008-01 @RN-008-11
  Escenario: Folio con formato inválido devuelve 400 con mensaje en español
    Dado que tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/INVALIDO-001/state"
    Entonces la respuesta tiene código de estado 400
    Y "type" es "validationError"
    Y "message" es "Formato de folio inválido. Use DAN-YYYY-NNNNN"
    Y "field" es "folio"

  @error-path @HU-008-01 @RN-008-11
  Escenario: Folio con año fuera de rango no cumple patrón DAN-YYYY-NNNNN devuelve 400
    Dado que tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-26-001/state"
    Entonces la respuesta tiene código de estado 400
    Y "type" es "validationError"
    Y "message" es "Formato de folio inválido. Use DAN-YYYY-NNNNN"

  @error-path @HU-008-01
  Escenario: Folio bien formado pero inexistente devuelve 404
    Dado que tengo credenciales válidas "dXNlcjpwYXNz"
    Y el folio "DAN-2026-99999" no existe en base de datos
    Cuando envío GET "/v1/quotes/DAN-2026-99999/state"
    Entonces la respuesta tiene código de estado 404
    Y "type" es "folioNotFound"
    Y "message" es "El folio DAN-2026-99999 no existe"
    Y "field" es null

  @error-path
  Escenario: Error interno del servidor devuelve 500 con mensaje genérico en español
    Dado que el repositorio MongoDB no está disponible (simulación de fallo)
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Y el folio "DAN-2026-00001" existe (en condiciones normales)
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces la respuesta tiene código de estado 500
    Y "type" es "internal"
    Y "message" es "Error interno del servidor"

  # =========================================================
  # Casos borde (@edge-case)
  # =========================================================

  @edge-case @HU-008-04 @RN-008-06
  Escenario: Folio sin ubicaciones retorna valores en cero y readyForCalculation false
    Dado que el folio "DAN-2026-00004" no tiene ubicaciones registradas
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00004/state"
    Entonces "data.locations.total" es 0
    Y "data.locations.calculable" es 0
    Y "data.locations.incomplete" es 0
    Y "data.locations.alerts" es una lista vacía
    Y "data.readyForCalculation" es false
    Y "data.progress.locations" es false

  @edge-case @HU-008-04 @RN-008-06
  Escenario: Todas las ubicaciones son calculables — readyForCalculation true sin alertas
    Dado que el folio "DAN-2026-00005" tiene 3 ubicaciones todas con validationStatus "calculable"
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00005/state"
    Entonces "data.locations.total" es 3
    Y "data.locations.calculable" es 3
    Y "data.locations.incomplete" es 0
    Y "data.locations.alerts" es una lista vacía
    Y "data.readyForCalculation" es true

  @edge-case @HU-008-04 @RN-008-06
  Escenario: Ninguna ubicación es calculable — readyForCalculation false con alertas para todas
    Dado que el folio "DAN-2026-00006" tiene 2 ubicaciones ambas con validationStatus "incomplete"
    Y la ubicación 1 tiene missingFields ["zipCode"]
    Y la ubicación 2 tiene missingFields ["businessLine.fireKey", "riskClassification"]
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00006/state"
    Entonces "data.locations.calculable" es 0
    Y "data.locations.incomplete" es 2
    Y "data.locations.alerts" tiene 2 elementos
    Y "data.readyForCalculation" es false

  @edge-case @HU-008-02 @RN-008-05
  Escenario: Folio con EnabledGuarantees vacío muestra coverageOptions en false
    Dado que el folio "DAN-2026-00004" tiene CoverageOptions.EnabledGuarantees vacío
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00004/state"
    Entonces "data.progress.coverageOptions" es false

  # =========================================================
  # Reglas de negocio — validaciones específicas
  # =========================================================

  @edge-case @RN-008-02
  Escenario: InsuredData.Name vacío mantiene generalInfo en false
    Dado que el folio "DAN-2026-00001" tiene InsuredData.Name "" (cadena vacía)
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces "data.progress.generalInfo" es false

  @edge-case @RN-008-02
  Escenario: InsuredData.Name solo con espacios mantiene generalInfo en false
    Dado que el folio "DAN-2026-00001" tiene InsuredData.Name "   " (solo espacios)
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces "data.progress.generalInfo" es false

  @smoke @critico @RN-008-03
  Escenario: layoutConfiguration es siempre true desde el momento de creación del folio
    Dado que el folio "DAN-2026-00001" existe en estado "draft" sin dato alguno
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces "data.progress.layoutConfiguration" es true
    Y la respuesta tiene código de estado 200

  @smoke @critico @RN-008-10
  Escenario: Toda respuesta 200 usa envelope data obligatorio
    Dado que el folio "DAN-2026-00001" existe
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces la respuesta tiene código de estado 200
    Y el cuerpo de la respuesta tiene el campo raíz "data"
    Y "data.folioNumber" existe y es una cadena no vacía

  @smoke @critico @RN-008-11
  Escenario: Los mensajes de error de validación están en español
    Dado que tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/MAL-FORMADO/state"
    Entonces la respuesta tiene código de estado 400
    Y el campo "message" contiene texto en español
    Y el campo "message" NO contiene texto en inglés (como "Invalid", "Error", "Bad Request")

  @edge-case @RN-008-07
  Escenario: Transición draft a in_progress es reflejada en el estado tras guardar datos generales
    Dado que el folio "DAN-2026-00001" tiene quoteStatus "draft"
    Y tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando el folio recibe un PUT de datos generales con InsuredData.Name "Nueva Aseguradora S.A."
    Y envío GET "/v1/quotes/DAN-2026-00001/state"
    Entonces "data.quoteStatus" es "in_progress"
    Y "data.progress.generalInfo" es true

  # =========================================================
  # Esquemas de escenario — variación de formato de folio
  # =========================================================

  @error-path
  Esquema del escenario: Folio con formato inválido siempre devuelve 400
    Dado que tengo credenciales válidas "dXNlcjpwYXNz"
    Cuando envío GET "/v1/quotes/<folio_invalido>/state"
    Entonces la respuesta tiene código de estado 400
    Y "type" es "validationError"
    Y "message" contiene "Formato de folio inválido"
    Ejemplos:
      | folio_invalido   | descripcion                  |
      | INVALIDO-001     | Prefijo incorrecto           |
      | DAN-26-001       | Año de 2 dígitos             |
      | DAN-2026-ABCDE   | Secuencia no numérica        |
      | DAN2026-00001    | Sin guion entre DAN y año    |
      | DAN-2026-0000    | Solo 4 dígitos en secuencia  |
      |                  | Folio vacío                  |
```

---

## Cobertura de HU y Reglas de Negocio

| HU / Regla | Escenarios que la cubren |
|------------|--------------------------|
| HU-008-01 | Folio draft, folio in_progress mixto, 401, 404, 500, 400 |
| HU-008-02 | Solo datos generales, folio con garantías, coverageOptions vacío |
| HU-008-03 | Alerta informativa, todas calculables sin alertas |
| HU-008-04 | 3 mixtas, 0 ubicaciones, todas calculables, ninguna calculable |
| HU-008-05 | Folio calculado con result, folio draft null, folio in_progress null |
| RN-008-01 | Progreso derivado de dato persistido (todos los escenarios) |
| RN-008-02 | Name vacío → false, Name solo espacios → false |
| RN-008-03 | layoutConfiguration siempre true desde draft |
| RN-008-04 | 0 ubicaciones → false, 1+ ubicaciones → true |
| RN-008-05 | EnabledGuarantees > 0 → true, vacío → false |
| RN-008-06 | readyForCalculation = calculable > 0 |
| RN-008-07 | Transición draft → in_progress |
| RN-008-08 | Alertas no bloquean (respuesta 200 con alerta) |
| RN-008-09 | calculationResult null cuando no calculado |
| RN-008-10 | Envelope { "data": {...} } en todos los 200 |
| RN-008-11 | Mensajes de error en español (400, 401, 404, 500) |

---

## Datos sintéticos — Fixtures detallados

```json
// FX-001 — draft
{
  "folioNumber": "DAN-2026-00001",
  "quoteStatus": "draft",
  "version": 1,
  "insuredData": { "name": "" },
  "locations": [],
  "coverageOptions": { "enabledGuarantees": [] }
}

// FX-002 — in_progress con mixtas
{
  "folioNumber": "DAN-2026-00002",
  "quoteStatus": "in_progress",
  "version": 5,
  "insuredData": { "name": "Aseguradora Nacional S.A. de C.V." },
  "locations": [
    {
      "index": 1,
      "locationName": "Bodega Central CDMX",
      "validationStatus": "calculable",
      "blockingAlerts": []
    },
    {
      "index": 2,
      "locationName": "Local sin CP",
      "validationStatus": "incomplete",
      "blockingAlerts": ["zipCode", "businessLine.fireKey"]
    }
  ],
  "coverageOptions": { "enabledGuarantees": [] }
}

// FX-003 — calculated
{
  "folioNumber": "DAN-2026-00003",
  "quoteStatus": "calculated",
  "version": 8,
  "netPremium": 125000.50,
  "commercialPremiumBeforeTax": 0,
  "commercialPremium": 174000.70,
  "insuredData": { "name": "Grupo Industrias S.A. de C.V." },
  "locations": [
    {
      "index": 1,
      "locationName": "Bodega Central CDMX",
      "validationStatus": "calculable",
      "blockingAlerts": [],
      "netPremium": 85000.30,
      "coveragePremiums": [
        { "guaranteeKey": "building_fire", "insuredAmount": 5000000, "rate": 0.00125, "premium": 6250.00 }
      ]
    }
  ],
  "coverageOptions": { "enabledGuarantees": ["building_fire"] }
}
```

> **Nota sobre FX-003:** `commercialPremiumBeforeTax` retorna `0` por el placeholder activo (MAY-002). Este valor es engañoso aunque documentado. Ver Matriz de Riesgos R-003.
