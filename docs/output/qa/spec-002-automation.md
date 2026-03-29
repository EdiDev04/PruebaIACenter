# Propuesta de Automatización — SPEC-002: Quote Data Model and Persistence

> Metodología: Criterios REIC (Repetitivo · Estable · Alto Impacto · Costo Manual alto)
> Spec fuente: `.github/specs/quote-data-model.spec.md` (status: IMPLEMENTED)
> Fecha: 2026-03-28

---

## Resumen Ejecutivo

| Flujos candidatos | P1 (automatizar ya) | P2 (próximo sprint) | P3 (backlog) | Posponer |
|:---:|:---:|:---:|:---:|:---:|
| 6 | 3 | 2 | 1 | 0 |

**Framework recomendado**: xUnit + MongoDB.Driver (integration tests) para todos los flujos de repositorio y middleware. Newman/Postman para validación de contratos HTTP. No se requiere k6 (no hay SLAs de performance definidos en esta spec).

**Costo estimado de implementación**: 1.5 sprints (suite completa P1 + P2)

---

## Criterios de Evaluación por Flujo

| ID | Flujo | Repetitivo | Estable | Alto Impacto | Costo Manual | ROI | Prioridad |
|---|---|:---:|:---:|:---:|:---:|:---:|:---:|
| F-001 | CRUD del repositorio (unit + integration) | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| F-002 | Optimistic locking — concurrencia | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| F-003 | Middleware exception mapping | ✅ Alta | ✅ Sí | ✅ Alta | ✅ Alto | 4/4 | **P1** |
| F-004 | BasicAuth — validación de credenciales | ✅ Alta | ✅ Sí | ⚠️ Media | ✅ Alto | 3/4 | **P2** |
| F-005 | MongoDB — índice único `folioNumber` | ⚠️ Media | ✅ Sí | ✅ Alta | ⚠️ Medio | 3/4 | **P2** |
| F-006 | CoreOhs client — resiliencia ante 503 | ⚠️ Media | ✅ Sí | ✅ Alta | ❌ Bajo | 2/4 | **P3** |

---

## Hoja de Ruta por Sprint

### Sprint 1 — P1 (Automatizar ya)

#### F-001: CRUD del repositorio
- **Framework**: xUnit + EphemeralMongo (MongoDB in-memory) o Testcontainers (MongoDB container)
- **Tests a cubrir**:
  - `CreateAsync` — folio nuevo → persiste con `version=1`, `quoteStatus="draft"`, `metadata.createdAt` no nulo
  - `CreateAsync` — folio duplicado → lanza `MongoWriteException` con E11000
  - `GetByFolioNumberAsync` — folio existente → retorna objeto completo
  - `GetByFolioNumberAsync` — folio inexistente → retorna `null`
  - `GetByIdempotencyKeyAsync` — key existente → retorna folio correcto
  - `UpdateGeneralInfoAsync` — versión correcta → `version+1`, solo fields de sección modificados
  - `UpdateFinancialResultAsync` — versión correcta → `netPremium`, `commercialPremium`, `quoteStatus="calculated"` actualizados; `insuredData` y `locations` intactos
- **Estimación**: 8h implementación · 2h mantenimiento/sprint
- **Costo manual equivalente**: ~40 min/ejecución × 20 releases/año = **800 min/año** ahorrados
- **ROI**: Alto — los CRUD se ejecutan en cada PR y pipeline CI

#### F-002: Optimistic locking — concurrencia
- **Framework**: xUnit + Testcontainers (MongoDB real — no mock para este caso)
- **Tests a cubrir**:
  - Un update con versión correcta → éxito, `version+1`
  - Un update con versión stale → `VersionConflictException`
  - Dos tasks en paralelo sobre el mismo folio mismo version → exactamente una gana, otra lanza excepción; version final = N+1
  - Escritura fallada → `version` y `metadata.updatedAt` no cambian en DB
- **Estimación**: 5h implementación · 1h mantenimiento/sprint
- **Costo manual equivalente**: imposible reproducir concurrencia manualmente de forma consistente → **riesgo latente sin automatización**
- **ROI**: Crítico — sin este test, el bug de concurrencia solo se detecta en producción

#### F-003: Middleware exception mapping
- **Framework**: xUnit + `WebApplicationFactory<Program>` (ASP.NET Core integration test)
- **Tests a cubrir**:
  - `FolioNotFoundException` → 404, body `type: "folioNotFound"`, sin stack trace
  - `VersionConflictException` → 409, body `type: "versionConflict"`, sin stack trace
  - `InvalidQuoteStateException` → 422, body `type: "invalidQuoteState"`
  - `CoreOhsUnavailableException` → 503, body `type: "coreOhsUnavailable"`
  - `Exception` genérica → 500, body `message: "Internal server error"`, sin stack trace, sin `"at System."`
- **Estimación**: 4h implementación · 1h mantenimiento/sprint
- **Costo manual equivalente**: ~25 min/ejecución × 20 releases/año = **500 min/año** ahorrados
- **ROI**: Alto — el mapeo es un contrato con el frontend; una regresión rompe toda la capa de errores UI

---

### Sprint 2 — P2 (Próximo sprint)

