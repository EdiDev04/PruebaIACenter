# Matriz de Riesgos — SPEC-007: Configuración de Opciones de Cobertura

> **Fuente:** `coverage-options-configuration.spec.md` v1.0 (APPROVED)
> **Generado:** 2026-03-29
> **Regla aplicada:** ASD — Alto = Obligatorio · Medio = Recomendado · Bajo = Opcional
> **Prerequisito:** `code-quality-report.md` → `QUALITY_GATE: PASSED` ✅
> **⚠️ Ciclo ASDD Lite:** Tests unitarios diferidos — riesgo R-01 aplica inmediatamente

---

## Resumen

| Métrica | Valor |
|---|---|
| Total de riesgos identificados | 12 |
| **ALTO (A) — Obligatorio** | **6** |
| **MEDIO (S) — Recomendado** | **4** |
| **Bajo (D) — Opcional** | **2** |
| Bloqueantes para release | 6 |
| Nota de ciclo | Flujo ASDD Lite activo — Fase 3 (Unit Tests) diferida |

---

## Tabla de riesgos

| ID | HU | Área | Descripción del Riesgo | Factores ASD | Nivel | Testing | Bloqueante |
|---|---|---|---|---|---|---|---|
| R-01 | — | Cobertura de tests | **Cobertura de tests unitarios: 0% (diferido a siguiente iteración con `orchestrator` completo)** | Código nuevo sin historial, cero validación aislada de lógica | **A** | Obligatorio | ✅ Sí |
| R-02 | HU-007-01 | Versionado optimista | Conflicto de concurrencia no detectado — dos usuarios editan simultáneamente y la versión no coincide | Operación con riesgo de corrupción de datos, integridad del agregado | **A** | Obligatorio | ✅ Sí |
| R-03 | HU-007-04 | Lógica de negocio | Warning de deshabilitación de garantías omitido en frontend — usuario deshabilita `building_fire` sin ser notificado de las ubicaciones afectadas | Regla de negocio crítica, bug no detectable a simple vista, impacto en cálculo subsiguiente | **A** | Obligatorio | ✅ Sí |
| R-04 | HU-007-03 | Integración externa | Integración con core-ohs — catálogo de garantías no disponible (503/timeout); frontend queda sin checkboxes renderizables | Dependencia no controlada (tercero), sin fallback de datos locales en UI | **A** | Obligatorio | ✅ Sí |
| R-05 | HU-007-01 | Conversión decimal/porcentaje | Bug en conversión bidireccional: UI captura en % (ej. `5.0`) y API persiste en decimal (`0.05`); error de dirección o factor provoca persistir `5.0` en lugar de `0.05` | Alta probabilidad de error silencioso, impacto financiero en cálculo de prima, sin alarma visible al usuario | **A** | Obligatorio | ✅ Sí |
| R-06 | HU-007-01 | Aislamiento de la sección | PUT coverage-options sobreescribe datos de ubicaciones u otros campos del folio — viola RN-007-01 (solo modifica sección `coverageOptions`) | Operación destructiva parcialmente irrecuperable, pérdida de datos de ubicaciones | **A** | Obligatorio | ✅ Sí |
| R-07 | HU-007-01 | Defaults automáticos | Folio sin configuración no retorna las 14 garantías por defecto — usuario ve formulario vacío o incompleto al entrar por primera vez | Lógica nueva sin historial, alta frecuencia de uso (todos los folios nuevos) | **S** | Recomendado | ❌ No |
| R-08 | HU-007-01 | Validaciones de entrada | Validación incompleta: `enabledGuarantees` acepta keys fuera del catálogo (`GuaranteeKeys.All`) sin retornar 400 | Lógica de negocio compleja, integridad referencial sin FK en MongoDB | **S** | Recomendado | ❌ No |
| R-09 | HU-007-01 | `lastWizardStep` | `metadata.lastWizardStep` no se actualiza a 3 tras PUT exitoso — el wizard no avanza su estado persistido | Componente con dependencias (wizard progress bar), cambio de comportamiento silencioso | **S** | Recomendado | ❌ No |
| R-10 | HU-007-03 | Integridad del catálogo | Catálogo retorna menos de 14 garantías o sin algún campo (`key`, `name`, `description`, `category`, `requiresInsuredAmount`) — UI renderiza incompleta | Código nuevo, contrato con sistema externo | **S** | Recomendado | ❌ No |
| R-11 | HU-007-01 | Response envelope | Response sin wrapper `{ "data": {...} }` — frontend no parsea correctamente el cuerpo | ADR establecido, bajo impacto aislado | **D** | Opcional | ❌ No |
| R-12 | HU-007-01 | Mensajes de error | Mensajes de error en inglés — viola ADR-008 (mensajes en español) | Requisito de UI, impacto cosmético en UX | **D** | Opcional | ❌ No |

