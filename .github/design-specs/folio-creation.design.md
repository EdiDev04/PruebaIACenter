# Design Spec — folio-creation

> **Spec de referencia**: `.github/specs/folio-creation.spec.md` (SPEC-003, status: APPROVED)
> **Agente**: ux-designer · **Feature type**: full-stack
> **Pantallas cubiertas**: Home / Inicio · Confirmación de folio creado
> **Última actualización**: 2026-03-29

---

## Sección 1 — Data → UI Mapping

### Pantalla 1: Home / Pantalla de inicio

Esta pantalla no tiene campos de formulario persistidos — toda la interacción desencadena llamadas al API. El mapeo se orienta al estado transitorio necesario para la UX.

| Campo / Dato | Origen | Componente UI | Tipo de input | Razón de diseño |
|---|---|---|---|---|
| `Idempotency-Key` | Generado en frontend (crypto.randomUUID) | Ninguno visible (interno) | — | El usuario no ve ni ingresa este valor; se genera automáticamente al hacer click en "Crear folio" |
| `folioNumber` (búsqueda) | Input del usuario | `TextInput` con máscara | Texto con validación de formato | Única forma de recuperar un folio; el formato `DAN-YYYY-NNNNN` requiere validación previa al API |
| Error state (folio no encontrado) | API 404 | `Toast` (error) | — | Feedback no bloqueante; el usuario puede reintentar |
| Error state (formato inválido) | Validación local | `InlineFieldError` bajo el input | — | Feedback inmediato antes de llamar al API (evita round-trip innecesario) |
| Loading state (creando folio) | Mutación en progreso | `Button` con spinner + texto "Creando..." | — | Indica actividad y previene doble submit |
| Loading state (abriendo folio) | Query en progreso | `Button` con spinner + "Buscando..." | — | Consistencia con el estado de creación |

### Pantalla 2: Confirmación de folio creado

| Campo / Dato | Origen | Componente UI | Tipo de input | Razón de diseño |
|---|---|---|---|---|
| `folioNumber` | API response 201 | `FolioNumberBadge` (read-only) | — | Principal identificador; debe ser copiable y visible en grande |
| `quoteStatus` | API response | `StatusBadge` | — | Estado `draft` → chip ámbar "En borrador" para que el usuario sepa que el folio no está completo |
| `version` | API response | Oculto en UI | — | Persiste en Redux store para optimistic locking futuro |
| CTA "Iniciar captura" | — | `Button` (primary, full-width) | — | Navega a `/quotes/{folioNumber}/general-info`; es el único CTA relevante en este punto |
| `metadata.createdAt` | API response | `ReadOnlyField` con formato de fecha | — | Confirma cuándo se creó el folio; contexto informativo |

---

## Sección 2 — Component Inventory

### Jerarquía FSD

```
pages/
  FolioHomePage.tsx               ← ensamblado de features
    features/folio-creation/
      ui/
        CreateFolioButton.tsx     ← botón + lógica de mutación
      model/
        useCreateFolio.ts         ← useMutation + generación de Idempotency-Key
        createFolioSchema.ts      ← Zod (sin campos, solo guarda la key interna)
    features/folio-search/
      ui/
        FolioSearchInput.tsx      ← input + validación de formato
        FolioSearchButton.tsx     ← botón de búsqueda
      model/
        useFolioSearch.ts         ← useQuery lazy (enabled: cuando usuario hace submit)
        folioSearchSchema.ts      ← Zod: regex DAN-YYYY-NNNNN
    widgets/
      FolioActionCard.tsx         ← card contenedor de cada acción (crear / abrir)
  FolioCreatedPage.tsx            ← pantalla de confirmación de folio
    features/folio-creation/
      ui/
        FolioCreatedConfirmation.tsx ← muestra folioNumber + status + CTA
    entities/folio/
      ui/
        FolioNumberBadge.tsx      ← badge grande con el número de folio
        StatusBadge.tsx           ← chip de quoteStatus
      model/
        folioSlice.ts             ← (en quoteWizardSlice) almacena activeFolio + currentStep
shared/
  ui/
    Toast.tsx                     ← notificaciones de error/éxito
    Button.tsx                    ← botón base; acepta prop isLoading
    TextInput.tsx                 ← input base; acepta error + helperText
```

