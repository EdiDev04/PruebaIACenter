# Design Spec: Visualizacion de Resultados y Alertas (SPEC-010)

> **Status:** APPROVED
> **Feature:** results-display
> **Ruta:** `/quotes/{folio}/terms-and-conditions` (Step 5 del wizard)
> **Fecha:** 2026-03-30
> **Autor:** ux-designer

---

## Seccion 1 -- Data a UI Mapping

### Cluster 1: Estado de la pagina (Gate)

| Campo | Componente UI | Tipo Input | Razon de diseno |
|---|---|---|---|
| `calculationResult` (null vs present) | Renderizado condicional | N/A | Determina que vista mostrar: empty state o resultados |
| `readyForCalculation` | Renderizado condicional | N/A | Dentro del empty state, determina si el usuario puede calcular o necesita completar ubicaciones |

### Cluster 2: Resumen financiero (Hero)

| Campo | Componente UI | Tipo Input | Razon de diseno |
|---|---|---|---|
| `netPremium` | `StatCard` (tarjeta de estadistica) | Read-only, moneda COP | Dato derivado del motor de calculo. Prominencia alta pero secundaria a commercialPremium |
| `commercialPremiumBeforeTax` | `StatCard` | Read-only, moneda COP | Desglose intermedio para transparencia |
| `commercialPremium` | `StatCard` (destacada) | Read-only, moneda COP | Dato final del cotizador. Tarjeta mas grande / visualmente prominente |

### Cluster 3: Desglose por ubicacion

| Campo | Componente UI | Tipo Input | Razon de diseno |
|---|---|---|---|
| `locationName` | Celda en tabla | Read-only, texto | Identificador de fila, alineado a la izquierda |
| `location.netPremium` | Celda en tabla | Read-only, moneda COP | Valor numerico alineado a la derecha |
| `validationStatus` | `Badge` | Read-only, enum | "Calculable" en verde, semantica de estado |
| `guaranteeKey` | Celda en sub-tabla (accordeon) | Read-only, texto | Nombre de la cobertura en la fila expandida |
| `insuredAmount` | Celda en sub-tabla | Read-only, moneda COP | Suma asegurada de la cobertura |
| `rate` | Celda en sub-tabla | Read-only, porcentaje | Tasa aplicada, formato `0.0025%` |
| `premium` | Celda en sub-tabla | Read-only, moneda COP | Prima individual de la cobertura |

### Cluster 4: Alertas incompletas

| Campo | Componente UI | Tipo Input | Razon de diseno |
|---|---|---|---|
| `locationName` (incompleta) | Titulo de alerta | Read-only, texto | Identifica cual ubicacion tiene problemas |
| `missingFields[]` | Lista de chips/tags | Read-only, texto espanol | Campos faltantes traducidos: "Codigo Postal", "Giro de Negocio (Incendio)" |
| Link "Editar ubicacion" | `Button` (variante link) | Accion | Navega a `/quotes/{folio}/locations` |

---

## Seccion 2 -- Behavioral Annotations

### Principios conductuales aplicados

| Principio | Aplicacion | Justificacion |
|---|---|---|
| **Peak-End Rule** | La prima comercial total (el "resultado") es el elemento mas prominente de la pagina | El usuario recuerda el momento cumbre (ver el precio) y el final de la experiencia |
| **Transparency Bias** | Desglose completo: neta > antes IVA > total; por ubicacion; por cobertura (tasa visible) | En seguros, la confianza aumenta cuando el usuario puede verificar como se llego al precio |
| **Progressive Disclosure** | Tabla de ubicaciones colapsada por defecto; click expande sub-tabla de coberturas | Evita sobrecarga cognitiva: primero el resumen, luego el detalle bajo demanda |
| **Incompleto diferente de Error** | Panel de alertas en ambar (warning), NO en rojo (danger) | Incompleto es un estado pendiente, no un error. Lenguaje: "Datos pendientes", no "Invalida" |
| **Action-Oriented Alerts** | Cada alerta tiene boton "Editar ubicacion" que lleva al formulario exacto | Reducir la friccion entre "ver el problema" y "resolverlo" |
| **Anchoring** | Prima comercial total (con IVA) es la tarjeta mas grande y la primera que el ojo encuentra | El usuario ancla su percepcion en el precio final real |
| **Status Quo Bias** | Boton "Recalcular" visible pero secundario; los resultados actuales se mantienen hasta nuevo calculo | No forzar al usuario a recalcular; los resultados vigentes son validos hasta que decida cambiar |

