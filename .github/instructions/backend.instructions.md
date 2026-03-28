---
applyTo: "src/**/*.cs"
---

> **Scope**: Se aplica al proyecto `Cotizador` (.NET / ASP.NET Core Web API con C#).

# Instrucciones para Archivos de Backend (C# / ASP.NET Core)

## Stack Técnico

| Capa | Tecnología |
|---|---|
| Lenguaje | C# (.NET 8) |
| Framework web | ASP.NET Core 8 (Controllers) |
| ODM para MongoDB | `MongoDB.Driver` oficial |
| Validación | FluentValidation |
| Mapeo | Mapster o AutoMapper |
| Testing | xUnit + Moq + FluentAssertions |
| HTTP Client (`core-ohs`) | Refit o `HttpClientFactory` |
| Logging | Serilog |
| Autenticación | Basic Auth (`[BasicAuthorize]`) |
| Configuración | `appsettings.json` + interfaces `ISettings` |

## Arquitectura Interna (Clean Architecture)

El proyecto sigue Clean Architecture con cuatro proyectos y una capa de tests:

```
src/
├── Cotizador.API              # Controllers
├── Cotizador.Application      # Casos de uso, motor de cálculo
├── Cotizador.Domain           # Entidades, value objects, reglas de negocio
├── Cotizador.Infrastructure
│   ├── Persistence/           # Repositorios MongoDB
│   └── ExternalServices/      # Cliente HTTP para core-ohs
└── Cotizador.Tests      # Tests unitarios e integración
```

### Responsabilidades por capa

| Proyecto | Responsabilidad | Dependencias permitidas |
|---|---|---|
| `Cotizador.Domain` | Entidades, value objects, reglas de dominio puras | Ninguna (núcleo sin dependencias externas) |
| `Cotizador.Application` | Casos de uso, motor de cálculo, interfaces de puertos (repositorios y servicios externos) | `Cotizador.Domain` |
| `Cotizador.Infrastructure/Persistence` | Repositorios MongoDB; implementan interfaces definidas en Application | `Cotizador.Application`, `Cotizador.Domain` |
| `Cotizador.Infrastructure/ExternalServices` | Cliente HTTP tipado para `core-ohs`; implementa interfaces definidas en Application | `Cotizador.Application`, `Cotizador.Domain` |
| `Cotizador.API` | Controllers; parseo HTTP, validación de entrada, delegación a Application | `Cotizador.Application` |
| `Cotizador.Tests` | Tests unitarios e integración de todas las capas | Todos los proyectos |

### Regla de dependencias

```
Cotizador.API → Cotizador.Application → Cotizador.Domain
                      ↑
       Cotizador.Infrastructure (Persistence + ExternalServices)
```

- `Domain` no referencia ningún otro proyecto del solution.
- `Application` solo referencia `Domain`; define interfaces (`IRepository`, `ICoreOhsClient`) que Infrastructure implementa.
- `Infrastructure` referencia `Application` y `Domain`; nunca es referenciada por `API` directamente.
- `API` solo referencia `Application` (nunca `Infrastructure` ni `Domain` directamente).

## Wiring de Dependencias (patrón obligatorio en `Program.cs`)

Todo el registro de dependencias vive en `Program.cs` (Composition Root). Seguir siempre el patrón:

```csharp
// ✅ Correcto — registrar interfaz → implementación con AddScoped
builder.Services.AddScoped<IQuoteUseCase, QuoteUseCase>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

// ✅ Correcto — cliente HTTP tipado para core-ohs
builder.Services.AddHttpClient<ICoreOhsClient, CoreOhsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CoreOhs:BaseUrl"]!);
});

// ✅ Correcto — constructor injection en Controller / Minimal API handler
public QuoteController(IQuoteUseCase quoteUseCase)
{
    _quoteUseCase = quoteUseCase;
}

// ✅ Correcto — constructor injection en UseCase (Application)
public QuoteUseCase(IQuoteRepository repository, ICoreOhsClient coreOhsClient, ILogger<QuoteUseCase> logger)
{
    _repository = repository;
    _coreOhsClient = coreOhsClient;
    _logger = logger;
}
```

- Use Cases, repositorios e interfaces de servicios externos → `AddScoped`.
- Clientes HTTP (`HttpClient`) → `AddHttpClient<TClient, TImpl>()`.
- Clientes MongoDB (`IMongoClient`) y configuraciones (`ISettings`) → `AddSingleton`.

NUNCA instanciar repositorios, servicios externos o Use Cases con `new` fuera de `Program.cs`.
NUNCA resolver dependencias manualmente con `ServiceLocator` o `GetService<>()` dentro de clases de negocio.

## Convenciones de Código

- Clases y métodos públicos en `PascalCase`.
- Campos privados en `_camelCase`.
- Interfaces con prefijo `I` (ej. `IQuoteUseCase`, `IQuoteRepository`, `ICoreOhsClient`).
- Use Cases con sufijo `UseCase` (ej. `CalculateQuoteUseCase`).
- Repositorios con sufijo `Repository` (ej. `QuoteRepository`); ubicados en `Infrastructure/Persistence/`.
- Clientes externos con sufijo `Client` (ej. `CoreOhsClient`); ubicados en `Infrastructure/ExternalServices/`.
- Entidades y value objects en `Domain/`; sin sufijos adicionales (ej. `Quote`, `Premium`, `Coverage`).
- Controllers con sufijo `Controller` y ruta base `api/v1/[controller]`.
- Todas las operaciones de MongoDB y HTTP externas son `async/await` (retornan `Task` o `Task<T>`).
- Configuración accedida siempre a través de `ISettings` inyectado por constructor.
- Logging estructurado con Serilog usando las constantes de operación definidas en cada clase.

## Cómo agregar un nuevo endpoint

1. **Domain**: si aplica, crear la entidad o value object en `Cotizador.Domain/`.
2. **Application**: crear la interfaz del Use Case en `Cotizador.Application/Interfaces/` e implementarla en `Cotizador.Application/UseCases/`. Si necesita datos, definir la interfaz del repositorio en `Cotizador.Application/Ports/`.
3. **Infrastructure**: implementar la interfaz del repositorio en `Cotizador.Infrastructure/Persistence/` o el cliente externo en `Cotizador.Infrastructure/ExternalServices/` según corresponda.
4. **Wiring**: registrar interfaz → implementación en `Program.cs` con `AddScoped` (o `AddHttpClient` para clientes externos).
5. **API**: crear o actualizar el Controller en `Cotizador.API/Controllers/` inyectando el Use Case por constructor.

> Ver `README.md` para la estructura de carpetas específica del proyecto.

## Nunca hacer

- Lógica de negocio en los Controllers.
- Lógica de dominio en Application (va en `Cotizador.Domain`).
- Acceso a MongoDB fuera de `Infrastructure/Persistence/`.
- Llamadas HTTP a `core-ohs` fuera de `Infrastructure/ExternalServices/`.
- Referenciar `Cotizador.Infrastructure` directamente desde `Cotizador.API`.
- Instanciar repositorios, clientes o Use Cases con `new` dentro de Controllers o Use Cases.
- Operaciones MongoDB o HTTP síncronas (siempre `await`).
- Registrar dependencias fuera de `Program.cs`.

---

> Para estándares de código limpio, SOLID, nombrado, API REST, seguridad y observabilidad, ver `.github/docs/lineamientos/dev-guidelines.md`.
