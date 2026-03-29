# Design Spec — general-info-management

> **Spec de referencia**: `.github/specs/general-info-management.spec.md` (SPEC-004, status: APPROVED)
> **Agente**: ux-designer · **Feature type**: full-stack
> **Pantallas cubiertas**: Step 1 del Wizard (Datos Generales) · Header/Sidebar del Wizard
> **Última actualización**: 2026-03-29

---

## Sección 1 — Data → UI Mapping

### Análisis de clusters

Se identifican **4 clusters funcionales** por cohesión semántica y validación conjunta.

#### Cluster A — Datos del Asegurado

Campos que identifican a la persona física/moral a asegurar. El usuario los piensa en conjunto porque definen al "cliente". Los campos email y teléfono son opcionales — se presenta el cluster completo pero sin marcarlos como obligatorios.

| Campo API | Tipo | Componente UI | Input type | Obligatorio | Razón de diseño |
|---|---|---|---|---|---|
| `insuredData.name` | `string` (max 200) | `TextInput` | Texto libre | ✅ Sí | Identificador principal del asegurado; error inmediato si vacío |
| `insuredData.taxId` | `string` (RFC 12-13 chars) | `TextInput` con máscara | Texto con validación RFC | ✅ Sí | Dato fiscal crítico; validar formato en tiempo real |
| `insuredData.email` | `string` (email) | `TextInput` | email | ❌ No | Datos de contacto opcionales; agrupar visualmente pero sin asterisco obligatorio |
| `insuredData.phone` | `string` | `TextInput` | tel | ❌ No | Complemento de contacto; formatear como (55) XXXX-XXXX |

#### Cluster B — Datos de Conducción

Campos que definen la relación comercial del folio (quién gestiona la póliza). El suscriptor es el campo pivot: al seleccionarlo, `officeName` se autocompleta automáticamente.

| Campo API | Tipo | Componente UI | Input type | Obligatorio | Razón de diseño |
|---|---|---|---|---|---|
| `conductionData.subscriberCode` | `string` (catálogo `GET /v1/subscribers`) | `ComboBox` | Selección única con búsqueda | ✅ Sí | Catálogo externo; puede tener muchos items → ComboBox con búsqueda |
| `conductionData.officeName` | `string` (derivado del suscriptor) | `ReadOnlyField` con chip "auto" | — | ✅ Sí (derivado) | Se autocompleta al seleccionar suscriptor; el usuario no lo edita directamente |
| `agentCode` | `string` (formato AGT-NNN) | `TextInput` con lookup | Texto + validación | ✅ Sí | El usuario conoce el código de su agente; hay lookup explícito para confirmar |

#### Cluster C — Tipo y Clasificación

Campos que categorizan el negocio y el riesgo. Son opciones acotadas — dropdown simple.

| Campo API | Tipo | Componente UI | Input type | Obligatorio | Razón de diseño |
|---|---|---|---|---|---|
| `businessType` | `enum` (NUEVO, RENOVACION, ENDOSO) | `RadioGroup` | Mutuamente excluyentes | ✅ Sí | Solo 3 opciones — radiogroup es más directo que dropdown; usuario ve todas las opciones |
| `riskClassification` | `string` (catálogo `GET /v1/catalogs/risk-classification`) | `Select` / `ComboBox` | Selección única | ✅ Sí | Catálogo pequeño (<7 items): preferir select nativo o simple dropdown |

#### Cluster D — Control de versión (oculto en UI, presente en payload)

| Campo API | Tipo | Componente UI | Razón de diseño |
|---|---|---|---|
| `version` | `integer` | Oculto (valor en Redux store) | El usuario nunca ve ni edita la versión; se envía en el payload automáticamente al guardar |

### Pantalla Header/Sidebar del Wizard

| Dato | Origen | Componente UI | Notas |
|---|---|---|---|
| `folioNumber` | Redux `activeFolio` | `FolioBadge` (pequeño, en header) | Visible en todo momento del wizard |
| `quoteStatus` | Redux / TanStack Query | `StatusBadge` (compacto) | Se actualiza al guardar (draft → in_progress) |
| `currentStep` / total | Redux `currentStep` | `WizardProgressBar` | Texto "Paso 1 de N" + barra visual de progreso |
| Nombre del step actual | Estático (array de steps) | `WizardStepLabel` | "Datos Generales", "Ubicaciones", etc. |

