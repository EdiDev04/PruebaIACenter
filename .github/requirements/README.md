# Requirements — Requerimientos de Negocio

Este directorio contiene los requerimientos de negocio que están **listos para ser especificados** pero aún no tienen una spec generada.

## ¿Qué es un Requerimiento?

Un requerimiento es un documento que describe **qué necesita el negocio**, antes de que el `Spec Generator` lo convierta en una spec técnica ASDD. Es la entrada al pipeline ASDD.

## Lifecycle

```
requirements/<feature>.md  →  /generate-spec  →  specs/<feature>.spec.md
  (requerimiento de negocio)     (Spec Generator)    (especificación técnica)
```

## Cómo Usar

1. Crear un archivo `<feature>.md` en este directorio con la descripción del requerimiento
2. Ejecutar `/generate-spec` o usar `@Spec Generator` en Copilot Chat
3. Una vez generada la spec en `.github/specs/`, el requerimiento puede archivarse o eliminarse

## Convención de Nombres

```
.github/requirements/<nombre-feature-kebab-case>.md
```

## Oleadas de Despliegue

Los requerimientos están organizados en oleadas según sus dependencias. Cada oleada es desplegable y testeable de forma incremental.

### Oleada 1 — Fundación (sin funcionalidad visible al usuario)
| REQ | Feature | Archivo | Dependencias | Estado |
|-----|---------|---------|-------------|--------|
| REQ-01 | Servicio de Referencia Core (Mock) | `core-reference-service.md` | Ninguna | LISTO PARA SPEC |
| REQ-02 | Modelo de Datos y Persistencia | `quote-data-model.md` | Ninguna | LISTO PARA SPEC |

### Oleada 2 — CRUD Base (primer incremento funcional)
| REQ | Feature | Archivo | Dependencias | Estado |
|-----|---------|---------|-------------|--------|
| REQ-03 | Creación y Apertura de Folio | `folio-creation.md` | REQ-01, REQ-02 | LISTO PARA SPEC |
| REQ-04 | Gestión de Datos Generales | `general-info-management.md` | REQ-03, REQ-01 | LISTO PARA SPEC |

### Oleada 3 — Gestión de Ubicaciones
| REQ | Feature | Archivo | Dependencias | Estado |
|-----|---------|---------|-------------|--------|
| REQ-05 | Configuración Layout de Ubicaciones | `location-layout-configuration.md` | REQ-03 | LISTO PARA SPEC |
| REQ-06 | Gestión de Ubicaciones de Riesgo | `location-management.md` | REQ-03, REQ-05, REQ-01 | LISTO PARA SPEC |
| REQ-07 | Configuración Opciones de Cobertura | `coverage-options-configuration.md` | REQ-03, REQ-01 | LISTO PARA SPEC |

### Oleada 4 — Cálculo y Resultados (valor central)
| REQ | Feature | Archivo | Dependencias | Estado |
|-----|---------|---------|-------------|--------|
| REQ-08 | Estado y Progreso de Cotización | `quote-state-progress.md` | REQ-03, REQ-06 | LISTO PARA SPEC |
| REQ-09 | Motor de Cálculo de Primas | `premium-calculation-engine.md` | REQ-01, REQ-02, REQ-06, REQ-07 | LISTO PARA SPEC |
| REQ-10 | Visualización de Resultados y Alertas | `results-display.md` | REQ-09, REQ-08 | LISTO PARA SPEC |

## Grafo de Dependencias

```
REQ-01 (Core Mock) ──────┬──────────────────────────────────────┐
                         │                                      │
REQ-02 (Data Model) ─────┼──► REQ-03 (Folio) ──┬──► REQ-04     │
                         │         │            │   (General)   │
                         │         ├──► REQ-05 (Layout)         │
                         │         │       │                    │
                         │         ├───────┴──► REQ-06 ─────────┤
                         │         │           (Locations)      │
                         │         │               │            │
                         │         └──► REQ-07     │            │
                         │            (Coverage)   │            │
                         │               │         │            │
                         │               │    REQ-08 (State)    │
                         │               │         │            │
                         └───────────────┴────► REQ-09 ─────────┘
                                              (Cálculo)
                                                  │
                                              REQ-10
                                             (Resultados)
```

> Actualiza esta tabla al agregar o procesar requerimientos.
