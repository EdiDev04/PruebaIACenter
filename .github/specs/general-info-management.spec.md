---
id: SPEC-004
status: DRAFT
feature: general-info-management
feature_type: full-stack
requires_design_spec: true
has_calculation_logic: false
affects_database: true
consumes_core_ohs: true
created: 2026-03-29
updated: 2026-03-29
author: spec-generator
version: "1.0"
related-specs: ["SPEC-001", "SPEC-002", "SPEC-003"]
priority: alta
estimated-complexity: M
---

# Spec: Gestión de Datos Generales de Cotización

> **Estado:** `DRAFT` → aprobar con `status: APPROVED` antes de iniciar implementación.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. RESUMEN EJECUTIVO

Implementar la consulta y guardado de los datos generales de una cotización: datos del asegurado (nombre, RFC, y opcionalmente correo electrónico y teléfono), datos de conducción (suscriptor, oficina), agente asociado, tipo de negocio y clasificación de riesgo. Corresponde al Step 1 del wizard (ADR-005). Los catálogos de suscriptores, agentes y clasificación de riesgo se consultan desde `cotizador-core-mock`. El backend valida la existencia del agente y suscriptor contra core-ohs antes de persistir. Los valores válidos de `businessType` se configuran vía `appsettings.json`. Este feature junto con SPEC-003 forma el primer flujo CRUD completo del sistema.

---

## 2. REQUERIMIENTOS

### 2.1 Historias de usuario

**HU-004-01**: Como usuario del cotizador, quiero capturar los datos del asegurado (nombre, RFC, y opcionalmente correo electrónico y teléfono) para identificar y contactar al cliente de la póliza.

**Criterios de aceptación (Gherkin):**

- **Dado** que tengo un folio existente `DAN-2026-00001` sin datos generales capturados
  **Cuando** envío `GET /v1/quotes/DAN-2026-00001/general-info`
  **Entonces** el sistema retorna HTTP 200 con los campos de datos generales vacíos o con valores por defecto
  **Y** el response incluye `version` actual del folio

- **Dado** que capturo nombre `"Grupo Industrial SA de CV"`, RFC `"GIN850101AAA"`, email `"contacto@grupoindustrial.com"` y teléfono `"5551234567"`
  **Cuando** envío `PUT /v1/quotes/DAN-2026-00001/general-info` con esos datos y `version: 1`
  **Entonces** el sistema persiste solo la sección de datos generales (incluyendo email y teléfono como opcionales)
  **Y** incrementa `version` a 2
  **Y** actualiza `metadata.updatedAt`
  **Y** retorna HTTP 200 con `{ "data": { ...datosActualizados } }`

- **Dado** que capturo solo nombre `"Empresa XYZ SA"` y RFC `"EXY900515BBB"` sin email ni teléfono
  **Cuando** envío `PUT /v1/quotes/DAN-2026-00001/general-info` con esos datos y `version: 1`
  **Entonces** el sistema persiste con `email: null` y `phone: null`
  **Y** la operación se completa exitosamente

---

**HU-004-02**: Como usuario del cotizador, quiero seleccionar un suscriptor del catálogo para asignar el underwriter responsable.

**Criterios de aceptación (Gherkin):**

- **Dado** que el servicio core-mock tiene 3 suscriptores activos
  **Cuando** el frontend carga el formulario de datos generales
  **Entonces** se muestra un selector (dropdown) con los suscriptores obtenidos de `GET /v1/subscribers`

- **Dado** que selecciono el suscriptor con código `SUB-001`
  **Cuando** guardo los datos generales
  **Entonces** el campo `conductionData.subscriberCode` se persiste como `"SUB-001"`
  **Y** el campo `conductionData.officeName` se persiste con la oficina correspondiente (`"CDMX Central"`)

---

**HU-004-03**: Como usuario del cotizador, quiero buscar y seleccionar un agente por clave para asociarlo a la cotización.

**Criterios de aceptación (Gherkin):**

- **Dado** que el agente `AGT-001` existe en el catálogo de core-mock
  **Cuando** envío datos generales con `agentCode: "AGT-001"`
  **Entonces** el backend valida que el agente existe en core-mock (`GET /v1/agents?code=AGT-001`)
  **Y** la operación se completa exitosamente

- **Dado** que el agente `AGT-999` no existe en el catálogo de core-mock
  **Cuando** envío datos generales con `agentCode: "AGT-999"`
  **Entonces** el backend retorna HTTP 422 con body `{ "type": "invalidQuoteState", "message": "El agente AGT-999 no está registrado en el catálogo", "field": null }`

---

**HU-004-04**: Como usuario del cotizador, quiero seleccionar el tipo de negocio y la clasificación de riesgo desde opciones predefinidas.

**Criterios de aceptación (Gherkin):**

