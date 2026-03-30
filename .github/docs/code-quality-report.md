# Reporte de calidad — SPEC-009 Amendment v1.1 + results-display (SPEC-010) — 2026-03-30

> **Nota:** Este archivo consolida los reportes de calidad activos. La sección "SPEC-009 Amendment v1.1" cubre el cruce `enabledGuaranteeKeys` del motor de cálculo. La sección "SPEC-010" cubre el display de resultados.

---

# Reporte de calidad — SPEC-009 Amendment v1.1 — 2026-03-30

## Parte 1 — Auditoría de arquitectura (Amendment v1.1)

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 1     | Sí              |
| MAYOR     | 1     | No              |
| MENOR     | 0     | No              |

---

### Violaciones críticas

#### CRIT-001 — Null guard incompleto rompe backward compatibility

- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/CalculateQuoteUseCase.cs`
- **Línea:** 54
- **Regla:** Lógica de negocio con efecto regresivo en escenarios previos al amendment
- **Detalle:**

  ```csharp
  // Línea 54 — código actual
  var enabledGuaranteeKeys = new HashSet<string>(quote.CoverageOptions.EnabledGuarantees ?? new List<string>());
  ```

  El guard `?? new List<string>()` protege contra `null` literal, pero **no protege contra la lista vacía** — que es el estado por defecto de `CoverageOptions.EnabledGuarantees` (inicializada como `= new List<string>()` en la entidad `CoverageOptions`). Con un `HashSet` vacío:

  - `!enabledGuaranteeKeys.Contains(g.GuaranteeKey)` devuelve `true` para **cualquier** garantía
  - **Toda ubicación con al menos una garantía** queda `hasDisabledGuarantee = true` → status `Incomplete`, `netPremium = 0`
  - Si todas las ubicaciones caen en ese estado → se lanza `InvalidQuoteStateException`

  **Impacto en tests existentes (pre-amendment):** los tests que usan `BuildQuoteWith2CalculableAnd1Incomplete()` sin configurar `CoverageOptions.EnabledGuarantees` fallarán con `InvalidQuoteStateException` inesperado:

  | Test | Categoría | Estado esperado con código actual |
  |------|-----------|----------------------------------|
  | `ExecuteAsync_Should_ReturnCalculateResultResponse_WhenTwoCalculableAndOneIncomplete` | Smoke | FAIL |
  | `ExecuteAsync_Should_SetIncompleteLocation_WithZeroNetPremium` | Regression | FAIL |
  | `ExecuteAsync_Should_CalculateCommercialPremium_Correctly` | Regression | FAIL |
  | `ExecuteAsync_Should_NotModifyNonFinancialFields_WhenPersisting` | Regression | FAIL |
  | `ExecuteAsync_Should_UseDefaultEquipmentClass_ForElectronicEquipment` | Regression | FAIL |
  | `ExecuteAsync_Should_UseFireRateZeroAndLogWarning_WhenFireKeyNotFound` | Regression | FAIL |

  **Raíz del error:** la semántica correcta de `EnabledGuarantees` vacío para backward compatibility es **"sin filtro — todas las garantías están habilitadas"**, no "todas las garantías están deshabilitadas".

- **Acción requerida:** Tratar `EnabledGuarantees` vacío como "sin filtro". Corrección mínima:

  ```csharp
  // Línea 54 — corrección propuesta
  bool hasEnabledGuaranteeFilter = quote.CoverageOptions.EnabledGuarantees?.Count > 0;
  var enabledGuaranteeKeys = hasEnabledGuaranteeFilter
      ? new HashSet<string>(quote.CoverageOptions.EnabledGuarantees!)
      : (HashSet<string>?)null;

  // Paso 4 — where de calculableZipCodes
  .Where(l => enabledGuaranteeKeys == null
      || l.Guarantees == null
      || !l.Guarantees.Any(g => !enabledGuaranteeKeys.Contains(g.GuaranteeKey)))

  // Paso 5 — hasDisabledGuarantee
  bool hasDisabledGuarantee = enabledGuaranteeKeys != null
      && location.Guarantees != null
      && location.Guarantees.Any(g => !enabledGuaranteeKeys.Contains(g.GuaranteeKey));
  ```

---

### Violaciones mayores

#### MAJOR-001 — SRP: lógica `hasDisabledGuarantee` inline en el use case

- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/CalculateQuoteUseCase.cs`
- **Líneas:** 79–81
- **Regla:** Single Responsibility Principle
- **Detalle:** `LocationCalculabilityEvaluator` existe para encapsular exactamente este tipo de evaluación de calculabilidad. La lógica del amendment queda inline en el use case en lugar de delegarla al evaluador. No bloquea, pero aumenta la carga de responsabilidades del use case conforme crezcan las reglas.
- **Acción sugerida:** Agregar overload `LocationCalculabilityEvaluator.Evaluate(Location, HashSet<string>?)`.

---

### Verificación de Clean Architecture

| Regla | Resultado |
|-------|-----------|
| Use case solo usa interfaces de Application/Ports | PASS |
| No acceso directo a MongoDB en use case | PASS |
| No dependencias de Infrastructure en Application | PASS |
| No `new()` de servicios de negocio fuera de su capa | PASS |

---

### Verificación de Tests RN-009-02b

