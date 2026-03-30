# Matriz de Riesgos — SPEC-006: Gestión de Ubicaciones de Riesgo

> **Fuente:** SPEC-006 v1.0 + location-management.design.md (APPROVED)
> **Generado:** 2026-03-29
> **Metodología:** Regla ASD — Alto (A) = Obligatorio · Medio (S) = Recomendado · Bajo (D) = Opcional
> **Prerequisito:** code-quality-report.md → Gate PASSED (re-auditoría 2026-03-29)

---

## Resumen ejecutivo

| Nivel | Total | Releases bloqueados | Acción |
|---|---|---|---|
| **Alto (A)** | 8 | Sí | Testing OBLIGATORIO antes de merge |
| **Medio (S)** | 7 | No | Testing RECOMENDADO — documentar si se omite |
| **Bajo (D)** | 4 | No | Priorizar en backlog |
| **Total** | **19** | — | — |

**Feature más riesgosa de la oleada 3:** SPEC-006 es la entrada de datos al motor de cálculo (SPEC-009). Un dato de ubicación incorrecto propagado al cálculo de tarifa genera cotizaciones erróneas directamente. El riesgo de mayor impacto es R-001 (lógica de calculabilidad).

---

## Vista consolidada SPEC-006

| ID | Riesgo | Clasificación ASD | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|---|
| R-001 | `validationStatus` evaluado incorrectamente — ubic. calculable marcada como incompleta (o viceversa). Impacta entrada al motor SPEC-009. | **Alto (A)** | Media | Alto | Tests unitarios `[Theory]` en `LocationCalculabilityEvaluator` con las 8 combinaciones de CP × fireKey × guarantees |
| R-002 | PATCH con `version` desactualizada silencioso → datos de otra sesión sobreescritos sin retornar 409. Pérdida irrecuperable. | **Alto (A)** | Baja | Alto | Test de concurrencia: dos PATCH simultáneos al mismo índice con la misma version; el segundo debe retornar 409 |
| R-003 | CP resuelto incorrectamente (o no resuelto) vía core-ohs → `catZone` incorrecta → error en tarifa de zona catastrófica en SPEC-009. | **Alto (A)** | Media | Alto | Integration test: mock core-ohs con CP `06600` → verificar `catZone: "A"` en respuesta. CP `99999` → 404 → `incomplete` |
| R-004 | PUT no reemplaza el array atómicamente → ubicaciones duplicadas o inconsistentes en MongoDB. | **Alto (A)** | Baja | Alto | Test de repositorio: PUT con N ubicaciones → `$set: { locations: [...] }` en una sola operación; verificar count exacto |
| R-005 | Garantía `glass` con `insuredAmount: 0` marcada como incompleta (debería ser válida para `requiresInsuredAmount: false`). | **Alto (A)** | Media | Alto | Test unitario explícito: `glass:0 + building_fire:5000000 + CP válido + fireKey` → `"calculable"` |
| R-006 | `constructionYear` fuera de rango (1799 o 2099) aceptado silenciosamente → dato inválido persistido. | **Alto (A)** | Baja | Alto | `[Theory]` con valores límite: 1800 ✓, 2026 ✓, 1799 ✗, 2027 ✗ |
| R-007 | PATCH modifica ubicación diferente a la indicada por `index` → data corruption silenciosa. | **Alto (A)** | Baja | Alto | Integration test: folio con 3 ubicaciones; PATCH index=2; verificar que solo `locations[1]` cambia |
| R-008 | Ubicación incompleta bloquea el guardado del folio completo → regresión de RN-006-02. Bloquea flujo de usuario. | **Alto (A)** | Media | Alto | E2E: PUT con 1 ubicación sin CP → HTTP 200 → `validationStatus: "incomplete"` — no 400/422 |
| R-009 | core-ohs 503 no manejado → excepción no controlada → 500 en API → formulario pierde datos. | **Medio (S)** | Media | Medio | Test de integración: mock core-ohs retorna 503 → API retorna 503 con mensaje en español, sin 500 |
| R-010 | Giro con `fireKey` vacío aceptado silenciosamente → ubicación calculable sin fireKey. Motor SPEC-009 no puede tarificar. | **Medio (S)** | Media | Medio | Test validador: `businessLine.fireKey = ""` → alerta incluida en `blockingAlerts`; `validationStatus: "incomplete"` |
| R-011 | PUT con 50 ubicaciones → timeout o error 500 (volumen de BSON o tiempo de procesamiento). | **Medio (S)** | Baja | Medio | Performance test k6: PUT con payload de 50 ubicaciones (~100KB) → p95 < 1s |
| R-012 | `GET /locations/summary` retorna `totalCalculable` inconsistente con el count real del array. | **Medio (S)** | Baja | Medio | Test de lógica: folio 3 ubicaciones (2 calculables) → `totalCalculable: 2`, `totalIncomplete: 1` |
| R-013 | Edición vía PATCH no recarga el estado de la grilla → badge `validationStatus` stale (muestra estado anterior). | **Medio (S)** | Media | Medio | E2E Playwright: editar ubicación incompleta → completar datos → badge cambia a "Calculable" sin reload manual |
| R-014 | Endpoint PUT `/locations` sin autenticación → acceso no autorizado a folio ajeno. | **Medio (S)** | Baja | Alto | Test de seguridad: PUT sin header `Authorization` → 401; con credenciales incorrectas → 401 |
| R-015 | Badge ámbar de ubicación incompleta no visible al usuario → no corrige antes de calcular tarifa. | **Medio (S)** | Media | Medio | E2E screenshot: badge #d97706 presente en grilla para `validationStatus: "incomplete"` — nunca color rojo (#dc2626) |
| R-016 | Garantía con `insuredAmount < 0` aceptada → suma negativa genera tarifa negativa o error en SPEC-009. | **Medio (S)** | Baja | Medio | Test validador: `insuredAmount: -1` → HTTP 400 + `"La suma asegurada debe ser mayor o igual a 0"` |
| R-017 | Formulario step 1 → navegar atrás desde step 2 → datos del step 1 se pierden. | **Bajo (D)** | Media | Bajo | E2E: completar step 1 → avanzar a step 2 → retroceder → campos del step 1 conservan sus valores |
| R-018 | Badge estado solo diferenciado por color → no accesible para usuarios con daltonismo (WCAG AA). | **Bajo (D)** | Baja | Bajo | Auditoría WCAG: badge "Calculable" y "Datos pendientes" incluyen ícono o texto diferenciador además del color |
| R-019 | Grilla de ubicaciones no se adapta a resoluciones < 1024px → columnas truncadas. | **Bajo (D)** | Baja | Bajo | Visual regression Playwright en viewport 768×1024: columnas clave (Nombre, Estado) visibles sin overflow |

