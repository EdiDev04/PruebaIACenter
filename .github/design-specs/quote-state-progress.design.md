# Design Spec: Estado y Progreso de la Cotizacion (SPEC-008)

> **Status:** `APPROVED`
> **Feature:** `quote-state-progress`
> **Spec origen:** `.github/specs/quote-state-progress.spec.md`
> **Pantallas Stitch:** 2 (WizardLayout con ProgressBar, Panel LocationAlerts)
> **Fecha:** 2026-03-29

---

## Seccion 1 -- Data a UI Mapping

### 1.1 Entidad consumida

No crea entidades nuevas. Consume `QuoteStateDto` retornado por `GET /v1/quotes/{folio}/state`.

### 1.2 Cluster Analysis

Los datos del `QuoteStateDto` se agrupan en **3 clusters funcionales** basados en cohesion visual:

| Cluster | Campos | Widget destino | Razon |
|---------|--------|---------------|-------|
| **C1: Identidad del folio** | `folioNumber`, `quoteStatus`, `version` | WizardHeader (breadcrumb + badge estado) | El usuario necesita anchor visual constante de en que folio esta trabajando |
| **C2: Progreso por seccion** | `progress.generalInfo`, `progress.layoutConfiguration`, `progress.locations`, `progress.coverageOptions` | ProgressBar (barra horizontal 4 secciones) | Los 4 booleanos mapean 1:1 a secciones visuales del wizard |
| **C3: Estado de ubicaciones** | `locations.total`, `locations.calculable`, `locations.incomplete`, `locations.alerts[]` | LocationAlerts (panel informativo) + WizardHeader badge | Agrupacion natural: resumen numerico + detalle de alertas |

### 1.3 Mapeo campo a componente

| Campo DTO | Tipo | Componente UI | Variante visual | Razon de diseno |
|-----------|------|--------------|-----------------|-----------------|
| `folioNumber` | string | `Breadcrumb` (texto ancla) | Texto monospace, siempre visible | Anchor visual -- el usuario siempre sabe en que folio trabaja |
| `quoteStatus` | enum (3 valores) | `Badge` / `StatusIndicator` | `draft`=gris, `in_progress`=azul, `calculated`=verde | 3 opciones mutuamente excluyentes, solo lectura |
| `version` | number | No visible en UI | Oculto (uso interno para optimistic locking) | No aporta al usuario, solo al sistema |
| `progress.generalInfo` | boolean | `StepIndicator` con checkmark | Check verde si true, circulo ambar si false | Booleano binario: completo/pendiente |
| `progress.layoutConfiguration` | boolean | `StepIndicator` con checkmark | Check verde si true, circulo ambar si false | Siempre true (defaults), pero se muestra consistente |
| `progress.locations` | boolean | `StepIndicator` con checkmark | Check verde si true, circulo ambar si false | Indica si hay al menos 1 ubicacion registrada |
| `progress.coverageOptions` | boolean | `StepIndicator` con checkmark | Check verde si true, circulo ambar si false | Indica si hay garantias habilitadas |
| `locations.total` | number | `Badge` numerico en header | Texto: "X ubicaciones" | Contexto rapido sin abrir panel |
| `locations.calculable` | number | `Badge` semantico en header | Verde: "X listas para calcular" | Feedback positivo -- cuantas estan OK |
| `locations.incomplete` | number | `Badge` semantico en header | Ambar: "X con datos pendientes" | Informativo, NO rojo (incompleto != error) |
| `locations.alerts[].locationName` | string | `AlertItem` titulo | Texto semibold | Identificador rapido de cual ubicacion |
| `locations.alerts[].missingFields` | string[] | `AlertItem` lista de chips | Chips ambar con nombres legibles | Detalle de que falta sin jerga tecnica |
| `locations.alerts[].index` | number | `AlertItem` link/boton | "Ir a editar" con navegacion | Accionable: click lleva a la ubicacion |
| `readyForCalculation` | boolean | `StatusBanner` o indicador en ProgressBar | Verde: "Listo para calcular" / Ambar: "Complete datos para calcular" | Respuesta directa a la pregunta del usuario: "puedo calcular ya?" |
| `calculationResult` | object/null | No aplica en este design spec | Se consume en SPEC-009 (pantalla de resultados) | Fuera de alcance de los widgets de progreso |

