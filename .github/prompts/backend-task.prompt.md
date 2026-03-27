---
name: backend-task
description: Implementa una funcionalidad en el backend ASP.NET Core Web API (C# .NET 9) basada en una spec ASDD aprobada.
argument-hint: "<nombre-feature> (debe existir .github/specs/<nombre-feature>.spec.md)"
agent: backend-developer
tools:
  - edit/createFile
  - edit/editFiles
  - read/readFile
  - search/listDirectory
  - search
  - execute/runInTerminal
---

Implementa el backend para el feature especificado, siguiendo la spec aprobada.

**Feature**: ${input:featureName:nombre del feature en kebab-case}

## Pasos obligatorios:

1. **Lee la spec** en `.github/specs/${input:featureName:nombre-feature}.spec.md` — si no existe, detente e informa al usuario.
2. **Lee las instrucciones de stack** en `.github/instructions/backend.instructions.md`.
3. **Revisa el código existente** en `src/` para entender patrones actuales de la Clean Architecture.
4. **Implementa en orden**:
   - `ReenviarDocElectronicos.Domain/Model/` — DTOs, modelos de dominio e interfaces (`IXUseCase`, `IXRepository`)
   - `ReenviarDocElectronicos.Infrastructure/Adapters/` — repositorio con `MongoDB.Driver` (implementa `IXRepository`)
   - `ReenviarDocElectronicos.Domain/UseCase/` — Use Case con lógica de negocio (implementa `IXUseCase`)
   - `ReenviarDocElectronicos.API/Controllers/` — Controller ASP.NET Core (inyecta `IXUseCase` por constructor)
5. **Registra las dependencias** en `Program.cs` usando `AddScoped<IXUseCase, XUseCase>()` y `AddScoped<IXRepository, XRepository>()`.
6. **Verifica compilación** ejecutando: `dotnet build`

## Restricciones:
- Sigue el patrón de wiring: registrar `interfaz → implementación` con `AddScoped` en `Program.cs`. NUNCA instanciar con `new` fuera de `Program.cs`.
- Los Controllers solo parsean HTTP y delegan al Use Case. Sin lógica de negocio en Controllers.
- El acceso a MongoDB es EXCLUSIVO de la capa `Infrastructure/Adapters/`. Nunca en Domain ni API.
- Todas las operaciones de MongoDB deben ser `async`/`await` (retornar `Task` o `Task<T>`).
- Usar `_camelCase` para campos privados y `PascalCase` para clases y métodos públicos.
- Las interfaces llevan prefijo `I` (ej. `IMyUseCase`, `IMyRepository`).
- Logging con Serilog. Autenticación con `[BasicAuthorize]` donde corresponda.
