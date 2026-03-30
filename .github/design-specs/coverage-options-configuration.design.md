# Design Spec: Configuracion de Opciones de Cobertura (SPEC-007)

> **Status:** DRAFT
> **Feature:** coverage-options-configuration
> **Wizard Step:** 3 (Technical Info)
> **Route:** `/quotes/{folio}/technical-info`
> **Spec origen:** `.github/specs/coverage-options-configuration.spec.md`

---

## Seccion 1 -- Data -> UI Mapping

### Entidad principal: `CoverageOptions` (nivel folio)

| Campo | Tipo dato | Componente UI | Tipo input | Razon de diseno |
|---|---|---|---|---|
| `enabledGuarantees` (fire: 3) | Multi-select, 3 opciones | `CheckboxGroup` seccion "Coberturas de Incendio" | Checkbox por item | <7 opciones, multi-seleccion, agrupacion cognitiva |
| `enabledGuarantees` (cat: 2) | Multi-select, 2 opciones | `CheckboxGroup` seccion "Catastrofes" | Checkbox por item | <7 opciones, multi-seleccion |
| `enabledGuarantees` (additional: 4) | Multi-select, 4 opciones | `CheckboxGroup` seccion "Coberturas Complementarias" | Checkbox por item | <7 opciones, multi-seleccion |
| `enabledGuarantees` (special: 5) | Multi-select, 5 opciones | `CheckboxGroup` seccion "Coberturas Especiales" | Checkbox por item | <7 opciones, multi-seleccion |
| `deductiblePercentage` | Decimal 0-1 (%) | `NumberInput` con sufijo "%" | Stepper 0.5% | Rango acotado conocido, stepper para precision |
| `coinsurancePercentage` | Decimal 0-1 (%) | `NumberInput` con sufijo "%" | Stepper 0.5% | Rango acotado conocido, stepper para precision |
| `version` | Integer | Hidden | N/A | Gestionado por server state, invisible al usuario |

### Entidad de referencia: `Guarantee` (catalogo de 14 items)

| Campo catalogo | Componente UI | Uso |
|---|---|---|
| `key` | Value del checkbox | Identificador tecnico |
| `name` | Label del checkbox | Texto visible al usuario |
| `description` | Tooltip en icono (?) | Explicacion en lenguaje simple |
| `category` | Agrupador visual de seccion | Organiza los 4 grupos de checkboxes |
| `requiresInsuredAmount` | Badge informativo "Requiere suma asegurada" | Informa al usuario que la ubicacion debera tener ese dato |

---

## Seccion 2 -- Behavioral Annotations

### Principios conductuales aplicados

| Principio | Aplicacion | Justificacion |
|---|---|---|
| **Smart Defaults** | Todas las 14 garantias vienen habilitadas por defecto | El usuario reduce (deshabilita lo que no necesita) en vez de construir desde cero. Reduce carga cognitiva y evita omisiones accidentales. Patron "opt-out > opt-in" para seguros |
| **Miller's Law** | 14 garantias agrupadas en 4 categorias (3+2+4+5) | Max 4 grupos visibles, cada grupo con max 5 items. Dentro del rango de procesamiento cognitivo humano |
| **Loss Aversion** | Warning al deshabilitar garantia usada en ubicaciones | "Esta garantia ya esta seleccionada en 3 ubicaciones. Deshabilitarla las marcara como incompletas." Activa aversion a la perdida para evitar deshabilitaciones accidentales |
| **Progressive Disclosure** | NO aplica (single-step form) | Solo 2 campos numericos + checkboxes agrupados. Total visible ~6 items (4 secciones colapsables + 2 inputs). No necesita multi-step |
| **Transparency** | Contador de garantias activas visible en header | "8 de 14 garantias habilitadas" -- feedback inmediato del estado |
| **Social Proof** | Badge "recomendado" en building_fire y contents_fire | Coberturas base que el 95%+ de polizas incluyen |

### Estados de la entidad CoverageOptions

