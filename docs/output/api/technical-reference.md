# Technical Reference — Cotizador de Seguros de Daños

## Table of Contents
1. [Data Model](#1-data-model)
2. [Core Reference Service API](#2-core-reference-service-api)
3. [Backend API Contract](#3-backend-api-contract)
4. [Repository Interface](#4-repository-interface)
5. [Architecture Decisions](#5-architecture-decisions)

---

## 1. Data Model

### 1.1 MongoDB Collection: `property_quotes`

**Schema (BSON camelCase):**

| Field | BSON Type | Description | Example |
|-------|-----------|-------------|---------|
| folioNumber | string | Unique PK, immutable. Format: `DAN-YYYY-NNNNN` | "DAN-2026-00001" |
| quoteStatus | string | "draft" \| "in_progress" \| "calculated" \| "finalized" | "draft" |
| insuredData | object | Insured party information | see below |
| insuredData.name | string | Legal name, max 200 chars | "Distribuidora del Norte S.A." |
| insuredData.taxId | string | RFC format | "DNO850101ABC" |
| insuredData.email | string? | Optional | "contact@company.mx" |
| insuredData.phone | string? | Optional | "+52 55 1234 5678" |
| conductionData | object | Underwriting conduction | see below |
| conductionData.subscriberCode | string | Format SUB-NNN | "SUB-001" |
| conductionData.officeName | string | Office name | "CDMX Central" |
| conductionData.branchOffice | string? | Optional | "Reforma 222" |
| agentCode | string | Format AGT-NNN | "AGT-001" |
| riskClassification | string | "standard" \| "preferred" \| "substandard" | "standard" |
| businessType | string | "commercial" \| "industrial" \| "residential" | "commercial" |
| layoutConfiguration | object | Placeholder — defined in SPEC-005 | {} |
| coverageOptions | object | Placeholder — defined in SPEC-007 | {} |
| locations | array | Embedded location documents | [] |
| netPremium | decimal | Total net premium | 15000.00 |
| commercialPremium | decimal | Total commercial premium | 18000.00 |
| premiumsByLocation | array | Premium breakdown per location | [] |
| version | int | Optimistic locking version, starts at 1 | 1 |
| metadata.createdAt | datetime | UTC, set once on creation | "2026-03-28T10:00:00Z" |
| metadata.updatedAt | datetime | UTC, updated on every write | "2026-03-28T10:00:00Z" |
| metadata.createdBy | string | Agent code who created | "AGT-001" |
| metadata.idempotencyKey | string | UUID for POST /v1/folios idempotency | "550e8400-..." |
| metadata.lastWizardStep | int | Last wizard step saved (0-4) | 0 |

**MongoDB Indexes:**

| Index Name | Fields | Options |
|------------|--------|---------|
| idx_folioNumber_unique | folioNumber | unique |
| idx_idempotencyKey_unique_sparse | metadata.idempotencyKey | unique, sparse |

---

### 1.2 Location Sub-document

| Field | Type | Description |
|-------|------|-------------|
| index | int | 1-based position |
| locationName | string | Human-readable name |
| address | string | Street address |
| zipCode | string | 5-digit code |
| state | string | State name |
| municipality | string | Municipality |
| neighborhood | string | Neighborhood |
| city | string | City |
| constructionType | string | e.g., "Type 1 - Solid" |
| level | int | Technical level 1–5 |
| constructionYear | int | Year built |
| businessLine.description | string | Business description |
| businessLine.fireKey | string | Fire tariff key, e.g., "B-03" |
| guarantees | string[] | Active guarantee keys |
| catZone | string | "A" \| "B" \| "C" |
| blockingAlerts | string[] | Reasons location is incomplete |
| validationStatus | string | "calculable" \| "incomplete" |

---

### 1.3 GuaranteeKeys Constants (14 total)

| Key | Category | Requires Insured Amount |
|-----|----------|------------------------|
| building_fire | fire | yes |
| contents_fire | fire | yes |
| coverage_extension | fire | yes |
| cat_tev | cat | yes |
| cat_fhm | cat | yes |
| debris_removal | additional | yes |
| extraordinary_expenses | additional | yes |
| rent_loss | additional | yes |
| business_interruption | additional | yes |
| electronic_equipment | special | yes |
| theft | special | yes |
| cash_and_securities | special | yes |
| glass | special | no |
| illuminated_signs | special | no |

---

### 1.4 QuoteStatus Constants

| Value | Description |
|-------|-------------|
| draft | Initial state on folio creation |
| in_progress | General info has been saved |
| calculated | Premium calculation has been run |
| finalized | Quote finalized (read-only) |

---

## 2. Core Reference Service API

**Base URL:** `http://localhost:3001`  
**Auth:** None  
**Headers:** `X-Correlation-Id: <uuid>` (echoed in response)  
**Response envelope:** `{ "data": ... }` for 2xx, `{ "type": string, "message": string }` for errors

| # | Method | Path | Description | Success | Error |
|---|--------|------|-------------|---------|-------|
| 1 | GET | /v1/subscribers | List all subscribers | 200 + Subscriber[] | — |
| 2 | GET | /v1/agents | List agents. `?code=AGT-NNN` to filter by code | 200 + Agent or Agent[] | 404 agentNotFound |
| 3 | GET | /v1/business-lines | List business lines with fireKey | 200 + BusinessLine[] | — |
| 4 | GET | /v1/zip-codes/{zipCode} | Get full zip code data | 200 + ZipCodeData | 404 zipCodeNotFound |
| 5 | POST | /v1/zip-codes/validate | Validate zip code existence | 200 + {valid, zipCode} | 400 validationError |
| 6 | GET | /v1/folios/next | Generate next sequential folio | 200 + {folioNumber} | — |
| 7 | GET | /v1/catalogs/risk-classification | List risk classifications | 200 + RiskClassification[] | — |
| 8 | GET | /v1/catalogs/guarantees | List 14 guarantee types | 200 + Guarantee[] | — |
| 9 | GET | /v1/tariffs/fire | Fire tariffs by fireKey | 200 + FireTariff[] | — |
| 10 | GET | /v1/tariffs/cat | CAT factors by zone | 200 + CatTariff[] | — |
| 11 | GET | /v1/tariffs/fhm | FHM tariffs by group/zone/condition | 200 + FhmTariff[] | — |
| 12 | GET | /v1/tariffs/electronic-equipment | Electronic equipment factors | 200 + ElectronicEquipmentFactor[] | — |
| 13 | GET | /v1/tariffs/calculation-parameters | Global calculation parameters | 200 + CalculationParameters | — |

---

## 3. Backend API Contract

### 3.1 Authentication

| Property | Value |
|----------|-------|
| Scheme | Basic Auth |
| Header | `Authorization: Basic <base64(username:password)>` |
| Config | `appsettings.Development.json` → `Auth.Username` / `Auth.Password` |

---

### 3.2 Middleware Pipeline (order is critical)

```
1. CorrelationIdMiddleware    — reads/generates X-Correlation-Id
2. ExceptionHandlingMiddleware — catches all exceptions, maps to HTTP
3. CORS
4. Authentication (Basic Auth)
5. Authorization ([Authorize] attribute)
6. Controllers
```

---

### 3.3 Error Response Mapping

| Exception | HTTP | type | Notes |
|-----------|------|------|-------|
| FolioNotFoundException | 404 | folioNotFound | includes folio in message |
| VersionConflictException | 409 | versionConflict | includes expected version |
| InvalidQuoteStateException | 422 | invalidQuoteState | operation not valid for current state |
| CoreOhsUnavailableException | 503 | coreOhsUnavailable | generic message — no internal path exposed |
| ValidationException (FluentValidation) | 400 | validationError | includes `field` name |
| Exception (unhandled) | 500 | internal | "Internal server error" — no details exposed |

**Error body format:**

```json
{ "type": "folioNotFound", "message": "Folio 'DAN-2026-00001' not found", "field": null }
```

---

## 4. Repository Interface

`IQuoteRepository` — `Cotizador.Application/Ports/IQuoteRepository.cs`

| Method | $set Fields | lastWizardStep | Throws |
|--------|-------------|----------------|--------|
| CreateAsync | — (InsertOne) | 0 | — |
| GetByFolioNumberAsync | — (Find) | — | — |
| GetByIdempotencyKeyAsync | — (Find) | — | — |
| UpdateGeneralInfoAsync | insuredData, conductionData, agentCode, businessType, riskClassification | 1 | VersionConflictException |
| UpdateLayoutAsync | layoutConfiguration | 2 | VersionConflictException |
| UpdateLocationsAsync | locations | 2 | VersionConflictException |
| PatchLocationAsync | locations.\<idx\> | 2 | VersionConflictException |
| UpdateCoverageOptionsAsync | coverageOptions | 3 | VersionConflictException |
| UpdateFinancialResultAsync | netPremium, commercialPremium, premiumsByLocation, quoteStatus | 4 | VersionConflictException |

**All update methods also set:** `version: expectedVersion + 1`, `metadata.updatedAt: DateTime.UtcNow`

**Optimistic locking:** filter is `{ folioNumber, version: expectedVersion }`. If `ModifiedCount == 0` → `VersionConflictException`.

---

## 5. Architecture Decisions

| ADR | Decision | Rationale |
|-----|----------|-----------|
| ADR-001 | Embedded documents in single MongoDB collection | Avoids joins; all quote data in one read; consistent with DDD aggregate root pattern |
| ADR-002 | Partial updates via UpdateOne + $set | Prevents section overwrite; each Use Case only touches its own fields |
| ADR-003 | Calculation engine as Application Use Case + Domain PremiumCalculator | Keeps business rules in Domain; Use Case orchestrates tariff data fetch + calculation |
| ADR-004 | Basic Auth with custom AuthenticationHandler | Simplest secure auth for a backend-to-backend system; credentials in config |
| ADR-005 | Wizard as Redux state machine (quoteWizardSlice) | Single source of truth for multi-step form; time-travel debugging; persistence to sessionStorage |
| ADR-006 | core-mock as standalone HTTP service (Node.js/Express) | Decoupled from .NET build; realistic HTTP round-trips; can be replaced by real service |
| ADR-007 | Wizard session — hybrid MongoDB + sessionStorage | MongoDB tracks last step for cross-device recovery; sessionStorage buffers unsaved form data |
