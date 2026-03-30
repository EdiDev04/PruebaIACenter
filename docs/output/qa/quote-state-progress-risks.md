# Matriz de Riesgos — Estado y Progreso de la Cotización (SPEC-008)

> **Feature:** `quote-state-progress`
> **Clasificación:** Regla ASD — Alto (A) | Medio (S) | Bajo (D)
> **Spec origen:** `.github/specs/quote-state-progress.spec.md`
> **Contexto:** Proyecto en modo **orchestrator-lite** — tests unitarios diferidos (0% cobertura actual). Riesgo aceptado conscientemente.
> **Generado:** 2026-03-30 | **Agente:** qa-agent

---

## Resumen Ejecutivo

| Nivel | Cantidad | Acción requerida |
|-------|----------|-----------------|
| **ALTO (A)** | 4 | Testing OBLIGATORIO — bloquea release |
| **MEDIO (S)** | 4 | Testing RECOMENDADO — documentar si se omite |
| **BAJO (D)** | 3 | Testing OPCIONAL — priorizar en backlog |
| **Total** | **11** | — |

---

## Tabla de Riesgos

| ID | HU / Regla | Área | Descripción del Riesgo | Probabilidad | Impacto | Nivel ASD | Testing |
|----|-----------|------|------------------------|-------------|---------|-----------|---------|
| R-001 | §8.3 | Cobertura de código | 0% tests unitarios en `GetQuoteStateUseCase` — derivación de progreso sin verificación automática (diferido orchestrator-lite) | Alta | Alto | **A** | Obligatorio |
| R-002 | MAY-001 | UX / Frontend | `isLoading`/`isError` no manejados en `WizardLayout.tsx` — UX silencioso cuando el estado falla | Media | Alto | **A** | Obligatorio |
| R-003 | MAY-002 | BE — Datos financieros | `CommercialPremiumBeforeTax: 0m` hardcodeado — retorna como valor aparentemente válido hasta SPEC-009 | Alta | Alto | **A** | Obligatorio |
| R-004 | RN-008-03 / RN-008-06 | Contrato BE↔FE | Sin tests de contrato (Pact/schema validation) — drift silencioso si el shape del QuoteStateDto cambia | Media | Alto | **A** | Obligatorio |
| R-005 | §3.7 | Performance / Caché | `staleTime: 0` — cada página del wizard dispara un fetch; sin SLAs definidos, el impacto es indetectable | Baja | Medio | **S** | Recomendado |
| R-006 | SUP-008-04 | Concurrencia / UX | Doble pestaña editando el mismo folio — una pestaña permanece con datos desactualizados hasta la siguiente navegación | Media | Medio | **S** | Recomendado |
| R-007 | SUP-008-03 | Integración SPEC-006 | `missingFields` derivados de `BlockingAlerts` de SPEC-006 — si ese formato cambia, ambos endpoints fallan sin alerta | Media | Medio | **S** | Recomendado |
| R-008 | MIN-002 | BE — Performance | `Regex.IsMatch(folio, pattern)` sin compilar — posible saturación del caché interno de .NET (>15 entradas) con 11+ endpoints | Baja | Medio | **S** | Recomendado |
| R-009 | MIN-001 | FE — Navegación | `layoutConfiguration` y `locations` apuntan al mismo `path: 'locations'` en `ProgressBar.tsx` — clic en "Layout" puede navegar a ruta incorrecta | Baja | Bajo | **D** | Opcional |
| R-010 | MAY-003 | BE — Mantenibilidad | Bloque de validación de folio duplicado en 11+ action methods (`QuoteController.cs`) — riesgo de inconsistencia futura | Baja | Bajo | **D** | Opcional |
| R-011 | HU-008-02 | FE — Progreso visual | ProgressBar visible en todas las páginas del wizard — si la query falla sin manejo, el componente desaparece (subriesgo de R-002) | Media | Bajo | **D** | Opcional |

---

## Detalle de Riesgos ALTO (A)

---

### R-001 — 0% Tests Unitarios en `GetQuoteStateUseCase` (diferido orchestrator-lite)

