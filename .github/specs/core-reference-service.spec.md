---
id: SPEC-001
status: IMPLEMENTED
feature: core-reference-service
feature_type: backend-only
requires_design_spec: false
has_calculation_logic: false
affects_database: false
consumes_core_ohs: false
created: 2026-03-28
updated: 2026-03-29
author: spec-generator
version: "1.1"
related-specs: []
priority: alta
estimated-complexity: M
---

# Spec: Core Reference Service (Mock)

> **Estado:** `APPROVED` → approve with `status: APPROVED` before starting implementation.
> **Lifecycle:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implement the `cotizador-core-mock` service that simulates `plataforma-core-ohs`. This standalone Node.js + Express + TypeScript HTTP service exposes 13 REST endpoints serving reference data: subscriber catalogs, agents, business lines, zip codes, sequential folio generation, risk classification, guarantees, and all tariff/factor tables needed by the premium calculation engine. It is the master data provider for the entire system and has zero external dependencies.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-001-01**: As the quoter backend, I want to query the subscriber catalog so that I can assign an underwriter to a quote.

**Acceptance criteria (Gherkin):**

- **Given** the mock service is running and the subscribers fixture contains at least 3 records
  **When** `GET /v1/subscribers` is called
  **Then** the response status is 200 and the body contains `{ "data": [...] }` with all subscriber records, each having `code`, `name`, `office`, `active` fields

- **Given** the mock service is running
  **When** `GET /v1/subscribers` is called with header `X-Correlation-Id: <uuid>`
  **Then** the response includes the same `X-Correlation-Id` header value

---

**HU-001-02**: As the quoter backend, I want to query agents by code so that I can associate an agent to the quote.

**Acceptance criteria (Gherkin):**

- **Given** agent `AGT-001` exists in the fixture
  **When** `GET /v1/agents?code=AGT-001` is called
  **Then** the response status is 200 and `data` contains the matching agent record with `code`, `name`, `region`, `active`

- **Given** agent `AGT-999` does not exist in the fixture
  **When** `GET /v1/agents?code=AGT-999` is called
  **Then** the response status is 404 with body `{ "type": "agentNotFound", "message": "Agente no encontrado" }`

---

**HU-001-03**: As the quoter backend, I want to query business lines with their `fireKey` so that I can map a location's commercial activity to fire tariffs.

**Acceptance criteria (Gherkin):**

- **Given** the business lines fixture contains at least 5 records
  **When** `GET /v1/business-lines` is called
  **Then** the response status is 200 and `data` is an array where each record has `code`, `description`, `fireKey`, `riskLevel`

- **Given** `fireKey` values in business lines fixture are referenced in the fire tariffs fixture
  **When** the fixtures are loaded
  **Then** every `fireKey` in business lines exists as a key in fire tariffs (cross-referential consistency)

---

**HU-001-04**: As the quoter backend, I want to query and validate zip codes so that I can resolve the catastrophic zone and technical level for each location.

**Acceptance criteria (Gherkin):**

- **Given** zip code `06600` exists in the fixture
  **When** `GET /v1/zip-codes/06600` is called
  **Then** the response status is 200 and `data` contains `zipCode`, `state`, `municipality`, `neighborhood`, `city`, `catZone`, `technicalLevel`

- **Given** zip code `99999` does not exist in the fixture
  **When** `GET /v1/zip-codes/99999` is called
  **Then** the response status is 404 with body `{ "type": "zipCodeNotFound", "message": "Código postal no encontrado" }`

- **Given** zip code `06600` exists in the fixture
  **When** `POST /v1/zip-codes/validate` is called with body `{ "zipCode": "06600" }`
  **Then** the response status is 200 and `data` contains `{ "valid": true, "zipCode": "06600" }`

- **Given** zip code `99999` does not exist in the fixture
  **When** `POST /v1/zip-codes/validate` is called with body `{ "zipCode": "99999" }`
  **Then** the response status is 200 and `data` contains `{ "valid": false, "zipCode": "99999" }`

---

**HU-001-05**: As the quoter backend, I want to generate sequential folios in format `DAN-YYYY-NNNNN` so that each quote is uniquely identified.

**Acceptance criteria (Gherkin):**

- **Given** the mock service starts with the folio counter at 1
  **When** `GET /v1/folios/next` is called for the first time
  **Then** the response status is 200 and `data` contains `{ "folioNumber": "DAN-2026-00001" }`

- **Given** `DAN-2026-00001` was already generated
  **When** `GET /v1/folios/next` is called again
  **Then** the response status is 200 and `data` contains `{ "folioNumber": "DAN-2026-00002" }` (strictly sequential, no repeats)

- **Given** the environment variable `FOLIO_START` is set to `100`
  **When** the mock service starts and `GET /v1/folios/next` is called
  **Then** the first folio generated is `DAN-2026-00100`

---

**HU-001-06**: As the quoter backend, I want to query catalogs of risk classification and guarantees so that I can configure available coverages for a quote.

**Acceptance criteria (Gherkin):**

- **Given** the risk classification fixture contains at least 3 levels
  **When** `GET /v1/catalogs/risk-classification` is called
  **Then** the response status is 200 and `data` is an array with records having `code`, `description`, `factor`