### 1.4 Traduccion de campos tecnicos a lenguaje usuario

| Campo tecnico (`missingFields`) | Label en UI (espanol) |
|--------------------------------|----------------------|
| `zipCode` | Codigo postal |
| `businessLine.fireKey` | Giro de negocio (clave incendio) |
| `businessLine.earthquakeKey` | Giro de negocio (clave sismo) |
| `constructionType` | Tipo de construccion |
| `numberOfFloors` | Numero de pisos |
| `insuredValues.buildingValue` | Valor del inmueble |
| `insuredValues.contentsValue` | Valor de contenidos |

---

## Seccion 2 -- Behavioral Annotations

### 2.1 Principios conductuales aplicados

| Principio | Aplicacion concreta | Justificacion |
|-----------|-------------------|---------------|
| **Goal Gradient Effect** | La barra de progreso con checkmarks crea sensacion de avance incremental. A medida que el usuario completa secciones, la barra se llena visualmente | Los usuarios aceleran su esfuerzo cuando perciben que estan cerca de completar. 4 secciones con checkmarks dan feedback tangible de progreso |
| **Zeigarnik Effect** | Las secciones pendientes (sin checkmark) crean tension cognitiva que motiva al usuario a completarlas | Las personas recuerdan tareas incompletas mejor que las completadas. El indicador visual de "pendiente" aprovecha esta tendencia |
| **Miller's Law** | El progreso se divide en exactamente 4 secciones (dentro del rango 4 +/- 1 de items en memoria de trabajo) | 4 secciones son faciles de recordar y trackear mentalmente sin sobrecarga cognitiva |
| **Hick's Law** | El panel de alertas presenta una accion unica por alerta ("Ir a editar") en vez de multiples opciones | Reducir opciones por item acelera la toma de decision. Una alerta = una accion |
| **Loss Aversion (invertida)** | El badge dice "X listas para calcular" (framing positivo) en vez de "X faltan datos" | Framing positivo es menos amenazante para agentes de seguros que procesan muchas cotizaciones al dia. No queremos generar ansiedad |
| **Signal Detection Theory** | Incompleto = ambar (warning informativo), Error real = rojo (danger). Dos senales distintas para dos estados distintos | Si todo es rojo, el usuario aprende a ignorar las alertas (cry wolf effect). Separar severidades mantiene la credibilidad del sistema de alertas |

### 2.2 Progressive disclosure

Este feature NO requiere progressive disclosure en formularios (no hay formularios). La disclosure se aplica al **nivel de detalle de alertas**:

| Nivel | Que muestra | Cuando se ve |
|-------|------------|-------------|
| **L1: Badge en header** | "2 listas para calcular, 1 con datos pendientes" | Siempre visible en WizardHeader |
| **L2: Barra de progreso** | 4 secciones con checkmarks | Siempre visible debajo del header |
| **L3: Panel de alertas** | Lista de ubicaciones incompletas con campos faltantes | Expandible/colapsable o en seccion de la pagina de Ubicaciones |

### 2.3 Smart defaults

| Elemento | Default | Razon |
|----------|---------|-------|
| Panel de alertas | Colapsado si 0 alertas, expandido si hay alertas | No ocupar espacio visual cuando todo esta OK |
| Seccion "Layout" en barra de progreso | Siempre con checkmark verde | Los defaults de layout existen desde creacion del folio (RN-008-03) |

### 2.4 Estados de la entidad `QuoteState`

| Estado (`quoteStatus`) | Indicador visual | Mensaje | Accion sugerida |
|------------------------|-----------------|---------|-----------------|
| `draft` | Badge gris "Borrador" | "Cotizacion recien creada. Complete los datos para avanzar." | Ninguna especial |
| `in_progress` | Badge azul "En progreso" | "Cotizacion en proceso. {X} de 4 secciones completadas." | Mostrar barra de progreso con estado actual |
| `calculated` | Badge verde "Calculada" | "Cotizacion calculada. Prima comercial: ${commercialPremium}" | Mostrar resultado financiero (SPEC-009) |

### 2.5 Estados del flag `readyForCalculation`

