---
description: Reglas de stack y arquitectura frontend. Se aplica automáticamente a cualquier archivo en cotizador-webapp/**
paths: 
  - "cotizador-webapp/**"
---

# Stack frontend — Cotizador

## Tecnologías aprobadas

| Capa | Tecnología |
|------|------------|
| Framework | React 18 + TypeScript (`strict: true`) |
| UI state | Redux Toolkit |
| Server state | TanStack Query |
| Formularios | React Hook Form + Zod |
| Arquitectura | FSD (Feature-Sliced Design) |
| Rutas | React Router v6 |
| Estilos | CSS Modules |
| HTTP | `fetch` nativo — cliente en `shared/api/` |
| Tests | Vitest + Testing Library |
| Build | Vite |

## Arquitectura FSD — regla de dependencias

```
app → pages → widgets → features → entities → shared
```

- Nunca importar una capa superior desde una inferior
- Cada slice expone solo su `index.ts` — nunca importar rutas internas
- Si dos features comparten algo → moverlo a `entities/` o `shared/`

## Separación de estado (obligatoria)

| Tipo | Herramienta |
|------|------------|
| Server state | TanStack Query — folio, catálogos, resultado cálculo |
| UI state transversal | Redux Toolkit — paso wizard, alertas globales |
| UI state local | `useState` / `useReducer` |
| Formularios | React Hook Form + Zod |

- NUNCA poner server state en Redux
- NUNCA mezclar TanStack Query y Redux para el mismo dato

## Convenciones de naming

| Artefacto | Convención | Ejemplo |
|-----------|------------|---------|
| Componentes | PascalCase | `LocationForm.tsx` |
| Hooks | prefijo `use` | `useLocationForm.ts` |
| Slices Redux | sufijo `Slice` | `quoteWizardSlice.ts` |
| Schemas Zod | sufijo `Schema` | `locationSchema.ts` |
| API helpers | camelCase | `folioApi.ts` |
| Env vars | `VITE_*` | `import.meta.env.VITE_API_URL` |

## Restricciones globales

- `strict: true` en `tsconfig.json` — NUNCA usar `any`
- NUNCA hardcodear URLs — siempre desde `import.meta.env`
- NUNCA estilos inline ni CSS globales (salvo reset en `app/styles/`)
- NUNCA lógica de negocio en componentes ni en páginas

## Rutas — React Router v6

Definir rutas en `app/router/` con `createBrowserRouter` y rutas anidadas:

```typescript
// app/router/router.tsx
import { createBrowserRouter } from 'react-router-dom';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'cotizar', element: <QuotePage /> },
      { path: 'folio/:id', element: <FolioPage /> },
    ],
  },
]);
```

- Páginas en `pages/` — son ensamblados, sin lógica propia
- Lazy loading con `React.lazy` + `Suspense` para páginas pesadas
- Parámetros de ruta tipados con `useParams` + aserción de tipo o validación Zod

## Componentes

- Un componente por archivo
- NUNCA lógica de negocio en componentes — delegar a hooks (`use<Feature>.ts`) o al store
- Props tipadas explícitamente con `interface Props { ... }`
- Preferir composición sobre herencia
- Componentes primitivos reutilizables en `shared/ui/`
- Limpiar efectos y suscripciones en el cleanup de `useEffect`

---

# Control de excepciones — estrategia por capa FSD

## Principio base

Los errores son ciudadanos de primera clase. Cada capa tiene una responsabilidad
distinta: detectar, transformar, propagar o presentar. Nunca silenciar un error
con un `catch` vacío. Nunca usar `null` como señal de error.

---

## Capa `shared/api/` — cliente HTTP

Responsabilidad: detectar errores de red y HTTP, transformarlos en tipos propios.

### Tipos de error (definir en `shared/api/errors.ts`)

```ts
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly message: string,
    public readonly field?: string
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export class CoreOhsUnavailableError extends ApiError {}
export class FolioNotFoundError extends ApiError {}
export class VersionConflictError extends ApiError {}
```

### Comportamiento obligatorio del cliente fetch

```ts
// CORRECTO — lanza siempre, nunca retorna null
async function apiFetch<T>(url: string, options?: RequestInit): Promise<T> {
  const res = await fetch(url, options);
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    if (res.status === 401) { redirectToLogin(); throw new ApiError(401, 'No autorizado'); }
    if (res.status === 404) throw new FolioNotFoundError(404, body.message ?? 'No encontrado');
    if (res.status === 409) throw new VersionConflictError(409, body.message ?? 'Conflicto de versión');
    if (res.status === 503) throw new CoreOhsUnavailableError(503, 'Servicio de referencia no disponible');
    throw new ApiError(res.status, body.message ?? 'Error inesperado', body.field);
  }
  return res.json();
}

// INCORRECTO — nunca hacer esto
const data = await fetch(url).then(r => r.json()).catch(() => null);
```

### Mapeo HTTP → comportamiento

| Status | Tipo de error | Comportamiento |
|--------|--------------|----------------|
| 400 | `ApiError` con `field` | Mapear al campo del formulario con `setError()` |
| 401 | `ApiError` | Redirigir a `/cotizador` |
| 404 | `FolioNotFoundError` | Mostrar pantalla de folio no encontrado |
| 409 | `VersionConflictError` | Mostrar alerta: "Folio modificado por otro proceso, recarga" |
| 503 | `CoreOhsUnavailableError` | Mostrar alerta global: "Servicio no disponible, intenta más tarde" |
| 5xx | `ApiError` | Mostrar alerta global genérica |

---

## Capa `entities/` y `features/` — TanStack Query

Responsabilidad: propagar errores del servidor al estado global o al formulario.

### useQuery — errores recuperables

```ts
// CORRECTO
const { data, error, isError } = useQuery({
  queryKey: ['folio', folio],
  queryFn: () => folioApi.get(folio),
  retry: 1,                          // un reintento para queries
  throwOnError: false,               // manejar en el componente con isError
});

// Si isError → dispatch(setGlobalAlert({ type: 'error', message: error.message }))
```

### useMutation — errores de negocio y validación

```ts
// CORRECTO
const mutation = useMutation({
  mutationFn: folioApi.updateGeneralInfo,
  retry: 0,                          // NUNCA reintentar mutaciones
  onError: (error) => {
    if (error instanceof ApiError && error.status === 400 && error.field) {
      // Error de validación → al formulario, no al estado global
      form.setError(error.field, { message: error.message });
      return;
    }
    // Cualquier otro error → alerta global
    dispatch(setGlobalAlert({ type: 'error', message: error.message }));
  },
});

// INCORRECTO — nunca swallowear en onError
onError: () => console.log('falló')
```

### Reglas para TanStack Query

- `retry: 0` en todas las mutaciones — sin excepción
- `retry: 1` en queries de datos críticos (folio, catálogos)
- NUNCA mostrar `error.message` crudo al usuario — usar mensajes del catálogo de errores
- NUNCA usar `onError` para lógica de negocio — solo para presentar errores

---

## Capa `features/` — React Hook Form + Zod

Responsabilidad: manejar errores de validación local y mapear errores 400 del backend.

### Validación Zod — nunca con try/catch

```ts
// CORRECTO — Zod integrado con RHF, errores en formState.errors
const form = useForm<LocationFormValues>({
  resolver: zodResolver(locationSchema),
});
// formState.errors.codigoPostal.message se muestra en el campo

// INCORRECTO — nunca parsear manualmente con try/catch en el submit
const onSubmit = async (data) => {
  try {
    locationSchema.parse(data); // ← innecesario, RHF ya lo hace
  } catch (e) { ... }
};
```

### Mapeo de errores 400 backend → campos del formulario

```ts
// CORRECTO — mapear field a field desde el error de API
const onSubmit = async (data: LocationFormValues) => {
  try {
    await mutation.mutateAsync(data);
  } catch (error) {
    if (error instanceof ApiError && error.status === 400 && error.field) {
      form.setError(error.field as keyof LocationFormValues, {
        type: 'server',
        message: error.message,
      });
    }
    // Los demás errores los maneja onError de useMutation
  }
};

// INCORRECTO — nunca mezclar errores de red con errores de validación
catch (error) {
  form.setError('root', { message: 'Algo salió mal' }); // ← demasiado genérico
}
```

### Reglas para formularios

- Errores de Zod → `formState.errors` — nunca capturar manualmente
- Errores 400 con `field` → `setError(field)` en el campo correspondiente
- Errores 400 sin `field` → `setError('root')` para mostrar bajo el formulario
- Errores de red (4xx sin field, 5xx) → dejar que `onError` de useMutation los maneje
- NUNCA mezclar errores de validación con errores de red en el mismo handler

---

## Capa `app/` — Error Boundaries

Responsabilidad: capturar errores de render no manejados antes de que colapsen la UI.

### Estructura obligatoria

```tsx
// app/providers/AppProviders.tsx — ErrorBoundary global
<GlobalErrorBoundary fallback={<GlobalErrorPage />}>
  <QueryClientProvider client={queryClient}>
    <Provider store={store}>
      <RouterProvider router={router} />
    </Provider>
  </QueryClientProvider>
</GlobalErrorBoundary>

// pages/<Page>/index.tsx — ErrorBoundary por página
<PageErrorBoundary fallback={<PageErrorFallback />}>
  <LocationsPage />
</PageErrorBoundary>
```

### Reglas para Error Boundaries

- ErrorBoundary global en `app/` — captura errores terminales que ninguna capa manejó
- ErrorBoundary por página — aísla fallos para que una página rota no tumbe el wizard completo
- NUNCA dejar un error de render burbujear hasta el root sin ErrorBoundary intermedio
- Los errores capturados por ErrorBoundary deben loggearse (Sentry u observabilidad configurada)
- NUNCA usar ErrorBoundary para manejar errores async — esos van en onError de TanStack Query

---

## Resumen: quién maneja qué

| Tipo de error | Dónde se maneja | Cómo se presenta |
|---------------|----------------|------------------|
| Validación Zod | RHF `formState.errors` | Mensaje bajo el campo |
| 400 con `field` | `form.setError(field)` | Mensaje bajo el campo |
| 400 sin `field` | `form.setError('root')` | Mensaje bajo el formulario |
| 401 | `shared/api/` | Redirect a `/cotizador` |
| 404 folio | TanStack Query `onError` | Página de folio no encontrado |
| 409 versión | TanStack Query `onError` | Alerta global con instrucción de recarga |
| 503 core-ohs | TanStack Query `onError` | Alerta global no bloqueante |
| 5xx genérico | TanStack Query `onError` | Alerta global genérica |
| Error de render | ErrorBoundary | Fallback UI por página |

## Nunca hacer

- Importar entre slices del mismo nivel FSD (`features/a` → `features/b`); usar `entities/` o `shared/`
- Importar rutas internas de un slice (solo desde su `index.ts`)
- Estado del servidor en Redux
- Estado de UI del wizard en TanStack Query
- Validaciones manuales fuera de Zod en formularios
- URLs hardcodeadas; siempre `import.meta.env.VITE_*`
- Usar `any` en TypeScript
- `catch` vacío — siempre distinguir entre error recuperable y error terminal

## Lineamientos completos

`.claude/docs/lineamientos/dev-guidelines.md` — Clean Code, SOLID, API REST, Seguridad, Observabilidad.
