# Lógica de Cálculo — Motor de Primas (SPEC-009)

**Versión:** 1.0  
**Última actualización:** 2026-03-30  
**Fuente:** `Cotizador.Domain.Services.PremiumCalculator` + `Cotizador.Application.UseCases.CalculateQuoteUseCase`

---

## Resumen ejecutivo

El motor ejecuta un algoritmo de 10 pasos:
1. Leer folio completo desde BD
2. Validar versión (versionado optimista)
3. Consultar tarifas en paralelo desde core-ohs
4. Obtener `technicalLevel` por código postal único
5. Calcular prima por ubicación (iteración)
6. Consolidar prima neta total
7. Validar que existe al menos una ubicación calculable
8. Calcular prima comercial (bruta y final con IVA)
9. Persistir resultado atómico en BD
10. Retornar `CalculateResultResponse`

**Invariante:** Ubicaciones incompletas nunca bloquean; generan alertas pero no impiden cálculo.

---

## Criterios de calculabilidad por ubicación

| Criterio | Condición | Si cumple | Si no cumple |
|----------|-----------|----------|-------------|
| **Código postal** | CP válido de 5 dígitos | ✓ | Alerta: "Código postal requerido" |
| **Giro comercial** | `BusinessLine.FireKey` definida | ✓ | Alerta: "Giro comercial requerido" |
| **Garantías tarifables** | ≥ 1 garantía con suma asegurada > 0 (o tarifa plana) | ✓ | Alerta: "Al menos una garantía es requerida" |

**Resolución:**
```
Si (CP válido ∧ FireKey ∧ Garantías) → validationStatus = "calculable"
Sino → validationStatus = "incomplete"
```

Solo las ubicaciones `"calculable"` participan en el cálculo de prima. Las `"incomplete"` aparecen en el resultado con `netPremium = 0` y `coveragePremiums = []`.

---

## Algoritmo paso a paso

### Paso 1 | Leer PropertyQuote

```csharp
PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
if (quote is null)
    throw FolioNotFoundException(folioNumber);
```

Lee el folio completo de MongoDB con todas sus secciones (ubicaciones, coberturas, etc.).

---

### Paso 2 | Validar versión

```csharp
if (quote.Version != request.Version)
    throw new VersionConflictException(folioNumber, request.Version);
```

Versionado optimista. Si la versión del cliente no coincide con la persistida → HTTP 409 Conflict.

---

### Paso 3 | Consultar tarifas en paralelo

```csharp
Task<List<FireTariffDto>> fireTariffsTask = _coreOhsClient.GetFireTariffsAsync(ct);
Task<List<CatTariffDto>> catTariffsTask = _coreOhsClient.GetCatTariffsAsync(ct);
Task<List<ElectronicEquipmentFactorDto>> equipFactorsTask = _coreOhsClient.GetElectronicEquipmentFactorsAsync(ct);
Task<CalculationParametersDto> calcParamsTask = _coreOhsClient.GetCalculationParametersAsync(ct);

await Task.WhenAll(fireTariffsTask, catTariffsTask, equipFactorsTask, calcParamsTask);

List<FireTariffDto> fireTariffs = await fireTariffsTask;
List<CatTariffDto> catTariffs = await catTariffsTask;
List<ElectronicEquipmentFactorDto> equipFactors = await equipFactorsTask;
CalculationParametersDto calcParams = await calcParamsTask;
```

Se parallelizan 4 llamadas HTTP a core-ohs para reducir latencia. Si alguna falla → HTTP 503.

**Tarifas obtenidas:**
- `fireTariffs[]` → lookup por `fireKey` de ubicación
- `catTariffs[]` → lookup por `zone` (CatZone)
- `equipFactors[]` → lookup por `equipmentClass + zoneLevel`
- `calcParams` → parámetros de loading factor e IVA

---

### Paso 4 | Batch lookup de codes técnicos por CP

```csharp
IEnumerable<string> calculableZipCodes = quote.Locations
    .Where(l => l.ValidationStatus == "calculable" && !string.IsNullOrWhiteSpace(l.ZipCode))
    .Select(l => l.ZipCode)
    .Distinct();

Dictionary<string, int> techLevelByZip = new();
foreach (string zipCode in calculableZipCodes)
{
    ZipCodeDto? zipData = await _coreOhsClient.GetZipCodeAsync(zipCode, ct);
    if (zipData is not null)
        techLevelByZip[zipCode] = zipData.TechnicalLevel;
}
```

Agrupa CPs únicos de ubicaciones calculables y consulta `technicalLevel` en batch (eliminando duplicados).