### Tabla de componentes

| Componente | Capa FSD | Props clave | Tipo de estado | Notas |
|---|---|---|---|---|
| `FolioHomePage` | `pages` | — | Sin estado local | Ensambla features; sin lógica propia |
| `CreateFolioButton` | `features/folio-creation` | `onSuccess(folioNumber)` | `isLoading: boolean` (local) | Llama `useCreateFolio` internamente; genera UUID antes del dispatch |
| `FolioSearchInput` | `features/folio-search` | `value`, `onChange`, `error` | Controlado por RHF | Valida formato con Zod antes de emitir submit |
| `FolioSearchButton` | `features/folio-search` | `onSubmit`, `isLoading` | Recibe desde padre | Deshabilitado si el input tiene error de formato |
| `FolioActionCard` | `widgets` | `title`, `description`, `children` | Sin estado | Card de presentación que contiene los features de acción |
| `FolioCreatedPage` | `pages` | `folioNumber`, `createdAt` | Server state (React Router loader o Redux) | Recibe datos del folio ya creado vía Router state |
| `FolioCreatedConfirmation` | `features/folio-creation` | `folioNumber`, `quoteStatus`, `onContinue()` | Sin estado local | Solo renderiza datos + CTA |
| `FolioNumberBadge` | `entities/folio` | `value: string` | Sin estado | Permite click-to-copy del folioNumber |
| `StatusBadge` | `entities/folio` | `status: QuoteStatus` | Sin estado | Mapa de colores definido en Visual Rules (§5) |
| `Toast` | `shared/ui` | `message`, `type: 'error'|'success'|'warning'` | Global (Redux o Context) | Se muestra desde cualquier feature |
| `Button` | `shared/ui` | `isLoading?`, `disabled?`, `variant: 'primary'|'secondary'|'ghost'` | Sin estado | El spinner reemplaza el texto cuando `isLoading: true` |
| `TextInput` | `shared/ui` | `label`, `value`, `onChange`, `error?`, `helperText?`, `placeholder?` | Controlado | Muestra `InlineFieldError` cuando `error` está definido |

### Zod Schemas

```typescript
// features/folio-search/model/folioSearchSchema.ts
import { z } from 'zod';

export const folioSearchSchema = z.object({
  folioNumber: z
    .string()
    .min(1, 'El número de folio es obligatorio')
    .regex(/^DAN-\d{4}-\d{5}$/, 'Formato inválido. Use DAN-YYYY-NNNNN (ej: DAN-2026-00001)'),
});

export type FolioSearchFormData = z.infer<typeof folioSearchSchema>;
```

```typescript
// features/folio-creation/model/createFolioSchema.ts
import { z } from 'zod';

// El schema no tiene campos visibles; se usa para tipar el estado del wizard post-creación
export const createdFolioSchema = z.object({
  folioNumber: z.string().regex(/^DAN-\d{4}-\d{5}$/),
  quoteStatus: z.enum(['draft', 'in_progress', 'calculated', 'closed']),
  version: z.number().int().positive(),
  metadata: z.object({
    createdAt: z.string().datetime(),
    lastWizardStep: z.number().int().min(0),
  }),
});

export type CreatedFolio = z.infer<typeof createdFolioSchema>;
```

---

## Sección 3 — Interaction Flows

### Flow 1: Crear folio nuevo (Happy path)

