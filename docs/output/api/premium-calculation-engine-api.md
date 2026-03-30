# API: Motor de Cálculo de Primas (SPEC-009)

**Versión:** 1.0  
**Última actualización:** 2026-03-30  
**Estado:** Implementado  

---

## Endpoint

```
POST /v1/quotes/{folio}/calculate
```

**Descripción:**  
Ejecuta el motor de cálculo de primas sobre un folio completo. Lee las ubicaciones calculables, consulta tarifas técnicas de core-ohs, aplica fórmulas simplificadas por cobertura, consolida la prima neta total, y deriva la prima comercial con y sin IVA. Persiste el resultado financiero de forma atómica.

---

## Autenticación

**Esquema:** Basic Auth  
**Header requerido:** `Authorization: Basic <base64(username:password)>`

---

## Parámetros

### Path

| Parámetro | Tipo | Descripción | Ejemplo |
|-----------|------|-------------|---------|
| `folio` | string | Número de folio en formato DAN-YYYY-NNNNN | `DAN-2026-00001` |

### Body (JSON)

```json
{
  "version": 5
}
```

| Campo | Tipo | Requerido | Validación | Descripción |
|-------|------|----------|-----------|-------------|
| `version` | integer | ✓ | > 0 | Versión actual del folio (versionado optimista) |

### Headers adicionales (opcionales)

| Header | Descripción |
|--------|-------------|
| `X-Correlation-Id` | UUID v4 para trazabilidad (generado por middleware si no se provee) |
| `Content-Type` | `application/json` |

---

## Response 200 (Exitoso)

```json
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
          {
            "guaranteeKey": "building_fire",
            "insuredAmount": 5000000.00,
            "rate": 0.00125,
            "premium": 6250.00
          },
          {
            "guaranteeKey": "contents_fire",
            "insuredAmount": 3000000.00,
            "rate": 0.00125,
            "premium": 3750.00
          },
          {
            "guaranteeKey": "cat_tev",
            "insuredAmount": 5000000.00,
            "rate": 0.0035,
            "premium": 17500.00
          },
          {
            "guaranteeKey": "glass",
            "insuredAmount": 0.00,
            "rate": 0.00,
            "premium": 500.00
          },
          {
            "guaranteeKey": "theft",
            "insuredAmount": 2000000.00,
            "rate": 0.0020,
            "premium": 4000.00
          }
        ]
      },
      {
        "locationIndex": 2,
        "locationName": "Sucursal Monterrey",
        "netPremium": 40430.20,
        "validationStatus": "calculable",
        "coveragePremiums": [
          {
            "guaranteeKey": "building_fire",
            "insuredAmount": 3000000.00,
            "rate": 0.00080,
            "premium": 2400.00
          },
          {
            "guaranteeKey": "cat_tev",
            "insuredAmount": 3000000.00,
            "rate": 0.0015,
            "premium": 4500.00
          }
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
```

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `netPremium` | decimal(18,2) | Prima neta total (suma de primas por cobertura, Σ ubicaciones calculables) |
| `commercialPremiumBeforeTax` | decimal(18,2) | Prima comercial bruta (sin IVA) = netPremium × 1.20 |
| `commercialPremium` | decimal(18,2) | Prima comercial final (con IVA) = commercialPremiumBeforeTax × 1.16 |
| `premiumsByLocation[].locationIndex` | integer | Índice 1-based de la ubicación |
| `premiumsByLocation[].locationName` | string | Nombre de la ubicación |
| `premiumsByLocation[].netPremium` | decimal(18,2) | Prima neta de la ubicación (Σ coberturas) o 0 si incompleta |
| `premiumsByLocation[].validationStatus` | string | `"calculable"` o `"incomplete"` |
| `premiumsByLocation[].coveragePremiums[]` | array | Array vacío si incompleta; con primas por cobertura si calculable |
| `premiumsByLocation[].coveragePremiums[].guaranteeKey` | string | Clave técnica de la cobertura (ej: `"building_fire"`) |
| `premiumsByLocation[].coveragePremiums[].insuredAmount` | decimal(18,2) | Suma asegurada de la cobertura (0 para tarifas planas) |
| `premiumsByLocation[].coveragePremiums[].rate` | decimal(8,6) | Tasa aplicada (0 para tarifas planas) |
| `premiumsByLocation[].coveragePremiums[].premium` | decimal(18,2) | Prima de la cobertura individual |
| `quoteStatus` | string | Siempre `"calculated"` si exitoso |
| `version` | integer | Nueva versión del folio (version + 1) |

### Garantías (coverageKeys)