---

## Detalle completo

| ID | HU | Área | Descripción del Riesgo | Factores ASD | Nivel | Bloqueante |
|---|---|---|---|---|---|---|
| R-001 | HU-006-01 / 05 | Motor calculabilidad | `validationStatus` evaluado incorrectamente → ubicación calculable marcada como incompleta (o viceversa). Impacta directamente la entrada al motor de cálculo SPEC-009. | Lógica de negocio compleja; impacto financiero indirecto; código nuevo | **A** | ✅ Sí |
| R-002 | HU-006-06 | Concurrencia | PATCH con `version` desactualizada silencioso → datos de otra sesión sobreescritos sin 409. Pérdida de datos irrecuperable. | Operación destructiva sin rollback; integración multi-usuario | **A** | ✅ Sí |
| R-003 | HU-006-04 | Integración externa | CP resuelto incorrectamente (o no resuelto) → `catZone` incorrecta → tarifa de zona catastrófica errónea en motor de cálculo. Dependencia no controlada de core-ohs. | Integración con sistema externo; impacto en cálculo financiero | **A** | ✅ Sí |
| R-004 | HU-006-01 | Persistencia | PUT no reemplaza el array atómicamente → ubicaciones duplicadas o inconsistentes en MongoDB. | Operación destructiva; sin rollback fácil en producción | **A** | ✅ Sí |
| R-005 | HU-006-05 | Regla de negocio | Garantía `glass` con `insuredAmount: 0` marcada como incompleta erróneamente (debería ser válida para `requiresInsuredAmount: false`). Cotización rechazada sin verdadera causa. | Lógica condicional compleja; distinción between 2 tipos de garantías | **A** | ✅ Sí |
| R-006 | HU-006-02 | Validación | `constructionYear` fuera de rango (1799 o 2099) aceptado silenciosamente → dato inválido persistido. | Validación de límite numérico; dato corrompe resultados de tarifa | **A** | ✅ Sí |
| R-007 | HU-006-06 | Integridad referencial | PATCH modifica ubicación diferente a la indicada por `index` → data corruption multi-tenant. | Operación destructiva; sin rollback; impacto en múltiples ubicaciones | **A** | ✅ Sí |
| R-008 | HU-006-08 | Guardado parcial | Ubicación incompleta bloquea el guardado del folio completo → regresión de RN-006-02. Bloquea flujo de usuario. | Regla de negocio fundamental; alta frecuencia de uso | **A** | ✅ Sí |
| R-009 | HU-006-04 | Integración externa | core-ohs 503 no manejado → excepción no controlada → 500 en API → pérdida de datos en formulario. | Dependencia externa; degradación del servicio | **S** | No |
| R-010 | HU-006-03 | Catálogo | Giro con `fireKey` vacío aceptado silenciosamente → ubicación calculable sin fireKey. Motor de cálculo SPEC-009 no puede tarificar. | Lógica de negocio; propagación de dato inválido | **S** | No |
| R-011 | HU-006-01 | Rendimiento | PUT con 50 ubicaciones → timeout o error 500 por límite de BSON (16MB) o tiempo de procesamiento. Dato: cada location ~2KB → 50 = ~100KB (dentro de límite, pero con garantías puede crecer). | Volumen de datos; SLA implícito | **S** | No |
| R-012 | HU-006-07 | Consistencia | `GET /locations/summary` retorna `totalCalculable` inconsistente con el array de `locations[]` (suma incorrecta). | Lógica de agregación; impacto en UI del wizard | **S** | No |
| R-013 | HU-006-06 | UI / UX | Edición vía PATCH no recarga el estado de la grilla → badge de validationStatus stale (muestra estado anterior). | Alta frecuencia de uso; impacto en decisión del usuario | **S** | No |
| R-014 | HU-006-01 | Seguridad | Endpoint PUT `/locations` sin autenticación → acceso no autorizado a datos de folio ajeno. | Autenticación; datos de cliente | **S** | No |
| R-015 | HU-006-08 | UI / UX | Badge ámbar de ubicación incompleta no visible al usuario → usuario desconoce el problema y no lo corrige antes de calcular. | Alta frecuencia de uso; usabilidad | **S** | No |
| R-016 | HU-006-05 | Validación | Garantía con `insuredAmount < 0` aceptada (debería ser >= 0). Suma negativa genera tarifa negativa o error en cálculo. | Validación de dominio; impacto financiero | **S** | No |
| R-017 | HU-006-02 | UI | Formulario step 1 → paso 2 no guarda datos ingresados del step 1 si el usuario navega hacia atrás → pérdida de datos parcial. | Usabilidad; flujo multi-step | **D** | No |
| R-018 | — | Accesibilidad | Badge de estado (calculable/incompleto) solo diferenciado por color → no accesible para usuarios con daltonismo (WCAG AA). | Ajuste de UI sin impacto funcional | **D** | No |
| R-019 | — | Estética | Grilla de ubicaciones no se adapta correctamente a resoluciones < 1024px → columnas truncadas. | Ajuste de estilo; uso tipicamente en desktop | **D** | No |
| R-020 | HU-006-04 | UX | Spinner de resolución CP sin timeout visible → usuario no sabe cuánto esperar si core-ohs tarda > 3s. | Usabilidad; latencia percibida | **D** | No |