`technicalLevel` se usa después para lookup en `electronic_equipment` factors.

---

### Paso 5 | Calcular prima por ubicación (iteración)

```csharp
var premiumsByLocation = new List<LocationPremium>();

foreach (Location location in quote.Locations)
{
    if (location.ValidationStatus != "calculable")
    {
        // Ubicación incompleta
        premiumsByLocation.Add(new LocationPremium
        {
            LocationIndex = location.Index,
            LocationName = location.LocationName,
            NetPremium = 0m,
            ValidationStatus = "incomplete",
            CoveragePremiums = new List<CoveragePremium>(),
        });
        continue;
    }

    // Calcular coberturas para ubicación calculable
    List<CoveragePremium> coveragePremiums = CalculateCoveragePremiums(
        location, fireTariffs, catTariffs, equipFactors, techLevelByZip);

    decimal locationNetPremium = PremiumCalculator.CalculateLocationNetPremium(coveragePremiums);

    premiumsByLocation.Add(new LocationPremium
    {
        LocationIndex = location.Index,
        LocationName = location.LocationName,
        NetPremium = locationNetPremium,
        ValidationStatus = "calculable",
        CoveragePremiums = coveragePremiums,
    });
}
```

#### Sub-función: CalculateCoveragePremiums

Para cada ubicación calculable:

```csharp
private List<CoveragePremium> CalculateCoveragePremiums(
    Location location,
    List<FireTariffDto> fireTariffs,
    List<CatTariffDto> catTariffs,
    List<ElectronicEquipmentFactorDto> equipFactors,
    Dictionary<string, int> techLevelByZip)
{
    // Obtener tasa de fuego válida para ubicación
    FireTariffDto? fireTariff = fireTariffs.FirstOrDefault(f => f.FireKey == location.BusinessLine.FireKey);
    decimal fireRate = fireTariff?.BaseRate ?? 0m;

    if (fireTariff is null)
        _logger.LogWarning("FireKey {FireKey} not found. Using rate 0.", location.BusinessLine.FireKey);

    // Obtener datos CAT (TEV/FHM) por zone
    CatTariffDto? catData = catTariffs.FirstOrDefault(c => c.Zone == location.CatZone);

    // Obtener nivel técnico de CP para electronic_equipment lookup
    techLevelByZip.TryGetValue(location.ZipCode, out int techLevel);

    var coveragePremiums = new List<CoveragePremium>();

    // Para cada garantía de la ubicación
    foreach (LocationGuarantee guarantee in location.Guarantees)
    {
        decimal rate = ResolveRate(guarantee.GuaranteeKey, fireRate, catData, equipFactors, techLevel);
        CoveragePremium coverage = PremiumCalculator.CalculateCoveragePremium(
            guarantee.GuaranteeKey,
            guarantee.InsuredAmount,
            rate);
        coveragePremiums.Add(coverage);
    }

    return coveragePremiums;
}
```

#### Sub-función: ResolveRate (resolución de tasa por cobertura)

```csharp
private static decimal ResolveRate(
    string guaranteeKey,
    decimal fireRate,
    CatTariffDto? catData,
    List<ElectronicEquipmentFactorDto> equipFactors,
    int techLevel)
{
    return guaranteeKey switch
    {
        "building_fire" or "contents_fire" or "coverage_extension"
            => fireRate,

        "cat_tev"
            => catData?.TevFactor ?? 0m,

        "cat_fhm"
            => catData?.FhmFactor ?? 0m,

        "debris_removal" or "extraordinary_expenses"
            => SimplifiedTariffRates.SupplementaryRate,  // 0.0010

        "rent_loss" or "business_interruption"
            => SimplifiedTariffRates.IncomeRate,  // 0.0015

        "theft" or "cash_and_securities"
            => SimplifiedTariffRates.SpecialRate,  // 0.0020

        "electronic_equipment"
            => ResolveEquipmentRate(equipFactors, techLevel),

        // glass, illuminated_signs: tarifa plana, rate será ignorada en CalculateCoveragePremium
        _ => 0m,
    };
}
```

#### Sub-función: ResolveEquipmentRate

```csharp
private static decimal ResolveEquipmentRate(
    List<ElectronicEquipmentFactorDto> equipFactors,
    int techLevel)
{
    ElectronicEquipmentFactorDto? factor = equipFactors.FirstOrDefault(
        e => e.EquipmentClass == "A" && e.ZoneLevel == techLevel);

    return factor?.Factor ?? equipFactors.FirstOrDefault()?.Factor ?? 0m;
}
```