**Descripción completa:**
El proyecto opera en modo **orchestrator-lite**, lo que significa que la Fase 3 (test-engineer-backend) está diferida. La lógica de derivación de progreso (`generalInfo`, `layoutConfiguration`, `locations`, `coverageOptions`, `readyForCalculation`, `calculationResult`) no tiene cobertura unitaria. Nueve casos de prueba están especificados en §8.3 pero no implementados.

**Lógica sin cobertura:**
- Derivación de `generalInfo` desde `InsuredData.Name` (RN-008-02)
- Derivación de `locations` desde `Locations.Count > 0` (RN-008-04)
- Derivación de `coverageOptions` desde `EnabledGuarantees.Count > 0` (RN-008-05)
- `readyForCalculation` cuando calculable > 0 (RN-008-06)
- `calculationResult` incluido solo cuando `quoteStatus == "calculated"` (RN-008-09)
- `FolioNotFoundException` cuando el folio no existe

**Riesgo aceptado conscientemente:**
Este riesgo es una **deuda técnica intencionada** del modelo orchestrator-lite. El equipo acepta que la lógica de derivación no está verificada automáticamente hasta que se active el orchestrator completo.

**Mitigación actual:**
- Cobertura parcial vía tests E2E / integración (propuesta de automatización)
- Revisión manual de cada caso durante testing exploratorio

**Mitigación definitiva:**
> ⚠️ **Activar orchestrator completo para SPEC-008** e implementar los 9 casos `GetQuoteStateUseCaseTests` definidos en §8.3 de la spec.

**Tests obligatorios al activar:**
1. Folio draft → todos false excepto `layoutConfiguration`
2. Folio in_progress con Name → `generalInfo: true`
3. Folio con ubicaciones → `locations: true`, conteo correcto
4. Folio con `enabledGuarantees > 0` → `coverageOptions: true`
5. Folio con `enabledGuarantees` vacío → `coverageOptions: false`
6. 1 calculable + 1 incompleta → `readyForCalculation: true`, alertas correctas
7. 0 calculables → `readyForCalculation: false`
8. Folio calculado → `calculationResult != null` con datos financieros
9. Folio inexistente → lanza `FolioNotFoundException`

**Bloqueante para release:** ✅ Sí (mitigado temporalmente por orchestrator-lite)

---

### R-002 — MAY-001: `isLoading`/`isError` no manejados — UX silencioso

**Descripción completa:**
En `cotizador-webapp/src/app/WizardLayout.tsx` (línea 10), la desestructuración es:

```ts
const { data: quoteState } = useQuoteStateQuery(folioNumber)
```

Solo se desestructura `data`. Si la query falla (red caída, 401, 503, timeout), `quoteState` queda `undefined` y:
- La `ProgressBar` desaparece silenciosamente
- Las `LocationAlerts` desaparecen silenciosamente
- El usuario no recibe retroalimentación alguna

Esto viola el principio de **feedback al usuario** bajo error y puede causar confusión durante testing de regresión.

**Condiciones que activan el riesgo:**
- MongoDB no disponible → 500
- Token expirado durante sesión activa → 401
- Timeout de red (>3 segundos) → query en `isLoading` sin renderizar fallback

**Mitigación inmediata (SPEC-008):**
```ts
const { data: quoteState, isLoading, isError } = useQuoteStateQuery(folioNumber)
if (isLoading) return <ProgressBarSkeleton />
if (isError) return <AlertaError mensaje="No se pudo cargar el estado de la cotización" />
```

**Tests obligatorios:**
- Test E2E: simular fallo de red en `GET /state` → verificar que aparece feedback de error (no silencio)
- Test unitario frontend: `WizardLayout` renderiza skeleton cuando `isLoading: true`
- Test unitario frontend: `WizardLayout` renderiza mensaje de error cuando `isError: true`

**Bloqueante para release:** ✅ Sí — UX silencioso en rutas de error es inaceptable para producción

---

### R-003 — MAY-002: `CommercialPremiumBeforeTax: 0m` — Dato financiero engañoso

**Descripción completa:**
En `GetQuoteStateUseCase.cs` (línea 86), el campo `CommercialPremiumBeforeTax` retorna `0m` como decimal no nullable. Para un folio `calculated`, la respuesta incluye:

```json
"calculationResult": {
  "netPremium": 125000.50,
  "commercialPremiumBeforeTax": 0,   ← placeholder engañoso
  "commercialPremium": 174000.70,
  ...
}
```

