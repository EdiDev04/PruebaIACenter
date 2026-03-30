# Matriz de Riesgos — SPEC-005: Configuración del Layout de Ubicaciones

> **Feature:** `location-layout-configuration`
> **Generado:** 2026-03-29
> **Derivado de:** `.github/specs/location-layout-configuration.spec.md`
> **Clasificación:** Regla ASD — Alto (A) = Obligatorio · Medio (S) = Recomendado · Bajo (D) = Opcional

---

## Resumen ejecutivo

| Nivel | Cantidad | Acción requerida |
|-------|----------|-----------------|
| **Alto (A)** | 7 | Testing OBLIGATORIO — bloquea release |
| **Medio (S)** | 5 | Testing RECOMENDADO — documentar si se omite |
| **Bajo (D)** | 3 | Testing OPCIONAL — priorizar en backlog |
| **Total** | **15** | |

---

## Detalle de riesgos

| ID | HU / Regla | Descripción del riesgo | Factores ASD | Nivel | Cobertura requerida |
|----|-----------|------------------------|-------------|-------|---------------------|
| R-005-01 | RN-005-02 | Versionado optimista no detecta conflicto → datos sobreescritos silenciosamente | Operación destructiva; sin rollback si se pierde version | **Alto** | Test unitario + integration: PUT con `ModifiedCount == 0` lanza `VersionConflictException` → HTTP 409 |
| R-005-02 | RN-005-01 | PUT layout modifica otras secciones del folio (`insuredData`, `locations`) por `$set` mal construido | Operación destructiva irrecuperable en datos de negocio | **Alto** | Test de integración MongoDB: verificar que solo `layoutConfiguration`, `version`, `metadata.updatedAt`, `metadata.lastWizardStep` cambian |
| R-005-03 | RN-005-03 | Folio sin layout configurado no retorna defaults → error o campo null en el frontend | Alta frecuencia de uso (todo folio nuevo carece de layout); regresión silenciosa | **Alto** | Test unitario `GetLayoutUseCase`: folio sin `layoutConfiguration` retorna `displayMode:"grid"` + 5 columnas exactas |
| R-005-04 | RN-005-05 | Frontend permite deseleccionar la última columna → PUT con `visibleColumns:[]` → 400 inesperado | Impacto en UX de alta frecuencia; debe bloquearse EN el cliente antes de llegar al servidor | **Alto** | Test unitario FE: `LayoutConfigPanel` previene desmarcado de último checkbox; Test BE: PUT con array vacío → 400 |
| R-005-05 | RN-005-06 | `metadata.lastWizardStep` no se actualiza a 2 en el `$set` → wizard queda en paso incorrecto | Afecta flujo de progreso del wizard en todo folio que use layout | **Alto** | Test de integración: verificar campo MongoDB `metadata.lastWizardStep === 2` tras PUT exitoso |
| R-005-06 | DRIFT-005 | Drift de contrato FE↔BE: campo `version` ausente o nombre distinto en el request/response | Integración entre dos equipos; cambio en uno rompe el otro silenciosamente | **Alto** | Test de integración: request FE vs. contrato §3.5b; response BE vs. §3.4 |
| R-005-07 | RN-005-04 | `displayMode` acepta valores fuera del enum (`"tabla"`, `"Grid"`, `""`) por falta de validación | Datos inválidos persistidos en MongoDB; rotura de UI al leer | **Alto** | Test unitario validator: todos los valores inválidos retornan 400 con `"field":"displayMode"` |
| R-005-08 | HU-005-01 | Response no incluye el envelope `{ "data": {...} }` → FE no puede deserializar | Lógica de negocio compleja de mapeo entre capas | **Medio** | Test de contrato: toda respuesta 2xx contiene wrapper `data`; error responses no lo contienen |
| R-005-09 | RN-005-08 | Mensajes de error en inglés en vez de español (ADR-008 violation) | Código nuevo sin historial; fácil de olvidar en nuevas excepciones | **Medio** | Test de snapshot: messages en todos los 400/404/409/500 están en español |
| R-005-10 | RN-005-09 | `sortBy`, `sortDirection`, `pageSize` son enviados al BE por el FE y persisten en MongoDB | Código FE nuevo sin historial; estado UI no debe cruzar la frontera API | **Medio** | Test FE: `useSaveLayout` construye el body sin campos de UI transitorio; Test BE: validator no acepta esos campos |
| R-005-11 | SUP-005-03 | Se agrega una columna a `Location` (SPEC-006) pero no se actualiza la lista de columnas válidas → validación incorrecta | Dependencia entre specs (SPEC-005 → SPEC-006); sin documentación explícita de la sincronización | **Medio** | Review cruzado al merge de SPEC-006; test de integración que valide lista de columnas contra schema `Location` |
| R-005-12 | HU-005-01 | Invalidación de caché TanStack Query no ocurre tras PUT → UI muestra versión desactualizada | Código FE nuevo; la query key `['layout', folio]` debe invalidarse exactamente al mutar | **Medio** | Test FE `useSaveLayout`: tras mutación exitosa, `queryClient.invalidateQueries(['layout', folio])` es llamado |
| R-005-13 | HU-005-02 | Regresión FSD: importaciones cruzadas entre layers (entity importa desde feature, widget desde entidad interna) | Código nuevo; violaciones FSD pasadas ya se corrigieron (riesgo de regresión) | **Bajo** | Lint FSD + revisión de código en PR (no requiere test específico) |
| R-005-14 | HU-005-01 | X-Correlation-Id no se propaga en las respuestas de error | Baja frecuencia de impacto; más de observabilidad que de negocio | **Bajo** | Test manual o revisar middleware una sola vez |
| R-005-15 | SUP-005-02 | El layout se convierte en paso separado del wizard en el futuro → cambios de routing no contemplados | Supuesto aprobado por el usuario; muy baja probabilidad de cambio en el corto plazo | **Bajo** | No requiere test — documentar el supuesto como ADR |

