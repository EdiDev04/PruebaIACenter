---
id: SPEC-002
status: DRAFT
feature: quote-data-model
feature_type: backend-only
requires_design_spec: false
has_calculation_logic: false
affects_database: true
consumes_core_ohs: false
created: 2026-03-28
updated: 2026-03-28
author: spec-generator
version: "1.0"
related-specs: ["SPEC-001"]
priority: alta
estimated-complexity: L
---

# Spec: Quote Data Model and Persistence

> **Estado:** `DRAFT` → approve with `status: APPROVED` before starting implementation.
> **Lifecycle:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Design and implement the core domain model, persistence layer (MongoDB repository), and foundational infrastructure for the property damage insurance quoter. The `PropertyQuote` entity is the aggregate root — a single MongoDB document in the `property_quotes` collection that tracks the full lifecycle of a quote: insured data, conduction, locations, coverages, and financial results. The repository exposes section-specific partial update methods with optimistic locking via `version` field. This spec also establishes the Clean Architecture project scaffolding, exception handling middleware, and all shared infrastructure (MongoDB connectivity, Basic Auth, Serilog, CORS).

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-002-01**: As the system, I want to persist a quote as a single MongoDB document so that all folio information lives in one aggregate.

**Acceptance criteria (Gherkin):**

- **Given** a new quote with `folioNumber: "DAN-2026-00001"` is created
  **When** the quote is saved to MongoDB
  **Then** a document exists in `property_quotes` with `folioNumber: "DAN-2026-00001"`, `version: 1`, `quoteStatus: "draft"`, and `metadata.createdAt` set to the current UTC time

- **Given** a quote with `folioNumber: "DAN-2026-00001"` already exists
  **When** a second document with the same `folioNumber` is inserted
  **Then** the operation fails due to the unique index on `folioNumber`

---

**HU-002-02**: As the system, I want to update sections of the quote partially so that writing general info does not overwrite locations, and writing financial results does not overwrite coverage options.

**Acceptance criteria (Gherkin):**

- **Given** a quote exists with `folioNumber: "DAN-2026-00001"`, `version: 3`, `locations: [loc1, loc2]` and `insuredData.name: "Old Name"`
  **When** `UpdateGeneralInfoAsync` is called with `insuredData.name: "New Name"` and `version: 3`
  **Then** in MongoDB, `insuredData.name` is `"New Name"`, `version` is `4`, `metadata.updatedAt` is refreshed, and `locations` remains `[loc1, loc2]` unchanged

- **Given** a quote exists with general info already set and `locations: [loc1]`
  **When** `UpdateFinancialResultAsync` is called setting `netPremium`, `commercialPremium`, `premiumsByLocation`
  **Then** only those 3 financial fields and `quoteStatus`, `version`, `metadata.updatedAt` change; `insuredData`, `locations`, `coverageOptions` remain untouched

---

**HU-002-03**: As the system, I want to implement optimistic locking so that concurrent edits are detected and rejected with an appropriate error.

**Acceptance criteria (Gherkin):**

- **Given** a quote exists with `folioNumber: "DAN-2026-00001"` and `version: 5`
  **When** `UpdateGeneralInfoAsync` is called with `version: 5`
  **Then** the update succeeds, and `version` becomes `6`

- **Given** a quote exists with `folioNumber: "DAN-2026-00001"` and `version: 5`
  **When** `UpdateGeneralInfoAsync` is called with `version: 4` (stale)
  **Then** the operation throws `VersionConflictException`

---

**HU-002-04**: As the system, I want every write operation to increment the version and update `metadata.updatedAt` automatically.

**Acceptance criteria (Gherkin):**

- **Given** a quote exists with `version: N` and `metadata.updatedAt: T1`
  **When** any write operation completes successfully
  **Then** `version` becomes `N+1` and `metadata.updatedAt > T1`

- **Given** a write operation fails (version conflict, validation error)
  **When** the operation throws an exception
  **Then** `version` and `metadata.updatedAt` remain unchanged in MongoDB

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-002-01 | `folioNumber` is immutable after creation | Attempt to change `folioNumber` on an existing document | Operation rejected / field not included in any `$set` | REQ-02 + bussines-context.md §8 |
| RN-002-02 | Every write uses `version` as filter condition | `UpdateOne({ folioNumber, version: N }, $set: { ..., version: N+1 })` | If `ModifiedCount == 0` → `VersionConflictException` | architecture-decisions.md §Versionado optimista |
| RN-002-03 | Financial result write does not overwrite other sections | `POST .../calculate` only touches `netPremium`, `commercialPremium`, `premiumsByLocation`, `quoteStatus` | Other fields remain untouched | REQ-02 §Reglas de negocio |
| RN-002-04 | `metadata.updatedAt` auto-updates on every write | Any `$set` operation | `metadata.updatedAt` is set to `DateTime.UtcNow` | REQ-02 §Reglas de negocio |
| RN-002-05 | Partial updates only modify their section's fields | See §3.6 section mapping | Fields outside the section are not included in `$set` | ADR-002 |
| RN-002-06 | `metadata.lastWizardStep` auto-updates on every write | Each use case deduces its step number | See mapping in §3.6 | ADR-007 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `folioNumber` | Non-null, matches regex `^DAN-\d{4}-\d{5}$` | "Invalid folio number format" | Sí |
| `version` | Must be > 0 on updates, must match persisted version | "Version conflict" | Sí (409) |
| `quoteStatus` | Must be one of: `draft`, `in_progress`, `calculated`, `finalized` | "Invalid quote status" | Sí |
| `insuredData.name` | Non-empty string, max 200 chars (when provided) | "Insured name is required" | Sí |
| `insuredData.taxId` | RFC format (when provided) | "Invalid tax ID format" | Sí |
| `agentCode` | Non-empty string matching `^AGT-\d{3}$` (when provided) | "Invalid agent code" | Sí |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         backend-only
requires_design_spec: false

