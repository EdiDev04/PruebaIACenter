---
id: SPEC-###
status: DRAFT
feature: nombre-del-feature
created: YYYY-MM-DD
updated: YYYY-MM-DD
author: spec-generator
version: "1.0"
related-specs: []
---

# Spec: [Nombre de la Funcionalidad]

> **Estado:** `DRAFT` → aprobar con `status: APPROVED` antes de iniciar implementación.
> **Ciclo de vida:** DRAFT → APPROVED → IN_PROGRESS → IMPLEMENTED → DEPRECATED

---

## 1. REQUERIMIENTOS

### Descripción
Resumen de la funcionalidad en 2-3 oraciones. Qué hace, para quién y qué problema resuelve.

### Requerimiento de Negocio
El requerimiento original tal como fue proporcionado por el usuario (o copiado de `.github/requirements/<feature>.md`).

### Historias de Usuario

#### HU-01: [Título descriptivo corto]

```
Como:        [rol del usuario — ej. Operador, Administrador]
Quiero:      [acción o funcionalidad concreta]
Para:        [valor o beneficio esperado por el negocio]

Prioridad:   Alta / Media / Baja
Estimación:  XS / S / M / L / XL
Dependencias: HU-X, HU-Y o Ninguna
Capa:        Backend
```

#### Criterios de Aceptación — HU-01

**Happy Path**
```gherkin
CRITERIO-1.1: [nombre del escenario exitoso]
  Dado que:  [contexto inicial válido]
  Cuando:    [acción del usuario]
  Entonces:  [resultado esperado verificable]
```

**Error Path**
```gherkin
CRITERIO-1.2: [nombre del escenario de error]
  Dado que:  [contexto inicial]
  Cuando:    [acción inválida o datos incorrectos]
  Entonces:  [manejo del error esperado con código HTTP y mensaje]
```

**Edge Case** *(si aplica)*
```gherkin
CRITERIO-1.3: [nombre del caso borde]
  Dado que:  [contexto de borde]
  Cuando:    [acción en el límite]
  Entonces:  [resultado esperado en el límite]
```

### Reglas de Negocio
1. Regla de validación (ej. "el campo X es obligatorio")
2. Regla de autorización (ej. "requiere `[BasicAuthorize]` con dominio `grupo-exito.com`")
3. Regla de integridad (ej. "el módulo debe ser único en la colección")

---

## 2. DISEÑO

### Modelos de Datos

#### Entidades afectadas
| Entidad | Proyecto | Colección MongoDB | Cambios | Descripción |
|---------|----------|-------------------|---------|-------------|
| `FeatureDocument` | Infrastructure | `collection_name` | nueva / modificada | Documento MongoDB |
| `FeatureDto` | Domain | — | nueva / modificada | DTO de dominio |

#### Campos del modelo
| Campo | Tipo C# | Obligatorio | Validación | Descripción |
|-------|---------|-------------|------------|-------------|
| `Id` | `string` | sí | auto-generado (`ObjectId`) | Identificador MongoDB |
| `name` | `string` | sí | `[Required]` max 100 chars | Nombre del recurso |
| `description` | `string` | no | max 500 chars | Descripción |
| `createdAt` | `DateTime` | sí | auto-generado UTC | Timestamp creación |

#### Índices / Constraints
- Listar índices necesarios con su justificación de uso (búsqueda frecuente, unicidad, etc.)

### API Endpoints

> Ruta base: `api/v1/[controller]/[action]`  
> Autenticación: `[BasicAuthorize("grupo-exito.com")]` en todos los endpoints.

#### POST /api/v1/[Feature]/Create
- **Descripción**: Crea un nuevo recurso
- **Auth requerida**: sí — `[BasicAuthorize]`
- **Request Body**:
  ```json
  { "field": "string" }
  ```
- **Response 200**:
  ```json
  { "header": { "status": 200, "message": "string" }, "data": { } }
  ```
- **Response 400**: modelo inválido o campo obligatorio faltante
- **Response 401**: credenciales ausentes o inválidas
- **Response 500**: error interno del servidor

#### GET /api/v1/[Feature]/GetAll
- **Descripción**: Consulta todos los recursos
- **Auth requerida**: sí
- **Response 200**:
  ```json
  { "header": { "status": 200, "message": "string" }, "data": [ ] }
  ```

#### PUT /api/v1/[Feature]/Update
- **Descripción**: Actualiza un recurso existente
- **Auth requerida**: sí
- **Request Body**: campos a actualizar
- **Response 200**: recurso actualizado
- **Response 400**: modelo inválido
- **Response 404**: no encontrado