- **Dado** que los tipos de negocio configurados en `appsettings.json` son `["commercial", "industrial", "residential"]`
  **Cuando** envío datos generales con `businessType: "commercial"`
  **Entonces** el valor es aceptado y persistido

- **Dado** que envío `businessType: "invalid_type"`
  **Cuando** el backend valida el campo
  **Entonces** retorna HTTP 400 con body `{ "type": "validationError", "message": "Tipo de negocio inválido. Valores permitidos: commercial, industrial, residential", "field": "businessType" }`

- **Dado** que las clasificaciones de riesgo vienen de core-mock (`GET /v1/catalogs/risk-classification`)
  **Cuando** el frontend carga el formulario
  **Entonces** se muestra un selector con las clasificaciones: `standard`, `preferred`, `substandard`

---

**HU-004-05**: Como usuario del cotizador, quiero guardar los datos generales y que el sistema actualice la versión de la cotización.

**Criterios de aceptación (Gherkin):**

- **Dado** que el folio tiene `version: 3`
  **Cuando** envío PUT con `version: 3` y datos generales válidos
  **Entonces** el folio se actualiza a `version: 4`
  **Y** si `quoteStatus` era `"draft"` → transiciona a `"in_progress"`
  **Y** `metadata.updatedAt` se actualiza
  **Y** `metadata.lastWizardStep` se actualiza a `1`

- **Dado** que el folio tiene `version: 4`
  **Cuando** envío PUT con `version: 3` (versión desactualizada)
  **Entonces** el sistema retorna HTTP 409 con body `{ "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }`

---

### 2.2 Reglas de negocio

| ID | Regla | Condición | Resultado | Origen |
|---|---|---|---|---|
| RN-004-01 | Actualización parcial — no afecta otras secciones | PUT general-info | Solo modifica `insuredData`, `conductionData`, `agentCode`, `businessType`, `riskClassification` | ADR-002 |
| RN-004-02 | Versionado optimista | PUT con `version` que no coincide | HTTP 409 VersionConflictException | architecture-decisions.md §Optimistic Versioning |
| RN-004-03 | Transición de estado `draft` → `in_progress` | Primera escritura exitosa en un folio con `quoteStatus: "draft"` | `$set: { quoteStatus: "in_progress" }` en la misma operación | REQ-08 §Reglas de negocio |
| RN-004-04 | El agente debe existir en core-ohs | PUT con `agentCode` | Backend consulta `GET /v1/agents?code=X`; si 404 → HTTP 422 | SUP-005 (confirmado por usuario) |
| RN-004-05 | El suscriptor debe referenciarse del catálogo | PUT con `conductionData.subscriberCode` | Backend verifica contra catálogo de suscriptores. Validación flexible: solo verifica formato en esta oleada | REQ-04 |
| RN-004-06 | `businessType` configurable | PUT con `businessType` no en la lista de `appsettings.json` | HTTP 400 | SUP-008 (confirmado por usuario) |
| RN-004-07 | `metadata.lastWizardStep` se actualiza a 1 | PUT general-info exitoso | Automático en el `$set` del repositorio | ADR-007 |
| RN-004-08 | Response envelope `{ "data": {...} }` | Toda respuesta 2xx | Wrapper obligatorio | architecture-decisions.md §Response Format |
| RN-004-09 | Mensajes de error en español | Toda respuesta de error | Campo `message` en español; `type` en inglés | ADR-008 |

### 2.3 Validaciones

| Campo | Regla de validación | Mensaje de error | Bloquea guardado |
|---|---|---|---|
| `insuredData.name` | Requerido, no vacío, max 200 chars | "El nombre del asegurado es obligatorio" | Sí (400) |
| `insuredData.taxId` | Requerido, formato RFC mexicano (regex) | "El RFC del asegurado es obligatorio y debe tener formato válido" | Sí (400) |
| `agentCode` | Requerido, formato `^AGT-\d{3}$`, debe existir en core-ohs | "Código de agente inválido" / "El agente {code} no está registrado en el catálogo" | Sí (400/422) |
| `conductionData.subscriberCode` | Requerido, formato `^SUB-\d{3}$` | "El suscriptor es obligatorio" | Sí (400) |
| `conductionData.officeName` | Requerido, no vacío | "La oficina es obligatoria" | Sí (400) |
| `businessType` | Requerido, debe estar en la lista configurada en `appsettings.json` | "Tipo de negocio inválido. Valores permitidos: {lista}" | Sí (400) |
| `riskClassification` | Requerido, debe ser valor válido del catálogo | "Clasificación de riesgo inválida" | Sí (400) |
| `version` | Requerido, entero > 0, debe coincidir con versión persistida | "Conflicto de versión" | Sí (409) |
| `insuredData.email` | Opcional, si presente debe ser email válido | "El correo electrónico no tiene formato válido" | Sí (400) |
| `insuredData.phone` | Opcional, si presente max 20 chars | "El teléfono no tiene formato válido" | Sí (400) |
| `conductionData.branchOffice` | Opcional | — | No |

