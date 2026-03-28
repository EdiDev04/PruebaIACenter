---
description: Reglas de backend para este proyecto (ASP.NET Core 8 + MongoDB.Driver + Clean Architecture). Se aplica automáticamente a archivos backend.
paths:
  - "cotizador-backend/**"
  - "src/**"
---

# Reglas de Backend — ASP.NET Core 8 + C# + Clean Architecture

## Stack aprobado

| Capa | Tecnología |
|------|------------|
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

**Prohibido:** PyMongo, FastAPI, Django, Flask, SQLAlchemy, bases de datos relacionales, acceso síncrono a MongoDB o HTTP externo.

## Arquitectura Interna (Clean Architecture)

```
src/
├── Cotizador.API              # Controllers
├── Cotizador.Application      # Casos de uso, motor de cálculo
├── Cotizador.Domain           # Entidades, value objects, reglas de negocio
├── Cotizador.Infrastructure
│   ├── Persistence/           # Repositorios MongoDB
│   └── ExternalServices/      # Cliente HTTP para core-ohs
└── Cotizador.Tests            # Tests unitarios e integración
```

### Responsabilidades por capa

| Proyecto | Responsabilidad | Dependencias permitidas |
|----------|----------------|------------------------|
| `Cotizador.Domain` | Entidades, value objects, reglas de dominio puras | Ninguna |
| `Cotizador.Application` | Casos de uso, motor de cálculo, interfaces de puertos | `Cotizador.Domain` |
| `Cotizador.Infrastructure/Persistence` | Repositorios MongoDB; implementan interfaces de Application | `Cotizador.Application`, `Cotizador.Domain` |
| `Cotizador.Infrastructure/ExternalServices` | Cliente HTTP tipado para `core-ohs` | `Cotizador.Application`, `Cotizador.Domain` |
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
- `Infrastructure` referencia `Application` y `Domain`; NUNCA es referenciada por `API`.
- `API` solo referencia `Application` (nunca `Infrastructure` ni `Domain` directamente).

## Wiring de Dependencias (patrón obligatorio en `Program.cs`)

```csharp
// ✅ Correcto — registrar interfaz → implementación con AddScoped
builder.Services.AddScoped<IQuoteUseCase, QuoteUseCase>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

// ✅ Correcto — cliente HTTP tipado para core-ohs
builder.Services.AddHttpClient<ICoreOhsClient, CoreOhsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["CoreOhs:BaseUrl"]!);
});

// ✅ Correcto — constructor injection en Controller
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

- Use Cases, repositorios e interfaces de servicios externos → `AddScoped`
- Clientes HTTP → `AddHttpClient<TClient, TImpl>()`
- Clientes MongoDB (`IMongoClient`) y configuraciones (`ISettings`) → `AddSingleton`

NUNCA instanciar repositorios, servicios externos o Use Cases con `new` fuera de `Program.cs`.
NUNCA resolver dependencias manualmente con `ServiceLocator` o `GetService<>()` dentro de clases de negocio.

## Convenciones de Código

- Clases y métodos públicos en `PascalCase`
- Campos privados en `_camelCase`
- Interfaces con prefijo `I` (ej. `IQuoteUseCase`, `IQuoteRepository`, `ICoreOhsClient`)
- Use Cases con sufijo `UseCase` (ej. `CalculateQuoteUseCase`)
- Repositorios con sufijo `Repository` en `Infrastructure/Persistence/`
- Clientes externos con sufijo `Client` en `Infrastructure/ExternalServices/`
- Entidades y value objects en `Domain/` sin sufijos (ej. `Quote`, `Premium`, `Coverage`)
- Controllers con sufijo `Controller` y ruta base `api/v1/[controller]`
- Todas las operaciones de MongoDB y HTTP externas son `async/await`
- Configuración siempre a través de `ISettings` inyectado por constructor
- Logging estructurado con Serilog

## Cómo agregar un nuevo endpoint

1. **Domain**: crear la entidad o value object en `Cotizador.Domain/`
2. **Application**: crear la interfaz del Use Case en `Cotizador.Application/Interfaces/` e implementarla en `Cotizador.Application/UseCases/`; si necesita datos, definir la interfaz del repositorio en `Cotizador.Application/Ports/`
3. **Infrastructure**: implementar la interfaz del repositorio en `Cotizador.Infrastructure/Persistence/` o el cliente externo en `Cotizador.Infrastructure/ExternalServices/`
4. **Wiring**: registrar interfaz → implementación en `Program.cs`
5. **API**: crear o actualizar el Controller en `Cotizador.API/Controllers/` inyectando el Use Case por constructor

## Control de Excepciones

### Excepciones de dominio (`Cotizador.Domain`)

| Excepción | Código HTTP |
|-----------|------------|
| `FolioNotFoundException` | 404 |
| `UbicacionIncompletaException` | alerta, no error |
| `VersionConflictException` | 409 |
| `CoreOhsUnavailableException` | 503 |

### Dónde se capturan

- NUNCA en Use Cases ni Repositories — solo lanzan
- NUNCA en Controllers directamente
- SIEMPRE en el middleware global: `ExceptionHandlingMiddleware` en `Cotizador.API`

### Formato de respuesta de error (obligatorio)

```json
{
  "type": "string",
  "message": "string",
  "field": "string|null"
}
```

### Principios

- Los errores son ciudadanos de primera clase. Nunca silenciar un error con un `catch` vacío.
- Siempre distinguir entre error recuperable (reintentar) y error terminal (mostrar pantalla de error).
- Nunca exponer stack traces ni errores internos en respuestas públicas.

## Anti-patrones Prohibidos

- Lógica de negocio en Controllers
- Lógica de dominio en Application (va en `Cotizador.Domain`)
- Acceso a MongoDB fuera de `Infrastructure/Persistence/`
- Llamadas HTTP a `core-ohs` fuera de `Infrastructure/ExternalServices/`
- Referenciar `Cotizador.Infrastructure` directamente desde `Cotizador.API`
- Instanciar repositorios, clientes o Use Cases con `new` dentro de Controllers o Use Cases
- Operaciones MongoDB o HTTP síncronas
- Registrar dependencias fuera de `Program.cs`
- Credenciales hardcodeadas en código

## Lineamientos completos

`.claude/docs/lineamientos/dev-guidelines.md` — Clean Code, SOLID, API REST, Seguridad, Observabilidad.