Flujo de ejecución:
  Fase 0.5 (ux-designer):    NO APLICA
  Fase 1.5 (core-ohs):       NO APLICA
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): APLICA (Domain model: entities, VOs, exceptions, constants,
                              IQuoteRepository interface, BSON convention, indexes, seed data)
  Fase 2 backend-developer:  APLICA (solution scaffolding, QuoteRepository impl, ICoreOhsClient +
                              CoreOhsClient, DTOs, middleware, auth, Program.cs)
  Fase 2 frontend-developer: NO APLICA

Bloqueos de ejecución:
  - database-agent can start immediately after spec.status == APPROVED
  - backend-developer is BLOCKED until database-agent completes Fase 1.5
    (needs Domain entities and IQuoteRepository interface to implement)
```

### 3.2 Design Spec

N/A — `requires_design_spec: false`.

### 3.3 Modelo de dominio

#### Domain Entities

```csharp
// Cotizador.Domain/Entities/PropertyQuote.cs
public class PropertyQuote
{
    public string FolioNumber { get; set; }            // PK, unique, format DAN-YYYY-NNNNN, immutable after creation
    public string QuoteStatus { get; set; }            // "draft" | "in_progress" | "calculated" | "finalized"
    public InsuredData InsuredData { get; set; }        // Insured party information
    public ConductionData ConductionData { get; set; } // Underwriting conduction info
    public string AgentCode { get; set; }               // Format AGT-NNN
    public string RiskClassification { get; set; }      // "standard" | "preferred" | "substandard"
    public string BusinessType { get; set; }            // "commercial" | "industrial" | "residential"
    public LayoutConfiguration LayoutConfiguration { get; set; }  // Location layout config (detailed in SPEC-005)
    public CoverageOptions CoverageOptions { get; set; }          // Coverage config (detailed in SPEC-007)
    public List<Location> Locations { get; set; }       // Embedded locations array
    public decimal NetPremium { get; set; }             // Total net premium (sum of location premiums)
    public decimal CommercialPremium { get; set; }      // Total commercial premium
    public List<LocationPremium> PremiumsByLocation { get; set; } // Premium breakdown per location
    public int Version { get; set; }                    // Optimistic locking — starts at 1
    public QuoteMetadata Metadata { get; set; }         // Audit metadata
}

// Cotizador.Domain/ValueObjects/InsuredData.cs
public class InsuredData
{
    public string Name { get; set; }     // Required, max 200 chars
    public string TaxId { get; set; }    // Required, RFC format
    public string Email { get; set; }    // Optional
    public string Phone { get; set; }    // Optional
}

// Cotizador.Domain/ValueObjects/ConductionData.cs
public class ConductionData
{
    public string SubscriberCode { get; set; }  // Required, format SUB-NNN
    public string OfficeName { get; set; }      // Required
    public string BranchOffice { get; set; }    // Optional
}

// Cotizador.Domain/ValueObjects/LayoutConfiguration.cs
public class LayoutConfiguration
{
    // Placeholder — detailed structure defined in SPEC-005 (location-layout-configuration)
    // Kept as an empty object for now to support partial updates from day one
}

// Cotizador.Domain/ValueObjects/CoverageOptions.cs
public class CoverageOptions
{
    // Placeholder — detailed structure defined in SPEC-007 (coverage-options-configuration)
    // Kept as an empty object for now to support partial updates from day one
}

// Cotizador.Domain/Entities/Location.cs
public class Location
{
    public int Index { get; set; }                // 1-based position in the array
    public string LocationName { get; set; }      // Human-readable name
    public string Address { get; set; }           // Street address
    public string ZipCode { get; set; }           // 5-digit code
    public string State { get; set; }             // State name
    public string Municipality { get; set; }      // Municipality name
    public string Neighborhood { get; set; }      // Neighborhood name
    public string City { get; set; }              // City name
    public string ConstructionType { get; set; }  // e.g., "Type 1 - Solid"
    public int Level { get; set; }                // Technical level (1-5)
    public int ConstructionYear { get; set; }     // Year built
    public BusinessLine BusinessLine { get; set; }// Business line with fireKey
    public List<string> Guarantees { get; set; }  // Active guarantee keys (from SPEC-001 catalog)
    public string CatZone { get; set; }           // Catastrophic zone resolved from zipCode
    public List<string> BlockingAlerts { get; set; }  // Reasons why location is incomplete
    public string ValidationStatus { get; set; }  // "calculable" | "incomplete"
}

// Cotizador.Domain/ValueObjects/BusinessLine.cs
public class BusinessLine
{
    public string Description { get; set; }  // Business activity description
    public string FireKey { get; set; }      // Maps to fire tariff key (e.g., "B-03")
}

// Cotizador.Domain/ValueObjects/LocationPremium.cs
public class LocationPremium
{
    public int LocationIndex { get; set; }           // References Location.Index
    public string LocationName { get; set; }         // Denormalized for display
    public decimal NetPremium { get; set; }           // Net premium for this location
    public string ValidationStatus { get; set; }     // "calculable" | "incomplete"
    public List<CoveragePremium> CoveragePremiums { get; set; } // Breakdown by guarantee
}

// Cotizador.Domain/ValueObjects/CoveragePremium.cs
public class CoveragePremium
{
    public string GuaranteeKey { get; set; }   // e.g., "building_fire"
    public decimal InsuredAmount { get; set; } // Sum insured for this coverage
    public decimal Rate { get; set; }          // Applied rate
    public decimal Premium { get; set; }       // Calculated premium = insuredAmount * rate
}

