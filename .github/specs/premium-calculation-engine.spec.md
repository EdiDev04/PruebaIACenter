---
id: SPEC-009
status: IMPLEMENTED
feature: premium-calculation-engine
feature_type: full-stack
requires_design_spec: false
has_calculation_logic: true
affects_database: true
consumes_core_ohs: true
has_fe_be_integration: true
created: 2026-03-29
updated: 2026-03-30
author: spec-generator
version: "1.1"
related-specs: ["SPEC-001", "SPEC-002", "SPEC-003", "SPEC-006", "SPEC-007", "SPEC-008"]
priority: critica
estimated-complexity: XL
---

# Spec: Motor de Cálculo de Primas

> **Estado:** `IN_PROGRESS` — v1.1: cruce de `enabledGuarantees` con garantías de ubicación al calcular (ref: SPEC-007 RN-007-06, SUP-007-05).
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → **AMENDMENT** → APPROVED → IN_PROGRESS → IMPLEMENTED

---

## 1. RESUMEN EJECUTIVO

Implementar el motor de cálculo que procesa un folio completo: lee las ubicaciones calculables, consulta tarifas técnicas de core-ohs, aplica fórmulas simplificadas por cobertura, consolida la prima neta total, y deriva la prima comercial (con y sin IVA) usando los parámetros globales. El resultado financiero se persiste atómicamente sin sobrescribir otras secciones del folio. El status pasa a `calculated` y la versión se incrementa. Este es el feature de mayor valor del cotizador — convierte datos de entrada en un resultado financiero trazable.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-009-01**: Como usuario del cotizador, quiero ejecutar el cálculo de mi cotización para obtener la prima neta y prima comercial del folio.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio `DAN-2026-00001` con 2 ubicaciones calculables y 1 incompleta
  **Cuando** envío `POST /v1/quotes/DAN-2026-00001/calculate` con `{ "version": 5 }`
  **Entonces** el sistema calcula prima neta para las 2 ubicaciones calculables
  **Y** genera alerta para la incompleta sin bloquear
  **Y** persiste `netPremium`, `commercialPremiumBeforeTax`, `commercialPremium`, `premiumsByLocation`
  **Y** `quoteStatus` cambia a `"calculated"`, `version` incrementa a 6
  **Y** la response contiene el resultado financiero completo

- **Dado** que el folio no tiene ubicaciones calculables (0 calculables)
  **Cuando** envío `POST .../calculate`
  **Entonces** retorna HTTP 422 con `{ "type": "invalidQuoteState", "message": "No hay ubicaciones calculables para ejecutar el cálculo" }`

---

**HU-009-02**: Como usuario del cotizador, quiero ver el desglose de prima por ubicación para entender la contribución de cada propiedad.

**Criterios de aceptación (Gherkin):**

- **Dado** que el cálculo fue exitoso con 2 ubicaciones calculables
  **Cuando** veo el resultado
  **Entonces** cada ubicación tiene su `netPremium`, `validationStatus: "calculable"`, y `coveragePremiums[]` con prima por cada garantía

- **Dado** que una ubicación es incompleta
  **Cuando** veo el resultado
  **Entonces** esa ubicación aparece con `netPremium: 0`, `validationStatus: "incomplete"`, y `coveragePremiums: []`

---

**HU-009-03**: Como usuario del cotizador, quiero que las ubicaciones incompletas generen alertas pero no impidan calcular las demás.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo 3 ubicaciones: 2 calculables y 1 incompleta
  **Cuando** ejecuto el cálculo
  **Entonces** la prima neta total = suma de primas de las 2 calculables
  **Y** la ubicación incompleta tiene `netPremium: 0` y sus `blockingAlerts`

---

**HU-009-04**: Como sistema, quiero persistir el resultado financiero atómicamente sin sobrescribir otras secciones.

**Criterios de aceptación (Gherkin):**

- **Dado** que ejecuto el cálculo exitosamente
  **Cuando** consulto la cotización
  **Entonces** `netPremium`, `commercialPremiumBeforeTax`, `commercialPremium`, `premiumsByLocation` están persistidos
  **Y** `insuredData`, `locations`, `coverageOptions` no fueron modificados
  **Y** `version` incrementó y `metadata.updatedAt` se actualizó

