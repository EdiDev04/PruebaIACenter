# Contratos de Integración — SPEC-003 y SPEC-004

> **Generado por:** integration agent  
> **Fecha de verificación:** 2026-03-29  
> **Specs auditadas:** SPEC-003 `folio-creation` · SPEC-004 `general-info-management`  
> **Backend client:** `cotizador-backend/src/Cotizador.Infrastructure/ExternalServices/CoreOhsClient.cs`  
> **Mock:** `cotizador-core-mock/src/` (routes + fixtures + types)

---

## Resumen ejecutivo

| Métrica | Valor |
|---|---|
| Endpoints auditados (inventario completo cliente) | 13 |
| Endpoints consumidos por SPEC-003 | 1 |
| Endpoints consumidos por SPEC-004 | 3 |
| Contratos ✅ OK | 12 |
| Contratos ⚠️ CONTRACT_DRIFT | 1 |
| Contratos ❌ MISSING | 0 |
| Bloqueo para SPEC-003 | **NO** |
| Bloqueo para SPEC-004 | **NO** |

> El único drift detectado (`DRIFT-001`) no afecta a los endpoints de SPEC-003 ni SPEC-004. Afectará a la feature `gestion-ubicaciones` si no se corrige antes de su implementación.

---

## Tabla de contratos — SPEC-003 (folio-creation)

| # | Endpoint mock | Método cliente | Método mock | DTO backend | Estado |
|---|---|---|---|---|---|
| C-001 | `GET /v1/folios/next` | `GenerateFolioAsync()` | `GET` | `FolioDto` | ✅ OK |

---

## Tabla de contratos — SPEC-004 (general-info-management)

| # | Endpoint mock | Método cliente | Método mock | DTO backend | Estado |
|---|---|---|---|---|---|
| C-002 | `GET /v1/subscribers` | `GetSubscribersAsync()` | `GET` | `SubscriberDto` | ✅ OK |
| C-003 | `GET /v1/agents?code=<code>` | `GetAgentByCodeAsync()` | `GET` | `AgentDto` | ✅ OK |
| C-004 | `GET /v1/catalogs/risk-classification` | `GetRiskClassificationsAsync()` | `GET` | `RiskClassificationDto` | ✅ OK |

---

## Inventario completo — todos los endpoints del cliente

Incluye endpoints de features futuras para auditoría preventiva.

| # | Endpoint mock | Método cliente | SPEC | Estado |
|---|---|---|---|---|
| C-001 | `GET /v1/folios/next` | `GenerateFolioAsync()` | SPEC-003 | ✅ OK |
| C-002 | `GET /v1/subscribers` | `GetSubscribersAsync()` | SPEC-004 | ✅ OK |
| C-003 | `GET /v1/agents?code=<code>` | `GetAgentByCodeAsync()` | SPEC-004 | ✅ OK |
| C-004 | `GET /v1/business-lines` | `GetBusinessLinesAsync()` | SPEC-004 | ✅ OK |
| C-005 | `GET /v1/zip-codes/:zipCode` | `GetZipCodeAsync()` | gestion-ubicaciones | ✅ OK |
| C-006 | `POST /v1/zip-codes/validate` | `ValidateZipCodeAsync()` | gestion-ubicaciones | ⚠️ DRIFT-001 |
| C-007 | `GET /v1/catalogs/risk-classification` | `GetRiskClassificationsAsync()` | SPEC-004 | ✅ OK |
| C-008 | `GET /v1/catalogs/guarantees` | `GetGuaranteesAsync()` | opciones-cobertura | ✅ OK |
| C-009 | `GET /v1/tariffs/fire` | `GetFireTariffsAsync()` | motor-calculo | ✅ OK |
| C-010 | `GET /v1/tariffs/cat` | `GetCatTariffsAsync()` | motor-calculo | ✅ OK |
| C-011 | `GET /v1/tariffs/fhm` | `GetFhmTariffsAsync()` | motor-calculo | ✅ OK |
| C-012 | `GET /v1/tariffs/electronic-equipment` | `GetElectronicEquipmentFactorsAsync()` | motor-calculo | ✅ OK |
| C-013 | `GET /v1/tariffs/calculation-parameters` | `GetCalculationParametersAsync()` | motor-calculo | ✅ OK |

---

## Detalle de contratos verificados

### C-001 — GET /v1/folios/next

**Endpoint consumido por:** SPEC-003 `folio-creation`  
**Método backend:** `CoreOhsClient.GenerateFolioAsync()`