---

## Sección 2 — Component Inventory

### Jerarquía FSD

```
pages/
  GeneralInfoPage.tsx               ← step 1 del wizard; ensambla widgets y features
    widgets/
      WizardLayout.tsx              ← layout estructural: header + content + footer nav
        WizardHeader.tsx            ← folioNumber + statusBadge + wizardProgressBar
        WizardStepNav.tsx           ← "Anterior" / "Guardar y continuar" con estado
    features/general-info-form/
      ui/
        GeneralInfoForm.tsx         ← formulario completo con 4 sections/clusters
        InsuredDataSection.tsx      ← cluster A: nombre, RFC, email, teléfono
        ConductionDataSection.tsx   ← cluster B: suscriptor (pivot), oficina (derivado), agente
        BusinessClassSection.tsx    ← cluster C: businessType (radio) + riskClassification (select)
        VersionConflictModal.tsx    ← modal para HTTP 409
      model/
        useGeneralInfoForm.ts       ← RHF + Zod + useMutation (PUT general-info)
        generalInfoSchema.ts        ← Zod schemas (validación completa)
      api/
        generalInfoApi.ts           ← PUT /v1/quotes/{folio}/general-info
    features/subscriber-selector/
      ui/
        SubscriberComboBox.tsx      ← ComboBox con búsqueda; autocompletar officeName
      model/
        useSubscribersQuery.ts      ← useQuery GET /v1/subscribers
    features/risk-classification/
      ui/
        RiskClassificationSelect.tsx ← select con catálogo
      model/
        useRiskClassificationQuery.ts ← useQuery GET /v1/catalogs/risk-classification
    entities/folio/
      model/
        quoteWizardSlice.ts         ← currentStep, activeFolio, folioVersion
      ui/
        FolioBadge.tsx              ← badge compacto del folioNumber
        StatusBadge.tsx             ← chip de quoteStatus
    shared/
      ui/
        TextInput.tsx
        ReadOnlyField.tsx           ← campo solo lectura con chip "auto"
        RadioGroup.tsx
        ComboBox.tsx
        Select.tsx
        Button.tsx
        Modal.tsx
        Toast.tsx
        WizardProgressBar.tsx
```

### Tabla de componentes

| Componente | Capa FSD | Props clave | Tipo de estado | Notas |
|---|---|---|---|---|
| `GeneralInfoPage` | `pages` | `folioNumber: string` (desde Router params) | Sin estado local | Ensambla widgets y features; inicializa `useQuery GET general-info` |
| `WizardLayout` | `widgets` | `header`, `content`, `footer` (slots) | Sin estado | Layout estructural con 3 slots |
| `WizardHeader` | `widgets` | `folioNumber`, `quoteStatus`, `currentStep`, `totalSteps` | Del Redux store | Sticky en top |
| `WizardStepNav` | `widgets` | `onBack()`, `onSave()`, `isSaving: boolean`, `canGoBack: boolean` | Del padre | Botones de navegación inferiores |
| `GeneralInfoForm` | `features/general-info-form` | `defaultValues?`, `onSubmitSuccess()` | RHF (register) | Controlado por React Hook Form |
| `InsuredDataSection` | `features/general-info-form` | `control`, `errors` (del RHF) | Sin estado local | Sub-sección del formulario |
| `ConductionDataSection` | `features/general-info-form` | `control`, `errors`, `subscribers[]`, `onSubscriberChange(code)` | Sin estado local | `onSubscriberChange` dispara autocompletado de officeName |
| `BusinessClassSection` | `features/general-info-form` | `control`, `errors`, `riskOptions[]` | Sin estado local | Radio group + select |
| `SubscriberComboBox` | `features/subscriber-selector` | `value`, `onChange(code, officeName)`, `options[]`, `isLoading` | Controlado | Al seleccionar, llama `onChange` con code + officeName para autocompletar |
| `RiskClassificationSelect` | `features/risk-classification` | `value`, `onChange`, `options[]`, `isLoading` | Controlado | Carga opciones desde API al montar |
| `ReadOnlyField` | `shared/ui` | `label`, `value`, `hasAutoChip?: boolean` | Sin estado | Muestra chip "auto" cuando `hasAutoChip: true` |
| `RadioGroup` | `shared/ui` | `name`, `options: {value, label}[]`, `value`, `onChange`, `error?` | Controlado | Cada opción es un label+radio nativo accesible |
| `VersionConflictModal` | `features/general-info-form` | `isOpen`, `onReload()`, `onCancel()` | Controlado por padre | Se abre cuando API retorna 409 |
| `WizardProgressBar` | `shared/ui` | `current: number`, `total: number` | Sin estado | Barra + texto "Paso 2 de 5" |