| Clave | Descripción | Cálculo |
|-------|-------------|---------|
| `building_fire` | Incendio estructura | suma_asegurada × tasa_fuego |
| `contents_fire` | Incendio contenidos | suma_asegurada × tasa_fuego |
| `coverage_extension` | Ampliación de cobertura | suma_asegurada × tasa_fuego |
| `cat_tev` | CAT - Terremoto/Volcán/Erupción | suma_asegurada × factor_tev|
| `cat_fhm` | CAT - Huracán/Maremoto | suma_asegurada × factor_fhm |
| `debris_removal` | Remoción de escombros | suma_asegurada × 0.0010 (rate simplificada) |
| `extraordinary_expenses` | Gastos extraordinarios | suma_asegurada × 0.0010 |
| `rent_loss` | Pérdida de renta | suma_asegurada × 0.0015 |
| `business_interruption` | Interrupción de negocio | suma_asegurada × 0.0015 |
| `electronic_equipment` | Equipos electrónicos | suma_asegurada × factor_equipC (por zona técnica) |
| `theft` | Robo | suma_asegurada × 0.0020 |
| `cash_and_securities` | Robo de valores | suma_asegurada × 0.0020 |
| `glass` | Cristales | **Prima fija: 500.00** (no depende de suma asegurada) |
| `illuminated_signs` | Letrero iluminado | **Prima fija: 500.00** (no depende de suma asegurada) |

---

## Response 400 (Validación de entrada)

```json
{
  "type": "validationError",
  "message": "La versión es obligatoria",
  "field": "version"
}
```

**Casos:**
- Folio inválido: `"Formato de folio inválido. Use DAN-YYYY-NNNNN"` (field: `"folio"`)
- Version ausente o <= 0: `"La versión es obligatoria"` (field: `"version"`)

---

## Response 401 (No autorizado)

```json
{
  "type": "unauthorized",
  "message": "Credenciales inválidas o ausentes",
  "field": null
}
```

---

## Response 404 (Folio no encontrado)

```json
{
  "type": "folioNotFound",
  "message": "El folio DAN-2026-99999 no existe",
  "field": null
}
```

---

## Response 409 (Conflicto de versión)

```json
{
  "type": "versionConflict",
  "message": "El folio fue modificado por otro proceso. Recargue para continuar",
  "field": null
}
```

**Causa:** La versión enviadano coincide con la persistida (versionado optimista).

---

## Response 422 (Estado de folio inválido)

```json
{
  "type": "invalidQuoteState",
  "message": "No hay ubicaciones calculables para ejecutar el cálculo",
  "field": null
}
```

**Causa:** El folio no tiene al menos una ubicación con `validationStatus == "calculable"`.

---

## Response 503 (Servicio no disponible)

```json
{
  "type": "coreOhsUnavailable",
  "message": "Servicio de catálogos no disponible, intente más tarde",
  "field": null
}
```

**Causa:** core-ohs no responde (tarifas, parámetros de cálculo, o datos de códigos postales).

---

## Response 500 (Error interno)

```json
{
  "type": "internal",
  "message": "Internal server error",
  "field": null
}
```

---

## Reglas de negocio aplicadas

### RN-009-01: Identificación por folio
Toda cotización se identifica únicamente por `folioNumber` en formato `DAN-YYYY-NNNNN`.

### RN-009-02: Clasificación de calculabilidad
Una ubicación es **calculable** si:
- Tiene código postal válido (5 dígitos)
- Tiene `BusinessLine.FireKey` definida
- Tiene al menos una garantía tarifable

Si le falta alguno, se clasifica como **incompleta** (`validationStatus == "incomplete"`).

### RN-009-03: Ubicaciones incompletas no bloquean cálculo
Si hay ≥ 1 ubicación calculable, el cálculo procede. Las incompletas aparecen con `netPremium: 0` y sin coberturas, pero generan alertas (`blockingAlerts`).

### RN-009-04: Al menos 1 calculable es obligatorio
Si **todas** las ubicaciones son incompletas (`calculable == 0`), se retorna HTTP 422 → `invalidQuoteState`.

### RN-009-05: Prima neta consolidada
```
netPremium = Σ (netPremium de ubicación calculable)
           = Σ (Σ (premium de cobertura))
```

### RN-009-06: Prima comercial bruta
```
commercialPremiumBeforeTax = netPremium × (1 + expeditionExpenses + agentCommission + issuingRights + surcharges)
                           = netPremium × (1 + 0.05 + 0.10 + 0.03 + 0.02)
                           = netPremium × 1.20
```

(Con parámetros estándar del fixture `calculation-parameters`).

### RN-009-07: Prima comercial con IVA
```
commercialPremium = commercialPremiumBeforeTax × (1 + iva)
                  = commercialPremiumBeforeTax × 1.16
```

### RN-009-08: Prima comercial a nivel folio, no por ubicación
La prima comercial se calcula UNA SOLA VEZ sobre la prima neta consolidada total, no por ubicación.

