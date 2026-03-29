# Matriz de Riesgos — SPEC-004 Proxy Catálogos
**Feature:** Endpoints proxy de catálogos (`GET /v1/subscribers`, `GET /v1/agents`, `GET /v1/catalogs/risk-classification`)  
**Spec de referencia:** `.github/specs/general-info-management.spec.md` §3.5  
**Generado por:** QA Agent  
**Fecha:** 2026-03-29

---

## Resumen Ejecutivo

| Nivel | Cantidad | Acción requerida |
|-------|----------|-----------------|
| **Alto (A)** | 5 | Testing OBLIGATORIO — bloquea release |
| **Medio (S)** | 4 | Testing RECOMENDADO — documentar si se omite |
| **Bajo (D)** | 2 | Testing OPCIONAL — priorizar en backlog |
| **Total** | **11** | |

---

## Detalle de la Matriz

| ID | HU / Endpoint | Descripción del Riesgo | Factores de Riesgo | Nivel | Acción |
|----|--------------|------------------------|-------------------|-------|--------|
| R-001 | GET /v1/subscribers | Datos incorrectos del catálogo propagados al frontend | Integración externa, alta frecuencia de uso, dato crítico para el contrato de cotización | **A** | Obligatorio |
| R-002 | GET /v1/agents · PUT general-info | Inconsistencia entre lo que el frontend muestra (proxy GET) y lo que el backend valida al persistir (PUT) | Doble punto de validación, lógica de negocio divergente, impacto financiero indirecto | **A** | Obligatorio |
| R-003 | Los 3 endpoints | Autenticación bypasseada — acceso no autorizado a catálogos | Autenticación/autorización, sin token → datos expuestos | **A** | Obligatorio |
| R-004 | GET /v1/agents | Formato de agente no validado — llamada a core-mock con datos inválidos | Integración externa llamada sin sanitizar, comportamiento indefinido en core-mock | **A** | Obligatorio |
| R-005 | Los 3 endpoints | core-mock caído sin manejo adecuado — excepción no traducida expone stack trace | Integración externa, degradación sin control, datos técnicos sensibles en respuesta | **A** | Obligatorio |
| R-006 | GET /v1/catalogs/risk-classification | Factores numéricos serialzados como string en lugar de decimal | Lógica de negocio (factores se usan en cálculo de prima), cambio de tipo afecta SPEC-005 | **S** | Recomendado |
| R-007 | GET /v1/subscribers | Suscriptor inactivo (`active: false`) mostrado en selector del frontend | Lógica de filtrado ausente, dato de negocio incorrecto propagado | **S** | Recomendado |
| R-008 | Flujo E2E wizard step 1 | Los catálogos (subscribers + risk-classification) se cargan secuencialmente en lugar de en paralelo | Alta frecuencia de uso, impacto en UX/performance del wizard | **S** | Recomendado |
| R-009 | GET /v1/agents | Código de agente inyectado en mensaje de error (R-004-09 RN) | Potencial de XSS o log injection si el código no se sanitiza antes de incluirlo en el mensaje | **S** | Recomendado |
| R-010 | GET /v1/subscribers | core-mock devuelve lista vacía — frontend muestra selector vacío sin feedback | Edge case de configuración, experiencia de usuario degradada sin indicación clara | **D** | Opcional |
| R-011 | Los 3 endpoints | Cabecera X-Correlation-Id no propagada hacia core-mock — trazabilidad de logs rota | Operacional/observabilidad, impacto limitado en producción | **D** | Opcional |

---

## Plan de Mitigación — Riesgos ALTO

### R-001: Datos incorrectos del catálogo propagados al frontend

**Descripción:** El backend actúa como passthrough de core-mock hacia el frontend. Si los datos del fixture de core-mock o el mapeo del cliente HTTP son incorrectos, el selector de suscriptores del wizard mostrará datos erróneos que el usuario tomará como válidos al guardar.

**Impacto concreto:** El usuario seleccionaría un suscriptor con nombre/oficina incorrecto, que quedaría persistido en MongoDB sin que el backend lo detecte, ya que solo verifica formato del código (RN-004-05).

**Factores:**
- Integración externa (ICoreOhsClient → core-mock)
- Dato que persiste en MongoDB (conductionData.subscriberCode + officeName)
- Alta frecuencia: se ejecuta en cada apertura del wizard step 1

**Controles técnicos:**
- Test de contrato: verificar que SubscriberDto mapea exactamente `code`, `name`, `office`, `active` desde la respuesta de core-mock
- Test de integración: levantar core-mock real y verificar que GET /v1/subscribers del backend devuelve los 3 suscriptores del fixture
- Test de snapshot: comparar respuesta de backend contra fixture de referencia

**Tests obligatorios:**
- `[Fact] GetSubscribers_Should_MapAllFields_FromCoreMockFixture` — integración
- `[Fact] GetSubscribers_Should_ReturnExactSubscriberCount` — contrato
- Escenario Gherkin: "Happy path — obtener catálogo de suscriptores" (spec-004-proxy-catalogs.gherkin.md)