### Zod Schemas

```typescript
// features/general-info-form/model/generalInfoSchema.ts
import { z } from 'zod';

const rfcRegex = /^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$/i;
const agentCodeRegex = /^AGT-\d{3}$/;
const subscriberCodeRegex = /^SUB-\d{3}$/;

export const insuredDataSchema = z.object({
  name: z
    .string()
    .min(1, 'El nombre del asegurado es obligatorio')
    .max(200, 'El nombre no puede exceder 200 caracteres'),
  taxId: z
    .string()
    .min(1, 'El RFC es obligatorio')
    .regex(rfcRegex, 'El RFC no tiene formato válido (ej: GIN850101AAA)'),
  email: z
    .string()
    .email('El correo electrónico no tiene formato válido')
    .optional()
    .or(z.literal('')),
  phone: z.string().optional().or(z.literal('')),
});

export const conductionDataSchema = z.object({
  subscriberCode: z
    .string()
    .min(1, 'El suscriptor es obligatorio')
    .regex(subscriberCodeRegex, 'Código de suscriptor inválido'),
  officeName: z.string().min(1, 'La oficina es obligatoria'),
  agentCode: z
    .string()
    .min(1, 'El código de agente es obligatorio')
    .regex(agentCodeRegex, 'Formato de agente inválido (ej: AGT-001)'),
});

export const businessClassSchema = z.object({
  businessType: z.enum(['NUEVO', 'RENOVACION', 'ENDOSO'], {
    errorMap: () => ({ message: 'Seleccione un tipo de negocio' }),
  }),
  riskClassification: z.string().min(1, 'La clasificación de riesgo es obligatoria'),
});

// Schema completo para el PUT
export const generalInfoFormSchema = insuredDataSchema
  .merge(conductionDataSchema)
  .merge(businessClassSchema);

export type GeneralInfoFormData = z.infer<typeof generalInfoFormSchema>;

// Payload del PUT (incluye version del Redux store)
export type GeneralInfoPayload = GeneralInfoFormData & { version: number };
```

---

## Sección 3 — Interaction Flows

### Flow 1: Carga inicial del formulario

```
GeneralInfoPage se monta (route: /quotes/:folioNumber/general-info)
  │
  ├─ [TanStack Query] useQuery GET /v1/quotes/{folioNumber}/general-info
  │    ├─ isLoading → mostrar skeleton del formulario (3 secciones con placeholders)
  │    ├─ 200 OK → poblar RHF con defaultValues: datos retornados por el API
  │    │           Si campos vacíos → RHF inicializa con strings vacíos
  │    │           Redux: dispatch(setFolioVersion(response.version))
  │    └─ 404 → [Toast] error "El folio no existe" + navigate("/")
  │
  ├─ [TanStack Query] useSubscribersQuery GET /v1/subscribers
  │    ├─ isLoading → SubscriberComboBox muestra "Cargando suscriptores..."
  │    └─ 200 OK → popula options del SubscriberComboBox
  │
  └─ [TanStack Query] useRiskClassificationQuery GET /v1/catalogs/risk-classification
       ├─ isLoading → RiskClassificationSelect muestra "Cargando..."
       └─ 200 OK → popula options del RiskClassificationSelect
```

