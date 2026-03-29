# Reporte de calidad — SPEC-002 quote-data-model — 2026-03-28

---

## Parte 1 — Auditoría de arquitectura

### Resumen

| Severidad | Total | Bloquea qa-agent |
|-----------|-------|-----------------|
| CRÍTICO   | 1     | Sí              |
| MAYOR     | 3     | No              |
| MENOR     | 3     | No              |

### Violaciones críticas

#### CRIT-001
- **Archivo:** `cotizador-backend/src/Cotizador.API/Program.cs`
- **Línea:** 29–32
- **Regla:** OWASP A05 Security Misconfiguration — CORS sin restricción de entorno
- **Detalle:** `AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()` se aplica en **todos los entornos** (incluyendo producción). El comentario dice "for development" pero no existe un guard `if (app.Environment.IsDevelopment())`. En producción, cualquier origen puede realizar solicitudes cross-origin con cualquier método HTTP, incluyendo `DELETE` y `PUT`.
- **Acción requerida:** Envolver la política CORS permisiva dentro de `if (app.Environment.IsDevelopment())`. Definir una política restrictiva para producción con orígenes explícitos vía `appsettings.Production.json`.

---

### Violaciones mayores

#### MAYOR-001
- **Archivo:** `cotizador-backend/src/Cotizador.API/Auth/BasicAuthHandler.cs`
- **Línea:** 65–68
- **Regla:** Excepción capturada con catch vacío / swallowed — MAYOR
- **Detalle:** El bloque `catch` general captura **toda excepción** (incluidas `OutOfMemoryException`, `ThreadAbortException`) sin registrar nada en el logger. Los fallos de autenticación quedan silenciados e invisibles para operaciones y monitoreo de seguridad (OWASP A09 — Security Logging and Monitoring Failures).
- **Acción:** Agregar `_logger.LogWarning(ex, "Error parsing Authorization header")` antes del `return`.

#### MAYOR-002
- **Archivo:** `cotizador-backend/src/Cotizador.API/Middleware/ExceptionHandlingMiddleware.cs`
- **Línea:** 49
- **Regla:** Errores internos revelan detalles de implementación — OWASP A05
- **Detalle:** El handler de `CoreOhsUnavailableException` devuelve `ex.Message` directamente al cliente HTTP. El mensaje incluye la ruta interna del servicio (e.g., `"Error communicating with core-ohs at '/v1/subscribers'."`), exponiendo la topología interna del sistema al consumidor externo.
- **Acción:** Retornar un mensaje genérico al cliente (`"Core service temporarily unavailable"`) y registrar `ex.Message` solo en el log interno.

#### MAYOR-003
- **Archivo:** `cotizador-backend/src/Cotizador.Domain/ValueObjects/` (todos los archivos)
- **Regla:** Single Responsibility / Naming — clases en `ValueObjects/` no son value objects DDD
- **Detalle:** `InsuredData`, `QuoteMetadata`, `BusinessLine`, `ConductionData`, `CoverageOptions`, `LayoutConfiguration`, `CoveragePremium` y `LocationPremium` son **clases mutables** con setters públicos y sin igualdad por valor (`Equals`/`GetHashCode`/tipo `record`). Llamarlos "ValueObjects" crea una abstracción engañosa que viola el principio de Single Responsibility (la carpeta mezcla semántica DDD con transfer bags). Esto además dificulta la detección de regresiones cuando se comparten referencias mutables.
- **Acción:** Renombrar la carpeta a `Models/` o convertir las clases a `record` types inmutables si el serializer MongoDB lo permite. Si permanecen mutables, retirarlos del namespace de ValueObjects.

---

### Sugerencias menores

#### MENOR-001
- **Archivo:** `cotizador-backend/src/Cotizador.Infrastructure/Persistence/QuoteRepository.cs`
- **Línea:** 33
- **Regla:** Magic string en filtro MongoDB
- **Detalle:** `Filter.Eq("metadata.idempotencyKey", idempotencyKey)` usa un string literal. Un cambio en el nombre serializado del campo no generará error de compilación.
- **Acción:** Usar expresión tipada: `Filter.Eq(q => q.Metadata.IdempotencyKey, idempotencyKey)` o definir una constante.

#### MENOR-002
- **Archivo:** `cotizador-backend/src/Cotizador.Infrastructure/Persistence/QuoteRepository.cs`
- **Línea:** 105
- **Regla:** Magic string en update MongoDB
- **Detalle:** `.Set($"locations.{arrayIndex}", patchData)` usa interpolación de string para acceder a elementos de array. Aceptable como patrón MongoDB, pero frágil ante renombrado del campo `locations`.
- **Acción:** Definir una constante para el nombre del campo (`private const string LocationsField = "locations"`).