### Progressive Disclosure

La pagina NO tiene steps internos (no es formulario). La disclosure es:

1. **Nivel 1 (siempre visible):** 3 tarjetas de resumen financiero + header con boton "Recalcular"
2. **Nivel 2 (siempre visible):** Tabla de ubicaciones calculables (filas colapsadas)
3. **Nivel 3 (bajo demanda):** Click en fila de ubicacion expande sub-tabla de coberturas (accordeon)
4. **Nivel lateral (siempre visible si hay alertas):** Panel de ubicaciones incompletas

### Smart Defaults

- Todas las filas de ubicacion empiezan **colapsadas** (evita wall of data)
- Si hay solo 1 ubicacion calculable, se expande automaticamente (no tiene sentido ocultar)
- El panel de alertas se muestra colapsado si hay mas de 3 ubicaciones incompletas

### Agrupacion cognitiva (Miller's Law)

Las coberturas dentro del accordeon se muestran como sub-tabla plana (no agrupadas por categoria) porque aqui el contexto es "verificar la prima calculada", no "seleccionar coberturas". La agrupacion por tipo ya se hizo en SPEC-007 (configuracion de coberturas).

### Estados de la entidad

| Estado | Indicador visual | Mensaje | Accion |
|---|---|---|---|
| No calculado + ready | Icono de calculadora + fondo suave | "Ejecute el calculo para ver los resultados de su cotizacion" | Boton primario "Calcular cotizacion" |
| No calculado + not ready | Icono de advertencia ambar | "No hay ubicaciones calculables. Complete al menos una ubicacion para poder calcular." | Link "Ir a ubicaciones" con flecha |
| Calculado | 3 tarjetas hero + tabla + alertas | N/A (los datos son el mensaje) | Boton secundario "Recalcular" en header |
| Calculando (loading) | Skeleton en tarjetas + spinner en tabla | "Calculando primas..." | Boton deshabilitado con spinner |
| Error de calculo | Banner rojo en top | "Error al calcular: [mensaje del backend]" | Boton "Reintentar calculo" |

---

## Seccion 3 -- Screen Flow + Hierarchy

### Wireframe ASCII -- Estado "Calculado" (principal)

```
+------------------------------------------------------------------+
| [Wizard Header - Step 5: Resultados]            [Folio: COT-XXX] |
+------------------------------------------------------------------+
|                                                                    |
| RESULTADOS DE LA COTIZACION            [Recalcular]               |
|                                                                    |
| +------------------+ +------------------+ +--------------------+  |
| | Prima Neta       | | Prima Comercial  | | PRIMA COMERCIAL    |  |
| | Total            | | (sin IVA)        | | TOTAL (con IVA)    |  |
| |                  | |                  | |                    |  |
| | $85.230,00       | | $102.276,00      | | $118.640,16        |  |
| | (solo riesgo)    | | (+ gastos exp.)  | | (precio final)     |  |
| +------------------+ +------------------+ +--------------------+  |
|                                                                    |
| DESGLOSE POR UBICACION                          2 ubicaciones     |
| +----------------------------------------------------------------+|
| | Ubicacion            | Prima Neta    | Estado                  ||
| |----------------------|---------------|-------------------------||
| | > Bodega Central     | $52.430,00    | [Calculable]            ||
| |   CDMX               |               |                         ||
| |----------------------|---------------|-------------------------||
| | > Nave Industrial    | $32.800,00    | [Calculable]            ||
| |   Monterrey          |               |                         ||
| |----------------------|---------------|-------------------------||
| |                      | $85.230,00    | TOTAL                   ||
| +----------------------------------------------------------------+|
|                                                                    |
| (Fila expandida - Bodega Central CDMX)                            |
| +----------------------------------------------------------------+|
| | Cobertura         | Suma Aseg.    | Tasa    | Prima            ||
| |-------------------|---------------|---------|------------------||
| | Incendio          | $5.000.000    | 0,25%   | $12.500,00       ||
| | Terremoto         | $5.000.000    | 0,50%   | $25.000,00       ||
| | Robo              | $2.000.000    | 0,40%   | $8.000,00        ||
| | Resp. Civil       | $1.000.000    | 0,693%  | $6.930,00        ||
| +----------------------------------------------------------------+|
|                                                                    |
| +-- UBICACIONES CON DATOS PENDIENTES (1) -------- [ambar] ------+|
| |                                                                 ||
| | ! Oficina Administrativa Guadalajara                            ||
| |   Campos faltantes:                                             ||
| |   [Codigo Postal] [Giro de Negocio (Incendio)]                 ||
| |                                        [Editar ubicacion ->]    ||
| |                                                                 ||
| +-----------------------------------------------------------------+|
|                                                                    |
+------------------------------------------------------------------+
```