---

## Plan de mitigación — Riesgos ALTO

### R-001: validationStatus evaluado incorrectamente

**Área afectada:** `LocationCalculabilityEvaluator.cs` + `UpdateLocationsUseCase` + `PatchLocationUseCase`

**Condiciones de calculabilidad (RN-006-01):**
1. `zipCode` presente y válido (5 dígitos numéricos)
2. `businessLine.fireKey` presente y no vacío
3. Al menos 1 garantía con `insuredAmount > 0` y `requiresInsuredAmount: true`
4. *O* solo garantías con `requiresInsuredAmount: false` (e.g., solo `glass`) → sigue siendo calculable si las demás condiciones se cumplen

**Tests obligatorios:**
- `[Fact]` calculable cuando CP + fireKey + building_fire:5000000 → `"calculable"`
- `[Fact]` glass:0 no genera alerta (requiresInsuredAmount: false) → sigue calculable
- `[Fact]` building_fire:0 genera alerta → `"incomplete"` (requiresInsuredAmount: true)
- `[Fact]` sin zipCode → `"incomplete"` + alerta "Código postal requerido"
- `[Fact]` sin fireKey → `"incomplete"` + alerta
- `[Fact]` sin ninguna garantía → `"incomplete"` + alerta
- `[Theory]` combinaciones 2x2x2 de los 3 factores (8 combinaciones)

**Bloqueante para release:** ✅ Sí

---

### R-002: Versionado optimista — sobreescritura silenciosa

**Área afectada:** `IQuoteRepository.UpdateLocationsAsync` + `IQuoteRepository.PatchLocationAsync`

**Escenario de riesgo:**
```
T=0: Usuario A y Usuario B leen folio con version=5
T=1: Usuario A envía PUT con version=5 → version→6 (OK)
T=2: Usuario B envía PATCH con version=5 → DEBE retornar 409, NO debe escribir
```

**Tests obligatorios:**
- `[Fact]` PUT con version=n-1 → 409 sin modificar datos
- `[Fact]` PATCH con version=n-2 → 409 sin modificar datos
- `[Fact]` PUT concurrente simulado: dos requests con misma version → solo uno debe ser exitoso
- Integration test: PUT exitoso incrementa version en mongo ($set versal)

