---
description: 'Ejecuta el Database Agent para diseñar esquemas de datos, generar entidades de dominio, repositorios MongoDB y seeders en C# a partir de la spec aprobada.'
agent: Database Agent
---

Ejecuta el Database Agent (MARCO DB) para diseñar y gestionar el modelo de persistencia del feature.

**Feature**: ${input:featureName:nombre del feature en kebab-case}

**Instrucciones para @Database Agent:**

1. Lee `.github/instructions/backend.instructions.md` — confirma el motor de BD y la arquitectura Clean Architecture aprobados
2. Lee `.github/docs/lineamientos/dev-guidelines.md`
3. Lee la **Sección 2 — DISEÑO — Modelos de Datos** de `.github/specs/${input:featureName}.spec.md`
4. Escanea entidades y repositorios existentes en:
   - `src/Cotizador.Domain/` — entidades y value objects
   - `src/Cotizador.Application/Ports/` — interfaces de repositorios
   - `src/Cotizador.Infrastructure/Persistence/` — implementaciones MongoDB
5. Ejecuta el flujo completo:
   - Diseña o actualiza el esquema de datos (entidades, campos, índices MongoDB)
   - Genera la entidad de dominio en `src/Cotizador.Domain/<Feature>.cs`
   - Genera la interfaz del repositorio en `src/Cotizador.Application/Ports/I<Feature>Repository.cs`
   - Genera la implementación del repositorio con `MongoDB.Driver` en `src/Cotizador.Infrastructure/Persistence/<Feature>Repository.cs`
   - Genera los índices MongoDB en `src/Cotizador.Infrastructure/Persistence/Indexes/<Feature>Indexes.cs`
   - Genera seeder con datos de prueba sintéticos en `src/Cotizador.Infrastructure/Persistence/Seeders/<Feature>Seeder.cs`
   - Registra la interfaz → implementación en `Program.cs` con `AddScoped`
   - Registra ADR si hay decisiones de diseño relevantes
6. Presenta reporte consolidado de cambios al modelo de datos

**Stack de persistencia obligatorio:**
- ODM: `MongoDB.Driver` oficial (tipado fuerte, BSON nativo)
- Entidades: C# records o clases inmutables en `Cotizador.Domain/`
- Interfaces de repositorio: definidas en `Cotizador.Application/Ports/`
- Implementaciones: únicamente en `Cotizador.Infrastructure/Persistence/`
- Todas las operaciones son `async/await` (`Task` o `Task<T>`)

**Prerequisito:** Debe existir `.github/specs/${input:featureName}.spec.md` con estado APPROVED y Sección 2 completa. Si no, ejecutar `/generate-spec` primero.

**Nota:** Ejecutar ANTES o en paralelo con el backend-developer para que los contratos de persistencia estén definidos antes de implementar los casos de uso en `Cotizador.Application/`.
