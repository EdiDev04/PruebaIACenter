# Reporte de Calidad — quote-state-progress (SPEC-008) — 2026-03-30

> **Re-auditoría post-fix** — 2026-03-30. CRIT-001 resuelto. Gate anterior: FAILED → Gate actual: **PASSED**.

---

## Parte 1 — Auditoría de Arquitectura

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 0     | —               |
| MAYOR     | 3     | No              |
| MENOR     | 2     | No              |

---

### Violaciones críticas

Ninguna.

> **CRIT-001 — RESUELTO** (post-fix 2026-03-30): `WizardLayout.tsx` fue movido de `cotizador-webapp/src/widgets/WizardLayout.tsx` a `cotizador-webapp/src/app/WizardLayout.tsx`. La capa `app/` está por encima de `widgets/` en la jerarquía FSD, por lo que las importaciones de `@/widgets/progress-bar`, `@/widgets/location-alerts` y `@/widgets/WizardHeader` son válidas. El archivo ya no existe en `widgets/` y `widgets/index.ts` no lo exporta. El router lo importa correctamente con ruta relativa `'../WizardLayout'` dentro del mismo slice `app/`.

---

### Violaciones mayores

#### MAY-001 — PERSISTE (no corregido)
- **Archivo:** `cotizador-webapp/src/app/WizardLayout.tsx`
- **Línea:** 10
- **Regla:** Error silencioso en query — sin manejo de estado de error o carga
- **Detalle:** `const { data: quoteState } = useQuoteStateQuery(folioNumber)` sigue desestructurando únicamente `data`. Si la query falla (red caída, 401, 503), `quoteState` es `undefined` y la barra de progreso y las alertas desaparecen sin retroalimentación al usuario.
- **Estado:** Sin cambios respecto al reporte anterior. **No se aplicó fix.**
- **Acción recomendada:** Desestructurar también `isLoading` e `isError` y mostrar un estado de carga/error apropiado.

#### MAY-002 — ACEPTADO (placeholder SPEC-009)
- **Archivo:** `cotizador-backend/src/Cotizador.Application/UseCases/GetQuoteStateUseCase.cs`
- **Línea:** 86
- **Regla:** Valor hardcodeado sin indicación clara de deuda técnica
- **Detalle:** `CommercialPremiumBeforeTax: 0m` está hardcodeado con el comentario `// Campo pendiente de SPEC-009 (motor de cálculo)`. `CalculationResultDto` usa `decimal` (no nullable), por lo que `0m` se retorna como valor aparentemente válido.
- **Estado:** **ACEPTADO** por spec — placeholder explícito de SPEC-009. No bloquea el gate de SPEC-008.
- **Acción recomendada (SPEC-009):** Hacer `CommercialPremiumBeforeTax` nullable (`decimal?`) hasta que el motor de cálculo esté implementado.

#### MAY-003 — PRE-EXISTENTE (sin cambios)
- **Archivo:** `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs`
- **Líneas:** múltiples (74, 95, 115, 136, 156, 177, 201, 231, 249, 267, 288…)
- **Regla:** DRY — lógica de validación duplicada en cada action method
- **Detalle (pre-existente):** El bloque de validación de folio via `Regex.IsMatch(folio, FolioConstants.FolioPattern)` se repite idénticamente en los 11+ action methods del controller. El endpoint `GetQuoteStateAsync` sigue el mismo patrón.
- **Estado:** Pre-existente, fuera del scope de SPEC-008. Sin cambios.
- **Acción recomendada (baja urgencia):** Extraer la validación a un `ActionFilter` o a un método privado `ValidateFolioFormat(folio)` que retorne `IActionResult?`.

---

### Sugerencias menores

#### MIN-001 — PERSISTE
- **Archivo:** `cotizador-webapp/src/widgets/progress-bar/ui/ProgressBar.tsx`
- **Líneas:** 12–13
- **Detalle:** `layoutConfiguration` y `locations` apuntan al mismo `path: 'locations'`. Si existen rutas separadas para layout (`/locations/layout`) y ubicaciones (`/locations`), el step "Layout" navega incorrectamente.
- **Estado:** Sin cambios. No forma parte del scope de SPEC-008.
- **Acción recomendada:** Verificar la configuración del router y ajustar el `path` del step `layoutConfiguration` si corresponde (p.ej. `'locations/layout'`).

