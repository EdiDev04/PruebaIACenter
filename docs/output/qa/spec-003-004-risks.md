# spec: SPEC-003 + SPEC-004
# feature: folio-creation + general-info-management
# date: 2026-03-29
# author: qa-agent
# quality-gate: PASSED

# Matriz de Riesgos — SPEC-003 (Folio Creation) + SPEC-004 (General Info Management)

## Resumen Ejecutivo

| Métrica        | Valor |
|----------------|-------|
| Total riesgos  | 14    |
| Nivel **ALTO** (A) — Testing Obligatorio  | 8  |
| Nivel **MEDIO** (S) — Testing Recomendado | 4  |
| Nivel **BAJO** (D) — Testing Opcional    | 2  |

> **Regla ASD:**
> - **ALTO (A)**: bloquea el release. Tests son obligatorios antes de promover a producción.
> - **MEDIO (S)**: recomendado. Si se omite, debe documentarse la deuda técnica y la aceptación de riesgo.
> - **BAJO (D)**: opcional. Priorizar en backlog una vez cubiertos los niveles superiores.

---

## Detalle de Riesgos

| ID     | HU            | Spec     | Área           | Descripción del Riesgo                                                                  | Factores                                           | Nivel | Testing requerido |
|--------|---------------|----------|----------------|-----------------------------------------------------------------------------------------|----------------------------------------------------|-------|-------------------|
| R-001  | HU-003-01     | SPEC-003 | Idempotencia   | El mecanismo de idempotencia falla y crea folios duplicados con el mismo `Idempotency-Key` | Integraciones externas, datos persistidos, concurrencia MongoDB | **A** | Obligatorio |
| R-002  | HU-003-04     | SPEC-003 | Resiliencia    | core-mock disponible intermitentemente: el backend no reintenta o lo hace más de una vez, causando inconsistencia | Dependencia sistema externo, SLA implícito         | **A** | Obligatorio |
| R-003  | HU-004-05     | SPEC-004 | Versionado     | Conflicto de versión no detectado: dos procesos simultáneos sobreescriben el mismo folio, perdiendo cambios | Concurrencia MongoDB, lógica de negocio crítica    | **A** | Obligatorio |
| R-004  | HU-004-03     | SPEC-004 | Integración    | Drift de contrato entre backend y core-mock para `GET /v1/agents?code=X`: formato del query param o respuesta cambia sin notificación | Integración externa, código nuevo sin historial     | **A** | Obligatorio |
| R-005  | HU-004-05     | SPEC-004 | Estado negocio | La transición `draft → in_progress` no se ejecuta en la primera escritura exitosa, o se ejecuta en escrituras que fallan (no atómico) | Lógica de negocio compleja, operación atómica MongoDB | **A** | Obligatorio |
| R-006  | HU-003-01     | SPEC-003 | Seguridad      | El header `Idempotency-Key` acepta valores no-UUID, permitiendo colisiones intencionales o ataques de enumeración | Validación de entrada, seguridad                   | **A** | Obligatorio |
| R-007  | HU-004-01     | SPEC-004 | Validación     | Campo `taxId` acepta RFC con formato incorrecto (regex insuficiente): personas físicas vs. morales tienen formatos distintos (12 vs. 13 chars) | Lógica de negocio compleja, código nuevo           | **A** | Obligatorio |
| R-008  | HU-004-01/02  | SPEC-004 | Datos nullable | Campos opcionales (`email`, `phone`, `branchOffice`) se persisten como string vacío `""` en lugar de `null`, rompiendo consultas de "sin valor" | Datos nullables, lógica de persistencia            | **A** | Obligatorio |
| R-009  | HU-004-04     | SPEC-004 | Configuración  | Los `allowedValues` de `businessType` en `appsettings.json` no son leídos en runtime, o no se invalida la caché tras cambio de configuración | Código nuevo sin historial, componente con dependencias | **S** | Recomendado |
| R-010  | HU-003-03     | SPEC-003 | Frontend       | La lógica de redirección wizard usa `lastWizardStep: 0` y no redirige correctamente a step 1 (borde: step 0 → step 1 implícito) | Código nuevo, funcionalidad de alta frecuencia     | **S** | Recomendado |
| R-011  | HU-004-02     | SPEC-004 | Frontend/UI    | El autocompletado de `officeName` al seleccionar suscriptor falla si el catálogo de core-mock tarda o está unavailable, dejando el campo vacío | Dependencia externa, componente con dependencias   | **S** | Recomendado |
| R-012  | HU-004-05     | SPEC-004 | Frontend       | El UI no muestra el diálogo de "Recargue para continuar" en respuesta 409, o lo hace pero borra el formulario del usuario | Alta frecuencia de uso, UX crítica                 | **S** | Recomendado |
| R-013  | HU-003-02     | SPEC-003 | Validación     | La validación de formato del `folioNumber` en path param es tolerante a variaciones (e.g., `DAN-2026-001` pasa en vez de rechazarse) | Refactorización sin cambio histórico, regex nuevo  | **D** | Opcional    |
| R-014  | HU-004-04     | SPEC-004 | UI             | Los labels del selector de `businessType` muestran el valor técnico (`"commercial"`) en vez del label en español, violando ADR-008 | Ajuste de UI, sin impacto funcional directo        | **D** | Opcional    |

---

## Plan de Mitigación — Riesgos ALTO