---

## 3. DISEÑO TÉCNICO

### 3.1 Clasificación y flujo de agentes

```
feature_type:         full-stack
requires_design_spec: true

Flujo de ejecución:
  Fase 0.5 (ux-designer):    APLICA — formulario de datos generales (wizard step 1)
  Fase 1.5 (core-ohs):       NO APLICA — endpoints de catálogos ya implementados (SPEC-001)
  Fase 1.5 (business-rules): NO APLICA
  Fase 1.5 (database-agent): NO APLICA — repositorio ya implementado (SPEC-002)
  Fase 2 integration:        APLICA — valida contratos: agents, subscribers, risk-classification (mock ↔ cliente HTTP)
  Fase 2 backend-developer:  APLICA — Use Cases + Controller + Validadores + Configuración
  Fase 2 frontend-developer: APLICA — página, formulario, queries de catálogos

Bloqueos de ejecución:
  - frontend-developer NO puede iniciar si design_spec.status != APPROVED
  - backend-developer puede iniciar inmediatamente tras spec.status == APPROVED
  - integration puede iniciar en paralelo con backend-developer tras spec.status == APPROVED
```

### 3.2 Design Spec

```
Status:  PENDING
Path:    .github/design-specs/general-info-management.design.md
Agente:  ux-designer (Fase 0.5)

Pantallas / vistas involucradas:
  - GeneralInfoPage (/quotes/{folio}/general-info): Formulario de datos generales del asegurado y conducción

Flujos de usuario a diseñar:
  - Carga inicial: GET datos existentes + GET catálogos → poblar formulario
  - Edición: modificar campos → validación local → PUT → confirmación
  - Error de versión: mostrar alerta "El folio fue modificado" con botón recargar

Inputs de comportamiento que el ux-designer debe conocer:
  - Selector de suscriptor (dropdown) con datos de GET /v1/subscribers
  - Campo agente: text input con búsqueda/validación contra GET /v1/agents?code=X
  - Selector de tipo de negocio: 3 opciones fijas (commercial, industrial, residential) con labels en español
  - Selector de clasificación de riesgo: dropdown con datos de GET /v1/catalogs/risk-classification
  - Todos los strings de UI en español (ADR-008)
```

### 3.3 Modelo de dominio

No se crean entidades ni value objects nuevos. Se reutilizan los de SPEC-002:

- `PropertyQuote` — entity, aggregate root
- `InsuredData` — value object: `Name`, `TaxId`, `Email?`, `Phone?`
- `ConductionData` — value object: `SubscriberCode`, `OfficeName`, `BranchOffice?`
- `QuoteStatus` — constants

**New Application DTOs:**

```csharp
// Cotizador.Application/DTOs/GeneralInfoDto.cs
public record GeneralInfoDto(
    InsuredDataDto InsuredData,
    ConductionDataDto ConductionData,
    string AgentCode,
    string BusinessType,
    string RiskClassification,
    int Version
);

// Cotizador.Application/DTOs/InsuredDataDto.cs
public record InsuredDataDto(
    string Name,
    string TaxId,
    string? Email,
    string? Phone
);

// Cotizador.Application/DTOs/ConductionDataDto.cs
public record ConductionDataDto(
    string SubscriberCode,
    string OfficeName,
    string? BranchOffice
);

// Cotizador.Application/DTOs/UpdateGeneralInfoRequest.cs
public record UpdateGeneralInfoRequest(
    InsuredDataDto InsuredData,
    ConductionDataDto ConductionData,
    string AgentCode,
    string BusinessType,
    string RiskClassification,
    int Version
);
```

**New Configuration class:**

```csharp
// Cotizador.Application/Settings/BusinessTypeSettings.cs
public class BusinessTypeSettings
{
    public List<string> AllowedValues { get; set; } = new() { "commercial", "industrial", "residential" };
}
```

**appsettings.json addition:**

```json
{
  "BusinessTypes": {
    "AllowedValues": ["commercial", "industrial", "residential"]
  }
}
```

### 3.4 Contratos API (backend)

```
GET /v1/quotes/{folio}/general-info
Propósito: Consultar los datos generales de la cotización
Auth: Basic Auth ([Authorize])
Use Case: GetGeneralInfoUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync()
Servicios externos: Ninguno

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001 (validado con regex ^DAN-\d{4}-\d{5}$)

Response 200:
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
    "riskClassification": "standard",
    "version": 3
  }
}

Response 200 (folio sin datos generales — recién creado):
{
  "data": {
    "insuredData": {
      "name": "",
      "taxId": "",
      "email": null,
      "phone": null
    },
    "conductionData": {
      "subscriberCode": "",
      "officeName": "",
      "branchOffice": null
    },
    "agentCode": "",
    "businessType": "",
    "riskClassification": "",
    "version": 1
  }
}

Response 400: { "type": "validationError", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN", "field": "folio" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

```
PUT /v1/quotes/{folio}/general-info
Propósito: Guardar/actualizar datos generales (actualización parcial con versionado optimista)
Auth: Basic Auth ([Authorize])
Use Case: UpdateGeneralInfoUseCase
Repositorios: IQuoteRepository.GetByFolioNumberAsync(), IQuoteRepository.UpdateGeneralInfoAsync()
Servicios externos: ICoreOhsClient.GetAgentByCodeAsync()