---

## Plan de mitigación — Riesgos ALTO (obligatorio antes del release)

### R-005-01: Versionado optimista no detecta conflicto

- **Origen:** `UpdateLayoutAsync` usa filtro `{ folioNumber, version: N }`. Si el documento fue modificado entre el GET y el PUT, `ModifiedCount == 0`.
- **Mitigación técnica:**
  - Verificar en `UpdateLayoutUseCase` que `ModifiedCount > 0`; si no → lanzar `VersionConflictException`
  - Mapear `VersionConflictException` → HTTP 409 en el middleware global
- **Tests obligatorios:**
  - `UpdateLayoutUseCaseTests` — mock retorna `ModifiedCount == 0` → throws `VersionConflictException`
  - Integration test: PUT con version desactualizada → 409 con body exacto del contrato §3.4
  - Test de concurrencia: dos PUTs simultáneos → segundo recibe 409
- **Bloqueante para release:** ✅ Sí

### R-005-02: Actualización parcial — PUT no afecta otras secciones del folio

- **Origen:** El `$set` de MongoDB debe ser **selectivo**: solo `layoutConfiguration`, `version`, `metadata.updatedAt`, `metadata.lastWizardStep`. Un `$set` con el documento completo sobrescribiría ubicaciones y datos del asegurado.
- **Mitigación técnica:**
  - Repositorio `UpdateLayoutAsync` construye el `UpdateDefinition` con `Set(p => p.LayoutConfiguration, ...)`, `Set(p => p.Version, ...)`, etc. — nunca `ReplaceOne`
  - Code review obligatorio del método de repositorio
- **Tests obligatorios:**
  - Integration test BE con MongoDB real (o `mongomock`): verificar campos intactos tras PUT layout
  - Test unitario: el `UpdateDefinition` compilado solo contiene los campos esperados
- **Bloqueante para release:** ✅ Sí

### R-005-03: Defaults cuando folio no tiene layout configurado

- **Origen:** `LayoutConfiguration` es un value object con defaults en C#. Si el documento en MongoDB tiene el campo `null` o ausente, el deserializer debe aplicar los defaults del value object.
- **Mitigación técnica:**
  - Confirmar que `MongoDB.Driver` respeta `= new LayoutConfiguration()` al deserializar un campo nulo/ausente
  - Si no, agregar lógica explícita en `GetLayoutUseCase`: `folio.LayoutConfiguration ?? new LayoutConfiguration()`