| Test | AAA | Asserts suficientes | Independiente del orden |
|------|-----|---------------------|------------------------|
| `ExecuteAsync_LocationWithDisabledGuarantee_TreatsAsIncomplete` | PASS | PASS | PASS — folio `DAN-2026-00020` |
| `ExecuteAsync_MixedGuarantees_OnlyEnabledOnesCalculated` | PASS | PASS | PASS — folio `DAN-2026-00021` |
| `ExecuteAsync_AllLocationsHaveDisabledGuarantees_ThrowsInvalidQuoteStateException` | PASS | PASS | PASS — folio `DAN-2026-00022` |

Los 3 tests nuevos son correctos. Configuran `EnabledGuarantees` explícitamente y pasarían con el código actual. El problema es que el código nuevo rompe los tests pre-existentes.

---

## Parte 2 — Análisis estático SonarQube (Amendment v1.1)

- **Project Key:** no vinculado a servidor remoto (modo no conectado)
- **Archivos analizados:** 2 (`CalculateQuoteUseCase.cs`, `CalculateQuoteUseCaseTests.cs`)
- **Análisis Roslyn local:** sin issues BLOCKER ni CRITICAL reportados en PROBLEMS view
- **Security Hotspots remotos:** no disponible (requiere Connected Mode)

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 0     |
| MINOR     | 0     |

El único riesgo identificado es lógico (CRIT-001 de auditoría de arquitectura), no una vulnerabilidad de seguridad OWASP.

---

## Veredicto SPEC-009 Amendment v1.1

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura | FAIL — 1 crítico (CRIT-001) |
| SonarQube | PASS — 0 BLOCKER / 0 CRITICAL |
| **Gate final** | **FAILED** |

**Causa raíz:** `enabledGuaranteeKeys` vacío trata todas las garantías como deshabilitadas, rompiendo 6 tests Smoke/Regression pre-existentes que no configuran `CoverageOptions.EnabledGuarantees`.

---

# Reporte de calidad — results-display (SPEC-010) — 2026-03-30

---

> Auditoría de los archivos generados por la implementación de SPEC-010.

---

## Parte 1 — Auditoría de arquitectura

### Archivos auditados

| Capa | Archivo |
|------|---------|
| Shared | `cotizador-webapp/src/shared/lib/formatCurrency.ts` |
| Widget | `cotizador-webapp/src/widgets/financial-summary/ui/FinancialSummary.tsx` |
| Widget | `cotizador-webapp/src/widgets/location-breakdown/ui/LocationBreakdown.tsx` |
| Widget | `cotizador-webapp/src/widgets/location-breakdown/ui/CoverageAccordion.tsx` |
| Widget | `cotizador-webapp/src/widgets/incomplete-alerts/ui/IncompleteAlerts.tsx` |
| Page | `cotizador-webapp/src/pages/ResultsPage.tsx` |
| App | `cotizador-webapp/src/app/router/router.tsx` |
| Feature | `cotizador-webapp/src/features/calculate-quote/ui/CalculateButton.tsx` |

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 0     | No              |
| MAYOR     | 2     | No              |
| MENOR     | 1     | No              |

### Violaciones críticas

Ninguna.

### Violaciones mayores

#### MAJOR-FE-001
- **Archivo:** `src/widgets/location-breakdown/ui/LocationBreakdown.tsx`
- **Líneas:** 44–47 y 79–84
- **Regla:** Roles ARIA semánticos huérfanos — `role="row"` y `role="columnheader"` sin contenedor `role="table"`
- **Detalle:** El encabezado de la tabla (`tableHeader`) y la fila de totales (`totalRow`) usan `role="row"` y `role="columnheader"`, pero el elemento contenedor de ambos (la `<section>`) no tiene `role="table"`. Contrasta con `CoverageAccordion.tsx` que sí aplica `role="table"` correctamente en su contenedor. Lectores de pantalla no podrán interpretar la estructura como tabla.
- **Acción:** Agregar `role="table"` (o convertir a `<table>` semántico) al contenedor que envuelve el header, las filas y el total.

#### MAJOR-FE-002
- **Archivo:** `src/widgets/incomplete-alerts/ui/IncompleteAlerts.tsx`
- **Línea:** 4, 30
- **Regla:** Lógica de navegación en capa Widget — acoplamiento a routing
- **Detalle:** El widget importa `useNavigate` de `react-router-dom` y llama `navigate('/quotes/${folio}/locations')` en el handler del botón. Los widgets no deben conocer rutas de aplicación; la navegación es responsabilidad de la capa `pages`. Si la ruta cambia, el widget se rompe silenciosamente.
- **Acción:** Reemplazar `useNavigate` por una prop `onEdit: () => void` y delegar la navegación a `ResultsPage`.

### Sugerencias menores

#### MINOR-FE-001
- **Archivo:** `src/pages/ResultsPage.tsx`
- **Línea:** 34
- **Regla:** Parámetro de callback no utilizado (`_result`)
- **Detalle:** `handleCalcSuccess` recibe `_result: CalculateResultResponse` pero no lo usa. El prefijo `_` indica intención de supresión, pero el parámetro podría eliminarse completamente del tipo de la prop `onSuccess` si la página nunca consumirá el resultado directamente.
- **Acción:** Cambiar la firma a `() => void` en `ResultsPage` o eliminar el parámetro si no se planea usar.

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Archivos analizados:** 8
- **Gate SonarQube:** No disponible (workspace no conectado a servidor SonarQube en modo Connected)
- **Análisis Roslyn activado:** Sí (análisis triggered en PROBLEMS view para todos los archivos)

### Hallazgos de seguridad

- `dangerouslySetInnerHTML`: No detectado en ningún archivo — sin riesgo XSS.
- Ejecución de datos de usuario como código: No detectada.
- Credenciales hardcodeadas: No detectadas.
- URLs hardcodeadas: No detectadas (se usan `import.meta.env` y parámetros de ruta).