### Wireframe ASCII -- Estado "No calculado + ready"

```
+------------------------------------------------------------------+
| [Wizard Header - Step 5: Resultados]            [Folio: COT-XXX] |
+------------------------------------------------------------------+
|                                                                    |
|                    [icono calculadora]                              |
|                                                                    |
|          Ejecute el calculo para ver los resultados                |
|               de su cotizacion                                     |
|                                                                    |
|          Tiene 2 ubicaciones listas para calcular.                 |
|                                                                    |
|                 [ Calcular cotizacion ]                             |
|                    (boton primario)                                 |
|                                                                    |
+------------------------------------------------------------------+
```

### Wireframe ASCII -- Estado "No calculado + not ready"

```
+------------------------------------------------------------------+
| [Wizard Header - Step 5: Resultados]            [Folio: COT-XXX] |
+------------------------------------------------------------------+
|                                                                    |
|                  [icono advertencia ambar]                          |
|                                                                    |
|         No hay ubicaciones calculables                             |
|                                                                    |
|         Complete al menos una ubicacion con todos                  |
|         los datos requeridos para poder calcular.                  |
|                                                                    |
|               [ Ir a ubicaciones -> ]                              |
|                 (link con flecha)                                   |
|                                                                    |
+------------------------------------------------------------------+
```

### Jerarquia de informacion (F-pattern)

1. **Primera linea F:** Titulo "Resultados de la Cotizacion" + boton "Recalcular" (esquina superior derecha)
2. **Barra horizontal F:** 3 tarjetas de resumen financiero (lectura izquierda a derecha: neta > sin IVA > total)
3. **Columna vertical F:** Tabla de desglose (lectura vertical por filas)
4. **Zona inferior:** Panel de alertas incompletas (ambar, menor prominencia)

### Tabla de interacciones

| Accion usuario | Resultado visual | API Call |
|---|---|---|
| Navega a la pagina (calculado) | Renderiza tarjetas + tabla + alertas | `GET /v1/quotes/{folio}/state` (cache TanStack) |
| Navega a la pagina (no calculado) | Renderiza empty state (ready o not-ready) | `GET /v1/quotes/{folio}/state` (cache TanStack) |
| Click "Calcular cotizacion" | Spinner en boton, skeleton en tarjetas | `POST /v1/quotes/{folio}/calculate` + invalidar cache |
| Click "Recalcular" | Spinner en boton, tabla en loading | `POST /v1/quotes/{folio}/calculate` + invalidar cache |
| Click en fila de ubicacion | Expande/colapsa sub-tabla de coberturas | Ninguna (estado local) |
| Click "Editar ubicacion" | Navega a `/quotes/{folio}/locations` | Ninguna (router navigation) |
| Click "Ir a ubicaciones" | Navega a `/quotes/{folio}/locations` | Ninguna (router navigation) |

---

## Seccion 4 -- Component Inventory

### Tabla de componentes

| Componente | Capa FSD | Props clave | Tipo de estado |
|---|---|---|---|
| `ResultsPage` | `pages/` | -- | Server state (useQuoteStateQuery) + mutation (useCalculateQuote) |
| `FinancialSummary` | `widgets/financial-summary/` | `netPremium`, `commercialPremiumBeforeTax`, `commercialPremium` | Stateless (props only) |
| `LocationBreakdown` | `widgets/location-breakdown/` | `premiumsByLocation: LocationPremiumDto[]` | Local state (expanded rows) |
| `CoverageAccordion` | `widgets/location-breakdown/` | `coveragePremiums: CoveragePremiumDto[]` | Stateless (props only) |
| `IncompleteAlerts` | `widgets/incomplete-alerts/` | `alerts: LocationAlertDto[]`, `folio: string` | Stateless (props only) |
| `EmptyState` | Inline en `ResultsPage` o `shared/ui/` | `readyForCalculation: boolean`, `onCalculate`, `folio` | Stateless |
| `formatCurrency` | `shared/lib/` | `(value: number) => string` | Pure function |