```
Usuario click "Crear folio nuevo"
  │
  ├─ [Frontend] generar idempotencyKey = crypto.randomUUID()
  ├─ [Frontend] setLoading(true) en CreateFolioButton
  ├─ [API] POST /v1/folios
  │         Header: Idempotency-Key: <uuid>
  │         Header: Authorization: Basic <base64>
  │
  ├─ [API Response 201 Created]
  │    body: { data: { folioNumber, quoteStatus: "draft", version: 1, metadata: {...} } }
  │    ├─ [Redux] dispatch(initWizard({ activeFolio: folioNumber, currentStep: 1 }))
  │    ├─ [Redux] dispatch(setFolioVersion(version))
  │    └─ [Router] navigate("/quotes/{folioNumber}/general-info", { state: { fromCreation: true } })
  │         → FolioCreatedPage se renderiza con el número y CTA
  │
  └─ [API Response 200 OK] (folio ya existía para ese Idempotency-Key)
       body: { data: { folioNumber, quoteStatus, version, metadata: { lastWizardStep } } }
       ├─ [Redux] dispatch(initWizard({ activeFolio: folioNumber, currentStep: metadata.lastWizardStep }))
       └─ [Router] navigate a la ruta del lastWizardStep correspondiente
```

### Flow 2: Abrir folio existente (Happy path)

```
Usuario ingresa folioNumber en FolioSearchInput
  │
  ├─ [RHF + Zod] validación inline al perder foco
  │    ├─ Formato válido → border verde + checkmark
  │    └─ Formato inválido → border rojo + InlineFieldError "Formato inválido. Use DAN-YYYY-NNNNN"
  │         FolioSearchButton queda DESHABILITADO si hay error de formato
  │
Usuario click "Abrir folio"
  │
  ├─ [API] GET /v1/quotes/{folioNumber}
  │
  ├─ [API Response 200 OK]
  │    body: { data: { folioNumber, quoteStatus, version, metadata: { lastWizardStep } } }
  │    └─ [Router] navigate a la ruta del step:
  │         step 0 → "/quotes/{folioNumber}/general-info"
  │         step 1 → "/quotes/{folioNumber}/general-info"
  │         step 2 → "/quotes/{folioNumber}/locations"
  │         step N → ... (según wizard route map)
  │
  ├─ [API Response 404 Not Found]
  │    └─ [Toast] error "El folio {folioNumber} no existe"
  │                                 body permanece en la pantalla de inicio
  │
  └─ [API Response 400 Bad Request] (nunca debería pasar si la validación FE funciona)
       └─ [Toast] error "Formato de folio inválido"
```

### Flow 3: Error de red / servicio no disponible

```
POST /v1/folios timeout o 503
  ├─ [Toast] error "No fue posible crear el folio. Intente nuevamente."
  ├─ [Button] vuelve a estado normal (isLoading: false)
  └─ [Reintentar] el usuario puede hacer click nuevamente
                  el mismo Idempotency-Key se reutiliza (guardado en estado local de la sesión)
                  → evita crear duplicados si el 1er request llegó pero falló la respuesta
```

---

## Sección 4 — Mockups ASCII

### Pantalla 1A: Home / Inicio — Estado inicial

