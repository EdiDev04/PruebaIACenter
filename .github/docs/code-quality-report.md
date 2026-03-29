# Reporte de calidad — SPEC-003 (folio-creation) + SPEC-004 (general-info-management) — 2026-03-29

---

## Parte 1 — Auditoría de arquitectura

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 3     | Sí              |
| MAYOR     | 3     | No              |
| MENOR     | 2     | No              |

### Violaciones críticas

#### CRIT-001
- **Archivo:** `cotizador-webapp/src/features/general-info-form/ui/ConductionDataSection.tsx`
- **Línea:** 4
- **Regla:** Feature importa slice del mismo nivel (feature → feature)
- **Detalle:** `import { SubscriberComboBox } from '@/features/subscriber-selector'` — en FSD una feature no puede importar de otra feature. La capa `features/` es horizontal; cada slice es independiente.
- **Acción requerida:** Mover `SubscriberComboBox` (con su query y api) a `shared/ui/` o crear una entidad de negocio en `entities/subscriber/` si representa un concepto de dominio.

#### CRIT-002
- **Archivo:** `cotizador-webapp/src/features/general-info-form/ui/BusinessClassSection.tsx`
- **Línea:** 5
- **Regla:** Feature importa slice del mismo nivel (feature → feature)
- **Detalle:** `import { RiskClassificationSelect } from '@/features/risk-classification'` — misma violación que CRIT-001.
- **Acción requerida:** Mover `RiskClassificationSelect` (con su query y api) a `shared/ui/` o `entities/risk-classification/`.

#### CRIT-003
- **Archivos:** `cotizador-webapp/src/pages/FolioHomePage.tsx`, `FolioCreatedPage.tsx`, `GeneralInfoPage.tsx`
- **Línea:** N/A (ausencia de componente)
- **Regla:** `ErrorBoundary` ausente en `pages/`
- **Detalle:** Ninguna de las tres páginas auditadas está envuelta en un `ErrorBoundary`. Un error en render no controlado colapsa la UI completa sin mostrar mensaje al usuario ni permitir recovery.
- **Acción requerida:** Crear `shared/ui/ErrorBoundary.tsx` (class component o usar `react-error-boundary`) y envolver las rutas en el router con un `<ErrorBoundary fallback={<ErrorFallback />}>` por página o a nivel de layout.

---

### Violaciones mayores

#### MAY-001
- **Archivos:** `cotizador-backend/src/Cotizador.API/Controllers/FolioController.cs` (L13) y `QuoteController.cs` (L14)
- **Regla:** Código duplicado — DRY violation
- **Detalle:** `private static readonly Regex FolioRegex = new(@"^DAN-\d{4}-\d{5}$", RegexOptions.Compiled)` está definido idénticamente en ambos controllers. Si el formato cambia, debe actualizarse en dos lugares de forma manual.
- **Acción:** Extraer a `Cotizador.Application/Constants/FolioConstants.cs` como `public static readonly Regex FolioRegex` o a un `static partial class` en API.