### R-001: Idempotencia — creación de folios duplicados
- **Condición de fallo**: la clave de idempotencia no está indexada en MongoDB, permitiendo race condition en inserciones concurrentes.
- **Mitigación técnica**:
  - Índice único en `metadata.idempotencyKey` en la colección `property_quotes`.
  - Test de integración con dos POSTs concurrentes con el mismo `Idempotency-Key`.
  - Test unitario de `GetByIdempotencyKeyAsync` + `CreateAsync` con verificación del documento persisted.
- **Tests obligatorios**: test de integración con concurrencia (2 hilos), test unitario de `CreateFolioUseCase`.
- **Bloqueante para release**: ✅ Sí

---

### R-002: Resiliencia ante indisponibilidad de core-mock
- **Condición de fallo**: el cliente HTTP no tiene timeout configurado, o el retry policy está mal configurado (más de 1 retry, o delay distinto a 500ms).
- **Mitigación técnica**:
  - Verificar configuración de Polly: `RetryCount = 1`, `WaitAndRetry(500ms)`.
  - Mock de `ICoreOhsClient` que lanza `TaskCanceledException` en el primer llamado.
  - Validar que no se persiste nada en MongoDB si ambos intentos fallan.
- **Tests obligatorios**: test unitario de `CreateFolioUseCase` con `ICoreOhsClient` mockeado para timeout.
- **Bloqueante para release**: ✅ Sí

---

### R-003: Conflicto de versión — pérdida de datos por concurrencia
- **Condición de fallo**: la condición de filtro en MongoDB (`{ folioNumber, version }`) no está correctamente configurada, y el `$set` se aplica aunque la versión cambió.
- **Mitigación técnica**:
  - Test unitario de `UpdateGeneralInfoUseCase` con versión stale: verificar `VersionConflictException`.
  - Test de integración: folio en versión 4, enviar PUT con versión 3 → expect 409.
  - Revisar que `UpdatedResult.MatchedCount == 0` dispara la excepción.
- **Tests obligatorios**: test unitario de use case + test de integración de endpoint.
- **Bloqueante para release**: ✅ Sí

---

### R-004: Contract drift entre backend y core-mock para validación de agente
- **Condición de fallo**: core-mock cambia la estructura de respuesta de `GET /v1/agents?code=X` (nuevo campo, renaming, paginación) y el cliente HTTP del backend lo parsea incorrectamente.
- **Mitigación técnica**:
  - Test de contrato (PACT o snapshot) entre `ICoreOhsClient` y el fixture `agents.json` de core-mock.
  - Validar que la deserialización produce un objeto con campo `code` y `name`.
- **Tests obligatorios**: test de integración contra core-mock levantado localmente con fixture `agents.json`.
- **Bloqueante para release**: ✅ Sí

---

### R-005: Transición de estado `draft → in_progress` no atómica
- **Condición de fallo**: el `$set: { quoteStatus: "in_progress" }` no está en la misma operación de MongoDB que el `$set` de `generalInfo`, permitiendo que el status quede en `draft` si la operación parcial falla.
- **Mitigación técnica**:
  - Verificar que el repositorio usa una sola operación `UpdateOneAsync` con todos los campos en un único `$set`.
  - Test unitario de `UpdateGeneralInfoUseCase` en folio `draft`: verificar que el documento resultante tiene `quoteStatus = "in_progress"` y `version` incrementado en la misma operación.
- **Tests obligatorios**: test unitario de use case con folio en estado `draft`.
- **Bloqueante para release**: ✅ Sí

---

### R-006: Validación insuficiente del header `Idempotency-Key`
- **Condición de fallo**: el backend valida solo la presencia del header pero no su formato UUID v4, permitiendo valores arbitrarios que podrían colisionar o ser explotados.
- **Mitigación técnica**:
  - Agregar validación de regex `^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-4[0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}$`.
  - Test con valores no-UUID: strings cortos, sin guiones, con caracteres especiales.
- **Tests obligatorios**: test unitario de `CreateFolioUseCase` con inputs no-UUID.
- **Bloqueante para release**: ✅ Sí

---

### R-007: RFC con formato incorrecto acepta valores inválidos
- **Condición de fallo**: la regex de RFC no distingue entre persona física (13 chars: 4 letras + 6 dígitos + 3 alfanuméricos) y persona moral (12 chars: 3 letras + 6 dígitos + 3 alfanuméricos), o no rechaza caracteres especiales.
- **Mitigación técnica**:
  - Definir regex canónica: `^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$` (FluentValidation).
  - Test con valores válidos: `GIN850101AAA` (moral), `PECR801120M25` (física).
  - Test con valores inválidos: `123`, `PECR-801120`, `GIN850101AAA1234`.
- **Tests obligatorios**: test unitario del validador `UpdateGeneralInfoRequestValidator`.
- **Bloqueante para release**: ✅ Sí

---

### R-008: Campos opcionales persistidos como string vacío en vez de null
- **Condición de fallo**: el binding del body ASP.NET Core convierte `null` a `""` en `string?`, y el mapeo al dominio persiste `""` en MongoDB en lugar de omitir el campo.
- **Mitigación técnica**:
  - Verificar el comportamiento del record `InsuredDataDto` con valores `null` en JSON.
  - Test unitario: enviar `email: null` en el body → verificar que el documento MongoDB tiene `email: null` (no `""`).
  - Considerar `[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]` si aplica.
- **Tests obligatorios**: test de integración contra MongoDB real o `MockMongoCollection`.
- **Bloqueante para release**: ✅ Sí