**El problema:** `0` es un valor aparentemente válido. El sistema de gestión de pólizas consumidor podría interpretar `0` como "sin impuesto antes de IVA" y usarlo en cálculos downstream incorrectos.

**Estado actual:** ACEPTADO por spec — placeholder explícito de SPEC-009. No bloquea el gate de SPEC-008.

**Mitigación para SPEC-009:**
- Hacer `CommercialPremiumBeforeTax` nullable (`decimal?`) hasta que SPEC-009 implemente el motor de cálculo
- Retornar `null` en vez de `0m` para distinguir "no calculado" de "calculado en cero"

**Tests obligatorios:**
- Test de contrato: documentar que `commercialPremiumBeforeTax` es `0` intencionalmente en SPEC-008
- Test de regresión SPEC-009: verificar que el valor real reemplaza al `0` tras activar el motor

**Bloqueante para release:** ⚠️ Parcial — aceptado para SPEC-008, bloqueante para SPEC-009.

---

### R-004 — Sin Validación de Contrato BE↔FE (Pact / Schema Validation)

**Descripción completa:**
No existe ningún mecanismo automatizado de validación del contrato entre el backend (`QuoteStateDto`) y el frontend (`QuoteState` TypeScript type). Si el backend agrega, elimina o renombra un campo en el DTO (por ejemplo, al migrar de `generalInfo` a `general_info` o al agregar `calculationResult.premiumsByLocation` en la respuesta), el frontend falla silenciosamente en runtime.

**Campos críticos sin contrato validado:**

| Campo | Riesgo de drift |
|-------|----------------|
| `data.progress.generalInfo` | Renaming: `general_info` vs `generalInfo` |
| `data.locations.alerts[].missingFields` | Array → puede volverse objeto en refactor |
| `data.calculationResult` | Campo opcional → puede desaparecer en refactor |

**Mitigación recomendada:**
- Implementar schema validation con `zod` en el cliente FE (parsear `QuoteStateDto` al vuelo)
- Opcionalmente: Pact consumer-driven contracts entre `quoteStateApi.ts` y `QuoteController`

**Tests obligatorios:**
- Test de integración: enviar respuesta real del BE, parsear con schema `zod`, verificar que no hay `ZodError`
- Alertar si el shape de `QuoteStateDto` cambia sin actualizar el schema del FE

**Bloqueante para release:** ✅ Sí — drift silencioso puede causar bugs en producción que no aparecen en desarrollo

---

## Detalle de Riesgos MEDIO (S)

---

### R-005 — `staleTime: 0` — Impacto en Performance Bajo Múltiples Páginas del Wizard

**Descripción:**
La query `useQuoteStateQuery` tiene `staleTime: 0`. Cada vez que el usuario navega entre páginas del wizard (General Info → Locations → Technical Info → Terms), TanStack Query realiza un nuevo fetch al endpoint `GET /v1/quotes/{folio}/state`. Si el usuario navega frecuentemente (p.ej., retrocede y avanza 5 veces), genera 5+ peticiones en pocos segundos.

**Impacto real:**
Sin SLAs definidos en la spec, el impacto actual es bajo. Sin embargo, si en el futuro se agrega SLA (p.ej. `P95 < 200ms`), este patrón puede degradar la experiencia en conexiones lentas.

**Mitigación recomendada:**
- Establecer `staleTime: 30_000` (30s) para reducir fetches en navegación rápida
- Invalidar manualmente la cache solo al mutar (PUT general-info, PUT locations, POST calculate)

---

### R-006 — Doble Pestaña Editando el Mismo Folio

**Descripción:**
SUP-008-04 documenta que si dos pestañas editan el mismo folio, una no verá cambios hasta navegar. Esto puede causar que un usuario guarde sobre datos más recientes sin saberlo (versionado optimista mitiga el conflicto en escritura, pero no en lectura de estado).

**Mitigación recomendada:**
- Test exploratorio con 2 pestañas simultáneas
- Evaluar si agregar un indicador visual de "datos desactualizados" (p.ej. un `refetchOnWindowFocus: true`)

---

### R-007 — Dependencia en `BlockingAlerts` de SPEC-006