| Valor | Indicador visual | Mensaje | Posicion |
|-------|-----------------|---------|----------|
| `true` | Banner verde sutil debajo de la barra de progreso | "{calculable} ubicacion(es) lista(s) para calcular" | Visible en todas las paginas del wizard |
| `false` (0 ubicaciones) | Banner ambar sutil | "Agregue al menos una ubicacion completa para poder calcular" | Visible en todas las paginas |
| `false` (hay ubicaciones pero ninguna calculable) | Banner ambar sutil | "Complete los datos de al menos una ubicacion para poder calcular" | Visible en todas las paginas |

---

## Seccion 3 -- Screen Flow + Hierarchy

### 3.1 Wireframe ASCII -- WizardLayout con ProgressBar

```
+============================================================================+
|  WIZARD HEADER                                                              |
|  +------------------------------------------------------------------------+|
|  | [Logo]  DAN-2026-00001  >  Datos Generales              [Borrador]  v5 ||
|  |         ~~~~~~~~~~~~       ~~~~~~~~~~~~~~~               ~~~~~~~~~~     ||
|  |         folio (anchor)     pagina actual                 badge status   ||
|  +------------------------------------------------------------------------+|
|                                                                             |
|  PROGRESS BAR                                                               |
|  +------------------------------------------------------------------------+|
|  |                                                                         ||
|  |  [V] Datos Generales --- [V] Layout --- [ ] Ubicaciones --- [ ] Cobert.||
|  |   ~~~~~ verde ~~~~~   ~~~ verde ~~~   ~~~ ambar/gris ~~~  ~~ ambar ~~  ||
|  |                                                                         ||
|  +------------------------------------------------------------------------+|
|                                                                             |
|  READY-FOR-CALCULATION BANNER (condicional)                                 |
|  +------------------------------------------------------------------------+|
|  | [i] 2 ubicaciones listas para calcular | 1 con datos pendientes        ||
|  +------------------------------------------------------------------------+|
|                                                                             |
|  +------------------------------------------------------------------------+|
|  |                                                                         ||
|  |                    CONTENIDO DE LA PAGINA ACTUAL                        ||
|  |                    (renderizado por cada ruta)                          ||
|  |                                                                         ||
|  |                                                                         ||
|  +------------------------------------------------------------------------+|
|                                                                             |
|  FOOTER NAVEGACION                                                          |
|  +------------------------------------------------------------------------+|
|  |  [< Anterior]                                          [Siguiente >]   ||
|  +------------------------------------------------------------------------+|
+============================================================================+
```

### 3.2 Wireframe ASCII -- Detalle ProgressBar (estados)

```
Estado: folio draft (recien creado)
+-----------------------------------------------------------------------+
|  [ ] Datos Generales --- [V] Layout --- [ ] Ubicaciones --- [ ] Cobert.|
|  gris (pendiente)     verde (auto)   gris (pendiente)   gris (pend.) |
+-----------------------------------------------------------------------+

Estado: folio in_progress (datos generales + 2 ubicaciones)
+-----------------------------------------------------------------------+
|  [V] Datos Generales --- [V] Layout --- [V] Ubicaciones --- [ ] Cobert.|
|  verde (completo)     verde (auto)   verde (completo)   ambar (pend.)|
+-----------------------------------------------------------------------+

Estado: todas las secciones completas
+-----------------------------------------------------------------------+
|  [V] Datos Generales --- [V] Layout --- [V] Ubicaciones --- [V] Cobert.|
|  verde (completo)     verde (completo) verde (completo)  verde (comp.)|
+-----------------------------------------------------------------------+
```

### 3.3 Wireframe ASCII -- Panel LocationAlerts

```
+---------------------------------------------+
| UBICACIONES CON DATOS PENDIENTES        [^] |
| ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ |
|                                             |
| [!] Local sin CP                            |
|     Campos faltantes:                       |
|     [Codigo postal] [Giro (clave incendio)] |
|     ~~~~~~~~ambar chips~~~~~~~~~~           |
|                                [Ir a editar]|
|                                             |
| ------------------------------------------ |
|                                             |
| [!] Nave Industrial Norte                   |
|     Campos faltantes:                       |
|     [Tipo de construccion] [Num. pisos]     |
|                                [Ir a editar]|
|                                             |
+---------------------------------------------+

Estado: sin alertas (todas calculables)
+---------------------------------------------+
| UBICACIONES                             [^] |
| ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ |
|                                             |
|  [V] Todas las ubicaciones estan completas  |
|      y listas para calcular.                |
|                                             |
+---------------------------------------------+

Estado: sin ubicaciones
+---------------------------------------------+
| UBICACIONES                             [^] |
| ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ |
|                                             |
|  [ ] No hay ubicaciones registradas.        |
|      Agregue una ubicacion para continuar.  |
|                                             |
+---------------------------------------------+
```