- **Dado** que envío `POST .../calculate` con `version: 3` pero la versión actual es 5
  **Cuando** el backend procesa
  **Entonces** retorna HTTP 409 con `{ "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar" }`

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-009-01 | La cotización se identifica por `folioNumber` | — | — | bussines-context.md §8 |
| RN-009-02 | Una ubicación NO se calcula si no tiene CP válido, `businessLine.fireKey`, o garantías tarifables | `validationStatus == "incomplete"` | `netPremium: 0` para esa ubicación | bussines-context.md §10 |
| RN-009-02b | **[AMENDMENT v1.1]** Una ubicación NO se calcula si alguna de sus garantías está fuera de `CoverageOptions.EnabledGuarantees` | Garantía local ∉ enabledGuarantees | `netPremium: 0`, `validationStatus: "incomplete"` para esa ubicación — aunque en BD su status sea `calculable` | SPEC-007 RN-007-06, SUP-007-05 |
| RN-009-03 | Ubicaciones incompletas generan alertas pero no bloquean el cálculo | ≥1 ubicación calculable | Cálculo procede con las calculables | bussines-context.md §1, §10 |
| RN-009-04 | Si 0 ubicaciones son calculables → error, no se persiste nada | `calculable == 0` | `InvalidQuoteStateException` → HTTP 422 | REQ-09 |
| RN-009-05 | `netPremium` = Σ primas netas por ubicación calculable | Cada ubicación: Σ primas por cobertura | Decimal, 2 decimales | bussines-context.md §5, §8 |
| RN-009-06 | `commercialPremiumBeforeTax` = `netPremium × (1 + expeditionExpenses + agentCommission + issuingRights + surcharges)` | Parámetros de `calculationParameters` | Decimal, 2 decimales | SUP-009-03 |
| RN-009-07 | `commercialPremium` = `commercialPremiumBeforeTax × (1 + iva)` | IVA de `calculationParameters` | Decimal, 2 decimales | SUP-009-03 |
| RN-009-08 | La prima comercial se calcula a nivel de folio, NO por ubicación | — | Un solo valor global, no N por ubicación | bussines-context.md §8, §10 |
| RN-009-09 | Resultado financiero persistido atómicamente sin sobrescribir otras secciones | `$set` parcial en MongoDB | `UpdateFinancialResultAsync` | ADR-002 |
| RN-009-10 | Al persistir: `quoteStatus = "calculated"`, `version += 1`, `metadata.updatedAt` actualizado, `metadata.lastWizardStep = 4` | — | — | ADR-002, ADR-007 |
| RN-009-11 | Versionado optimista | `version` en body vs `version` persistida | 409 si difieren | architecture-decisions.md |
| RN-009-12 | Fórmulas simplificadas documentadas | S-04 del reto | Montos no realistas vs producción, aceptable | S-04 |
| RN-009-13 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | — | architecture-decisions.md |
| RN-009-14 | Mensajes de error en español | Toda respuesta de error | — | ADR-008 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea cálculo |
|---|---|---|---|
| `folio` (path) | Formato `DAN-YYYY-NNNNN` | "Formato de folio inválido. Use DAN-YYYY-NNNNN" | Sí (400) |
| `version` (body) | Requerido, entero > 0 | "La versión es obligatoria" | Sí (400) |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: false    ← la UI de resultados vive en SPEC-010; este feature es motor + endpoint

Flujo de ejecución:
  Fase 0.5 (ux-designer):    NO APLICA — sin UI propia (la visualización es SPEC-010)
  Fase 1.5 (core-ohs):       NO APLICA — endpoints de tarifas ya implementados (SPEC-001)
  Fase 1.5 (business-rules): APLICA — PremiumCalculator (Domain) con fórmulas documentadas
  Fase 1.5 (database-agent): NO APLICA — UpdateFinancialResultAsync ya existe (SPEC-002)
  Fase 2 backend-developer:  APLICA — Use Case orquestador + Domain service + DTO + Controller
  Fase 2 frontend-developer: APLICA (mínimo) — botón "Calcular" + mutation. UI completa en SPEC-010

Bloqueos de ejecución:
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
  - frontend-developer puede iniciar tras spec.status == APPROVED (no requiere design spec)
```

### 3.2 Design Spec

N/A — `requires_design_spec: false`. La UI de resultados se define en SPEC-010.

### 3.3 Modelo de dominio

**Modificar `PropertyQuote`** — agregar campo para prima antes de IVA:

```csharp
// Cotizador.Domain/Entities/PropertyQuote.cs — MODIFICAR
public decimal CommercialPremiumBeforeTax { get; set; }  // NUEVO — prima con gastos, sin IVA
```

**Nuevo Domain Service — PremiumCalculator (funciones puras, sin I/O):**

```csharp
// Cotizador.Domain/Services/PremiumCalculator.cs — CREAR
public static class PremiumCalculator
{
    /// <summary>Calcula la prima de una cobertura individual.</summary>
    public static CoveragePremium CalculateCoveragePremium(
        string guaranteeKey,
        decimal insuredAmount,
        decimal rate);