// Cotizador.Domain/ValueObjects/QuoteMetadata.cs
public class QuoteMetadata
{
    public DateTime CreatedAt { get; set; }      // UTC, set once on creation
    public DateTime UpdatedAt { get; set; }      // UTC, updated on every write
    public string CreatedBy { get; set; }        // Agent or user who created the folio
    public string IdempotencyKey { get; set; }   // UUID for POST /v1/folios idempotency
    public int LastWizardStep { get; set; }      // Last step saved by the wizard (0-4) — ADR-007
}
```

#### Domain Exceptions

```csharp
// Cotizador.Domain/Exceptions/FolioNotFoundException.cs
public class FolioNotFoundException : Exception
{
    public string FolioNumber { get; }
    public FolioNotFoundException(string folioNumber)
        : base($"Folio '{folioNumber}' not found") => FolioNumber = folioNumber;
}

// Cotizador.Domain/Exceptions/VersionConflictException.cs
public class VersionConflictException : Exception
{
    public string FolioNumber { get; }
    public int ExpectedVersion { get; }
    public VersionConflictException(string folioNumber, int expectedVersion)
        : base($"Version conflict on folio '{folioNumber}'. Expected version: {expectedVersion}")
    { FolioNumber = folioNumber; ExpectedVersion = expectedVersion; }
}

// Cotizador.Domain/Exceptions/InvalidQuoteStateException.cs
public class InvalidQuoteStateException : Exception
{
    public string FolioNumber { get; }
    public string CurrentState { get; }
    public InvalidQuoteStateException(string folioNumber, string currentState, string message)
        : base(message)
    { FolioNumber = folioNumber; CurrentState = currentState; }
}

// Cotizador.Domain/Exceptions/CoreOhsUnavailableException.cs
public class CoreOhsUnavailableException : Exception
{
    public CoreOhsUnavailableException(string message) : base(message) { }
    public CoreOhsUnavailableException(string message, Exception inner) : base(message, inner) { }
}
```

#### Domain Constants

```csharp
// Cotizador.Domain/Constants/QuoteStatus.cs
public static class QuoteStatus
{
    public const string Draft = "draft";
    public const string InProgress = "in_progress";
    public const string Calculated = "calculated";
    public const string Finalized = "finalized";
}

// Cotizador.Domain/Constants/ValidationStatus.cs
public static class ValidationStatus
{
    public const string Calculable = "calculable";
    public const string Incomplete = "incomplete";
}

// Cotizador.Domain/Constants/GuaranteeKeys.cs
public static class GuaranteeKeys
{
    public const string BuildingFire = "building_fire";
    public const string ContentsFire = "contents_fire";
    public const string CoverageExtension = "coverage_extension";
    public const string CatTev = "cat_tev";
    public const string CatFhm = "cat_fhm";
    public const string DebrisRemoval = "debris_removal";
    public const string ExtraordinaryExpenses = "extraordinary_expenses";
    public const string RentLoss = "rent_loss";
    public const string BusinessInterruption = "business_interruption";
    public const string ElectronicEquipment = "electronic_equipment";
    public const string Theft = "theft";
    public const string CashAndSecurities = "cash_and_securities";
    public const string Glass = "glass";
    public const string IlluminatedSigns = "illuminated_signs";

    public static readonly string[] All = new[]
    {
        BuildingFire, ContentsFire, CoverageExtension, CatTev, CatFhm,
        DebrisRemoval, ExtraordinaryExpenses, RentLoss, BusinessInterruption,
        ElectronicEquipment, Theft, CashAndSecurities, Glass, IlluminatedSigns
    };
}
```

### 3.4 Contratos API (backend)

This spec does NOT create endpoints. It creates the repository interface and implementation that downstream specs (SPEC-003 through SPEC-010) consume. However, the spec DOES create the shared infrastructure:

#### Exception Handling Middleware

```
Middleware: ExceptionHandlingMiddleware (registered in Program.cs pipeline)

Behavior:
  FolioNotFoundException        → 404 { "type": "folioNotFound", "message": "...", "field": null }
  VersionConflictException      → 409 { "type": "versionConflict", "message": "...", "field": null }
  InvalidQuoteStateException    → 422 { "type": "invalidQuoteState", "message": "...", "field": null }
  CoreOhsUnavailableException   → 503 { "type": "coreOhsUnavailable", "message": "...", "field": null }
  ValidationException (Fluent)  → 400 { "type": "validationError", "message": "...", "field": "fieldName" }
  Exception (unhandled)         → 500 { "type": "internal", "message": "Internal server error", "field": null }

NEVER expose stack traces or internal details in the response body.
Log the full exception with Serilog (Warning for 4xx, Error for 5xx).
```

#### Basic Auth Handler

```
Scheme: Basic Auth
Header: Authorization: Basic <base64(user:pass)>
Config: appsettings.json → "Auth": { "Username": "...", "Password": "..." }
Attribute: [Authorize] on all controllers
```

### 3.5 Contratos core-ohs consumidos

N/A — this spec does not consume core-ohs. It establishes the `ICoreOhsClient` interface for downstream specs.

#### ICoreOhsClient interface (for downstream specs)

```csharp
// Cotizador.Application/Ports/ICoreOhsClient.cs
public interface ICoreOhsClient
{
    Task<List<SubscriberDto>> GetSubscribersAsync(CancellationToken ct = default);
    Task<AgentDto?> GetAgentByCodeAsync(string code, CancellationToken ct = default);
    Task<List<BusinessLineDto>> GetBusinessLinesAsync(CancellationToken ct = default);
    Task<ZipCodeDto?> GetZipCodeAsync(string zipCode, CancellationToken ct = default);
    Task<ZipCodeValidationDto> ValidateZipCodeAsync(string zipCode, CancellationToken ct = default);
    Task<FolioDto> GenerateFolioAsync(CancellationToken ct = default);
    Task<List<RiskClassificationDto>> GetRiskClassificationsAsync(CancellationToken ct = default);
    Task<List<GuaranteeDto>> GetGuaranteesAsync(CancellationToken ct = default);
    Task<List<FireTariffDto>> GetFireTariffsAsync(CancellationToken ct = default);
    Task<List<CatTariffDto>> GetCatTariffsAsync(CancellationToken ct = default);
    Task<List<FhmTariffDto>> GetFhmTariffsAsync(CancellationToken ct = default);
    Task<List<ElectronicEquipmentFactorDto>> GetElectronicEquipmentFactorsAsync(CancellationToken ct = default);
    Task<CalculationParametersDto> GetCalculationParametersAsync(CancellationToken ct = default);
}
```

### 3.6 Repository interface and section mapping

```csharp
// Cotizador.Application/Ports/IQuoteRepository.cs
public interface IQuoteRepository
{
    /// <summary>Creates a new quote document. Fails if folioNumber already exists.</summary>
    Task CreateAsync(PropertyQuote quote, CancellationToken ct = default);

