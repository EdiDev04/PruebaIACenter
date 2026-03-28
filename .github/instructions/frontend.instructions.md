---
applyTo: "src/projects/**/*.{ts,html,scss}"
---

> **Scope**: Se aplica al frontend React del proyecto `cotizador-webapp`. Arquitectura FSD (Feature-Sliced Design).

# Instrucciones para Archivos de Frontend (React 18)

## Stack Tecnológico

| Capa | Tecnología |
|---|---|
| Framework | React 18 + TypeScript |
| UI State | Redux Toolkit (wizard de cotización, alertas de folio) |
| Server State | TanStack Query (folio, catálogos, cálculo) |
| Formularios | React Hook Form + Zod (validación tipada) |
| Arquitectura | FSD (Feature-Sliced Design) |
| Rutas | React Router v6 |
| Estilos | CSS Modules por componente |
| HTTP | `fetch` nativo o cliente tipado en `shared/api/` |
| Tests | Vitest + Testing Library |
| Build | Vite |

## Arquitectura FSD

El proyecto sigue **Feature-Sliced Design**. Cada capa solo puede importar capas inferiores:

```
src/
├── app/         # Providers, router, store, estilos globales
├── pages/       # Componentes de ruta (ensamblado de widgets/features)
├── widgets/     # Bloques UI compuestos reutilizables entre páginas
├── features/    # Interacciones de usuario (wizard de cotización, búsqueda de folio)
├── entities/    # Entidades de negocio (folio, catálogos, cotización)
├── shared/      # UI kit, utilidades, cliente HTTP, tipos base
│   ├── api/     # Instancia HTTP y helpers de fetch
│   ├── ui/      # Componentes primitivos reutilizables
│   ├── lib/     # Utilidades puras (formateo, validaciones genéricas)
│   └── types/   # Tipos y enums compartidos
```

### Regla de dependencias FSD

```
app → pages → widgets → features → entities → shared
```

- **Nunca** importar una capa superior desde una capa inferior.
- Cada slice tiene su propio `index.ts` (public API). Solo importar desde el `index.ts` del slice, nunca rutas internas.
- Si dos features necesitan compartir algo, moverlo a `entities/` o `shared/`.

## Convenciones Obligatorias

- **Estilos**: SIEMPRE CSS Modules (`.module.css` o `.module.scss`) por componente. NUNCA estilos inline ni clases CSS globales salvo reset/variables en `app/styles/`.
- **Nombres de archivos**: `PascalCase` para componentes (`QuoteWizard.tsx`), `camelCase` para hooks, servicios y utilidades (`useQuoteForm.ts`, `folioApi.ts`).
- **Sufijos**: componentes sin sufijo (`QuoteWizard.tsx`), hooks con prefijo `use` (`useQuoteForm.ts`), stores con sufijo `Slice` (`quoteSlice.ts`), schemas Zod con sufijo `Schema` (`quoteSchema.ts`).
- **Exports**: cada carpeta expone un `index.ts`; no importar archivos internos del slice directamente.
- **Tipado estricto**: `strict: true` en `tsconfig.json`. NUNCA usar `any`; preferir tipos inferidos de Zod o TanStack Query.
- **Variables de entorno**: leer siempre desde `import.meta.env.VITE_*`. NUNCA hardcodear URLs.

## Server State — TanStack Query

Usar TanStack Query para **todo** estado que venga del servidor (folio, catálogos, resultado de cálculo).

```typescript
// entities/folio/api/folioApi.ts
export const getFolio = async (id: string): Promise<Folio> => {
  const res = await fetch(`${import.meta.env.VITE_API_URL}/folios/${id}`);
  if (!res.ok) throw new Error('Error al obtener folio');
  return res.json();
};

// entities/folio/model/useFolioQuery.ts
import { useQuery } from '@tanstack/react-query';
import { getFolio } from '../api/folioApi';

export const useFolioQuery = (id: string) =>
  useQuery({ queryKey: ['folio', id], queryFn: () => getFolio(id), enabled: !!id });
```

- Keys de query: array tipado `['entidad', params]`.
- Mutaciones con `useMutation`; invalidar queries relacionadas en `onSuccess`.
- NUNCA mezclar TanStack Query con Redux para el mismo dato del servidor.

## UI State — Redux Toolkit

Usar Redux Toolkit para **estado de UI** que debe persistir entre pasos o componentes no relacionados jerárquicamente: pasos del wizard, alertas globales de folio, selecciones transversales.

```typescript
// features/quote-wizard/model/quoteWizardSlice.ts
import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface QuoteWizardState { currentStep: number; }
const initialState: QuoteWizardState = { currentStep: 0 };

export const quoteWizardSlice = createSlice({
  name: 'quoteWizard',
  initialState,
  reducers: {
    nextStep: (state) => { state.currentStep += 1; },
    goToStep: (state, action: PayloadAction<number>) => { state.currentStep = action.payload; },
  },
});

export const { nextStep, goToStep } = quoteWizardSlice.actions;
```

- Store configurado en `app/store/`. Un slice por feature de UI.
- Acceder al store SIEMPRE con typed hooks (`useAppSelector`, `useAppDispatch`) definidos en `app/store/hooks.ts`.
- NUNCA poner estado del servidor en Redux; para eso está TanStack Query.

## Formularios — React Hook Form + Zod

Usar React Hook Form + Zod para **todos** los formularios. El schema Zod es la fuente de verdad de la validación.

```typescript
// features/quote-form/model/quoteSchema.ts
import { z } from 'zod';

export const quoteSchema = z.object({
  locationId: z.string().min(1, 'Selecciona una ubicación'),
  coverageType: z.enum(['basic', 'full']),
  insuredValue: z.number().positive('El valor debe ser mayor a 0'),
});

export type QuoteFormData = z.infer<typeof quoteSchema>;

// features/quote-form/ui/QuoteForm.tsx
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';

const { register, handleSubmit, formState: { errors } } = useForm<QuoteFormData>({
  resolver: zodResolver(quoteSchema),
});
```

- Un archivo `<feature>Schema.ts` por formulario.
- Inferir siempre el tipo del formulario desde `z.infer<typeof schema>`.
- NUNCA validar campos manualmente con `if`; toda la lógica de validación vive en el schema Zod.

## Rutas — React Router v6

Las rutas se definen en `app/router/`. Usar `createBrowserRouter` con rutas anidadas según las rutas funcionales del reto.

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

- Páginas en `pages/`; son ensamblados, no tienen lógica propia.
- Lazy loading con `React.lazy` + `Suspense` para páginas pesadas.
- Parámetros de ruta tipados con `useParams` + aserción de tipo o validación Zod.

## Componentes

- Un componente por archivo.
- No lógica de negocio en componentes; delegar a hooks (`use<Feature>.ts`) o al store.
- Props tipadas explícitamente con `interface Props { ... }`.
- Preferir composición sobre herencia. Componentes primitivos en `shared/ui/`.
- Limpiar efectos y suscripciones en el cleanup de `useEffect`.

## Nunca hacer

- Importar entre slices del mismo nivel FSD (ej. `features/a` → `features/b`); usar `entities/` o `shared/`.
- Importar rutas internas de un slice (solo desde su `index.ts`).
- Estado del servidor en Redux.
- Estado de UI propio del wizard en TanStack Query.
- Validaciones manuales fuera de Zod en formularios.
- URLs hardcodeadas; siempre `import.meta.env.VITE_*`.
- Usar `any` en TypeScript.

---

> Para estándares de código limpio, naming, API REST y seguridad, ver `.github/docs/lineamientos/dev-guidelines.md`.