| Estado | Indicador visual | Mensaje | Accion |
|---|---|---|---|
| Sin configurar (defaults) | Badge ambar "Sin personalizar" | "Se usaran las opciones por defecto: todas las coberturas habilitadas, sin deducible ni coaseguro" | Boton "Personalizar" |
| Configurado | Badge verde "Configurado" | "8 de 14 coberturas habilitadas, Deducible: 5%, Coaseguro: 10%" | Boton "Modificar" |
| Guardando | Spinner en boton | "Guardando opciones..." | Formulario deshabilitado |
| Error de version | Banner ambar tipo alert | "El folio fue modificado por otro proceso. Recarga para continuar" | Boton "Recargar" |
| Error de catalogo | Banner rojo tipo alert | "No se pudo cargar el catalogo de garantias. Intenta de nuevo." | Boton "Reintentar" |

### Warning de deshabilitacion

- **Trigger**: Usuario desmarca un checkbox de garantia que esta seleccionada en al menos 1 ubicacion
- **Componente**: Dialog/modal de confirmacion
- **Titulo**: "Confirmar deshabilitacion"
- **Mensaje**: "La cobertura {nombre} esta seleccionada en {N} ubicacion(es). Si la deshabilitas, esas ubicaciones quedaran marcadas como incompletas al calcular."
- **Acciones**: "Cancelar" (secundario) | "Deshabilitar" (primario, estilo warning)
- **Datos necesarios**: Query `['locations', folio]` para contar ubicaciones afectadas

---

## Seccion 3 -- Screen Flow + Hierarchy

### Wireframe ASCII -- Formulario de Opciones de Cobertura

```
+------------------------------------------------------------------+
| [<] Folio DAN-2026-00001                    [Paso 3 de 5] ====== |
+------------------------------------------------------------------+
|                                                                    |
|  Opciones de Cobertura                                            |
|  [Badge: 8 de 14 habilitadas]                                    |
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | Parametros Globales                                           | |
|  |                                                               | |
|  |  Deducible (%)         Coaseguro (%)                         | |
|  |  [  5.0  ] [-][+]     [  10.0  ] [-][+]                     | |
|  |                                                               | |
|  |  (i) Estos porcentajes aplican a todas las coberturas        | |
|  +--------------------------------------------------------------+ |
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | [v] Coberturas de Incendio (3)           [Seleccionar todas] | |
|  |  [x] Incendio Edificios (?)        [Badge: recomendado]     | |
|  |  [x] Incendio Contenidos (?)       [Badge: recomendado]     | |
|  |  [x] Extension de Cobertura (?)                              | |
|  +--------------------------------------------------------------+ |
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | [v] Catastrofes (2)                      [Seleccionar todas] | |
|  |  [x] Terremoto y Erupcion Volcanica (?)                     | |
|  |  [x] Fenomenos Hidrometeorologicos (?)                      | |
|  +--------------------------------------------------------------+ |
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | [v] Coberturas Complementarias (4)       [Seleccionar todas] | |
|  |  [x] Remocion de Escombros (?)                              | |
|  |  [x] Gastos Extraordinarios (?)                             | |
|  |  [ ] Perdida de Rentas (?)                                  | |
|  |  [ ] Interrupcion de Negocio (?)                            | |
|  +--------------------------------------------------------------+ |
|                                                                    |
|  +--------------------------------------------------------------+ |
|  | [v] Coberturas Especiales (5)            [Seleccionar todas] | |
|  |  [ ] Equipo Electronico (?)                                  | |
|  |  [x] Robo (?)                                                | |
|  |  [ ] Dinero y Valores (?)                                    | |
|  |  [x] Vidrios (?)                                             | |
|  |  [x] Anuncios Luminosos (?)                                  | |
|  +--------------------------------------------------------------+ |
|                                                                    |
|              [Anterior]                  [Guardar y Continuar]    |
+------------------------------------------------------------------+
```

### Wireframe ASCII -- Warning Dialog

```
+----------------------------------------------+
|  (!) Confirmar deshabilitacion                |
|                                               |
|  La cobertura "Incendio Edificios" esta       |
|  seleccionada en 3 ubicacion(es).             |
|                                               |
|  Si la deshabilitas, esas ubicaciones         |
|  quedaran marcadas como incompletas           |
|  al calcular.                                 |
|                                               |
|         [Cancelar]    [Deshabilitar]          |
+----------------------------------------------+
```

