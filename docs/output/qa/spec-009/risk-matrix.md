# Matriz de Riesgos — SPEC-009: Motor de Cálculo de Primas

> **Feature:** `premium-calculation-engine`  
> **Spec:** SPEC-009 | **Estado:** IN_PROGRESS  
> **Generado:** 2026-03-30 | **Agente:** QA Agent  
> **Metodología:** Regla ASD (Alto=Obligatorio, Medio=Recomendado, Bajo=Opcional)

---

## Resumen Ejecutivo

| Nivel | Cantidad | Acción |
|---|---|---|
| **Alto (A)** | 7 | Testing OBLIGATORIO — bloquea el release |
| **Medio (S)** | 5 | Testing RECOMENDADO — documentar si se omite |
| **Bajo (D)** | 3 | Testing OPCIONAL — priorizar en backlog |
| **Total** | **15** | |

---

## Detalle de Riesgos

| ID | HU | Área | Descripción del Riesgo | Factores | Nivel | Testing |
|---|---|---|---|---|---|---|
| R-001 | HU-009-01 | Fórmulas financieras | `commercialPremiumBeforeTax` calculada incorrectamente: factor de gastos aplicado sobre prima equivocada o con parámetros mal leídos de core-ohs | Lógica financiera, integración externa, impacto en cotización final | **A** | Obligatorio |
| R-002 | HU-009-01 | Fórmulas financieras | `commercialPremium` (con IVA) no usa `commercialPremiumBeforeTax` como base — se aplica IVA directamente sobre `netPremium` | Lógica financiera, orden de operaciones | **A** | Obligatorio |
| R-003 | HU-009-04 | Versionado optimista | Conflicto de versión no detectado: dos procesos sobrescriben el mismo folio con datos distintos sin generar 409 | Integridad de datos, concurrencia, sin rollback posible | **A** | Obligatorio |
| R-004 | HU-009-04 | Persistencia atómica | `$set` parcial en MongoDB sobrescribe `insuredData`, `locations` o `coverageOptions` por uso incorrecto del operador | Integridad de datos, sin rollback, ADR-002 | **A** | Obligatorio |
| R-005 | HU-009-01 | Integración core-ohs | core-ohs no disponible no devuelve 503 — se persisten datos incorrectos con prima 0 o se lanza excepción no controlada | Integración externa, dependencia no controlada, integridad de datos | **A** | Obligatorio |
| R-006 | HU-009-02 | Precisión numérica | Redondeo de decimales en primas produce errores de centavos acumulados al sumar por cobertura y por ubicación | Lógica financiera, precisión `decimal` vs `double` | **A** | Obligatorio |
| R-007 | HU-009-03 | Clasificación de ubicaciones | Una ubicación `incomplete` es tratada como `calculable` (o viceversa) — introduce montos incorrectos o 422 falsos | Lógica de negocio compleja, RN-009-02 | **A** | Obligatorio |
| R-008 | HU-009-02 | Contratos API | `coveragePremiums` devuelto en orden distinto al esperado por el frontend — rompe la visualización del desglose | Código nuevo sin historial, integración FE↔BE | **S** | Recomendado |
| R-009 | HU-009-01 | Validación de entrada | `version: 0` o `version: -1` pasa la validación y causa comportamiento indefinido en el filtro MongoDB | Validación de límites, boundary values | **S** | Recomendado |
| R-010 | HU-009-03 | Caso extremo | Folio con 0 ubicaciones totales (sin `locations[]`) — el sistema lanza excepción no controlada en vez de 422 limpio | Edge case, código nuevo sin historial | **S** | Recomendado |
| R-011 | HU-009-01 | Idempotencia | Doble submit del botón "Calcular" genera dos cálculos simultáneos con la misma versión — el segundo provoca 409 inesperado visualmente | Alta frecuencia de uso, UX | **S** | Recomendado |
| R-012 | HU-009-02 | Contratos core-ohs | `fireKey` de una ubicación no existe en el catálogo de tarifas de core-ohs — el motor no maneja el `null` y lanza NullReferenceException | Integración, datos inconsistentes entre colecciones | **S** | Recomendado |
| R-013 | HU-009-04 | Metadatos | `metadata.lastWizardStep` no se actualiza a 4 post-cálculo — impacta la navegación del wizard en el frontend | Funcionalidad interna, impacto limitado | **D** | Opcional |
| R-014 | HU-009-01 | Cabeceras | `X-Correlation-Id` no se propaga a core-ohs — dificulta el rastreo en logs distribuidos | Feature interna, trazabilidad | **D** | Opcional |
| R-015 | HU-009-02 | Estilo respuesta | `locationName` devuelto con espacios extra o capitalización distinta a la almacenada | Ajuste estético, sin impacto financiero | **D** | Opcional |

---

## Plan de Mitigación — Riesgos ALTO

### R-001: Fórmula `commercialPremiumBeforeTax` incorrecta

- **Descripción:** El factor compuesto (gastos + comisión + derechos + sobreprimas) se aplica con los parámetros equivocados o no se lee correctamente desde `calculationParameters` de core-ohs.
- **Mitigación técnica:**
  - Test unitario de `PremiumCalculator.CalculateCommercialPremium()` con valores fijos del fixture:
    - Input: `netPremium=125430.50`, params=`{0.05, 0.10, 0.03, 0.02, 0.16}`
    - Expected: `beforeTax=150516.60` exacto (× 1.20), `withTax=174599.26` exacto (× 1.16)
  - Test de integración que valida que los parámetros leídos de `GET /v1/tariffs/calculation-parameters` son los mismos que se pasan a `PremiumCalculator`