- **Given** the guarantees fixture contains all 14 domain coverages
  **When** `GET /v1/catalogs/guarantees` is called
  **Then** the response status is 200 and `data` is an array of 14 records, each with `key`, `name`, `description`, `category`, `requiresInsuredAmount`

---

**HU-001-07**: As the quoter backend, I want to query fire tariffs, CAT factors (TEV/FHM), electronic equipment factors, and global calculation parameters so that the premium engine can execute.

**Acceptance criteria (Gherkin):**

- **Given** the fire tariffs fixture contains at least 5 records by `fireKey`
  **When** `GET /v1/tariffs/fire` is called
  **Then** the response status is 200 and `data` is an array with records having `fireKey`, `baseRate`, `description`

- **Given** the CAT tariffs fixture has zones A, B, C
  **When** `GET /v1/tariffs/cat` is called
  **Then** the response status is 200 and `data` is an array with records having `zone`, `tevFactor`, `fhmFactor`

- **Given** the FHM tariff fixture has at least 3 records
  **When** `GET /v1/tariffs/fhm` is called
  **Then** the response status is 200 and `data` is an array with records having `group`, `zone`, `condition`, `rate`

- **Given** the electronic equipment factors fixture has at least 3 entries
  **When** `GET /v1/tariffs/electronic-equipment` is called
  **Then** the response status is 200 and `data` is an array with records having `equipmentClass`, `zoneLevel`, `factor`

- **Given** the calculation parameters fixture exists
  **When** `GET /v1/tariffs/calculation-parameters` is called
  **Then** the response status is 200 and `data` is a single object with `expeditionExpenses`, `agentCommission`, `issuingRights`, `iva`, `surcharges`, `effectiveDate`

---

### 2.2 Reglas de negocio

**RN-001-01**: Folio numbers are generated sequentially with format `DAN-YYYY-NNNNN` where `YYYY` is the current year and `NNNNN` is a zero-padded counter.
Fuente: bussines-context.md §8 + architecture-decisions.md §Formato de `numeroFolio`
Impacto: backend

**RN-001-02**: Folio counter never repeats a value within a single service lifecycle (in-memory; resets on restart — acceptable per S-03).
Fuente: REQ-01 §Criterios de aceptación
Impacto: backend

**RN-001-03**: All fixture datasets must be cross-referentially consistent — every `fireKey` in business lines must exist in fire tariffs; every `catZone` in zip codes must exist in CAT tariffs.
Fuente: REQ-01 §Criterios de aceptación
Impacto: backend

**RN-001-04**: The guarantees catalog contains exactly 14 coverage types defined in the domain.
Fuente: bussines-context.md §5

Impacto: backend

### 2.3 Restricciones técnicas

- **Stack**: Node.js + Express + TypeScript (ADR-006)
- **Data source**: JSON fixture files read at startup into memory (S-03)
- **Port**: 3001 (configurable via `PORT` env var)
- **No authentication** required on mock endpoints
- **Header propagation**: all endpoints must read and echo `X-Correlation-Id` header (architecture-decisions.md §Headers requeridos en requests a cotizador-core-mock)
- **Response envelope**: all 2xx responses wrap payload in `{ "data": ... }` (architecture-decisions.md §Formato de respuesta exitosa)
- **Error format**: all error responses follow `{ "type": string, "message": string }` (architecture-decisions.md §Formato de respuesta de error)
- **Independent deployment**: the service starts with zero external dependencies

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         backend-only
requires_design_spec: false

Flujo de ejecución:
  Fase 0.5 (ux-designer):    NO APLICA
  Fase 1.5 (core-ohs):       APLICA — this IS the core-ohs agent's primary deliverable
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA
  Fase 2 backend-developer:  NO APLICA (mock is built by core-ohs agent, not backend-developer)
  Fase 2 frontend-developer: NO APLICA

Agente ejecutor: core-ohs (Fase 1.5)

Bloqueos de ejecución:
  - core-ohs agent can start immediately after spec.status == APPROVED
  - backend-developer (SPEC-002+) is UNBLOCKED once core-ohs mock is running
```

### 3.2 Design Spec

N/A — `requires_design_spec: false`.

### 3.3 Data types (TypeScript)

Since this is a Node.js mock (not the C# backend), data types are defined in TypeScript:

```typescript
// src/types/subscriber.ts
export interface Subscriber {
  code: string;       // Format: SUB-NNN
  name: string;       // Full name of subscriber
  office: string;     // Office location
  active: boolean;    // Whether subscriber is active
}

// src/types/agent.ts
export interface Agent {
  code: string;       // Format: AGT-NNN
  name: string;       // Full name of agent
  region: string;     // Geographic region
  active: boolean;    // Whether agent is active
}

// src/types/businessLine.ts
export interface BusinessLine {
  code: string;       // Format: BL-NNN
  description: string;// Human-readable description
  fireKey: string;    // Links to fire tariff (e.g., "B-03")
  riskLevel: string;  // "low" | "medium" | "high"
}

