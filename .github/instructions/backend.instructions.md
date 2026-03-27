---
applyTo: "src/**/*.cs"
---

> **Scope**: Se aplica al proyecto `Exito.ReenviarDocElectronicos.Services` (.NET / ASP.NET Core Web API con C#).

# Instrucciones para Archivos de Backend (C# / ASP.NET Core)

## Stack Técnico

| Componente | Tecnología |
|---|---|
| Lenguaje | C# (.NET 9) |
| Framework web | ASP.NET Core Web API |
| Base de datos | MongoDB (via `MongoDB.Driver`) |
| Mapeo de objetos | AutoMapper |
| Logging | Serilog |
| Autenticación | Basic Auth (`[BasicAuthorize]`) |
| Configuración | `appsettings.json` + interfaces `ISettings` |

## Arquitectura en Capas

El proyecto sigue una arquitectura hexagonal (Clean Architecture) dividida en tres proyectos:

```
API (Controllers) → Domain (UseCases + Interfaces) → Infrastructure (Adapters/Repositories)
```

- **`ReenviarDocElectronicos.API/Controllers/`**: Solo parsear HTTP + delegar al Use Case. Sin lógica de negocio.
- **`ReenviarDocElectronicos.Domain/UseCase/`**: Toda la lógica de negocio. Recibe repositorios por constructor. Implementa la interfaz correspondiente.
- **`ReenviarDocElectronicos.Domain/Model/`**: DTOs, modelos de dominio e interfaces de repositorios y casos de uso.
- **`ReenviarDocElectronicos.Infrastructure/Adapters/`**: Único lugar con acceso a MongoDB. Implementa las interfaces definidas en Domain.

## Wiring de Dependencias (patrón obligatorio en `Program.cs`)

Todo el registro de dependencias vive en `Program.cs` (Composition Root). Seguir siempre el patrón:

```csharp
// ✅ Correcto — registrar interfaz → implementación con AddScoped
builder.Services.AddScoped<IMyUseCase, MyUseCase>();
builder.Services.AddScoped<IMyRepository, MyRepository>();

// ✅ Correcto — constructor injection en Controller
public MyController(IMyUseCase myUseCase)
{
    _myUseCase = myUseCase;
}

// ✅ Correcto — constructor injection en UseCase
public MyUseCase(IMyRepository myRepository, ILogger<MyUseCase> logger)
{
    _myRepository = myRepository;
    _logger = logger;
}
```

- Repositorios e interfaces de Use Cases → `AddScoped`.
- Configuraciones (`ISettings`) y clientes RabbitMQ → `AddSingleton`.
- Clientes MongoDB (`IMongoClient`) → `AddSingleton`.

NUNCA instanciar repositorios o Use Cases con `new` fuera de `Program.cs`.
NUNCA resolver dependencias manualmente con `ServiceLocator` o `GetService<>()` dentro de clases de negocio.

## Convenciones de Código

- Clases y métodos públicos en `PascalCase`.
- Campos privados en `_camelCase`.
- Interfaces con prefijo `I` (ej. `IMyUseCase`, `IMyRepository`).
- Use Cases con sufijo `UseCase` (ej. `SearchElectronicDocumentDianUseCase`).
- Repositorios con sufijo `Repository` o `Adapter` (ej. `ConfigurationRepository`, `ApprovedInvoiceDianAdapter`).
- Controllers con sufijo `Controller` y ruta base `api/v1/[controller]/[action]`.
- Todas las operaciones de MongoDB son `async/await` (retornan `Task` o `Task<T>`).
- Configuración accedida siempre a través de `ISettings` inyectado por constructor.
- Logging estructurado con Serilog usando las constantes de operación definidas en cada clase.

## Nuevas Rutas / Controladores

Para agregar un nuevo endpoint:
1. Crear la interfaz del Use Case en `ReenviarDocElectronicos.Domain/Model/Interfaces/`.
2. Implementar el Use Case en `ReenviarDocElectronicos.Domain/UseCase/`.
3. Si necesita acceso a datos, crear la interfaz del repositorio en `Domain/Model/Repository/` e implementarla en `ReenviarDocElectronicos.Infrastructure/Adapters/`.
4. Registrar la nueva interfaz → implementación en `Program.cs` con `AddScoped`.
5. Crear o actualizar el Controller en `ReenviarDocElectronicos.API/Controllers/` inyectando el Use Case por constructor.

> Ver `README.md` para la estructura de carpetas específica del proyecto.

## Nunca hacer

- Lógica de negocio en los Controllers.
- Acceso a MongoDB fuera de la capa de Infrastructure.
- Instanciar repositorios o Use Cases con `new` dentro de Controllers o Use Cases.
- Operaciones MongoDB síncronas (siempre `await`).
- Registrar dependencias fuera de `Program.cs`.

---

> Para estándares de código limpio, SOLID, nombrado, API REST, seguridad y observabilidad, ver `.github/docs/lineamientos/dev-guidelines.md`.