**Bloqueante para release:** ✅ Sí

---

### R-002: Inconsistencia proxy GET vs. validación PUT

**Descripción:** El frontend usa GET /v1/agents?code=AGT-001 para mostrar datos del agente. Sin embargo, la validación definitiva ocurre en el PUT /v1/quotes/{folio}/general-info, donde el backend vuelve a llamar a core-mock para verificar la existencia. Si los dos puntos de validación difieren (por ejemplo, uno usa caché y el otro no, o un agente fue desactivado entre los dos llamados), el formulario podría permitir seleccionar un agente que después es rechazado al guardar.

**Impacto concreto:** El usuario completa el formulario con un agente aparentemente válido, presiona "Guardar", y recibe un 422, sin entender el motivo ya que el campo de agente mostró éxito.

**Factores:**
- Doble consulta a core-mock (GET proxy + validación en PUT)
- Condición de carrera si el agente se desactiva entre ambas llamadas
- Costo de soporte: casos de usuario confundido difíciles de reproducir

**Controles técnicos:**
- Test de integración E2E que ejercita GET /v1/agents (éxito) seguido de PUT donde ese agente ya no existe en mock
- Documentar el timeout entre GET proxy y PUT como comportamiento esperado, no bug
- Verificar que el mensaje de error 422 del PUT sea suficientemente claro para el usuario

**Tests obligatorios:**
- Escenario Gherkin: "E2E — agente válido en búsqueda pero 404 al persistir" (a implementar)
- Test de integración: `UpdateGeneralInfo_Should_Return422_WhenAgent_WasValid_But_Not_At_Persist_Time`

**Bloqueante para release:** ✅ Sí

---

### R-003: Autenticación bypasseada en endpoints proxy

**Descripción:** Los tres endpoints proxy están decorados con `[Authorize]` a nivel de controlador. Si el middleware de autenticación no está correctamente configurado en `Program.cs`, o si existe alguna ruta alternativa no protegida, un cliente podría acceder a los catálogos sin credenciales.

**Impacto concreto:** Exposición de datos del catálogo de agentes (nombres, regiones) o suscriptores a actores no autorizados. En un contexto de seguro de propiedad, estos datos son sensibles.

**Factores:**
- Autenticación/autorización (Regla ASD: ALTO automático)
- OWASP A01:2021 — Broken Access Control
- Los endpoints proxy son el único punto de acceso del frontend a los catálogos

**Controles técnicos:**
- Test de regresión de seguridad: enviar peticiones sin `Authorization` header y verificar 401
- Test: enviar `Authorization: Bearer token_invalido` (Basic Auth, no Bearer) y verificar 401
- Verificar en `Program.cs` que `app.UseAuthentication()` y `app.UseAuthorization()` están antes de `app.MapControllers()`

**Tests obligatorios:**
- `[Fact] GetSubscribers_Should_Return401_WhenNoAuthorization`
- `[Fact] GetAgentByCode_Should_Return401_WhenNoAuthorization`
- `[Fact] GetRiskClassifications_Should_Return401_WhenNoAuthorization`
- Escenarios Gherkin: bloque `@seguridad` del gherkin de proxy

**Bloqueante para release:** ✅ Sí

---

### R-004: Formato de agente no validado antes de llamar a core-mock

**Descripción:** El controlador valida el formato del código de agente (regex `^AGT-\d{3}$`) antes de llamar a core-mock. Si esta validación fuera eliminada o bypasseada (por ejemplo, enviando un código malformado con caracteres especiales), core-mock recibiría una petición con datos no sanitizados.

**Impacto concreto:**
- core-mock podría comportarse de forma indefinida con inputs mal formados
- Códigos con caracteres especiales en la query string podrían causar comportamientos inesperados
- Posible injection en logs si el código viaja sin sanitizar al mensaje de error

**Factores:**
- Validación de entrada en boundary del sistema (OWASP A03:2021 — Injection)
- Integración externa: core-mock recibe el input sin filtro si la validación falla
- El parámetro `code` aparece textualmente en el mensaje de error 404 (RN-004-09)

**Controles técnicos:**
- Verificar que la regex `^AGT-\d{3}$` está compilada con `RegexOptions.Compiled` (performance) ✅ ya implementado
- Test con payloads maliciosos: `AGT-001'; DROP TABLE`, `<script>alert(1)</script>`, `../../../etc/passwd`
- Verificar que el código se incluye en el mensaje de error a través de string interpolation segura (sin eval)

**Tests obligatorios:**
- Esquema del escenario Gherkin: "Formatos de código de agente inválidos rechazados con 400"
- `[Theory] GetAgentByCode_Should_Return400_ForAllInvalidFormats`

**Bloqueante para release:** ✅ Sí

---

### R-005: core-mock caído sin manejo adecuado