#### MIN-002 — PERSISTE
- **Archivo:** `cotizador-backend/src/Cotizador.API/Controllers/QuoteController.cs`
- **Detalle:** `Regex.IsMatch(string, string)` usa el caché interno de .NET (hasta 15 entradas). Con 11+ patterns en el proceso, podría saturar el caché. Alternativa: usar `[GeneratedRegex]` (C# 7+) o un campo estático `static readonly Regex _folioRegex = new(FolioConstants.FolioPattern, RegexOptions.Compiled)`.
- **Estado:** Sin cambios. No bloquea (patrón simple y anclado, sin riesgo de ReDoS).

---

## Parte 2 — Análisis Estático SonarQube

### Resumen ejecutivo

- **Project Key:** `cotizador-backend` (sonar.properties) — servidor remoto no disponible
- **Modo:** Análisis local con SonarQube for IDE (Roslyn) — 4 archivos C# analizados; revisión manual para archivos TypeScript/TSX
- **Archivos analizados:** 9 (4 backend `.cs`, 5 frontend `.ts`/`.tsx`)
- **Gate SonarQube servidor:** N/A (sin servidor conectado)

> ⚠️ El workspace no está vinculado a un proyecto SonarQube remoto. Se ejecutó `sonarqube_analyze_file` sobre los 4 archivos C# (análisis Roslyn local). `sonarqube_list_potential_security_issues` no pudo recuperar hotspots sin modo conectado. Las conclusiones de seguridad provienen de revisión manual de código.

### Archivos analizados

| # | Archivo | Lenguaje | Método |
|---|---------|----------|--------|
| 1 | `Application/DTOs/QuoteStateDtos.cs` | C# | sonarqube_analyze_file |
| 2 | `Application/Interfaces/IGetQuoteStateUseCase.cs` | C# | sonarqube_analyze_file |
| 3 | `Application/UseCases/GetQuoteStateUseCase.cs` | C# | sonarqube_analyze_file |
| 4 | `API/Controllers/QuoteController.cs` | C# | sonarqube_analyze_file |
| 5 | `entities/quote-state/model/types.ts` | TypeScript | Revisión manual |
| 6 | `entities/quote-state/model/useQuoteStateQuery.ts` | TypeScript | Revisión manual |
| 7 | `entities/quote-state/api/quoteStateApi.ts` | TypeScript | Revisión manual |
| 8 | `widgets/progress-bar/ui/ProgressBar.tsx` | TypeScript | Revisión manual |
| 9 | `widgets/location-alerts/ui/LocationAlerts.tsx` | TypeScript | Revisión manual |

### Conteo de issues (revisión manual + Roslyn)

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 0     |
| MAJOR     | 1     |
| MINOR     | 1     |
| INFO      | 0     |

### Issues BLOCKER y CRITICAL — Ninguno

### Issues MAJOR — Revisión recomendada

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `Application/UseCases/GetQuoteStateUseCase.cs` | 86 | `S1192` / hardcoded-value | `CommercialPremiumBeforeTax: 0m` hardcodeado — retorna valor ficticio a clientes (aceptado como placeholder SPEC-009) |

### Issues MINOR

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `API/Controllers/QuoteController.cs` | múltiples | `S1067` / duplicate-code | Regex.IsMatch duplicado en 11+ métodos sin extracción a helper (pre-existente) |

---

## Checklist de Seguridad (OWASP Top 10)

| Control | Estado | Detalle |
|---------|--------|---------|
| Autenticación en endpoint | ✅ PASS | `[Authorize]` a nivel de clase cubre todos los endpoints incluyendo `/state` |
| Validación de path param `folio` | ✅ PASS | `Regex.IsMatch(folio, FolioConstants.FolioPattern)` con patrón `^DAN-\d{4}-\d{5}$` antes de cualquier uso |
| Exposición de datos sensibles | ✅ PASS | Los DTOs no incluyen credenciales, tokens ni datos personales sensibles |
| ReDoS en regex | ✅ PASS | Patrón simple y anclado (`^...$`) — sin backtracking exponencial |
| Logging seguro (no log injection) | ✅ PASS | `LogInformation("... {UseCase} ... {Folio}", ...)` — structured logging, no interpolación |
| Inyección de dependencias correcta | ✅ PASS | `IGetQuoteStateUseCase` inyectado como Scoped a través de DI |
| Acceso directo a MongoDB en Controller | ✅ PASS | Controller solo interactúa con IGetQuoteStateUseCase |
| URL hardcodeada en frontend | ✅ PASS | `endpoints.quoteState(folio)` — centralizado en `shared/api/endpoints.ts` |
| `any` en TypeScript | ✅ PASS | Sin uso de `any` en los archivos auditados |

---

## Verificación de Clean Architecture (Backend)

| Regla | Estado | Detalle |
|-------|--------|---------|
| API no referencia Infrastructure directamente | ✅ PASS | `QuoteController` solo importa `Application.DTOs` e `Application.Interfaces` |
| Use Case depende solo de interfaces (IRepository) | ✅ PASS | `GetQuoteStateUseCase` inyecta `IQuoteRepository` (puerto en Application/Ports) |
| Controller sin lógica de negocio | ✅ PASS | `GetQuoteStateAsync` valida formato folio y delega a `_getQuoteStateUseCase.ExecuteAsync` |
| Infrastructure no referencia API | ✅ PASS | (No hay modificaciones a Infrastructure en SPEC-008) |
| DTOs en Application layer | ✅ PASS | `QuoteStateDtos.cs` ubicado en `Cotizador.Application/DTOs/` |
| Async/Await correcto | ✅ PASS | Sin `.Result` ni `.Wait()` — CancellationToken propagado correctamente |
| Registro DI correcto | ✅ PASS | `services.AddScoped<IGetQuoteStateUseCase, GetQuoteStateUseCase>()` en `ApplicationServiceCollectionExtensions` |

---

## Verificación FSD (Frontend) — Post-fix

| Regla | Estado | Detalle |
|-------|--------|---------|
| `app/WizardLayout` no importa de otros widgets | ✅ PASS | `app/` layer puede importar de `widgets/` — válido en FSD |
| `WizardLayout` eliminado de `widgets/` | ✅ PASS | Ningún archivo `WizardLayout*` existe en `cotizador-webapp/src/widgets/` |
| `widgets/index.ts` no exporta `WizardLayout` | ✅ PASS | Solo exporta: `FolioActionCard`, `WizardHeader`, `WizardStepNav`, `CoverageOptionsForm` |
| `router.tsx` importa `WizardLayout` desde `app/` | ✅ PASS | `import { WizardLayout } from '../WizardLayout'` — ruta relativa dentro del slice `app/` |
| `entities/quote-state` no importa de `widgets/` | ✅ PASS | Solo importa de `@/shared/api` y módulos internos |
| `widgets/progress-bar` importa solo de `entities/` y `shared/` | ✅ PASS | Importa `ProgressDto` de `@/entities/quote-state` |
| `widgets/location-alerts` importa solo de `entities/` y `shared/` | ✅ PASS | Importa `LocationAlertDto` de `@/entities/quote-state` |
| Public APIs via index.ts | ✅ PASS | Los slices exportan exclusivamente desde `index.ts` |
| Sin fetch directo en componentes | ✅ PASS | Todo el acceso HTTP va a través de `shared/api/httpClient` |
| Sin Server State en Redux | ✅ PASS | React Query gestiona el server state; Redux no es usado para quotes |

---

## Veredicto Consolidado

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura | ✅ PASS — 0 violaciones críticas (CRIT-001 resuelto) |
| SonarQube local (Roslyn) | ✅ PASS — 0 BLOCKER, 0 CRITICAL detectados |
| Seguridad OWASP | ✅ PASS — todos los controles verificados |
| **Gate final** | ✅ **PASSED** |

> El fix de CRIT-001 es correcto y completo. `WizardLayout` en `app/` puede importar de `widgets/` sin violar FSD. No existen violaciones críticas ni issues bloqueantes SonarQube pendientes.

---

## Issues pendientes (no bloqueantes)

| ID | Archivo | Severidad | Estado | Notas |
|----|---------|-----------|--------|-------|
| MAY-001 | `app/WizardLayout.tsx:10` | MAYOR | Abierto | `isError`/`isLoading` no manejados. Crear ticket de seguimiento |
| MAY-002 | `GetQuoteStateUseCase.cs:86` | MAYOR | Aceptado | Placeholder SPEC-009. Resolver en SPEC-009 |
| MAY-003 | `QuoteController.cs:múltiples` | MAYOR | Abierto | DRY pre-existente, fuera del scope de SPEC-008 |
| MIN-001 | `ProgressBar.tsx:12` | MENOR | Abierto | Mismo `path` para dos steps distintos |
| MIN-002 | `QuoteController.cs:múltiples` | MENOR | Abierto | `Regex.IsMatch` sin campo estático compilado |
