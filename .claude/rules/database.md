---
description: Reglas de acceso a datos para este proyecto (MongoDB + MongoDB.Driver + C#). Se aplica automáticamente a archivos de modelos y repositorios.
paths:
  - "**/models/**"
  - "**/repositories/**"
  - "**/entities/**"
  - "**/schemas/**"
  - "**/migrations/**"
---

# Reglas de Base de Datos — MongoDB + MongoDB.Driver (C#)

## Stack aprobado

- **MongoDB** — base de datos principal (única, sin persistencia relacional)
- **`MongoDB.Driver`** oficial — ÚNICO cliente aprobado para C#/.NET
- **Operaciones async** — todas las queries usan `async Task` + `await`

**Prohibido:** PyMongo, Motor async, SQLAlchemy, Django ORM, bases de datos relacionales (PostgreSQL, MySQL, SQLite), acceso síncrono a MongoDB.

## Convenciones de MongoDB

- Colecciones en snake_case plural: `quotes`, `locations`, `coverages`
- IDs expuestos en API: string (no exponer `_id` de MongoDB en respuestas API)
- Timestamps: `CreatedAt` / `UpdatedAt` generados en la app con `DateTime.UtcNow`
- Paginación via `Skip()` / `Limit()` en queries
- Acceso asíncrono exclusivamente: todas las operaciones usan `async Task` + `await`

## Separación de Modelos (obligatorio)

| Modelo | Propósito | Contiene |
|--------|-----------|----------|
| **Request / Input** | Datos que el cliente envía | Solo campos que el cliente provee |
| **Update / Patch** | Datos para actualizar | Todos los campos opcionales |
| **Response / Output** | Lo que la API retorna | Campos seguros para exponer (sin `_id`) |
| **Document / Entity** | Documento interno de MongoDB | Campos internos + timestamps |

## Reglas de Diseño

- **IDs como strings** — nunca exponer `_id` de Mongo en contratos API
- **Timestamps UTC** — `CreatedAt` / `UpdatedAt` en la app, nunca en el cliente
- **Índices justificados** — solo crear índices con un caso de uso documentado
- **Sin datos sensibles en texto plano** — nunca almacenar passwords sin hash
- **Repositorio como única puerta de acceso a MongoDB** — Use Cases no tocan `MongoDB.Driver` directamente

## Patrón de Repositorio (obligatorio)

```csharp
// Infrastructure/Persistence/QuoteRepository.cs
public class QuoteRepository : IQuoteRepository
{
    private readonly IMongoCollection<QuoteDocument> _collection;

    public QuoteRepository(IMongoClient mongoClient, ISettings settings)
    {
        var db = mongoClient.GetDatabase(settings.DatabaseName);
        _collection = db.GetCollection<QuoteDocument>("quotes");
    }

    public async Task<QuoteDocument?> GetByIdAsync(string id)
    {
        return await _collection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task InsertAsync(QuoteDocument doc)
    {
        await _collection.InsertOneAsync(doc);
    }

    public async Task<bool> UpdateAsync(string id, UpdateDefinition<QuoteDocument> update)
    {
        var result = await _collection.UpdateOneAsync(x => x.Id == id, update);
        return result.ModifiedCount > 0;
    }
}
```

## Wiring de MongoDB (en `Program.cs`)

```csharp
// ✅ Correcto — IMongoClient como Singleton
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(builder.Configuration["MongoDB:ConnectionString"]));

// ✅ Correcto — Repositorio registrado como Scoped
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
```

## Anti-patrones Prohibidos

- Acceso síncrono a MongoDB (bloquea el thread pool — prohibido siempre)
- Queries N+1 (iterar llamadas a DB en un bucle)
- Lógica de negocio en repositorios (va en `Cotizador.Application`)
- Acceso directo a `MongoDB.Driver` desde Use Cases (siempre via repositorio)
- `_id` de MongoDB en respuestas API
- Estado de conexión global mutable
- Instanciar `MongoClient` fuera de `Program.cs`
- Operaciones no awaiteadas sobre collections de Mongo