    /// <summary>Finds a quote by folioNumber. Returns null if not found.</summary>
    Task<PropertyQuote?> GetByFolioNumberAsync(string folioNumber, CancellationToken ct = default);

    /// <summary>Finds a quote by idempotencyKey. Returns null if not found.</summary>
    Task<PropertyQuote?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);

    /// <summary>Updates general info section. Throws VersionConflictException if version mismatch.</summary>
    /// <remarks>$set: insuredData, conductionData, agentCode, businessType, riskClassification, version+1, metadata.updatedAt, metadata.lastWizardStep=1</remarks>
    Task UpdateGeneralInfoAsync(string folioNumber, int expectedVersion, InsuredData insuredData, ConductionData conductionData, string agentCode, string businessType, string riskClassification, CancellationToken ct = default);

    /// <summary>Updates layout configuration section.</summary>
    /// <remarks>$set: layoutConfiguration, version+1, metadata.updatedAt, metadata.lastWizardStep=2</remarks>
    Task UpdateLayoutAsync(string folioNumber, int expectedVersion, LayoutConfiguration layout, CancellationToken ct = default);

    /// <summary>Replaces the entire locations array.</summary>
    /// <remarks>$set: locations, version+1, metadata.updatedAt, metadata.lastWizardStep=2</remarks>
    Task UpdateLocationsAsync(string folioNumber, int expectedVersion, List<Location> locations, CancellationToken ct = default);

    /// <summary>Updates a single location by index (PATCH semantics).</summary>
    /// <remarks>$set: locations.<index>.* (only provided fields), version+1, metadata.updatedAt, metadata.lastWizardStep=2</remarks>
    Task PatchLocationAsync(string folioNumber, int expectedVersion, int locationIndex, Location patchData, CancellationToken ct = default);

    /// <summary>Updates coverage options section.</summary>
    /// <remarks>$set: coverageOptions, version+1, metadata.updatedAt, metadata.lastWizardStep=3</remarks>
    Task UpdateCoverageOptionsAsync(string folioNumber, int expectedVersion, CoverageOptions options, CancellationToken ct = default);

    /// <summary>Updates financial result without touching other sections.</summary>
    /// <remarks>$set: netPremium, commercialPremium, premiumsByLocation, quoteStatus="calculated", version+1, metadata.updatedAt, metadata.lastWizardStep=4</remarks>
    Task UpdateFinancialResultAsync(string folioNumber, int expectedVersion, decimal netPremium, decimal commercialPremium, List<LocationPremium> premiumsByLocation, CancellationToken ct = default);
}
```

**Section → Fields mapping (for $set operations):**

| Section | Method | Fields in $set | lastWizardStep |
|---|---|---|---|
| General info | `UpdateGeneralInfoAsync` | `insuredData`, `conductionData`, `agentCode`, `businessType`, `riskClassification` | 1 |
| Layout | `UpdateLayoutAsync` | `layoutConfiguration` | 2 |
| Locations (full) | `UpdateLocationsAsync` | `locations` | 2 |
| Location (patch) | `PatchLocationAsync` | `locations.<idx>.*` | 2 |
| Coverage options | `UpdateCoverageOptionsAsync` | `coverageOptions` | 3 |
| Financial result | `UpdateFinancialResultAsync` | `netPremium`, `commercialPremium`, `premiumsByLocation`, `quoteStatus` | 4 |

**All methods additionally set**: `version: expectedVersion + 1`, `metadata.updatedAt: DateTime.UtcNow`, `metadata.lastWizardStep: <N>`.

**All update methods filter by**: `{ folioNumber, version: expectedVersion }`. If `ModifiedCount == 0` → throw `VersionConflictException`.

### 3.7 Estado y queries

N/A — backend-only, no frontend state.

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Create | `property_quotes` | `InsertOne` | — | — | `folioNumber_1` (unique) |
| Read by folio | `property_quotes` | `Find` | `{ folioNumber }` | Full document | `folioNumber_1` |
| Read by idempotency | `property_quotes` | `Find` | `{ "metadata.idempotencyKey" }` | Full document | `metadata.idempotencyKey_1` (unique sparse) |
| Update general info | `property_quotes` | `UpdateOne` | `{ folioNumber, version }` | `$set` per section | `folioNumber_1` |
| Update layout | `property_quotes` | `UpdateOne` | `{ folioNumber, version }` | `$set` per section | `folioNumber_1` |
| Update locations | `property_quotes` | `UpdateOne` | `{ folioNumber, version }` | `$set` per section | `folioNumber_1` |
| Patch location | `property_quotes` | `UpdateOne` | `{ folioNumber, version }` | `$set` per field | `folioNumber_1` |
| Update coverage | `property_quotes` | `UpdateOne` | `{ folioNumber, version }` | `$set` per section | `folioNumber_1` |
| Update financial | `property_quotes` | `UpdateOne` | `{ folioNumber, version }` | `$set` per section | `folioNumber_1` |

**Versionado optimista**: Every `UpdateOne` uses the compound filter `{ folioNumber: X, version: N }` and includes `$set: { version: N+1 }`. If `UpdateResult.ModifiedCount == 0` → `throw new VersionConflictException(folioNumber, N)`.

**All updates are partial** (`$set` on specific fields). Never `ReplaceOne`.

### 3.9 Backend project structure

```
cotizador-backend/
├── src/
│   ├── Cotizador.Domain/
│   │   ├── Entities/
│   │   │   ├── PropertyQuote.cs
│   │   │   └── Location.cs
│   │   ├── ValueObjects/
│   │   │   ├── InsuredData.cs
│   │   │   ├── ConductionData.cs
│   │   │   ├── LayoutConfiguration.cs
│   │   │   ├── CoverageOptions.cs
│   │   │   ├── BusinessLine.cs
│   │   │   ├── LocationPremium.cs
│   │   │   ├── CoveragePremium.cs
│   │   │   └── QuoteMetadata.cs
│   │   ├── Constants/
│   │   │   ├── QuoteStatus.cs
│   │   │   ├── ValidationStatus.cs
│   │   │   └── GuaranteeKeys.cs
│   │   └── Exceptions/
│   │       ├── FolioNotFoundException.cs
│   │       ├── VersionConflictException.cs
│   │       ├── InvalidQuoteStateException.cs
│   │       └── CoreOhsUnavailableException.cs
│   ├── Cotizador.Application/
│   │   ├── Ports/
│   │   │   ├── IQuoteRepository.cs
│   │   │   └── ICoreOhsClient.cs
│   │   ├── DTOs/
│   │   │   ├── SubscriberDto.cs
│   │   │   ├── AgentDto.cs
│   │   │   ├── BusinessLineDto.cs
│   │   │   ├── ZipCodeDto.cs
│   │   │   ├── ZipCodeValidationDto.cs
│   │   │   ├── FolioDto.cs
│   │   │   ├── RiskClassificationDto.cs
│   │   │   ├── GuaranteeDto.cs
│   │   │   ├── FireTariffDto.cs
│   │   │   ├── CatTariffDto.cs
│   │   │   ├── FhmTariffDto.cs
│   │   │   ├── ElectronicEquipmentFactorDto.cs
│   │   │   └── CalculationParametersDto.cs
│   │   └── UseCases/
│   │       └── (created by downstream specs)
│   ├── Cotizador.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── QuoteRepository.cs          # Implements IQuoteRepository
│   │   │   ├── MongoDbSettings.cs          # Connection settings
│   │   │   └── ServiceCollectionExtensions.cs  # AddPersistence(IServiceCollection)
│   │   ├── ExternalServices/
│   │   │   ├── CoreOhsClient.cs            # Implements ICoreOhsClient
│   │   │   └── ServiceCollectionExtensions.cs  # AddExternalServices(IServiceCollection)
│   │   └── ServiceCollectionExtensions.cs  # AddInfrastructure(IServiceCollection) — aggregates both
│   ├── Cotizador.API/
│   │   ├── Middleware/
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── CorrelationIdMiddleware.cs
│   │   ├── Auth/
│   │   │   └── BasicAuthHandler.cs
│   │   ├── Controllers/
│   │   │   └── (created by downstream specs)
│   │   └── Program.cs                     # Composition Root
│   └── Cotizador.Tests/
│       ├── Domain/
│       │   └── (entity tests)
│       ├── Application/
│       │   └── (use case tests)
│       └── Infrastructure/
│           └── (repository tests)
├── Cotizador.sln
└── README.md
```

**Key decisions for project scaffolding:**

- `Infrastructure` exposes `AddInfrastructure(IServiceCollection)` method invoked from `Program.cs`. This way `API` registers Infrastructure implementations without directly referencing Infrastructure types in Controllers.
- `Program.cs` is the Composition Root: MongoDB client (singleton), repositories (scoped), use cases (scoped), HttpClient for core-ohs (typed).
- Middleware pipeline order: `CorrelationIdMiddleware` → `ExceptionHandlingMiddleware` → Authentication → Routing → Controllers.

### 3.10 MongoDB field mapping (BSON)

The MongoDB BSON document uses `camelCase` field names. The C# classes use `PascalCase` properties. Mapping is handled via `BsonClassMap` or `ConventionPack` with `CamelCaseElementNameConvention`.

```csharp
// In Infrastructure/Persistence/MongoDbSettings.cs or ServiceCollectionExtensions.cs
var conventionPack = new ConventionPack
{
    new CamelCaseElementNameConvention()
};
ConventionRegistry.Register("camelCase", conventionPack, _ => true);
```

**Resulting BSON field names:**
```json
{
  "folioNumber": "DAN-2026-00001",
  "quoteStatus": "draft",
  "insuredData": { "name": "...", "taxId": "...", "email": "...", "phone": "..." },
  "conductionData": { "subscriberCode": "...", "officeName": "...", "branchOffice": "..." },
  "agentCode": "AGT-001",
  "riskClassification": "standard",
  "businessType": "commercial",
  "layoutConfiguration": {},
  "coverageOptions": {},
  "locations": [],
  "netPremium": 0.0,
  "commercialPremium": 0.0,
  "premiumsByLocation": [],
  "version": 1,
  "metadata": {
    "createdAt": "2026-03-28T10:00:00Z",
    "updatedAt": "2026-03-28T10:00:00Z",
    "createdBy": "AGT-001",
    "idempotencyKey": "550e8400-e29b-41d4-a716-446655440000",
    "lastWizardStep": 0
  }
}
```

---

## 4. LÓGICA DE CÁLCULO

N/A — `has_calculation_logic: false`.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos |
|---|---|---|
| `property_quotes` | create / read / update (partial) | See full model in §3.3 and §3.10 |

### 5.2 Cambios de esquema

New collection — no changes to existing schema.

### 5.3 Índices requeridos

```javascript
// Unique index on folioNumber — primary lookup key
db.property_quotes.createIndex(
  { folioNumber: 1 },
  { unique: true, name: "idx_folioNumber_unique" }
)

