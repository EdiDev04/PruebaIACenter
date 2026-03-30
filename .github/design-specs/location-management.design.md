# Design Spec: Gestión de Ubicaciones de Riesgo

> **SPEC:** SPEC-006
> **Feature:** location-management
> **Status:** APPROVED
> **Creado:** 2026-03-29
> **Autor:** ux-designer

---

## Seccion 1 -- Data -> UI Mapping

### Entidad principal: `Location`

| Campo | Tipo | Componente UI | Tipo input | Razon de diseno |
|---|---|---|---|---|
| `index` | `number` (auto) | No editable en formulario | Derivado | Se asigna automáticamente. Visible en la grilla como "#" |
| `locationName` | `string` (requerido) | `TextInput` | Texto libre | Campo primario de identificacion del inmueble |
| `address` | `string` (requerido) | `TextInput` | Texto libre | Direccion completa del inmueble |
| `zipCode` | `string` (5 digitos) | `TextInput` con trigger | Texto controlado | Campo pivot: al ingresar 5 digitos resuelve zona, estado, municipio y colonia automaticamente |
| `state` | `string` (auto) | `ReadOnlyField` con chip "auto" | Solo lectura | Resuelto automaticamente desde zipCode via core-ohs |
| `municipality` | `string` (auto) | `ReadOnlyField` con chip "auto" | Solo lectura | Resuelto automaticamente desde zipCode via core-ohs |
| `neighborhood` | `string` (auto) | `ReadOnlyField` con chip "auto" | Solo lectura | Resuelto automaticamente desde zipCode via core-ohs |
| `city` | `string` (auto) | `ReadOnlyField` con chip "auto" | Solo lectura | Resuelto automaticamente desde zipCode via core-ohs |
| `catZone` | `string` (auto) | `ReadOnlyField` con badge de zona | Solo lectura | Resuelto automaticamente. Zona A/B/C impacta tarificacion |
| `constructionType` | `string` (catalogo pequeno <7) | `RadioGroup` | Seleccion unica | Opciones: Tipo 1 Macizo, Tipo 2 Mixto, Tipo 3 Ligero, Tipo 4 Metalico. <7 opciones → radio visible |
| `level` | `number` (entero >= 0) | `NumberInput` con stepper | Numerico acotado | Niveles tipicamente 1-30. Stepper facilita entrada precisa |
| `constructionYear` | `number` (1800-2026) | `NumberInput` libre | Numerico libre | Rango amplio, mejor texto numerico que stepper |
| `businessLine` | `object` (catalogo ~20 opciones) | `ComboBox` con busqueda | Seleccion unica catalogo grande | Catalogo de giros comerciales con >7 opciones → ComboBox con busqueda por nombre o fireKey |
| `guarantees` | `LocationGuarantee[]` (multi-seleccion con monto) | `CheckboxGroup` agrupado + `NumberInput` por garantia | Multi-seleccion con valor | 14 garantias agrupadas en 4 categorias. Cada garantia seleccionada muestra campo de suma asegurada. requiresInsuredAmount indica si el monto es obligatorio |
| `validationStatus` | `string` enum: calculable/incomplete | `Badge` de estado | Solo lectura | Calculado por backend. Verde = calculable, Ambar = incompleto |
| `blockingAlerts` | `string[]` | `AlertList` inline | Solo lectura | Lista de alertas que explican por que la ubicacion esta incompleta |

### Garantias agrupadas por cohesion funcional (14 garantias en 4 grupos)

**Grupo 1 -- Coberturas Base (4 garantias)**
| Clave | Label UI | requiresInsuredAmount | Badge |
|---|---|---|---|
| `building_fire` | Incendio de edificio | Si | Recomendado |
| `contents_fire` | Incendio de contenido | Si | Recomendado |
| `glass` | Cristales | No | — |
| `illuminated_signs` | Anuncios luminosos | No | — |