// src/types/zipCode.ts
export interface ZipCodeData {
  zipCode: string;        // 5-digit code
  state: string;          // State name
  municipality: string;   // Municipality name
  neighborhood: string;   // Neighborhood name
  city: string;           // City name
  catZone: string;        // Catastrophic zone ("A" | "B" | "C" ...)
  technicalLevel: number; // Technical level (1-5)
}

// src/types/catalog.ts
export interface RiskClassification {
  code: string;        // "standard" | "preferred" | "substandard"
  description: string; // Human-readable
  factor: number;      // Multiplier (e.g., 1.0, 0.85, 1.25)
}

export interface Guarantee {
  key: string;                  // Snake_case key (e.g., "building_fire")
  name: string;                 // Display name
  description: string;          // Full description
  category: string;             // "fire" | "cat" | "additional" | "special"
  requiresInsuredAmount: boolean;// Whether it needs a sum insured
}

// src/types/tariff.ts
export interface FireTariff {
  fireKey: string;    // Links to BusinessLine.fireKey
  baseRate: number;   // Rate as decimal (e.g., 0.00125)
  description: string;// Description of the business type
}

export interface CatTariff {
  zone: string;       // Catastrophic zone ("A" | "B" | "C")
  tevFactor: number;  // TEV factor as decimal
  fhmFactor: number;  // FHM factor as decimal
}

export interface FhmTariff {
  group: number;      // Group number (1, 2, 3...)
  zone: string;       // Zone code
  condition: string;  // "standard" | "reinforced"
  rate: number;       // Rate as decimal
}

export interface ElectronicEquipmentFactor {
  equipmentClass: string; // "A" | "B" | "C"
  zoneLevel: number;      // 1-5
  factor: number;         // Factor as decimal
}

// src/types/calculationParameters.ts
export interface CalculationParameters {
  expeditionExpenses: number;  // Fraction (e.g., 0.05 = 5%)
  agentCommission: number;     // Fraction (e.g., 0.10 = 10%)
  issuingRights: number;       // Fraction (e.g., 0.03 = 3%)
  iva: number;                 // VAT fraction (e.g., 0.16 = 16%)
  surcharges: number;          // Fraction (e.g., 0.02 = 2%)
  effectiveDate: string;       // ISO date string
}

// src/types/folio.ts
export interface FolioResponse {
  folioNumber: string; // Format: DAN-YYYY-NNNNN
}
```

### 3.4 Contratos API

All endpoints are served by the mock on `http://localhost:3001`.

---

#### GET /v1/subscribers

**Purpose**: Return the full list of subscribers (underwriters).
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "code": "SUB-001", "name": "María González López", "office": "CDMX Central", "active": true },
    { "code": "SUB-002", "name": "Carlos Ramírez Torres", "office": "Guadalajara Norte", "active": true },
    { "code": "SUB-003", "name": "Ana Martínez Ruiz", "office": "Monterrey Sur", "active": true }
  ]
}
```

---

#### GET /v1/agents

**Purpose**: Return agents list. Supports optional query param `code` for filtering.
**Auth**: None
**Query params**: `code` (optional) — filter by agent code

**Response 200** (no filter):
```json
{
  "data": [
    { "code": "AGT-001", "name": "Roberto Hernández", "region": "Centro", "active": true },
    { "code": "AGT-002", "name": "Laura Sánchez", "region": "Occidente", "active": true },
    { "code": "AGT-003", "name": "Pedro Díaz", "region": "Norte", "active": true }
  ]
}
```

**Response 200** (with `?code=AGT-001`):
```json
{
  "data": { "code": "AGT-001", "name": "Roberto Hernández", "region": "Centro", "active": true }
}
```

**Response 404** (with `?code=AGT-999`):
```json
{ "type": "agentNotFound", "message": "Agente no encontrado" }
```

---

#### GET /v1/business-lines

**Purpose**: Return business lines catalog with fire keys.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "code": "BL-001", "description": "Storage warehouse", "fireKey": "B-03", "riskLevel": "medium" },
    { "code": "BL-002", "description": "Retail store", "fireKey": "C-01", "riskLevel": "low" },
    { "code": "BL-003", "description": "Chemical plant", "fireKey": "A-07", "riskLevel": "high" },
    { "code": "BL-004", "description": "Office building", "fireKey": "D-02", "riskLevel": "low" },
    { "code": "BL-005", "description": "Restaurant", "fireKey": "E-04", "riskLevel": "medium" }
  ]
}
```

---

#### GET /v1/zip-codes/{zipCode}

**Purpose**: Return full data for a single zip code including catastrophic zone and technical level.
**Auth**: None
**Path params**: `zipCode` — 5-digit string

**Response 200**:
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

**Response 404**:
```json
{ "type": "zipCodeNotFound", "message": "Código postal no encontrado" }
```

---

#### POST /v1/zip-codes/validate

**Purpose**: Validate whether a zip code exists in the catalog without returning full data.
**Auth**: None

**Request body**:
```json
{ "zipCode": "06600" }
```

**Response 200** (exists):
```json
{ "data": { "valid": true, "zipCode": "06600" } }
```