### 3.4 Jerarquia de informacion (F-pattern)

```
Linea de escaneo 1 (horizontal superior):
  FOLIO -----> PAGINA ACTUAL -----> BADGE ESTADO

Linea de escaneo 2 (horizontal):
  [V] Datos Gen. ---> [V] Layout ---> [ ] Ubicaciones ---> [ ] Cobert.

Linea de escaneo 3 (horizontal):
  "2 ubicaciones listas" -----> "1 con datos pendientes"

Escaneo vertical (izquierda):
  El ojo baja por el borde izquierdo del contenido principal
```

### 3.5 Tabla de interacciones

| Accion del usuario | Resultado visual | API call |
|-------------------|-----------------|----------|
| Navega a cualquier pagina del wizard | ProgressBar se renderiza con datos frescos | `GET /v1/quotes/{folio}/state` (useQuoteStateQuery al montar) |
| Guarda datos generales (mutacion) | ProgressBar actualiza: checkmark verde en "Datos Generales" | Invalidacion de `['quote-state', folio]` tras PUT exitoso |
| Agrega una ubicacion (mutacion) | ProgressBar actualiza: checkmark verde en "Ubicaciones"; badge actualiza conteo | Invalidacion de `['quote-state', folio]` tras PUT exitoso |
| Habilita garantias (mutacion) | ProgressBar actualiza: checkmark verde en "Opciones de Cobertura" | Invalidacion de `['quote-state', folio]` tras PUT exitoso |
| Click "Ir a editar" en alerta de ubicacion | Navega a `/quotes/{folio}/locations?edit={index}` | Ninguna (navegacion client-side) |
| Click en seccion de la barra de progreso | Navega a la pagina correspondiente del wizard | Ninguna (navegacion client-side) |

---

## Seccion 4 -- Component Inventory

### 4.1 Tabla de componentes

| Componente | Capa FSD | Props clave | Tipo de estado | Notas |
|-----------|---------|------------|---------------|-------|
| `ProgressBar` | `widgets/progress-bar` | `progress: ProgressDto`, `currentStep?: string` | Server state (derivado de useQuoteStateQuery) | Barra horizontal con 4 StepIndicators conectados por lineas |
| `StepIndicator` | `shared/ui` (o interno de ProgressBar) | `label: string`, `completed: boolean`, `active: boolean` | Props (stateless) | Circulo con checkmark o numero + label debajo |
| `LocationAlerts` | `widgets/location-alerts` | `alerts: LocationAlertDto[]`, `folio: string`, `total: number`, `calculable: number` | Server state (derivado de useQuoteStateQuery) | Panel colapsable con lista de alertas |
| `AlertItem` | Interno de `LocationAlerts` | `alert: LocationAlertDto`, `folio: string` | Props (stateless) | Una fila de alerta con nombre, chips de campos faltantes, boton editar |
| `ReadyBanner` | `shared/ui` o interno de WizardLayout | `ready: boolean`, `calculable: number`, `incomplete: number`, `total: number` | Props (stateless) | Banner informativo debajo de la barra de progreso |
| `QuoteStatusBadge` | `shared/ui` | `status: QuoteStatus` | Props (stateless) | Badge con color semantico segun estado |
| `WizardHeader` (modificado) | `widgets/` (existente) | Agrega `quoteState: QuoteStateDto` | Server state | Integra breadcrumb con folio + QuoteStatusBadge + conteo ubicaciones |
| `WizardLayout` (modificado) | `app/layouts` (existente) | Sin cambio de props, consume useQuoteStateQuery internamente | Server state | Orquesta ProgressBar + ReadyBanner + LocationAlerts + contenido |

### 4.2 Entity layer