### Zod Schemas

```typescript
// Schema para el resultado completo (read-only, validacion de shape)
const CoveragePremiumSchema = z.object({
  guaranteeKey: z.string(),
  insuredAmount: z.number().nonnegative(),
  rate: z.number().nonnegative(),
  premium: z.number().nonnegative(),
});

const LocationPremiumSchema = z.object({
  locationName: z.string(),
  netPremium: z.number().nonnegative(),
  validationStatus: z.enum(["calculable", "incomplete"]),
  coveragePremiums: z.array(CoveragePremiumSchema),
});

const CalculationResultSchema = z.object({
  netPremium: z.number().nonnegative(),
  commercialPremiumBeforeTax: z.number().nonnegative(),
  commercialPremium: z.number().nonnegative(),
  premiumsByLocation: z.array(LocationPremiumSchema),
});

// Schema para las alertas de ubicaciones
const LocationAlertSchema = z.object({
  locationName: z.string(),
  missingFields: z.array(z.string()),
});
```

Nota: No hay "guardado parcial" porque esta pagina es 100% read-only. Los schemas validan la shape del response del backend.

---

## Seccion 5 -- Validation + Feedback UX

### Matriz de estados y feedback

Esta pagina no tiene formularios de entrada. El feedback UX se centra en los **estados de la pagina** y las **acciones de calculo**.

| Estado / Accion | Trigger | Feedback positivo | Feedback error |
|---|---|---|---|
| Pagina carga (calculado) | Mount + GET /state | Tarjetas con datos + tabla poblada | Banner rojo "Error al cargar resultados" + boton "Reintentar" |
| Pagina carga (no calculado, ready) | Mount + GET /state | Empty state con boton "Calcular" | Banner rojo "Error al cargar estado" |
| Pagina carga (no calculado, not ready) | Mount + GET /state | Empty state con link a ubicaciones | Banner rojo "Error al cargar estado" |
| Click "Calcular" | Click boton | Spinner en boton + skeleton en tarjetas; al terminar: transicion smooth a resultados | Toast rojo "Error al calcular: {mensaje}" + boton visible para reintentar |
| Click "Recalcular" | Click boton | Spinner en boton + overlay semi-transparente sobre datos actuales | Toast rojo "Error al recalcular: {mensaje}" + datos anteriores se mantienen |
| Expand ubicacion | Click fila | Sub-tabla se despliega con animacion (max 300ms) | N/A (operacion local) |

### Estados visuales de componentes

| Componente | Vacio | Cargando | Con datos | Error |
|---|---|---|---|---|
| StatCard (tarjeta) | Fondo gris claro, "$0,00" | Skeleton animado | Valor formateado COP, fondo blanco | Borde rojo, icono error |
| Tabla ubicaciones | "No hay ubicaciones calculables" | Filas skeleton (3 filas fantasma) | Filas con datos + badge | Mensaje error inline |
| Panel alertas | No se renderiza | N/A | Cards ambar con campos faltantes | N/A |
| Boton Calcular | Habilitado (ready) o no renderizado (not ready) | Deshabilitado + spinner | N/A (se reemplaza por resultados) | Habilitado + texto "Reintentar" |

### Tratamiento especial de errores

- **Error de calculo (5xx/timeout):** Banner rojo en la parte superior de la pagina, datos anteriores se mantienen si existian, boton "Reintentar calculo"
- **Version conflict (409):** Modal de conflicto de version (reutilizar componente de SPEC-004). Mensaje: "La cotizacion fue modificada por otro usuario. Recargue para ver los cambios."
- **No hay ubicaciones calculables y el usuario llega directo a la URL:** Redirect suave al Step 2 (ubicaciones) con toast informativo

---

## Seccion 6 -- Stitch Prompts

### Prompt 1: Estado No Calculado (ready + not-ready)

**Screen name:** `results/estado-no-calculado`

