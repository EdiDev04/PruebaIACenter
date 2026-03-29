# Matriz de Riesgos — SPEC-002: Quote Data Model and Persistence

> Metodología: Regla ASD (Alto = obligatorio automatizar / bloquea release · Medio = recomendado · Bajo = opcional)
> Spec fuente: `.github/specs/quote-data-model.spec.md` (status: IMPLEMENTED)
> Fecha de análisis: 2026-03-28

---

## Resumen Ejecutivo

| Total riesgos | Alto (A) | Medio (S) | Bajo (D) |
|:---:|:---:|:---:|:---:|
| 10 | 5 | 3 | 2 |

---

## Tabla de Riesgos

| ID | HU / Área | Descripción del Riesgo | Factor ASD | Probabilidad | Impacto | Nivel | Testing | Estado |
|---|---|---|---|---|---|---|---|---|
| R-001 | HU-002-03 · Optimistic Locking | Dos usuarios actualizan el mismo folio simultáneamente; ambas escrituras aceptadas → clobber de datos silencioso | Concurrencia · Pérdida de datos | Alta | Alto | **A** | Obligatorio | Abierto |
| R-002 | HU-002-02 · Partial Updates | `UpdateFinancialResultAsync` sobreescribe `locations` o `insuredData` si el `$set` MongoDB incluye campos fuera de su sección | Parcialidad de updates | Media | Alto | **A** | Obligatorio | Abierto |
| R-003 | HU-002-01 · Persistencia | Inserción duplicada del mismo `folioNumber` pasa sin error si el índice único no existe o no se crea en el boot | Integridad de datos | Media | Alto | **A** | Obligatorio | Abierto |
| R-004 | Auth · Seguridad | Credenciales básicas expuestas en `appsettings.json` en repositorio; rotación no prevista en spec | Seguridad · Datos sensibles | Alta | Alto | **A** | Obligatorio | Abierto |
| R-005 | Middleware · Seguridad | Stack trace o detalles internos filtrados en respuestas HTTP 500 (ya corregido en code-quality) | Seguridad · Exposición de info | Baja | Alto | **A** | Obligatorio | ✅ Mitigado |
| R-006 | Infrastructure · BSON | Convención camelCase no aplicada uniformemente → campos persistidos con PascalCase en MongoDB → deserialización falla | Convención BSON | Media | Medio | **S** | Recomendado | Abierto |
| R-007 | HU-002-04 · Metadata | `metadata.updatedAt` no se refresca en alguna operación de escritura → auditoría incorrecta | Lógica de negocio | Baja | Medio | **S** | Recomendado | Abierto |
| R-008 | Integration · CoreOhs | `CoreOhsClient` no disponible → requests cuelgan sin timeout explícito → cascada de fallos | Integración externa | Media | Medio | **S** | Recomendado | Abierto |
| R-009 | Infrastructure · CORS | CORS abierto sin guarda de entorno en ambientes distintos de Development | Seguridad | Baja | Bajo | **D** | Opcional | ✅ Mitigado |
| R-010 | HU-002-01 · Validación | `folioNumber` con formato inválido (`DAN-26-00001`, vacío) persiste si la validación no está en la capa correcta | Validación de entrada | Baja | Bajo | **D** | Opcional | Abierto |

---

## Plan de Mitigación — Riesgos ALTO

### R-001: Optimistic locking — escritura concurrente silenciosa

- **Descripción del escenario de fallo**: Usuario A lee `version=3`, Usuario B lee `version=3`. B escribe primero → version=4. A intenta escribir con version=3 → debería lanzar `VersionConflictException`. Si el filtro MongoDB `{ folioNumber, version: 3 }` no detecta correctamente el `ModifiedCount==0`, ambas escrituras pasan.
- **Mitigación técnica**:
  - Verificar que cada método de update en `QuoteRepository` usa el patrón: `UpdateOne({ folioNumber, version: N }, $set: { ..., version: N+1 })` y chequea `result.ModifiedCount == 0` para lanzar la excepción.
  - Agregar test de integración con paralelismo (dos `Task` simultáneos sobre el mismo folio).
- **Tests obligatorios**:
  - Unit: `QuoteRepository` — mock de MongoDB retorna `ModifiedCount=0` → `VersionConflictException`.
  - Integration: dos requests concurrentes al mismo folio — exactamente uno gana.
- **Bloqueante para release**: ✅ Sí

---

### R-002: Partial update — sección financiera sobreescribe otras secciones

- **Descripción del escenario de fallo**: `UpdateFinancialResultAsync` incluye accidentalmente en su `$set` los fields `insuredData` o `locations`, borrando datos previamente guardados.
- **Mitigación técnica**:
  - Revisar que el `UpdateDefinition` de cada método solo lista los campos de su sección (ver §3.6 de la spec).
  - Test: persistir un folio con `insuredData` y `locations` completos → llamar `UpdateFinancialResultAsync` → verificar que `insuredData` y `locations` no cambian.
- **Tests obligatorios**:
  - Integration: estado antes/después de cada método de update, verificando que los campos fuera de sección permanecen intactos.
- **Bloqueante para release**: ✅ Sí

---

### R-003: Índice único en `folioNumber` — doble inserción no rechazada

- **Descripción del escenario de fallo**: Si el índice único de MongoDB no se crea al iniciar la aplicación (o se crea en segundo plano después de la primera inserción), dos documentos con el mismo `folioNumber` pueden coexistir.
- **Mitigación técnica**:
  - Verificar que `ServiceCollectionExtensions` o `Program.cs` crea el índice `{ folioNumber: 1 }, { unique: true }` de forma síncrona antes de aceptar tráfico.
  - Test de integración: insertar mismo `folioNumber` dos veces → confirmar excepción MongoDB `E11000`.
- **Tests obligatorios**:
  - Integration: doble `CreateAsync` con mismo `folioNumber` → falla en el segundo.
- **Bloqueante para release**: ✅ Sí

---

### R-004: Credenciales hardcodeadas en `appsettings.json`

- **Descripción del escenario de fallo**: `appsettings.json` con `"Auth": { "Username": "admin", "Password": "cotizador2026" }` comiteado en el repositorio → exposición de credenciales de producción.
- **Mitigación técnica**:
  - Mover las credenciales a `appsettings.Development.json` (no comitear) o a variables de entorno / Azure Key Vault para producción.
  - Agregar `.gitignore` entry para `appsettings.Production.json`.
  - `appsettings.json` debe tener valores placeholder (e.g., `"${AUTH_USERNAME}"`).
- **Tests obligatorios**:
  - Security check: validar que `appsettings.json` del entorno productivo no contiene contraseñas en texto plano (pipeline CI scan).
  - Unit: `BasicAuthHandler` — credenciales incorrectas → 401; correctas → 200.
- **Bloqueante para release**: ✅ Sí

---

### R-005: Stack trace en HTTP 500 (MITIGADO)

- **Estado**: ✅ Mitigado — corregido en el ciclo `code-quality` previo a esta fase QA.
- **Verificación residual**: el escenario Gherkin `@middleware @seguridad` "Middleware does NOT expose stack trace" cubre la regresión automáticamente.

---

## Mitigaciones Aplicadas Previamente

| ID | Riesgo | Fix aplicado |
|---|---|---|
| R-005 | Stack trace en 500 | `ExceptionHandlingMiddleware` retorna `"message": "Internal server error"` sin detalles internos |
| R-009 | CORS sin env guard | `builder.Environment.IsDevelopment()` guard aplicado en `Program.cs` |