**Lógica:**
- Buscar por `equipmentClass = "A"` (por defecto, SUP-009-05) Y `zoneLevel = technicalLevel` obtenido del CP
- Si no encuentra exacto, fallback al primer factor disponible
- Si no hay factores, retorna 0 (la cobertura tendrá prima 0)

---

### Paso 6 | Consolidar prima neta total

```csharp
decimal netPremium = premiumsByLocation
    .Where(p => p.ValidationStatus == "calculable")
    .Sum(p => p.NetPremium);
```

Suma SOLO las primas netas de ubicaciones calculables, omitiendo las incompletas (que tienen `netPremium = 0`).

---

### Paso 7 | Validar que existe al menos una ubicación calculable

```csharp
bool hasCalculable = premiumsByLocation.Any(p => p.ValidationStatus == "calculable");
if (!hasCalculable)
    throw new InvalidQuoteStateException(
        folioNumber,
        quote.QuoteStatus,
        "No hay ubicaciones calculables para ejecutar el cálculo");
```

Si **todas** las ubicaciones son incompletas → HTTP 422 `invalidQuoteState`.

---

### Paso 8 | Calcular prima comercial

```csharp
(decimal beforeTax, decimal withTax) = PremiumCalculator.CalculateCommercialPremium(
    netPremium,
    calcParams.ExpeditionExpenses,      // 0.05
    calcParams.AgentCommission,         // 0.10
    calcParams.IssuingRights,           // 0.03
    calcParams.Surcharges,              // 0.02
    calcParams.Iva);                    // 0.16
```

#### Función: PremiumCalculator.CalculateCommercialPremium

```csharp
public static (decimal BeforeTax, decimal WithTax) CalculateCommercialPremium(
    decimal netPremium,
    decimal expeditionExpenses,
    decimal agentCommission,
    decimal issuingRights,
    decimal surcharges,
    decimal iva)
{
    decimal loadingFactor = 1m + expeditionExpenses + agentCommission + issuingRights + surcharges;
    decimal beforeTax = Math.Round(netPremium * loadingFactor, 2);
    decimal withTax = Math.Round(beforeTax * (1m + iva), 2);
    return (beforeTax, withTax);
}
```

**Con parámetros estándar:**
```
loadingFactor = 1 + 0.05 + 0.10 + 0.03 + 0.02 = 1.20
beforeTax = netPremium × 1.20
withTax = beforeTax × (1 + 0.16) = beforeTax × 1.16
```