#### MENOR-003
- **Archivo:** `cotizador-backend/src/Cotizador.API/appsettings.json`
- **Línea:** 19–22
- **Regla:** Credenciales de desarrollo en archivo base (no en `appsettings.Development.json`)
- **Detalle:** `Auth.Username = "admin"` y `Auth.Password = "cotizador2026"` están en `appsettings.json` (base, versionado). Deberían estar únicamente en `appsettings.Development.json` (gitignored) o en User Secrets / variables de entorno. La regla del proyecto permite appsettings, pero el archivo base se comparte entre todos los entornos.
- **Acción:** Mover al archivo `appsettings.Development.json` y configurar el archivo `.gitignore` adecuadamente.

---

## Parte 2 — Análisis estático SonarQube

### Resumen ejecutivo

- **Project Key:** No resuelto — workspace no vinculado a SonarQube Cloud/Server en Connected Mode
- **Archivos analizados con Roslyn (local):** 5 archivos (QuoteRepository.cs, BasicAuthHandler.cs, CoreOhsClient.cs, ExceptionHandlingMiddleware.cs, Program.cs, PropertyQuote.cs)
- **Gate SonarQube Server:** N/A (no Connected Mode disponible)
- **Análisis local SonarLint:** Disparado. No se reportaron issues BLOCKER ni CRITICAL adicionales en PROBLEMS view más allá de los detectados manualmente.

### Conteo de issues (análisis local Roslyn + auditoría manual)

| Severidad | Total |
|-----------|-------|
| BLOCKER   | 0     |
| CRITICAL  | 1     |
| MAJOR     | 3     |
| MINOR     | 3     |
| INFO      | 0     |

### Issues CRITICAL — Acción requerida

| # | Archivo | Línea | Regla | Mensaje | Severidad |
|---|---------|-------|-------|---------|-----------|
| 1 | `Cotizador.API/Program.cs` | 29–32 | OWASP-A05 / S5122 | CORS AllowAnyOrigin sin restricción de entorno — Security Hotspot confirmado | CRITICAL |

### Issues MAJOR — Revisión recomendada

| # | Archivo | Línea | Regla | Mensaje |
|---|---------|-------|-------|---------|
| 1 | `Cotizador.API/Auth/BasicAuthHandler.cs` | 65–68 | S2486 / S1166 | Catch vacío swallows todas las excepciones sin logging |
| 2 | `Cotizador.API/Middleware/ExceptionHandlingMiddleware.cs` | 49 | OWASP-A05 | ex.Message de CoreOhsUnavailableException expone paths internos al cliente HTTP |
| 3 | `Cotizador.Domain/ValueObjects/*.cs` | — | SRP / DDD | Clases mutables sin value equality nombradas como ValueObjects — abstracción engañosa |

---

## Parte 3 — Verificación de reglas específicas SPEC-002

### Optimistic locking
- `BuildVersionedFilter` usa filtro compuesto `{ folioNumber, version }`: ✅
- `ExecuteUpdateAsync` verifica `result.ModifiedCount == 0` y lanza `VersionConflictException`: ✅
- Todos los métodos `Update*` y `PatchLocation*` usan este patrón: ✅

### IQuoteRepository en Application/Ports
- Interfaz correctamente ubicada en `Cotizador.Application/Ports/`: ✅
- Todos los métodos retornan `Task<T>` con `CancellationToken`: ✅

### Excepciones de dominio en Domain/Exceptions
- `FolioNotFoundException`, `VersionConflictException`: ✅ — correctamente ubicadas y con propiedades de contexto
- Ausencia de `InvalidQuoteStateException` — referenciada en middleware pero su ubicación no fue auditada (posiblemente ya existe)

### ICoreOhsClient — inyección de parámetros en URL
- `Uri.EscapeDataString()` aplicado en todos los parámetros de query string y path: ✅

---

## Veredicto consolidado

| Fuente | Estado |
|--------|--------|
| Auditoría arquitectura | FAIL — 1 crítico (CORS sin env guard) |
| SonarQube local (Roslyn) | PASS — 0 BLOCKER, 0 CRITICAL adicionales |
| **Gate final** | **FAILED** |

**Causa raíz del fallo:** La política CORS de `Program.cs` permite cualquier origen, cabecera y método HTTP en todos los entornos sin distinción. Esto es un Security Hotspot OWASP A05 confirmado que se aplicaría a producción tal como está el código. Debe resolverse antes de proceder a Fase 3.

Los issues MAYOR-001 (catch swallowed en BasicAuthHandler), MAYOR-002 (exposición de path interno en CoreOhsUnavailableException) y MAYOR-003 (mutable "ValueObjects") deben corregirse pero no bloquean el gate.

---

```
QUALITY_GATE: FAILED — 1 violación crítica de arquitectura/seguridad, ver .github/docs/code-quality-report.md
```