- **Tests obligatorios:** Unitario (PremiumCalculator), Integración (flujo completo)
- **Bloqueante para release:** ✅ Sí

---

### R-002: IVA aplicado sobre base incorrecta

- **Descripción:** `commercialPremium = netPremium × (1+iva)` en vez de `commercialPremiumBeforeTax × (1+iva)`.
- **Mitigación técnica:**
  - Test unitario explícito que verifica que `commercialPremium / commercialPremiumBeforeTax ≈ 1.16`
  - Test que confirma `commercialPremium ≠ netPremium × 1.16`
- **Tests obligatorios:** Unitario (PremiumCalculator — orden de operaciones)
- **Bloqueante para release:** ✅ Sí

---

### R-003: Versionado optimista no detecta conflicto

- **Descripción:** El filtro MongoDB `{ folioNumber, version: N }` con `UpdateOne` no evalúa `ModifiedCount == 0` correctamente, o la condición de lanzar `VersionConflictException` no se ejecuta.
- **Mitigación técnica:**
  - Test de integración/repositorio: ejecutar `UpdateFinancialResultAsync` con versión incorrecta → verificar que `ModifiedCount == 0` y que se lanza `VersionConflictException`
  - Test de API: POST .../calculate con `version: 3` cuando la versión real es 5 → esperar HTTP 409
  - Test de concurrencia: dos requests simultáneos con la misma versión — solo uno debe ser HTTP 200, el otro HTTP 409
- **Tests obligatorios:** Unitario (repositorio), API (409), concurrencia (stress test básico)
- **Bloqueante para release:** ✅ Sí

---

### R-004: Persistencia sobrescribe secciones no objetivo

- **Descripción:** El `$set` en MongoDB incluye accidentalmente campos de `insuredData`, `locations` o `coverageOptions`, destruyendo datos del usuario.
- **Mitigación técnica:**
  - Test de integración: antes del cálculo → snapshot de `insuredData`/`locations`/`coverageOptions`; después del cálculo → verificar que los tres campos son byte-identical al snapshot
  - Test unitario de la implementación `UpdateFinancialResultAsync`: validar que el `UpdateDefinition` solo contiene las claves del resultado financiero
- **Tests obligatorios:** Integración (snapshot pre/post), Unitario (definición del update)
- **Bloqueante para release:** ✅ Sí

---

### R-005: core-ohs caído — datos incorrectos persisten

- **Descripción:** Si core-ohs devuelve timeout o error 5xx y el middleware no maneja correctamente la excepción, el motor puede calcular con tarifas `null` o `0` y persistir una prima incorrecta.
- **Mitigación técnica:**
  - Test con mock de `ICoreOhsClient` que lanza `HttpRequestException` — verificar que el use case lanza `CoreOhsUnavailableException` y que `UpdateFinancialResultAsync` nunca es llamado
  - Test de API con core-ohs simulado en timeout → esperar HTTP 503, verificar que el folio no cambió de versión
- **Tests obligatorios:** Unitario (use case con cliente mockeado), API (503 con mock)
- **Bloqueante para release:** ✅ Sí

---

### R-006: Errores de redondeo en decimales

- **Descripción:** Uso de `double` en lugar de `decimal` en algún cálculo intermedio produce pérdidas de precisión que se acumulan al sumar coberturas y ubicaciones.
- **Mitigación técnica:**
  - Revisar que `PremiumCalculator` usa `decimal` en todas las operaciones (no `float` ni `double`)
  - Test unitario con 5 coberturas: verificar que la suma es exacta al centavo (ej. 6250.00 + 3750.00 + 17500.00 + 500.00 + 4000.00 = 32000.00)
  - Test con prima de 3 decimales: confirmar que se redondea a `Math.Round(x, 2, MidpointRounding.AwayFromZero)`
- **Tests obligatorios:** Unitario (PremiumCalculator — precisión decimal)
- **Bloqueante para release:** ✅ Sí

---

### R-007: Clasificación incorrecta de ubicaciones incomplete/calculable

- **Descripción:** La lógica de validación `validationStatus` no detecta correctamente cuándo una ubicación es `incomplete` (CP inválido, sin `fireKey`, sin garantías tarifables), causando que se intente calcular con datos nulos.
- **Mitigación técnica:**
  - Tests paramétricos de `LocationValidationService` (o equivalente) para cada combinación de campos faltantes:
    - Sin CP → `incomplete`
    - Con CP pero sin `fireKey` → `incomplete`
    - Con CP y `fireKey` pero sin garantías → `incomplete`
    - Con todos los requeridos → `calculable`
  - Test de integración: verificar que una ubicación con `fireKey = null` aparece en response con `validationStatus: "incomplete"` y `netPremium: 0`
- **Tests obligatorios:** Unitario (clasificador por campo), Integración (response validationStatus)
- **Bloqueante para release:** ✅ Sí

---

## Cobertura de Riesgos por Tipo de Test

| Tipo de Test | Riesgos cubiertos | Estado |
|---|---|---|
| Unitario — PremiumCalculator | R-001, R-002, R-006 | Obligatorio |
| Unitario — Repositorio/UpdateFinancialResult | R-003, R-004 | Obligatorio |
| Unitario — Validador de ubicaciones | R-007, R-009, R-010 | Obligatorio + Recomendado |
| Integración — Use Case completo con mocks | R-005, R-007, R-012 | Obligatorio + Recomendado |
| API (controller) — Contratos HTTP | R-001, R-003, R-005, R-008 | Obligatorio + Recomendado |
| E2E — Flujo completo en ambiente | R-003, R-004, R-011 | Recomendado |
| Performance / concurrencia | R-003, R-011 | Recomendado |