Request:
  Headers:
    Authorization: Basic dXNlcjpwYXNz
    Content-Type: application/json
    X-Correlation-Id: (opcional, UUID v4)
  Path params:
    folio: DAN-2026-00001
  Body:
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

Response 200:
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
    "riskClassification": "standard",
    "version": 2
  }
}

Response 400: { "type": "validationError", "message": "El nombre del asegurado es obligatorio", "field": "insuredData.name" }
Response 400: { "type": "validationError", "message": "Tipo de negocio inválido. Valores permitidos: commercial, industrial, residential", "field": "businessType" }
Response 401: { "type": "unauthorized", "message": "Credenciales inválidas o ausentes", "field": null }
Response 404: { "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe", "field": null }
Response 409: { "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar", "field": null }
Response 422: { "type": "invalidQuoteState", "message": "El agente AGT-999 no está registrado en el catálogo", "field": null }
Response 503: { "type": "coreOhsUnavailable", "message": "Servicio de catálogos no disponible, intente más tarde", "field": null }
Response 500: { "type": "internal", "message": "Error interno del servidor", "field": null }
```

### 3.5 Contratos core-ohs consumidos

```
GET /v1/subscribers
Propósito: Obtener catálogo de suscriptores (frontend consume directo para poblar selector)
Response 200: { "data": [{ "code": "SUB-001", "name": "María González López", "office": "CDMX Central", "active": true }, ...] }
Fixture: cotizador-core-mock/src/fixtures/subscribers.json
Datos extraídos: code, name, office
Mapeado a: ConductionData.SubscriberCode, ConductionData.OfficeName
Manejo de error: Timeout/5xx → alerta global "Servicio no disponible"
```

```
GET /v1/agents?code={code}
Propósito: Validar existencia de agente (backend valida antes de persistir)
Response 200: { "data": { "code": "AGT-001", "name": "Roberto Hernández", "region": "Centro", "active": true } }
Response 404: { "type": "AgentNotFoundException", "message": "Agent AGT-999 not found" }
Fixture: cotizador-core-mock/src/fixtures/agents.json
Datos extraídos: code (existencia)
Mapeado a: PropertyQuote.AgentCode
Manejo de error: 404 de core → InvalidQuoteStateException (422 al cliente con mensaje en español)
```

```
GET /v1/catalogs/risk-classification
Propósito: Obtener clasificaciones de riesgo (frontend consume para selector)
Response 200: { "data": [{ "code": "standard", "description": "Standard risk", "factor": 1.0 }, ...] }
Fixture: cotizador-core-mock/src/fixtures/riskClassification.json
Datos extraídos: code, description
Mapeado a: PropertyQuote.RiskClassification
Manejo de error: Timeout/5xx → alerta global
```

### 3.6 Estructura frontend (FSD)

```
cotizador-webapp/src/
├── pages/
│   └── general-info/
│       ├── index.ts                           # CREAR — Public API
│       └── ui/
│           └── GeneralInfoPage.tsx             # CREAR — Ensamblado: GeneralInfoForm widget
├── widgets/
│   └── general-info-form/
│       ├── index.ts                            # CREAR — Public API
│       └── ui/
│           └── GeneralInfoForm.tsx             # CREAR — Formulario completo con secciones
├── features/
│   └── save-general-info/
│       ├── index.ts                            # CREAR — Public API
│       ├── model/
│       │   └── useSaveGeneralInfo.ts           # CREAR — useMutation(PUT /v1/quotes/{folio}/general-info)
│       ├── ui/
│       │   └── SaveGeneralInfoButton.tsx       # CREAR — Botón "Guardar" con loading
│       └── strings.ts                          # CREAR — Strings de la acción en español
├── entities/
│   ├── folio/
│   │   └── api/
│   │       └── folioApi.ts                     # MODIFICAR — agregar getGeneralInfo(), updateGeneralInfo()
│   ├── general-info/
│   │   ├── index.ts                            # CREAR — Public API
│   │   ├── model/
│   │   │   ├── types.ts                        # CREAR — GeneralInfoDto, UpdateGeneralInfoRequest, FormValues
│   │   │   ├── useGeneralInfoQuery.ts          # CREAR — useQuery(['general-info', folio])
│   │   │   └── generalInfoSchema.ts            # CREAR — Zod schema para validación del form
│   │   ├── api/
│   │   │   └── generalInfoApi.ts               # CREAR — getGeneralInfo(), updateGeneralInfo()
│   │   └── strings.ts                          # CREAR — Etiquetas: "Nombre del asegurado", "RFC", etc.
│   ├── subscriber/
│   │   ├── index.ts                            # CREAR — Public API
│   │   ├── model/
│   │   │   ├── types.ts                        # CREAR — SubscriberDto
│   │   │   └── useSubscribersQuery.ts          # CREAR — useQuery(['subscribers'], staleTime: 30min)
│   │   └── api/
│   │       └── subscriberApi.ts                # CREAR — getSubscribers()
│   ├── agent/
│   │   ├── index.ts                            # CREAR — Public API
│   │   ├── model/
│   │   │   ├── types.ts                        # CREAR — AgentDto
│   │   │   └── useAgentQuery.ts                # CREAR — useQuery(['agent', code], enabled: !!code)
│   │   └── api/
│   │       └── agentApi.ts                     # CREAR — getAgentByCode()
│   └── risk-classification/
│       ├── index.ts                            # CREAR — Public API
│       ├── model/
│       │   ├── types.ts                        # CREAR — RiskClassificationDto
│       │   └── useRiskClassificationsQuery.ts  # CREAR — useQuery(['risk-classifications'], staleTime: 30min)
│       └── api/
│           └── riskClassificationApi.ts        # CREAR — getRiskClassifications()
└── shared/
    └── api/
        └── endpoints.ts                        # MODIFICAR — agregar rutas de general-info, subscribers, agents, risk-classifications