// Unique sparse index on idempotencyKey — for POST /v1/folios idempotency
db.property_quotes.createIndex(
  { "metadata.idempotencyKey": 1 },
  { unique: true, sparse: true, name: "idx_idempotencyKey_unique_sparse" }
)
```

### 5.4 Datos semilla

A sample document fixture for testing:

```json
{
  "folioNumber": "DAN-2026-00001",
  "quoteStatus": "in_progress",
  "insuredData": {
    "name": "Distribuidora del Norte S.A. de C.V.",
    "taxId": "DNO850101ABC",
    "email": "contacto@distnorte.mx",
    "phone": "+52 55 1234 5678"
  },
  "conductionData": {
    "subscriberCode": "SUB-001",
    "officeName": "CDMX Central",
    "branchOffice": "Reforma 222"
  },
  "agentCode": "AGT-001",
  "riskClassification": "standard",
  "businessType": "commercial",
  "layoutConfiguration": {},
  "coverageOptions": {},
  "locations": [
    {
      "index": 1,
      "locationName": "Central Warehouse CDMX",
      "address": "Av. Industria 340",
      "zipCode": "06600",
      "state": "Ciudad de México",
      "municipality": "Cuauhtémoc",
      "neighborhood": "Doctores",
      "city": "Ciudad de México",
      "constructionType": "Type 1 - Solid",
      "level": 2,
      "constructionYear": 1998,
      "businessLine": {
        "description": "Storage warehouse",
        "fireKey": "B-03"
      },
      "guarantees": ["building_fire", "contents_fire", "cat_tev", "theft"],
      "catZone": "A",
      "blockingAlerts": [],
      "validationStatus": "calculable"
    }
  ],
  "netPremium": 0.0,
  "commercialPremium": 0.0,
  "premiumsByLocation": [],
  "version": 1,
  "metadata": {
    "createdAt": "2026-03-28T10:00:00Z",
    "updatedAt": "2026-03-28T10:00:00Z",
    "createdBy": "AGT-001",
    "idempotencyKey": "550e8400-e29b-41d4-a716-446655440000",
    "lastWizardStep": 0
  }
}
```

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto |
|---|---|---|---|
| SUP-002-01 | `insuredData` fields: `name` (required), `taxId` (required), `email` (optional), `phone` (optional) | Minimum fields to identify the insured party. Business context only mentions `nombre` and `rfc`. Added email/phone as optional since they are common in insurance forms. | If more fields are needed, add them via spec amendment |
| SUP-002-02 | `conductionData` fields: `subscriberCode` (required), `officeName` (required), `branchOffice` (optional) | Minimum to trace policy underwriting conduction. Business context mentions `suscriptor` and `oficina`. | If more fields are needed, add them via spec amendment |
| SUP-002-03 | `layoutConfiguration` and `coverageOptions` are placeholder objects in SPEC-002. Their internal structure is defined by SPEC-005 and SPEC-007 respectively | Avoids blocking SPEC-002 by downstream features. The partial update mechanism works regardless of internal structure | If fields are needed sooner, SPEC-005/SPEC-007 must be generated first |
| SUP-002-04 | Collection name is `property_quotes` (English) as defined in REQ-02 | User confirmed all code in English. Architecture-decisions uses `cotizaciones_danos` but that's conceptual Spanish naming | If `cotizaciones_danos` is required for MongoDB collection, rename in repository. No structural impact |
| SUP-002-05 | Guarantee keys are English snake_case (e.g., `building_fire` not `incendio_edificios`) | User confirmed all code in English. Keys match SPEC-001 catalog | All systems must use these keys consistently |
| SUP-002-06 | BSON fields use camelCase via MongoDB ConventionPack | Standard MongoDB convention. Aligns with REQ-02 field names | If a different convention is needed, change the ConventionPack configuration |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        └── [database-agent]     (Fase 1.5 — Domain model + IQuoteRepository + indexes + BSON + seeds)
                │
                └── [backend-developer]  (Fase 2 — BLOCKED until database-agent completes)
                        │                  solution scaffolding, QuoteRepository impl,
                        │                  ICoreOhsClient + CoreOhsClient, DTOs,
                        │                  middleware, auth, Program.cs
                        │
                        └── [test-engineer-backend]  (Fase 3 — unit + integration tests)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `database-agent` | `spec-generator` | `specs/quote-data-model.spec.md` → `status: APPROVED` |
| `backend-developer` | `database-agent` | Domain entities, VOs, exceptions, constants, `IQuoteRepository` interface created in `Cotizador.Domain/` and `Cotizador.Application/Ports/` |
| `test-engineer-backend` | `backend-developer` | Repository impl, middleware, and infrastructure complete |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-001 | core-reference-service | co-deployed (same wave; GuaranteeKeys must align with SPEC-001 catalog) |
| SPEC-003 | folio-creation | extends (creates first document using `IQuoteRepository.CreateAsync`) |
| SPEC-004 | general-info-management | extends (uses `UpdateGeneralInfoAsync`) |
| SPEC-005 | location-layout-configuration | extends (`LayoutConfiguration` internal structure) |
| SPEC-006 | location-management | extends (uses `UpdateLocationsAsync`, `PatchLocationAsync`) |
| SPEC-007 | coverage-options-configuration | extends (`CoverageOptions` internal structure) |
| SPEC-008 | quote-state-progress | depends-on (reads from `property_quotes`) |
| SPEC-009 | premium-calculation-engine | extends (uses `UpdateFinancialResultAsync`) |
| SPEC-010 | results-display | depends-on (reads financial results) |

---

## 8. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready)** — before starting implementation:
- [ ] Spec in state `APPROVED`
- [ ] All assumptions approved by user
- [ ] SPEC-001 approved (for `GuaranteeKeys` alignment)

**DoD (Definition of Done)** — to consider the feature complete:
- [ ] .NET solution (`Cotizador.sln`) created with all 5 projects and correct references
- [ ] All domain entities and value objects implemented per §3.3
- [ ] All domain exceptions implemented per §3.3
- [ ] All domain constants implemented per §3.3
- [ ] `IQuoteRepository` interface implemented per §3.6
- [ ] `QuoteRepository` implementation with MongoDB.Driver per §3.8
- [ ] All partial update methods use `$set` and never `ReplaceOne` (RN-002-05)
- [ ] All updates filter by `{ folioNumber, version }` and throw `VersionConflictException` on `ModifiedCount == 0` (RN-002-02)
- [ ] `metadata.updatedAt` and `metadata.lastWizardStep` updated in every write (RN-002-04, RN-002-06)
- [ ] `ICoreOhsClient` interface defined per §3.5
- [ ] `ExceptionHandlingMiddleware` implemented per §3.4 and architecture-decisions.md
- [ ] `CorrelationIdMiddleware` implemented (reads/generates `X-Correlation-Id`)
- [ ] `BasicAuthHandler` implemented per ADR-004
- [ ] `Program.cs` wires all dependencies via `AddInfrastructure()` extension methods
- [ ] MongoDB indexes created per §5.3
- [ ] BSON camelCase convention applied per §3.10
- [ ] Unit tests for domain entities (PropertyQuote, Location)
- [ ] Unit tests for VersionConflictException scenario
- [ ] Integration tests for QuoteRepository (create, read, partial update, version conflict)
- [ ] No Clean Architecture violations (`API → Application → Domain ← Infrastructure`)

---

## 9. LISTA DE TAREAS

### 9.1 database-agent (Fase 1.5 — executes FIRST, unblocks backend-developer)

#### Domain — Entities
- [ ] Create `Cotizador.Domain/Entities/PropertyQuote.cs` per §3.3
- [ ] Create `Cotizador.Domain/Entities/Location.cs` per §3.3

#### Domain — Value Objects
- [ ] Create `Cotizador.Domain/ValueObjects/InsuredData.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/ConductionData.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/LayoutConfiguration.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/CoverageOptions.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/BusinessLine.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/LocationPremium.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/CoveragePremium.cs`
- [ ] Create `Cotizador.Domain/ValueObjects/QuoteMetadata.cs`

#### Domain — Constants
- [ ] Create `Cotizador.Domain/Constants/QuoteStatus.cs`
- [ ] Create `Cotizador.Domain/Constants/ValidationStatus.cs`
- [ ] Create `Cotizador.Domain/Constants/GuaranteeKeys.cs`

#### Domain — Exceptions
- [ ] Create `Cotizador.Domain/Exceptions/FolioNotFoundException.cs`
- [ ] Create `Cotizador.Domain/Exceptions/VersionConflictException.cs`
- [ ] Create `Cotizador.Domain/Exceptions/InvalidQuoteStateException.cs`
- [ ] Create `Cotizador.Domain/Exceptions/CoreOhsUnavailableException.cs`

#### Application — Ports (interfaces only, implementations go to backend-developer)
- [ ] Create `Cotizador.Application/Ports/IQuoteRepository.cs` per §3.6

#### MongoDB — BSON Convention
- [ ] Apply `CamelCaseElementNameConvention` in `ConventionPack` registration per §3.10

#### MongoDB — Indexes
- [ ] Create MongoDB index `idx_folioNumber_unique` on `property_quotes.folioNumber` (unique) per §5.3
- [ ] Create MongoDB index `idx_idempotencyKey_unique_sparse` on `property_quotes.metadata.idempotencyKey` (unique, sparse) per §5.3

#### Seed Data
- [ ] Insert seed document from §5.4 for testing
- [ ] Validate BSON document matches schema in §3.10

### 9.2 backend-developer (Fase 2 — BLOCKED until database-agent completes §9.1)

#### Solution scaffolding
- [ ] Create `Cotizador.sln` with 5 projects: `Cotizador.Domain`, `Cotizador.Application`, `Cotizador.Infrastructure`, `Cotizador.API`, `Cotizador.Tests`
- [ ] Set project references per Clean Architecture rules (§3.1)
- [ ] Add NuGet packages: `MongoDB.Driver`, `FluentValidation`, `Serilog`, `Serilog.Sinks.Console`

#### Application — Ports (core-ohs client interface)
- [ ] Create `Cotizador.Application/Ports/ICoreOhsClient.cs` per §3.5

#### Application — DTOs (for core-ohs responses)
- [ ] Create `Cotizador.Application/DTOs/SubscriberDto.cs`
- [ ] Create `Cotizador.Application/DTOs/AgentDto.cs`
- [ ] Create `Cotizador.Application/DTOs/BusinessLineDto.cs`
- [ ] Create `Cotizador.Application/DTOs/ZipCodeDto.cs`
- [ ] Create `Cotizador.Application/DTOs/ZipCodeValidationDto.cs`
- [ ] Create `Cotizador.Application/DTOs/FolioDto.cs`
- [ ] Create `Cotizador.Application/DTOs/RiskClassificationDto.cs`
- [ ] Create `Cotizador.Application/DTOs/GuaranteeDto.cs`
- [ ] Create `Cotizador.Application/DTOs/FireTariffDto.cs`
- [ ] Create `Cotizador.Application/DTOs/CatTariffDto.cs`
- [ ] Create `Cotizador.Application/DTOs/FhmTariffDto.cs`
- [ ] Create `Cotizador.Application/DTOs/ElectronicEquipmentFactorDto.cs`
- [ ] Create `Cotizador.Application/DTOs/CalculationParametersDto.cs`

#### Infrastructure — Persistence
- [ ] Create `Cotizador.Infrastructure/Persistence/MongoDbSettings.cs` — connection string, database name from `IConfiguration`
- [ ] Create `Cotizador.Infrastructure/Persistence/QuoteRepository.cs` — implements `IQuoteRepository` with `MongoDB.Driver`
  - [ ] `CreateAsync` — `InsertOneAsync`
  - [ ] `GetByFolioNumberAsync` — `Find({ folioNumber })`
  - [ ] `GetByIdempotencyKeyAsync` — `Find({ "metadata.idempotencyKey" })`
  - [ ] `UpdateGeneralInfoAsync` — `UpdateOneAsync` with `$set` for general info fields + version increment
  - [ ] `UpdateLayoutAsync` — `UpdateOneAsync` with `$set` for layout + version increment
  - [ ] `UpdateLocationsAsync` — `UpdateOneAsync` with `$set` for locations + version increment
  - [ ] `PatchLocationAsync` — `UpdateOneAsync` with `$set` for `locations.<idx>.*` + version increment
  - [ ] `UpdateCoverageOptionsAsync` — `UpdateOneAsync` with `$set` for coverage + version increment
  - [ ] `UpdateFinancialResultAsync` — `UpdateOneAsync` with `$set` for financial fields + version increment
  - All updates: filter `{ folioNumber, version: expected }`, throw `VersionConflictException` if `ModifiedCount == 0`
- [ ] Create `Cotizador.Infrastructure/Persistence/ServiceCollectionExtensions.cs` — `AddPersistence(this IServiceCollection, IConfiguration)`

#### Infrastructure — External Services
- [ ] Create `Cotizador.Infrastructure/ExternalServices/CoreOhsClient.cs` — implements `ICoreOhsClient` with `HttpClient`
- [ ] Create `Cotizador.Infrastructure/ExternalServices/ServiceCollectionExtensions.cs` — `AddExternalServices(this IServiceCollection, IConfiguration)` with `AddHttpClient`, timeout 10s, 1 retry per architecture-decisions.md
- [ ] Create `Cotizador.Infrastructure/ServiceCollectionExtensions.cs` — `AddInfrastructure()` that calls both `AddPersistence` and `AddExternalServices`

#### API — Middleware
- [ ] Create `Cotizador.API/Middleware/ExceptionHandlingMiddleware.cs` per §3.4
- [ ] Create `Cotizador.API/Middleware/CorrelationIdMiddleware.cs` — reads `X-Correlation-Id`, generates UUID if missing, enriches Serilog context

#### API — Auth
- [ ] Create `Cotizador.API/Auth/BasicAuthHandler.cs` per ADR-004

#### API — Program.cs
- [ ] Create `Cotizador.API/Program.cs` — Composition Root
  - Register `AddInfrastructure()`
  - Register middleware pipeline: CORS → CorrelationId → ExceptionHandling → Authentication → Routing → Controllers
  - Configure Serilog
  - Configure CORS for `http://localhost:3000` (webapp)
  - Read `appsettings.json` for MongoDB connection, core-ohs URL, auth credentials