Ambos valores se redondean a 2 decimales (banker's rounding en .NET).

---

### Paso 9 | Persistir resultado atómico

```csharp
await _repository.UpdateFinancialResultAsync(
    folioNumber,
    request.Version,           // expectedVersion
    netPremium,
    beforeTax,                 // commercialPremiumBeforeTax
    withTax,                   // commercialPremium
    premiumsByLocation,
    ct);
```

#### Operación MongoDB (tupla)

```javascript
// Filtro (versionado optimista)
db.property_quotes.findAndModify({
  query: { folioNumber: "DAN-2026-00001", version: 5 },
  update: {
    $set: {
      netPremium: 125430.50,
      commercialPremiumBeforeTax: 150516.60,
      commercialPremium: 174599.26,
      premiumsByLocation: [
        { locationIndex: 1, locationName: "...", netPremium: ..., validationStatus: "calculable", coveragePremiums: [...] },
        ...
      ],
      quoteStatus: "calculated",
      version: 6,
      metadata: {
        updatedAt: ISODate("2026-03-30T12:34:45.123Z"),
        lastWizardStep: 4,
        // resto de metadata sin cambios
      }
      // insuredData, locations, coverageOptions NO se modifican
    }
  }
});
```

Si `ModifiedCount == 0` (la versión no coincide) → `VersionConflictException` → HTTP 409.

---

### Paso 10 | Retornar CalculateResultResponse

```csharp
return new CalculateResultResponse(
    netPremium,
    beforeTax,
    withTax,
    locationDtos,          // mapeado de premiumsByLocation
    QuoteStatus.Calculated,
    request.Version + 1    // nueva versión
);
```

La respuesta envuelta en `{ data: ... }` por el controlador.

---

## Fórmulas consolidadas

### Prima por cobertura

**Caso general (suma asegurada × tasa):**
```
premium = insuredAmount × rate
```

**Caso especial (tarifa plana):**
```
premium = 500.00  (para glass, illuminated_signs)
```

Todos redondean a 2 decimales.

### Prima neta por ubicación

```
netPremium_ubicacion = Σ premium_cobertura
                     = Σ (insuredAmount_i × rate_i)  [o 500.00 para planas]
```

### Prima neta total (folio)

```
netPremium = Σ netPremium_ubicacion  [solo ubicaciones calculables]
```

### Prima comercial bruta (sin IVA)

```
commercialPremiumBeforeTax = netPremium × loadingFactor
                           = netPremium × (1 + 0.05 + 0.10 + 0.03 + 0.02)
                           = netPremium × 1.20
```

Donde:
- `0.05` = expeditionExpenses (gastos de trámite)
- `0.10` = agentCommission (comisión de agente)
- `0.03` = issuingRights (derechos de emisión)
- `0.02` = surcharges (recargos diversos)

### Prima comercial final (con IVA)

```
commercialPremium = commercialPremiumBeforeTax × (1 + iva)
                  = commercialPremiumBeforeTax × 1.16
```

---

## Ejemplos numéricos

### Ejemplo 1: 1 ubicación calculable, 1 cobertura

**Datos:**
- Ubicación: Bodega Central, CP 06600, FireKey B-03
- Cobertura: `building_fire`, Suma asegurada = 5,000,000

**Tarifas (core-ohs):**
- `fireTariffs` contiene: `{ fireKey: "B-03", baseRate: 0.00125 }`
- `calcParams` = estándar (0.05, 0.10, 0.03, 0.02, 0.16)

**Cálculo:**
```
rate = 0.00125
premium_cobertura = 5,000,000 × 0.00125 = 6,250.00
netPremium_ubicacion = 6,250.00
netPremium = 6,250.00

commercialPremiumBeforeTax = 6,250.00 × 1.20 = 7,500.00
commercialPremium = 7,500.00 × 1.16 = 8,700.00
```

---

### Ejemplo 2: 2 ubicaciones calculables, varias coberturas

**Ubicación 1: Bodega Central**
```
building_fire:   5M × 0.00125 = 6,250.00
contents_fire:   3M × 0.00125 = 3,750.00
cat_tev:         5M × 0.0035  = 17,500.00
glass:           0 × 0        = 500.00 (plana)
theft:           2M × 0.0020  = 4,000.00
net_ubicacion_1 = 31,000.00
```

**Ubicación 2: Sucursal Monterrey**
```
building_fire:   3M × 0.00125 = 3,750.00
cat_tev:         3M × 0.0015  = 4,500.00
net_ubicacion_2 = 8,250.00
```

**Consolidación:**
```
netPremium = 31,000.00 + 8,250.00 = 39,250.00

commercialPremiumBeforeTax = 39,250.00 × 1.20 = 47,100.00
commercialPremium = 47,100.00 × 1.16 = 54,636.00
```

---

### Ejemplo 3: Con ubicación incompleta (no interfiere)

**Ubicación 1, 2:** (calculables, como arriba)

**Ubicación 3: Local sin datos**
```
validationStatus = "incomplete"
netPremium = 0
coveragePremiums = []
```

**Cálculo:**
```
netPremium = 31,000 + 8,250 + 0 = 39,250.00  ← omite la incompleta
```

La ubicación 3 aparece en el resultado pero no contribuye.

---

## Notas técnicas

### Precisión y redondeo

- **Tipo:** `decimal(18, 2)` en .NET (128 bits)
- **Redondeo:** `Math.Round(value, 2)` usa banker's rounding (redondeo al par más cercano)
- **Ejemplo:** 1.235 → 1.24 (redondeado al 4, par más cercano)

### Rendimiento

- **Parallelización:** 4 llamadas a core-ohs ejecutadas en paralelo (no secuencial)
- **Batch lookup:** CPs únicos agrupados antes de consultar al API
- **Timeout:** 30 segundos por llamada HTTP (configurable)

### Logging

- **Warning:** Si `fireKey` no se encuentra en el catálogo
- **Info:** Duración total de cálculo
- **Error:** Si core-ohs no responde

### Seguridad

- **Inyección:** N/A — tarifas/parámetros vienen de BD confiable (core-ohs)
- **Desbordamiento:** Validar que las sumas no excedan `decimal.MaxValue`
- **División por cero:** N/A — no hay divisiones

---

## Supuestos documentados (SPEC-009)

| ID | Supuesto | Impacto |
|---|---|---|
| SUP-009-01 | Fórmulas simplificadas (no actuariales) | Montos no realistas vs. producción; aceptable para reto |
| SUP-009-03 | Prima comercial con 5 parámetros estándar | Si se agregan más, ajustar fórmula |
| SUP-009-05 | `electronic_equipment` por defecto clase "A" | Prima aproximada, no exacta |
| SUP-009-06 | `technicalLevel` consultado en runtime | Si hay CP no resuelto, fallback a rate 0 |