```

**Props/hooks por componente:**

| Componente | Props | Hooks / queries | Acción |
|---|---|---|---|
| `GeneralInfoPage` | — | `useParams()` para `folio` | Ensambla `GeneralInfoForm` |
| `GeneralInfoForm` | `folio: string` | `useGeneralInfoQuery`, `useSubscribersQuery`, `useRiskClassificationsQuery`, React Hook Form + Zod | Formulario con secciones, validación, guardado |
| `SaveGeneralInfoButton` | `onSave: () => void`, `isLoading: boolean` | — | Botón submit del form |
| `useGeneralInfoQuery` | `folio: string` | TanStack Query `['general-info', folio]` | Carga datos existentes, staleTime: 0 |
| `useSaveGeneralInfo` | `folio: string` | useMutation PUT | Guarda, invalida query, dispatch alerta en error |

### 3.7 Estado y queries

| Tipo | Herramienta | Key / Slice | Datos | Invalidación |
|---|---|---|---|---|
| Server state | TanStack Query | `['general-info', folio]` | `GeneralInfoDto` | Al mutar (PUT exitoso) |
| Server state | TanStack Query | `['subscribers']` | `SubscriberDto[]` | staleTime: 30min, no invalida |
| Server state | TanStack Query | `['agent', code]` | `AgentDto` | enabled: `!!code`, staleTime: 5min |
| Server state | TanStack Query | `['risk-classifications']` | `RiskClassificationDto[]` | staleTime: 30min, no invalida |
| UI state | Redux | `quoteWizardSlice.stepsCompleted[1]` | `boolean` | `markComplete(1)` tras PUT exitoso |
| Form state | React Hook Form | `generalInfoForm` | `GeneralInfoFormValues` | On submit + `useFormPersist` (ADR-007) |

### 3.8 Persistencia MongoDB

| Operación | Colección | Tipo | Filtro | Proyección | Índice requerido |
|---|---|---|---|---|---|
| Read (GET) | `property_quotes` | `Find` | `{ folioNumber }` | Full document (extraer sección general-info + version) | `folioNumber_1` (existing) |
| Update (PUT) | `property_quotes` | `UpdateOne` | `{ folioNumber, version: N }` | `$set: insuredData, conductionData, agentCode, businessType, riskClassification, quoteStatus (condicional), version: N+1, metadata.updatedAt, metadata.lastWizardStep: 1` | `folioNumber_1` (existing) |

- **Versionado optimista**: filtro por `{ folioNumber, version }`. Si `ModifiedCount == 0` → `VersionConflictException`.
- **Transición de estado**: Si `quoteStatus == "draft"` → incluir `$set: { quoteStatus: "in_progress" }`.
- **Actualización parcial**: No toca `locations`, `coverageOptions`, `layoutConfiguration`, `netPremium`, `commercialPremium`, `premiumsByLocation`.
- **Nota**: `UpdateGeneralInfoAsync` en `IQuoteRepository` ya está definido e implementado en SPEC-002.

**Cambio requerido en `UpdateGeneralInfoAsync`**: Agregar lógica condicional para transicionar `quoteStatus` de `draft` a `in_progress`. El repositorio actual no hace esta transición. Opciones:
1. El Use Case lee el folio antes de actualizar y, si es `draft`, agrega `quoteStatus: "in_progress"` al update — **preferido**, la decisión de negocio vive en Application.
2. Se pasa un parámetro adicional `string? newQuoteStatus` al método del repositorio.

**Decisión**: Opción 2 — agregar parámetro opcional `string? newQuoteStatus = null` a `UpdateGeneralInfoAsync`. Si es non-null, se incluye en el `$set`.

---

## 4. LÓGICA DE CÁLCULO

N/A — este feature no involucra cálculo.

---

## 5. MODELO DE DATOS

### 5.1 Colecciones afectadas

| Colección | Operación | Campos modificados |
|---|---|---|
| `property_quotes` | Read (GET) + UpdateOne (PUT) | `insuredData`, `conductionData`, `agentCode`, `businessType`, `riskClassification`, `quoteStatus` (condicional), `version`, `metadata.updatedAt`, `metadata.lastWizardStep` |

### 5.2 Cambios de esquema

Ninguno. Los campos ya están definidos en SPEC-002.

### 5.3 Índices requeridos

Ya definidos en SPEC-002. No se crean índices nuevos.

### 5.4 Datos semilla

Ninguno. Los catálogos (suscriptores, agentes, clasificaciones de riesgo) ya existen como fixtures en `cotizador-core-mock` (SPEC-001).

---

## 6. SUPUESTOS Y LIMITACIONES

| ID | Supuesto | Justificación | Impacto si es incorrecto | Aprobado por |
|---|---|---|---|---|
| SUP-004-01 | El backend valida que `agentCode` exista en core-ohs antes de persistir (422 si no existe) | REQ-04 dice "buscar y seleccionar un agente" — implica que el agente debe ser válido | Si no se valida, se podrían guardar códigos de agente inexistentes | usuario |
| SUP-004-02 | `quoteStatus` transiciona de `"draft"` a `"in_progress"` al guardar la primera sección (general info) | `architecture-decisions.md` dice: "`draft` → `in_progress` al guardar primera sección" | Si la transición ocurre en otro punto, ajustar el Use Case | usuario |
| SUP-004-03 | Campos obligatorios: `insuredData.name`, `insuredData.taxId`, `agentCode`, `conductionData.subscriberCode`, `conductionData.officeName`, `businessType`, `riskClassification` | Son los datos mínimos para identificar asegurado y conducción | Si algún campo debería ser opcional, la validación se afloja | usuario |
| SUP-004-04 | Valores de `businessType` configurables vía `appsettings.json`, default: `["commercial", "industrial", "residential"]` | El usuario confirmó que estos valores son fijos pero deben ser modificables sin recompilar | Si se quiere un catálogo dinámico de core-mock, habría que crear nuevo endpoint | usuario |
| SUP-004-05 | La validación de `subscriberCode` en Oleada 2 es solo de formato (`^SUB-\d{3}$`), no contra core-ohs | Reducir acoplamiento en esta oleada. La validación completa contra el catálogo se puede agregar en oleadas futuras | Si se requiere validación estricta, agregar consulta a `GET /v1/subscribers` en el Use Case | usuario |

---

## 7. DEPENDENCIAS DE EJECUCIÓN

### 7.1 Grafo de agentes

```
[spec-generator] → APPROVED
        │
        ├── [ux-designer]        (Fase 0.5, requires_design_spec=true)
        │       └── design.status=APPROVED → desbloquea frontend-developer
        │
        ├── [integration]        (Fase 2, paralelo — valida contratos core-ohs)
        ├── [backend-developer]  (Fase 2, no bloqueado — SPEC-002 ya implementó Domain + Repository)
        └── [frontend-developer] (Fase 2, BLOQUEADO hasta design.status=APPROVED)
                │
                ├── [test-engineer-backend]   (Fase 3, paralelo)
                └── [test-engineer-frontend]  (Fase 3, paralelo)
