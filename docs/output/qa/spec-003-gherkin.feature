# spec: SPEC-003
# feature: folio-creation
# date: 2026-03-29
# author: qa-agent
# status: IN_PROGRESS

Feature: Folio Creation and Opening
  As a user of the cotizador
  I want to create new folios and open existing ones
  So that I can start or resume a property insurance quote

  Background:
    Given the API base URL is "http://localhost:5000/v1"
    And I have valid Basic Auth credentials encoded as "dXNlcjpwYXNz"
    And the core-mock service is available at "http://localhost:3000"
    And the MongoDB collection "property_quotes" is clean

  # ─────────────────────────────────────────────
  # HU-003-01: Create new folio
  # ─────────────────────────────────────────────

  @smoke @critico @HU-003-01
  Scenario: Create a new folio successfully (happy path)
    Given no folio exists with idempotency key "550e8400-e29b-41d4-a716-446655440000"
    And core-mock returns folioNumber "DAN-2026-00001" from GET /v1/folios/next
    When I send POST /v1/folios with headers:
      | Authorization   | Basic dXNlcjpwYXNz                       |
      | Idempotency-Key | 550e8400-e29b-41d4-a716-446655440000     |
      | Content-Type    | application/json                        |
    Then the response status is 201
    And the response body matches:
      | data.folioNumber   | DAN-2026-00001 |
      | data.quoteStatus   | draft          |
      | data.version       | 1              |
      | data.metadata.lastWizardStep | 0  |
    And the response body contains "data.metadata.createdAt" in UTC ISO-8601 format
    And the document is persisted in MongoDB with quoteStatus "draft" and version 1

  @regression @HU-003-01
  Scenario: Idempotent creation — same Idempotency-Key returns existing folio without creating duplicate
    Given a folio "DAN-2026-00001" already exists with idempotency key "550e8400-e29b-41d4-a716-446655440000" and version 3
    When I send POST /v1/folios with headers:
      | Authorization   | Basic dXNlcjpwYXNz                       |
      | Idempotency-Key | 550e8400-e29b-41d4-a716-446655440000     |
      | Content-Type    | application/json                        |
    Then the response status is 200
    And the response body matches:
      | data.folioNumber | DAN-2026-00001 |
      | data.version     | 3              |
    And the MongoDB collection "property_quotes" contains exactly 1 document with folioNumber "DAN-2026-00001"

  @error-path @HU-003-01
  Scenario: Missing Idempotency-Key header returns 400
    When I send POST /v1/folios with headers:
      | Authorization | Basic dXNlcjpwYXNz |
      | Content-Type  | application/json   |
    Then the response status is 400
    And the response body matches:
      | type    | validationError                              |
      | message | El header Idempotency-Key es obligatorio     |
      | field   | Idempotency-Key                              |
    And no document is created in MongoDB

  @security @HU-003-01
  Scenario: Missing Authorization header returns 401
    When I send POST /v1/folios with headers:
      | Idempotency-Key | 550e8400-e29b-41d4-a716-446655440001 |
      | Content-Type    | application/json                     |
    Then the response status is 401

  @edge-case @HU-003-01
  Scenario Outline: Invalid Idempotency-Key format (non-UUID) is rejected
    When I send POST /v1/folios with:
      | Authorization   | Basic dXNlcjpwYXNz |
      | Idempotency-Key | <key>              |
    Then the response status is 400
    And the response body field "type" is "validationError"

    Examples:
      | key                  |
      | not-a-uuid           |
      | 12345                |
      | 550e8400-XXXX-41d4   |

  # ─────────────────────────────────────────────
  # HU-003-02: Open existing folio
  # ─────────────────────────────────────────────

  @smoke @critico @HU-003-02
  Scenario: Open an existing folio by folioNumber (happy path)
    Given a folio exists in MongoDB with the following data:
      | folioNumber       | DAN-2026-00001  |
      | quoteStatus       | in_progress     |
      | version           | 3               |
      | lastWizardStep    | 2               |
      | createdAt         | 2026-03-28T15:00:00Z |
    When I send GET /v1/quotes/DAN-2026-00001 with Authorization "Basic dXNlcjpwYXNz"
    Then the response status is 200
    And the response body matches:
      | data.folioNumber           | DAN-2026-00001  |
      | data.quoteStatus           | in_progress     |
      | data.version               | 3               |
      | data.metadata.lastWizardStep | 2             |

  @error-path @HU-003-02
  Scenario: Open a non-existent folio returns 404
    Given no folio exists with folioNumber "DAN-2026-99999"
    When I send GET /v1/quotes/DAN-2026-99999 with Authorization "Basic dXNlcjpwYXNz"
    Then the response status is 404
    And the response body matches:
      | type    | folioNotFound                           |
      | message | El folio DAN-2026-99999 no existe       |
      | field   | null                                    |

  @error-path @HU-003-02
  Scenario Outline: Invalid folio format in path param returns 400
    When I send GET /v1/quotes/<folio> with Authorization "Basic dXNlcjpwYXNz"
    Then the response status is 400
    And the response body matches:
      | type    | validationError                                     |
      | message | Formato de folio inválido. Use DAN-YYYY-NNNNN       |
      | field   | folio                                               |

    Examples:
      | folio         |
      | INVALID-FORMAT |
      | DAN-26-001    |
      | DAN-2026-1    |
      | 12345         |
      | DAN-2026-000000 |

  @security @HU-003-02
  Scenario: Get folio without Authorization returns 401
    Given a folio exists with folioNumber "DAN-2026-00001"
    When I send GET /v1/quotes/DAN-2026-00001 without Authorization header
    Then the response status is 401

  # ─────────────────────────────────────────────
  # HU-003-03: Wizard redirect after create/open
  # ─────────────────────────────────────────────

  @smoke @HU-003-03 @frontend
  Scenario: After creating a new folio, frontend redirects to step 1 (general-info)
    Given the user is on the HomePage "/"
    And the folio "DAN-2026-00001" was just created (status 201)
    When the frontend processes the 201 response
    Then the browser navigates to "/quotes/DAN-2026-00001/general-info"
    And the quoteWizardSlice state is:
      | currentStep  | 1              |
      | activeFolio  | DAN-2026-00001 |

  @regression @HU-003-03 @frontend
  Scenario: After opening folio at step 2, frontend redirects to step 2 (locations)
    Given a folio "DAN-2026-00001" exists with lastWizardStep 2
    And the user searches for "DAN-2026-00001" on the HomePage
    When the frontend processes the 200 response
    Then the browser navigates to "/quotes/DAN-2026-00001/locations"
    And the quoteWizardSlice state is:
      | currentStep  | 2              |
      | activeFolio  | DAN-2026-00001 |

  @regression @HU-003-03 @frontend
  Scenario: After opening folio at step 0, frontend redirects to step 1 (general-info)
    Given a folio "DAN-2026-00002" exists with lastWizardStep 0
    And the user searches for "DAN-2026-00002" on the HomePage
    When the frontend processes the 200 response
    Then the browser navigates to "/quotes/DAN-2026-00002/general-info"
    And the quoteWizardSlice state has currentStep 1

  # ─────────────────────────────────────────────
  # HU-003-04: Resilience when core-mock is unavailable
  # ─────────────────────────────────────────────

  @resilience @HU-003-04
  Scenario: core-mock times out — backend retries once and returns 503
    Given core-mock is configured to not respond (timeout after 10 seconds)
    When I send POST /v1/folios with:
      | Authorization   | Basic dXNlcjpwYXNz                       |
      | Idempotency-Key | 660e8400-e29b-41d4-a716-446655440099     |
    Then the backend retries the call to GET /v1/folios/next exactly 1 time
    And the total wait before response is at least 500ms (retry delay)
    And the response status is 503
    And the response body matches:
      | type    | coreOhsUnavailable                                             |
      | message | Servicio de catálogos no disponible, intente más tarde         |
      | field   | null                                                           |
    And no document is created in MongoDB

  @resilience @HU-003-04
  Scenario: core-mock returns 5xx error — backend retries once and returns 503
    Given core-mock returns HTTP 500 on GET /v1/folios/next
    When I send POST /v1/folios with:
      | Authorization   | Basic dXNlcjpwYXNz                       |
      | Idempotency-Key | 770e8400-e29b-41d4-a716-446655440088     |
    Then the backend retries exactly once with 500ms delay
    And the response status is 503
    And the response body field "type" is "coreOhsUnavailable"
    And no document is created in MongoDB

  @resilience @HU-003-04
  Scenario: core-mock recovers on retry — folio creation succeeds
    Given core-mock times out on the first call to GET /v1/folios/next
    And core-mock returns "DAN-2026-00002" on the second call
    When I send POST /v1/folios with:
      | Authorization   | Basic dXNlcjpwYXNz                       |
      | Idempotency-Key | 880e8400-e29b-41d4-a716-446655440077     |
    Then the response status is 201
    And the response body field "data.folioNumber" is "DAN-2026-00002"