    /// <summary>Calcula la prima neta de una ubicación = Σ primas de coberturas.</summary>
    public static decimal CalculateLocationNetPremium(
        List<CoveragePremium> coveragePremiums);

    /// <summary>
    /// Deriva la prima comercial a partir de la prima neta y los parámetros globales.
    /// commercialPremiumBeforeTax = netPremium × (1 + expenses + commission + rights + surcharges)
    /// commercialPremium = commercialPremiumBeforeTax × (1 + iva)
    /// </summary>
    public static (decimal BeforeTax, decimal WithTax) CalculateCommercialPremium(
        decimal netPremium,
        decimal expeditionExpenses,
        decimal agentCommission,
        decimal issuingRights,
        decimal surcharges,
        decimal iva);
}
```

**Nuevas constantes de tarifas simplificadas:**

```csharp
// Cotizador.Domain/Constants/SimplifiedTariffRates.cs — CREAR
public static class SimplifiedTariffRates
{
    /// <summary>Tasa para debris_removal, extraordinary_expenses.</summary>
    public const decimal SupplementaryRate = 0.0010m;

    /// <summary>Tasa para rent_loss, business_interruption.</summary>
    public const decimal IncomeRate = 0.0015m;

    /// <summary>Tasa para theft, cash_and_securities.</summary>
    public const decimal SpecialRate = 0.0020m;

    /// <summary>Prima fija para glass, illuminated_signs.</summary>
    public const decimal FlatPremium = 500.00m;

    /// <summary>Clase de equipo electrónico por defecto.</summary>
    public const string DefaultEquipmentClass = "A";
}
```

**Existentes (sin cambios):**
- `LocationPremium` — ya tiene `LocationIndex`, `LocationName`, `NetPremium`, `ValidationStatus`, `CoveragePremiums`
- `CoveragePremium` — ya tiene `GuaranteeKey`, `InsuredAmount`, `Rate`, `Premium`
- `IQuoteRepository.UpdateFinancialResultAsync()` — ya existe; **necesita** agregar param `commercialPremiumBeforeTax`

**Modificar `IQuoteRepository.UpdateFinancialResultAsync`** — agregar parámetro:

```csharp
// Cotizador.Application/Ports/IQuoteRepository.cs — MODIFICAR firma
Task UpdateFinancialResultAsync(
    string folioNumber,
    int expectedVersion,
    decimal netPremium,
    decimal commercialPremiumBeforeTax,  // NUEVO
    decimal commercialPremium,
    List<LocationPremium> premiumsByLocation,
    CancellationToken ct = default);
```

**Nuevos DTOs:**

```csharp
// Cotizador.Application/DTOs/CalculateRequest.cs
public record CalculateRequest(int Version);

// Cotizador.Application/DTOs/CalculateResultResponse.cs
public record CalculateResultResponse(
    decimal NetPremium,
    decimal CommercialPremiumBeforeTax,
    decimal CommercialPremium,
    List<LocationPremiumDto> PremiumsByLocation,
    string QuoteStatus,
    int Version
);

// LocationPremiumDto y CoveragePremiumDto ya definidos en SPEC-008
```

### 3.4 Contratos API (backend)

```
POST /v1/quotes/{folio}/calculate
Propósito: Ejecutar el motor de cálculo de primas sobre el folio
Auth: Basic Auth ([Authorize])
Use Case: CalculateQuoteUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync(), IQuoteRepository.UpdateFinancialResultAsync()
Servicios externos: ICoreOhsClient.GetFireTariffsAsync(), ICoreOhsClient.GetCatTariffsAsync(), ICoreOhsClient.GetElectronicEquipmentFactorsAsync(), ICoreOhsClient.GetCalculationParametersAsync(), ICoreOhsClient.GetZipCodeAsync()

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    Content-Type: application/json
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001
  Body:
{
  "version": 5
}