```
Disena una pagina de "estado vacio" para el Step 5 (Resultados) de un cotizador de seguros de danos.
La pagina pertenece a un wizard de 4 pasos y muestra un empty state centrado verticalmente.

CONTEXTO: Un agente de seguros esta cotizando. Llego al paso final pero aun no ha ejecutado el calculo
de primas. La pagina debe motivarlo a ejecutar el calculo o guiarlo a completar datos faltantes.

LAYOUT: Pagina con header de wizard (Step 5 activo, titulo "Resultados"). Contenido centrado
verticalmente en el area principal.

MOSTRAR DOS VARIANTES en la misma pagina, separadas por una linea divisora con label "Variante A" y "Variante B":

VARIANTE A - "Listo para calcular":
- Icono grande de calculadora (outline, color primario azul, 64px)
- Titulo: "Ejecute el calculo para ver los resultados"
- Subtitulo: "Tiene 2 ubicaciones listas para calcular."
- Boton primario grande: "Calcular cotizacion" (con icono de play/flecha)
- Texto auxiliar debajo: "El calculo puede tomar unos segundos"

VARIANTE B - "No hay ubicaciones listas":
- Icono de advertencia en ambar (NO rojo), 64px
- Titulo: "No hay ubicaciones calculables"
- Subtitulo: "Complete al menos una ubicacion con todos los datos requeridos para poder calcular."
- Link con flecha: "Ir a ubicaciones ->" (color primario, sin boton, estilo link)
- NO mostrar boton de calcular (esta deshabilitado conceptualmente)

ESTILO:
- Fondo blanco limpio, sin tarjetas contenedoras
- Tipografia clara: titulo 24px semibold, subtitulo 16px regular gris
- Espacio generoso entre elementos (32px entre secciones)
- WCAG AA: contraste minimo 4.5:1
- Responsive: centrado en desktop, full-width en mobile

ACCESIBILIDAD:
- role="status" en el contenedor del mensaje
- aria-live="polite" para cambios de estado
- Boton con aria-label descriptivo
```

### Prompt 2: Resumen Financiero (3 tarjetas)

**Screen name:** `results/resumen-financiero`

```
Disena la seccion hero de resultados financieros para un cotizador de seguros de danos.
Muestra 3 tarjetas de resumen de primas en una fila horizontal.

CONTEXTO: Un agente de seguros acaba de calcular las primas de una cotizacion con 2 ubicaciones.
Esta es la informacion mas importante de toda la aplicacion: el precio final de la poliza.

LAYOUT: Fila de 3 tarjetas (CSS Grid, 3 columnas iguales en desktop, stack vertical en mobile).
Encima de las tarjetas: titulo "Resultados de la Cotizacion" alineado a la izquierda con boton
"Recalcular" alineado a la derecha en la misma linea.

TARJETA 1 - Prima Neta Total:
- Label: "Prima Neta Total"
- Valor: "$85.230,00" (formato COP: punto para miles, coma para decimales)
- Subtexto: "Solo riesgo puro"
- Estilo: borde izquierdo azul 4px, fondo blanco, sombra suave

TARJETA 2 - Prima Comercial (sin IVA):
- Label: "Prima Comercial (sin IVA)"
- Valor: "$102.276,00"
- Subtexto: "Incluye gastos de expedicion"
- Estilo: borde izquierdo azul 4px, fondo blanco, sombra suave

TARJETA 3 - Prima Comercial Total (con IVA) - DESTACADA:
- Label: "Prima Comercial Total"
- Badge: "Precio final" en verde
- Valor: "$118.640,16" (tipografia 32px bold, mas grande que las otras 2 tarjetas que usan 24px)
- Subtexto: "IVA incluido"
- Estilo: borde izquierdo verde 4px, fondo ligeramente tintado verde claro (5% opacidad),
  sombra mas pronunciada que las otras 2

HEADER:
- Titulo "Resultados de la Cotizacion" (20px semibold)
- Boton "Recalcular" con icono de refresh, variante outlined/secundario
- Alineados en la misma fila con space-between

ESTILO:
- Tarjetas con border-radius 8px, padding 24px
- Labels en gris oscuro 14px
- Valores en negro/dark 24px semibold (32px para la destacada)
- Subtextos en gris claro 12px
- Gap entre tarjetas: 16px
- WCAG AA obligatorio
- Responsive: en mobile las 3 tarjetas se apilan verticalmente

ACCESIBILIDAD:
- Cada tarjeta tiene role="region" con aria-label="Prima Neta Total: $85.230,00"
- Valores numericos con aria-label que incluye el texto completo
```

### Prompt 3: Desglose por Ubicaciones (tabla + accordeon)

**Screen name:** `results/desglose-ubicaciones`

