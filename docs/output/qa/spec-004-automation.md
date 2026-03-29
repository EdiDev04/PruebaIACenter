# Propuesta de Automatización — SPEC-004 Proxy Catálogos
**Feature:** Endpoints proxy de catálogos  
**Spec de referencia:** `.github/specs/general-info-management.spec.md` §3.5  
**Generado por:** QA Agent  
**Fecha:** 2026-03-29

---

## Resumen Ejecutivo

| Métrica | Valor |
|---------|-------|
| Flujos candidatos evaluados | 9 |
| P1 — Automatizar en Sprint actual | 4 |
| P2 — Automatizar en Sprint siguiente | 3 |
| Posponer / No automatizar | 2 |
| Framework API (Integration) | xUnit + HttpClient + WebApplicationFactory (.NET) |
| Framework E2E | Playwright (TypeScript) |
| Esfuerzo estimado total | 2 sprints |

**Justificación de selección:** Los flujos P1 cubren directamente los criterios de evaluación del reto: "consistencia de APIs", "manejo de errores" y "trazabilidad". Los flujos de autenticación (R-003) y resiliencia (R-005) son los de mayor ROI porque son difíciles de cubrir manualmente en cada release y de alto impacto si fallan en producción.

---

## Criterios de Evaluación (4/4)

| Criterio | Descripción | Aplica a |
|----------|-------------|----------|
| ✅ Repetitivo | Los catálogos se cargan en cada apertura del wizard — frecuencia muy alta | Todos los flujos |
| ✅ Estable | Los endpoints proxy son passthrough — su contrato no cambia entre sprints | Flujos de contrato |
| ✅ Alto Impacto | Un 503 no manejado o un 401 bypasseado impacta a todos los usuarios del wizard | Auth + Resiliencia |
| ✅ Costo Alto | Ejecutar manualmente los 9 escenarios de la matriz toma ~45 min por release | Todos los flujos |

---

## Matriz de Priorización (ROI)

| ID | Flujo | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad |
|----|-------|-----------|---------|--------------|--------------|-----|-----------|
| FLUJO-001 | Tests de contrato proxy → core-mock | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (25 min) | 4/4 | **P1** |
| FLUJO-002 | Autenticación 401 en los 3 endpoints | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (15 min) | 4/4 | **P1** |
| FLUJO-003 | Resiliencia 503 — core-mock caído | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (20 min) | 4/4 | **P1** |
| FLUJO-004 | Validación formato agentCode (matriz de formatos) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (15 min) | 4/4 | **P1** |
| FLUJO-005 | E2E wizard step 1 — ciclo completo exitoso | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto (20 min) | 4/4 | **P2** |
| FLUJO-006 | Regresión — agente válido en GET, 422 en PUT | ⚠️ Media | ✅ Sí | ✅ Alta | ✅ Alto (12 min) | 3/4 | **P2** |
| FLUJO-007 | Tipo decimal del campo factor (riskClassification) | ⚠️ Media | ✅ Sí | ⚠️ Media | ⚠️ Medio (8 min) | 2/4 | **P2** |
| FLUJO-008 | Suscriptores inactivos filtrados | ❌ Baja | ⚠️ No | ⚠️ Media | ❌ Bajo (5 min) | 1/4 | **Posponer** |
| FLUJO-009 | X-Correlation-Id propagado a core-mock | ❌ Baja | ✅ Sí | ❌ Baja | ❌ Bajo (5 min) | 1/4 | **Posponer** |

---

## Selección de Frameworks

### Framework 1 — Integration Tests (.NET)
**Framework:** xUnit + `WebApplicationFactory<Program>` + `HttpClient`  

**Justificación:**
- El backend es ASP.NET Core 8 — `WebApplicationFactory` es el estándar para integration tests en .NET
- Permite levantar el backend en proceso (sin puerto real) con core-mock mockeado via `IHttpClientFactory`
- Compatible con el stack existente: el proyecto `Cotizador.Tests` ya usa xUnit + Moq + FluentAssertions
- Permite verificar TANTO la lógica del controlador COMO el middleware de autenticación y el global exception handler — lo que los unit tests actuales NO pueden verificar

**Aplica a:** FLUJO-001, FLUJO-002, FLUJO-003, FLUJO-004, FLUJO-006, FLUJO-007

### Framework 2 — E2E (Frontend + Backend)
**Framework:** Playwright (TypeScript)  

**Justificación:**
- El frontend es React/Vite (TypeScript) — Playwright es el estándar CI-first para stack TS
- El proyecto `cotizador-automatization/` está preparado para E2E
- Multi-browser (Chromium, Firefox, WebKit) para verificar comportamiento del wizard
- Permite simular el flujo completo del usuario: cargar catálogos → seleccionar → guardar

**Aplica a:** FLUJO-005

---

## Hoja de Ruta por Sprint

### Sprint Actual — P1 (4 flujos)
> Bloqueantes de release — deben completarse antes de pasar a QA final.

#### FLUJO-001: Tests de contrato proxy → core-mock
**Tipo:** Integration (WebApplicationFactory)  
**Ruta:** `cotizador-backend/src/Cotizador.Tests/Integration/Catalog/`  
**Estimación:** 3 días