### Jerarquia de informacion (F-pattern)

1. **Header**: Folio + progress bar del wizard (orientacion)
2. **Titulo + counter badge**: "Opciones de Cobertura" + "8 de 14 habilitadas"
3. **Parametros globales**: Deducible y coaseguro (decision rapida, 2 campos)
4. **Secciones de garantias**: 4 grupos colapsables con checkboxes
5. **Acciones**: Anterior / Guardar y Continuar

### Tabla de interacciones

| Accion usuario | Resultado visual | API call |
|---|---|---|
| Carga de pagina | Spinner, luego formulario poblado | `GET /v1/quotes/{folio}/coverage-options` + `GET /v1/catalogs/guarantees` |
| Marca/desmarca checkbox | Checkbox toggle + counter actualizado | Ninguno (state local) |
| Desmarca checkbox con ubicaciones afectadas | Warning dialog con count | Consulta local de `['locations', folio]` (ya cacheado) |
| Confirma deshabilitacion en dialog | Checkbox se desmarca, counter actualiza | Ninguno (state local) |
| Cancela deshabilitacion en dialog | Dialog cierra, checkbox queda marcado | Ninguno |
| Cambia deducible/coaseguro | Valor actualiza en input | Ninguno (state local) |
| Click "Guardar y Continuar" | Boton loading, luego navegacion al paso 4 | `PUT /v1/quotes/{folio}/coverage-options` |
| Respuesta 409 del PUT | Banner ambar "Folio modificado" + boton Recargar | Ninguno |
| Click "Recargar" (tras 409) | Re-fetch del formulario | `GET /v1/quotes/{folio}/coverage-options` |
| Click "Anterior" | Navegacion al paso 2 | Ninguno (no guarda) |
| "Seleccionar todas" en seccion | Todos los checkboxes de esa seccion se marcan | Ninguno (state local) |

---

## Seccion 4 -- Component Inventory

### Tabla de componentes

| Componente | Capa FSD | Props clave | Tipo de estado |
|---|---|---|---|
| `TechnicalInfoPage` | pages/technical-info | -- | Ensambla widget, `useParams()` para folio |
| `CoverageOptionsForm` | widgets/coverage-options-form | `folio: string` | Form state (RHF), server state (TanStack Query) |
| `GuaranteeCheckboxGroup` | entities/guarantee (UI) | `category: string`, `guarantees: GuaranteeDto[]`, `selectedKeys: string[]`, `onToggle`, `onSelectAll` | Controlled por parent |
| `GuaranteeCheckbox` | entities/guarantee (UI) | `guarantee: GuaranteeDto`, `checked: boolean`, `affectedCount?: number`, `onChange` | Controlled |
| `DisableGuaranteeDialog` | features/save-coverage-options | `guaranteeName: string`, `affectedCount: number`, `onConfirm`, `onCancel` | UI state (open/closed) |
| `PercentageInput` | shared/ui | `label: string`, `value: number`, `onChange`, `min: 0`, `max: 100`, `step: 0.5` | Controlled |
| `GuaranteeCounter` | shared/ui | `enabled: number`, `total: number` | Derived (presentational) |

### Zod Schemas

```typescript
// entities/coverage-options/model/coverageOptionsSchema.ts

import { z } from 'zod';

// Schema completo para el formulario (single step, no progressive)
export const coverageOptionsFormSchema = z.object({
  enabledGuarantees: z
    .array(z.string())
    .min(1, 'Debe habilitar al menos una cobertura'),
  deductiblePercentage: z
    .number()
    .min(0, 'El deducible no puede ser negativo')
    .max(100, 'El deducible no puede superar 100%'),
  coinsurancePercentage: z
    .number()
    .min(0, 'El coaseguro no puede ser negativo')
    .max(100, 'El coaseguro no puede superar 100%'),
});

// Schema para envio al API (convierte % a decimal 0-1)
export const coverageOptionsApiSchema = coverageOptionsFormSchema.transform((data) => ({
  enabledGuarantees: data.enabledGuarantees,
  deductiblePercentage: data.deductiblePercentage / 100,
  coinsurancePercentage: data.coinsurancePercentage / 100,
}));

export type CoverageOptionsFormValues = z.infer<typeof coverageOptionsFormSchema>;
```