**Descripción:** Si core-mock no está disponible (timeout, conexión rechazada, HTTP 5xx), la excepción `CoreOhsUnavailableException` debe ser capturada y traducida a HTTP 503 con el envelope de error estándar. Si el middleware global de excepciones no está correctamente configurado, la excepción podría propagarse exponiendo el stack trace al cliente.

**Impacto concreto:**
- Stack trace con rutas internas, versiones de librerías y URLs de core-mock expuestas al cliente
- Información sensible de infraestructura visible públicamente
- OWASP A05:2021 — Security Misconfiguration

**Factores:**
- Integración externa (ICoreOhsClient) — dependencia no controlada
- Todos los endpoints proxy dependen de esta resiliencia
- Exposición de información de infraestructura interna

**Controles técnicos:**
- Verificar que el middleware de excepciones en `Program.cs` captura `CoreOhsUnavailableException` y retorna 503
- Verificar que el body de error NO incluye `exception`, `stackTrace`, `innerException` ni URLs internas
- Test de regresión: simular timeout en ICoreOhsClient y verificar formato de respuesta 503

**Tests obligatorios:**
- `[Fact] GetSubscribers_Should_Return503_WhenCoreOhsUnavailable` (sin stacktrace en body)
- `[Fact] GetAgentByCode_Should_Return503_WhenCoreOhsUnavailable`
- `[Fact] GetRiskClassifications_Should_Return503_WhenCoreOhsUnavailable`
- Escenarios Gherkin: "core-mock no disponible" en cada bloque

**Bloqueante para release:** ✅ Sí

---

## Plan de Mitigación — Riesgos MEDIO

### R-006: Factores numéricos serializados como string

**Mitigación:** Test de tipo de dato en response de clasificación de riesgo. Verificar que `factor` es `decimal`/`number` en JSON, no `"1.0"` string. Crítico porque SPEC-005 (cálculo de prima) consume este catálogo.

**Tests recomendados:** `GetRiskClassifications_Should_SerializeFactorAsDecimal()`

**Bloqueante para release:** ⚠️ No (pero puede bloquear SPEC-005)

---

### R-007: Suscriptores inactivos mostrados en selector

**Mitigación:** Verificar con el PO si el filtrado de `active: false` es responsabilidad del backend (proxy) o del frontend. Si el backend debe filtrar: test que verifica que suscriptores con `active: false` no aparecen en la respuesta del proxy. Los 3 suscriptores del fixture actual son activos; este riesgo aplica en producción.

**Tests recomendados:** Test de integración con fixture que incluya un suscriptor `active: false`

**Bloqueante para release:** ⚠️ No (sin suscriptores inactivos en local) — documentar decisión

---

### R-008: Carga secuencial de catálogos en lugar de paralela

**Mitigación:** Verificar en el frontend que `useSubscribersQuery` y `useRiskClassificationsQuery` se lanzan en paralelo (no en cascada). Las queries de React Query son independientes y no deben depender una de la otra.

**Tests recomendados:** Test que verifica que ambas queries tienen `status: loading` simultáneamente al montar el componente `GeneralInfoForm`

**Bloqueante para release:** ⚠️ No

---

### R-009: Código de agente en mensaje de error sin sanitizar (log injection)

**Mitigación:** Verificar que el código de agente se incluye en el mensaje de error mediante interpolación estándar de C# (`$"El agente {code} no está..."`) y que los logs no duplican el input sin transformar. Con la regex `^AGT-\d{3}$` ya implementada, el riesgo de XSS está mitigado para este endpoint, pero el riesgo de log injection subsiste si el logger recibe el input antes de la validación.

**Tests recomendados:** Test de log injection: `GetAgentByCode_Should_Validate_BeforeLogging`

**Bloqueante para release:** ⚠️ No

---

## Cobertura de Tests Unitarios Existentes

Los siguientes escenarios ya están cubiertos en `CatalogControllerTests.cs` — **NO duplicar**:

| Test existente | Cubre |
|---|---|
| `GetSubscribers_Should_Return200_WhenUseCaseSucceeds` | R-001 happy path |
| `GetSubscribers_Should_PropagateException_WhenCoreOhsUnavailable` | R-005 parcial (lanza excepción, no verifica 503) |
| `GetAgentByCode_Should_Return200_WhenAgentExists` | R-002 happy path proxy |
| `GetAgentByCode_Should_Return404_WhenAgentNotFound` | R-002 parcial |
| `GetAgentByCode_Should_Return400_WhenCodeFormatInvalid` | R-004 |
| `GetRiskClassifications_Should_Return200_WhenUseCaseSucceeds` | R-001 risk-classification |

**Brechas identificadas en tests existentes:**
- R-003 (auth 401): no cubierto en unitarios (el `[Authorize]` no se activa en tests de controlador sin middleware)
- R-005 (503 con formato correcto): `PropagateException` verifica que se lanza la excepción pero NO verifica que el middleware la convierte a 503 con body estándar → requiere test de integración
- R-006 (tipo decimal): no cubierto
- Formatos inválidos adicionales en R-004: solo se prueba "INVALID", no la matriz completa de formatos