### Flow 2: Selección de Suscriptor (campo pivot)

```
Usuario selecciona suscriptor "Oficina CDMX Central (SUB-001)"
  │
  ├─ SubscriberComboBox llama onChange(code: "SUB-001", officeName: "CDMX Central")
  ├─ [RHF] setValue("subscriberCode", "SUB-001")
  ├─ [RHF] setValue("officeName", "CDMX Central")         ← autocompletado instantáneo
  └─ ReadOnlyField "Oficina" muestra "CDMX Central" con chip "auto"
                                                           animación fade-in del valor derivado
```

### Flow 3: Guardar datos generales (Happy path)

```
Usuario click "Guardar y continuar"
  │
  ├─ [RHF] trigger() — validación completa del formulario
  │    ├─ Errores presentes → scroll al primer campo inválido + highlight de errores
  │    └─ Sin errores → continuar
  │
  ├─ [WizardStepNav] isSaving: true → botón "Guardando..." + disabled
  │
  ├─ [API] PUT /v1/quotes/{folioNumber}/general-info
  │         Body: { ...formData, version: <version del Redux store> }
  │
  ├─ [API Response 200 OK]
  │    body: { data: { ...general-info actualizado, version: N+1 } }
  │    ├─ [Redux] dispatch(setFolioVersion(N+1))
  │    ├─ [Redux] dispatch(nextStep())
  │    ├─ [TanStack] invalidateQueries(['folio', folioNumber])      ← refresca el folio
  │    └─ [Router] navigate a /quotes/{folioNumber}/locations (step 2)
  │
  ├─ [API Response 409 Conflict]
  │    └─ [Modal] VersionConflictModal.isOpen = true
  │         ├─ "Recargar" → refetch GET general-info → poblar formulario con datos actuales
  │         └─ "Cancelar" → cerrar modal, datos del form preservados (usuario puede editar)
  │
  ├─ [API Response 422 Unprocessable]
  │    └─ [Toast] error "El agente {agentCode} no está registrado en el catálogo"
  │              El formulario permanece visible para corrección
  │
  └─ [API Response 404]
       └─ [Toast] error "El folio no existe" + navigate("/")
```

### Flow 4: Validación inline de RFC

```
Usuario ingresa RFC y pierde foco (onBlur)
  │
  ├─ [RHF + Zod] validación de rfcRegex
  │    ├─ Válido → borde verde + sin mensaje de error
  │    └─ Inválido → borde rojo + error "El RFC no tiene formato válido (ej: GIN850101AAA)"
  │
(NO se llama al API — la validación de RFC es solo local en esta feature)
```

---

## Sección 4 — Mockups ASCII

