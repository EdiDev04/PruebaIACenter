# Decisiones de Arquitectura y Stack Tecnológico — Cotizador

> Documento de referencia único. Toda decisión de stack y arquitectura del proyecto vive aquí.
> En caso de conflicto con otro archivo, este documento prevalece.

---

## 1. Visión General

| Componente | Proyecto | Lenguaje / Runtime |
|---|---|---|
| Backend API | `cotizador-backend` | C# / .NET 8 |
| Frontend SPA | `cotizador-webapp` | TypeScript / Node |
| Core mock externo | `cotizador-core-mock` | Node.js + Express + TypeScript|
| Automatización | `cotizador-automatization` | — |

---

## 2. Backend — Stack Técnico

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

---

## 3. Backend — Arquitectura (Clean Architecture)

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
|---|---|---|
| `Cotizador.Domain` | Entidades, value objects, reglas de dominio puras | Ninguna |
| `Cotizador.Application` | Casos de uso, motor de cálculo, interfaces de puertos | `Cotizador.Domain` |
| `Cotizador.Infrastructure/Persistence` | Repositorios MongoDB | `Cotizador.Application`, `Cotizador.Domain` |
| `Cotizador.Infrastructure/ExternalServices` | Cliente HTTP tipado para `core-ohs` | `Cotizador.Application`, `Cotizador.Domain` |
| `Cotizador.API` | Controllers; parseo HTTP y delegación a Application | `Cotizador.Application` |
| `Cotizador.Tests` | Tests unitarios e integración | Todos |

### Regla de dependencias

```
Cotizador.API → Cotizador.Application → Cotizador.Domain
                        ↑
         Cotizador.Infrastructure (Persistence + ExternalServices)
```

- `Domain` no referencia ningún otro proyecto.
- `Application` solo referencia `Domain`; define interfaces (`IRepository`, `ICoreOhsClient`).
- `Infrastructure` implementa dichas interfaces; nunca es referenciada por `API` directamente.
- `API` solo referencia `Application`.

### Convenciones de naming BE

| Artefacto | Convención | Ejemplo |
|---|---|---|
| Clases / métodos públicos | `PascalCase` | `CalculateQuoteUseCase` |
| Campos privados | `_camelCase` | `_repository` |
| Interfaces | Prefijo `I` | `IQuoteRepository` |
| Use Cases | Sufijo `UseCase` | `CalculateQuoteUseCase` |
| Repositorios | Sufijo `Repository` | `QuoteRepository` |
| Clientes externos | Sufijo `Client` | `CoreOhsClient` |
| Controllers | Sufijo `Controller`, ruta `api/v1/[controller]` | `QuoteController` |
| Entidades / value objects | Sin sufijo | `Quote`, `Premium` |

### Wiring (Composition Root — `Program.cs`)

- Use Cases y repositorios → `AddScoped`.
- Clientes HTTP → `AddHttpClient<TClient, TImpl>()`.
- `IMongoClient` e `ISettings` → `AddSingleton`.
- NUNCA `new` fuera de `Program.cs`. NUNCA `ServiceLocator` / `GetService<>()` en clases de negocio.

---

## 4. Frontend — Stack Técnico

| Capa | Tecnología |
|---|---|
| Framework | React 18 + TypeScript |
| UI State | Redux Toolkit |
| Server State | TanStack Query |
| Formularios | React Hook Form + Zod |
| Arquitectura | FSD (Feature-Sliced Design) |
| Rutas | React Router v6 |
| Estilos | CSS Modules |
| HTTP | `fetch` nativo — cliente en `shared/api/` |
| Tests | Vitest + Testing Library |
| Build | Vite |

---

## 5. Frontend — Arquitectura (FSD)

```
src/
├── app/         # Providers, router, store, estilos globales
├── pages/       # Componentes de ruta (ensamblado de widgets/features)
├── widgets/     # Bloques UI compuestos reutilizables entre páginas
├── features/    # Interacciones de usuario (wizard, búsqueda de folio)
├── entities/    # Entidades de negocio (folio, catálogos, cotización)
└── shared/      # UI kit, utilidades, cliente HTTP, tipos base
    ├── api/     # Cliente HTTP y helpers de fetch
    ├── ui/      # Componentes primitivos
    ├── lib/     # Utilidades puras
    └── types/   # Tipos y enums compartidos
```

### Regla de dependencias FSD

```
app → pages → widgets → features → entities → shared
```

- Nunca importar una capa superior desde una capa inferior.
- Cada slice expone solo su `index.ts` (public API). No importar rutas internas.
- Si dos features comparten algo, moverlo a `entities/` o `shared/`.

### Separación de estado FE

| Tipo de estado | Herramienta | Ejemplos |
|---|---|---|
| Server state | TanStack Query | Folio, catálogos, resultado de cálculo |
| UI state transversal | Redux Toolkit | Paso del wizard, alertas globales |
| UI state local | `useState` / `useReducer` | Estado de un campo, toggle |
| Formularios | React Hook Form + Zod | Wizard de cotización |

- NUNCA poner server state en Redux.
- NUNCA mezclar TanStack Query y Redux para el mismo dato.

### Convenciones de naming FE

| Artefacto | Convención | Ejemplo |
|---|---|---|
| Componentes | `PascalCase` sin sufijo | `QuoteWizard.tsx` |
| Hooks | Prefijo `use`, `camelCase` | `useQuoteForm.ts` |
| Slices Redux | Sufijo `Slice` | `quoteWizardSlice.ts` |
| Schemas Zod | Sufijo `Schema` | `quoteSchema.ts` |
| API helpers | `camelCase` | `folioApi.ts` |
| Variables de entorno | `VITE_*` en `import.meta.env` | `VITE_API_URL` |

- `strict: true` en `tsconfig.json`. NUNCA usar `any`.
- NUNCA hardcodear URLs.
- NUNCA estilos inline ni CSS globales (salvo reset/variables en `app/styles/`).

---

## 6. Restricciones Globales

| Restricción | Aplica a |
|---|---|
| No lógica de negocio en Controllers ni en componentes React | BE + FE |
| No acceso a MongoDB fuera de `Infrastructure/Persistence/` | BE |
| No llamadas HTTP a `core-ohs` fuera de `Infrastructure/ExternalServices/` | BE |
| No referenciar `Infrastructure` desde `API` | BE |
| No operaciones síncronas a MongoDB | BE |
| No importar slices del mismo nivel FSD entre sí | FE |
| No importar rutas internas de un slice (solo `index.ts`) | FE |

---

> Para lineamientos de código limpio, SOLID, seguridad y observabilidad: `.github/docs/lineamientos/dev-guidelines.md`
> Para instrucciones detalladas por capa: `.github/instructions/backend.instructions.md` · `.github/instructions/frontend.instructions.md`