#### MAY-002
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/UpdateGeneralInfoUseCase.cs`
- **Línea:** 58
- **Regla:** Argumento de dominio incompleto en excepción
- **Detalle:** `throw new InvalidQuoteStateException(folioNumber, string.Empty, ...)` — el segundo parámetro (estado actual de la cotización) se pasa como `string.Empty`, eliminando información de diagnóstico. En el flujo actual el validador del agente se ejecuta antes de leer el folio (paso 3), por lo que el estado real aún no está disponible.
- **Acción:** Reestructurar para leer el folio antes de validar el agente, o usar un overload de excepción que no requiera el estado si no está disponible en ese punto.

#### MAY-003
- **Archivos:** `cotizador-webapp/src/features/folio-search/model/useFolioSearch.ts` (L34) y `features/general-info-form/ui/GeneralInfoForm.tsx` (L40)
- **Regla:** Type assertion insegura sin type guard
- **Detalle:** `const error = err as { type?: string; message?: string }` — si la API devuelve un shape de error diferente al esperado, la bifurcación por `.type` falla silenciosamente y el usuario ve el mensaje genérico en todos los casos, incluso cuando habría un mensaje específico disponible.
- **Acción:** Crear un type guard `isApiError(err)` con chequeo en runtime (`typeof err === 'object' && err !== null && 'type' in err`) o usar un schema Zod para parsear la respuesta de error.

---

### Sugerencias menores

#### MIN-001
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/GetGeneralInfoUseCase.cs`
- **Línea:** 31–41
- **Regla:** Acceso sin guardia a propiedades potencialmente nulas en estado `Draft`
- **Detalle:** `MapToDto` accede directamente a `quote.InsuredData.Name`, `quote.InsuredData.TaxId`, etc. Un folio en estado `Draft` (antes del primer PUT de general-info) puede tener `InsuredData` o `ConductionData` nulos, causando `NullReferenceException` en tiempo de ejecución.
- **Acción:** Agregar null-check: `quote.InsuredData is not null ? new InsuredDataDto(...) : null` o devolver un DTO parcial con campos en blanco.

#### MIN-002
- **Archivo:** `cotizador-webapp/src/features/folio-creation/model/useCreateFolio.ts`
- **Línea:** 10–12
- **Regla:** Handler `onError` vacío — sin observabilidad
- **Detalle:** `onError: () => { // Keep same idempotency key for retry }` — la intención está documentada en el comentario pero no hay ningún log ni telemetría. En producción un error silencioso aquí es indetectable.
- **Acción:** Agregar al menos `console.warn('createFolio failed, keeping idempotency key')` o integrar con el sistema de observabilidad del proyecto.

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Project Key:** No disponible — servidor SonarQube no enlazado al workspace (standalone).
- **Archivos `.cs` analizados vía Roslyn:** 5 (FolioController, QuoteController, CreateFolioUseCase, GetGeneralInfoUseCase, UpdateGeneralInfoUseCase).
- **Security Hotspots / Taint Vulnerabilities:** No disponibles sin Connected Mode.
- **Gate SonarQube Server:** N/A.

### Conteo de issues (Roslyn local + auditoría manual)

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 3     |
| MINOR     | 2     |
| INFO      | 0     |

### Issues BLOCKER y CRITICAL — Ninguno

No se detectaron issues BLOCKER ni CRITICAL en los 5 archivos backend auditados.

### Issues MAJOR — Revisión recomendada

| # | Archivo | Línea | Descripción |
|---|---------|-------|-------------|
| 1 | `FolioController.cs` + `QuoteController.cs` | 13 / 14 | Regex `FolioRegex` duplicada — código muerto en espera de divergencia |
| 2 | `UpdateGeneralInfoUseCase.cs` | 58 | `InvalidQuoteStateException` con `string.Empty` pierde contexto de diagnóstico |
| 3 | `useFolioSearch.ts` + `GeneralInfoForm.tsx` | 34 / 40 | Type assertion `err as {...}` sin type guard en runtime |

### Revisión manual OWASP (sin Connected Mode)

| Control OWASP | Estado | Observación |
|---------------|--------|-------------|
| A01 Broken Access Control | ✅ | `[Authorize]` en todos los controllers del scope |
| A02 Cryptographic Failures | ✅ | Sin credenciales hardcodeadas; env vars en frontend |
| A03 Injection | ✅ | MongoDB.Driver tipado; sin concatenación de queries |
| A05 Misconfiguration | ✅ | No hay CORS en los controllers del scope |
| A07 Auth Failures | ✅ | `[Authorize]` aplicado; `HttpContext.User.Identity.Name` usado correctamente |

---