---

## Seccion 5 -- Validation + Feedback UX

### Matriz de validacion

| Campo | Trigger | Feedback positivo | Feedback error |
|---|---|---|---|
| `enabledGuarantees` | On uncheck (cada toggle) | Counter verde "N de 14" | Borde rojo + "Debe habilitar al menos una cobertura" cuando array queda vacio |
| `enabledGuarantees` (con ubicaciones) | On uncheck | N/A | Warning dialog "Cobertura en uso en N ubicaciones" |
| `deductiblePercentage` | On blur | Check verde sutil | Borde rojo + "El deducible debe estar entre 0% y 100%" |
| `coinsurancePercentage` | On blur | Check verde sutil | Borde rojo + "El coaseguro debe estar entre 0% y 100%" |
| Formulario completo | On submit | Toast "Opciones guardadas" + navegacion | Banner con error especifico |

### Estados visuales de campo

| Estado | Visual |
|---|---|
| Vacio (default) | Input con placeholder "0.0", borde gris neutro |
| Foco | Borde azul, sombra sutil |
| Valido | Borde verde sutil al perder foco, check icon |
| Error | Borde rojo, mensaje debajo en rojo, icono warning |
| Deshabilitado | Fondo gris claro, cursor not-allowed |

### Tratamiento de errores criticos

| Error | Componente | Mensaje | Accion |
|---|---|---|---|
| Catalogo no disponible (503) | Banner alert rojo, full-width top | "No se pudo cargar el catalogo de garantias. El servicio no esta disponible." | Boton "Reintentar" que re-fetcha |
| Version conflict (409) | Banner alert ambar, full-width top | "El folio fue modificado por otro proceso. Recarga la pagina para ver los datos actualizados." | Boton "Recargar" que invalida cache y re-fetcha |
| Folio no encontrado (404) | Redirect con toast | "El folio no existe" | Redirige a /cotizador |
| Error generico (500) | Toast notificacion | "Error al guardar las opciones. Intenta de nuevo." | Toast con auto-dismiss 5s |

---

## Seccion 6 -- Stitch Prompts

### Prompt 1: Formulario principal de opciones de cobertura

```
Disena un formulario de configuracion de opciones de cobertura para un cotizador de seguros de danos a la propiedad. El usuario es un agente de seguros profesional que esta en el Paso 3 de un wizard de 5 pasos.

LAYOUT:
- Header superior con breadcrumb: "Folio DAN-2026-00001" y progress bar mostrando paso 3 de 5 activo
- Contenido centrado con max-width 800px
- Titulo "Opciones de Cobertura" con un badge a la derecha que dice "8 de 14 habilitadas" en color azul

SECCION PARAMETROS GLOBALES:
- Card con borde sutil, titulo "Parametros Globales"
- Dos campos en fila horizontal:
  - "Deducible (%)" - input numerico con valor "5.0", botones stepper +/-, sufijo "%"
  - "Coaseguro (%)" - input numerico con valor "10.0", botones stepper +/-, sufijo "%"
- Nota informativa debajo: icono (i) + "Estos porcentajes aplican a todas las coberturas del folio"

SECCION GARANTIAS - 4 grupos en cards separadas, cada una con:
- Header con titulo del grupo, contador entre parentesis, y link "Seleccionar todas" a la derecha
- Checkboxes verticales con label, icono de tooltip (?), y badge "recomendado" donde aplique

Grupo 1 "Coberturas de Incendio" (3):
- [x] Incendio Edificios (?) [badge verde: recomendado]
- [x] Incendio Contenidos (?) [badge verde: recomendado]
- [x] Extension de Cobertura (?)

Grupo 2 "Catastrofes" (2):
- [x] Terremoto y Erupcion Volcanica (?)
- [x] Fenomenos Hidrometeorologicos (?)

Grupo 3 "Coberturas Complementarias" (4):
- [x] Remocion de Escombros (?)
- [x] Gastos Extraordinarios (?)
- [ ] Perdida de Rentas (?)
- [ ] Interrupcion de Negocio (?)

Grupo 4 "Coberturas Especiales" (5):
- [ ] Equipo Electronico (?)
- [x] Robo (?)
- [ ] Dinero y Valores (?)
- [x] Vidrios (?)
- [x] Anuncios Luminosos (?)

FOOTER DE ACCIONES:
- Boton secundario "Anterior" a la izquierda
- Boton primario "Guardar y Continuar" a la derecha, color azul

ESTILO:
- Fondo gris muy claro (#F8F9FA)
- Cards con fondo blanco, border-radius 8px, sombra sutil
- Tipografia clara y profesional
- Checkboxes con estilo moderno (rounded, color azul al activar)
- Espaciado generoso entre secciones (24px)
- WCAG AA compliant, contraste minimo 4.5:1
- Responsive: en mobile los inputs de parametros se apilan verticalmente
```