| Archivo | Capa FSD | Contenido |
|---------|---------|-----------|
| `entities/quote-state/model/types.ts` | Entity model | Tipos TS: `QuoteStateDto`, `ProgressDto`, `LocationsStateDto`, `LocationAlertDto`, `CalculationResultDto` |
| `entities/quote-state/model/useQuoteStateQuery.ts` | Entity model | `useQuoteStateQuery(folio)` -- TanStack Query con staleTime: 0 |
| `entities/quote-state/api/quoteStateApi.ts` | Entity API | `getQuoteState(folio)` -- GET /v1/quotes/{folio}/state |
| `entities/quote-state/index.ts` | Public API | Re-exports de types + hook + api |

### 4.3 Zod Schemas

```typescript
// entities/quote-state/model/schemas.ts

import { z } from 'zod';

export const progressSchema = z.object({
  generalInfo: z.boolean(),
  layoutConfiguration: z.boolean(),
  locations: z.boolean(),
  coverageOptions: z.boolean(),
});

export const locationAlertSchema = z.object({
  index: z.number().int().positive(),
  locationName: z.string().min(1),
  missingFields: z.array(z.string()).min(1),
});

export const locationsStateSchema = z.object({
  total: z.number().int().min(0),
  calculable: z.number().int().min(0),
  incomplete: z.number().int().min(0),
  alerts: z.array(locationAlertSchema),
});

export const coveragePremiumSchema = z.object({
  guaranteeKey: z.string(),
  insuredAmount: z.number().min(0),
  rate: z.number().min(0),
  premium: z.number().min(0),
});

export const locationPremiumSchema = z.object({
  locationIndex: z.number().int().positive(),
  locationName: z.string(),
  netPremium: z.number().min(0),
  validationStatus: z.string(),
  coveragePremiums: z.array(coveragePremiumSchema),
});

export const calculationResultSchema = z.object({
  netPremium: z.number().min(0),
  commercialPremiumBeforeTax: z.number().min(0),
  commercialPremium: z.number().min(0),
  premiumsByLocation: z.array(locationPremiumSchema),
});

export const quoteStateSchema = z.object({
  folioNumber: z.string().regex(/^DAN-\d{4}-\d{5}$/),
  quoteStatus: z.enum(['draft', 'in_progress', 'calculated']),
  version: z.number().int().positive(),
  progress: progressSchema,
  locations: locationsStateSchema,
  readyForCalculation: z.boolean(),
  calculationResult: calculationResultSchema.nullable(),
});

// Tipos derivados de los schemas
export type QuoteStateDto = z.infer<typeof quoteStateSchema>;
export type ProgressDto = z.infer<typeof progressSchema>;
export type LocationsStateDto = z.infer<typeof locationsStateSchema>;
export type LocationAlertDto = z.infer<typeof locationAlertSchema>;
export type CalculationResultDto = z.infer<typeof calculationResultSchema>;
```

---

## Seccion 5 -- Validation + Feedback UX

### 5.1 Nota sobre validacion

Este feature es **solo lectura** -- no hay formularios ni inputs del usuario. La "validacion" se refiere a como el sistema comunica estados derivados, no a validacion de campos de entrada.

### 5.2 Matriz de estados visuales

