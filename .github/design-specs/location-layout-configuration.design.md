# Design Spec: Configuracion del Layout de Ubicaciones

> **SPEC:** SPEC-005
> **Feature:** location-layout-configuration
> **Status:** DRAFT
> **Creado:** 2026-03-29
> **Autor:** ux-designer

---

## Seccion 1 -- Data -> UI Mapping

### Entidad: `LayoutConfiguration`

| Campo | Tipo | Componente UI | Tipo input | Razon de diseno |
|---|---|---|---|---|
| `displayMode` | Enum: "grid" / "list" | `SegmentedControl` | Seleccion unica | 2 opciones mutuamente excluyentes. Segmented control ofrece feedback instantaneo y menor carga cognitiva que radio buttons para toggles binarios |
| `visibleColumns` | `string[]` (multi-seleccion, 15 opciones) | `CheckboxGroup` agrupado en 4 secciones | Multi-seleccion | 15 opciones requieren agrupacion cognitiva (Miller's Law). 4 categorias semanticas reducen la carga cognitiva |
| `version` | `number` (interno) | No visible | Hidden | Versionado optimista -- transparente para el usuario |

### Columnas disponibles agrupadas por cohesion funcional

**Grupo 1 -- Identificacion (3 columnas)**
| Columna | Label UI | Default visible |
|---|---|---|
| `index` | # (Numero) | Si |
| `locationName` | Nombre de ubicacion | Si |
| `address` | Direccion | No |

**Grupo 2 -- Ubicacion geografica (5 columnas)**
| Columna | Label UI | Default visible |
|---|---|---|
| `zipCode` | Codigo postal | Si |
| `state` | Estado | No |
| `municipality` | Municipio | No |
| `neighborhood` | Colonia | No |
| `city` | Ciudad | No |

**Grupo 3 -- Caracteristicas del inmueble (3 columnas)**
| Columna | Label UI | Default visible |
|---|---|---|
| `constructionType` | Tipo constructivo | No |
| `level` | Nivel | No |
| `constructionYear` | Ano de construccion | No |

**Grupo 4 -- Clasificacion y estado (4 columnas)**
| Columna | Label UI | Default visible |
|---|---|---|
| `businessLine` | Giro comercial | Si |
| `guarantees` | Coberturas | No |
| `catZone` | Zona catastrofica | No |
| `validationStatus` | Estado de validacion | Si |

---

## Seccion 2 -- Behavioral Annotations

### Principios conductuales aplicados

| Principio | Aplicacion | Justificacion |
|---|---|---|
| **Smart defaults** | 5 columnas pre-seleccionadas (index, locationName, zipCode, businessLine, validationStatus) | Reduce decision fatigue. Las 5 columnas por defecto cubren el 80% de la informacion que un agente necesita para una vista rapida |
| **Progressive disclosure** | Las 15 columnas se organizan en 4 grupos colapsables. Solo el grupo "Identificacion" y "Clasificacion y estado" estan expandidos por defecto | Evita abrumar con 15 checkboxes planos. El agente ve primero las columnas mas relevantes |
| **Miller's Law** | 4 grupos de columnas (3+5+3+4) | El cerebro procesa 4 chunks mejor que 15 items individuales |
| **Feedback inmediato** | Al cambiar el display mode o las columnas, la grilla se actualiza visualmente en tiempo real (preview en vivo) | Reduce incertidumbre. El usuario ve el efecto de su configuracion antes de guardar |
| **Reconocimiento sobre memoria** | Cada columna tiene un label descriptivo en espanol, no el nombre tecnico del campo | El agente no necesita recordar que "businessLine" significa "Giro comercial" |
| **Tolerancia al error** | Minimo 1 columna debe estar seleccionada. Si el usuario intenta deseleccionar la ultima, se muestra mensaje preventivo y se bloquea la accion | Previene estado invalido sin permitir que el usuario llegue a un error |

### Modelo de interaccion

Este panel es un **configurador auxiliar** integrado en la pagina de ubicaciones (Step 2 del wizard). No es un paso independiente.

**Patron de activacion:**
1. El usuario ve un boton/icono de configuracion (engranaje) en la barra de herramientas de la grilla de ubicaciones
2. Al hacer clic, se abre un **popover o panel lateral** con las opciones de layout
3. Los cambios se aplican en tiempo real a la grilla (preview)
4. El guardado se ejecuta con un boton "Guardar configuracion" dentro del panel
5. Al cerrar el panel sin guardar, se revierten los cambios visuales

**Guardado:**
- Boton explicito "Guardar configuracion" (no auto-save)
- Razon: auto-save genera llamadas PUT innecesarias mientras el usuario experimenta con opciones
- Al guardar exitosamente: toast de confirmacion "Configuracion de vista guardada"
- Conflicto de version (409): alerta con boton "Recargar" que hace GET fresh

### Estados del panel

| Estado | Indicador visual | Mensaje | Accion disponible |
|---|---|---|---|
| Cargando | Skeleton del panel con 2 secciones | -- | Ninguna (panel deshabilitado) |
| Configuracion por defecto | Badge "Por defecto" junto al titulo | "Vista predeterminada" | Modificar |
| Configuracion personalizada | Badge "Personalizada" junto al titulo | "Vista personalizada" | Modificar, Restaurar defaults |
| Guardando | Spinner en boton "Guardar" | "Guardando..." | Ninguna (boton deshabilitado) |
| Error de conflicto | Alerta ambar | "El folio fue modificado por otro proceso. Recargue para continuar" | Recargar |
| Error de red | Alerta roja | "No se pudo guardar la configuracion. Intente de nuevo" | Reintentar |

---

## Seccion 3 -- Screen Flow + Hierarchy

### Wireframe ASCII -- Panel de configuracion de layout

```
+------------------------------------------------------------------+
| Ubicaciones del folio DAN-2026-00001                        [<>]  |
|                                                              |    |
|  [Grilla de ubicaciones...]                                  |    |
|                                                              |    |
|  +-- Panel Config (popover/drawer derecho) ----------------+ |    |
|  | Configurar vista                    [Badge: Por defecto]| |    |
|  |                                                         | |    |
|  | Modo de visualizacion                                   | |    |
|  | [====Grilla====] [    Lista    ]   <- SegmentedControl  | |    |
|  |                                                         | |    |
|  | Columnas visibles                                       | |    |
|  |                                                         | |    |
|  | v Identificacion                                        | |    |
|  |   [x] # (Numero)                                       | |    |
|  |   [x] Nombre de ubicacion                              | |    |
|  |   [ ] Direccion                                         | |    |
|  |                                                         | |    |
|  | > Ubicacion geografica (1 de 5)                         | |    |
|  |                                                         | |    |
|  | > Caracteristicas del inmueble (0 de 3)                 | |    |
|  |                                                         | |    |
|  | v Clasificacion y estado                                | |    |
|  |   [x] Giro comercial                                   | |    |
|  |   [ ] Coberturas                                        | |    |
|  |   [ ] Zona catastrofica                                 | |    |
|  |   [x] Estado de validacion                              | |    |
|  |                                                         | |    |
|  | 5 columnas seleccionadas                                | |    |
|  |                                                         | |    |
|  | [Restaurar predeterminados]  [Guardar configuracion]    | |    |
|  +---------------------------------------------------------+ |    |
+------------------------------------------------------------------+
```

### Jerarquia de informacion (F-pattern)

1. **Titulo del panel** + Badge de estado (por defecto / personalizada)
2. **Modo de visualizacion** -- SegmentedControl prominente
3. **Columnas visibles** -- grupos expandibles con checkboxes
4. **Counter** de columnas seleccionadas
5. **Acciones** -- Restaurar | Guardar

### Tabla de interacciones

| Accion del usuario | Resultado visual | API call |
|---|---|---|
| Clic en icono engranaje | Abre panel de configuracion como popover/drawer | `GET /v1/quotes/{folio}/locations/layout` |
| Toggle SegmentedControl grid/list | Grilla cambia de modo en preview | Ninguna (local) |
| Marcar/desmarcar checkbox de columna | Columna aparece/desaparece en preview de grilla | Ninguna (local) |
| Intentar desmarcar ultima columna | Checkbox no se desmarca + tooltip "Debe haber al menos una columna visible" | Ninguna |
| Clic "Restaurar predeterminados" | Checkboxes vuelven a los 5 defaults, modo vuelve a grid | Ninguna (local) |
| Clic "Guardar configuracion" | Spinner en boton, luego toast "Configuracion guardada" | `PUT /v1/quotes/{folio}/locations/layout` |
| Cerrar panel sin guardar | Grilla revierte a la configuracion guardada | Ninguna |
| Error 409 en guardado | Alerta ambar con boton "Recargar" | -- |
| Clic "Recargar" en alerta 409 | Panel recarga datos frescos del servidor | `GET /v1/quotes/{folio}/locations/layout` |

---

## Seccion 4 -- Component Inventory

### Tabla de componentes

| Componente | Capa FSD | Props clave | Tipo de estado |
|---|---|---|---|
| `LayoutConfigPanel` | `widgets/layout-config` | `folio: string` | Compuesto: server state (query) + form state local |
| `DisplayModeToggle` | `shared/ui` | `value: "grid" \| "list"`, `onChange: (mode) => void` | Controlado por padre |
| `ColumnGroupCheckbox` | `shared/ui` | `groupLabel: string`, `columns: Column[]`, `selected: string[]`, `onChange: (cols) => void`, `defaultExpanded: boolean` | Controlado por padre |
| `ColumnCounter` | `shared/ui` | `count: number`, `total: number` | Derivado (solo lectura) |
| `useLayoutQuery` | `entities/layout` | `folio: string` | Server state (TanStack Query) |
| `useSaveLayout` | `features/save-layout` | `folio: string` | Mutation (TanStack Query) |

### Zod Schemas

```typescript
// entities/layout/model/layoutSchema.ts

import { z } from 'zod';

const VALID_COLUMNS = [
  'index', 'locationName', 'address', 'zipCode', 'state',
  'municipality', 'neighborhood', 'city', 'constructionType',
  'level', 'constructionYear', 'businessLine', 'guarantees',
  'catZone', 'validationStatus'
] as const;

const DISPLAY_MODES = ['grid', 'list'] as const;

// Schema completo para la respuesta GET
export const layoutConfigurationSchema = z.object({
  displayMode: z.enum(DISPLAY_MODES),
  visibleColumns: z.array(z.enum(VALID_COLUMNS)).min(1, 'Debe seleccionar al menos una columna visible'),
  version: z.number().int().positive(),
});

// Schema para el request PUT (identico estructura, pero semanticamente es el input del form)
export const updateLayoutRequestSchema = z.object({
  displayMode: z.enum(DISPLAY_MODES),
  visibleColumns: z.array(z.enum(VALID_COLUMNS)).min(1, 'Debe seleccionar al menos una columna visible'),
  version: z.number().int().positive(),
});

// Tipos derivados
export type LayoutConfiguration = z.infer<typeof layoutConfigurationSchema>;
export type UpdateLayoutRequest = z.infer<typeof updateLayoutRequestSchema>;
```

---

## Seccion 5 -- Validation + Feedback UX

### Matriz de validacion

| Campo | Trigger | Feedback positivo | Feedback error |
|---|---|---|---|
| `displayMode` | Cambio de segmento | Grilla cambia de modo en tiempo real (preview) | N/A (no puede tener estado invalido -- siempre es uno de los dos) |
| `visibleColumns` | Toggle de checkbox | Columna aparece/desaparece en preview, counter se actualiza | "Debe haber al menos una columna visible" (tooltip en el checkbox que se intento desmarcar) |
| `version` | Al guardar (PUT) | Toast "Configuracion de vista guardada" | Alerta ambar "El folio fue modificado por otro proceso. Recargue para continuar" |

### Estados visuales

| Estado | Visual |
|---|---|
| Panel cerrado | Solo se ve el icono de engranaje en la toolbar de la grilla |
| Panel abierto, config default | Badge azul "Por defecto" junto al titulo |
| Panel abierto, config personalizada | Badge gris "Personalizada" junto al titulo |
| Checkbox marcado | Checkbox lleno con color primario |
| Checkbox desmarcado | Checkbox vacio, borde gris |
| Ultimo checkbox (protegido) | Si se intenta desmarcar: tooltip de advertencia, checkbox permanece marcado |
| Guardando | Boton "Guardar" muestra spinner y texto "Guardando...", deshabilitado |
| Guardado exitoso | Toast verde esquina superior derecha, 3 segundos, "Configuracion de vista guardada" |
| Error 409 | Alerta inline ambar dentro del panel con boton "Recargar" |
| Error de red | Alerta inline roja dentro del panel con boton "Reintentar" |

### Tratamiento del conflicto de version (409)

Este es el unico error critico del feature. Tratamiento:
1. Mostrar alerta **ambar** (no rojo -- es un conflicto, no un error del usuario)
2. Mensaje: "El folio fue modificado por otro proceso. Recargue para continuar"
3. Boton "Recargar" que ejecuta GET fresh y resetea el formulario
4. Mientras la alerta esta visible, el boton "Guardar" queda deshabilitado
5. Al recargar, la alerta desaparece y el usuario puede continuar editando

---

## Seccion 6 -- Stitch Prompts

### Prompt 1: Panel de configuracion de layout (estado normal)

```
Disena un panel de configuracion de vista para una grilla de ubicaciones de un cotizador profesional de seguros de danos. El panel aparece como un popover o drawer lateral derecho, anclado a un boton de engranaje en la toolbar de la grilla.

CONTEXTO:
- El usuario es un agente de seguros profesional que configura como ve la lista de ubicaciones de riesgo
- El panel es auxiliar (no es un paso del wizard), se abre sobre la pagina de ubicaciones
- Ancho del panel: 360px aproximadamente
- Fondo blanco con sombra lateral sutil

ESTRUCTURA DEL PANEL:
1. HEADER: Titulo "Configurar vista" con un badge azul claro que dice "Por defecto" a la derecha. Boton X para cerrar en la esquina superior derecha.

2. MODO DE VISUALIZACION: Subtitulo "Modo de visualizacion". Debajo, un SegmentedControl con dos opciones: icono de grilla + "Grilla" (seleccionado, fondo azul) y icono de lista + "Lista" (no seleccionado, fondo gris claro).

3. COLUMNAS VISIBLES: Subtitulo "Columnas visibles" con un counter gris "(5 de 15)".

   Cuatro grupos expandibles tipo accordion:

   a) "Identificacion" (expandido):
      - [x] # (Numero)
      - [x] Nombre de ubicacion
      - [ ] Direccion

   b) "Ubicacion geografica" con indicador "(1 de 5)" (colapsado):
      Solo muestra el titulo del grupo con chevron para expandir

   c) "Caracteristicas del inmueble" con indicador "(0 de 3)" (colapsado):
      Solo muestra el titulo del grupo con chevron para expandir

   d) "Clasificacion y estado" (expandido):
      - [x] Giro comercial
      - [ ] Coberturas
      - [ ] Zona catastrofica
      - [x] Estado de validacion

4. FOOTER DEL PANEL: Dos botones alineados horizontalmente.
   - Izquierda: boton texto/link "Restaurar predeterminados"
   - Derecha: boton primario "Guardar configuracion"

ESTILO:
- Tipografia clara, sans-serif, tamano 14px para labels de checkbox, 12px para counters
- Espaciado generoso entre grupos (16px)
- Checkboxes con tamano touch-friendly (min 20x20px area de clic)
- Colores sobrios profesionales: azul para selecciones activas, gris para inactivos
- WCAG AA: contraste minimo 4.5:1 en todos los textos
- Bordes redondeados suaves (8px) en el panel
- Separador horizontal sutil entre las secciones

DATOS DE EJEMPLO:
- El panel muestra la configuracion por defecto: modo Grilla, 5 columnas seleccionadas
- Los grupos colapsados muestran cuantas columnas estan seleccionadas dentro

ACCESIBILIDAD:
- Todos los checkboxes tienen labels asociados
- El SegmentedControl es navegable con teclado (flechas izquierda/derecha)
- Los grupos accordion son expandibles con Enter/Space
- Role="dialog" para el panel con aria-label="Configurar vista de ubicaciones"
```

### Prompt 2: Panel con configuracion personalizada y todos los grupos expandidos

```
Disena el mismo panel de configuracion de vista pero en estado personalizado con todos los grupos expandidos. El usuario ha modificado la configuracion.

CONTEXTO:
- Cotizador profesional de seguros de danos
- Panel lateral derecho de 360px, fondo blanco, sombra lateral
- El usuario ha cambiado a modo Lista y ha seleccionado 8 columnas

ESTRUCTURA DEL PANEL:
1. HEADER: Titulo "Configurar vista" con badge gris que dice "Personalizada". Boton X para cerrar.

2. MODO DE VISUALIZACION: SegmentedControl con "Lista" seleccionado (fondo azul, icono de lista) y "Grilla" no seleccionado.

3. COLUMNAS VISIBLES: Counter "(8 de 15)".

   Todos los grupos expandidos:

   a) "Identificacion":
      - [x] # (Numero)
      - [x] Nombre de ubicacion
      - [x] Direccion

   b) "Ubicacion geografica" con indicador "(3 de 5)":
      - [x] Codigo postal
      - [x] Estado
      - [ ] Municipio
      - [ ] Colonia
      - [x] Ciudad

   c) "Caracteristicas del inmueble" con indicador "(1 de 3)":
      - [x] Tipo constructivo
      - [ ] Nivel
      - [ ] Ano de construccion

   d) "Clasificacion y estado":
      - [ ] Giro comercial
      - [ ] Coberturas
      - [ ] Zona catastrofica
      - [x] Estado de validacion

4. FOOTER: "Restaurar predeterminados" (link texto) | "Guardar configuracion" (boton primario azul)

ESTILO:
- Misma paleta y tipografia que el panel por defecto
- El badge "Personalizada" es gris oscuro sobre fondo gris claro (para distinguirlo del "Por defecto" que es azul)
- Los grupos expandidos muestran todos los checkboxes con spacing de 12px entre ellos
- El counter general "(8 de 15)" usa tipografia semi-bold
- WCAG AA obligatorio
- Sin scroll si cabe, con scroll interno suave si el contenido excede la altura del viewport

DATOS DE EJEMPLO:
- Modo: Lista
- 8 columnas seleccionadas distribuidas entre los 4 grupos
- Refleja una configuracion realista de un agente que quiere ver mas detalle geografico
```

### Prompt 3: Estado de error -- Conflicto de version

```
Disena el panel de configuracion de vista mostrando un estado de error por conflicto de version. El panel aparece como drawer lateral derecho.

CONTEXTO:
- Cotizador profesional de seguros de danos
- El usuario intento guardar su configuracion pero otro proceso modifico el folio
- El panel debe mostrar una alerta clara pero NO alarmante (es un conflicto, no un error grave)

ESTRUCTURA DEL PANEL:
1. HEADER: "Configurar vista" con badge "Personalizada" gris

2. ALERTA DE CONFLICTO (prominente, debajo del header):
   - Fondo ambar claro (NO rojo -- incompleto/conflicto no es error)
   - Icono de advertencia (triangulo con exclamacion) en ambar oscuro
   - Texto: "El folio fue modificado por otro proceso. Recargue para continuar"
   - Boton dentro de la alerta: "Recargar" (boton outline ambar)

3. CONTENIDO DEL PANEL (deshabilitado visualmente):
   - SegmentedControl con opacidad reducida (0.5)
   - Checkboxes con opacidad reducida (0.5)
   - Todo no interactuable mientras el conflicto esta activo

4. FOOTER:
   - "Restaurar predeterminados" deshabilitado (gris)
   - "Guardar configuracion" deshabilitado (gris, no clickeable)

ESTILO:
- La alerta ambar es el foco visual principal (F-pattern: primera linea de escaneo)
- Color ambar: fondo #FFF8E1, borde #F9A825, texto #E65100
- El resto del panel se ve "apagado" con opacidad reducida
- El boton "Recargar" tiene borde ambar y texto ambar, hover con fondo ambar claro
- WCAG AA: el texto de la alerta debe tener contraste 4.5:1 contra el fondo ambar
- Tipografia del mensaje de alerta: 14px, peso regular
```

---

## Apendice -- Configuracion de columnas

### Mapa de columnas tecnicas a labels UI

| Nombre tecnico | Label en espanol | Tooltip descriptivo |
|---|---|---|
| `index` | # (Numero) | Numero secuencial de la ubicacion dentro del folio |
| `locationName` | Nombre de ubicacion | Nombre descriptivo del inmueble o predio |
| `address` | Direccion | Direccion completa de la ubicacion |
| `zipCode` | Codigo postal | Codigo postal de 5 digitos |
| `state` | Estado | Entidad federativa donde se ubica el inmueble |
| `municipality` | Municipio | Municipio o alcaldia |
| `neighborhood` | Colonia | Colonia o asentamiento |
| `city` | Ciudad | Ciudad o localidad |
| `constructionType` | Tipo constructivo | Clasificacion de materiales de construccion (macizo, mixto, etc.) |
| `level` | Nivel | Nivel de riesgo del tipo constructivo |
| `constructionYear` | Ano de construccion | Ano en que se construyo el inmueble |
| `businessLine` | Giro comercial | Actividad economica que se realiza en el inmueble |
| `guarantees` | Coberturas | Coberturas de seguro activas para esta ubicacion |
| `catZone` | Zona catastrofica | Clasificacion de riesgo catastrofico (A, B, C...) |
| `validationStatus` | Estado de validacion | Si la ubicacion tiene datos completos para calculo |