---

## Plan de mitigación — Riesgos ALTO (A)

### R-01 — Cobertura de tests unitarios: 0%

> **⚠️ RIESGO ESTRUCTURAL DEL CICLO ASDD LITE**

- **Contexto:** El flujo ASDD Lite activo en este ciclo difirió la Fase 3 (Test Engineer Backend + Frontend). Ningún test unitario fue generado para `GetCoverageOptionsUseCase`, `UpdateCoverageOptionsUseCase`, el value object `CoverageOptions`, el validator `UpdateCoverageOptionsRequestValidator`, ni para los hooks y features del frontend.
- **Impacto:** Cualquier regresión introducida en lógica de defaults, validaciones o mapeo decimal/porcentaje no tiene covertura de detección automática.
- **Mitigación:**
  - Ejecutar el orchestrator completo (`/asdd-orchestrate`) en la siguiente iteración para activar la Fase 3.
  - Priorizar los tests unitarios de `UpdateCoverageOptionsUseCase` (versionado optimista) y `CoverageOptions` (defaults) antes de mergear a rama principal.
  - Los tests unitarios que deben generarse primero: ver sección de automatización `coverage-options-configuration-automation.md`.
- **Tests obligatorios al activar Fase 3:**
  - `UpdateCoverageOptionsUseCaseTests` — escenarios: éxito, versión incorrecta (409), key inválida (400), deducible fuera de rango (400)
  - `CoverageOptionsValueObjectTests` — escenario: constructor con defaults retorna 14 garantías, `DeductiblePercentage = 0`, `CoinsurancePercentage = 0`
  - `UpdateCoverageOptionsRequestValidatorTests` — matriz de campos inválidos
  - `useSaveCoverageOptions.test.ts` — mutación exitosa, error 409, error 400
- **Bloqueante para release:** ✅ Sí — no liberar a producción sin al menos tests de smoke unitarios en use cases críticos

---

### R-02 — Versionado optimista (conflicto de concurrencia)

- **Contexto:** `PUT /v1/quotes/{folio}/coverage-options` implementa versionado optimista usando el campo `version` del documento MongoDB. Si dos usuarios cargan el formulario simultáneamente y uno guarda primero, el segundo debe recibir 409 y no sobreescribir.
- **Riesgo concreto:** Si la comparación de versiones no es atómica (ej. read-then-write sin findOneAndUpdate con filtro de versión), se pueden perder datos del primer usuario.
- **Mitigación:**
  - Verificar que `UpdateCoverageOptionsAsync` en MongoDB usa `{ folioNumber: folio, version: versionEnviada }` como filtro del `FindOneAndUpdate`.
  - Test de integración: ejecutar dos PUT simultáneos con la misma versión — solo uno debe retornar 200, el otro debe retornar 409.
  - Escenario API-07 del Gherkin cubre este caso.
- **Bloqueante para release:** ✅ Sí

---

### R-03 — Warning de deshabilitación de garantías no mostrado

- **Contexto:** Cuando el usuario desmarca una garantía que ya está seleccionada en una o más ubicaciones del folio, el frontend debe mostrar un dialog de confirmación antes de permitir el guardado (RN-007-06). Si el warning no aparece, el usuario puede deshabilitar garantías sin ser consciente del impacto, generando inconsistencias silenciosas detectadas recién en el paso de cálculo.
- **Riesgo concreto:** La lógica consulta `['locations', folio]` desde el cache de TanStack Query. Si el cache está vacío o la query key no coincide, el count de ubicaciones afectadas será 0 y el warning no se mostrará aunque existan ubicaciones.
- **Mitigación:**
  - Test E2E-02: verificar que el dialog aparece con el count correcto cuando existen ubicaciones con esa garantía.
  - Validar que la query key de ubicaciones en el cache es idéntica a la usada en SPEC-006.
  - Test unitario del hook `useDisableGuaranteeWarning` (o similar) al activar Fase 3.
- **Bloqueante para release:** ✅ Sí