```
┌─────────────────────────────────────────────────────────────────────┐
│  🏢 Cotizador de Seguros de Daños                    [Usuario: Admin]│
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│                    ┌─────────────────────────┐                      │
│                    │   COTIZADOR DE DAÑOS    │                      │
│                    │   Seguros a la Propiedad│                      │
│                    └─────────────────────────┘                      │
│                                                                     │
│  ┌────────────────────────────┐  ┌────────────────────────────────┐ │
│  │  ╔══════════════════════╗  │  │  ╔════════════════════════════╗│ │
│  │  ║  CREAR FOLIO NUEVO   ║  │  │  ║  ABRIR FOLIO EXISTENTE    ║│ │
│  │  ╚══════════════════════╝  │  │  ╚════════════════════════════╝│ │
│  │                            │  │                                │ │
│  │  Inicia una nueva          │  │  Retoma una cotización        │ │
│  │  cotización de seguros     │  │  en progreso                  │ │
│  │  de daños a la propiedad   │  │                                │ │
│  │                            │  │  ┌──────────────────────────┐ │ │
│  │                            │  │  │ DAN-YYYY-NNNNN           │ │ │
│  │                            │  │  │ ej: DAN-2026-00001       │ │ │
│  │                            │  │  └──────────────────────────┘ │ │
│  │                            │  │                                │ │
│  │  ┌──────────────────────┐  │  │  ┌──────────────────────────┐ │ │
│  │  │  + Crear folio nuevo │  │  │  │  🔍 Abrir folio          │ │ │
│  │  └──────────────────────┘  │  │  └──────────────────────────┘ │ │
│  │  (Button — primary)        │  │  (Button — secondary)         │ │
│  └────────────────────────────┘  └────────────────────────────────┘ │
│                                                                     │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Pantalla 1B: Home — Input de búsqueda con error de formato

```
│  ┌────────────────────────────────────────────────────────────────┐ │
│  │  ╔════════════════════════════════════════════════════════════╗│ │
│  │  ║  ABRIR FOLIO EXISTENTE                                    ║│ │
│  │  ╚════════════════════════════════════════════════════════════╝│ │
│  │                                                                │ │
│  │  ┌──────────────────────────────────────────────────────────┐ │ │
│  │  │ DAN-9999-XXXX                                            │ │ │ ← border rojo
│  │  └──────────────────────────────────────────────────────────┘ │ │
│  │  ⚠ Formato inválido. Use DAN-YYYY-NNNNN (ej: DAN-2026-00001) │ │ ← InlineFieldError
│  │                                                                │ │
│  │  ┌──────────────────────────────┐                             │ │
│  │  │  🔍 Abrir folio  [disabled] │                             │ │ ← Button deshabilitado
│  │  └──────────────────────────────┘                             │ │
│  └────────────────────────────────────────────────────────────────┘ │
```

### Pantalla 1C: Home — Creando folio (loading)

```
│  ┌────────────────────────────┐                                     │
│  │  ╔══════════════════════╗  │                                     │
│  │  ║  CREAR FOLIO NUEVO   ║  │                                     │
│  │  ╚══════════════════════╝  │                                     │
│  │                            │                                     │
│  │  ┌──────────────────────┐  │                                     │
│  │  │  ⟳ Creando folio...  │  │ ← spinner inline + texto cambia    │
│  │  └──────────────────────┘  │                                     │
│  │  (Button — disabled)        │                                     │
│  └────────────────────────────┘                                     │
```

### Pantalla 2: Confirmación de folio creado

```
┌─────────────────────────────────────────────────────────────────────┐
│  🏢 Cotizador de Seguros de Daños                    [Usuario: Admin]│
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│                   ✅  Folio creado exitosamente                     │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────────┐│
│  │                                                                 ││
│  │               ┌─────────────────────────────┐                  ││
│  │               │   DAN-2026-00001        [📋] │                  ││ ← FolioNumberBadge
│  │               │   (click para copiar)        │                  ││   grande, copiable
│  │               └─────────────────────────────┘                  ││
│  │                                                                 ││
│  │               ┌──────────────────┐                             ││
│  │               │  🟡 En borrador  │                             ││ ← StatusBadge ámbar
│  │               └──────────────────┘                             ││
│  │                                                                 ││
│  │   Creado el: 29/03/2026 10:32 hrs                               ││ ← ReadOnlyField
│  │                                                                 ││
│  │  ─────────────────────────────────────────────────────────────  ││
│  │                                                                 ││
│  │  Ya tienes tu folio. Ahora captura los datos de la cotización.  ││
│  │                                                                 ││
│  │  ┌─────────────────────────────────────────────────────────┐   ││
│  │  │         → Iniciar captura: Datos Generales              │   ││ ← Button primary
│  │  └─────────────────────────────────────────────────────────┘   ││   full-width
│  │                                                                 ││
│  └─────────────────────────────────────────────────────────────────┘│
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