| Elemento | Estado | Indicador visual | Texto | Color |
|----------|--------|-----------------|-------|-------|
| StepIndicator | Completo | Circulo relleno verde + checkmark blanco | "Datos Generales" (bold) | Verde (#22C55E o token semantico success) |
| StepIndicator | Pendiente | Circulo outline ambar + icono reloj/vacio | "Datos Generales" (regular) | Ambar (#F59E0B o token semantico warning) |
| StepIndicator | Activo (pagina actual) | Circulo con borde azul grueso | "Datos Generales" (bold + underline) | Azul (#3B82F6 o token semantico info) |
| Linea conectora entre steps | Completo (ambos lados OK) | Linea solida verde | -- | Verde |
| Linea conectora entre steps | Pendiente | Linea punteada gris | -- | Gris (#D1D5DB) |
| QuoteStatusBadge | draft | Badge pill gris claro | "Borrador" | Gris (#6B7280) |
| QuoteStatusBadge | in_progress | Badge pill azul | "En progreso" | Azul (#3B82F6) |
| QuoteStatusBadge | calculated | Badge pill verde | "Calculada" | Verde (#22C55E) |
| ReadyBanner | Listo | Barra sutil con icono check | "{N} ubicacion(es) lista(s) para calcular" | Verde fondo claro (#F0FDF4) |
| ReadyBanner | No listo (sin ubicaciones) | Barra sutil con icono info | "Agregue al menos una ubicacion completa para poder calcular" | Ambar fondo claro (#FFFBEB) |
| ReadyBanner | No listo (ninguna calculable) | Barra sutil con icono info | "Complete los datos de al menos una ubicacion para poder calcular" | Ambar fondo claro (#FFFBEB) |
| AlertItem | Ubicacion incompleta | Icono warning ambar + nombre bold | Ver wireframe | Ambar (#F59E0B) |
| MissingField chip | Campo faltante | Chip/tag con fondo ambar claro | Nombre legible del campo (ver tabla 1.4) | Ambar claro (#FEF3C7, texto #92400E) |

### 5.3 Estados de carga y error

| Escenario | Indicador visual | Comportamiento |
|-----------|-----------------|---------------|
| Cargando estado (query en flight) | Skeleton de la barra de progreso (4 circulos grises pulsantes + lineas) | Skeleton mantiene el layout para evitar layout shift |
| Error en query (500) | Barra de progreso oculta, banner rojo sutil "No se pudo cargar el estado de la cotizacion. Intente recargar." | No bloquea la pagina -- el contenido principal sigue visible |
| Folio no encontrado (404) | Redirect a pagina de inicio o notificacion "El folio no existe" | Gestionado a nivel de WizardLayout, no del widget de progreso |
| Sin conexion | Usa datos cacheados si existen, o skeleton indefinido | TanStack Query maneja retry automatico |

### 5.4 Accesibilidad WCAG AA

| Requisito | Implementacion |
|-----------|---------------|
| Contraste minimo 4.5:1 | Todos los textos sobre fondos de color (ambar sobre blanco, verde sobre blanco) verificados con ratio >= 4.5:1. Usar tonos oscuros para texto (#92400E sobre #FEF3C7 para ambar) |
| Roles ARIA | `role="progressbar"` en ProgressBar con `aria-valuenow` (secciones completadas) y `aria-valuemax="4"` |
| Labels ARIA | Cada StepIndicator con `aria-label="Datos Generales: completado"` o `"Datos Generales: pendiente"` |
| Alertas live | Panel de LocationAlerts con `role="status"` y `aria-live="polite"` para que lectores de pantalla anuncien cambios |
| Navegacion por teclado | Secciones de la barra de progreso son focusables (Tab) y activables (Enter/Space) |
| Skip link | "Ir al contenido principal" para saltar la barra de progreso si no es relevante |
| Texto alternativo a color | Checkmarks (iconos) y texto "Completado"/"Pendiente" complementan el color verde/ambar |

---

## Seccion 6 -- Stitch Prompts

### Prompt 1: WizardLayout con ProgressBar

```
Disena una pagina completa de wizard para un cotizador profesional de seguros de danos.
La pagina sera usada por agentes de seguros que procesan cotizaciones comerciales e industriales.

LAYOUT GENERAL:
- Fondo gris claro (#F9FAFB), contenido centrado en max-width 1200px
- Sin sidebar -- layout de una sola columna

SECCION 1 -- WIZARD HEADER (barra superior):
- Altura fija ~64px, fondo blanco, sombra sutil inferior (shadow-sm)
- Lado izquierdo: logo placeholder (cuadrado 32x32 gris) + breadcrumb con el folio "DAN-2026-00001"
  en font monospace azul + separador ">" + nombre de pagina actual "Datos Generales" en font regular
- Lado derecho: badge pill que dice "En progreso" con fondo azul claro y texto azul oscuro
- Debajo del breadcrumb (o al lado): un texto pequeno gris "v5"

SECCION 2 -- BARRA DE PROGRESO (debajo del header, fondo blanco, padding 16px):
- Barra horizontal con 4 nodos circulares conectados por lineas
- Cada nodo es un circulo de 32px con un icono adentro
- Los nodos estan equidistribuidos horizontalmente con lineas de conexion entre ellos
- Debajo de cada nodo, un label de texto:
  * Nodo 1: checkmark blanco sobre circulo verde relleno, label "Datos Generales" en bold
  * Nodo 2: checkmark blanco sobre circulo verde relleno, label "Layout" en bold
  * Nodo 3: circulo con borde ambar (#F59E0B) y fondo ambar claro, icono de reloj o vacio, label "Ubicaciones" en regular
  * Nodo 4: circulo con borde gris y fondo gris claro, label "Opciones de Cobertura" en regular gris
- Las lineas entre nodos 1-2 son solidas verdes, entre 2-3 punteadas grises, entre 3-4 punteadas grises
- El nodo 3 (Ubicaciones) tiene un borde azul extra grueso indicando que es la pagina actual

SECCION 3 -- BANNER DE CALCULABILIDAD (debajo de la barra, borde-top 1px gris):
- Banner de altura ~40px, fondo verde muy claro (#F0FDF4), borde izquierdo 3px verde
- Icono de check-circle verde a la izquierda
- Texto: "2 ubicaciones listas para calcular" en verde oscuro (#166534)
- Al lado derecho del mismo banner, separado con pipe: "1 con datos pendientes" en ambar (#92400E)

SECCION 4 -- CONTENIDO PLACEHOLDER:
- Area blanca con border-radius 8px, padding 24px, sombra sm
- Titulo h2: "Ubicaciones de Riesgo"
- Subtitulo gris: "Agregue y configure las ubicaciones a asegurar"
- Debajo, una tabla o grid placeholder con 3 filas representando ubicaciones:
  * Fila 1: "Bodega Central CDMX" - badge verde "Calculable"
  * Fila 2: "Oficinas Monterrey" - badge verde "Calculable"
  * Fila 3: "Local sin CP" - badge ambar "Datos pendientes"

SECCION 5 -- FOOTER DE NAVEGACION:
- Barra inferior sticky, fondo blanco, sombra superior, padding 16px
- Lado izquierdo: boton outline "< Anterior"
- Lado derecho: boton primario azul "Siguiente >"

ESTILO GENERAL:
- Tipografia: Inter o system-ui, tamanos 14px body, 12px labels, 18px titulos
- Espaciado generoso entre secciones (gap 16-24px)
- Bordes redondeados (8px cards, 4px inputs, 999px badges)
- Paleta: azul primario, verde para completado/exito, ambar para pendiente/warning, gris para inactivo
- WCAG AA: contraste minimo 4.5:1 en todos los textos
- NO usar rojo para estados "pendientes" -- rojo es solo para errores reales

Muestra la pagina en estado intermedio: 2 de 4 secciones completadas, pagina actual es Ubicaciones.
```

### Prompt 2: Panel LocationAlerts

```
Disena un panel de alertas informativas para ubicaciones incompletas en un cotizador de seguros de danos.
El panel se mostrara dentro de la pagina de ubicaciones del wizard, debajo del contenido principal o como
seccion lateral. Sera usado por agentes de seguros profesionales.

LAYOUT DEL PANEL:
- Ancho completo (100% del contenedor), max-width 800px
- Fondo blanco, border 1px gris claro (#E5E7EB), border-radius 8px
- Sombra sutil (shadow-sm)

HEADER DEL PANEL:
- Padding 16px, border-bottom 1px gris
- Lado izquierdo: icono de alerta triangular ambar + titulo "Ubicaciones con datos pendientes" en font 16px semibold
- Lado derecho: badge pill "1 pendiente" en ambar + boton chevron para colapsar/expandir

CONTENIDO -- 2 ALERTAS DE EJEMPLO:

Alerta 1:
- Padding 16px, border-bottom 1px gris muy claro
- Linea 1: icono warning ambar (pequeno, 16px) + "Local sin CP" en font 14px semibold negro
- Linea 2 (indentada): texto gris 12px "Campos faltantes:"
- Linea 3 (indentada): dos chips/tags con fondo ambar claro (#FEF3C7), texto ambar oscuro (#92400E),
  border-radius 4px, padding 4px 8px:
  * "Codigo postal"
  * "Giro de negocio (clave incendio)"
- Linea 4 (alineada a la derecha): link/boton terciario azul "Ir a editar >" con hover underline

Alerta 2:
- Mismo formato que Alerta 1
- Nombre: "Nave Industrial Norte"
- Chips: "Tipo de construccion", "Numero de pisos"
- Link "Ir a editar >"

ESTADO ALTERNATIVO -- SIN ALERTAS (mostrar debajo del panel con alertas):
- Misma estructura de panel pero contenido diferente
- Icono check-circle verde + "Todas las ubicaciones estan completas y listas para calcular"
  en verde oscuro (#166534), fondo verde claro (#F0FDF4)
- Sin boton de colapsar (no hay nada que detallar)

ESTADO ALTERNATIVO -- SIN UBICACIONES (mostrar debajo del anterior):
- Panel con fondo gris muy claro (#F9FAFB)
- Icono info gris + "No hay ubicaciones registradas. Agregue una ubicacion para continuar."
  en gris (#6B7280)

ESTILOS:
- Tipografia Inter o system-ui
- Colores: ambar para warnings (#F59E0B iconos, #FEF3C7 fondos, #92400E textos),
  verde para exito (#22C55E iconos, #F0FDF4 fondo, #166534 texto),
  gris para vacio (#6B7280 texto, #F9FAFB fondo)
- NUNCA usar rojo -- "datos pendientes" NO es un error, es un estado informativo
- WCAG AA: todos los textos con contraste >= 4.5:1
- Chips de campos faltantes deben ser legibles y no truncados
- El link "Ir a editar" debe tener focus visible (outline azul) para accesibilidad por teclado
- Todo el texto en espanol

Muestra los 3 estados del panel: con alertas, sin alertas, sin ubicaciones. Apilarlos verticalmente
con separacion de 32px para que se vean los 3 estados en una sola pantalla.
```

---

## Anexo A -- Stitch Generation Config

```json
{
  "feature": "quote-state-progress",
  "screens": [
    {
      "key": "quote-state-progress/wizard-layout-progressbar",
      "title": "WizardLayout con ProgressBar",
      "promptSection": "Prompt 1",
      "model": "GEMINI_3_FLASH",
      "designSystemApply": true
    },
    {
      "key": "quote-state-progress/panel-location-alerts",
      "title": "Panel LocationAlerts",
      "promptSection": "Prompt 2",
      "model": "GEMINI_3_FLASH",
      "designSystemApply": true
    }
  ],
  "projectId": "3802726589783048530",
  "designSystemAssetId": "4588898471681033353"
}
```

---

## Pantallas Stitch generadas

| Pantalla | screenId | Archivo HTML de referencia | Estado |
|---|---|---|---|
| WizardLayout con ProgressBar | `3c93731b4459409eb9a9ac79e33cedd6` | `screens/quote-state-progress/wizard-layout-progressbar.html` | Aprobado |
| Panel de Alertas de Ubicaciones | `b4d5f5f6d1b842c6b1c2841d1396b473` | `screens/quote-state-progress/panel-location-alerts.html` | Aprobado |

Design system aplicado: `4588898471681033353` | Proyecto Stitch: `3802726589783048530`

---

## Notas de implementacion para el frontend-developer

1. **Leer los `.html` de referencia** en `screens/quote-state-progress/` como guia visual — NO copiar verbatim; adaptar a React + CSS Modules + FSD.
2. **Tipos TypeScript** deben inferirse desde los Zod schemas de la Seccion 4.3.
3. **El `WizardLayout`** consume `useQuoteStateQuery(folio)` internamente; el `folio` se extrae con `useParams()`.
4. **Nodo activo del ProgressBar** se determina con `useLocation().pathname` — comparar contra rutas de cada seccion.
5. **Invalidacion de queries**: agregar `queryClient.invalidateQueries(['quote-state', folio])` en `onSuccess` de cada mutacion de datos del wizard (datos generales, ubicaciones, coberturas).
6. **Mapeo de campos tecnicos a espanol**: crear helper `getMissingFieldLabel(field: string): string` en `shared/lib/` — NO poner la logica de mapeo en el componente.
7. **Panel de alertas**: usar `useState(expanded)` inicializado en `alerts.length > 0`.
8. **NUNCA** usar color rojo para estado "pendiente" — rojo solo para errores reales (ver Seccion 5.2).
9. **WCAG AA**: area minima de click 44x44px en todos los nodos del ProgressBar y filas de alerta.

---

```yaml
status: APPROVED
```