### RN-009-09: Persistencia atómica sin sobrescrituras
La operación de BD usa `$set` parcial. **Solo actualiza:**
- `netPremium`
- `commercialPremiumBeforeTax` (nuevo en SPEC-009)
- `commercialPremium`
- `premiumsByLocation`
- `quoteStatus` → `"calculated"`
- `version` → `version + 1`
- `metadata.updatedAt` → `DateTime.UtcNow`
- `metadata.lastWizardStep` → `4`

**NO modifica:** `insuredData`, `conductionData`, `locations`, `coverageOptions`, etc.

### RN-009-10: Versionado optimista
La persistencia filtra por `{ folioNumber, version }`. Si `ModifiedCount == 0`, falla (409 Conflict). El cliente debe recargar y reintentar.

### RN-009-11: Response envelope
Toda respuesta exitosa (2xx) está envuelta en `{ data: ... }`. Los errores (4xx, 5xx) no tienen envelope.

### RN-009-12: Mensajes de error en español
Todos los mensajes del sistema están en español (ADR-008).

---

## Reglas de tarifas simplificadas (S-04)

Las tarifas no representan valores actuariales reales. Son aproximaciones documentadas aceptables para el reto:

| Tipo | Tasa / Prima | Coberturas | Origen |
|------|-------------|-----------|--------|
| **Fuego base** | Per `fireTariffs.baseRate` | `building_fire`, `contents_fire`, `coverage_extension` | core-ohs tariffs/fire |
| **CAT TEV** | Per `catTariffs.tevFactor` | `cat_tev` | core-ohs tariffs/cat |
| **CAT FHM** | Per `catTariffs.fhmFactor` | `cat_fhm` | core-ohs tariffs/cat |
| **Suplementarias** | 0.0010 | `debris_removal`, `extraordinary_expenses` | SimplifiedTariffRates |
| **Ingresos** | 0.0015 | `rent_loss`, `business_interruption` | SimplifiedTariffRates |
| **Especial** | 0.0020 | `theft`, `cash_and_securities` | SimplifiedTariffRates |
| **Equipo electrónico** | Per factor de zona | `electronic_equipment` | core-ohs tariffs/electronic-equipment |
| **Plano (vidrio)** | 500.00 fijo | `glass`, `illuminated_signs` | SimplifiedTariffRates |

---

## Dependencias de core-ohs

El endpoint consume estas 4 llamadas en paralelo a core-ohs:

```
GET /v1/tariffs/fire
  Retorna: { data: [{ fireKey: string, baseRate: decimal, description: string }, ...] }
  Uso: lookup por fireKey de cada ubicación

GET /v1/tariffs/cat
  Retorna: { data: [{ zone: string, tevFactor: decimal, fhmFactor: decimal }, ...] }
  Uso: lookup por zone (CatZone de ubicación)

GET /v1/tariffs/electronic-equipment
  Retorna: { data: [{ equipmentClass: string, zoneLevel: int, factor: decimal }, ...] }
  Uso: lookup por (equipmentClass="A", zoneLevel=zipCode.technicalLevel)

GET /v1/tariffs/calculation-parameters
  Retorna: { data: { expeditionExpenses: decimal, agentCommission: decimal, issuingRights: decimal, iva: decimal, surcharges: decimal, effectiveDate: string } }
  Uso: parámetros de loading factor e IVA
```

Además, por cada CP único del folio:
```
GET /v1/zip-codes/{zipCode}
  Retorna: { data: { zipCode: string, state: string, municipality: string, neighborhood: string, city: string, catZone: string, technicalLevel: int } }
  Uso: obtener technicalLevel para lookup de electronic_equipment
```

Si alguna llamada falla → HTTP 503.

---

## Ejemplo cURL

```bash
curl -X POST "https://api.cotizador.local/v1/quotes/DAN-2026-00001/calculate" \
  -H "Authorization: Basic dXN1YXJpbzpjb250cmFzZW5h" \
  -H "Content-Type: application/json" \
  -H "X-Correlation-Id: 550e8400-e29b-41d4-a716-446655440000" \
  -d '{
    "version": 5
  }'
```

---

## Notas de implementación

1. **Precisión decimal:** Todos los montos se redondean a 2 decimales (banker's rounding).
2. **Timeout:** Las llamadas paralelas a core-ohs tienen timeout de 30s. Si alguna excede → HTTP 503.
3. **Logging:** FireKey no encontrado → `LogWarning` en el servidor (no falla).
4. **Caché:** Los mocks de core-ohs aplican caché implícitamente en memoria. En producción, considerar Redis.
5. **Circuit breaker:** Si core-ohs está caído > 5 min, exponential backoff automático del cliente HTTP.