### Toast de error — Folio no encontrado

```
┌─────────────────────────────────────────────────────────────────────┐
│                                                      ┌────────────┐ │
│                                                      │ ✕  Error   │ │
│                                                      ├────────────┤ │
│                                                      │ El folio   │ │
│                                                      │ DAN-2026-  │ │
│                                                      │ 99999 no   │ │
│                                                      │ existe     │ │
│                                                      └────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Sección 5 — Visual Rules

### Paleta de colores semánticos

| Token | Color hex | Uso en esta feature |
|---|---|---|
| `--color-primary` | `#1A56DB` (azul) | Botón "Crear folio nuevo" (acción principal) |
| `--color-secondary` | `#374151` (gris oscuro) | Botón "Abrir folio" (acción secundaria) |
| `--color-status-draft` | `#D97706` (ámbar) | StatusBadge cuando `quoteStatus: "draft"` |
| `--color-status-in-progress` | `#2563EB` (azul medio) | StatusBadge cuando `quoteStatus: "in_progress"` |
| `--color-error` | `#DC2626` (rojo) | Borde input con error de formato / Toast error |
| `--color-success` | `#16A34A` (verde) | Icono ✅ en pantalla de confirmación |
| `--color-surface` | `#FFFFFF` | Fondo de cards |
| `--color-bg` | `#F9FAFB` | Fondo de página |
| `--color-border` | `#E5E7EB` | Borde de cards y inputs en estado normal |
| `--color-border-focus` | `#1A56DB` | Borde de input en foco |
| `--color-border-valid` | `#16A34A` | Borde de input con formato válido |
| `--color-text-primary` | `#111827` | Texto principal |
| `--color-text-secondary` | `#6B7280` | Texto de descripción / helper text |

### Estados visuales del FolioSearchInput

| Estado | Borde | Icono derecho | Helper text |
|---|---|---|---|
| Vacío / inicial | `--color-border` (gris) | — | `Ej: DAN-2026-00001` (placeholder) |
| En foco | `--color-border-focus` (azul) | — | — |
| Válido (formato OK) | `--color-border-valid` (verde) | ✓ verde | — |
| Error de formato | `--color-error` (rojo) | ✗ rojo | "Formato inválido. Use DAN-YYYY-NNNNN" en rojo |
| Deshabilitado | `--color-border` (gris, 50% opacidad) | — | — |

### StatusBadge — Mapa de quoteStatus

| `quoteStatus` | Etiqueta UI | Color | Token |
|---|---|---|---|
| `draft` | En borrador | Ámbar | `--color-status-draft` |
| `in_progress` | En progreso | Azul | `--color-status-in-progress` |
| `calculated` | Calculado | Verde | `--color-status-calculated` (`#16A34A`) |
| `closed` | Cerrado | Gris | `--color-status-closed` (`#9CA3AF`) |

### FolioNumberBadge — Reglas visuales

- Tipografía: `font-size: 2rem; font-weight: 700; font-family: monospace`
- Fondo: `--color-bg` con borde `--color-border`
- Padding: `16px 24px`
- Click-to-copy: al hacer click, muestra tooltip breve "¡Copiado!" durante 2s y vuelve
- Color del texto: `--color-text-primary`

### Botones — Reglas

| Variant | Background | Texto | Hover | Disabled | Loading |
|---|---|---|---|---|---|
| `primary` | `--color-primary` | Blanco | 10% más oscuro | 40% opacidad | Spinner blanco + texto cambia |
| `secondary` | Transparente | `--color-secondary` | Fondo gris claro | 40% opacidad | Spinner gris + texto cambia |
| `ghost` | Transparente | `--color-primary` | Fondo azul 5% | 40% opacidad | Spinner azul |