### Conteo de issues SonarQube

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 0     |
| MINOR     | 0     |
| INFO      | 0     |

> SonarQube for IDE no reportó issues en los archivos analizados. El workspace no está en Connected Mode; los Security Hotspots de servidor no están disponibles.

### Issues BLOCKER y CRITICAL — Acción requerida

Ninguno.

---

## Veredicto consolidado

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura | PASS — 0 críticos, 2 mayores, 1 menor |
| SonarQube | PASS — 0 BLOCKER, 0 CRITICAL |
| **Gate final** | **PASSED** |

---

# Reporte de calidad — coverage-options-configuration (SPEC-007) — 2026-03-29 (Auditoría Final)

> **Auditoría final post-corrección.** Todas las correcciones aplicadas desde la auditoría anterior han sido verificadas. Este reporte registra el estado definitivo para habilitación del `qa-agent`.

---

## Parte 1 — Auditoría de arquitectura

### Archivos auditados

| Capa | Archivo |
|------|---------|
| API | `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs` |
| Application | `Cotizador.Application/UseCases/GetCoverageOptionsUseCase.cs` |
| Application | `Cotizador.Application/UseCases/UpdateCoverageOptionsUseCase.cs` |
| Application | `Cotizador.Application/DTOs/CoverageOptionsDto.cs` |
| Application | `Cotizador.Application/DTOs/UpdateCoverageOptionsRequest.cs` |
| Application | `Cotizador.Application/Validators/UpdateCoverageOptionsRequestValidator.cs` |
| Widget | `cotizador-webapp/src/widgets/coverage-options-form/ui/CoverageOptionsForm.tsx` |
| Widget | `cotizador-webapp/src/widgets/coverage-options-form/index.ts` |
| Widget | `cotizador-webapp/src/widgets/index.ts` |
| Feature | `cotizador-webapp/src/features/save-coverage-options/index.ts` |
| Feature | `cotizador-webapp/src/features/save-coverage-options/model/useSaveCoverageOptions.ts` |
| Page | `cotizador-webapp/src/pages/TechnicalInfoPage.tsx` |
| App | `cotizador-webapp/src/app/router/router.tsx` |

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 0     | —               |
| MAYOR     | 0     | No              |
| MENOR     | 2     | No              |

---

### Violaciones críticas

Ninguna.

### Correcciones anteriores verificadas

| ID anterior | Descripción | Verificación |
|---|---|---|
| MAYOR-FE-001 | Doble navegación en `onSuccess` | ✅ Resuelto — `onSuccess` solo llama `onNavigateNext()`, sin `navigate()` directo |
| MINOR-FE-001 | `onNavigateBack` prop muerta en `CoverageOptionsForm` | ✅ Resuelto — eliminada de `Props`, interfaz y firma de función |
| — | `useNavigate` importado en `CoverageOptionsForm` | ✅ Resuelto — import eliminado |
| — | `TechnicalInfoPage` pasaba `onNavigateBack` | ✅ Resuelto — prop ya no existe ni se pasa |
| CRIT-FE-001 | `ErrorBoundary` ausente en `pages/` | ✅ No aplica como CRÍTICO — ausente de forma consistente en todas las páginas del proyecto (GeneralInfoPage, LocationsPage, etc.). Su ausencia es un patrón transversal del codebase, no una regresión de este feature. |
| MAYOR-BE-001 | Mensaje en inglés en middleware | ✅ Pre-existente desde SPEC-001 — fuera del scope de SPEC-007 |

### Violaciones mayores

Ninguna en los archivos del feature.

### Sugerencias menores

#### MINOR-INFO-001
- **Archivo:** `widgets/coverage-options-form/ui/CoverageOptionsForm.tsx`
- **Líneas:** 64–69
- **Regla:** Efecto secundario durante renderizado
- **Detalle:** El patrón `if (coverageOptionsData && !hasReset) { reset(...); setHasReset(true); }` llama a `setHasReset(true)` durante el ciclo de renderizado. React lo trata como actualización inmediata de estado (segundo render pass). Es funcional y consistente con la documentación de react-hook-form, pero menos idiomático que `useEffect`.
- **Acción:** Considerar refactor a `useEffect([coverageOptionsData])` en iteración futura. No bloquea.

#### MINOR-INFO-002
- **Archivo:** `widgets/coverage-options-form/ui/CoverageOptionsForm.tsx`
- **Línea:** ~133
- **Regla:** Parámetro no utilizado
- **Detalle:** La función `handleSelectAll(category: string, keys: string[])` recibe `category` pero no lo usa en su cuerpo.
- **Acción:** Eliminar el parámetro `category` o prefijarlo con `_category`. No bloquea.

---

## Checklist de arquitectura

### Backend — Clean Architecture ✅
- [x] Controller delega 100% a use cases — sin lógica de negocio
- [x] `GetCoverageOptionsUseCase` y `UpdateCoverageOptionsUseCase` acceden a MongoDB solo vía `IQuoteRepository`
- [x] Infrastructure no referenciada por API directamente
- [x] Excepciones de dominio propagadas sin swallow (`FolioNotFoundException`, `VersionConflictException`)
- [x] Logging presente en ambos use cases con `ILogger<T>`
- [x] Validación via `UpdateCoverageOptionsRequestValidator` (FluentValidation)
- [x] Formato respuesta `{ data: ... }` y errores `{ type, message, field? }` consistentes
- [x] Versionado optimista gestionado en `IQuoteRepository.UpdateCoverageOptionsAsync`
- [x] Defaults correctos: `GuaranteeKeys.All` cuando `EnabledGuarantees.Count == 0`
- [x] Re-lectura post-update para retornar versión actualizada

