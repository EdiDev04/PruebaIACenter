# Modelo de Datos — Motor de Primas (SPEC-009)

**Versión:** 1.0  
**Última actualización:** 2026-03-30  
**Base de datos:** MongoDB (MongoDB.Driver)  

---

## Resumen ejecutivo

SPEC-009 agrega un campo a `PropertyQuote` (`CommercialPremiumBeforeTax`) y actualiza el método de persistencia de resultados financieros. No requiere nuevas colecciones ni índices especiales. Todo es código C# con deserialización automática a/desde BSON.

---

## Colección principal: `property_quotes`

### Estructura de documento (PropertyQuote)

```csharp
public class PropertyQuote
{
    public string FolioNumber { get; set; }
    public string QuoteStatus { get; set; }
    public InsuredData InsuredData { get; set; }
    public ConductionData ConductionData { get; set; }
    public string AgentCode { get; set; }
    public string RiskClassification { get; set; }
    public string BusinessType { get; set; }
    public LayoutConfiguration LayoutConfiguration { get; set; }
    public CoverageOptions CoverageOptions { get; set; }
    public List<Location> Locations { get; set; }
    
    // SPEC-009: Resultados financieros
    public decimal NetPremium { get; set; }
    public decimal CommercialPremiumBeforeTax { get; set; }  // NUEVO en SPEC-009
    public decimal CommercialPremium { get; set; }
    public List<LocationPremium> PremiumsByLocation { get; set; }
    
    public int Version { get; set; }
    public QuoteMetadata Metadata { get; set; }
}
```

### JSON en MongoDB (documento ejemplo)

```json
{
  "_id": ObjectId("..."),
  "folioNumber": "DAN-2026-00001",
  "quoteStatus": "calculated",
  "insuredData": { /* ... */ },
  "conductionData": { /* ... */ },
  "agentCode": "AG-001",
  "riskClassification": "low",
  "businessType": "commercial",
  "layoutConfiguration": { /* ... */ },
  "coverageOptions": { /* ... */ },
  "locations": [ /* ... */ ],
  
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
        /* ... más coberturas ... */
      ]
    },
    /* ... más ubicaciones ... */
  ],
  
  "version": 6,
  "metadata": {
    "idempotencyKey": "550e8400-e29b-41d4-a716-446655440000",
    "createdAt": ISODate("2026-03-25T10:00:00Z"),
    "updatedAt": ISODate("2026-03-30T12:34:45.123Z"),
    "lastWizardStep": 4
  }
}
```

---

## Cambios de esquema (deltas de SPEC-008 → SPEC-009)

### Campo nuevo: `CommercialPremiumBeforeTax`

| Propiedad | Valor |
|-----------|-------|
| **Nombre en BD** | `commercialPremiumBeforeTax` |
| **Tipo C#** | `decimal` |
| **Tipo BSON** | `Double` (128-bit IEEE 754) |
| **Rango válido** | 0.00 a 9,999,999.99 |
| **Precisión** | 2 decimales |
| **Propósito** | Prima comercial sin IVA (antes de aplicar impuesto) |
| **Cálculo** | `netPremium × 1.20` (con parámetros estándar) |
| **Nullable** | No (tiene valor por defecto 0 si no se persiste) |
| **Índice** | No |