---

### R-04 — Integración con core-ohs no disponible

- **Contexto:** `GET /v1/catalogs/guarantees` es un proxy que depende de `ICoreOhsClient.GetGuaranteesAsync()`. Si core-ohs no responde, el backend debe retornar 503 y el frontend debe mostrar un banner de error con opción de reintentar — sin quedar en estado de carga infinita.
- **Riesgo concreto:** Si el timeout de `ICoreOhsClient` no está configurado, la petición puede bloquear el thread indefinidamente. Si el frontend no maneja el 503, queda en spinner infinito sin checkboxes.
- **Mitigación:**
  - Verificar que `HttpClient` tiene `Timeout` configurado en el cliente de core-ohs.
  - Escenario API-14 y E2E-06 del Gherkin cubren el caso de no disponibilidad.
  - Test de contrato: el response 503 debe incluir `type: "coreOhsUnavailable"` para que el frontend lo clasifique correctamente.
- **Bloqueante para release:** ✅ Sí

---

### R-05 — Conversión decimal/porcentaje bidireccional

- **Contexto:** El frontend muestra deducible y coaseguro en porcentaje (0–100%) usando `PercentageInput`. Al enviar a la API, el schema Zod `coverageOptionsApiSchema` divide por 100 (`data.deductiblePercentage / 100`). Al recibir de la API, el frontend multiplica por 100 para mostrar. Un error en cualquier dirección de esta conversión (ej. enviar `5.0` en lugar de `0.05`) no es detectado por el usuario y se persiste silenciosamente.
- **Riesgo concreto:**
  - Bug A: `deductiblePercentage / 100` no se aplica → se persiste `5.0` en lugar de `0.05` (factor 100x)
  - Bug B: Al leer de la API, `0.05 * 100` no se aplica → el input muestra `0.05` en lugar de `5.0`
- **Mitigación:**
  - Test E2E-04: verificar que el PUT enviado contiene `deductiblePercentage: 0.08` cuando el usuario ingresó `8.0` en la UI.
  - Test unitario de `coverageOptionsApiSchema.transform()` al activar Fase 3.
  - Inspeccionar el payload de red en los tests E2E (interceptar la llamada PUT con Playwright `route.fulfill`).
- **Bloqueante para release:** ✅ Sí

---

### R-06 — Aislamiento de la sección coverage-options en el PUT

- **Contexto:** `PUT /v1/quotes/{folio}/coverage-options` debe modificar **únicamente** los campos `coverageOptions`, `version`, `metadata.updatedAt` y `metadata.lastWizardStep`. No debe sobreescribir datos de ubicaciones, datos generales ni ningún otro campo del documento MongoDB.
- **Riesgo concreto:** Si el repositorio usa `ReplaceOne` en lugar de `UpdateOne` con `$set` parcial, todo el documento se reemplaza con los datos del request, eliminando ubicaciones ya registradas.
- **Mitigación:**
  - Verificar que `UpdateCoverageOptionsAsync` usa `UpdateOne` con operador `$set` parcial (no `ReplaceOne`).
  - Test de integración: agregar ubicaciones al folio → ejecutar PUT coverage-options → verificar que las ubicaciones persisten intactas.
  - Escenario API-06 del Gherkin verifica que `coverageOptions` se actualiza sin efecto secundario en otras secciones.
- **Bloqueante para release:** ✅ Sí

---

## Resumen de bloqueantes para release

| ID | Descripción breve | Estado sugerido |
|---|---|---|
| R-01 | 0% cobertura tests unitarios (ASDD Lite — diferido) | 🔴 Pendiente — activar Fase 3 |
| R-02 | Versionado optimista concurrente | 🟡 Cubrir con API-07 + test integración |
| R-03 | Warning garantías deshabilitadas | 🟡 Cubrir con E2E-02 |
| R-04 | core-ohs no disponible → 503 | 🟡 Cubrir con API-14 + E2E-06 |
| R-05 | Conversión decimal/porcentaje bidireccional | 🟡 Cubrir con E2E-04 (inspección de payload) |
| R-06 | Aislamiento de sección en PUT (no ReplaceOne) | 🟡 Cubrir con test integración + API-06 |

> Los riesgos R-02 a R-06 quedan cubiertos por los escenarios Gherkin de `coverage-options-configuration-gherkin.md`. El R-01 requiere activar el orchestrator completo para la Fase 3.