```csharp
// Descripción técnica de lo que se verifica:
// 1. Backend llama efectivamente a core-mock (no mock interno)
// 2. Los campos code, name, office, active de SubscriberDto coinciden con fixtures
// 3. Los campos code, name, region, active de AgentDto coinciden con fixtures
// 4. Los campos code, description, factor de RiskClassificationDto coinciden con fixtures
// 5. Los tres endpoints retornan el envelope { "data": [...] }

[Fact] ProxySubscribers_Should_ReturnAllSubscribersFromCoreMock()
[Fact] ProxySubscribers_Should_MapAllFieldsFromCoreMockResponse()
[Fact] ProxyAgent_Should_ReturnAgentFieldsFromCoreMock_WhenCodeExists()
[Fact] ProxyRiskClassification_Should_ReturnAllClassificationsFromCoreMock()
[Fact] ProxyRiskClassification_Should_MapFactorAsDecimalNotString()
```

**DoR:**
- [ ] core-mock disponible en `http://localhost:3000` (o configurado como TestServer)
- [ ] `appsettings.Test.json` con URL de core-mock apuntando al test server
- [ ] Fixtures de core-mock accesibles desde el proyecto de tests

---

#### FLUJO-002: Autenticación 401 en los 3 endpoints
**Tipo:** Integration (WebApplicationFactory con auth middleware activo)  
**Ruta:** `cotizador-backend/src/Cotizador.Tests/Integration/Catalog/`  
**Estimación:** 1 día

```csharp
// Descripción: Los unit tests actuales NO activan el middleware [Authorize].
// WebApplicationFactory sí lo activa — permite verificar el comportamiento real.

[Fact] GetSubscribers_Should_Return401_WhenNoAuthorizationHeader()
[Fact] GetSubscribers_Should_Return401_WhenInvalidCredentials()
[Fact] GetAgentByCode_Should_Return401_WhenNoAuthorizationHeader()
[Fact] GetRiskClassifications_Should_Return401_WhenNoAuthorizationHeader()
[Fact] Auth_Response_Should_IncludeUnauthorizedType_InBody()
```

**Cobertura de riesgo:** R-003 (Alto)

---

#### FLUJO-003: Resiliencia 503 — core-mock caído
**Tipo:** Integration (WebApplicationFactory con ICoreOhsClient mockeado para lanzar timeout)  
**Ruta:** `cotizador-backend/src/Cotizador.Tests/Integration/Catalog/`  
**Estimación:** 2 días

```csharp
// Descripción: Verifica que el GlobalExceptionHandler convierte CoreOhsUnavailableException → 503
// con el body estándar { type, message, field } y SIN stack trace.

[Fact] GetSubscribers_Should_Return503_WithStandardBody_WhenCoreOhsThrowsTimeout()
[Fact] GetSubscribers_Should_Not_ExposeStackTrace_InErrorResponse()
[Fact] GetAgentByCode_Should_Return503_WhenCoreOhsUnavailable()
[Fact] GetRiskClassifications_Should_Return503_WhenCoreOhsUnavailable()
[Fact] ServiceUnavailable_Response_Should_HaveCoreOhsUnavailableType()
```

**Cobertura de riesgo:** R-005 (Alto)

---

#### FLUJO-004: Validación formato agentCode — matriz completa
**Tipo:** Unit + Integration  
**Ruta:** `cotizador-backend/src/Cotizador.Tests/API/Controllers/CatalogControllerTests.cs` (ampliar)  
**Estimación:** 1 día

```csharp
// Los unit tests existentes solo cubren "INVALID".
// Ampliar con [Theory] para cubrir la matriz completa de formatos inválidos.

[Theory]
[InlineData("AGT001")]    // Sin guion
[InlineData("AGT-01")]    // 2 dígitos
[InlineData("AGT-0001")]  // 4 dígitos
[InlineData("agt-001")]   // Minúsculas
[InlineData("AGT-00A")]   // Carácter no numérico
[InlineData("AGT-")]      // Sin dígitos
[InlineData("")]          // Cadena vacía
[InlineData(" ")]         // Solo espacio
public async Task GetAgentByCode_Should_Return400_ForAllInvalidFormats(string invalidCode)

// Test nuevo: sin parámetro code
[Fact] GetAgentByCode_Should_Return400_WhenCodeParameterIsMissing()
```

**Cobertura de riesgo:** R-004 (Alto)

---

### Sprint Siguiente — P2 (3 flujos)

#### FLUJO-005: E2E wizard step 1 — ciclo completo exitoso
**Tipo:** E2E (Playwright TypeScript)  
**Ruta:** `cotizador-automatization/e2e/specs/general-info/`  
**Estimación:** 3 días