### Pantalla: Header/Sidebar del Wizard (componente persistente)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  🏢 Cotizador de Daños                               [Usuario: Admin] ✕ │
├─────────────────────────────────────────────────────────────────────────┤
│  Folio: [DAN-2026-00001]  [🔵 En progreso]   Paso 1 de 4              │
│  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━   │ ← barra progreso 25%
│  ① Datos Generales  ○ Ubicaciones  ○ Coberturas  ○ Resultados          │
├─────────────────────────────────────────────────────────────────────────┤
│  [CONTENIDO DEL STEP]                                                   │
└─────────────────────────────────────────────────────────────────────────┘
```

### Pantalla: Step 1 — Datos Generales (estado inicial / vacío)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  🏢 Cotizador de Daños                               [Usuario: Admin] ✕ │
├─────────────────────────────────────────────────────────────────────────┤
│  Folio: [DAN-2026-00001]  [🟡 En borrador]   Paso 1 de 4               │
│  ████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░   │ ← barra 25%
│  ① Datos Generales  ○ Ubicaciones  ○ Coberturas  ○ Resultados           │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌── A. DATOS DEL ASEGURADO ─────────────────────────────────────────┐  │
│  │                                                                   │  │
│  │  Nombre del asegurado *                                           │  │
│  │  ┌─────────────────────────────────────────────────────────────┐ │  │
│  │  │                                                             │ │  │
│  │  └─────────────────────────────────────────────────────────────┘ │  │
│  │  Ej: Grupo Industrial SA de CV                                    │  │
│  │                                                                   │  │
│  │  RFC *                          Teléfono (opcional)               │  │
│  │  ┌──────────────────────────┐   ┌─────────────────────────────┐  │  │
│  │  │                          │   │                             │  │  │
│  │  └──────────────────────────┘   └─────────────────────────────┘  │  │
│  │  Ej: GIN850101AAA                (55) XXXX-XXXX                   │  │
│  │                                                                   │  │
│  │  Correo electrónico (opcional)                                    │  │
│  │  ┌─────────────────────────────────────────────────────────────┐ │  │
│  │  │                                                             │ │  │
│  │  └─────────────────────────────────────────────────────────────┘ │  │
│  │  Ej: contacto@empresa.com                                         │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌── B. DATOS DE CONDUCCIÓN ─────────────────────────────────────────┐  │
│  │                                                                   │  │
│  │  Suscriptor *                                                     │  │
│  │  ┌─────────────────────────────────────────────────────────── ▼┐ │  │
│  │  │ Seleccionar suscriptor...                                    │ │  │
│  │  └──────────────────────────────────────────────────────────────┘ │  │
│  │                                                                   │  │
│  │  Oficina                                                          │  │
│  │  ┌─────────────────────────────────────┐ [auto]                  │  │
│  │  │ — (se completa al elegir suscriptor)│                         │  │
│  │  └─────────────────────────────────────┘                         │  │
│  │                                                                   │  │
│  │  Código de agente *                                               │  │
│  │  ┌──────────────────────────┐                                    │  │
│  │  │ AGT-                     │ 🔍                                  │  │
│  │  └──────────────────────────┘                                    │  │
│  │  Ej: AGT-001                                                      │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌── C. TIPO Y CLASIFICACIÓN DE RIESGO ──────────────────────────────┐  │
│  │                                                                   │  │
│  │  Tipo de negocio *                                                │  │
│  │  ○ Nuevo negocio   ○ Renovación   ○ Endoso                        │  │
│  │                                                                   │  │
│  │  Clasificación de riesgo *                                        │  │
│  │  ┌─────────────────────────────────────────────────────────── ▼┐ │  │
│  │  │ Seleccionar clasificación...                                 │ │  │
│  │  └──────────────────────────────────────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
├─────────────────────────────────────────────────────────────────────────┤
│  [← Volver]                          [Guardar y continuar →]            │
└─────────────────────────────────────────────────────────────────────────┘
```

### Pantalla: Step 1 — Con errores de validación

```
│  ┌── A. DATOS DEL ASEGURADO ─────────────────────────────────────────┐  │
│  │                                                                   │  │
│  │  Nombre del asegurado *                                           │  │
│  │  ┌─────────────────────────────────────────────────────────────┐ │  │
│  │  │                           ← borde ROJO                      │ │  │
│  │  └─────────────────────────────────────────────────────────────┘ │  │
│  │  ⚠ El nombre del asegurado es obligatorio                         │  │ ← InlineFieldError
│  │                                                                   │  │
│  │  RFC *                                                            │  │
│  │  ┌──────────────────────────┐                                    │  │
│  │  │ INVALIDO123              │ ← borde ROJO                       │  │
│  │  └──────────────────────────┘                                    │  │
│  │  ⚠ El RFC no tiene formato válido (ej: GIN850101AAA)              │  │
│  └───────────────────────────────────────────────────────────────────┘  │
```

### Pantalla: Oficina autocompletada al seleccionar suscriptor

```
│  ┌── B. DATOS DE CONDUCCIÓN ─────────────────────────────────────────┐  │
│  │                                                                   │  │
│  │  Suscriptor *                                                     │  │
│  │  ┌─────────────────────────────────────────────────────────── ▼┐ │  │
│  │  │ ✓ SUB-001 — Oficina CDMX Central                             │ │  │ ← seleccionado
│  │  └──────────────────────────────────────────────────────────────┘ │  │
│  │                                                                   │  │
│  │  Oficina                                                          │  │
│  │  ┌─────────────────────────────────────┐ [auto]                  │  │
│  │  │ CDMX Central                        │ ← valor derivado        │  │ ← ReadOnlyField verde
│  │  └─────────────────────────────────────┘                         │  │   fadeIn animation
│  │                                                                   │  │
└───────────────────────────────────────────────────────────────────────┘
```