**Grupo 2 -- Catastrofes Naturales (4 garantias)**
| Clave | Label UI | requiresInsuredAmount | Badge |
|---|---|---|---|
| `cat_tev` | Terremoto y erupcion volcanica | Si | — |
| `cat_hm` | Huracan y marea de tormenta | Si | — |
| `cat_hi` | Inundacion | Si | — |
| `cat_other` | Otras catastrofes | Si | — |

**Grupo 3 -- Complementarias (3 garantias)**
| Clave | Label UI | requiresInsuredAmount | Badge |
|---|---|---|---|
| `theft` | Robo con violencia | Si | — |
| `machinery_breakdown` | Maquinaria y equipo | Si | — |
| `electronic_equipment` | Equipo electronico | Si | — |

**Grupo 4 -- Especiales (3 garantias)**
| Clave | Label UI | requiresInsuredAmount | Badge |
|---|---|---|---|
| `business_interruption` | Interrupcion de negocio | Si | — |
| `civil_liability` | Responsabilidad civil | Si | — |
| `cash` | Dinero y valores | Si | — |

---

## Seccion 2 -- Behavioral Annotations

### Principios conductuales aplicados

| Principio | Aplicacion | Justificacion |
|---|---|---|
| Progressive disclosure | Formulario de ubicacion en 2 steps: (1) Datos generales + geograficos, (2) Giro + Garantias. Max 6 campos visibles por step | Evita sobrecarga cognitiva con 14 campos de una ubicacion |
| Campo pivot (CP) | Al ingresar 5 digitos en zipCode, auto-resolucion inmediata (<500ms) de 5 campos derivados | El CP es el campo con mayor densidad informacional. Elimina el 35% de la carga de captura manual |
| Smart defaults | Coberturas base (building_fire + contents_fire) pre-seleccionadas con badge "Recomendado" | Los dos primeros giros son universales en seguros de danos. Reduce friccion en el flujo mas comun |
| Incompleto != Error | Badge ambar "Datos pendientes" (nunca rojo) para ubicaciones incompletas | Rojo activa respuesta de evitacion. Ambar invita a completar. El guardado parcial es valido por diseno |
| Miller's Law | 14 garantias organizadas en 4 grupos. 5 columnas default en grilla de ubicaciones | Max 4 grupos semanticos. Max 7 elementos por grupo. Reduce carga de decision |
| Feedback inmediato | Spinner inline en zipCode durante resolucion. Mensaje de error contextual si CP no encontrado | Latencia percibida < 200ms con skeleton visible. No dejar al usuario sin feedback |
| Guardado parcial | Boton "Guardar" activo desde el primer campo valido. PATCH en edicion, PUT en nuevas ubicaciones | Reduce ansiedad de perdida de datos. El agente puede retomar en cualquier momento |
| Transparencia de estado | Contador activo "X de Y ubicaciones calculables" visible en el encabezado de la pagina | El agente sabe de un vistazo cuantas ubicaciones listas para calcular tarifa |

### Progressive disclosure — 2 steps del formulario

**Step 1 — Datos del Inmueble (6 campos)**
1. Nombre de ubicacion (campo primario, foco automatico)
2. Direccion
3. Codigo postal (campo pivot)
4. Tipo constructivo (radio group)
5. Nivel
6. Ano de construccion

Al avanzar al Step 2, los campos auto-resueltos (estado, municipio, colonia, ciudad, zona cat.) se muestran como resumen compacto read-only.

**Step 2 — Clasificacion y Coberturas (2 selectores principales)**
1. Giro comercial (ComboBox con busqueda)
2. Garantias activas (4 grupos colapsables con checkbox + monto)

### Estados de la entidad