```
Disena una tabla de desglose de primas por ubicacion con filas expandibles (accordeon)
para un cotizador de seguros de danos.

CONTEXTO: Despues de ver el resumen financiero, el agente de seguros necesita verificar
como se distribuye la prima entre las ubicaciones aseguradas y que tasa se aplico a cada
cobertura. Esta seccion es para trazabilidad y verificacion.

LAYOUT: Seccion completa debajo del resumen financiero. Titulo de seccion + contador + tabla.

HEADER DE SECCION:
- Titulo: "Desglose por Ubicacion" (18px semibold)
- Badge contador: "2 ubicaciones" (pill gris claro)
- Alineados en la misma linea

TABLA PRINCIPAL:
- 3 columnas: Ubicacion | Prima Neta | Estado
- Columna "Ubicacion": texto alineado izquierda, con icono chevron (>) que rota al expandir
- Columna "Prima Neta": moneda COP alineada derecha
- Columna "Estado": badge "Calculable" en verde (chip con icono check)

FILAS DE EJEMPLO (2 filas):
Fila 1: "Bodega Central CDMX" | "$52.430,00" | [Calculable]
Fila 2: "Nave Industrial Monterrey" | "$32.800,00" | [Calculable]

FILA TOTAL (al final):
- Sin icono chevron
- "Total" en bold
- "$85.230,00" en bold
- Sin badge de estado
- Fondo gris muy claro para diferenciar

FILA EXPANDIDA (mostrar la Fila 1 expandida como ejemplo):
Al hacer click en "Bodega Central CDMX", se despliega una sub-tabla debajo de la fila:

Sub-tabla con 4 columnas: Cobertura | Suma Asegurada | Tasa | Prima
- Incendio | $5.000.000,00 | 0,250% | $12.500,00
- Terremoto | $5.000.000,00 | 0,500% | $25.000,00
- Robo | $2.000.000,00 | 0,400% | $8.000,00
- Responsabilidad Civil | $1.000.000,00 | 0,693% | $6.930,00
Sub-total fila: | | | $52.430,00

La sub-tabla tiene:
- Fondo ligeramente gris (diferenciado de la tabla principal)
- Padding-left extra (indentacion visual de 32px)
- Tipografia mas pequena (13px vs 14px de la tabla principal)
- Bordes internos mas suaves

ESTADOS DE LA TABLA:
- Hover en fila: fondo gris suave + cursor pointer
- Fila expandida: icono chevron rotado 90 grados, fondo ligeramente diferente
- Animacion de expansion: max-height transition 300ms ease

ESTILO:
- Tabla con border-radius 8px, borde 1px gris claro
- Header de tabla: fondo gris muy claro, texto uppercase 12px bold gris
- Celdas: padding 12px 16px
- WCAG AA obligatorio
- Responsive: en mobile la tabla se convierte en cards apiladas

ACCESIBILIDAD:
- Tabla semantica con thead/tbody
- Filas expandibles con aria-expanded="true/false"
- Sub-tabla con aria-label="Coberturas de Bodega Central CDMX"
- role="row" y role="cell" correctos
```

### Prompt 4: Panel de Alertas Incompletas

**Screen name:** `results/panel-alertas-incompletas`