```typescript
// Descripción: Flujo completo del usuario en el wizard step 1
// Verifica que los catálogos cargaron, el agente fue buscado y los datos se guardaron.

test('wizard-step1: cargar catálogos, buscar agente y guardar datos generales', async ({ page }) => {
  // Navegar al folio DAN-2026-00001
  // Verificar que dropdown de suscriptores tiene 3 opciones (SUB-001, SUB-002, SUB-003)
  // Verificar que dropdown de clasificación tiene 3 opciones
  // Escribir AGT-001 → verificar que aparece "Roberto Hernández (Centro)"
  // Llenar formulario completo
  // Click "Guardar" → verificar status 200 e incremento de version
  // Verificar transición de estado "draft" → "in_progress"
})

test('wizard-step1: conflicto de versión muestra alerta con botón recargar', async ({ page }) => {
  // ...
})

test('wizard-step1: core-mock caído muestra mensaje de error en formulario', async ({ page }) => {
  // Mockear network con Playwright route interception → retornar 503
  // Verificar que el frontend muestra el mensaje de error apropiado
  // Verificar que el botón "Guardar" está deshabilitado
})
```

**ROI del reto:** Cubre el criterio "flujo completo de cotización" — el de mayor visibilidad en la evaluación.

---

#### FLUJO-006: Regresión — inconsistencia proxy GET vs. validación PUT
**Tipo:** Integration (.NET)  
**Ruta:** `cotizador-backend/src/Cotizador.Tests/Integration/GeneralInfo/`  
**Estimación:** 2 días

```csharp
// Verificar que si GET /v1/agents?code=AGT-001 retorna 200 (agente encontrado)
// pero justo antes del PUT el core-mock ya no tiene ese agente,
// el PUT retorna 422 con mensaje en español.

[Fact] UpdateGeneralInfo_Should_Return422_WhenAgentExistsAtQuery_ButDisappearsAtPersist()
[Fact] UpdateGeneralInfo_Should_Return422_Body_WithSpanishMessage()
[Fact] UpdateGeneralInfo_422_Body_Should_IncludeAgentCode_InMessage()
```

---

#### FLUJO-007: Tipo decimal del campo factor en risk-classification
**Tipo:** Integration (.NET)  
**Ruta:** `cotizador-backend/src/Cotizador.Tests/Integration/Catalog/`  
**Estimación:** 1 día

```csharp
// Verificar serialización JSON de RiskClassificationDto.Factor como número decimal.
// Riesgo: SPEC-005 (cálculo) falla si recibe "1.0" (string) en lugar de 1.0 (number).

[Fact] GetRiskClassifications_Factor_Should_BeDeserializedAsDecimal_NotString()
[Fact] GetRiskClassifications_PreferredFactor_Should_BeLessThan_1()
[Fact] GetRiskClassifications_SubstandardFactor_Should_BeGreaterThan_1()
```

---

### Posponer (2 flujos)

| Flujo | Razón para posponer |
|-------|---------------------|
| FLUJO-008: suscriptores inactivos | Sin suscriptores inactivos en fixtures locales; requiere acuerdo con PO sobre responsabilidad de filtrado |
| FLUJO-009: X-Correlation-Id propagado | Impacto bajo en funcionalidad; es observabilidad operacional, no criterio de aceptación del reto |

---

## DoR de Automatización (por flujo)

Antes de iniciar la automatización de cualquier flujo:
- [ ] El escenario Gherkin correspondiente fue ejecutado manualmente con éxito (sin bugs críticos)
- [ ] Los datos de prueba (fixtures de core-mock) están identificados y disponibles
- [ ] El ambiente de integración (`http://localhost:3000` para core-mock, `http://localhost:5000` para backend) es estable
- [ ] Se definió si core-mock se levanta como proceso real o como WireMock/TestServer
- [ ] Aprobación del equipo para iniciar la sprint de automatización

## DoD de Automatización (por flujo)

Antes de marcar un flujo como automatizado:
- [ ] Código del test revisado por al menos un par
- [ ] El test se ejecuta en verde en el pipeline CI (GitHub Actions)
- [ ] El test falla si se introduce un bug intencional (mutation testing manual)
- [ ] El test se documenta en el README de `cotizador-automatization/`
- [ ] Si el test E2E: se ejecuta en Chromium + Firefox (mínimo 2 browsers)

---

## Alineación con Criterios de Evaluación del Reto

| Criterio del Reto | Flujos que lo cubren |
|-------------------|---------------------|
| ✅ Trazabilidad del cálculo | FLUJO-007 (tipo decimal de factores → insumo de SPEC-005) |
| ✅ Consistencia de APIs y manejo de errores | FLUJO-001, FLUJO-003 (503), FLUJO-006 (422 inconsistencia) |
| ✅ Seguridad — control de acceso | FLUJO-002 (401 en los 3 endpoints) |
| ✅ Flujo completo de cotización | FLUJO-005 (E2E wizard step 1) |
| ✅ Validación de inputs | FLUJO-004 (formato agentCode) |

---

## Estimación Total

| Prioridad | Flujos | Esfuerzo |
|-----------|--------|----------|
| P1 (Sprint actual) | FLUJO-001 al 004 | 7 días-persona |
| P2 (Sprint siguiente) | FLUJO-005 al 007 | 6 días-persona |
| **Total** | 7 flujos | **~3 semanas** |
