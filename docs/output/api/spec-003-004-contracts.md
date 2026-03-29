# API Contracts — SPEC-003 (folio-creation) + SPEC-004 (general-info-management)

> **Generado:** 2026-03-29  
> **Specs:** SPEC-003 · SPEC-004  
> **Auth:** Basic Auth — `Authorization: Basic <base64(username:password)>`  
> **Base URL:** `http://<host>/v1`  
> **Content-Type:** `application/json`

---

## POST /v1/folios

Crea un nuevo folio de cotización. La operación es idempotente mediante el header `Idempotency-Key`.

**Auth:** Basic Auth (requerido)

**Headers requeridos:**

| Header | Tipo | Descripción |
|---|---|---|
| `Authorization` | string | `Basic <base64(username:password)>` |
| `Idempotency-Key` | string | UUID único por intento de creación. Si ya existe un folio con esta clave, se retorna el folio existente (HTTP 200). |

**Response 201 Created** — folio creado:

```json
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "draft",
    "version": 1,
    "metadata": {
      "createdAt": "2026-03-29T14:00:00Z",
      "updatedAt": "2026-03-29T14:00:00Z",
      "createdBy": "usuario@ejemplo.com",
      "lastWizardStep": 0
    }
  }
}
```

**Response 200 OK** — folio ya existente (idempotencia):

```json
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "in_progress",
    "version": 3,
    "metadata": {
      "createdAt": "2026-03-28T10:00:00Z",
      "updatedAt": "2026-03-29T09:30:00Z",
      "createdBy": "usuario@ejemplo.com",
      "lastWizardStep": 1
    }
  }
}
```

**Response 400 Bad Request** — header `Idempotency-Key` ausente o vacío:

```json
{
  "type": "validationError",
  "message": "El header Idempotency-Key es obligatorio",
  "field": "Idempotency-Key"
}
```

**Response 401 Unauthorized** — credenciales inválidas o ausentes (sin body, gestionado por ASP.NET Core).

**Response 503 Service Unavailable** — `cotizador-core-mock` no disponible:

```json
{
  "type": "coreOhsUnavailable",
  "message": "The reference data service is temporarily unavailable. Please try again later.",
  "field": null
}
```

**Ejemplo curl:**

```bash
curl -X POST http://localhost:5000/v1/folios \
  -H "Authorization: Basic dXNlcjpwYXNz" \
  -H "Idempotency-Key: f47ac10b-58cc-4372-a567-0e02b2c3d479"
```

---

## GET /v1/quotes/{folio}

Retorna el resumen de estado de una cotización por número de folio.

**Auth:** Basic Auth (requerido)

**Path parameters:**

| Parámetro | Formato | Ejemplo |
|---|---|---|
| `folio` | `DAN-YYYY-NNNNN` | `DAN-2026-00001` |

**Response 200 OK:**

```json
{
  "data": {
    "folioNumber": "DAN-2026-00001",
    "quoteStatus": "in_progress",
    "version": 3,
    "metadata": {
      "createdAt": "2026-03-28T10:00:00Z",
      "updatedAt": "2026-03-29T09:30:00Z",
      "createdBy": "usuario@ejemplo.com",
      "lastWizardStep": 1
    }
  }
}
```

**Response 400 Bad Request** — formato de folio inválido:

```json
{
  "type": "validationError",
  "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN",
  "field": "folio"
}
```

**Response 401 Unauthorized** — sin body, gestionado por ASP.NET Core.

**Response 404 Not Found** — folio no encontrado:

```json
{
  "type": "folioNotFound",
  "message": "El folio DAN-2026-99999 no existe",
  "field": null
}
```

**Response 500 Internal Server Error:**

```json
{
  "type": "internal",
  "message": "Internal server error",
  "field": null
}
```

**Ejemplo curl:**

```bash
curl -X GET http://localhost:5000/v1/quotes/DAN-2026-00001 \
  -H "Authorization: Basic dXNlcjpwYXNz"
```

---

## GET /v1/quotes/{folio}/general-info

Retorna la sección de datos generales de una cotización.

**Auth:** Basic Auth (requerido)

**Path parameters:**

| Parámetro | Formato | Ejemplo |
|---|---|---|
| `folio` | `DAN-YYYY-NNNNN` | `DAN-2026-00001` |

**Response 200 OK:**

```json
{
  "data": {
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
    "riskClassification": "bajo",
    "version": 2
  }
}
```