```
Disena un panel de alertas para ubicaciones incompletas en un cotizador de seguros de danos.
Este panel se muestra debajo de la tabla de desglose cuando hay ubicaciones que no pudieron
ser incluidas en el calculo por tener datos faltantes.

CONTEXTO: De 3 ubicaciones registradas, 2 se calcularon exitosamente y 1 tiene datos
faltantes. El panel debe comunicar esto como un estado "pendiente" (NO como error) y
facilitar la correccion.

LAYOUT: Seccion completa debajo de la tabla de desglose. Contenedor con borde ambar.

HEADER DEL PANEL:
- Icono de advertencia triangular en ambar
- Titulo: "Ubicaciones con datos pendientes" (16px semibold, color ambar oscuro)
- Badge contador: "(1)" al lado del titulo
- Todo sobre fondo ambar claro (ambar al 10% opacidad)
- Borde izquierdo 4px ambar solido

CARD DE UBICACION INCOMPLETA:
- Nombre: "Oficina Administrativa Guadalajara" (14px semibold)
- Subtitulo: "Esta ubicacion no fue incluida en el calculo"
- Seccion "Campos faltantes:":
  - Chips/tags en gris claro con los nombres de campos EN ESPANOL:
    [Codigo Postal] [Giro de Negocio (Incendio)]
  - Los chips tienen icono de campo vacio (outline circle)
- Boton: "Editar ubicacion" con icono de flecha derecha, variante link/text en color primario
- Separador sutil si hay mas de 1 ubicacion incompleta

SEGUNDO EJEMPLO (para mostrar multiples alertas):
Agregar una segunda card:
- Nombre: "Almacen Queretaro"
- Campos faltantes: [Tipo de Inmueble] [Numero de Pisos] [Coberturas]
- Mismo boton "Editar ubicacion"

TEXTO AUXILIAR al final del panel:
- "Complete los datos pendientes y presione 'Recalcular' para incluir estas ubicaciones."
- Texto gris claro, 13px, italic

IMPORTANTE - NO USAR ROJO:
- Todo el panel es ambar/warning: fondo ambar claro, borde ambar, icono ambar
- Ningun elemento en rojo. "Incompleto" no es un error, es un estado pendiente
- El lenguaje dice "datos pendientes", NO "datos invalidos" o "errores"

ESTILO:
- Panel con border-radius 8px
- Cards internas sin borde, separadas por divider 1px
- Chips de campos faltantes: border-radius 16px, padding 4px 12px, fondo gris 100
- Espacio entre cards: 16px
- WCAG AA obligatorio

ACCESIBILIDAD:
- Panel con role="alert" y aria-label="Ubicaciones con datos pendientes"
- Cada card con role="listitem" dentro de role="list"
- Links de edicion con aria-label="Editar ubicacion Oficina Administrativa Guadalajara"
- Contraste del texto ambar sobre fondo ambar claro >= 4.5:1
```

---

## Anexo A -- Traduccion de campos tecnicos a espanol

| Campo tecnico (missingFields) | Texto en espanol para UI |
|---|---|
| `zipCode` | Codigo Postal |
| `businessLine.fireKey` | Giro de Negocio (Incendio) |
| `businessLine.miscKey` | Giro de Negocio (Diversos) |
| `propertyType` | Tipo de Inmueble |
| `floors` | Numero de Pisos |
| `basements` | Sotanos |
| `constructionType` | Tipo de Construccion |
| `coverages` | Coberturas |
| `insuredValue` | Valor Asegurado |

## Anexo B -- Formato de moneda COP

```typescript
const formatCurrency = (value: number): string =>
  new Intl.NumberFormat('es-CO', {
    style: 'currency',
    currency: 'COP',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);

// Ejemplos:
// 85230     → "$85.230,00"
// 125430.5  → "$125.430,50"
// 0         → "$0,00"
// 5000000   → "$5.000.000,00"
```

## Anexo C -- Datos de ejemplo para Stitch

```json
{
  "calculationResult": {
    "netPremium": 85230.00,
    "commercialPremiumBeforeTax": 102276.00,
    "commercialPremium": 118640.16,
    "premiumsByLocation": [
      {
        "locationName": "Bodega Central CDMX",
        "netPremium": 52430.00,
        "validationStatus": "calculable",
        "coveragePremiums": [
          { "guaranteeKey": "Incendio", "insuredAmount": 5000000, "rate": 0.0025, "premium": 12500.00 },
          { "guaranteeKey": "Terremoto", "insuredAmount": 5000000, "rate": 0.005, "premium": 25000.00 },
          { "guaranteeKey": "Robo", "insuredAmount": 2000000, "rate": 0.004, "premium": 8000.00 },
          { "guaranteeKey": "Responsabilidad Civil", "insuredAmount": 1000000, "rate": 0.00693, "premium": 6930.00 }
        ]
      },
      {
        "locationName": "Nave Industrial Monterrey",
        "netPremium": 32800.00,
        "validationStatus": "calculable",
        "coveragePremiums": [
          { "guaranteeKey": "Incendio", "insuredAmount": 3000000, "rate": 0.003, "premium": 9000.00 },
          { "guaranteeKey": "Terremoto", "insuredAmount": 3000000, "rate": 0.006, "premium": 18000.00 },
          { "guaranteeKey": "Robo", "insuredAmount": 1500000, "rate": 0.00387, "premium": 5800.00 }
        ]
      }
    ]
  },
  "incompleteLocations": [
    {
      "locationName": "Oficina Administrativa Guadalajara",
      "missingFields": ["zipCode", "businessLine.fireKey"]
    }
  ]
}
```