```

### 7.2 Tabla de bloqueos

| Agente | Bloqueado por | Condición de desbloqueo |
|---|---|---|
| `ux-designer` | `spec-generator` | `specs/general-info-management.spec.md` → `status: APPROVED` |
| `integration` | `spec-generator` | `specs/general-info-management.spec.md` → `status: APPROVED` |
| `backend-developer` | `spec-generator` | `specs/general-info-management.spec.md` → `status: APPROVED` |
| `frontend-developer` | `ux-designer` | `design-specs/general-info-management.design.md` → `status: APPROVED` |
| `test-engineer-backend` | `backend-developer` | Implementación backend completa |
| `test-engineer-frontend` | `frontend-developer` | Implementación frontend completa |

### 7.3 Specs relacionadas

| Spec ID | Feature | Tipo de relación |
|---|---|---|
| SPEC-001 | core-reference-service | depende-de (endpoints de catálogos: subscribers, agents, risk-classification) |
| SPEC-002 | quote-data-model | depende-de (entidades, repositorio `UpdateGeneralInfoAsync`, excepciones) |
| SPEC-003 | folio-creation | depende-de (el folio debe existir antes de capturar datos generales) |

---

## 8. LISTA DE TAREAS

### 8.1 integration

- [ ] Documentar contrato `GET /v1/agents?code={code}` en `.github/docs/integration-contracts.md`: request, response 200, response 404
- [ ] Documentar contrato `GET /v1/subscribers` en `.github/docs/integration-contracts.md`: request, response 200
- [ ] Documentar contrato `GET /v1/catalogs/risk-classification` en `.github/docs/integration-contracts.md`: request, response 200
- [ ] Verificar que `cotizador-core-mock/src/routes/agentRoutes.ts` responde 404 con `{ "type": "AgentNotFoundException", "message": "..." }` cuando el agente no existe
- [ ] Verificar que `cotizador-core-mock/src/routes/subscriberRoutes.ts` retorna `{ "data": [...] }` con campos `code`, `name`, `office`, `active`
- [ ] Verificar que `cotizador-core-mock/src/routes/catalogRoutes.ts` ruta `/risk-classification` retorna `{ "data": [...] }` con campos `code`, `description`, `factor`
- [ ] Verificar que `CoreOhsClient.GetAgentByCodeAsync()` mapea correctamente response 200 → `AgentDto` y response 404 → `null`
- [ ] Verificar que `CoreOhsClient.GetSubscribersAsync()` mapea a `List<SubscriberDto>`
- [ ] Verificar que `CoreOhsClient.GetRiskClassificationsAsync()` mapea a `List<RiskClassificationDto>`
- [ ] Reportar CONTRACT_DRIFT si hay discrepancias entre mock, cliente HTTP y spec §3.5

### 8.2 backend-developer

- [ ] Crear `BusinessTypeSettings` en `Cotizador.Application/Settings/`
- [ ] Agregar sección `"BusinessTypes"` en `appsettings.json` y `appsettings.Development.json`
- [ ] Registrar `BusinessTypeSettings` en `Program.cs` con `Configure<BusinessTypeSettings>()`
- [ ] Crear `IGetGeneralInfoUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `GetGeneralInfoUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ILogger<GetGeneralInfoUseCase>`
  - Flujo: buscar por folioNumber → si null throw FolioNotFoundException → mapear sección a GeneralInfoDto