> Los campos `email`, `phone` y `branchOffice` pueden ser `null` si no fueron capturados.

**Response 400 Bad Request** — formato de folio inválido:

```json
{
  "type": "validationError",
  "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN",
  "field": "folio"
}
```

**Response 401 Unauthorized** — sin body.

**Response 404 Not Found:**

```json
{
  "type": "folioNotFound",
  "message": "El folio DAN-2026-99999 no existe",
  "field": null
}
```

**Ejemplo curl:**

```bash
curl -X GET http://localhost:5000/v1/quotes/DAN-2026-00001/general-info \
  -H "Authorization: Basic dXNlcjpwYXNz"
```

---

## PUT /v1/quotes/{folio}/general-info

Actualiza la sección de datos generales de una cotización. Aplica optimistic concurrency mediante el campo `version`. Si el folio está en estado `draft`, la primera actualización exitosa lo transiciona a `in_progress`.

**Auth:** Basic Auth (requerido)

**Path parameters:**

| Parámetro | Formato | Ejemplo |
|---|---|---|
| `folio` | `DAN-YYYY-NNNNN` | `DAN-2026-00001` |

**Request Body:**

```json
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
  "riskClassification": "bajo",
  "version": 1
}
```

**Validaciones del request body:**

| Campo | Requerido | Regla |
|---|---|---|
| `insuredData.name` | Sí | No vacío, máx. 200 caracteres |
| `insuredData.taxId` | Sí | Regex `^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$` |
| `insuredData.email` | No | Formato email válido si se envía |
| `insuredData.phone` | No | Máx. 20 caracteres si se envía |
| `conductionData.subscriberCode` | Sí | Formato `SUB-NNN` |
| `conductionData.officeName` | Sí | No vacío |
| `conductionData.branchOffice` | No | — |
| `agentCode` | Sí | Formato `AGT-NNN`; debe existir en catálogo core-ohs |
| `businessType` | Sí | Uno de: `commercial`, `industrial`, `residential` |
| `riskClassification` | Sí | No vacío |
| `version` | Sí | Entero > 0; debe coincidir con la versión actual del folio |

**Response 200 OK:**

```json
{
  "data": {
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
    "riskClassification": "bajo",
    "version": 2
  }
}
```

**Response 400 Bad Request** — folio con formato inválido o fallo de validación del body:

```json
{
  "type": "validationError",
  "message": "El RFC del asegurado es obligatorio y debe tener formato válido",
  "field": "InsuredData.TaxId"
}
```

**Response 401 Unauthorized** — sin body.

**Response 404 Not Found** — folio no encontrado:

```json
{
  "type": "folioNotFound",
  "message": "El folio DAN-2026-00001 no existe",
  "field": null
}
```

**Response 409 Conflict** — conflicto de versión (optimistic concurrency):

```json
{
  "type": "versionConflict",
  "message": "El folio DAN-2026-00001 tiene un conflicto de versión",
  "field": null
}
```

**Response 422 Unprocessable Entity** — agente no encontrado en catálogo:

```json
{
  "type": "invalidQuoteState",
  "message": "El agente AGT-999 no está registrado en el catálogo",
  "field": null
}
```

**Response 503 Service Unavailable** — `cotizador-core-mock` no disponible:

```json
{
  "type": "coreOhsUnavailable",
  "message": "The reference data service is temporarily unavailable. Please try again later.",
  "field": null
}
```

**Ejemplo curl:**

```bash
curl -X PUT http://localhost:5000/v1/quotes/DAN-2026-00001/general-info \
  -H "Authorization: Basic dXNlcjpwYXNz" \
  -H "Content-Type: application/json" \
  -d '{
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
    "riskClassification": "bajo",
    "version": 1
  }'
```

---

## Modelo de errores

Todos los errores siguen el mismo envelope:

```json
{
  "type": "string",
  "message": "string",
  "field": "string | null"
}
```

| `type` | HTTP | Descripción |
|---|---|---|
| `validationError` | 400 | Campo inválido o header faltante |
| `folioNotFound` | 404 | El folio no existe en MongoDB |
| `versionConflict` | 409 | `version` enviada no coincide con la actual |
| `invalidQuoteState` | 422 | Estado del folio no permite la operación (ej. agente no en catálogo) |
| `coreOhsUnavailable` | 503 | `cotizador-core-mock` no disponible |
| `internal` | 500 | Error no controlado |