- **Tests obligatorios:**
  - `GetLayoutUseCaseTests` — folio sin campo `layoutConfiguration` → retorna `displayMode:"grid"` + 5 columnas exactas
  - Integration test: GET sobre folio recién creado (SPEC-003) → defaults en response
- **Bloqueante para release:** ✅ Sí

### R-005-04: Frontend previene visibleColumns vacío

- **Origen:** Si el usuario puede llegar a tener 0 columnas seleccionadas, el PUT fallará con 400. La experiencia debe bloquearse antes de hacer la llamada.
- **Mitigación técnica:**
  - `LayoutConfigPanel`: deshabilitar checkbox cuando solo queda 1 columna marcada
  - Deshabilitar botón "Guardar" si `visibleColumns.length === 0`
  - Validación Zod en `layoutSchema.ts`: `z.array(z.string()).min(1)`
- **Tests obligatorios:**
  - `LayoutConfigPanel.test.tsx`: con 1 columna visible, el checkbox está `disabled`
  - `layoutSchema.test.ts`: `visibleColumns:[]` → error de validación Zod
  - Test BE: PUT con `visibleColumns:[]` → 400 con `field:"visibleColumns"` y mensaje en español
- **Bloqueante para release:** ✅ Sí

### R-005-05: metadata.lastWizardStep actualizado a 2

- **Origen:** `lastWizardStep: 2` debe ir en el `$set` del `UpdateLayoutAsync`. Si se omite, el wizard no puede determinar hasta qué paso llegó el usuario.
- **Mitigación técnica:**
  - Añadir explícitamente `Set(p => p.Metadata.LastWizardStep, 2)` en el `UpdateDefinition` de `UpdateLayoutAsync`
  - No delegar en aplicación; debe ir en el repositorio como parte de la operación atómica
- **Tests obligatorios:**
  - Integration test MongoDB: tras PUT exitoso, leer el documento y verificar `metadata.lastWizardStep === 2`
  - `UpdateLayoutUseCaseTests`: verificar que el repositorio es llamado y el mock captura el campo
- **Bloqueante para release:** ✅ Sí

### R-005-06: Drift de contrato FE↔BE

- **Origen:** §3.4 (contrato BE) y §3.5b (consumo FE) deben estar sincronizados. Campos renombrados, tipos distintos o structures anidadas diferentes rompen la integración silenciosamente.
- **Mitigación técnica:**
  - Checklist de integration agent: validar campo a campo §3.4 vs. §3.5b antes de merge
  - Types FE (`LayoutConfigurationDto` en `entities/layout/model/types.ts`) deben reflejar exactamente el response BE
- **Tests obligatorios:**
  - Test de integración E2E: llamar al BE real, deserializar con el type FE → sin errores de tipo
  - Snapshot test del response BE contra el schema esperado por el FE
- **Bloqueante para release:** ✅ Sí

### R-005-07: Validación de displayMode — enum estricto

- **Origen:** El validador FluentValidation debe rechazar cualquier valor que no sea exactamente `"grid"` o `"list"` (case-sensitive).
- **Mitigación técnica:**
  - `UpdateLayoutRequestValidator`: `Must(v => v == "grid" || v == "list")` — no usar `ToLower()` ni comparación case-insensitive
  - Mensaje: `"Modo de visualización inválido. Valores permitidos: grid, list"` (en español, ADR-008)
- **Tests obligatorios:**
  - `UpdateLayoutRequestValidatorTests` con datos de prueba: `"grid"` ✅, `"list"` ✅, `"tabla"` ❌, `"Grid"` ❌, `"LIST"` ❌, `""` ❌, `null` ❌
  - Integration test: PUT con cada valor inválido → 400 con `field:"displayMode"`
- **Bloqueante para release:** ✅ Sí

---

## Dependencias y riesgos heredados

| Spec dependencia | Riesgo heredado | Impacto en SPEC-005 |
|-----------------|----------------|---------------------|
| SPEC-002 (quote-data-model) | `UpdateLayoutAsync` no implementado o con firma diferente | Bloquea completo el backend de SPEC-005 |
| SPEC-003 (folio-creation) | Folio no existe → todos los tests requieren folio previo | Todos los escenarios de GET/PUT fallan |
| SPEC-006 (location-management) | Agregan columnas a `Location` sin actualizar lista de columnas válidas | R-005-11 |
