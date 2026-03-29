# spec: SPEC-004
# feature: general-info-management
# date: 2026-03-29
# author: qa-agent
# status: IN_PROGRESS

Feature: General Info Management — Wizard Step 1
  As a user of the cotizador
  I want to capture and persist the general information of a quote
  So that the system can identify the insured party, agent, and business context

  Background:
    Given the API base URL is "http://localhost:5000/v1"
    And I have valid Basic Auth credentials encoded as "dXNlcjpwYXNz"
    And a folio "DAN-2026-00001" exists in MongoDB with quoteStatus "draft" and version 1
    And core-mock has the following agents registered:
      | code    | name              |
      | AGT-001 | Juan Pérez López  |
      | AGT-002 | María García Ruiz |
    And core-mock has the following subscribers registered:
      | code    | name             | officeName    |
      | SUB-001 | MGA Underwriting | CDMX Central  |
      | SUB-002 | AXA Partners     | Monterrey Norte |
    And core-mock returns risk classifications: "standard", "preferred", "substandard"
    And the configured allowed businessTypes are: "commercial", "industrial", "residential"

  # ─────────────────────────────────────────────
  # HU-004-01: Capture insured data
  # ─────────────────────────────────────────────

  @smoke @critico @HU-004-01
  Scenario: Load general-info for a folio with no data yet returns defaults
    Given folio "DAN-2026-00001" has no generalInfo saved
    When I send GET /v1/quotes/DAN-2026-00001/general-info with Authorization "Basic dXNlcjpwYXNz"
    Then the response status is 200
    And the response body contains "data.version" equal to 1
    And "data.insuredData.name" is null or empty
    And "data.insuredData.taxId" is null or empty

  @smoke @critico @HU-004-01
  Scenario: Save general-info with all fields (complete payload)
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with:
      """
      {
        "insuredData": {
          "name": "Grupo Industrial SA de CV",
          "taxId": "GIN850101AAA",
          "email": "contacto@grupoindustrial.com",
          "phone": "5551234567"
        },
        "conductionData": {
          "subscriberCode": "SUB-001",
          "officeName": "CDMX Central",
          "branchOffice": null
        },
        "agentCode": "AGT-001",
        "businessType": "commercial",
        "riskClassification": "standard",
        "version": 1
      }
      """
    Then the response status is 200
    And the response body matches:
      | data.insuredData.name                  | Grupo Industrial SA de CV       |
      | data.insuredData.taxId                 | GIN850101AAA                    |
      | data.insuredData.email                 | contacto@grupoindustrial.com    |
      | data.insuredData.phone                 | 5551234567                      |
      | data.conductionData.subscriberCode     | SUB-001                         |
      | data.conductionData.officeName         | CDMX Central                    |
      | data.agentCode                         | AGT-001                         |
      | data.businessType                      | commercial                      |
      | data.riskClassification                | standard                        |
    And the persisted document has version 2
    And "metadata.updatedAt" is updated

  @regression @HU-004-01
  Scenario: Save general-info with only required fields (no email, no phone)
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with:
      """
      {
        "insuredData": {
          "name": "Empresa XYZ SA",
          "taxId": "EXY900515BBB",
          "email": null,
          "phone": null
        },
        "conductionData": {
          "subscriberCode": "SUB-002",
          "officeName": "Monterrey Norte",
          "branchOffice": null
        },
        "agentCode": "AGT-001",
        "businessType": "industrial",
        "riskClassification": "preferred",
        "version": 1
      }
      """
    Then the response status is 200
    And the persisted document has "insuredData.email" as null
    And the persisted document has "insuredData.phone" as null
    And the document version is incremented to 2

  @error-path @HU-004-01
  Scenario Outline: Missing required insured fields return 400
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with <missing_field> absent or empty
    Then the response status is 400
    And the response body field "type" is "validationError"
    And the response body field "message" is "<expected_message>"

    Examples:
      | missing_field       | expected_message                                                         |
      | insuredData.name    | El nombre del asegurado es obligatorio                                   |
      | insuredData.taxId   | El RFC del asegurado es obligatorio y debe tener formato válido          |

  @error-path @HU-004-01
  Scenario Outline: Optional fields with invalid format when present return 400
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with insuredData.<field> as "<invalid_value>"
    Then the response status is 400
    And the response body field "type" is "validationError"
    And the response body field "message" contains "<expected_fragment>"

    Examples:
      | field | invalid_value                | expected_fragment          |
      | email | not-an-email                 | correo electrónico         |
      | phone | 123456789012345678901        | teléfono                   |
      | taxId | 123                          | RFC                        |

  # ─────────────────────────────────────────────
  # HU-004-02: Subscriber selection and office autocomplete
  # ─────────────────────────────────────────────

  @smoke @HU-004-02 @frontend
  Scenario: Subscriber dropdown is populated from core-mock on form load
    Given the user navigates to "/quotes/DAN-2026-00001/general-info"
    When the GeneralInfoPage finishes loading
    Then the subscriber dropdown shows 2 options:
      | code    | label            |
      | SUB-001 | MGA Underwriting |
      | SUB-002 | AXA Partners     |

  @regression @HU-004-02 @frontend
  Scenario: Selecting subscriber SUB-001 auto-fills the officeName field
    Given the user is on the GeneralInfoPage for folio "DAN-2026-00001"
    When the user selects subscriber "SUB-001" from the dropdown
    Then the officeName field is automatically set to "CDMX Central"
    And the officeName field is read-only

  @regression @HU-004-02
  Scenario: subscriberCode and officeName are persisted correctly
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with:
      | conductionData.subscriberCode | SUB-001       |
      | conductionData.officeName     | CDMX Central  |
      | version                       | 1             |
    Then the MongoDB document has conductionData.subscriberCode "SUB-001"
    And the MongoDB document has conductionData.officeName "CDMX Central"

  @error-path @HU-004-02
  Scenario: Missing subscriberCode returns 400
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with "conductionData.subscriberCode" absent
    Then the response status is 400
    And the response body field "message" is "El suscriptor es obligatorio"

  # ─────────────────────────────────────────────
  # HU-004-03: Agent validation against core-mock
  # ─────────────────────────────────────────────

  @smoke @critico @HU-004-03
  Scenario: Agent AGT-001 exists in core-mock — general-info saved successfully
    Given core-mock returns agent "AGT-001" on GET /v1/agents?code=AGT-001
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with agentCode "AGT-001" and version 1
    Then the backend calls GET /v1/agents?code=AGT-001 on core-mock
    And the response status is 200
    And the persisted document has agentCode "AGT-001"

  @error-path @HU-004-03
  Scenario: Agent AGT-999 does not exist in core-mock — returns 422
    Given core-mock returns HTTP 404 on GET /v1/agents?code=AGT-999
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with agentCode "AGT-999" and version 1
    Then the response status is 422
    And the response body matches:
      | type    | invalidQuoteState                                         |
      | message | El agente AGT-999 no está registrado en el catálogo       |
      | field   | null                                                      |
    And no changes are persisted to the MongoDB document

  @error-path @HU-004-03
  Scenario: Invalid agentCode format returns 400 before calling core-mock
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with agentCode "INVALID" and version 1
    Then the response status is 400
    And the response body field "type" is "validationError"
    And the response body field "message" is "Código de agente inválido"
    And core-mock is NOT called

  @resilience @HU-004-03
  Scenario: core-mock times out during agent validation — returns 503
    Given core-mock does not respond to GET /v1/agents?code=AGT-001
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with agentCode "AGT-001" and version 1
    Then the response status is 503
    And the response body field "type" is "coreOhsUnavailable"
    And no changes are persisted

  # ─────────────────────────────────────────────
  # HU-004-04: Business type and risk classification
  # ─────────────────────────────────────────────

  @smoke @HU-004-04
  Scenario Outline: Valid businessType values are accepted
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with businessType "<type>" and version 1
    Then the response status is 200
    And the persisted document has businessType "<type>"

    Examples:
      | type        |
      | commercial  |
      | industrial  |
      | residential |

  @error-path @HU-004-04
  Scenario: Invalid businessType returns 400
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with businessType "invalid_type" and version 1
    Then the response status is 400
    And the response body matches:
      | type    | validationError                                                                  |
      | message | Tipo de negocio inválido. Valores permitidos: commercial, industrial, residential |
      | field   | businessType                                                                     |

  @smoke @HU-004-04 @frontend
  Scenario: Risk classification dropdown loads from core-mock
    Given the user navigates to "/quotes/DAN-2026-00001/general-info"
    When the GeneralInfoPage finishes loading
    Then the riskClassification dropdown shows exactly 3 options:
      | value       |
      | standard    |
      | preferred   |
      | substandard |

  @regression @HU-004-04
  Scenario Outline: Valid riskClassification values are persisted
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with riskClassification "<value>" and version 1
    Then the response status is 200
    And the persisted MongoDB document has riskClassification "<value>"

    Examples:
      | value       |
      | standard    |
      | preferred   |
      | substandard |

  # ─────────────────────────────────────────────
  # HU-004-05: Optimistic versioning
  # ─────────────────────────────────────────────

  @smoke @critico @HU-004-05
  Scenario: Successful update increments version and transitions draft to in_progress
    Given folio "DAN-2026-00001" has quoteStatus "draft" and version 1
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with a valid payload and version 1
    Then the response status is 200
    And the MongoDB document has version 2
    And the MongoDB document has quoteStatus "in_progress"
    And "metadata.lastWizardStep" is 1

  @regression @HU-004-05
  Scenario: Successful update on already in_progress folio keeps status in_progress
    Given folio "DAN-2026-00001" has quoteStatus "in_progress" and version 3
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with a valid payload and version 3
    Then the response status is 200
    And the MongoDB document has version 4
    And the MongoDB document has quoteStatus "in_progress"

  @error-path @critico @HU-004-05
  Scenario: Version conflict — stale version returns 409
    Given folio "DAN-2026-00001" has been updated to version 4 by another process
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with version 3 (stale)
    Then the response status is 409
    And the response body matches:
      | type    | versionConflict                                                     |
      | message | El folio fue modificado por otro proceso. Recargue para continuar   |
      | field   | null                                                                |
    And the MongoDB document version remains 4 (unchanged)

  @edge-case @HU-004-05
  Scenario: Version 0 or negative in request returns 400
    When I send PUT /v1/quotes/DAN-2026-00001/general-info with version 0
    Then the response status is 400
    And the response body field "type" is "validationError"

  @error-path @HU-004-05
  Scenario: Folio not found during update returns 404
    Given no folio exists with folioNumber "DAN-2026-88888"
    When I send PUT /v1/quotes/DAN-2026-88888/general-info with a valid payload and version 1
    Then the response status is 404
    And the response body field "type" is "folioNotFound"

  @edge-case @HU-004-05 @frontend
  Scenario: Frontend detects 409 conflict and shows reload prompt
    Given the user has the GeneralInfoPage open with folio "DAN-2026-00001"
    And the folio version was updated to 4 by another process while the user was editing
    When the user submits the form (sending version 3)
    And the backend returns HTTP 409
    Then the UI displays the alert: "El folio fue modificado por otro proceso. Recargue para continuar"
    And a "Recargar" button is visible
    And the form data is NOT cleared