**Bloqueante para release:** ✅ Sí

---

### R-003: CP resuelto incorrectamente — catZone errónea

**Área afectada:** `ZipCodeProxyService` + frontend lookup + `LocationDto.CatZone`

**Tests obligatorios:**
- Integration: `GET /api/zip-codes/06600` → `catZone: "A"` (fixture: zipCodes.json)
- Integration: `GET /api/zip-codes/03100` → `catZone: "B"`
- Unit: CP inexistente → 404 de core-ohs → ubicación `incomplete`, sin excepción 500
- Unit: core-ohs 503 → manejo graceful (no propagar 500 al cliente)
- Contract: `catZone` en response body es string no nulo cuando CP válido

**Bloqueante para release:** ✅ Sí

---

### R-004: PUT no reemplaza array atómicamente

**Área afectada:** `IQuoteRepository.UpdateLocationsAsync` → operación `$set: { locations: [...] }`

**Tests obligatorios:**
- `[Fact]` PUT con 3 ubicaciones sobre folio con 2 → array mongo tiene exactamente 3 docs
- `[Fact]` PUT con 2 ubicaciones sobre folio con 3 → eliminación implícita confirmada (2 docs en mongo)
- `[Fact]` PUT con [] → array vacío en mongo (no error)
- Integration: verificar que NO hay operaciones `$push` / `$addToSet` (solo `$set`)

**Bloqueante para release:** ✅ Sí

---

### R-005: Garantía flat-rate con insuredAmount 0 marcada incorrectamente como incompleta

**Área afectada:** `LocationCalculabilityEvaluator` — rama `requiresInsuredAmount`

**Tests obligatorios:**
- `[Fact]` glass (requiresInsuredAmount: false) + insuredAmount=0 → no genera alerta
- `[Fact]` illuminated_signs (requiresInsuredAmount: false) + insuredAmount=0 → calculable si demás condiciones OK
- `[Fact]` building_fire (requiresInsuredAmount: true) + insuredAmount=0 → genera alerta específica
- `[Theory]` todos los 14 keys mapeados correctamente según `requiresInsuredAmount` del catálogo

**Bloqueante para release:** ✅ Sí

---

### R-006: constructionYear fuera de rango aceptado silenciosamente

**Área afectada:** `UpdateLocationsRequestValidator` / `PatchLocationRequestValidator`

**Tests obligatorios:**
- `[Theory]` 1799 → 400; `1800` → OK; `2026` → OK; `2027` → 400 (año actual 2026)
- `[Fact]` null/ausente → OK (campo opcional)

**Bloqueante para release:** ✅ Sí

---

### R-007: PATCH modifica ubicación diferente al índice indicado

**Área afectada:** `PatchLocationUseCase` → lógica de búsqueda por `index` en el array

**Tests obligatorios:**
- `[Fact]` PATCH index=2 → solo la ubicación 2 modificada en mongo (`locations.1.*` en dot-notation)
- `[Fact]` PATCH index=2 con folio de 3 ubicaciones → ubicaciones 1 y 3 sin cambios
- `[Fact]` PATCH index=99 con folio de 3 → 404 folioNotFound

**Bloqueante para release:** ✅ Sí

---

### R-008: Ubicación incompleta bloquea guardado (regresión RN-006-02)

**Área afectada:** `UpdateLocationsUseCase` / `PatchLocationUseCase` — flujo de retorno ante validationStatus=incomplete

**Tests obligatorios:**
- `[Fact]` PUT con 1 ubicación sin CP → response 200 (no 400/422)
- `[Fact]` PUT con mezcla calculable+incompleta → response 200, ambas persistidas
- E2E: guardar ubicación incompleta en UI → sin bloqueo, badge ámbar visible

**Bloqueante para release:** ✅ Sí

---

## Cobertura de riesgos por capa

| Capa | Riesgos cubiertos | Nivel mínimo de cobertura recomendado |
|---|---|---|
| Domain / Calculabilidad | R-001, R-005, R-006 | 100% de ramas en `LocationCalculabilityEvaluator` |
| Application / Use Cases | R-002, R-004, R-007, R-008 | 90%+ líneas en use cases de locations |
| API / Controllers | R-014 (auth), R-006 (validators) | 85%+ |
| Integration / core-ohs | R-003, R-009, R-010 | Tests con mock de core-ohs en todas las ramas |
| Frontend / UI | R-013, R-015, R-017 | E2E flujos críticos |
| Performance | R-011 | k6: 50 ubicaciones, TPS definido |

---

## Historial de cambios

| Fecha | Versión | Cambio |
|---|---|---|
| 2026-03-29 | 1.0 | Creación inicial — 19 riesgos identificados |