### Modal: Conflicto de versión (409)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  ╔══════════════════════════════════════╗               │
│                  ║  ⚠  Folio modificado                ║               │
│                  ╠══════════════════════════════════════╣               │
│                  ║                                      ║               │
│                  ║  El folio fue modificado por otro    ║               │
│                  ║  proceso mientras trabajabas.        ║               │
│                  ║  ¿Deseas recargar el folio con       ║               │
│                  ║  los datos más recientes?            ║               │
│                  ║                                      ║               │
│                  ║  Nota: no perderás lo no guardado — ║               │
│                  ║  puedes revisar y volver a guardar.  ║               │
│                  ║                                      ║               │
│                  ║  [Cancelar]      [Recargar folio →] ║               │
│                  ╚══════════════════════════════════════╝               │
└─────────────────────────────────────────────────────────────────────────┘
```

### Estado: Guardando (durante el PUT)

```
├─────────────────────────────────────────────────────────────────────────┤
│  [← Volver]  (disabled)              [⟳ Guardando...] (disabled)       │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Sección 5 — Visual Rules

### Paleta de colores semánticos

| Token | Color hex | Uso en esta feature |
|---|---|---|
| `--color-primary` | `#1A56DB` | Botón "Guardar y continuar" · borde de campo en foco · step activo |
| `--color-error` | `#DC2626` | Borde de campo inválido · texto de error inline · ícono ⚠ |
| `--color-success` | `#16A34A` | Borde de campo válido · checkmark en suscriptor seleccionado · officeName derivado |
| `--color-warning` | `#D97706` | StatusBadge "En borrador" · chip "auto" de readOnly field |
| `--color-info` | `#2563EB` | StatusBadge "En progreso" · tooltips informativos |
| `--color-surface` | `#FFFFFF` | Fondo de cards de sección |
| `--color-bg` | `#F9FAFB` | Fondo de página |
| `--color-section-border` | `#E5E7EB` | Borde de cards de sección (A, B, C) |
| `--color-readonly-bg` | `#F3F4F6` | Fondo de ReadOnlyField (indica que no es editable) |
| `--color-text-primary` | `#111827` | Texto de labels y valores de campos |
| `--color-text-secondary` | `#6B7280` | Placeholder · helper text · texto de secciones |
| `--color-text-required` | `#DC2626` | Asterisco `*` en labels de campos obligatorios |
| `--color-step-active` | `#1A56DB` | Círculo del step activo en el header del wizard |
| `--color-step-inactive` | `#9CA3AF` | Círculo de steps no alcanzados |
| `--color-step-completed` | `#16A34A` | Círculo de steps completados (check ✓) |

### Estados visuales de campos de formulario

| Estado | Borde | Label | Fondo | Ícono/accesorio | Mensaje |
|---|---|---|---|---|---|
| Vacío / inicial | `--color-section-border` gris | Normal | Blanco | — | Placeholder gris |
| En foco | `--color-primary` azul (2px) | Azul | Blanco | — | — |
| Válido (post-blur) | `--color-success` verde (2px) | Normal | Blanco | ✓ verde derecha | — |
| Error (post-blur) | `--color-error` rojo (2px) | Normal | `#FEF2F2` tinte rojo | ✗ rojo derecha | Error en rojo bajo el campo |
| Read-only / derivado | `--color-section-border` gris (1px) | Normal | `--color-readonly-bg` | Chip [auto] ámbar | — |
| Deshabilitado | Gris (50% opacidad) | Gris claro | Gris muy claro | — | — |
| Cargando opciones | Gris normal | Normal | Blanco | Spinner dentro | "Cargando..." en gris |

### Reglas de sección (Clusters)