#### F-004: BasicAuth — validación de credenciales
- **Framework**: xUnit + `WebApplicationFactory<Program>`
- **Tests a cubrir**:
  - Header correcto (`admin:cotizador2026` en base64) → 200
  - Contraseña incorrecta → 401
  - Sin header → 401 + `WWW-Authenticate: Basic`
  - Header malformado (Bearer token, base64 inválido) → 401
- **Estimación**: 3h implementación · 0.5h mantenimiento/sprint
- **Costo manual equivalente**: ~15 min/ejecución × 20 releases/año = **300 min/año** ahorrados
- **ROI**: Medio-Alto — seguridad, pero el mecanismo es simple y estable

#### F-005: MongoDB — índice único en `folioNumber`
- **Framework**: xUnit + Testcontainers
- **Tests a cubrir**:
  - Verificar que el índice existe en la colección después del boot
  - Doble inserción del mismo `folioNumber` → error MongoDB E11000
  - `folioNumber` con formato inválido rechazado antes de llegar a MongoDB
- **Estimación**: 3h implementación · 0.5h mantenimiento/sprint
- **Costo manual equivalente**: detección tardía (producción) → ~8h de debugging/incidente
- **ROI**: Medio — protección de integridad de datos

---

### Sprint 3 — P3 (Backlog, no urgente)

#### F-006: CoreOhs client — resiliencia ante 503
- **Framework**: xUnit + Moq (mock de `ICoreOhsClient`)
- **Tests a cubrir**:
  - `CoreOhsClient` lanza excepción de red → use case la convierte en `CoreOhsUnavailableException`
  - Middleware mapea a 503 con `type: "coreOhsUnavailable"`
- **Nota**: Este flujo está parcialmente cubierto por F-003 (middleware). La capa de resiliencia (timeouts, retries con Polly) se especificará en specs futuras que consuman `ICoreOhsClient`.
- **Estimación**: 4h implementación · 1h mantenimiento/sprint
- **ROI**: Medio — el impacto depende de qué tan frecuente sea `core-ohs` no disponible en el ambiente real

---

## DoR de Automatización (Definition of Ready)

Antes de iniciar la automatización de cualquier flujo, verificar:

- [ ] El flujo fue ejecutado manualmente al menos una vez sin bugs críticos abiertos
- [ ] Los datos de prueba están identificados y disponibles (ver tabla de datos de prueba abajo)
- [ ] El ambiente de CI tiene MongoDB disponible (Testcontainers o MongoDB Atlas Test Project)
- [ ] Las credenciales de test están en variables de entorno del pipeline, nunca hardcodeadas
- [ ] El equipo aprobó el flujo para automatización

## DoD de Automatización (Definition of Done)

Al completar la automatización de un flujo:

- [ ] Tests pasan en modo `dotnet test` local y en el pipeline CI
- [ ] Código revisado por pares (PR aprobado)
- [ ] Integrado al pipeline CI con gate en PR (falla el merge si el test falla)
- [ ] Tiempo de ejecución < 60s por suite (para no bloquear el pipeline)
- [ ] No hay datos de producción ni credenciales reales en los fixtures de test

---

## Datos de Prueba Recomendados

| Escenario | Campo | Valor válido | Valor inválido |
|---|---|---|---|
| Creación de folio | `folioNumber` | `DAN-2026-00001` | `INVALID-FORMAT`, `DAN-26-00001`, `""` |
| Idempotencia | `idempotencyKey` | `550e8400-e29b-41d4-a716-446655440000` | `not-a-uuid` |
| Optimistic locking | `version` correcta | `3` | `2` (cuando actual es `3`) |
| General info | `insuredData.name` | `Test Corp SA` | `""` (vacío) |
| General info | `insuredData.taxId` | `TCO850101ABC` | `INVALID-TAX` |
| Agent code | `agentCode` | `AGT-001` | `A-1`, `""` |
| Basic Auth | credentials | `admin` / `cotizador2026` | `admin` / `wrong-pass` |
| Auth header | base64 | `YWRtaW46Y290aXphZG9yMjAyNg==` | `invalidbase64===` |
| Financial result | `netPremium` | `12500.00` | `-100.00` |

---

## Justificación de ROI para el Reto

El reto evalúa dos criterios directamente relacionados con los flujos P1:

1. **Trazabilidad del cálculo** (F-001, F-002): Los tests de repositorio y concurrencia garantizan que el aggregate root `PropertyQuote` jamás pierde datos por una escritura concurrente o una actualización parcial mal construida. Sin estos tests, el evaluador podría detectar inconsistencias en los datos al ejecutar flujos de cálculo.

2. **Consistencia de APIs y manejo de errores** (F-003): El middleware de excepciones es el contrato entre backend y frontend. Los tests P1 de F-003 aseguran que el evaluador siempre recibe respuestas con el `type` correcto, sin stack traces, con el HTTP status esperado — independientemente del camino de error que tome.

**Flujos con ROI más alto**:
1. **F-002 — Optimistic locking** (ROI crítico): imposible de cubrir manualmente de forma confiable; bug en producción = pérdida de datos.
2. **F-001 — CRUD del repositorio** (ROI 4/4): se ejecuta en cada PR; detecta regresiones de sección-isolation (R-002) y de índice único (R-003).
3. **F-003 — Middleware exception mapping** (ROI 4/4): contrato con el frontend; cobertura alta, mantenimiento bajo, ejecución rápida.