| Estado | Badge | Color | Mensaje | Accion sugerida |
|---|---|---|---|---|
| `calculable` | Calculable | Verde (#16a34a) | Ubicacion lista para cotizar | Ninguna |
| `incomplete` | Datos pendientes | Ambar (#d97706) | Faltan datos requeridos | Link "Completar" que abre el formulario |
| Nueva (no guardada) | Sin guardar | Gris | En proceso de captura | — |

---

## Seccion 3 -- Screen Flow + Hierarchy

### Wireframe ASCII — Pagina principal de ubicaciones

```
┌─────────────────────────────────────────────────────────────────────────┐
│  WIZARD HEADER — Paso 2: Ubicaciones                                    │
│  DAN-2026-00001 > Paso 1 (OK) > [Paso 2: Ubicaciones] > Paso 3 > Paso 4│
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  Ubicaciones de riesgo                        [Configurar vista ⚙]     │
│  3 ubicaciones — 2 calculables, 1 pendiente                             │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │ #  │ Nombre           │ CP    │ Giro         │ Estado       │ ⋮  │   │
│  ├────┼──────────────────┼───────┼──────────────┼──────────────┼────┤   │
│  │ 1  │ Bodega CDMX      │ 06600 │ Storage wh.  │ ● Calculable │ ⋮  │   │
│  │ 2  │ Sucursal Del Valle│ 03100 │ Retail store │ ● Calculable │ ⋮  │   │
│  │ 3  │ Almacen Norte    │  —    │  —           │ ◐ Pendiente  │ ⋮  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │  ⚠ 1 ubicacion con datos pendientes — Completar para cotizar    │  │
│  │  • Almacen Norte: Codigo postal requerido, Giro comercial req.  │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  [+ Agregar ubicacion]                     [Continuar →] (2 calculables)│
└─────────────────────────────────────────────────────────────────────────┘
```

### Wireframe ASCII — Formulario de ubicacion (drawer/modal)

```
┌─────────────────────────────────────────────────────────────────────────┐
│  Nueva ubicacion                                           [×]          │
├─────────────────────────────────────────────────────────────────────────┤
│  ● Datos del inmueble  ○ Coberturas                                     │
│  ─────────────────────────────────────────────────────────────          │
│                                                                         │
│  Nombre de la ubicacion *                                               │
│  [Bodega Central CDMX                          ]                        │
│                                                                         │
│  Direccion *                                                            │
│  [Av. Industria 340                            ]                        │
│                                                                         │
│  Codigo postal *                         [06600]  [🔍 Resolviendo...]  │
│                                                                         │
│  ┌─ Datos resueltos automáticamente ──────────────────────────────┐    │
│  │  Estado [auto]: Ciudad de México   Municipio [auto]: Cuauhtémoc│    │
│  │  Colonia [auto]: Doctores          Zona cat. [auto]: A          │    │
│  └────────────────────────────────────────────────────────────────┘    │
│                                                                         │
│  Tipo constructivo                                                      │
│  ◉ Tipo 1 - Macizo  ○ Tipo 2 - Mixto  ○ Tipo 3 - Ligero               │
│                                                                         │
│  Nivel         Año de construccion                                      │
│  [2    ▲▼]    [1998            ]                                        │
│                                                                         │
│  ─────────────────────────────────────────────────────────              │
│  [Cancelar]                           [Siguiente → Coberturas]          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Jerarquía de información (F-pattern)

1. **Nivel 1 (escaneado horizontal):** Nombre de ubicacion, CP, Estado de validacion
2. **Nivel 2 (escaneado vertical):** Giro comercial, Coberturas seleccionadas
3. **Nivel 3 (detalle bajo demanda):** Direccion completa, ano de construccion, tipo constructivo, zona catastrofica

### Tabla de interacciones

| Accion del usuario | Resultado visual | API call |
|---|---|---|
| Clic "+ Agregar ubicacion" | Abre formulario step 1 (drawer) con foco en Nombre | Ninguno |
| Ingresa 5 digitos en CP | Spinner inline → campos auto-resueltos aparecen | `GET /v1/zip-codes/{cp}` |
| CP no encontrado (404) | Mensaje error "CP no encontrado" bajo el campo, campos auto en gris | Ninguno adicional |
| Avanza a Step 2 | Vista coberturas: 4 grupos acordeon, building_fire + contents_fire pre-check | Ninguno |
| Clic "Guardar" (nueva) | PUT con array actualizado → badge "Calculable" o "Pendiente" | `PUT /v1/quotes/{folio}/locations` |
| Clic menu contextual ⋮ → Editar | Abre formulario con datos precargados | `GET /v1/quotes/{folio}/locations` (cached) |
| Clic "Guardar" (edicion) | PATCH ubicacion individual → estado actualizado en grilla | `PATCH /v1/quotes/{folio}/locations/{index}` |
| Clic menu contextual ⋮ → Eliminar | Modal confirmacion → PUT sin esa ubicacion | `PUT /v1/quotes/{folio}/locations` |
| Clic "Continuar →" | Navega a Paso 3 (solo si >= 1 ubicacion calculable) | Ninguno |
| Clic "Configurar vista ⚙" | Abre panel lateral SPEC-005 (LayoutConfig) | `GET /v1/quotes/{folio}/layout` |
| Version conflict (409) | Toast "El folio fue modificado. Recargue para continuar" + reload forzado | Revalidacion caché |

---

## Seccion 4 -- Component Inventory

### Componentes por capa FSD

| Componente | Capa FSD | Props clave | Tipo de estado |
|---|---|---|---|
| `LocationsPage` | pages | `folio: string` | Coordinador, sin estado propio |
| `LocationsGrid` | widgets | `locations`, `onEdit`, `onDelete`, `visibleColumns` | Servidor (TanStack Query) |
| `LocationRow` | entities/location/ui | `location`, `onEdit`, `onDelete` | Presentacional |
| `LocationStatusBadge` | entities/location/ui | `status: "calculable" \| "incomplete"` | Presentacional |
| `LocationForm` | features/save-locations/ui | `folio`, `locationIndex?`, `onSuccess` | Formulario controlado (react-hook-form) |
| `LocationFormStep1` | features/save-locations/ui | `form`, `zipCodeState` | Sub-formulario (step 1) |
| `LocationFormStep2` | features/save-locations/ui | `form`, `businessLines`, `guaranteeCatalog` | Sub-formulario (step 2) |
| `ZipCodeField` | features/save-locations/ui | `control`, `onResolved`, `onError` | Campo con side-effect |
| `AutoResolvedFields` | features/save-locations/ui | `state`, `municipality`, `neighborhood`, `catZone` | Presentacional read-only |
| `BusinessLineSelector` | features/save-locations/ui | `control`, `options` | ComboBox controlado |
| `GuaranteesPanel` | features/save-locations/ui | `control`, `catalog` | Panel multi-seleccion con montos |
| `GuaranteeGroup` | features/save-locations/ui | `group`, `items`, `control` | Acordeon con checkboxes |
| `LocationsSummaryBanner` | features/save-locations/ui | `summary` | Presentacional (solo si incomplete > 0) |
| `DeleteLocationModal` | features/delete-location/ui | `locationName`, `onConfirm`, `onCancel` | Modal de confirmacion |

### Zod schemas por step

**Step 1 — Datos del inmueble**
```typescript
const locationStep1Schema = z.object({
  locationName: z.string().min(1, "El nombre de la ubicacion es obligatorio").max(200),
  address: z.string().min(1, "La direccion es obligatoria").max(300),
  zipCode: z.string().regex(/^\d{5}$/, "El codigo postal debe ser de 5 digitos").optional().or(z.literal("")),
  state: z.string().optional(),
  municipality: z.string().optional(),
  neighborhood: z.string().optional(),
  city: z.string().optional(),
  catZone: z.string().optional(),
  constructionType: z.enum(["Tipo 1 - Macizo", "Tipo 2 - Mixto", "Tipo 3 - Ligero", "Tipo 4 - Metalico"]).optional(),
  level: z.number().int().min(0).optional(),
  constructionYear: z.number().int().min(1800).max(2026).optional(),
});
```

**Step 2 — Clasificacion y coberturas**
```typescript
const guaranteeItemSchema = z.object({
  guaranteeKey: z.string(),
  insuredAmount: z.number().min(0, "La suma asegurada debe ser mayor o igual a 0"),
});

const locationStep2Schema = z.object({
  businessLine: z.object({
    description: z.string(),
    fireKey: z.string().min(1),
  }).optional(),
  guarantees: z.array(guaranteeItemSchema).optional(),
});
```

---

## Seccion 5 -- Validation + Feedback UX

### Matriz de validacion

| Campo | Trigger | Feedback positivo | Feedback de error |
|---|---|---|---|
| `locationName` | onBlur | Borde verde sutil | "El nombre de la ubicacion es obligatorio" (rojo bajo el campo) |
| `address` | onBlur | Borde verde sutil | "La direccion es obligatoria" |
| `zipCode` | onChange a 5 digitos | Spinner → campos auto aparecen con chip "auto" | "Codigo postal no encontrado" (ambar, no rojo — dato opcional) |
| `constructionType` | onChange | Radio seleccionado resaltado | — (campo opcional) |
| `level` | onBlur | Borde verde sutil | "El nivel debe ser un numero positivo" |
| `constructionYear` | onBlur | Borde verde sutil | "El ano de construccion es invalido (1800–2026)" |
| `businessLine` | onChange | Opcion seleccionada con checkmark | — (campo opcional, pero incompleta si falta) |
| `guarantees[].insuredAmount` | onBlur (si requiresInsuredAmount) | Monto formateado como moneda | "La suma asegurada es requerida para esta garantia" (ambar si omitida) |
| Version conflict (409) | Submit | — | Toast "El folio fue modificado por otro proceso. Recargue para continuar" |

### Estados visuales de campo

| Estado | Descripcion | Estilo |
|---|---|---|
| Vacio | Sin input | Borde gris, placeholder gris claro |
| Foco | Usuario editando | Borde azul, sombra focus ring |
| Valido | Input correcto tras blur | Borde verde, icono check opcional |
| Error | Validacion fallida | Borde rojo, texto error rojo bajo el campo |
| Auto-resuelto | Campo derivado de CP | Fondo gris claro, chip "auto" azul, no editable |
| Deshabilitado | Campo no aplicable | Opacidad 40%, cursor not-allowed |

### Tratamiento especial de errores criticos

**CP no encontrado (404 de core-ohs):**
- Mensaje ambar (no rojo): "Codigo postal no encontrado. Puedes continuar con la captura, pero la ubicacion quedara pendiente."
- Los campos auto-resueltos muestran placeholder "–" en gris
- La ubicacion se guarda con `validationStatus: "incomplete"` + alerta "Codigo postal requerido"
- No bloquea el guardado

**Servicio no disponible (503 de core-ohs):**
- Toast: "El servicio de catalogos no esta disponible. Intenta en unos momentos."
- CP queda sin resolver. Boton "Reintentar" inline junto al campo CP.

**Garantia sin suma asegurada requerida:**
- Badge ambar en la fila de la garantia: "Suma asegurada requerida"
- No bloquea seleccion. Si se guarda sin monto → alerta en `blockingAlerts`

---

## Seccion 6 -- Stitch Prompts

### Pantalla 1 — Grilla de ubicaciones (estado mixto: calculable + pendiente)

```
Diseña una página de gestión de ubicaciones de riesgo para un sistema de cotización de seguros de daños en México. Es el paso 2 de un wizard con 4 pasos.

CONTEXTO DE NEGOCIO:
El usuario es un agente de seguros que captura inmuebles a asegurar (ubicaciones de riesgo). Cada ubicación tiene datos físicos, giro comercial y coberturas con suma asegurada. El sistema calcula automáticamente si la ubicación tiene todos los datos para cotizar (estado: Calculable) o le faltan datos (estado: Datos pendientes).

LAYOUT:
- Página completa con wizard header en la parte superior
- Breadcrumb: DAN-2026-00001 > Paso 1: Datos generales (completado) > Paso 2: Ubicaciones (activo) > Paso 3 > Paso 4
- Debajo del header: título "Ubicaciones de riesgo" con subtítulo contador "3 ubicaciones — 2 calculables, 1 pendiente"
- Botón secundario "Configurar vista ⚙" alineado a la derecha del título
- Tabla de datos con 5 columnas: # (número de índice), Nombre de ubicación, Código postal, Giro comercial, Estado de validación. Última columna: menú contextual (3 puntos)
- Filas de la tabla: usar datos realistas de seguros mexicanos

DATOS DE EJEMPLO EN LA TABLA:
- Fila 1: # 1, "Bodega Central CDMX", 06600, "Storage warehouse", badge verde "Calculable"
- Fila 2: # 2, "Sucursal Del Valle", 03100, "Retail store", badge verde "Calculable"
- Fila 3: # 3, "Almacén Norte", "–", "–", badge ámbar "Datos pendientes"

COMPONENTES ESPECÍFICOS:
- Banner de alerta (fondo ámbar muy suave, icono advertencia) debajo de la tabla: "1 ubicación con datos pendientes. Completa los datos para incluirla en la cotización. • Almacén Norte: Código postal requerido, Giro comercial requerido"
- Botón primario "+ Agregar ubicación" debajo de la alerta (alineado izquierda)
- Botón primario "Continuar →" alineado a la derecha, con texto secundario "(2 ubicaciones calculables)" indicando cuántas se incluirán en el cálculo
- Menú contextual de la fila 3 expandido mostrando opciones: "Editar", "Eliminar"

ESTILO:
- Diseño profesional para agentes de seguros, no para consumidores finales
- Tabla con zebra striping suave (filas alternas)
- Paleta sobria: blancos, grises, acentos azul corporativo
- Badges: verde para Calculable, ámbar para Datos pendientes. NUNCA rojo para "pendiente"
- Tipografía clara, espaciado generoso, WCAG AA
- Fila 3 con badge ámbar debe comunicar "falta algo" no "hay un error"
```

### Pantalla 2 — Formulario de ubicacion step 1 (datos del inmueble con CP resuelto)

```
Diseña el formulario de captura de datos de una ubicación de riesgo para un sistema de cotización de seguros de daños en México. Es un panel lateral (drawer) de 520px que aparece sobre la página de grilla.

CONTEXTO DE NEGOCIO:
El agente de seguros está capturando una nueva ubicación. El código postal es el campo clave: al ingresar 5 dígitos el sistema resuelve automáticamente zona catastrófica, estado, municipio y colonia desde el servicio de catálogos. Los campos resueltos se muestran como solo lectura con un chip "auto" que indica que fueron completados automáticamente.

LAYOUT DEL DRAWER:
- Header del drawer: "Nueva ubicación" con botón X para cerrar (alineado derecha)
- Indicador de pasos: "● Datos del inmueble  ○ Coberturas" (step 1 de 2 activo)
- Separador visual
- Formulario con campos en orden

CAMPOS DEL FORMULARIO (todos en columna única):
1. "Nombre de la ubicación *" — TextInput — valor: "Bodega Central CDMX"
2. "Dirección *" — TextInput — valor: "Av. Industria 340"
3. "Código postal *" — TextInput con icono de búsqueda a la derecha — valor: "06600" — con indicador de estado "Resuelto ✓" en verde junto al campo
4. Bloque de campos auto-resueltos (fondo gris muy claro, esquinas redondeadas, sin borde prominente):
   - Etiqueta de sección: "Datos resueltos automáticamente" en texto pequeño gris
   - "Estado [auto]": chip azul "auto" + texto "Ciudad de México" (campo no editable)
   - "Municipio [auto]": chip azul "auto" + texto "Cuauhtémoc"
   - "Colonia [auto]": chip azul "auto" + texto "Doctores"
   - "Zona catastrófica [auto]": chip azul "auto" + badge especial "Zona A" (azul oscuro)
   - Layout de 2 columnas para estos 4 campos
5. "Tipo constructivo" — RadioGroup horizontal con 4 opciones: "Tipo 1 – Macizo" (seleccionado), "Tipo 2 – Mixto", "Tipo 3 – Ligero", "Tipo 4 – Metálico"
6. Layout de 2 columnas: "Nivel" (NumberInput con stepper, valor: 2) | "Año de construcción" (NumberInput, valor: 1998)

FOOTER DEL DRAWER:
- Botón ghost "Cancelar" (izquierda)
- Botón primario "Siguiente → Coberturas" (derecha)

ESTILO:
- Drawer con sombra lateral pronunciada sobre la página de grilla (la grilla visible en el fondo con overlay oscuro suave)
- Fondo blanco del drawer
- Chip "auto" en azul corporativo pequeño, texto en gris, sin borde
- Sección de datos auto-resueltos con fondo #f8fafc y esquinas redondeadas
- Sin bordes rojos — el código postal ya fue resuelto exitosamente
- Tipografía de labels en gris oscuro, inputs con borde gris claro
- WCAG AA, contraste mínimo 4.5:1
```

### Pantalla 3 — Formulario de ubicacion step 2 (coberturas y giro comercial)

```
Diseña el Step 2 del formulario de captura de una ubicación de riesgo para un sistema de cotización de seguros de daños en México. Es un panel lateral (drawer) de 520px. En este step, el Tab "Coberturas" está ACTIVO y el tab "Datos del inmueble" está COMPLETADO (con ícono checkmark).

CONTEXTO DE NEGOCIO:
El agente de seguros ya capturó los datos físicos del inmueble en el Step 1. Ahora debe seleccionar el giro comercial (tipo de actividad del negocio asegurado) y las coberturas (garantías) que aplican a esta ubicación. Cada cobertura puede requerir una suma asegurada (monto en pesos MXN). Las coberturas recomendadas vienen pre-seleccionadas.

LAYOUT DEL DRAWER:
- Header del drawer: "Nueva ubicación" con botón X para cerrar (alineado derecha)
- Tabs de navegación horizontal: "✓ Datos del inmueble" (completado, gris con checkmark) | "● Coberturas" (activo, azul, subrayado)
- Resumen compacto de Step 1 justo debajo de los tabs: fila horizontal read-only en gris claro con "Bodega Central CDMX — Av. Industria 340, CP 06600 | Tipo 1 – Macizo, Nivel 2, 1998"
- Separador fino
- Formulario de Step 2

CAMPOS DEL FORMULARIO (Step 2):

SECCIÓN "Giro comercial":
- Label: "Giro comercial *"
- ComboBox con campo de búsqueda — placeholder: "Buscar giro..." — valor seleccionado: "Storage warehouse - Bodega y almacenamiento"
- Texto de ayuda pequeño: "El giro determina las tasas de riesgo aplicables"

SECCIÓN "Coberturas" con contador activo:
- Subtítulo: "Coberturas activas" + badge contador azul con número "2 seleccionadas"
- Texto explicativo pequeño: "Selecciona las coberturas y define la suma asegurada para cada una"
- 4 grupos colapsables (accordion) con chevron ▼/▶:

GRUPO 1 — "Coberturas base" (EXPANDIDO por defecto):
- Header del grupo: "Coberturas base" en negrita + chip verde "Recomendadas" a la derecha
- 4 filas de cobertura:
  * "Incendio de edificio" — checkbox MARCADO + badge verde "Recomendado" + campo "$" NumberInput valor "2,500,000" (suma asegurada requerida)
  * "Incendio de contenido" — checkbox MARCADO + badge verde "Recomendado" + campo "$" NumberInput valor "800,000"
  * "Cristales" — checkbox NO marcado (sin suma asegurada)
  * "Anuncios luminosos" — checkbox NO marcado (sin suma asegurada)

GRUPO 2 — "Catástrofes naturales" (COLAPSADO):
- Header: "Catástrofes naturales" con contador en gris "0 de 4 seleccionadas"
- (No expandido — solo mostrar el header con chevron ▶)

GRUPO 3 — "Complementarias" (COLAPSADO):
- Header: "Complementarias" con contador en gris "0 de 3 seleccionadas"

GRUPO 4 — "Especiales" (COLAPSADO):
- Header: "Especiales" con contador en gris "0 de 3 seleccionadas"

FOOTER DEL DRAWER:
- Botón ghost "← Atrás" (izquierda) — regresa al Step 1
- Botón primario "Guardar ubicación" (derecha) con ícono save

ESTILO:
- Drawer con sombra lateral sobre la grilla (grilla visible en el fondo con overlay oscuro suave)
- Tabs con "Datos del inmueble" con ícono ✓ en verde indicando completado
- Resumen de Step 1 en banda gris muy suave (#f8fafc) con tipografía pequeña
- Grupos accordion con borde izquierdo sutil, hover state al pasar cursor
- Filas de cobertura con fondo blanco, separador entre filas
- Coberturas pre-seleccionadas (Incendio de edificio, Incendio de contenido) con fondo azul muy suave (#f0f4ff) para distinguirlas visualmente
- Suma asegurada: inputs de número con prefijo "$" y formato con separador de miles
- Badge "Recomendado" en verde (no amber), no en rojo
- Contador "2 seleccionadas" en badge azul prominente
- WCAG AA, tipografía legible, contraste mínimo 4.5:1
```

### Pantalla 4 — Resumen de validacion con alertas (estado del folio)

```
Diseña el panel de resumen de validación de ubicaciones para un sistema de cotización de seguros de daños en México. Es una vista de estado completa que muestra cuántas ubicaciones están listas y cuáles necesitan datos adicionales.

CONTEXTO DE NEGOCIO:
El agente de seguros ha capturado 4 ubicaciones. Antes de continuar al paso de coberturas y cálculo, el sistema muestra un resumen claro del estado de cada ubicación. El objetivo es que el agente entienda de un vistazo qué está listo y qué necesita atención, sin que el estado "pendiente" se perciba como un error grave.

LAYOUT:
- Página completa (misma estructura que la grilla de ubicaciones)
- Wizard header con Paso 2 activo
- Título: "Resumen de ubicaciones" con subtítulo "Revisa el estado antes de continuar"
- Contador principal prominente: dos métricas lado a lado en tarjetas:
  - Tarjeta verde: "3 Calculables — Listas para cotizar"
  - Tarjeta ámbar: "1 Pendiente — Requiere datos"

LISTA DE UBICACIONES CON ESTADO:
Mostrar 4 tarjetas de ubicación apiladas verticalmente, cada una con:
- Número de índice y nombre de ubicación (columna izquierda, negrita)
- Badge de estado (columna derecha)
- Si tiene alertas: lista expandida de alertas en texto pequeño ámbar bajo el nombre

DATOS DE EJEMPLO:
- Ubicación 1: "Bodega Central CDMX" | badge verde "● Calculable" | Sin alertas
- Ubicación 2: "Sucursal Del Valle" | badge verde "● Calculable" | Sin alertas
- Ubicación 3: "Oficinas Corporativas Polanco" | badge verde "● Calculable" | Sin alertas
- Ubicación 4: "Almacén Norte Ecatepec" | badge ámbar "◐ Datos pendientes" | Alertas: "• Código postal requerido" y "• Giro comercial requerido" en texto ámbar

COMPONENTES ESPECÍFICOS:
- Banner informativo azul suave en la parte inferior de la lista: "Las ubicaciones con datos pendientes no se incluirán en el cálculo de tarifa. Puedes continuar y completarlas después."
- Botón de acción secundario junto a la ubicación 4: "Completar datos →" en texto azul (link-style button)
- Dos botones en el footer:
  - Izquierda: botón ghost "← Volver a ubicaciones"
  - Derecha: botón primario "Continuar con 3 ubicaciones →" (especificando cuántas se incluirán)

ESTILO:
- Tarjetas con sombra suave, bordes redondeados
- Tarjeta de ubicación pendiente (Almacén Norte) tiene borde izquierdo ámbar de 3px (no rojo)
- Métricas superiores prominentes para comunicar estado de un vistazo
- Paleta profesional, no alarmante
- El estado pendiente debe verse como "trabajo por terminar", no como "error crítico"
- WCAG AA, tipografía legible
```