#### Configuration
- [ ] Create `Cotizador.API/appsettings.json` with MongoDB connection string, core-ohs base URL, auth credentials
- [ ] Create `Cotizador.API/appsettings.Development.json` with local overrides

### 9.3 frontend-developer

N/A — backend-only.

### 9.4 test-engineer-backend

- [ ] Unit test: `PropertyQuote` entity — verify default values after construction
- [ ] Unit test: `QuoteStatus` constants — verify all 4 values
- [ ] Unit test: `GuaranteeKeys.All` — verify 14 keys
- [ ] Unit test: `FolioNotFoundException` — verify message includes folioNumber
- [ ] Unit test: `VersionConflictException` — verify message includes folioNumber and version
- [ ] Integration test: `QuoteRepository.CreateAsync` — insert and verify document in MongoDB
- [ ] Integration test: `QuoteRepository.GetByFolioNumberAsync` — read back inserted document
- [ ] Integration test: `QuoteRepository.GetByIdempotencyKeyAsync` — find by idempotency key
- [ ] Integration test: `QuoteRepository.UpdateGeneralInfoAsync` — verify only general info fields change, version increments
- [ ] Integration test: `QuoteRepository.UpdateGeneralInfoAsync` with wrong version → `VersionConflictException`
- [ ] Integration test: `QuoteRepository.UpdateLocationsAsync` — verify locations replaced, other fields untouched
- [ ] Integration test: `QuoteRepository.PatchLocationAsync` — verify single location patched
- [ ] Integration test: `QuoteRepository.UpdateCoverageOptionsAsync` — verify isolation
- [ ] Integration test: `QuoteRepository.UpdateFinancialResultAsync` — verify financial fields set, other sections untouched
- [ ] Integration test: `QuoteRepository.CreateAsync` duplicate `folioNumber` → MongoDB duplicate key exception
- [ ] Integration test: `ExceptionHandlingMiddleware` — verify 404/409/422/503/500 responses

### 9.5 test-engineer-frontend

N/A — backend-only.