**Descripción:**
`missingFields` en las alertas se deriva de `blockingAlerts` calculados por `LocationCalculabilityEvaluator` (SPEC-006). Si SPEC-006 cambia el formato de `blockingAlerts` (p.ej., de `string[]` a `{ code: string, description: string }[]`), SPEC-008 falla silenciosamente sin un test de contrato cruzado.

**Mitigación recomendada:**
- Test de integración que valide el formato de `blockingAlerts` al construir `LocationAlertDto`

---

### R-008 — Regex sin Compilar en `QuoteController` — Saturación de Caché (MIN-002)

**Descripción:**
`Regex.IsMatch(folio, FolioConstants.FolioPattern)` usa el caché interno de .NET (hasta 15 entradas). Con 11+ endpoints en el controller usando el mismo patrón, puede saturar el caché y forzar recompilación en cada request.

**Mitigación recomendada (baja urgencia):**
```csharp
// En FolioConstants.cs o QuoteController.cs
private static readonly Regex FolioRegex = new(FolioConstants.FolioPattern, RegexOptions.Compiled);
```

---

## Detalle de Riesgos BAJO (D)

---

### R-009 — MIN-001: `layoutConfiguration` y `locations` apuntan al mismo `path: 'locations'`

Ambos steps en `ProgressBar.tsx` tienen `path: 'locations'`. Un clic en "Layout" navega a la misma ruta que "Ubicaciones". Impacto: confusión de navegación menor. Solución: verificar si existe `/locations/layout` como ruta separada y corregir.

---

### R-010 — MAY-003: Validación de folio duplicada en 11+ action methods

Bloque `if (!Regex.IsMatch(...)) return BadRequest(...)` repetido en cada endpoint del controller. Riesgo de que futuros cambios en el pattern se apliquen en algunos métodos y no en otros. Solución: extraer a `ActionFilter` o método privado.

---

### R-011 — `ProgressBar` desaparece silenciosamente (subriesgo de R-002)

Si `useQuoteStateQuery` falla y no hay manejo de `isError`, el componente `ProgressBar` no renderiza nada. El impacto visual es la desaparición del indicador de progreso en todas las páginas. Cubierto en la mitigación de R-002.

---

## Plan de Mitigación — Cronograma

| Riesgo | Acción | Responsable | Sprint |
|--------|--------|-------------|--------|
| R-001 | Activar orchestrator completo → implementar `GetQuoteStateUseCaseTests` (9 casos) | test-engineer-backend | Al activar orchestrator |
| R-002 | Fix MAY-001: manejar `isLoading`/`isError` en `WizardLayout.tsx` | frontend-developer | SPEC-008 (inmediato) |
| R-003 | Hacer `CommercialPremiumBeforeTax` nullable en SPEC-009 | backend-developer | SPEC-009 |
| R-004 | Agregar schema validation `zod` en `quoteStateApi.ts` | frontend-developer | SPEC-008 o SPEC-009 |
| R-005 | Evaluar staleTime 30s + invalidación manual | frontend-developer | Sprint siguiente |
| R-006 | Test exploratorio doble pestaña | qa-agent | Manual — sprint actual |
| R-007 | Test de integración formato `blockingAlerts` | test-engineer-backend | Al activar orchestrator |
| R-008 | Compilar regex con `RegexOptions.Compiled` | backend-developer | Backlog bajo |
| R-009 | Verificar paths en `ProgressBar.tsx` | frontend-developer | Backlog bajo |
| R-010 | Extraer validación folio a `ActionFilter` | backend-developer | Backlog bajo |
| R-011 | Cubierto por fix de R-002 | frontend-developer | SPEC-008 (inmediato) |

---

## Riesgo Accepted — Registro Formal

| ID | Riesgo | Nivel | Decisión | Justificación | Revisión |
|----|--------|-------|----------|---------------|---------|
| R-001 | 0% tests unitarios `GetQuoteStateUseCase` | **A** | **ACEPTADO** — diferido | orchestrator-lite prioriza velocidad de entrega sobre cobertura. Mitigado por E2E. | Al activar orchestrator completo |
| R-003 | `CommercialPremiumBeforeTax: 0m` | **A** | **ACEPTADO** — placeholder SPEC-009 | Explícitamente documentado en spec y en MAY-002 del reporte estático. | SPEC-009 |