**Response 200** (does not exist):
```json
{ "data": { "valid": false, "zipCode": "99999" } }
```

**Response 400** (missing field):
```json
{ "type": "validationError", "message": "Field 'zipCode' is required" }
```

---

#### GET /v1/folios/next

**Purpose**: Generate and return the next sequential folio number. Has side effect: increments internal counter.
**Auth**: None

**Response 200**:
```json
{ "data": { "folioNumber": "DAN-2026-00001" } }
```

---

#### GET /v1/catalogs/risk-classification

**Purpose**: Return risk classification levels catalog.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "code": "standard", "description": "Standard risk", "factor": 1.0 },
    { "code": "preferred", "description": "Preferred risk - lower risk profile", "factor": 0.85 },
    { "code": "substandard", "description": "Substandard risk - higher risk profile", "factor": 1.25 }
  ]
}
```

---

#### GET /v1/catalogs/guarantees

**Purpose**: Return the full catalog of 14 available guarantee types.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "key": "building_fire", "name": "Building Fire", "description": "Base coverage for building structure against fire", "category": "fire", "requiresInsuredAmount": true },
    { "key": "contents_fire", "name": "Contents Fire", "description": "Coverage for movable assets and inventory against fire", "category": "fire", "requiresInsuredAmount": true },
    { "key": "coverage_extension", "name": "Coverage Extension", "description": "Additional risks on top of fire (water damage, explosion, etc.)", "category": "fire", "requiresInsuredAmount": true },
    { "key": "cat_tev", "name": "CAT TEV", "description": "Catastrophe — Earthquake, Volcanic Eruption", "category": "cat", "requiresInsuredAmount": true },
    { "key": "cat_fhm", "name": "CAT FHM", "description": "Catastrophe — Hydrometeorological Phenomena (hurricane, flood)", "category": "cat", "requiresInsuredAmount": true },
    { "key": "debris_removal", "name": "Debris Removal", "description": "Post-disaster cleanup costs", "category": "additional", "requiresInsuredAmount": true },
    { "key": "extraordinary_expenses", "name": "Extraordinary Expenses", "description": "Additional disbursements derived from a claim", "category": "additional", "requiresInsuredAmount": true },
    { "key": "rent_loss", "name": "Rent Loss", "description": "Lost income due to property inhabitation", "category": "additional", "requiresInsuredAmount": true },
    { "key": "business_interruption", "name": "Business Interruption", "description": "Lost profits due to business operations halt", "category": "additional", "requiresInsuredAmount": true },
    { "key": "electronic_equipment", "name": "Electronic Equipment", "description": "All-risk coverage for electronic equipment", "category": "special", "requiresInsuredAmount": true },
    { "key": "theft", "name": "Theft", "description": "Theft with violence and/or assault", "category": "special", "requiresInsuredAmount": true },
    { "key": "cash_and_securities", "name": "Cash and Securities", "description": "Cash, checks, securities in safe or in transit", "category": "special", "requiresInsuredAmount": true },
    { "key": "glass", "name": "Glass", "description": "Accidental glass breakage", "category": "special", "requiresInsuredAmount": false },
    { "key": "illuminated_signs", "name": "Illuminated Signs", "description": "Damage to illuminated signage", "category": "special", "requiresInsuredAmount": false }
  ]
}
```

---

#### GET /v1/tariffs/fire

**Purpose**: Return fire tariffs by fire key.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "fireKey": "A-07", "baseRate": 0.00250, "description": "Chemical plant" },
    { "fireKey": "B-03", "baseRate": 0.00125, "description": "Storage warehouse" },
    { "fireKey": "C-01", "baseRate": 0.00080, "description": "Retail store" },
    { "fireKey": "D-02", "baseRate": 0.00060, "description": "Office building" },
    { "fireKey": "E-04", "baseRate": 0.00110, "description": "Restaurant" }
  ]
}
```

---

#### GET /v1/tariffs/cat

**Purpose**: Return CAT factors (TEV and FHM) by catastrophic zone.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "zone": "A", "tevFactor": 0.0035, "fhmFactor": 0.0028 },
    { "zone": "B", "tevFactor": 0.0022, "fhmFactor": 0.0018 },
    { "zone": "C", "tevFactor": 0.0015, "fhmFactor": 0.0012 }
  ]
}
```

---

#### GET /v1/tariffs/fhm