### Prompt 2: Warning dialog de deshabilitacion

```
Disena un dialog modal de confirmacion de advertencia para un cotizador de seguros de danos.

CONTEXTO: El usuario esta deshabilitando una cobertura (garantia) que ya esta seleccionada en ubicaciones existentes del folio. El sistema advierte antes de proceder.

LAYOUT:
- Overlay oscuro semi-transparente sobre el formulario
- Modal centrado, max-width 480px, border-radius 12px, fondo blanco
- Padding generoso (24px)

CONTENIDO:
- Icono de advertencia triangular color ambar (no rojo - esto no es un error, es una advertencia)
- Titulo: "Confirmar deshabilitacion" en texto oscuro, bold, tamano 18px
- Parrafo: "La cobertura Incendio Edificios esta seleccionada en 3 ubicacion(es)." en texto normal
- Segundo parrafo: "Si la deshabilitas, esas ubicaciones quedaran marcadas como incompletas al momento de calcular la prima." en texto gris mas suave
- Separador horizontal sutil
- Dos botones alineados a la derecha:
  - "Cancelar" - boton outline/secundario
  - "Deshabilitar" - boton con fondo ambar/warning (NO rojo), texto blanco

ESTILO:
- Profesional y sobrio, transmite seriedad pero no alarma
- Icono ambar, NO rojo (deshabilitar no es un error, es una decision informada)
- El boton "Deshabilitar" en ambar refuerza que es un warning, no un peligro
- WCAG AA, contraste minimo 4.5:1
- Focus trap dentro del modal
- El boton "Cancelar" recibe foco inicial
```

### Prompt 3: Estado de error - Conflicto de version (409)

```
Disena un estado de error de conflicto de version para el formulario de opciones de cobertura de un cotizador de seguros.

CONTEXTO: El usuario intento guardar pero otro proceso modifico el folio. Se muestra un banner de advertencia sobre el formulario.

LAYOUT:
- Misma estructura que el formulario de opciones de cobertura (header con wizard step 3, titulo, cards de garantias)
- PERO con un banner de alerta prominente entre el header y el formulario
- El formulario debajo aparece con opacidad reducida (0.6) y deshabilitado (no interactuable)

BANNER DE ALERTA:
- Full-width del contenido (max-width 800px)
- Fondo ambar claro (#FFF3CD), borde izquierdo solido ambar (4px)
- Icono de advertencia triangular
- Titulo bold: "Folio modificado"
- Mensaje: "El folio DAN-2026-00001 fue modificado por otro proceso mientras editabas. Los datos mostrados pueden estar desactualizados."
- Boton dentro del banner: "Recargar datos" en estilo outline ambar

FORMULARIO DESHABILITADO:
- Todos los checkboxes visibles pero grayed out
- Inputs de deducible y coaseguro con valores pero no editables
- Botones de footer deshabilitados

ESTILO:
- Ambar para el conflicto de version (NO rojo - no es un error del usuario)
- Formulario con overlay sutil de deshabilitacion
- WCAG AA, contraste minimo 4.5:1
```