- Cada cluster A / B / C se presenta en una **card con borde sutil** y título de sección en `font-size: 0.75rem; font-weight: 600; text-transform: uppercase; letter-spacing: 0.05em; color: --color-text-secondary`.
- Separación entre secciones: `margin-bottom: 24px`.
- Campos dentro de la sección: `gap: 16px` (vertical entre filas de inputs).
- Campos en fila: grid de 2 columnas para campos cortos (RFC + Teléfono en la misma fila).

### Reglas del ReadOnlyField con chip "auto"

- Fondo: `--color-readonly-bg` (#F3F4F6)
- Chip [auto]: background ámbar claro `#FEF3C7`, texto ámbar `#92400E`, border-radius redondo, `font-size: 0.65rem`, `font-weight: 600`
- Cuando el valor se autocompleta: animación `fade-in` 300ms
- `cursor: not-allowed` para indicar que no es editable
- `aria-readonly: "true"` + `aria-describedby` apuntando al chip con texto "Este campo se completa automáticamente al seleccionar el suscriptor"

### WizardProgressBar — Reglas

- Barra horizontal full-width en el header
- Fondo: `#E5E7EB` (gris claro) 
- Fill: `--color-primary` azul, animación `transition: width 400ms ease-in-out`
- `width`: `(currentStep / totalSteps) * 100%`
- Texto: "Paso {n} de {total}" — `font-size: 0.8rem; color: --color-text-secondary`

### Indicadores de step en el wizadrd header

| Estado del step | Estilo del indicador |
|---|---|
| Activo (actual) | Círculo relleno azul `--color-step-active`, número en blanco bold |
| Completado | Círculo relleno verde `--color-step-completed`, ícono ✓ en blanco |
| No alcanzado | Círculo borde gris `--color-step-inactive`, número en gris |

### Accesibilidad (WCAG AA — obligatorio)

- Cada `TextInput` tiene `<label>` explícito asociado por `htmlFor/id`.
- Campos obligatorios: asterisco `*` en el label + `aria-required="true"` en el input.
- `InlineFieldError`: `role="alert"` + `aria-live="polite"` + `id` referenciado por `aria-describedby` del input.
- `VersionConflictModal`: `role="dialog"`, `aria-modal="true"`, `aria-labelledby` apuntando al título, foco atrapado en el modal mientras está abierto.
- `ReadOnlyField`: `aria-readonly="true"` + `aria-label` descriptivo.
- `WizardProgressBar`: `role="progressbar"`, `aria-valuenow`, `aria-valuemin="0"`, `aria-valuemax="100"`.
- `RadioGroup`: grupo con `<fieldset>` + `<legend>` para el label del grupo.
- Contraste mínimo 4.5:1 en todos los textos. Contrario: rojo error #DC2626 sobre blanco → ratio 5.1:1 ✓.
- Navegación completa por teclado: Tab, Enter, Space, Arrow keys en radiogroup.

---

## Sección 6 — Stitch Prompts (referencia para generación futura)

### Prompt — Wizard Header (componente persistente)

```
Diseña un header de wizard para un cotizador de seguros de daños.
Es un header sticky que aparece en TODOS los pasos del wizard y nunca desaparece.

CONTENIDO (de izquierda a derecha):
- Logo/nombre app pequeño: "Cotizador de Daños"
- Badge con número de folio: "DAN-2026-00001" en tipografía monospace, fondo gris claro
- Chip de estado compacto: "🔵 En progreso" (azul) o "🟡 En borrador" (ámbar)
- Spacer
- Progress bar horizontal: barra azul que avanza según el paso actual (paso 1 de 4 = 25%)
- Texto: "Paso 1 de 4"

SEGUNDA FILA (debajo del header principal):
- 4 indicadores de step en línea horizontal:
  ① "Datos Generales" (activo — círculo azul relleno, texto en negrita)
  ○ "Ubicaciones" (inactivo — círculo borde gris)
  ○ "Coberturas" (inactivo — círculo borde gris)
  ○ "Resultados" (inactivo — círculo borde gris)

ESTILO: Fondo blanco, borde inferior sutil (#E5E7EB). Altura total ~80px.
Fuente: Inter, sans-serif. Colores: azul #1A56DB (activo/progreso), ámbar #D97706 (borrador), gris #9CA3AF (inactivo).
ACCESIBILIDAD: WCAG AA. Steps con aria-current="step" en el activo. Progress bar con role="progressbar".
```

### Prompt — Formulario Datos Generales (Step 1)

```
Diseña el paso 1 de un wizard de cotización de seguros de daños a la propiedad.
El usuario es un agente o underwriter de seguros trabajando en escritorio.

LAYOUT: Formulario de una columna, ancho máximo 800px, centrado. Debajo del wizard header.
Fondo de página gris muy claro (#F9FAFB). Secciones del formulario en cards blancas con borde sutil y border-radius 8px.

SECCIÓN A — "Datos del Asegurado" (card):
- Título de sección: "A. DATOS DEL ASEGURADO" en texto pequeño gris uppercase
- Campo full-width: "Nombre del asegurado *" — TextInput grande, placeholder "Grupo Industrial SA de CV"
- Fila 2 columnas: "RFC *" (TextInput, placeholder "GIN850101AAA") | "Teléfono (opcional)" (TextInput, placeholder "(55) XXXX-XXXX")
- Campo full-width: "Correo electrónico (opcional)" — TextInput, placeholder "contacto@empresa.com"

SECCIÓN B — "Datos de Conducción" (card debajo):
- Título: "B. DATOS DE CONDUCCIÓN"
- Campo full-width: "Suscriptor *" — ComboBox dropdown con búsqueda, placeholder "Seleccionar suscriptor..."
- Fila 2 columnas:
  - "Oficina" — campo no editable con fondo gris claro (#F3F4F6) y chip ámbar pequeño "[auto]" a la derecha, placeholder "Se completa al elegir suscriptor"
  - "Código de agente *" — TextInput con ícono de búsqueda 🔍, placeholder "AGT-001"

SECCIÓN C — "Tipo y Clasificación" (card debajo):
- Título: "C. TIPO Y CLASIFICACIÓN DE RIESGO"
- "Tipo de negocio *" — 3 radio buttons en fila: "Nuevo negocio" | "Renovación" | "Endoso"
- "Clasificación de riesgo *" — Select dropdown, placeholder "Seleccionar clasificación..."

FOOTER DEL WIZARD (sticky bottom):
- Botón ghost izquierda: "← Volver"
- Botón primary derecha azul (#1A56DB): "Guardar y continuar →"

CAMPOS OBLIGATORIOS: marcados con asterisco * en el label y texto de ayuda bajo el campo.
TIPOGRAFÍA: Inter. Labels: 14px medium. Inputs: 16px. Sección titles: 11px uppercase gris.
ACCESIBILIDAD: WCAG AA. Labels explícitos. Aria-required en campos obligatorios.
ESTILO: Sobrio, profesional. Sin ilustraciones. Formulario limpio con mucho espacio en blanco.
```

### Prompt — Modal de conflicto de versión (409)

```
Diseña un modal de diálogo de advertencia para un cotizador de seguros.
El modal aparece cuando el sistema detecta que el folio fue modificado por otro proceso (conflicto de versión).

CONTENIDO:
- Ícono de advertencia ⚠ en ámbar grande al inicio
- Título: "Folio modificado"
- Texto: "El folio fue modificado por otro proceso mientras trabajabas. ¿Deseas recargar el folio con los datos más recientes?"
- Nota informativa pequeña en gris: "No perderás tu trabajo — podrás revisar los datos actualizados y volver a guardar."
- Fila de botones alineados a la derecha:
  - Botón ghost: "Cancelar"
  - Botón primary azul: "Recargar folio →"

ESTILO: Modal centrado, máximo 480px ancho. Fondo de overlay oscuro semitransparente.
Card blanca con border-radius 12px, sombra fuerte. Padding 32px.
Tono: advertencia, NO error. Usar ámbar, no rojo.
ACCESIBILIDAD: role="dialog", aria-modal="true", foco atrapado dentro del modal, Escape cierra el modal (equivalente a Cancelar).
```