#### Mock (fuente de verdad)

```typescript
// routes/folioRoutes.ts
router.get('/next', (_req, res) => {
  const folioResponse: FolioResponse = { folioNumber: `DAN-${year}-${padded}` };
  res.status(200).json({ data: folioResponse });
});

// types/index.ts
interface FolioResponse { folioNumber: string; }
```

Ejemplo de respuesta:
```json
{ "data": { "folioNumber": "DAN-2026-00001" } }
```

#### Backend (consumidor)

```csharp
// DTOs/FolioDto.cs
public record FolioDto(string FolioNumber);

// CoreOhsClient.cs
return await GetDataAsync<FolioDto>("/v1/folios/next", ct);
// Deserializa doc.RootElement.GetProperty("data") con PropertyNameCaseInsensitive = true
```

#### Verificación campo a campo

| Campo mock (JSON) | Tipo mock | Campo backend (C#) | Tipo backend | Match |
|---|---|---|---|---|
| `folioNumber` | `string` | `FolioNumber` | `string` | ✅ (case-insensitive) |

**Errores del mock:** ninguno definido (el contador avanza siempre). El backend no maneja 404 en este endpoint; si core-ohs no responde lanza `CoreOhsUnavailableException` → HTTP 503. Alineado con HU-003-04. ✅

**Estado: ✅ OK**

---

### C-002 — GET /v1/subscribers

**Endpoint consumido por:** SPEC-004 `general-info-management`  
**Método backend:** `CoreOhsClient.GetSubscribersAsync()`

#### Mock (fuente de verdad)

```typescript
// routes/subscriberRoutes.ts
router.get('/', (_req, res) => {
  res.status(200).json({ data: subscribers });
});

// types/index.ts
interface Subscriber { code: string; name: string; office: string; active: boolean; }
```

Ejemplo fixture (`fixtures/subscribers.json`):
```json
[
  { "code": "SUB-001", "name": "María González López", "office": "CDMX Central", "active": true },
  { "code": "SUB-002", "name": "Carlos Ramírez Torres", "office": "Guadalajara Norte", "active": true },
  { "code": "SUB-003", "name": "Ana Martínez Ruiz",    "office": "Monterrey Sur",   "active": true }
]
```

Respuesta completa:
```json
{ "data": [ { "code": "SUB-001", "name": "...", "office": "...", "active": true }, ... ] }
```

#### Backend (consumidor)

```csharp
// DTOs/SubscriberDto.cs
public record SubscriberDto(string Code, string Name, string Office, bool Active);

// CoreOhsClient.cs
return await GetDataAsync<List<SubscriberDto>>("/v1/subscribers", ct);
```

#### Verificación campo a campo

| Campo mock (JSON) | Tipo mock | Campo backend (C#) | Tipo backend | Match |
|---|---|---|---|---|
| `code` | `string` | `Code` | `string` | ✅ |
| `name` | `string` | `Name` | `string` | ✅ |
| `office` | `string` | `Office` | `string` | ✅ |
| `active` | `boolean` | `Active` | `bool` | ✅ |

**Estado: ✅ OK**

---

### C-003 — GET /v1/agents?code=\<code\>

**Endpoint consumido por:** SPEC-004 `general-info-management`  
**Método backend:** `CoreOhsClient.GetAgentByCodeAsync(string code)`

#### Mock (fuente de verdad)

```typescript
// routes/agentRoutes.ts
router.get('/', (req, res) => {
  const { code } = req.query;
  if (code !== undefined) {
    const agent = agents.find(a => a.code === code);
    if (!agent) {
      res.status(404).json({ type: 'agentNotFound', message: 'Agente no encontrado' });
      return;
    }
    res.status(200).json({ data: agent });
    return;
  }
  res.status(200).json({ data: agents }); // lista completa si no hay ?code
});

// types/index.ts
interface Agent { code: string; name: string; region: string; active: boolean; }
```

Ejemplo fixture (`fixtures/agents.json`):
```json
[
  { "code": "AGT-001", "name": "Roberto Hernández", "region": "Centro",     "active": true },
  { "code": "AGT-002", "name": "Laura Sánchez",      "region": "Occidente", "active": true },
  { "code": "AGT-003", "name": "Pedro Díaz",          "region": "Norte",     "active": true }
]
```

Respuesta 200:
```json
{ "data": { "code": "AGT-001", "name": "Roberto Hernández", "region": "Centro", "active": true } }
```
Respuesta 404:
```json
{ "type": "agentNotFound", "message": "Agente no encontrado" }
```
_(Sin wrapper `data` — es un error.)_

#### Backend (consumidor)

```csharp
// DTOs/AgentDto.cs
public record AgentDto(string Code, string Name, string Region, bool Active);

// CoreOhsClient.cs
public async Task<AgentDto?> GetAgentByCodeAsync(string code, CancellationToken ct = default)
{
    try   { return await GetDataAsync<AgentDto>($"/v1/agents?code={Uri.EscapeDataString(code)}", ct); }
    catch (HttpRequestException) { return null; }
}
```

#### Verificación campo a campo (200)

| Campo mock (JSON) | Tipo mock | Campo backend (C#) | Tipo backend | Match |
|---|---|---|---|---|
| `code` | `string` | `Code` | `string` | ✅ |
| `name` | `string` | `Name` | `string` | ✅ |
| `region` | `string` | `Region` | `string` | ✅ |
| `active` | `boolean` | `Active` | `bool` | ✅ |

#### Manejo de errores

| Escenario | Mock responde | Backend maneja | Resultado para use case |
|---|---|---|---|
| Agente no encontrado | 404 `{ type: "agentNotFound" }` | `GetDataAsync` lanza `HttpRequestException`; `GetAgentByCodeAsync` la captura | Retorna `null` |
| core-ohs caído | (network error) | `GetDataAsync` lanza `CoreOhsUnavailableException` | Propaga la excepción → HTTP 503 |

El use case de SPEC-004 recibe `null` y debe lanzar su propia excepción de dominio → HTTP 422. Cadena correcta. ✅

**Estado: ✅ OK**

---

### C-004 — GET /v1/catalogs/risk-classification

**Endpoint consumido por:** SPEC-004 `general-info-management`  
**Método backend:** `CoreOhsClient.GetRiskClassificationsAsync()`

#### Mock (fuente de verdad)

```typescript
// routes/catalogRoutes.ts
router.get('/risk-classification', (_req, res) => {
  res.status(200).json({ data: riskClassifications });
});

// types/index.ts
interface RiskClassification { code: string; description: string; factor: number; }
```

Fixture (`fixtures/riskClassification.json`):
```json
[
  { "code": "standard",    "description": "Riesgo estándar",                         "factor": 1.00 },
  { "code": "preferred",   "description": "Riesgo preferente — perfil de riesgo bajo","factor": 0.85 },
  { "code": "substandard", "description": "Riesgo subestándar — perfil de riesgo alto","factor": 1.25 }
]
```

#### Backend (consumidor)

```csharp
// DTOs/RiskClassificationDto.cs
public record RiskClassificationDto(string Code, string Description, decimal Factor);

// CoreOhsClient.cs
return await GetDataAsync<List<RiskClassificationDto>>("/v1/catalogs/risk-classification", ct);
```

#### Verificación campo a campo

| Campo mock (JSON) | Tipo mock | Campo backend (C#) | Tipo backend | Match |
|---|---|---|---|---|
| `code` | `string` | `Code` | `string` | ✅ |
| `description` | `string` | `Description` | `string` | ✅ |
| `factor` | `number` (float) | `Factor` | `decimal` | ✅ (JSON float → C# decimal) |

**Estado: ✅ OK**

---

### C-005 — GET /v1/zip-codes/:zipCode

**Endpoint consumido por:** `gestion-ubicaciones` (SPEC futura)  
**Método backend:** `CoreOhsClient.GetZipCodeAsync(string zipCode)`

#### Verificación campo a campo

| Campo mock (ZipCodeData) | Tipo mock | Campo backend (ZipCodeDto) | Tipo backend | Match |
|---|---|---|---|---|
| `zipCode` | `string` | `ZipCode` | `string` | ✅ |
| `state` | `string` | `State` | `string` | ✅ |
| `municipality` | `string` | `Municipality` | `string` | ✅ |
| `neighborhood` | `string` | `Neighborhood` | `string` | ✅ |
| `city` | `string` | `City` | `string` | ✅ |
| `catZone` | `string` | `CatZone` | `string` | ✅ |
| `technicalLevel` | `number` (int) | `TechnicalLevel` | `int` | ✅ |

**Estado: ✅ OK**

---

### C-006 — /v1/zip-codes/validate ⚠️ CONTRACT_DRIFT

**Endpoint consumido por:** `gestion-ubicaciones` (SPEC futura)  
**Método backend:** `CoreOhsClient.ValidateZipCodeAsync(string zipCode)`

Ver **DRIFT-001** en la sección de drifts detectados.

---

### C-007 — GET /v1/catalogs/guarantees

**Verificación campo a campo** (Guarantee mock vs GuaranteeDto backend):

| Campo mock | Tipo mock | Campo backend | Tipo backend | Match |
|---|---|---|---|---|
| `key` | `string` | `Key` | `string` | ✅ |
| `name` | `string` | `Name` | `string` | ✅ |
| `description` | `string` | `Description` | `string` | ✅ |
| `category` | `string` | `Category` | `string` | ✅ |
| `requiresInsuredAmount` | `boolean` | `RequiresInsuredAmount` | `bool` | ✅ |

**Estado: ✅ OK**

---

### C-008 — GET /v1/tariffs/fire

| Campo mock (FireTariff) | Tipo mock | Campo backend (FireTariffDto) | Tipo backend | Match |
|---|---|---|---|---|
| `fireKey` | `string` | `FireKey` | `string` | ✅ |
| `baseRate` | `number` | `BaseRate` | `decimal` | ✅ |
| `description` | `string` | `Description` | `string` | ✅ |

**Estado: ✅ OK**

---

### C-009 — GET /v1/tariffs/cat

| Campo mock (CatTariff) | Tipo mock | Campo backend (CatTariffDto) | Tipo backend | Match |
|---|---|---|---|---|
| `zone` | `string` | `Zone` | `string` | ✅ |
| `tevFactor` | `number` | `TevFactor` | `decimal` | ✅ |
| `fhmFactor` | `number` | `FhmFactor` | `decimal` | ✅ |

**Estado: ✅ OK**

---

### C-010 — GET /v1/tariffs/fhm

| Campo mock (FhmTariff) | Tipo mock | Campo backend (FhmTariffDto) | Tipo backend | Match |
|---|---|---|---|---|
| `group` | `number` (int) | `Group` | `int` | ✅ |
| `zone` | `string` | `Zone` | `string` | ✅ |
| `condition` | `string` | `Condition` | `string` | ✅ |
| `rate` | `number` | `Rate` | `decimal` | ✅ |

**Estado: ✅ OK**

---

### C-011 — GET /v1/tariffs/electronic-equipment

| Campo mock (ElectronicEquipmentFactor) | Tipo mock | Campo backend (ElectronicEquipmentFactorDto) | Tipo backend | Match |
|---|---|---|---|---|
| `equipmentClass` | `string` | `EquipmentClass` | `string` | ✅ |
| `zoneLevel` | `number` (int) | `ZoneLevel` | `int` | ✅ |
| `factor` | `number` | `Factor` | `decimal` | ✅ |

**Estado: ✅ OK**

---

### C-012 — GET /v1/tariffs/calculation-parameters

| Campo mock (CalculationParameters) | Tipo mock | Campo backend (CalculationParametersDto) | Tipo backend | Match |
|---|---|---|---|---|
| `expeditionExpenses` | `number` | `ExpeditionExpenses` | `decimal` | ✅ |
| `agentCommission` | `number` | `AgentCommission` | `decimal` | ✅ |
| `issuingRights` | `number` | `IssuingRights` | `decimal` | ✅ |
| `iva` | `number` | `Iva` | `decimal` | ✅ |
| `surcharges` | `number` | `Surcharges` | `decimal` | ✅ |
| `effectiveDate` | `string` | `EffectiveDate` | `string` | ✅ |

**Estado: ✅ OK**

---

## Drifts detectados

### DRIFT-001 — Método HTTP incorrecto en `/v1/zip-codes/validate`

| Atributo | Valor |
|---|---|
| **ID** | DRIFT-001 |
| **Endpoint** | `/v1/zip-codes/validate` |
| **Severidad** | ⚠️ Medio |
| **Bloquea SPEC-003** | No |
| **Bloquea SPEC-004** | No |
| **Bloquea `gestion-ubicaciones`** | **Sí — bloqueante** |

#### Descripción del drift

El cliente HTTP del backend llama al endpoint con **GET** y el `zipCode` como query-string, pero el mock lo implementa como **POST** con el `zipCode` en el cuerpo del request.

**Backend** (`CoreOhsClient.cs` línea 57–59):
```csharp
public async Task<ZipCodeValidationDto> ValidateZipCodeAsync(string zipCode, CancellationToken ct = default)
{
    // ← GetDataAsync usa _httpClient.GetAsync(...)
    return await GetDataAsync<ZipCodeValidationDto>($"/v1/zip-codes/validate?zipCode={Uri.EscapeDataString(zipCode)}", ct);
}
```

**Mock** (`routes/zipCodeRoutes.ts` líneas 27–44):
```typescript
// POST /v1/zip-codes/validate
router.post('/validate', (req: Request, res: Response) => {
  const { zipCode } = req.body as { zipCode?: unknown }; // ← body, no query string
  ...
});
```

#### Impacto en tiempo de ejecución

Cuando el backend llama `GET /v1/zip-codes/validate?zipCode=06600`:

1. Express intentará hacer match en `zipCodeRoutes`
2. `POST /validate` no coincide con `GET`
3. El router de Express tiene también `GET /:zipCode` → captura el segmento `"validate"` como parámetro
4. Busca zipCode `"validate"` en el fixture → no encontrado → **404**
5. `GetDataAsync` lanza `HttpRequestException`
6. `ValidateZipCodeAsync` propaga la excepción → el use case falla

#### Acciones recomendadas

**Opción A (recomendada) — Corregir el cliente backend:**

```csharp
// CoreOhsClient.cs
public async Task<ZipCodeValidationDto> ValidateZipCodeAsync(string zipCode, CancellationToken ct = default)
{
    var requestBody = JsonContent.Create(new { zipCode });
    HttpResponseMessage response;
    try
    {
        response = await _httpClient.PostAsync("/v1/zip-codes/validate", requestBody, ct);
    }
    catch (Exception ex)
    {
        throw new CoreOhsUnavailableException("/v1/zip-codes/validate", ex);
    }
    if (!response.IsSuccessStatusCode)
        throw new HttpRequestException($"core-ohs returned {(int)response.StatusCode} for '/v1/zip-codes/validate'.");

    using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
    return doc.RootElement.GetProperty("data").Deserialize<ZipCodeValidationDto>(JsonOptions)
        ?? throw new InvalidOperationException("Unexpected null from /v1/zip-codes/validate.");
}
```

**Opción B — Cambiar el mock a GET con query-string:**

```typescript
// zipCodeRoutes.ts — antes del /:zipCode para no colisionar
router.get('/validate', (req: Request, res: Response) => {
  const zipCode = req.query.zipCode as string | undefined;
  ...
});
```

> **Responsable sugerido:** `backend-developer` (Opción A) — es el cambio de menor impacto y el mock ya tiene una sólida convención REST para recursos.  
> El mock usa POST porque es semánticamente una operación de validación sin persistencia, lo que es defendible. Cambiar el cliente es más seguro.

---

## Notas de diseño del cliente HTTP

### Deserialización case-insensitive

El cliente usa `JsonSerializerOptions { PropertyNameCaseInsensitive = true }`. Esto permite mapear automáticamente `camelCase` del JSON del mock a `PascalCase` de los records C#. No se detectaron conflictos de casing en ningún contrato. ✅

### Envelope `{ "data": ... }`

El mock envuelve todas las respuestas 2xx en `{ "data": <payload> }`. El cliente extrae esta propiedad via `doc.RootElement.GetProperty("data")` antes de deserializar. Patrón consistente en todos los endpoints. ✅

### Errores del mock (sin wrapper `data`)

Los errores (`404`, `400`) del mock retornan `{ "type": "...", "message": "..." }` **sin** wrapper `data`. El cliente detecta el status no-exitoso antes de intentar parsear → lanza `HttpRequestException`. Los métodos que deben diferenciar 404 de otros errores (ej. `GetAgentByCodeAsync`) capturan `HttpRequestException` apropiadamente. ✅

### Propagación de `CoreOhsUnavailableException`

Cuando hay error de red, `GetDataAsync` lanza `CoreOhsUnavailableException`. Todos los métodos del cliente excepto `GetAgentByCodeAsync` y `GetZipCodeAsync` propagan esta excepción hacia arriba. Los dos métodos que retornan `null` en caso de 4xx **no capturan** `CoreOhsUnavailableException`, por lo que la resiliencia de la HU-003-04 se mantiene correcta. ✅

---

## Conclusión para SPEC-003 y SPEC-004

| Feature | Endpoints verificados | Drifts bloqueantes | Puede implementarse |
|---|---|---|---|
| SPEC-003 `folio-creation` | 1 / 1 ✅ | 0 | **Sí** |
| SPEC-004 `general-info-management` | 3 / 3 ✅ | 0 | **Sí** |

> DRIFT-001 se registra para que `backend-developer` lo corrija antes de iniciar la implementación de la feature `gestion-ubicaciones`. No requiere acción bloqueante en las fases actuales.