El campo se persiste con la actualización de resultado financiero. Los documentos existentes sin este campo se tratan como 0 en lectura (valor por defecto de `decimal` en C#).

### Cambios a operaciones existentes

**`UpdateFinancialResultAsync` (IQuoteRepository)**

Antes (SPEC-008):
```csharp
Task UpdateFinancialResultAsync(
    string folioNumber,
    int expectedVersion,
    decimal netPremium,
    decimal commercialPremium,
    List<LocationPremium> premiumsByLocation,
    CancellationToken ct = default);
```

Después (SPEC-009):
```csharp
Task UpdateFinancialResultAsync(
    string folioNumber,
    int expectedVersion,
    decimal netPremium,
    decimal commercialPremiumBeforeTax,  // NUEVO parámetro
    decimal commercialPremium,
    List<LocationPremium> premiumsByLocation,
    CancellationToken ct = default);
```

**Operación MongoDB correspondiente:**

```javascript
db.property_quotes.updateOne(
  {
    folioNumber: "DAN-2026-00001",
    version: 5
  },
  {
    $set: {
      netPremium: 125430.50,
      commercialPremiumBeforeTax: 150516.60,      // NUEVO campo
      commercialPremium: 174599.26,
      premiumsByLocation: [ /* ... */ ],
      quoteStatus: "calculated",
      version: 6,
      "metadata.updatedAt": ISODate("2026-03-30T12:34:45.123Z"),
      "metadata.lastWizardStep": 4
    }
  }
);
```

**Garantías:**
- Solo actualiza 8 campos: `netPremium`, `commercialPremiumBeforeTax`, `commercialPremium`, `premiumsByLocation`, `quoteStatus`, `version`, `metadata.updatedAt`, `metadata.lastWizardStep`
- NO modifica: `insuredData`, `conductionData`, `locations`, `coverageOptions`, etc.
- Operación atómica: un solo `$set` parcial

---

## Tipos de datos (ValueObjects)

### LocationPremium

Estructura que describe la prima consolidada por ubicación:

```csharp
public class LocationPremium
{
    public int LocationIndex { get; set; }
    public string LocationName { get; set; }
    public decimal NetPremium { get; set; }
    public string ValidationStatus { get; set; }  // "calculable" | "incomplete"
    public List<CoveragePremium> CoveragePremiums { get; set; }
}
```

**En BSON:**
```json
{
  "locationIndex": 1,
  "locationName": "Bodega Central CDMX",
  "netPremium": 85000.30,
  "validationStatus": "calculable",
  "coveragePremiums": [ /* array de CoveragePremium */ ]
}
```

### CoveragePremium

Estructura que describe la prima de una garantía individual:

```csharp
public class CoveragePremium
{
    public string GuaranteeKey { get; set; }
    public decimal InsuredAmount { get; set; }
    public decimal Rate { get; set; }
    public decimal Premium { get; set; }
}
```

**En BSON:**
```json
{
  "guaranteeKey": "building_fire",
  "insuredAmount": 5000000.00,
  "rate": 0.00125,
  "premium": 6250.00
}
```

---

## Índices

### Índices existentes (sin cambios)

| Nombre | Campo | Única | Sparse | Propósito |
|--------|-------|-------|--------|----------|
| `idx_folioNumber_unique` | `folioNumber` | ✓ | N/A | Búsqueda rápida por folio + constraint de unicidad |
| `idx_idempotencyKey_unique_sparse` | `metadata.idempotencyKey` | ✓ | ✓ | Idempotencia (si se proporciona) |

### Nuevos índices (SPEC-009)

**No se crean índices nuevos.** Las consultas de actualización filtran por `{ folioNumber, version }`, ambas cubiertas por `idx_folioNumber_unique` (folio) y `version` está en el documento raíz (scan eficiente).

Se recomienda futuro análisis si hay consultas por `{ quoteStatus, createdAt }` (ej. "todos los calculados del mes").

---

## Migraciones de datos

### Migración 1: Agregar campo `CommercialPremiumBeforeTax` a documentos existentes

**Contexto:** Documentos persistidos por SPEC-008 no tienen `commercialPremiumBeforeTax`.

**Acción:** MongoDB 4.2+ applica valor por defecto en lectura (C# deserializa como `0`). No se requiere migración de datos activa. Si se desea rellenar para auditoría histórica:

```javascript
db.property_quotes.updateMany(
  { commercialPremiumBeforeTax: { $exists: false } },
  [
    {
      $set: {
        commercialPremiumBeforeTax: {
          $cond: [
            { $ne: ["$netPremium", 0] },
            { $round: [{ $multiply: ["$netPremium", 1.2] }, 2] },
            0
          ]
        }
      }
    }
  ]
);
```

Este script recalcula el valor faltante como `netPremium × 1.20`. Opcional; no afecta funcionamiento ya que C# trata ausencia como `0`.

---

## Ciclo de vida de un documento

### Estado 1: DRAFT (post-folio creation, SPEC-003)

```json
{
  "folioNumber": "DAN-2026-00001",
  "quoteStatus": "draft",
  "version": 1,
  "netPremium": 0,
  "commercialPremiumBeforeTax": 0,
  "commercialPremium": 0,
  "premiumsByLocation": [],
  "metadata": { "lastWizardStep": 0, "createdAt": "...", "updatedAt": "..." }
}
```

### Estado 2: IN_PROGRESS (durante wizard)

```json
{
  "quoteStatus": "draft",  // o "in_progress" si existe ese estado
  "version": 5,
  "metadata": { "lastWizardStep": 3, "updatedAt": "..." }  // wizard avanzó
}
```

### Estado 3: CALCULATED (post-POST /calculate)

```json
{
  "quoteStatus": "calculated",
  "version": 6,
  "netPremium": 125430.50,
  "commercialPremiumBeforeTax": 150516.60,
  "commercialPremium": 174599.26,
  "premiumsByLocation": [
    { "locationIndex": 1, "netPremium": 85000.30, ... },
    ...
  ],
  "metadata": { "lastWizardStep": 4, "updatedAt": "2026-03-30T12:34:45.123Z" }
}
```

---

## Validaciones de BD

Implementadas en el nivel de aplicación (C#), no en restricciones de BD:

| Validación | Dónde | Cómo |
|----------|-------|------|
| `folioNumber` único | Índice unique `idx_folioNumber_unique` | MongoDB garantiza 1 documento por folio |
| `version` > 0 | C# validator `CalculateRequestValidator` | Rechaza `version <= 0` antes de BD |
| `netPremium` >= 0 | C# tipo `decimal` (no negativo por lógica) | Suma de primas nunca es negativa |
| ≥ 1 ubicación calculable | C# use case `CalculateQuoteUseCase` | Lanza exception si `hasCalculable == false` |
| Precisión 2 decimales | C# `Math.Round(value, 2)` | "Todos los montos redondeados antes de persistir |
| Versionado optimista | Filtro MongoDB `{ folioNumber, version }` | No cumple → `ModifiedCount == 0` → exception |

---

## Comportamiento en lecturas

### Lectura de un documento ANTERIOR a SPEC-009

Documento sin campo `commercialPremiumBeforeTax`:

```json
{
  "folioNumber": "DAN-2026-00001",
  "netPremium": 100000,
  "commercialPremium": 116000,
  // commercialPremiumBeforeTax ausente
}
```

**Al deserializar en C#:**
```csharp
PropertyQuote quote = collection.Find(...).First();
// quote.CommercialPremiumBeforeTax == 0m  (valor por defecto de decimal)
```

**Impacto:** Si se intenta releer y recalcular, el campo faltante se trata transitoriamente como 0. Se recomienda consultar la queries de lectura para nunca estar en este estado inconsistente (ya que POST /calculate `$set` siempre el campo).

---

## Comportamiento en escrituras

### Operación UpdateFinancialResultAsync (sin modificar otros campos)

Garantiza que los cambios son **parciales y atómicos**:
- Solo 8 campos en `$set`
- Índice versionado garantiza que solo el documento correcto se actualiza
- Si la versión no coincide, la actualización falla (0 documentos modificados) → `VersionConflictException`

### Rollback en caso de error

No hay rollback automático. Si `UpdateFinancialResultAsync` falla:
- Versión conflict → cliente debe recargar y reintentar
- Excepción de Red → cliente debe reintentar (idempotencia vía `metadata.idempotencyKey` si está soportada)
- Error de BD → HTTP 500, el documento permanece sin cambios

---

## Consultas de ejemplo

### Consulta 1: Leer folio para calcular

```csharp
FilterDefinition<PropertyQuote> filter = Builders<PropertyQuote>.Filter.Eq(q => q.FolioNumber, "DAN-2026-00001");
PropertyQuote? quote = await collection.Find(filter).FirstOrDefaultAsync();
```

**Índice usado:** `idx_folioNumber_unique` (escaneo directo, O(log n))

### Consulta 2: Persistir resultado

```csharp
FilterDefinition<PropertyQuote> filter = Builders<PropertyQuote>.Filter.And(
    Builders<PropertyQuote>.Filter.Eq(q => q.FolioNumber, "DAN-2026-00001"),
    Builders<PropertyQuote>.Filter.Eq(q => q.Version, 5)
);

UpdateDefinition<PropertyQuote> update = Builders<PropertyQuote>.Update
    .Set(q => q.NetPremium, 125430.50m)
    .Set(q => q.CommercialPremiumBeforeTax, 150516.60m)
    .Set(q => q.CommercialPremium, 174599.26m)
    .Set(q => q.PremiumsByLocation, premiumsByLocation)
    .Set(q => q.QuoteStatus, "calculated")
    .Set(q => q.Version, 6)
    .Set(q => q.Metadata.UpdatedAt, DateTime.UtcNow)
    .Set(q => q.Metadata.LastWizardStep, 4);

UpdateResult result = await collection.UpdateOneAsync(filter, update);
if (result.ModifiedCount == 0)
    throw new VersionConflictException(...);
```

**Índice usado:** `idx_folioNumber_unique` (filter por folio)

### Consulta 3: Estadísticas (futuro)

```csharp
// Contar cotizaciones calculadas
collection.CountDocuments(Builders<PropertyQuote>.Filter.Eq(q => q.QuoteStatus, "calculated"))
```

**Recomendación:** Crear índice `idx_quoteStatus` si frecuencia > 100 consultas/día.

---

## Capacidad y crecimiento

### Tamaño de documento típico

```
folioNumber:                 ~50 bytes
quoteStatus:                 ~10 bytes
insuredData:                 ~500 bytes
conductionData:              ~300 bytes
locations[] (3 ubicaciones): ~5,000 bytes
  - guarantees (5-10 por ubicación): ~1,500 bytes
coverageOptions:             ~500 bytes
premiumsByLocation[] (3):    ~1,500 bytes (SPEC-009)
metadata:                    ~200 bytes

TOTAL: ~8,500 - 10,000 bytes por documento
```

### Proyecciones de BD

- 10,000 folios activos por año × 10 KB = **100 MB** por año
- Índices (2 índices, ~5% del tamaño total) = **~5 MB**
- **Presupuesto de 3 años:** ~330 MB + índices

---

## Backups y restauración

### Estrategia de backup (recomendada)

- **Frecuencia:** Diaria (cron)
- **Herramienta:** `mongodump` / `mongorestore`
- **Retención:** 30 días (rolling window)

```bash
mongodump --uri="mongodb+srv://user:pass@cluster.mongodb.net/cotizador" \
  --out=backups/cotizador-$(date +%Y%m%d)

# Restaurar (si es necesario)
mongorestore --uri="mongodb+srv://..." backups/cotizador-20260330/
```

---

## Notas de implementación

1. **Versionado:** Crítico para evitar race conditions. Siempre verificar `ModifiedCount` post-update.
2. **Precisión decimal:** MongoDB no tiene `decimal` nativo. `Math.Round` en C# antes de persistir.
3. **Índices:** Revisión trimestral de planos de consulta (`.Explain()` en MongoDB 3.6+).
4. **Compresión:** MongoDB 4.2+ soporta compresión Snappy/Zstd (configurar en opciones de BD).