- [ ] Crear `IUpdateGeneralInfoUseCase` en `Cotizador.Application/Interfaces/`
- [ ] Implementar `UpdateGeneralInfoUseCase` en `Cotizador.Application/UseCases/`
  - Inyecta: `IQuoteRepository`, `ICoreOhsClient`, `BusinessTypeSettings`, `ILogger<UpdateGeneralInfoUseCase>`
  - Flujo: validar businessType contra config → validar agentCode contra core-ohs → leer folio para verificar si es draft → llamar UpdateGeneralInfoAsync con quoteStatus condicional → retornar DTO actualizado
- [ ] Crear DTOs: `GeneralInfoDto`, `InsuredDataDto`, `ConductionDataDto`, `UpdateGeneralInfoRequest` en `Cotizador.Application/DTOs/`
- [ ] Crear validador FluentValidation `UpdateGeneralInfoRequestValidator`
  - name: requerido, max 200
  - taxId: requerido, formato RFC
  - agentCode: requerido, formato `^AGT-\d{3}$`
  - subscriberCode: requerido, formato `^SUB-\d{3}$`
  - officeName: requerido
  - businessType: requerido, debe estar en `BusinessTypeSettings.AllowedValues`
  - riskClassification: requerido
  - version: requerido, > 0
- [ ] Modificar `IQuoteRepository.UpdateGeneralInfoAsync` — agregar parámetro opcional `string? newQuoteStatus = null`
- [ ] Modificar `QuoteRepository.UpdateGeneralInfoAsync` — si `newQuoteStatus` != null, incluir en `$set`
- [ ] Crear `QuoteController` en `Cotizador.API/Controllers/` (o extender el creado en SPEC-003)
  - `GET /v1/quotes/{folio}/general-info`
  - `PUT /v1/quotes/{folio}/general-info`
- [ ] Registrar Use Cases en `Program.cs`
- [ ] Mensajes de error en español (ADR-008)

### 8.3 frontend-developer