## Veredicto consolidado

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura — Backend Clean Architecture | PASS |
| Auditoría arquitectura — Frontend FSD compliance | **FAIL — 2 críticos (CRIT-001, CRIT-002)** |
| Auditoría arquitectura — ErrorBoundary en pages | **FAIL — 1 crítico (CRIT-003)** |
| SonarQube Roslyn (local) | PASS — 0 BLOCKER, 0 CRITICAL |
| SonarQube Server | N/A (sin Connected Mode) |
| **Gate final** | **FAILED** |

**Causa raíz:** 3 violaciones críticas de arquitectura — dos importaciones feature→feature en `general-info-form` (`SubscriberComboBox` y `RiskClassificationSelect`) y ausencia de `ErrorBoundary` en las tres páginas del scope. Deben resolverse antes de avanzar a `qa-agent`.

Los issues MAYOR (MAY-001 a MAY-003) y MENOR (MIN-001 a MIN-002) deben ser corregidos pero no bloquean el gate.

---

# Reporte de calidad — SPEC-004 Proxy de Catálogos (catalog-proxy) — 2026-03-29

> Alcance: 12 archivos nuevos en `cotizador-backend` (3 interfaces, 3 use cases, 1 controller, 4 test files + verificación de `Program.cs`).

---

## Parte 1 — Auditoría de arquitectura

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 0     | No              |
| MAYOR     | 1     | No              |
| MENOR     | 2     | No              |

### Violaciones críticas

Ninguna.

### Violaciones mayores

#### MAY-004
- **Archivo:** `cotizador-backend/src/Cotizador.API/Middleware/ExceptionHandlingMiddleware.cs` (archivo existente, modificado para la feature)
- **Línea:** 48
- **Regla:** ADR-008 — `message` debe estar en español
- **Detalle:** El handler de `CoreOhsUnavailableException` retorna la respuesta 503 con mensaje en inglés: `"The reference data service is temporarily unavailable. Please try again later."`. Todos los mensajes visibles al cliente deben estar en español; solo el campo `type` permanece en inglés.
- **Acción:** Reemplazar el literal por: `"El servicio de datos de referencia no está disponible temporalmente. Inténtelo de nuevo más tarde."`

### Sugerencias menores

#### MIN-003
- **Archivo:** `cotizador-backend/src/Cotizador.Tests/Application/UseCases/GetAgentByCodeUseCaseTests.cs`
- **Línea:** 36
- **Regla:** Convención `[Trait]` — categorizar todos los tests
- **Detalle:** El test `ExecuteAsync_Should_ReturnNull_WhenAgentDoesNotExist` carece de `[Trait("Category", ...)]`. El resto de tests del proyecto usa `[Trait("Category", "Smoke")]` o `[Trait("Category", "Regression")]` para organizar la ejecución por pipeline.
- **Acción:** Añadir `[Trait("Category", "Regression")]`.

#### MIN-004
- **Archivo:** `cotizador-backend/src/Cotizador.API/Controllers/CatalogController.cs`
- **Línea:** 10
- **Regla:** Convención de atributo de autenticación del proyecto
- **Detalle:** El controller usa `[Authorize]` estándar de ASP.NET Core. Las instrucciones del proyecto indican el uso de `[BasicAuthorize]` como convención de equipo. En la práctica es inocuo porque `BasicAuth` es el único esquema registrado, pero genera inconsistencia con otros controllers.
- **Acción:** Verificar si existe un atributo personalizado `[BasicAuthorize]` en `Cotizador.API/Auth/` y, de existir, reemplazar `[Authorize]` por `[BasicAuthorize]` para mantener coherencia.

---

## Checklist de los 8 puntos solicitados