Response 200 (cálculo exitoso):
{
  "data": {
    "netPremium": 125430.50,
    "commercialPremiumBeforeTax": 150516.60,
    "commercialPremium": 174599.26,
    "premiumsByLocation": [
      {
        "locationIndex": 1,
        "locationName": "Bodega Central CDMX",
        "netPremium": 85000.30,
        "validationStatus": "calculable",
        "coveragePremiums": [
          { "guaranteeKey": "building_fire", "insuredAmount": 5000000.00, "rate": 0.00125, "premium": 6250.00 },
          { "guaranteeKey": "contents_fire", "insuredAmount": 3000000.00, "rate": 0.00125, "premium": 3750.00 },
          { "guaranteeKey": "cat_tev", "insuredAmount": 5000000.00, "rate": 0.0035, "premium": 17500.00 },
          { "guaranteeKey": "glass", "insuredAmount": 0.00, "rate": 0.00, "premium": 500.00 },
          { "guaranteeKey": "theft", "insuredAmount": 2000000.00, "rate": 0.0020, "premium": 4000.00 }
        ]
      },
      {
        "locationIndex": 2,
        "locationName": "Sucursal Monterrey",
        "netPremium": 40430.20,
        "validationStatus": "calculable",
        "coveragePremiums": [
          { "guaranteeKey": "building_fire", "insuredAmount": 3000000.00, "rate": 0.00080, "premium": 2400.00 },
          { "guaranteeKey": "cat_tev", "insuredAmount": 3000000.00, "rate": 0.0015, "premium": 4500.00 }
        ]
      },
      {
        "locationIndex": 3,
        "locationName": "Local sin datos",
        "netPremium": 0,
        "validationStatus": "incomplete",
        "coveragePremiums": []
      }
    ],
    "quoteStatus": "calculated",
    "version": 6
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 400: { "type": "validationError", "message": "La versión es obligatoria", "field": "version" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 409: { "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }
Response 422: { "type": "invalidQuoteState", "message": "No hay ubicaciones calculables para ejecutar el cálculo", "field": null }
Response 503: { "type": "coreOhsUnavailable", "message": "Servicio de tarifas no disponible, intente más tarde", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Contratos core-ohs consumidos

```
GET /v1/tariffs/fire
Response 200: { "data": [{ "fireKey": "B-03", "baseRate": 0.00125, "description": "Bodega de almacenamiento" }, ...] }
Datos extraídos: baseRate por fireKey
Mapeado a: PremiumCalculator → tasa para building_fire, contents_fire, coverage_extension

GET /v1/tariffs/cat
Response 200: { "data": [{ "zone": "A", "tevFactor": 0.0035, "fhmFactor": 0.0028 }, ...] }
Datos extraídos: tevFactor por zone (para cat_tev), fhmFactor por zone (para cat_fhm — S-FHM)
Mapeado a: PremiumCalculator → tasa para cat_tev, cat_fhm

GET /v1/tariffs/electronic-equipment
Response 200: { "data": [{ "equipmentClass": "A", "zoneLevel": 2, "factor": 0.0052 }, ...] }
Datos extraídos: factor por equipmentClass + zoneLevel
Mapeado a: PremiumCalculator → tasa para electronic_equipment (default equipmentClass="A", zoneLevel=zipCode.technicalLevel)

GET /v1/tariffs/calculation-parameters
Response 200: { "data": { "expeditionExpenses": 0.05, "agentCommission": 0.10, "issuingRights": 0.03, "iva": 0.16, "surcharges": 0.02 } }
Datos extraídos: todos los 5 campos
Mapeado a: PremiumCalculator.CalculateCommercialPremium()

GET /v1/zip-codes/{zipCode} (ya consumido por SPEC-006, reutilizado aquí)
Datos extraídos: technicalLevel (para electronic_equipment lookup)
Manejo de error: si CP no resuelve, la ubicación ya es "incomplete" → omitida del cálculo
```

### 3.5b Contratos FE ↔ BE

```
POST /v1/quotes/{folio}/calculate
Consumido por:
  Archivo FE:    features/calculate-quote/model/useCalculateQuote.ts
  Hook/Query:    useMutation (TanStack Query)

Request FE → BE:
  { "version": 5 }

Response BE → FE (200):
  { "data": { "netPremium": ..., "commercialPremiumBeforeTax": ..., "commercialPremium": ..., "premiumsByLocation": [...], "quoteStatus": "calculated", "version": 6 } }

Errores manejados por el FE:
  - 409: alerta "El folio fue modificado, recarga para continuar"
  - 422: alerta "No hay ubicaciones calculables"
  - 503: alerta "Servicio de tarifas no disponible"
  - 500: notificación genérica

Invalidación de caché:
  - Invalida: ['quote-state', folio]
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── features/
│   └── calculate-quote/
│       ├── index.ts                           # CREAR — Public API
│       ├── model/
│       │   └── useCalculateQuote.ts           # CREAR — useMutation(POST .../calculate)
│       ├── ui/
│       │   ├── CalculateButton.tsx            # CREAR — Botón "Calcular cotización"
│       │   └── CalculateButton.module.css     # CREAR
│       └── strings.ts                         # CREAR
└── shared/
    └── api/
        └── endpoints.ts                       # MODIFICAR — agregar ruta calculate
```

| Componente | Props | Hooks | Acción |
|---|---|---|---|
| `CalculateButton` | `folio: string`, `version: number`, `disabled: boolean` | `useCalculateQuote` | Al hacer clic, ejecuta mutación. Disabled si `readyForCalculation == false` |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query (mutation) | — | `CalculateResultResponse` | Al mutar: invalida `['quote-state', folio]` |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read | `property_quotes` | `Find` | `{ folioNumber }` | Full document | `folioNumber_1` (existing) |
| Update | `property_quotes` | `UpdateOne` | `{ folioNumber, version: N }` | `$set: { netPremium, commercialPremiumBeforeTax, commercialPremium, premiumsByLocation, quoteStatus: "calculated", version: N+1, metadata.updatedAt, metadata.lastWizardStep: 4 }` | `folioNumber_1` (existing) |

- **Versionado optimista**: filtro `{ folioNumber, version }`. Si `ModifiedCount == 0` → `VersionConflictException`.
- **Operación atómica**: un solo `UpdateOne` con `$set` parcial — no toca `insuredData`, `locations`, etc.

---

## 4. LÓGICA DE CÁLCULO

### 4.1 Flujo del motor (CalculateQuoteUseCase)

```
1. Leer PropertyQuote completo por folioNumber
2. Si folio no existe → throw FolioNotFoundException
3. Validar version
4. Leer tarifas en paralelo desde core-ohs:
   - fireTariffs  = ICoreOhsClient.GetFireTariffsAsync()
   - catTariffs   = ICoreOhsClient.GetCatTariffsAsync()
   - equipFactors = ICoreOhsClient.GetElectronicEquipmentFactorsAsync()
   - calcParams   = ICoreOhsClient.GetCalculationParametersAsync()
4b. [AMENDMENT v1.1] Construir enabledSet = HashSet(quote.CoverageOptions.EnabledGuarantees)
5. Para cada ubicación:
   hasDisabledGuarantee = ubicacion.Guarantees.ALGUNA(g => g.GuaranteeKey ∉ enabledSet)
   SI validationStatus == "incomplete" O hasDisabledGuarantee:
     premiumsByLocation.ADD({ index, name, netPremium: 0, status: "incomplete", coverages: [] })
     CONTINUAR
   SINO:
     covPremiums = calcularPrimasPorCobertura(ubicacion, fireTariffs, catTariffs, equipFactors)
     locNetPremium = PremiumCalculator.CalculateLocationNetPremium(covPremiums)
     premiumsByLocation.ADD({ index, name, netPremium: locNetPremium, status: "calculable", coverages: covPremiums })
6. netPremium = Σ premiumsByLocation[calculable].netPremium
7. SI netPremium == 0 Y no hay ubicaciones calculables → throw InvalidQuoteStateException
8. (beforeTax, withTax) = PremiumCalculator.CalculateCommercialPremium(netPremium, params...)
9. Persistir: UpdateFinancialResultAsync(folio, version, netPremium, beforeTax, withTax, premiumsByLocation)
10. Retornar CalculateResultResponse
```

### 4.2 Fórmulas por cobertura (PremiumCalculator — funciones puras)

```
FUNCIÓN calcularPrimasPorCobertura(ubicacion, fireTariffs, catTariffs, equipFactors) → List<CoveragePremium>:
  fireRate = fireTariffs.BUSCAR(fireKey == ubicacion.BusinessLine.FireKey).baseRate
  catZone  = ubicacion.CatZone
  catData  = catTariffs.BUSCAR(zone == catZone)
  techLevel = obtenerTechnicalLevel(ubicacion.ZipCode)  // lookup en memoria o por ZipCodeDto cacheado

  PARA CADA guarantee EN ubicacion.Guarantees:
    SEGÚN guarantee.GuaranteeKey:

      "building_fire", "contents_fire", "coverage_extension":
        rate = fireRate
        premium = guarantee.InsuredAmount × rate

      "cat_tev":
        rate = catData.tevFactor
        premium = guarantee.InsuredAmount × rate

      "cat_fhm":
        rate = catData.fhmFactor     // S-FHM: aproximación simplificada
        premium = guarantee.InsuredAmount × rate

      "debris_removal", "extraordinary_expenses":
        rate = SimplifiedTariffRates.SupplementaryRate  // 0.0010
        premium = guarantee.InsuredAmount × rate

      "rent_loss", "business_interruption":
        rate = SimplifiedTariffRates.IncomeRate  // 0.0015
        premium = guarantee.InsuredAmount × rate

      "electronic_equipment":
        equipFactor = equipFactors.BUSCAR(
          equipmentClass == SimplifiedTariffRates.DefaultEquipmentClass AND
          zoneLevel == techLevel
        )
        SI equipFactor encontrado:
          rate = equipFactor.factor
        SINO:
          rate = equipFactors[0].factor  // fallback al primer factor disponible
        premium = guarantee.InsuredAmount × rate

      "theft", "cash_and_securities":
        rate = SimplifiedTariffRates.SpecialRate  // 0.0020
        premium = guarantee.InsuredAmount × rate

      "glass", "illuminated_signs":
        rate = 0  // tarifa plana, no se basa en suma asegurada
        premium = SimplifiedTariffRates.FlatPremium  // 500.00

    covPremiums.ADD({ guaranteeKey, insuredAmount, rate, premium })

  RETORNA covPremiums
```

### 4.3 Fórmula de prima comercial

```
FUNCIÓN CalculateCommercialPremium(netPremium, expenses, commission, rights, surcharges, iva):
  loadingFactor = 1 + expenses + commission + rights + surcharges
                = 1 + 0.05 + 0.10 + 0.03 + 0.02
                = 1.20

  beforeTax = netPremium × loadingFactor
  withTax   = beforeTax × (1 + iva)
            = beforeTax × 1.16

  RETORNA (Math.Round(beforeTax, 2), Math.Round(withTax, 2))
```

### 4.4 Obtención de `technicalLevel`

El Use Case necesita `technicalLevel` del código postal de cada ubicación para el lookup de `electronic_equipment`. Opciones:

1. El `ZipCodeDto` ya se consultó en SPEC-006 cuando el usuario ingresó el CP y se podría haber persistido. Sin embargo, `Location` no almacena `technicalLevel`.
2. **Decisión**: El Use Case hace lookup de `technicalLevel` por cada CP único de las ubicaciones calculables llamando a `ICoreOhsClient.GetZipCodeAsync()`. Los CPs se agrupan para evitar llamadas duplicadas.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos modificados |
|---|---|---|
| `property_quotes` | UpdateOne | `netPremium`, `commercialPremiumBeforeTax` (nuevo), `commercialPremium`, `premiumsByLocation`, `quoteStatus`, `version`, `metadata.updatedAt`, `metadata.lastWizardStep` |

### 5.2 Cambios de esquema

| Campo | Tipo ANTES | Tipo DESPUÉS |
|---|---|---|
| `commercialPremiumBeforeTax` | No existe | `decimal` (nuevo) |

### 5.3 Índices requeridos

Ninguno nuevo. Usa `folioNumber_1` existente.

### 5.4 Datos semilla

Ninguno. Las tarifas vienen de `cotizador-core-mock` fixtures (SPEC-001).

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-009-01 | Fórmulas simplificadas por grupo de cobertura (ver §4.2). No se aplican ajustes actuariales reales | S-04 del reto: "simplified documented formulas" | Montos no realistas vs producción; aceptable para el reto | usuario |
| SUP-009-02 | S-FHM: `Location` no expone `group` ni `condition`. CAT FHM usa `catTariffs.fhmFactor` como aproximación simplificada. El lookup granular de `fhmTariffs` (por group/zone/condition) queda fuera de alcance. Si en el futuro se requiere granularidad, añadir `fhmGroup` a `Location` en una oleada posterior | `Location` no tiene los campos necesarios para el lookup completo | Prima CAT FHM será una aproximación, no el valor exacto | usuario |
| SUP-009-03 | Prima comercial usa los 5 campos del fixture: `commercialPremiumBeforeTax = netPremium × (1 + 0.05 + 0.10 + 0.03 + 0.02)` = `netPremium × 1.20`; `commercialPremium = beforeTax × (1 + 0.16)` = `beforeTax × 1.16`. Se persisten y muestran 3 valores | Usa todos los campos disponibles eliminando riesgo de dominio | Si se agregan más parámetros, ajustar la fórmula | usuario |
| SUP-009-04 | Body de `POST .../calculate` es solo `{ "version": N }`. El motor lee TODOS los datos del folio persistido | Simplifica el contrato y asegura que se calcula sobre datos guardados, no transitorios | Si se requieren overrides en request (ej. "calcular solo ubicaciones 1 y 2"), agregar campos opcionales | usuario |
| SUP-009-05 | `electronic_equipment` default a `equipmentClass = "A"` porque `Location` no tiene este campo. `zoneLevel` se obtiene de `zipCode.technicalLevel` del CP | Simplificación documentada, cumple con S-04 | Prima de equipo electrónico será una aproximación | spec-generator |
| SUP-009-06 | `technicalLevel` se obtiene consultando CP de cada ubicación al momento del cálculo (agrupando CPs duplicados) | Evita agregar campo a Location; los CPs son pocos y el lookup es rápido con caché del mock | Si el mock es lento o tiene muchos CPs, considerar cachear en Location | spec-generator |
| SUP-009-07 | Si `fireKey` no se encuentra en `fireTariffs`, esa cobertura genera rate=0 y premium=0 (sin bloquear). Se puede loggear como warning | Defensive coding; no debe detener todo el cálculo por un key faltante | Si se requiere bloqueo por key faltante, cambiar a exception | spec-generator |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [business-rules]     (Fase 1.5, PremiumCalculator + SimplifiedTariffRates)
        │
        ├── [backend-developer]  (Fase 2, Use Case + Controller + modificar IQuoteRepository)
        └── [frontend-developer] (Fase 2, CalculateButton + mutation — mínimo)
                │
                ├── [test-engineer-backend]   (Fase 3)
                └── [test-engineer-frontend]  (Fase 3)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `business-rules` | `spec-generator` | `specs/premium-calculation-engine.spec.md` → `status: APPROVED` |
| `backend-developer` | `spec-generator` + `business-rules` | Spec APPROVED + PremiumCalculator creado en Domain |
| `frontend-developer` | `spec-generator` | Spec APPROVED (no requiere design spec) |
| `test-engineer-backend` | `backend-developer` | Implementación completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-001 | core-reference-service | depende-de (endpoints de tarifas: fire, cat, electronic-equipment, calculation-parameters) |
| SPEC-002 | quote-data-model | depende-de (PropertyQuote, LocationPremium, CoveragePremium, UpdateFinancialResultAsync) |
| SPEC-003 | folio-creation | depende-de (folio debe existir) |
| SPEC-006 | location-management | depende-de (ubicaciones con Guarantees, ValidationStatus, CatZone, BusinessLine.FireKey) |
| SPEC-007 | coverage-options-configuration | depende-de (EnabledGuarantees whitelist) |
| SPEC-008 | quote-state-progress | afecta (calculationResult en QuoteStateDto se pobla con los datos persistidos por este motor) |
| SPEC-010 | results-display | afecta (la UI de resultados consume los datos persistidos por este motor) |

---

## 8. LISTA DE TAREAS

### 8.1 business-rules (Domain)

- [ ] Crear `Cotizador.Domain/Services/PremiumCalculator.cs` — static class con funciones puras
  - `CalculateCoveragePremium(guaranteeKey, insuredAmount, rate)` → `CoveragePremium`
  - `CalculateLocationNetPremium(coveragePremiums)` → `decimal`
  - `CalculateCommercialPremium(netPremium, expenses, commission, rights, surcharges, iva)` → `(decimal BeforeTax, decimal WithTax)`
- [ ] Crear `Cotizador.Domain/Constants/SimplifiedTariffRates.cs` — constantes documentadas

### 8.2 backend-developer

- [ ] Agregar campo `CommercialPremiumBeforeTax` a `PropertyQuote`
- [ ] Modificar `IQuoteRepository.UpdateFinancialResultAsync()` — agregar parámetro `commercialPremiumBeforeTax`
- [ ] Actualizar implementación de `UpdateFinancialResultAsync` en `QuoteRepository` — incluir `commercialPremiumBeforeTax` en `$set`
- [ ] Crear DTOs: `CalculateRequest`, `CalculateResultResponse` (reutilizar `LocationPremiumDto`, `CoveragePremiumDto` de SPEC-008)
- [ ] Crear validador FluentValidation `CalculateRequestValidator` — `version` required > 0
- [ ] Crear `ICalculateQuoteUseCase` interface
- [ ] Implementar `CalculateQuoteUseCase`:
  - Leer folio
  - Leer tarifas en paralelo (`Task.WhenAll`)
  - Agrupar CPs de ubicaciones calculables → batch lookup de technicalLevel
  - Para cada ubicación: evaluar calculabilidad, calcular primas por cobertura
  - Consolidar prima neta, derivar prima comercial
  - Persistir resultado atómico
  - Retornar response
- [ ] Agregar endpoint `POST /v1/quotes/{folio}/calculate` en `QuoteController`
- [ ] Registrar Use Case + Validator en `Program.cs`
- [ ] Mensajes de error en español (ADR-008)

### 8.3 frontend-developer

- [ ] Crear `features/calculate-quote/` — mutation hook + CalculateButton
- [ ] `CalculateButton` disabled si `readyForCalculation == false` (dato de SPEC-008 query)
- [ ] Al mutar exitosamente, invalidar `['quote-state', folio]`
- [ ] Agregar ruta en `shared/api/endpoints.ts`

### 8.4 test-engineer-backend

- [ ] `PremiumCalculatorTests` — CalculateCoveragePremium: building_fire con rate 0.00125 y insuredAmount 5M → premium 6250
- [ ] `PremiumCalculatorTests` — CalculateCoveragePremium: glass → premium fija 500
- [ ] `PremiumCalculatorTests` — CalculateLocationNetPremium: suma correcta de 3 coberturas
- [ ] `PremiumCalculatorTests` — CalculateCommercialPremium: netPremium 100k → beforeTax 120k, withTax 139200
- [ ] `CalculateQuoteUseCaseTests` — 2 calculables + 1 incompleta → netPremium = suma de 2; incompleta con netPremium 0
- [ ] `CalculateQuoteUseCaseTests` — 0 calculables → throws InvalidQuoteStateException
- [ ] `CalculateQuoteUseCaseTests` — version mismatch → throws VersionConflictException
- [ ] `CalculateQuoteUseCaseTests` — folio inexistente → throws FolioNotFoundException
- [ ] `CalculateQuoteUseCaseTests` — core-ohs no disponible → throws CoreOhsUnavailableException
- [ ] `CalculateQuoteUseCaseTests` — fireKey no encontrado → rate 0, warning logged
- [ ] `CalculateQuoteUseCaseTests` — verifica que status pasa a "calculated" y version incrementa
- [ ] `CalculateQuoteUseCaseTests` — **[AMENDMENT v1.1]** ubicación calculable con garantía no habilitada en CoverageOptions → treated as incomplete (netPremium 0)
- [ ] `CalculateQuoteUseCaseTests` — **[AMENDMENT v1.1]** 2 ubicaciones calculables, 1 con garantía deshabilitada → solo 1 calculable en resultado
- [ ] `CalculateQuoteUseCaseTests` — **[AMENDMENT v1.1]** todas las ubicaciones con garantías deshabilitadas + 0 calculables restantes → throws InvalidQuoteStateException
- [ ] `CalculateQuoteUseCaseTests` — electronic_equipment con techLevel lookup correcto
- [ ] Integration: POST /calculate → verificar persistencia atómica (no sobrescribir insuredData, locations)

### 8.5 test-engineer-frontend

- [ ] `CalculateButton.test.tsx` — disabled cuando readyForCalculation false
- [ ] `CalculateButton.test.tsx` — click ejecuta mutación
- [ ] `useCalculateQuote.test.ts` — mutación exitosa invalida quote-state

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready):**
- [ ] Spec en estado `APPROVED`
- [ ] SPEC-001 implementada (core-mock endpoints de tarifas)
- [ ] SPEC-002 implementada (PropertyQuote, repositorio con UpdateFinancialResultAsync)
- [ ] SPEC-006 implementada (ubicaciones con Guarantees, ValidationStatus, CatZone, BusinessLine)
- [ ] SPEC-007 implementada (CoverageOptions con EnabledGuarantees)

**DoD (Definition of Done):**
- [ ] `POST /v1/quotes/{folio}/calculate` responde según contrato §3.4
- [ ] `PremiumCalculator` en Domain con funciones puras — unit testable sin mocks
- [ ] Fórmulas simplificadas documentadas como constantes en `SimplifiedTariffRates`
- [ ] S-FHM documentado: CAT FHM usa `catTariffs.fhmFactor` simplificado
- [ ] Prima comercial usa los 5 campos del fixture: 3 valores persistidos (`netPremium`, `commercialPremiumBeforeTax`, `commercialPremium`)
- [ ] Persistencia atómica sin sobrescribir otras secciones
- [ ] `quoteStatus` cambia a `"calculated"`, version incrementa
- [ ] Ubicaciones incompletas generan alertas pero no bloquean el cálculo
- [ ] **[AMENDMENT v1.1]** Ubicaciones con garantías fuera de `EnabledGuarantees` se tratan como `incomplete` en el resultado del cálculo (RN-009-02b)
- [ ] 0 ubicaciones calculables → HTTP 422
- [ ] Versionado optimista funcional (409 ante conflicto)
- [ ] Frontend: botón "Calcular" disabled si `readyForCalculation == false`
- [ ] Tests unitarios de Domain (PremiumCalculator) pasando
- [ ] Tests unitarios de Use Case pasando
- [ ] Sans violaciones de Clean Architecture