### Reglas de accesibilidad (WCAG AA)

- Contraste mínimo 4.5:1 en texto sobre fondo (error rojo sobre blanco: OK — ratio 5.1:1)
- `FolioSearchInput` tiene `aria-describedby` apuntando al `InlineFieldError`
- `CreateFolioButton` y `FolioSearchButton` tienen `aria-busy="true"` cuando `isLoading`
- `FolioNumberBadge` tiene `aria-label="Número de folio: DAN-2026-00001"` y `role="button"` para click-to-copy
- Toda acción debe ser accesible por teclado (Tab/Enter/Space)

---

## Sección 6 — Stitch Prompts (referencia para generación futura)

### Prompt — Pantalla Home / Inicio

```
Diseña una pantalla de inicio para un cotizador de seguros de daños a la propiedad (incendio y riesgos aliados).
El usuario es un agente o underwriter de seguros que trabaja en escritorio.

LAYOUT: Dos cards centradas en la pantalla, dispuestas en una grid de 2 columnas con gap de 32px.
Fondo de página: gris muy claro (#F9FAFB). Cards con fondo blanco, borde sutil, border-radius 12px, sombra ligera.

CARD IZQUIERDA — "Crear folio nuevo":
- Título grande: "Crear folio nuevo"
- Subtítulo: "Inicia una nueva cotización de seguros de daños a la propiedad"
- Botón primario azul (#1A56DB) full-width: "+ Crear folio nuevo"
- Sin campos de formulario

CARD DERECHA — "Abrir folio existente":
- Título grande: "Abrir folio existente"
- Subtítulo: "Retoma una cotización en progreso"
- Input de texto con placeholder "DAN-YYYY-NNNNN (ej: DAN-2026-00001)" y label "Número de folio"
- Botón secundario (borde gris oscuro, fondo blanco) full-width: "🔍 Abrir folio"

HEADER global: Logo "Cotizador de Daños" a la izquierda, nombre de usuario a la derecha.
TIPOGRAFÍA: Fuente sans-serif profesional (Inter o similar). Títulos 24px bold, subtítulos 14px gris.
ACCESIBILIDAD: Contraste WCAG AA. Labels explícitos en inputs. Botones con estados hover y disabled visibles.
ESTILO: Sobrio, profesional, confiable. Sin ilustraciones ni iconos decorativos excesivos.
No incluir navegación lateral ni sidebar en esta pantalla — es la pantalla de entrada al sistema.
```

### Prompt — Pantalla Confirmación de folio creado

```
Diseña una pantalla de confirmación de creación de folio para un cotizador de seguros de daños.
El usuario acaba de crear un folio nuevo y el sistema lo confirmó con número DAN-2026-00001.

LAYOUT: Pantalla centrada, card única de confirmación con ancho máximo 560px, centrada vertical y horizontalmente.

CONTENIDO DE LA CARD:
- Ícono de éxito (checkmark verde grande ✅) en la parte superior
- Texto "Folio creado exitosamente" como h1 (verde oscuro)
- Casilla grande con el número de folio "DAN-2026-00001" en tipografía monospace 28px bold, con ícono de copiado al portapapeles (📋) a la derecha
- Chip de estado ámbar "#D97706": "🟡 En borrador"
- Texto informativo pequeño: "Creado el 29/03/2026 10:32 hrs"
- Separador horizontal línea sutil
- Texto de guía: "Ya tienes tu folio. Ahora captura los datos de la cotización."
- Botón primario full-width: "→ Iniciar captura: Datos Generales"

FONDO: Gris muy claro, card blanca con sombra media y border-radius 16px.
TIPOGRAFÍA: Inter. Stack de información centrado.
ACCESIBILIDAD: WCAG AA, botón con foco visible, número de folio con aria-label descriptivo.
```