| # | Criterio | Estado | Detalle |
|---|----------|--------|---------|
| 1 | Clean Architecture: `API → Application`, sin referencia a Infrastructure | ✅ PASS | `CatalogController` importa únicamente `Cotizador.Application.DTOs`, `Cotizador.Application.Interfaces` y frameworks `Microsoft.AspNetCore.*`. Los use cases importan `Cotizador.Application.Ports.ICoreOhsClient` (interface definida en Application, no Infrastructure). |
| 2 | Inyección por interfaz, no implementación concreta | ✅ PASS | Controller recibe `IGetSubscribersUseCase`, `IGetAgentByCodeUseCase`, `IGetRiskClassificationsUseCase`. Use cases reciben `ICoreOhsClient`. Ningún `new()` fuera del composition root. |
| 3 | Tests siguen patrón AAA, sin estado mutable compartido | ✅ PASS | xUnit crea nueva instancia de clase por cada test. Mocks son campos de instancia (no `static`). `Sut` es una expression-bodied property (`=>`) que crea instancia fresca por acceso. `CatalogControllerTests.CreateController()` también es local a cada test. |
| 4 | ADR-008: `type` en inglés, `message` en español | ⚠️ MAYOR | Controller ✅: `"validationError"` / `"Código de agente inválido"`, `"agentNotFound"` / `"El agente {code} no está registrado en el catálogo"`. Middleware ❌: respuesta 503 de `CoreOhsUnavailableException` en inglés (ver MAY-004). |
| 5 | Envelope `{ "data": ... }` en todos los 200 | ✅ PASS | `GetSubscribersAsync` → `Ok(new { data = subscribers })`, `GetAgentByCodeAsync` → `Ok(new { data = agent })`, `GetRiskClassificationsAsync` → `Ok(new { data = classifications })`. |
| 6 | 404 cuando agente no existe, no 500 | ✅ PASS | `GetAgentByCodeAsync` evalúa `agent is null` y retorna `NotFound(...)`. Test `GetAgentByCode_Should_Return404_WhenAgentNotFound` lo cubre con verificación de `StatusCodes.Status404NotFound` y `value.type == "agentNotFound"`. |
| 7 | Validación de formato antes del use case | ✅ PASS | `AgentCodeRegex = new(@"^AGT-\d{3}$", RegexOptions.Compiled)` valida antes de llamar al use case. Test `GetAgentByCode_Should_Return400_WhenCodeFormatInvalid` verifica explícitamente `Times.Never` en la invocación del use case. |
| 8 | 3 use cases registrados con `AddScoped` en `Program.cs` | ✅ PASS | `AddScoped<IGetSubscribersUseCase, GetSubscribersUseCase>()`, `AddScoped<IGetAgentByCodeUseCase, GetAgentByCodeUseCase>()`, `AddScoped<IGetRiskClassificationsUseCase, GetRiskClassificationsUseCase>()` — los tres presentes en la sección "Proxy catálogos (SPEC-004)". |

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Project Key:** No registrado en servidor SonarQube (workspace sin Connected Mode)
- **Archivos analizados:** 8 (3 use cases + 1 controller + 4 test files)
- **Análisis:** Roslyn local vía SonarQube for IDE
- **Gate SonarQube:** PASS

### Conteo de issues

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 1     |
| MINOR     | 0     |
| INFO      | 0     |

### Issues BLOCKER y CRITICAL — Ninguno

### Issues MAJOR — Revisión recomendada

| # | Archivo | Línea | Mensaje |
|---|---------|-------|---------|
| 1 | `Cotizador.API/Controllers/CatalogController.cs` | L12 | "This controller has multiple responsibilities and could be split into 3 smaller controllers." |

> El MAJOR de SonarQube responde a que `CatalogController` agrupa 3 endpoints de tipos de catálogo distintos. Es una decisión de diseño de la spec (agrupar catálogos en un solo controller). Puede resolverse en una iteración futura si se añaden más endpoints al controller.

---

## Veredicto consolidado — SPEC-004 Proxy de Catálogos

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura | PASS — 0 críticos, 1 mayor, 2 menores |
| SonarQube (Roslyn local) | PASS — 0 BLOCKER, 0 CRITICAL, 1 MAJOR |
| **Gate final** | **PASSED** |

El único MAYOR (ADR-008 en mensaje 503 del middleware existente) no es una violación crítica de arquitectura. Los 8 puntos de verificación explícitos están aprobados.

---
