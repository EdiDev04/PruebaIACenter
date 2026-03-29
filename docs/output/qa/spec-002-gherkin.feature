# language: en
Feature: Quote Data Model and Persistence (SPEC-002)
  As the quoter backend system,
  I want to persist, retrieve, and partially update PropertyQuote documents in MongoDB
  So that all folio lifecycle information is correctly maintained with optimistic locking.

  Background:
    Given the MongoDB collection "property_quotes" is available
    And the unique index on "folioNumber" is enforced
    And Basic Auth is configured with username "admin" and password "cotizador2026"

  # ─────────────────────────────────────────────
  # HU-002-01 — Quote Creation
  # ─────────────────────────────────────────────

  @smoke @critico @HU-002-01
  Scenario: Create a new quote successfully
    Given no document exists in "property_quotes" with folioNumber "DAN-2026-00001"
    When a new PropertyQuote is created with:
      | folioNumber   | DAN-2026-00001       |
      | quoteStatus   | draft                |
      | idempotencyKey| 550e8400-e29b-41d4-a716-446655440000 |
    Then the document is persisted in "property_quotes"
    And the persisted document has:
      | folioNumber        | DAN-2026-00001      |
      | version            | 1                   |
      | quoteStatus        | draft               |
    And "metadata.createdAt" is set to the current UTC time
    And "metadata.updatedAt" equals "metadata.createdAt"

  @error-path @HU-002-01
  Scenario: Reject duplicate folioNumber on creation
    Given a document with folioNumber "DAN-2026-00001" already exists in "property_quotes"
    When a second PropertyQuote with folioNumber "DAN-2026-00001" is inserted
    Then the operation fails with a MongoDB duplicate key error
    And no additional document appears in "property_quotes" with folioNumber "DAN-2026-00001"
    And the existing document version remains unchanged

  @smoke @critico @HU-002-01
  Scenario: Idempotency key resolves existing folio without duplicate creation
    Given a quote with idempotencyKey "550e8400-e29b-41d4-a716-446655440000" already exists
      And that quote has folioNumber "DAN-2026-00001" and version 1
    When GetByIdempotencyKeyAsync is called with "550e8400-e29b-41d4-a716-446655440000"
    Then the existing quote "DAN-2026-00001" is returned
    And no new document is created in "property_quotes"

  @edge-case @HU-002-01
  Scenario Outline: Reject folioNumber with invalid format on creation
    When a PropertyQuote with folioNumber "<invalidFolio>" is created
    Then the operation is rejected with message "Invalid folio number format"
    And no document is persisted in "property_quotes"
    Examples:
      | invalidFolio     |
      | INVALID-FORMAT   |
      | DAN-26-00001     |
      | DAN-2026-1       |
      | dan-2026-00001   |
      |                  |

  # ─────────────────────────────────────────────
  # HU-002-02 — Partial Updates (section isolation)
  # ─────────────────────────────────────────────

  @smoke @critico @HU-002-02
  Scenario: Update general info without overwriting locations
    Given a quote exists with:
      | folioNumber         | DAN-2026-00001       |
      | version             | 3                    |
      | insuredData.name    | Old Corp SA          |
      | insuredData.taxId   | OLD850101ABC         |
      | locations count     | 2                    |
    When UpdateGeneralInfoAsync is called with:
      | insuredData.name  | New Corp SA          |
      | insuredData.taxId | NEW850101XYZ         |
      | agentCode         | AGT-001              |
      | businessType      | commercial           |
      | riskClassification| standard             |
      | version           | 3                    |
    Then the persisted document has:
      | insuredData.name  | New Corp SA          |
      | insuredData.taxId | NEW850101XYZ         |
      | version           | 4                    |
    And "metadata.updatedAt" is refreshed to current UTC time
    And "metadata.lastWizardStep" is 1
    And "locations" still contains exactly 2 location documents
    And "coverageOptions" is unchanged
    And "netPremium" is unchanged

  @smoke @critico @HU-002-02
  Scenario: Update financial result without overwriting insuredData, locations, or coverageOptions
    Given a quote exists with:
      | folioNumber         | DAN-2026-00001       |
      | version             | 5                    |
      | insuredData.name    | Test Corp SA         |
      | insuredData.taxId   | TCO850101ABC         |
      | locations count     | 1                    |
    When UpdateFinancialResultAsync is called with:
      | netPremium          | 12500.00             |
      | commercialPremium   | 15000.00             |
      | premiumsByLocation count | 1               |
      | version             | 5                    |
    Then the persisted document has:
      | netPremium          | 12500.00             |
      | commercialPremium   | 15000.00             |
      | quoteStatus         | calculated           |
      | version             | 6                    |
    And "metadata.lastWizardStep" is 4
    And "insuredData.name" remains "Test Corp SA"
    And "insuredData.taxId" remains "TCO850101ABC"
    And "locations" remains unchanged with 1 document
    And "coverageOptions" is unchanged

  @critico @HU-002-02
  Scenario: Update locations does not touch general info or financial results
    Given a quote exists with:
      | folioNumber         | DAN-2026-00001       |
      | version             | 2                    |
      | insuredData.name    | Test Corp SA         |
      | netPremium          | 5000.00              |
    When UpdateLocationsAsync is called with a list of 3 locations and version 2
    Then the persisted document has version 3
    And "locations" has exactly 3 documents
    And "insuredData.name" remains "Test Corp SA"
    And "netPremium" remains 5000.00
    And "metadata.lastWizardStep" is 2

  @critico @HU-002-02
  Scenario: Patch a single location by index does not affect other locations
    Given a quote exists with:
      | folioNumber     | DAN-2026-00001 |
      | version         | 7              |
      | locations count | 3              |
    When PatchLocationAsync is called for locationIndex 2 with:
      | locationName | Updated Office |
      | zipCode      | 06600          |
      | version      | 7              |
    Then the persisted document has version 8
    And "locations[1].locationName" is "Updated Office"
    And "locations[0]" is unchanged
    And "locations[2]" is unchanged

  # ─────────────────────────────────────────────
  # HU-002-03 — Optimistic Locking
  # ─────────────────────────────────────────────

  @smoke @critico @HU-002-03
  Scenario: UpdateGeneralInfo succeeds when version matches
    Given a quote exists with folioNumber "DAN-2026-00001" and version 5
    When UpdateGeneralInfoAsync is called with version 5 and insuredData.name "Test Corp SA"
    Then the operation succeeds
    And the persisted document has version 6
    And "insuredData.name" is "Test Corp SA"

  @smoke @critico @HU-002-03
  Scenario: UpdateGeneralInfo fails with stale version
    Given a quote exists with folioNumber "DAN-2026-00001" and version 5
    When UpdateGeneralInfoAsync is called with version 4 (stale) and insuredData.name "Stale Corp"
    Then the operation throws VersionConflictException
    And the persisted document still has version 5
    And "insuredData.name" is not changed to "Stale Corp"

  @critico @HU-002-03
  Scenario: Concurrent updates — second writer loses with version conflict
    Given a quote exists with folioNumber "DAN-2026-00001" and version 3
    When two concurrent requests both attempt UpdateGeneralInfoAsync with version 3
    Then exactly one request succeeds and version becomes 4
    And the other request throws VersionConflictException
    And the final version in MongoDB is 4 (not 5)

  # ─────────────────────────────────────────────
  # HU-002-04 — Automatic version + updatedAt increment
  # ─────────────────────────────────────────────

  @smoke @HU-002-04
  Scenario Outline: Every successful write increments version and refreshes updatedAt
    Given a quote exists with folioNumber "DAN-2026-00001" and version <initialVersion>
    And "metadata.updatedAt" is T1
    When <operation> is called successfully
    Then the persisted document has version <expectedVersion>
    And "metadata.updatedAt" is greater than T1
    Examples:
      | initialVersion | operation                  | expectedVersion |
      | 1              | UpdateGeneralInfoAsync     | 2               |
      | 2              | UpdateLayoutAsync          | 3               |
      | 3              | UpdateLocationsAsync       | 4               |
      | 4              | UpdateCoverageOptionsAsync | 5               |
      | 5              | UpdateFinancialResultAsync | 6               |

  @HU-002-04
  Scenario: Failed write (version conflict) does not alter version or updatedAt
    Given a quote exists with folioNumber "DAN-2026-00001" and version 3
    And "metadata.updatedAt" is T1
    When UpdateGeneralInfoAsync is called with stale version 2
    Then VersionConflictException is thrown
    And the persisted document still has version 3
    And "metadata.updatedAt" remains T1

  # ─────────────────────────────────────────────
  # Exception Handling Middleware
  # ─────────────────────────────────────────────

  @smoke @critico @middleware
  Scenario: Middleware maps FolioNotFoundException to HTTP 404
    Given the middleware pipeline is active
    When a use case throws FolioNotFoundException for folioNumber "DAN-2026-99999"
    Then the HTTP response has status 404
    And the response body is:
      """
      {
        "type": "folioNotFound",
        "message": "Folio 'DAN-2026-99999' not found",
        "field": null
      }
      """
    And no stack trace is present in the response body

  @smoke @critico @middleware
  Scenario: Middleware maps VersionConflictException to HTTP 409
    Given the middleware pipeline is active
    When a use case throws VersionConflictException for folioNumber "DAN-2026-00001" with expectedVersion 3
    Then the HTTP response has status 409
    And the response body is:
      """
      {
        "type": "versionConflict",
        "message": "Version conflict on folio 'DAN-2026-00001'. Expected version: 3",
        "field": null
      }
      """
    And no stack trace is present in the response body

  @middleware
  Scenario: Middleware maps InvalidQuoteStateException to HTTP 422
    Given the middleware pipeline is active
    When a use case throws InvalidQuoteStateException for folioNumber "DAN-2026-00001" state "finalized"
    Then the HTTP response has status 422
    And the response body "type" is "invalidQuoteState"
    And no stack trace is present in the response body

  @middleware
  Scenario: Middleware maps CoreOhsUnavailableException to HTTP 503
    Given the middleware pipeline is active
    When a use case throws CoreOhsUnavailableException with message "core-ohs is unreachable"
    Then the HTTP response has status 503
    And the response body "type" is "coreOhsUnavailable"
    And no stack trace is present in the response body

  @smoke @critico @middleware @seguridad
  Scenario: Middleware does NOT expose stack trace in unhandled 500 response
    Given the middleware pipeline is active
    When any unhandled Exception is thrown internally
    Then the HTTP response has status 500
    And the response body is:
      """
      {
        "type": "internal",
        "message": "Internal server error",
        "field": null
      }
      """
    And the response body does NOT contain "StackTrace"
    And the response body does NOT contain "at System."
    And the full exception is logged via Serilog at Error level

  # ─────────────────────────────────────────────
  # Basic Auth
  # ─────────────────────────────────────────────

  @smoke @critico @seguridad @auth
  Scenario: Valid Basic Auth credentials return 200
    Given the API has Basic Auth configured
    When a request is made with Authorization header "Basic YWRtaW46Y290aXphZG9yMjAyNg=="
    Then the HTTP response has status 200

  @smoke @critico @seguridad @auth
  Scenario: Invalid Basic Auth credentials return 401
    Given the API has Basic Auth configured
    When a request is made with Authorization header for user "admin" and wrong password "wrong-pass"
    Then the HTTP response has status 401
    And the response body does NOT contain any internal detail

  @smoke @critico @seguridad @auth
  Scenario: Missing Authorization header returns 401
    Given the API has Basic Auth configured
    When a request is made without any Authorization header
    Then the HTTP response has status 401
    And the "WWW-Authenticate" response header is present with scheme "Basic"

  @seguridad @auth
  Scenario Outline: Various invalid auth header formats return 401
    Given the API has Basic Auth configured
    When a request is made with Authorization header "<invalidHeader>"
    Then the HTTP response has status 401
    Examples:
      | invalidHeader                        |
      | Bearer someJwtToken                  |
      | Basic invalidbase64===               |
      | Basic                                |

  # ─────────────────────────────────────────────
  # Folio lookup (GetByFolioNumberAsync)
  # ─────────────────────────────────────────────

  @smoke @critico
  Scenario: Retrieve existing quote by folioNumber
    Given a quote exists with folioNumber "DAN-2026-00001" and version 2
    When GetByFolioNumberAsync is called with "DAN-2026-00001"
    Then the returned object has folioNumber "DAN-2026-00001" and version 2
    And all fields are deserialized in camelCase from MongoDB

  @error-path
  Scenario: GetByFolioNumberAsync returns null for non-existent folio
    Given no document with folioNumber "DAN-2026-99999" exists in "property_quotes"
    When GetByFolioNumberAsync is called with "DAN-2026-99999"
    Then the result is null (not an exception)