**Purpose**: Return FHM tariffs by group, zone, and condition.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "group": 1, "zone": "A", "condition": "standard", "rate": 0.0028 },
    { "group": 1, "zone": "B", "condition": "standard", "rate": 0.0018 },
    { "group": 1, "zone": "C", "condition": "standard", "rate": 0.0012 },
    { "group": 2, "zone": "A", "condition": "standard", "rate": 0.0032 },
    { "group": 2, "zone": "B", "condition": "reinforced", "rate": 0.0014 },
    { "group": 2, "zone": "C", "condition": "reinforced", "rate": 0.0009 }
  ]
}
```

---

#### GET /v1/tariffs/electronic-equipment

**Purpose**: Return electronic equipment factors by class and zone level.
**Auth**: None

**Response 200**:
```json
{
  "data": [
    { "equipmentClass": "A", "zoneLevel": 1, "factor": 0.0045 },
    { "equipmentClass": "A", "zoneLevel": 2, "factor": 0.0052 },
    { "equipmentClass": "B", "zoneLevel": 1, "factor": 0.0038 },
    { "equipmentClass": "B", "zoneLevel": 2, "factor": 0.0044 },
    { "equipmentClass": "C", "zoneLevel": 1, "factor": 0.0030 },
    { "equipmentClass": "C", "zoneLevel": 2, "factor": 0.0036 }
  ]
}
```

---

#### GET /v1/tariffs/calculation-parameters

**Purpose**: Return global calculation parameters for deriving commercial premium from net premium.
**Auth**: None

**Response 200**:
```json
{
  "data": {
    "expeditionExpenses": 0.05,
    "agentCommission": 0.10,
    "issuingRights": 0.03,
    "iva": 0.16,
    "surcharges": 0.02,
    "effectiveDate": "2026-01-01"
  }
}
```

---

### 3.5 Contratos core-ohs consumidos

N/A — this service IS core-ohs.

### 3.6 Estructura frontend

N/A — `feature_type: backend-only`.

### 3.7 Estado y queries

N/A — `feature_type: backend-only`.

### 3.8 Persistencia MongoDB

N/A — the mock uses in-memory fixtures loaded from JSON files. No MongoDB.

### 3.9 Project structure

```
cotizador-core-mock/
├── src/
│   ├── routes/
│   │   ├── subscriberRoutes.ts    # GET /v1/subscribers
│   │   ├── agentRoutes.ts         # GET /v1/agents
│   │   ├── businessLineRoutes.ts  # GET /v1/business-lines
│   │   ├── zipCodeRoutes.ts       # GET /v1/zip-codes/:zipCode + POST /v1/zip-codes/validate
│   │   ├── folioRoutes.ts         # GET /v1/folios
│   │   ├── catalogRoutes.ts       # GET /v1/catalogs/risk-classification + guarantees
│   │   └── tariffRoutes.ts        # GET /v1/tariffs/fire|cat|fhm|electronic-equipment|calculation-parameters
│   ├── fixtures/
│   │   ├── subscribers.json
│   │   ├── agents.json
│   │   ├── businessLines.json
│   │   ├── zipCodes.json
│   │   ├── riskClassification.json
│   │   ├── guarantees.json
│   │   ├── fireTariffs.json
│   │   ├── catTariffs.json
│   │   ├── fhmTariffs.json
│   │   ├── electronicEquipmentFactors.json
│   │   └── calculationParameters.json
│   ├── middleware/
│   │   └── correlationId.ts       # Reads/echoes X-Correlation-Id header
│   ├── types/
│   │   └── index.ts               # All TypeScript interfaces
│   └── index.ts                   # Express app entry point — port, middleware, route mount
├── package.json
├── tsconfig.json
└── README.md
```

### 3.10 Environment variables

| Variable | Default | Description |
|---|---|---|
| `PORT` | `3001` | HTTP port the mock listens on |
| `FOLIO_START` | `1` | Initial value for the folio counter |

---

## 4. LÓGICA DE CÁLCULO

N/A — `has_calculation_logic: false`.

---

## 5. MODELO DE DATOS

N/A — `affects_database: false`. Data is served from in-memory fixtures.

### 5.4 Seed data (fixtures)

The following fixtures are the minimum datasets. Each file contains the exact data from §3.4 response examples, expanded to meet the minimums specified in REQ-01:

| Fixture file | Min records | Cross-reference |
|---|---|---|
| `subscribers.json` | 3 | — |
| `agents.json` | 3 | — |
| `businessLines.json` | 5 | `fireKey` → `fireTariffs[].fireKey` |
| `zipCodes.json` | 10 | `catZone` → `catTariffs[].zone` |
| `riskClassification.json` | 3 | — |
| `guarantees.json` | 14 (exact) | `key` matches `GuaranteeKeys` constants in SPEC-002 |
| `fireTariffs.json` | 5 | `fireKey` ← `businessLines[].fireKey` |
| `catTariffs.json` | 3 | `zone` ← `zipCodes[].catZone` |
| `fhmTariffs.json` | 3+ | `zone` ← `catTariffs[].zone` |
| `electronicEquipmentFactors.json` | 3+ | — |
| `calculationParameters.json` | 1 (single object) | — |

**Consistency rule**: On startup, the service should log a warning if cross-reference integrity is violated (e.g., a `fireKey` in business lines not found in fire tariffs).

---

## 6. SUPUESTOS Y LIMITACIONES

**SUP-001-01**: The mock service uses Node.js + Express + TypeScript.
Razón: ADR-006 explicitly lists Express/Node as an option. Lightweight for a fixture-serving mock.
Riesgo si es incorrecto: Would need to rewrite in a different runtime.
Aprobado por: usuario

**SUP-001-02**: Folio counter is in-memory and resets on service restart.
Razón: S-03 in architecture-decisions.md — mock data does not persist between restarts.
Riesgo si es incorrecto: Tests that depend on a specific folio number sequence must restart the mock.
Aprobado por: usuario

**SUP-001-03**: No authentication on mock endpoints.
Razón: Mock is an internal development service; the main backend applies Basic Auth in its own layer.
Riesgo si es incorrecto: None — the mock never faces external traffic.
Aprobado por: usuario

**SUP-001-04**: The mock listens on port 3001 by default.
Razón: Avoids collision with webapp (3000) and .NET backend (5000+).
Riesgo si es incorrecto: Port is configurable via env var.
Aprobado por: usuario

**SUP-001-05**: The mock wraps all success responses in `{ "data": ... }` envelope.
Razón: architecture-decisions.md §Formato de respuesta exitosa applies to integration contracts.
Riesgo si es incorrecto: CoreOhsClient parser would need adjustment.
Aprobado por: usuario

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        └── [core-ohs]           (Fase 1.5 — implements mock service)
                │
                ├── [backend-developer]   (Fase 2 — consumes mock via ICoreOhsClient)
                └── [test-engineer-backend]  (Fase 3 — tests mock endpoints)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `core-ohs` | `spec-generator` | `specs/core-reference-service.spec.md` → `status: APPROVED` |
| `backend-developer` (downstream specs) | `core-ohs` | Mock service running with all 13 endpoints |
| `test-engineer-backend` | `core-ohs` | Mock service implementation complete |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-002 | quote-data-model | co-deployed (same wave, no dependency) |
| SPEC-003 | folio-creation | depends-on (consumes `GET /v1/folios`) |
| SPEC-004 | general-info-management | depends-on (consumes subscribers, agents) |
| SPEC-006 | location-management | depends-on (consumes zip codes, business lines) |
| SPEC-009 | premium-calculation-engine | depends-on (consumes all tariffs) |

---

## 8. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready)** — before starting implementation:
- [ ] Spec in state `APPROVED`
- [ ] All assumptions approved by user

**DoD (Definition of Done)** — to consider the feature complete:
- [ ] All 13 endpoints implemented by `core-ohs` agent and responding with correct data per §3.4
- [ ] All fixtures contain minimum record counts per §5.4
- [ ] Cross-referential consistency between fixtures validated (RN-001-03)
- [ ] Folio generation is sequential and non-repeating (RN-001-01, RN-001-02)
- [ ] `X-Correlation-Id` header propagated on all endpoints
- [ ] Response envelope `{ "data": ... }` on all 2xx responses
- [ ] Error format `{ "type", "message" }` on all error responses
- [ ] Service starts independently with `npm start` or equivalent
- [ ] Unit tests pass for fixture consistency
- [ ] Integration tests pass for all endpoints (200, 404, 400 scenarios)
- [ ] README with setup instructions

---

## 9. LISTA DE TAREAS

### 9.1 database-agent

N/A — no database in this feature.

### 9.2 core-ohs agent

#### Project setup
- [ ] Initialize Node.js project with `package.json` (Express, TypeScript, ts-node, @types/express)
- [ ] Create `tsconfig.json` with strict mode
- [ ] Create `src/index.ts` — Express app, port config, middleware registration, route mounting

#### Middleware
- [ ] Create `src/middleware/correlationId.ts` — read `X-Correlation-Id` from request, echo in response, generate UUID if missing

#### Types
- [ ] Create `src/types/index.ts` — all TypeScript interfaces per §3.3

#### Fixtures (11 files)
- [ ] Create `src/fixtures/subscribers.json` — 3+ records
- [ ] Create `src/fixtures/agents.json` — 3+ records
- [ ] Create `src/fixtures/businessLines.json` — 5+ records with `fireKey`
- [ ] Create `src/fixtures/zipCodes.json` — 10+ records with `catZone` and `technicalLevel`
- [ ] Create `src/fixtures/riskClassification.json` — 3 records
- [ ] Create `src/fixtures/guarantees.json` — exactly 14 records
- [ ] Create `src/fixtures/fireTariffs.json` — 5+ records matching business line fireKeys
- [ ] Create `src/fixtures/catTariffs.json` — 3 zones (A, B, C)
- [ ] Create `src/fixtures/fhmTariffs.json` — 3+ records
- [ ] Create `src/fixtures/electronicEquipmentFactors.json` — 3+ records
- [ ] Create `src/fixtures/calculationParameters.json` — 1 object

#### Routes (7 files)
- [ ] Create `src/routes/subscriberRoutes.ts` — `GET /v1/subscribers`
- [ ] Create `src/routes/agentRoutes.ts` — `GET /v1/agents` with optional `?code=` filter
- [ ] Create `src/routes/businessLineRoutes.ts` — `GET /v1/business-lines`
- [ ] Create `src/routes/zipCodeRoutes.ts` — `GET /v1/zip-codes/:zipCode` + `POST /v1/zip-codes/validate`
- [ ] Create `src/routes/folioRoutes.ts` — `GET /v1/folios/next` with in-memory sequential counter
- [ ] Create `src/routes/catalogRoutes.ts` — `GET /v1/catalogs/risk-classification` + `GET /v1/catalogs/guarantees`
- [ ] Create `src/routes/tariffRoutes.ts` — 5 sub-routes for fire, cat, fhm, electronic-equipment, calculation-parameters

#### Documentation
- [ ] Create `README.md` with setup, env vars, and endpoint reference

### 9.3 frontend-developer

N/A — backend-only.

### 9.4 test-engineer-backend

- [ ] Fixture consistency tests — validate cross-references between `businessLines[].fireKey` → `fireTariffs[].fireKey`
- [ ] Fixture consistency tests — validate `zipCodes[].catZone` → `catTariffs[].zone`
- [ ] Fixture consistency tests — validate guarantees count === 14
- [ ] Integration test: `GET /v1/subscribers` → 200, array with code/name/office/active
- [ ] Integration test: `GET /v1/agents?code=AGT-001` → 200, single agent
- [ ] Integration test: `GET /v1/agents?code=AGT-999` → 404
- [ ] Integration test: `GET /v1/business-lines` → 200, array with fireKey
- [ ] Integration test: `GET /v1/zip-codes/06600` → 200, full data
- [ ] Integration test: `GET /v1/zip-codes/99999` → 404
- [ ] Integration test: `POST /v1/zip-codes/validate` → 200, valid/invalid
- [ ] Integration test: `GET /v1/folios/next` → 200, sequential folio
- [ ] Integration test: `GET /v1/folios/next` called twice → second folio increments
- [ ] Integration test: `GET /v1/catalogs/risk-classification` → 200, 3+ records
- [ ] Integration test: `GET /v1/catalogs/guarantees` → 200, 14 records
- [ ] Integration test: `GET /v1/tariffs/fire` → 200, array with fireKey/baseRate
- [ ] Integration test: `GET /v1/tariffs/cat` → 200, zones A/B/C
- [ ] Integration test: `GET /v1/tariffs/fhm` → 200, array
- [ ] Integration test: `GET /v1/tariffs/electronic-equipment` → 200, array
- [ ] Integration test: `GET /v1/tariffs/calculation-parameters` → 200, single object
- [ ] Integration test: `X-Correlation-Id` header propagation on any endpoint

### 9.5 test-engineer-frontend

N/A — backend-only.

---

## AMENDMENT-001: Política de idioma (ADR-008)

**Fecha:** 2026-03-29
**Origen:** ADR-008 — Idioma del Frontend: código en inglés, UI en español
**Impacto:** Contratos API (mensajes de error) + Fixtures (datos visibles al usuario)

### A1.1 Principio general

ADR-008 aplica a **todos los componentes**, incluyendo `cotizador-core-mock`. La regla:

| Plano | Idioma | Aplica a en este spec |
|---|---|---|
| **Código fuente** | Inglés | Nombres de tipos, archivos, rutas, variables, interfaces, `key` fields, `type` en errores |
| **Contenido visible al usuario** | Español | Campo `message` en errores, campos `name`/`description` en fixtures de catálogos |

### A1.2 Correcciones en contratos API (§3.4) — campo `message` de errores

Todos los campos `message` en respuestas de error deben estar en español. El campo `type` permanece en inglés (es un identificador de código).

| Endpoint | `type` (inglés ✅) | `message` actual (inglés ❌) | `message` corregido (español ✅) |
|---|---|---|---|
| `GET /v1/agents?code=AGT-999` | `agentNotFound` | `"Agent not found"` | `"Agente no encontrado"` |
| `GET /v1/zip-codes/99999` | `zipCodeNotFound` | `"Zip code not found"` | `"Código postal no encontrado"` |
| `POST /v1/zip-codes/validate` (400) | `validationError` | `"Field 'zipCode' is required"` | `"El campo 'zipCode' es obligatorio"` |

### A1.3 Correcciones en fixtures — datos visibles al usuario

Los campos `name`, `description` y equivalentes que se muestran en la UI deben estar en español. Los campos que son identificadores de código (`key`, `fireKey`, `code`, `zone`, `condition`) permanecen en inglés.

#### `guarantees.json` — campos `name` y `description`

| `key` (inglés ✅) | `name` actual (inglés ❌) | `name` corregido (español ✅) | `description` corregido (español ✅) |
|---|---|---|---|
| `building_fire` | `Building Fire` | `Incendio Edificios` | `Cobertura base sobre la construcción contra incendio` |
| `contents_fire` | `Contents Fire` | `Incendio Contenidos` | `Cobertura sobre bienes muebles e inventarios contra incendio` |
| `coverage_extension` | `Coverage Extension` | `Extensión de Cobertura` | `Riesgos adicionales sobre incendio (daño por agua, explosión, etc.)` |
| `cat_tev` | `CAT TEV` | `CAT TEV` | `Catástrofe — Terremoto, Erupción Volcánica` |
| `cat_fhm` | `CAT FHM` | `CAT FHM` | `Catástrofe — Fenómenos Hidrometeorológicos (huracán, inundación)` |
| `debris_removal` | `Debris Removal` | `Remoción de Escombros` | `Costos de limpieza post-siniestro` |
| `extraordinary_expenses` | `Extraordinary Expenses` | `Gastos Extraordinarios` | `Erogaciones adicionales derivadas del siniestro` |
| `rent_loss` | `Rent Loss` | `Pérdida de Rentas` | `Lucro cesante por inhabilitación del inmueble` |
| `business_interruption` | `Business Interruption` | `Interrupción de Negocio` | `Pérdida de utilidades por interrupción del negocio` |
| `electronic_equipment` | `Electronic Equipment` | `Equipo Electrónico` | `Cobertura all-risk para equipos electrónicos` |
| `theft` | `Theft` | `Robo` | `Robo con violencia y/o asalto` |
| `cash_and_securities` | `Cash and Securities` | `Dinero y Valores` | `Efectivo, cheques, títulos en caja fuerte o en tránsito` |
| `glass` | `Glass` | `Vidrios` | `Rotura accidental de cristales` |
| `illuminated_signs` | `Illuminated Signs` | `Anuncios Luminosos` | `Daño a letreros y señalética iluminada` |

> **Nota:** Los textos en español provienen directamente de `bussines-context.md` §5 (Componentes técnicos de cobertura).

#### `businessLines.json` — campo `description`

| `code` (inglés ✅) | `description` actual (inglés ❌) | `description` corregido (español ✅) |
|---|---|---|
| `BL-001` | `Storage warehouse` | `Bodega de almacenamiento` |
| `BL-002` | `Retail store` | `Tienda de retail` |
| `BL-003` | `Chemical plant` | `Planta química` |
| `BL-004` | `Office building` | `Edificio de oficinas` |
| `BL-005` | `Restaurant` | `Restaurante` |

#### `riskClassification.json` — campo `description`

| `code` (inglés ✅) | `description` actual (inglés ❌) | `description` corregido (español ✅) |
|---|---|---|
| `standard` | `Standard risk` | `Riesgo estándar` |
| `preferred` | `Preferred risk - lower risk profile` | `Riesgo preferente — perfil de riesgo bajo` |
| `substandard` | `Substandard risk - higher risk profile` | `Riesgo subestándar — perfil de riesgo alto` |

#### `fireTariffs.json` — campo `description`

| `fireKey` (inglés ✅) | `description` actual (inglés ❌) | `description` corregido (español ✅) |
|---|---|---|
| `A-07` | `Chemical plant` | `Planta química` |
| `B-03` | `Storage warehouse` | `Bodega de almacenamiento` |
| `C-01` | `Retail store` | `Tienda de retail` |
| `D-02` | `Office building` | `Edificio de oficinas` |
| `E-04` | `Restaurant` | `Restaurante` |

#### Fixtures que NO requieren cambios

- `subscribers.json` — nombres ya están en español ✅
- `agents.json` — nombres ya están en español ✅
- `zipCodes.json` — nombres geográficos ya están en español ✅
- `catTariffs.json` — solo contiene códigos (`zone`) y valores numéricos ✅
- `fhmTariffs.json` — solo contiene códigos y valores numéricos. Campo `condition` es identificador de código ✅
- `electronicEquipmentFactors.json` — solo contiene códigos y valores numéricos ✅
- `calculationParameters.json` — solo contiene valores numéricos y una fecha ✅

### A1.4 Correcciones en Gherkin (§2.1)

Los criterios Gherkin en las HU que mencionan mensajes de error deben reflejar el texto en español:

| HU | Criterio afectado | Texto actual | Texto corregido |
|---|---|---|---|
| HU-001-02 | Edge case (AGT-999) | `"Agent not found"` | `"Agente no encontrado"` |
| HU-001-04 | Edge case (99999) | `"Zip code not found"` | `"Código postal no encontrado"` |

### A1.5 Lo que NO cambia (confirmación)

- Campo `type` en errores → inglés (`agentNotFound`, `zipCodeNotFound`, `validationError`) ✅
- Campo `key` en garantías → inglés (`building_fire`, `cat_tev`) ✅
- Campo `fireKey` → inglés (`B-03`, `A-07`) ✅
- Campo `code` en todos los catálogos → inglés (`AGT-001`, `SUB-001`, `BL-001`) ✅
- Campo `category` en garantías → inglés (`fire`, `cat`, `additional`, `special`) ✅
- Campo `riskLevel` en business lines → inglés (`low`, `medium`, `high`) ✅
- Campo `condition` en FHM tariffs → inglés (`standard`, `reinforced`) ✅
- Nombres de tipos TypeScript → inglés ✅
- Rutas de archivos → inglés ✅
- Rutas de API → inglés ✅

### A1.6 Tareas adicionales derivadas del amendment

- [x] Actualizar `src/fixtures/guarantees.json` — campos `name` y `description` a español
- [x] Actualizar `src/fixtures/businessLines.json` — campo `description` a español
- [x] Actualizar `src/fixtures/riskClassification.json` — campo `description` a español
- [x] Actualizar `src/fixtures/fireTariffs.json` — campo `description` a español
- [x] Actualizar mensajes de error 404/400 en todas las rutas a español
- [ ] Actualizar tests de integración que validan texto de `message` en errores
- [x] Actualizar criterios Gherkin de HU-001-02 y HU-001-04 con mensajes en español