- [ ] Crear `entities/general-info/` — types, schema Zod, query, api
- [ ] Crear `entities/subscriber/` — types, query (staleTime 30min), api
- [ ] Crear `entities/agent/` — types, query (enabled: !!code), api
- [ ] Crear `entities/risk-classification/` — types, query (staleTime 30min), api
- [ ] Crear `features/save-general-info/` — `useSaveGeneralInfo` mutation + `SaveGeneralInfoButton`
- [ ] Crear `widgets/general-info-form/` — formulario con React Hook Form + Zod, secciones: asegurado, conducción, agente, negocio, riesgo
- [ ] Crear `pages/general-info/` — `GeneralInfoPage` ensambla el widget
- [ ] Crear `shared/lib/useFormPersist.ts` — hook genérico que serializa `form.getValues()` a `sessionStorage` cada 3s (debounced), restaura al montar, y limpia tras `mutation.onSuccess` (implementación definida en ADR-007)
- [ ] Integrar `useFormPersist` en el formulario de general-info con key `wizard:{folio}:step:1`
- [ ] Agregar ruta `/quotes/:folio/general-info` en `app/router/router.tsx`
- [ ] Agregar endpoints en `shared/api/endpoints.ts`
- [ ] Labels, placeholders, mensajes de error y validación en español (ADR-008)
- [ ] Strings en: `entities/general-info/strings.ts`, `features/save-general-info/strings.ts`

### 8.4 test-engineer-backend

- [ ] `GetGeneralInfoUseCaseTests` — folio con datos → retorna DTO mapeado
- [ ] `GetGeneralInfoUseCaseTests` — folio sin datos (recién creado) → retorna campos vacíos
- [ ] `GetGeneralInfoUseCaseTests` — folio inexistente → throws FolioNotFoundException
- [ ] `UpdateGeneralInfoUseCaseTests` — datos válidos, agente existe → actualización exitosa, version+1
- [ ] `UpdateGeneralInfoUseCaseTests` — agente inexistente en core → throws InvalidQuoteStateException
- [ ] `UpdateGeneralInfoUseCaseTests` — businessType inválido → throws ValidationException
- [ ] `UpdateGeneralInfoUseCaseTests` — version mismatch → throws VersionConflictException
- [ ] `UpdateGeneralInfoUseCaseTests` — folio en draft → transiciona a in_progress
- [ ] `UpdateGeneralInfoUseCaseTests` — folio ya in_progress → no cambia quoteStatus
- [ ] `UpdateGeneralInfoUseCaseTests` — core-ohs no disponible → throws CoreOhsUnavailableException
- [ ] `UpdateGeneralInfoRequestValidatorTests` — name vacío → invalid
- [ ] `UpdateGeneralInfoRequestValidatorTests` — taxId formato incorrecto → invalid
- [ ] `UpdateGeneralInfoRequestValidatorTests` — agentCode formato incorrecto → invalid
- [ ] `QuoteControllerTests` — GET general-info folio existente → 200
- [ ] `QuoteControllerTests` — PUT general-info válido → 200 con version incrementada
- [ ] `QuoteControllerTests` — PUT general-info folio inválido → 400

### 8.5 test-engineer-frontend

- [ ] `GeneralInfoForm.test.tsx` — renderiza campos, carga catálogos via mock
- [ ] `GeneralInfoForm.test.tsx` — validación Zod: nombre vacío muestra error
- [ ] `GeneralInfoForm.test.tsx` — validación Zod: RFC inválido muestra error
- [ ] `GeneralInfoForm.test.tsx` — submit exitoso invoca mutación y marca step complete
- [ ] `useGeneralInfoQuery.test.ts` — fetch correcto mapea respuesta
- [ ] `useSaveGeneralInfo.test.ts` — mutación exitosa invalida query
- [ ] `useSaveGeneralInfo.test.ts` — error 409 muestra alerta de conflicto de versión
- [ ] `generalInfoSchema.test.ts` — validaciones de campos requeridos y formatos

---

## 9. CRITERIOS DE ACEPTACIÓN DEL FEATURE

**DoR (Definition of Ready)** — antes de iniciar implementación:
- [ ] Spec en estado `APPROVED`
- [ ] Design spec en estado `APPROVED` (bloquea frontend)
- [ ] Todos los supuestos aprobados por el usuario
- [ ] SPEC-001 implementada (core-mock con endpoints de catálogos)
- [ ] SPEC-002 implementada (Domain + Repository)
- [ ] SPEC-003 implementada (folio existe para poder capturar datos generales)

**DoD (Definition of Done)** — para considerar el feature terminado:
- [ ] `GET /v1/quotes/{folio}/general-info` responde según contrato §3.4
- [ ] `PUT /v1/quotes/{folio}/general-info` responde según contrato §3.4 (todos los códigos de error)
- [ ] Validación de agente contra core-ohs funciona (422 si no existe)
- [ ] `businessType` validado contra configuración de `appsettings.json`
- [ ] Transición `draft` → `in_progress` al guardar primera sección
- [ ] Versionado optimista funcional (409 ante conflicto)
- [ ] `metadata.lastWizardStep` se actualiza a 1
- [ ] Mensajes de error en español (ADR-008)
- [ ] Frontend: formulario con catálogos, validación Zod, guardado con feedback visual
- [ ] `useFormPersist` integrado (ADR-007)
- [ ] Tests unitarios BE y FE pasando
- [ ] Sin violaciones de Clean Architecture (`API → Application → Domain ← Infrastructure`)
- [ ] Sin violaciones de reglas FSD