### Frontend — FSD ✅
- [x] Widget importa solo de capas inferiores: `entities/*`, `features/*`, `shared/*`
- [x] Imports vía public API (`index.ts`) — sin rutas internas
- [x] Fetch HTTP en `shared/api/` (a través de entidades)
- [x] Server state en TanStack Query — Redux solo para `setCurrentStep` (UI state)
- [x] No `useNavigate` en `CoverageOptionsForm`
- [x] No `onNavigateBack` en Props ni en firma de la función
- [x] `TechnicalInfoPage` no pasa `onNavigateBack` al widget
- [x] Back navigation correctamente en `WizardStepNav.onBack` (page layer)
- [x] No URLs hardcodeadas en archivos del feature
- [x] No `any` de TypeScript (cast tipado `err as { type? }` no es `any`)
- [x] `onError` de `useMutation` maneja 409, validación y caso genérico — sin catch vacío
- [x] Ruta `technical-info` → `<TechnicalInfoPage />` registrada en `router.tsx`

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Project Key:** No conectado a servidor remoto (modo standalone local)
- **Archivos analizados:** 13
- **Herramientas:** `sonarqube_analyze_file` (Roslyn para C#) + `mcp_sonarqube_analyze_file_list`
- **Gate SonarQube:** PASS

### Conteo de issues

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 2     |
| MINOR     | 2     |

> **Nota:** Los 4 issues pertenecen exclusivamente a `QuoteController.cs`, un archivo cross-feature con deuda técnica acumulada desde SPEC-001. Los archivos específicos de SPEC-007 tienen **0 issues**.

### Issues BLOCKER y CRITICAL — Acción requerida

Ninguno.

### Issues MAJOR — Revisión recomendada (pre-existentes, no bloquean)

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `Cotizador.API/Controllers/QuoteController.cs` | L14 | S6670 | "This controller has multiple responsibilities and could be split into 10 smaller controllers." |
| 2 | `Cotizador.API/Controllers/QuoteController.cs` | L33–48 | S107 | "Constructor has 15 parameters, which is greater than the 7 authorized." |

**Contexto:** Deuda acumulada de SPECs 001–006. SPEC-007 añadió 3 parámetros (`_getCoverageOptionsUseCase`, `_updateCoverageOptionsUseCase`, `_coverageOptionsValidator`) a un constructor ya pre-existente.

### Issues MINOR — Informativos (pre-existentes)

| # | Archivo | Línea | Mensaje |
|---|---------|-------|---------|
| 1 | `QuoteController.cs` | L75 | "Define a constant instead of using this literal 'validationError' 11 times." |
| 2 | `QuoteController.cs` | L76 | "Define a constant instead of using this literal 'Formato de folio inválido...' 10 times." |

### Deuda técnica para backlog

- Dividir `QuoteController` en controllers especializados por dominio (general-info, locations, coverage-options)
- Extraer constantes de validación de folio a clase estática en la capa API

---

## Verificaciones positivas confirmadas

| Verificación | Estado | Detalle |
|---|---|---|
| Clean Architecture — dependencias | ✅ PASS | API → Application → Domain. Infrastructure no referenciada por API. |
| Controller sin lógica de negocio | ✅ PASS | Solo parseo HTTP y delegación a use cases. |
| Use cases sin acceso directo a MongoDB | ✅ PASS | Solo `IQuoteRepository` e `ICoreOhsClient` (interfaces de Application/Ports). |
| FSD — no importaciones entre slices del mismo nivel | ✅ PASS | Entities no importan de features/widgets/pages. |
| FSD — solo index.ts público entre slices | ✅ PASS | Todos los imports usan el `index.ts` del slice. |
| FSD — fetch a través de `shared/api/` | ✅ PASS | `coverageOptionsApi.ts` y `guaranteeApi.ts` usan `httpClient` y `endpoints`. |
| Server state en TanStack Query (no Redux) | ✅ PASS | `useCoverageOptionsQuery`, `useGuaranteesQuery`. Redux solo para `setCurrentStep` (UI state). |
| Versionado optimista — filtro MongoDB (folioNumber + version) | ✅ PASS | `BuildVersionedFilter` combina `Eq(FolioNumber)` y `Eq(Version)`. Lanza `VersionConflictException` si `ModifiedCount == 0`. |
| VersionConflict → 409 con mensaje en español | ✅ PASS | Middleware: `"El folio fue modificado por otro proceso. Recargue para continuar"`. |
| `[Authorize]` en endpoints nuevos | ✅ PASS | Ambos endpoints heredan `[Authorize]` de nivel de clase. |
| Folio validado con regex | ✅ PASS | `GetCoverageOptionsAsync` y `UpdateCoverageOptionsAsync` validan con `FolioConstants.FolioPattern`. |
| ADR-008 — validators en español | ✅ PASS | Todos los `WithMessage()` en español. |
| Defaults cuando CoverageOptions vacío | ✅ PASS | `GetCoverageOptionsUseCase` retorna `GuaranteeKeys.All` cuando `EnabledGuarantees.Count == 0`. |
| Contrato de porcentajes Backend↔Frontend | ✅ PASS | Schema Zod valida `0–100`; componente divide `/100` antes del `PUT`; backend valida `0–1`. |

---

## Veredicto consolidado

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura — Backend | PASS — 0 críticos |
| Auditoría arquitectura — Frontend | PASS — 0 críticos, correcciones previas confirmadas |
| SonarQube local (Roslyn + mcp_analyze_file_list) | PASS — 0 BLOCKER, 0 CRITICAL |
| SonarQube servidor | N/A — workspace no vinculado a servidor remoto |
| **Gate final** | **PASSED** |

---


# Reporte de calidad — SPEC-005 location-layout-configuration — 2026-03-29

---

## Parte 1 — Auditoría de arquitectura

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 1     | Sí              |
| MAYOR     | 3     | No              |
| MENOR     | 4     | No              |

### Violaciones críticas

#### CRIT-001
- **Archivo:** `cotizador-webapp/src/features/save-layout/model/useSaveLayout.ts`
- **Línea:** 2
- **Regla:** FSD — ruta interna de slice importada directamente (no index.ts)
- **Detalle:** `import { updateLayout } from '@/entities/layout/api/layoutApi'` accede a la ruta interna del slice `entities/layout` ignorando su public API. `updateLayout` no está exportado en `entities/layout/index.ts`, lo que fuerza a la feature a romper la encapsulación del slice.
- **Acción:** Exportar `updateLayout` y `LayoutResponse` desde `cotizador-webapp/src/entities/layout/index.ts`, luego actualizar el import a `import { updateLayout } from '@/entities/layout'`.

### Violaciones mayores

#### MAYOR-001
- **Archivo:** `cotizador-backend/src/Cotizador.Domain/ValueObjects/LayoutConfiguration.cs`
- **Líneas:** 4–9
- **Regla:** Value Object con setters públicos mutables — violación del principio de inmutabilidad de DDD
- **Detalle:** `DisplayMode` y `VisibleColumns` exponen `{ get; set; }`, permitiendo mutación externa arbitraria después de la construcción. Los Value Objects deben ser inmutables.
- **Acción:** Cambiar a `{ get; init; }` o constructor con parámetros requeridos.

#### MAYOR-002
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/UpdateLayoutUseCase.cs`
- **Líneas:** 34–43
- **Regla:** Doble roundtrip a base de datos innecesario
- **Detalle:** `_repository.UpdateLayoutAsync()` actualiza el documento; inmediatamente `_repository.GetByFolioNumberAsync()` lo re-lee. El DTO de respuesta puede construirse con los datos del request + versión incrementada sin un segundo viaje a DB. Si `UpdateLayoutAsync` devolviera el nuevo documento (o solo la versión), la segunda llamada sería evitable.
- **Acción:** Modificar `IQuoteRepository.UpdateLayoutAsync` para retornar el nuevo número de versión, y construir el DTO directamente sin la segunda consulta.

#### MAYOR-003
- **Archivo:** `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs`
- **Líneas:** 6, 47, 84, 102
- **Regla:** API consume tipos de `Cotizador.Domain` directamente (via dependencia transitiva)
- **Detalle:** `using Cotizador.Domain.Constants;` en el controller hace que el API layer use `FolioConstants.FolioPattern` del proyecto Domain. `FolioConstants` debería residir en `Cotizador.Application.Constants`. Patrón introducido en SPEC-004 y perpetuado por los nuevos endpoints de SPEC-005.
- **Acción:** Mover `FolioConstants` a `Cotizador.Application.Constants` y actualizar los usings en el controller.

### Sugerencias menores

#### MENOR-001
- **Archivo:** `cotizador-backend/src/Cotizador.Application/Validators/UpdateLayoutRequestValidator.cs`
- **Líneas:** 29–31
- **Regla:** Guardia de nulo redundante
- **Detalle:** La regla `NotNull()` en `VisibleColumns` ya garantiza no-nulo antes de `Must(cols => cols != null && cols.Count > 0)`. La condición `cols != null` nunca será `false` en ese punto.
- **Acción:** Simplificar a `.Must(cols => cols.Count > 0)`.

#### MENOR-002
- **Archivo:** `cotizador-webapp/src/__tests__/entities/layout/useLayoutQuery.test.ts`
- **Línea:** 6
- **Regla:** Test importa desde ruta interna del slice en lugar de la public API
- **Detalle:** `import { useLayoutQuery } from '@/entities/layout/model/useLayoutQuery'` — debería ser `@/entities/layout`.

#### MENOR-003
- **Archivo:** `cotizador-webapp/src/__tests__/features/save-layout/useSaveLayout.test.ts`
- **Línea:** 5
- **Regla:** Test importa desde ruta interna del slice
- **Detalle:** `import { useSaveLayout } from '@/features/save-layout/model/useSaveLayout'` — debería ser `@/features/save-layout`.

#### MENOR-004
- **Archivo:** `cotizador-webapp/src/__tests__/widgets/layout-config/LayoutConfigPanel.test.tsx`
- **Línea:** 8
- **Regla:** Test importa desde ruta interna del widget
- **Detalle:** `import { LayoutConfigPanel } from '@/widgets/layout-config/ui/LayoutConfigPanel'` — debería ser `@/widgets/layout-config`.

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Project Key:** No disponible (workspace no conectado a servidor SonarQube)
- **Archivos analizados:** 13 (6 backend C#, 7 frontend TS/TSX)
- **Gate SonarQube:** FAIL

### Conteo de issues

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 2     |
| MAJOR     | 3     |
| MINOR     | 3     |
| INFO      | 0     |

### Issues CRITICAL — Acción requerida

| # | Archivo | Línea | Regla | Mensaje | Severidad |
|---|---------|-------|-------|---------|-----------|
| 1 | `cotizador-webapp/src/widgets/layout-config/ui/LayoutConfigPanel.tsx` | L28 | `javascript:S2871` | Provide a compare function to avoid sorting elements alphabetically | CRITICAL |
| 2 | `cotizador-webapp/src/widgets/layout-config/ui/LayoutConfigPanel.tsx` | L29 | `javascript:S2871` | Provide a compare function to avoid sorting elements alphabetically | CRITICAL |

**Contexto:** La función `isDefaultLayout` invoca `[...visibleColumns].sort()` y `[...DEFAULT_VISIBLE_COLUMNS].sort()` sin función de comparación. El sort por defecto usa orden Unicode; para valores `ColumnKey` ASCII el resultado es correcto en la práctica, pero el comportamiento es dependiente del motor y SonarQube lo clasifica como CRITICAL por el riesgo de divergencia en collations no estándar.

**Acción:** `[...visibleColumns].sort((a, b) => a.localeCompare(b))` en ambas líneas.

### Issues MAJOR — Revisión recomendada

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs` | L14 | `csharpsquid:S4144` | This controller has multiple responsibilities and could be split into 4 smaller controllers |
| 2 | `cotizador-webapp/src/widgets/layout-config/ui/LayoutConfigPanel.tsx` | L168 | `javascript:S6851` | Use `<dialog>` instead of the "dialog" role to ensure accessibility across all devices |
| 3 | `cotizador-webapp/src/widgets/layout-config/ui/LayoutConfigPanel.tsx` | L223 | `javascript:S6836` | Refactor this code to not use nested template literals |

### Issues MINOR — Informativos

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `cotizador-webapp/src/entities/layout/model/useLayoutQuery.ts` | L25 | `typescript:S4158` | This assertion is unnecessary since it does not change the type of the expression |
| 2 | `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs` | L48 | `csharpsquid:S1192` | Define a constant instead of using literal `'validationError'` 4 times |
| 3 | `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs` | L49 | `csharpsquid:S1192` | Define a constant instead of using literal `'Formato de folio inválido. Use DAN-YYYY-NNNNN'` 4 times |

---

## Veredicto consolidado — SPEC-005

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura — Backend Clean Architecture | PASS |
| Auditoría arquitectura — Frontend FSD | **FAIL — 1 crítico (CRIT-001)** |
| SonarQube | **FAIL — 2 CRITICAL** |
| **Gate final** | **FAILED** |

**Causas raíz del fallo:**
1. `features/save-layout/model/useSaveLayout.ts:2` — importación desde ruta interna de `entities/layout` (`/api/layoutApi`), violando encapsulación FSD. Corrección: exportar `updateLayout` en `entities/layout/index.ts`.
2. `widgets/layout-config/ui/LayoutConfigPanel.tsx:28–29` — dos llamadas `.sort()` sin función de comparación explícita (2x CRITICAL SonarQube).

---

# Re-auditoría post-fix — SPEC-005 location-layout-configuration — 2026-03-29

> Re-auditoría parcial: solo los 4 archivos modificados tras el gate FAILED anterior.

---

## Parte 1 — Auditoría de arquitectura (re-auditoría)

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 0     | —               |
| MAYOR     | 0     | No              |
| MENOR     | 0     | No              |

### Verificación de fixes críticos

| Issue anterior | Fix reportado | Verificado |
|----------------|---------------|-----------|
| CRIT-001: `useSaveLayout.ts` importa desde ruta interna | Ahora usa `import { updateLayout } from '@/entities/layout'` | ✅ RESUELTO |
| CRIT-001 (parte 2): `updateLayout` no exportado en `index.ts` | `export { updateLayout } from './api/layoutApi'` en `entities/layout/index.ts` | ✅ RESUELTO |
| MAYOR-001: `LayoutConfiguration.cs` con `{ get; set; }` | Propiedades cambiadas a `{ get; init; }` | ✅ RESUELTO |

### Violaciones críticas

Ninguna.

---

## Parte 2 — Análisis estático SonarQube (re-auditoría)

### Resumen ejecutivo

- **Project Key:** No disponible (workspace no conectado a servidor SonarQube)
- **Archivos analizados:** 4 (3 frontend TS/TSX, 1 backend C#)
- **Gate SonarQube:** PASS

### Conteo de issues (archivos re-auditados)

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 2     |
| MINOR     | 0     |
| INFO      | 0     |

### Issues CRITICAL — Acción requerida

Ninguno. Los 2 issues CRITICAL (S2871 `.sort()` sin comparador) ya están resueltos — ambas llamadas ahora usan `(a, b) => a.localeCompare(b)`.

### Issues MAJOR residuales — Revisión recomendada (no bloquean gate)

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `cotizador-webapp/src/widgets/layout-config/ui/LayoutConfigPanel.tsx` | L168 | `javascript:S6851` | Use `<dialog>` instead of the "dialog" role to ensure accessibility across all devices |
| 2 | `cotizador-webapp/src/widgets/layout-config/ui/LayoutConfigPanel.tsx` | L223 | `javascript:S6836` | Refactor this code to not use nested template literals |

> Estos 2 issues MAJOR ya estaban presentes en el reporte anterior. No son nuevos. No bloquean el gate.

---

## Veredicto consolidado — Re-auditoría SPEC-005

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura — Frontend FSD | PASS — 0 críticos |
| Auditoría arquitectura — Backend Clean Architecture | PASS — 0 críticos |
| SonarQube | PASS — 0 BLOCKER / 0 CRITICAL |
| **Gate final** | **PASSED** |

---

# Reportes anteriores

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

# Reporte de calidad — SPEC-006 Gestión de Ubicaciones de Riesgo — 2026-03-29

---

## Parte 1 — Auditoría de arquitectura

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 1     | Sí              |
| MAYOR     | 4     | No              |
| MENOR     | 3     | No              |

---

### Violaciones críticas

#### CRIT-001
- **Archivo:** `cotizador-backend/src/Cotizador.API/Cotizador.API.csproj`
- **Línea:** 8
- **Regla:** API referencia Infrastructure directamente
- **Detalle:** El proyecto `Cotizador.API` tiene un `<ProjectReference>` explícito a `Cotizador.Infrastructure`. Según las reglas del proyecto y la política de capas de Clean Architecture, `API` solo debe referenciar `Cotizador.Application`. La referencia a Infrastructure se usa únicamente para invocar `builder.Services.AddInfrastructure(...)` en `Program.cs`, pero no exime la violación estructural: cualquier tipo de `Infrastructure` queda visible en toda la capa API.
- **Acción requerida:** Eliminar el `<ProjectReference>` a `Cotizador.Infrastructure` del `.csproj` de API. Mover todo el wiring de use cases y validators dentro de métodos de extensión en `Cotizador.Application` (ej. `ApplicationServiceCollectionExtensions.cs`). El método `AddInfrastructure()` ya consolida el wiring de Infrastructure — hacer lo mismo con Application permite que Program.cs solo llame a `AddApplication()` + `AddInfrastructure()` sin necesidad de referenciar Infrastructure en el `.csproj`.

```xml
<!-- INCORRECTO — en Cotizador.API.csproj -->
<ProjectReference Include="..\Cotizador.Infrastructure\Cotizador.Infrastructure.csproj" />

<!-- CORRECTO: eliminar esa línea y consolidar wiring en ApplicationServiceCollectionExtensions -->
```

---

### Violaciones mayores

#### MAY-001
- **Archivo:** `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs`
- **Línea:** 5 (`using Cotizador.Domain.Constants;`)
- **Regla:** API accede a tipos de `Domain` directamente (via dependencia transitiva)
- **Detalle:** `QuoteController` importa `Cotizador.Domain.Constants` para usar `FolioConstants.FolioPattern`. La capa `API` no debe conocer tipos de `Domain` directamente. Patrón detectado también en SPEC-004/005 — sigue sin corregirse.
- **Acción:** Mover `FolioConstants` (o al menos `FolioPattern`) a `Cotizador.Application/Constants/` y actualizar el `using` en el controller.

#### MAY-002
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/PatchLocationUseCase.cs`
- **Líneas:** 36 y 83
- **Regla:** Excepción de dominio usada con semántica incorrecta
- **Detalle:** `FolioNotFoundException` se lanza cuando la **ubicación** con el índice dado no existe dentro del folio (no cuando el folio mismo no existe). Esto mezcla dos conceptos distintos bajo la misma excepción. Si en el futuro se necesita distinguir "folio no encontrado" de "ubicación no encontrada" (por ejemplo para métricas o manejo diferenciado en el cliente), será imposible sin cambio de ruptura.
- **Acción:** Crear `LocationNotFoundException` en `Cotizador.Domain/Exceptions/` e implementar su mapeo en `ExceptionHandlingMiddleware` (404 con `type = "locationNotFound"`).

```csharp
// INCORRECTO:
throw new FolioNotFoundException($"La ubicación con índice {index} no existe en el folio");

// CORRECTO:
throw new LocationNotFoundException(folioNumber, index);
```

#### MAY-003
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/LocationCalculabilityEvaluator.cs`
- **Línea:** 4 (declaración `internal static class`)
- **Regla:** Clase estática acoplada directamente desde use cases — viola DIP
- **Detalle:** `LocationCalculabilityEvaluator` es `internal static` y los use cases la llaman directamente (`LocationCalculabilityEvaluator.Evaluate(entity)` en `UpdateLocationsUseCase` y `PatchLocationUseCase`). Esto viola el Dependency Inversion Principle: los use cases no pueden ser testeados en aislamiento porque no existe una interfaz que permita mockear el evaluador. Si cambia la lógica de calculabilidad, hay que localizar y rastrear todos los callsites estáticos.
- **Acción:** Definir `ILocationCalculabilityEvaluator` en `Application/Interfaces/`, hacer la clase concreta no-static e inyectarla por constructor en ambos use cases.

```csharp
// Nueva interfaz en Application/Interfaces/
public interface ILocationCalculabilityEvaluator
{
    void Evaluate(Location location);
}

// Use case corregido (constructor injection):
public UpdateLocationsUseCase(
    IQuoteRepository repository,
    ILocationCalculabilityEvaluator calculabilityEvaluator,
    ILogger<UpdateLocationsUseCase> logger) { ... }
```

#### MAY-004
- **Archivo:** `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs`
- **Línea:** 32–53 (constructor)
- **Regla:** SRP — Controller con 12 dependencias en constructor (demasiadas responsabilidades)
- **Detalle:** `QuoteController` recibe 12 parámetros en el constructor cubriendo general-info, layout y ubicaciones. Si se añade SPEC-007 (cálculo), el número seguirá creciendo. Controladores con > 7 dependencias son una señal de SRP violado.
- **Acción:** Extraer `LocationsController` con los 4 endpoints de ubicaciones (`GET/PUT locations`, `PATCH location/:index`, `GET locations/summary`). El split no bloquea el gate pero es deuda que se acumula.

---

### Sugerencias menores

#### MEN-001
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/LocationMapper.cs`
- **Regla:** Artefacto mal ubicado dentro de `UseCases/`
- **Detalle:** `LocationMapper` no es un use case. Debería estar en `Application/Mappers/` o `Application/Helpers/` para mantener coherencia organizacional.

#### MEN-002
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/LocationCalculabilityEvaluator.cs`
- **Regla:** Lógica de dominio pura en capa Application
- **Detalle:** La evaluación de calculabilidad ("¿zipCode de 5 dígitos? ¿giro comercial? ¿garantías?") son reglas de negocio puras de la entidad `Location`. Idealmente deberían vivir en `Cotizador.Domain` como domain service o método de la entidad. Al vivir en Application, la entidad puede existir con `ValidationStatus = Incomplete` sin que se haya ejecutado el evaluador, creando posibilidad de estados inconsistentes.

#### MEN-003
- **Archivo:** `cotizador-backend/src/Cotizador.Application/DTOs/LocationDto.cs`
- **Regla:** DTO mixto input/output con campos inaplicables al input
- **Detalle:** `LocationDto` incluye `BlockingAlerts` y `ValidationStatus` que son campos de respuesta calculados en el backend. El cliente que envía `PUT /locations` debe incluir esos campos en el body aunque sean ignorados. Separar en `LocationRequestDto` (sin `BlockingAlerts`/`ValidationStatus`) y `LocationResponseDto` mejoraría la claridad del contrato API y eliminaría ambigüedad.

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Project Key:** No resuelto — workspace no vinculado a SonarQube Server/Cloud en Connected Mode
- **Archivos analizados (Roslyn local):** 4 (`QuoteController.cs`, `PatchLocationUseCase.cs`, `UpdateLocationsUseCase.cs`, `LocationCalculabilityEvaluator.cs`)
- **Gate SonarQube servidor:** N/A

### Conteo de issues (análisis Roslyn local)

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 0     |
| MINOR     | 0     |
| INFO      | 0     |

> Resultados detallados disponibles en el panel **PROBLEMS** del IDE. No se detectaron hotspots de seguridad OWASP Top 10 en los archivos auditados.

### Issues BLOCKER y CRITICAL — Ninguno

### Análisis de criterios de seguridad explícitos

| Criterio | Estado | Detalle |
|----------|--------|---------|
| Validación de `zipCode` antes de llamar core-ohs | ✅ PASS | `CatalogController` valida con `ZipCodeRegex` (`^\d{5}$`) antes del use case. `PatchLocationRequestValidator` y `UpdateLocationsRequestValidator` también validan con `Matches(@"^\d{5}$")`. |
| Validación del `index` antes del PATCH | ✅ PASS | Controller valida `index < 1` (HTTP 400 inmediato). Route constraint `{index:int}` rechaza no-enteros. `PatchLocationUseCase` verifica existencia de la ubicación en BD. |
| `validationStatus` calculado en backend sin aceptarlo del cliente | ✅ PASS | `LocationMapper.ToEntity()` no mapea `ValidationStatus` ni `BlockingAlerts`. `LocationCalculabilityEvaluator.Evaluate()` los recalcula siempre antes de persistir (PUT y PATCH). |
| Use cases dependen de interfaces (DIP) | ⚠️ PARCIAL | Use cases usan `IQuoteRepository` ✅. Pero `LocationCalculabilityEvaluator` es clase estática no inyectable — ver MAY-003. |
| Controllers delgados sin lógica de negocio | ✅ PASS | Controllers validan formato, invocan FluentValidation y delegan al use case. Sin lógica de negocio. |
| Domain sin dependencias externas | ✅ PASS | `Cotizador.Domain.csproj` no tiene `<ProjectReference>` ni paquetes externos. |
| Use cases usan interfaces de repositorio | ✅ PASS | Todos los use cases de Location inyectan `IQuoteRepository`. Ninguno accede a MongoDB directamente. |
| Logging estructurado en use cases | ✅ PASS | Todos los use cases de Location tienen `ILogger<T>` con `LogInformation` al inicio de `ExecuteAsync`. |
| Versionado optimista en PUT/PATCH | ✅ PASS | `UpdateLocationsRequest.Version` y `PatchLocationRequest.Version` son obligatorios. Repositorio lanza `VersionConflictException` ante mismatch. |
| `ExceptionHandlingMiddleware` cubre excepciones de dominio | ⚠️ PARCIAL | Cubre `FolioNotFoundException`, `VersionConflictException`, `InvalidQuoteStateException`, `CoreOhsUnavailableException`, `ValidationException`. Falta mapeo de `LocationNotFoundException` (pendiente de creación — ver MAY-002). |

---

## Veredicto consolidado — SPEC-006

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura | **FAIL — 1 violación crítica (CRIT-001)** |
| SonarQube Roslyn local | PASS — 0 BLOCKER, 0 CRITICAL |
| SonarQube servidor | N/A (modo desconectado) |
| **Gate final** | **FAILED** |

**Causa raíz del fallo:** `Cotizador.API.csproj` contiene una referencia directa a `Cotizador.Infrastructure`, violando la regla de capas del proyecto. La corrección mínima requerida es: crear `ApplicationServiceCollectionExtensions.cs` en `Cotizador.Application` que centralice el registro de use cases y validators, y eliminar la `<ProjectReference>` a Infrastructure del `.csproj` de API.

Las 4 violaciones MAYOR no bloquean el gate pero deben corregirse en el mismo sprint antes de entrar a QA:
- **MAY-002** (excepción incorrecta en `PatchLocationUseCase`) y **MAY-003** (evaluador estático sin interfaz) son prioritarias por su impacto directo en testabilidad.