#### DELETE /api/v1/[Feature]/Delete
- **Descripción**: Elimina un recurso
- **Auth requerida**: sí
- **Response 200**: eliminado exitosamente
- **Response 404**: no encontrado

### Arquitectura de Capas

#### Interfaces nuevas (Domain)
| Interfaz | Proyecto | Descripción |
|----------|----------|-------------|
| `I[Feature]UseCase` | Domain/Model/Interfaces | Contrato del Use Case |
| `I[Feature]Repository` | Domain/Model/Repository | Contrato del repositorio |

#### Use Cases nuevos (Domain)
| Clase | Implementa | Descripción |
|-------|-----------|-------------|
| `[Feature]UseCase` | `I[Feature]UseCase` | Lógica de negocio |

#### Adapters nuevos (Infrastructure)
| Clase | Colección MongoDB | Descripción |
|-------|------------------|-------------|
| `[Feature]Repository` | `collection_name` | Acceso a datos |

#### Controllers nuevos (API)
| Clase | Ruta base | Use Cases inyectados |
|-------|-----------|---------------------|
| `[Feature]Controller` | `api/v1/[Feature]` | `I[Feature]UseCase` |

#### Registro en Program.cs
```csharp
builder.Services.AddScoped<I[Feature]UseCase, [Feature]UseCase>();
builder.Services.AddScoped<I[Feature]Repository, [Feature]Repository>();
```

### Arquitectura y Dependencias
- Paquetes NuGet nuevos requeridos: ninguno / listar si aplica
- Servicios externos: listar integraciones (MongoDB, RabbitMQ, APIs externas)
- Impacto en `Program.cs`: registrar nuevas dependencias con `AddScoped`

### Notas de Implementación
> Observaciones técnicas, decisiones de diseño o advertencias para los agentes de desarrollo.

---

## 3. LISTA DE TAREAS

> Checklist accionable para todos los agentes. Marcar cada ítem (`[x]`) al completarlo.
> El Orchestrator monitorea este checklist para determinar el progreso.

### Backend

#### Domain — Interfaces y Use Cases
- [ ] Crear interfaz `I[Feature]UseCase` en `Domain/Model/Interfaces/`
- [ ] Crear interfaz `I[Feature]Repository` en `Domain/Model/Repository/`
- [ ] Crear DTO `[Feature]Dto` en `Domain/Model/Document/`
- [ ] Implementar `[Feature]UseCase` en `Domain/UseCase/`

#### Infrastructure — Adapters
- [ ] Crear documento MongoDB `[Feature]Document` en `Infrastructure/Adapters/MongoDb/Model/`
- [ ] Implementar `[Feature]Repository` en `Infrastructure/Adapters/MongoDb/`

#### API — Controller y Wiring
- [ ] Crear `[Feature]Request` en `API/` o `Domain/Model/Request/` con validaciones `[Required]`
- [ ] Implementar `[Feature]Controller` en `API/Controllers/`
- [ ] Registrar en `Program.cs`: `AddScoped<I[Feature]UseCase, [Feature]UseCase>()` y `AddScoped<I[Feature]Repository, [Feature]Repository>()`

#### Tests (ReenviarDocElectronicos.UnitTest)
- [ ] `[Feature]UseCaseTest` — `Execute_Should_[DoSomething]_WhenValidData` (happy path)
- [ ] `[Feature]UseCaseTest` — `Execute_Should_ThrowArgumentException_WhenRepositoryFails` (error path)
- [ ] `[Feature]ControllerTest` — `Post_Should_Return200_WhenRequestIsValid` (happy path)
- [ ] `[Feature]ControllerTest` — `Post_Should_Return400_WhenModelStateIsInvalid` (error path)
- [ ] `[Feature]RepositoryTest` — `[Method]_Should_[DoSomething]_WhenValidData` (happy path)
- [ ] `[Feature]RepositoryTest` — `[Method]_Should_ThrowArgumentException_WhenMongoFails` (error path)

### QA
- [ ] Ejecutar skill `/gherkin-case-generator` → criterios CRITERIO-1.1, 1.2, 1.3
- [ ] Ejecutar skill `/risk-identifier` → clasificación ASD de riesgos
- [ ] Revisar cobertura de tests contra criterios de aceptación
- [ ] Validar que todas las reglas de negocio están cubiertas
- [ ] Actualizar estado spec: `status: IMPLEMENTED`
