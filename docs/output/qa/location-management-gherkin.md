# Gherkin Cases — SPEC-006: Gestión de Ubicaciones de Riesgo

> **Fuente:** SPEC-006 v1.0 + location-management.design.md (APPROVED)
> **Generado:** 2026-03-29
> **Estado:** READY
> **Prerequisito:** code-quality-report.md → Gate PASSED (re-auditoría 2026-03-29)

---

## Resumen de cobertura

| # | Capa | HU | Flujo | Escenarios | Etiquetas |
|---|---|---|---|---|---|
| E2E-01 | UI / Wizard | HU-006-01 + 05 | Agregar ubicación calculable | 1 | @smoke @critico @e2e |
| E2E-02 | UI / Wizard | HU-006-04 + 08 | CP inexistente — badge ámbar | 1 | @critico @integracion @e2e |
| E2E-03 | UI / Wizard | HU-006-06 | Editar giro con PATCH | 1 | @smoke @critico @e2e |
| E2E-04 | UI / Wizard | — | Eliminar con confirmación modal | 1 | @critico @e2e |
| E2E-05 | UI / Wizard | HU-006-06 | Conflict 409 — toast + formulario abierto | 1 | @critico @concurrencia @e2e |
| E2E-06 | UI / Wizard | HU-006-07 | Botón Continuar deshabilitado | 1 | @happy-path @ui @e2e |
| E2E-07 | UI / Wizard | HU-006-02 | Validación locationName vacío | 1 | @error-path @validacion @e2e |
| API-01 | API | HU-006-01 | PUT calculable — atomicidad | 3 | @smoke @critico |
| API-02 | API | HU-006-02 | Datos físicos del inmueble | 2 | @smoke @critico |
| API-03 | API | HU-006-03 | Giro comercial desde catálogo | 2 | @happy-path |
| API-04 | API | HU-006-04 | Resolución automática de CP | 4 | @smoke @integracion |
| API-05 | API | HU-006-05 | Garantías con suma asegurada | 4 | @critico @regla-negocio |
| API-06 | API | HU-006-06 | Edición PATCH ubicación individual | 4 | @smoke @critico |
| API-07 | API | HU-006-07 | Resumen de validación | 2 | @happy-path |
| API-08 | API | HU-006-08 | Guardado parcial sin bloqueo | 3 | @critico @regla-negocio |
| API-09 | API | — | Validaciones de formulario | 6 | @error-path @validacion |
| API-10 | API | — | Eliminación implícita vía PUT | 2 | @happy-path @regla-negocio |
| API-11 | API | — | Conflicto de versión 409 | 3 | @critico @concurrencia |
| API-12 | API | — | Auth / Sin credenciales | 2 | @seguridad |

**Total: 46 escenarios** (7 E2E UI + 39 API)

---

## Datos de prueba sintéticos

| Fixture | Campo | Valor |
|---|---|---|
| Folio base | folioNumber | `DAN-2026-00001` |
| Folio sin ubicaciones | folioNumber | `DAN-2026-00002` |
| Folio inexistente | folioNumber | `DAN-2026-99999` |
| CP válido CDMX | zipCode | `06600` |
| CP válido B. Juárez | zipCode | `03100` |
| CP inexistente | zipCode | `99999` |
| CP no numérico | zipCode | `ABCDE` |
| CP corto | zipCode | `0660` |
| Giro válido | fireKey | `B-03` (Storage warehouse) |
| Giro válido retail | fireKey | `C-01` (Retail store) |
| Garantía requiere monto | guaranteeKey | `building_fire` (requiresInsuredAmount: true) |
| Garantía plana | guaranteeKey | `glass` (requiresInsuredAmount: false) |
| Garantía inválida | guaranteeKey | `invalid_guarantee_key` |
| Año válido | constructionYear | `1998` |
| Año borde inferior | constructionYear | `1800` |
| Año borde superior | constructionYear | `2026` |
| Año fuera de rango (futuro) | constructionYear | `2099` |
| Año fuera de rango (pasado) | constructionYear | `1799` |
| Nombre largo | locationName | `A` × 201 chars |
| Nivel inválido | level | `-1` |
| Credenciales válidas | Authorization | `Basic dXNlcjpwYXNz` |
| Sin credenciales | Authorization | (ausente) |

---

## Escenarios E2E — Wizard de Ubicaciones (Playwright)

> Escenarios conductuales end-to-end que validan el flujo de usuario en la interfaz.
> Estos escenarios son independientes de los escenarios API y se ejecutan con Playwright.

```gherkin
#language: es
Característica: Gestión de ubicaciones de riesgo — Interfaz de usuario (SPEC-006)

  El agente del cotizador registra, edita y elimina ubicaciones de riesgo
  en el paso 2 del wizard. Las ubicaciones incompletas muestran badge ámbar
  (nunca rojo) y no bloquean el guardado del folio.

  Antecedentes:
    Dado que el agente está autenticado en el sistema
    Y el folio "DAN-2026-00001" existe en estado "en_proceso" con version 2

# ═══════════════════════════════════════════════════════════
# E2E-01: Agregar ubicación calculable
# ═══════════════════════════════════════════════════════════

  @smoke @critico @e2e @hu-006-01 @hu-006-05
  Escenario: Agregar ubicación calculable con todos los datos requeridos
    Dado que el agente está en el paso 2 del wizard del folio "DAN-2026-00001"
    Cuando completa el formulario Step 1 con:
      | Campo             | Valor                  |
      | Nombre            | Bodega Central CDMX    |
      | Dirección         | Av. Industria 340      |
      | Código postal     | 06600                  |
      | Tipo constructivo | Tipo 1 - Macizo        |
      | Nivel             | 2                      |
      | Año construcción  | 1998                   |
    Y avanza al Step 2 y selecciona el giro "Storage warehouse (B-03)"
    Y selecciona la garantía "Incendio de edificio" con monto 1,000,000
    Y hace clic en "Guardar ubicación"
    Entonces la ubicación "Bodega Central CDMX" aparece en la grilla con badge verde "Calculable"
    Y el botón "Continuar →" se activa
    Y el contador del encabezado muestra "1 de 1 ubicaciones calculables"

# ═══════════════════════════════════════════════════════════
# E2E-02: Agregar ubicación con CP no encontrado
# ═══════════════════════════════════════════════════════════

  @critico @integracion @e2e @hu-006-04 @hu-006-08
  Escenario: Agregar ubicación con código postal inexistente — badge ámbar
    Dado que el agente está en el paso 2 del wizard del folio "DAN-2026-00001"
    Cuando ingresa el código postal "99999" en el campo CP del Step 1
    Y el sistema consulta core-ohs y recibe respuesta 404
    Entonces se muestra el mensaje ámbar "Código postal no encontrado. Puedes continuar sin él, pero la ubicación quedará incompleta."
    Y los campos auto-resueltos (estado, municipio, colonia, zona cat.) muestran "–"
    Cuando hace clic en "Guardar ubicación" sin completar los datos de calculabilidad
    Entonces la ubicación aparece en la grilla con badge ámbar "Datos pendientes"
    Y el badge NUNCA es de color rojo
    Y el botón "Continuar →" permanece deshabilitado si no hay otras ubicaciones calculables

# ═══════════════════════════════════════════════════════════
# E2E-03: Editar ubicación existente (PATCH)
# ═══════════════════════════════════════════════════════════

  @smoke @critico @e2e @hu-006-06
  Escenario: Editar solo el giro comercial de una ubicación existente sin afectar las demás
    Dado que el folio "DAN-2026-00001" tiene 3 ubicaciones con version 5:
      | Index | Nombre             | Giro  | Estado     |
      | 1     | Bodega CDMX        | B-03  | calculable |
      | 2     | Sucursal Del Valle | C-01  | calculable |
      | 3     | Almacén Norte      | —     | incomplete |
    Cuando el agente abre el menú ⋮ de la ubicación "Bodega CDMX" (index 1) y selecciona "Editar"
    Y modifica el giro comercial a "Retail store (C-01)"
    Y hace clic en "Guardar"
    Entonces la grilla muestra la ubicación 1 con el nuevo giro "Retail store"
    Y las ubicaciones 2 y 3 permanecen sin cambios
    Y se envió PATCH /v1/quotes/DAN-2026-00001/locations/1 al servidor
    Y "data.locations[0].locationBusinessLine.fireKey" es "C-01"
    Y "data.version" es 6

# ═══════════════════════════════════════════════════════════
# E2E-04: Eliminar ubicación con confirmación modal
# ═══════════════════════════════════════════════════════════

  @critico @e2e @hu-006-05-delete
  Escenario: Eliminar ubicación mediante confirmación en modal
    Dado que el folio "DAN-2026-00001" tiene 3 ubicaciones:
      | Index | Nombre             | Estado     |
      | 1     | Bodega CDMX        | calculable |
      | 2     | Sucursal Del Valle | calculable |
      | 3     | Almacén Norte      | incomplete |
    Cuando el agente hace clic en ⋮ de "Sucursal Del Valle" (index 2) y selecciona "Eliminar"
    Entonces se muestra el modal "¿Eliminar Sucursal Del Valle?"
    Cuando el agente confirma la eliminación
    Entonces la grilla muestra 2 ubicaciones (Bodega CDMX y Almacén Norte)
    Y se envió PUT /v1/quotes/DAN-2026-00001/locations con el array sin la ubicación de índice 2
    Y "data.locations" tiene exactamente 2 elementos

# ═══════════════════════════════════════════════════════════
# E2E-05: Conflicto de versión — 409
# ═══════════════════════════════════════════════════════════

  @critico @concurrencia @e2e @hu-006-06
  Escenario: Guardado rechazado por conflicto de versión — formulario permanece abierto
    Dado que el agente tiene abierto el formulario de edición de la ubicación 1 con version 3
    Y otro proceso actualizó el folio dejándolo en version 4
    Cuando el agente guarda la edición y el servidor responde 409
    Entonces se muestra el toast "El folio fue modificado. Recargue para continuar."
    Y el formulario NO se cierra
    Y los datos ingresados por el agente permanecen visibles en el formulario
    Y no se navega automáticamente a ninguna otra página

# ═══════════════════════════════════════════════════════════
# E2E-06: Botón Continuar deshabilitado
# ═══════════════════════════════════════════════════════════

  @happy-path @ui @e2e @hu-006-07
  Escenario: Botón Continuar deshabilitado cuando todas las ubicaciones son incompletas
    Dado que el folio "DAN-2026-00001" tiene 2 ubicaciones:
      | Index | Nombre        | validationStatus |
      | 1     | Oficina Norte | incomplete       |
      | 2     | Bodega Sur    | incomplete       |
    Entonces el botón "Continuar →" está deshabilitado (atributo disabled presente)
    Y el tooltip del botón indica "Agrega al menos 1 ubicación calculable para continuar"
    Y el contador del encabezado muestra "0 de 2 ubicaciones calculables"

# ═══════════════════════════════════════════════════════════
# E2E-07 (negativo): Validación locationName vacío en Step 1
# ═══════════════════════════════════════════════════════════

  @error-path @validacion @e2e @hu-006-02
  Escenario: Formulario no avanza al Step 2 si el nombre de ubicación está vacío
    Dado que el agente está en el Step 1 del formulario de nueva ubicación
    Cuando deja vacío el campo "Nombre de la ubicación"
    Y hace clic en "Siguiente → Coberturas"
    Entonces se muestra el error inline "El nombre de la ubicación es obligatorio"
    Y el formulario permanece en el Step 1
    Y el campo "Nombre de la ubicación" tiene el foco (accesibilidad)
    Y no se realiza ninguna llamada API al servidor
```

---

## Feature principal — API (Newman / Supertest)

```gherkin
#language: es
Característica: Gestión de Ubicaciones de Riesgo — SPEC-006

  El sistema permite registrar, editar y eliminar ubicaciones de riesgo dentro de un folio.
  Cada ubicación puede ser calculable o incompleta según sus datos.
  Las ubicaciones incompletas no bloquean el guardado del folio.

# ═══════════════════════════════════════════════════════════════
# HU-006-01: Agregar ubicación calculable
# ═══════════════════════════════════════════════════════════════

  Antecedentes:
    Dado que el sistema está autenticado con credenciales "Basic dXNlcjpwYXNz"
    Y el folio "DAN-2026-00001" existe con version 2 y 0 ubicaciones

  @smoke @critico @hu-006-01
  Escenario: Agregar primera ubicación calculable al folio
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con el body:
      """
      {
        "locations": [{
          "index": 1,
          "locationName": "Bodega Central CDMX",
          "address": "Av. Industria 340",
          "zipCode": "06600",
          "state": "Ciudad de México",
          "municipality": "Cuauhtémoc",
          "neighborhood": "Doctores",
          "city": "Ciudad de México",
          "constructionType": "Tipo 1 - Macizo",
          "level": 2,
          "constructionYear": 1998,
          "locationBusinessLine": { "description": "Storage warehouse", "fireKey": "B-03" },
          "guarantees": [
            { "guaranteeKey": "building_fire", "insuredAmount": 5000000 },
            { "guaranteeKey": "glass", "insuredAmount": 0 }
          ],
          "catZone": "A"
        }],
        "version": 2
      }
      """
    Entonces la respuesta tiene status 200
    Y "data.locations[0].validationStatus" es "calculable"
    Y "data.locations[0].blockingAlerts" es un array vacío
    Y "data.version" es 3
    Y "data.locations[0].index" es 1

  @critico @hu-006-01
  Escenario: PUT con 3 ubicaciones reemplaza el array completo atómicamente
    Dado que el folio "DAN-2026-00001" tiene 2 ubicaciones y version 4
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con 3 ubicaciones (las 2 existentes + 1 nueva con index 3) y version 4
    Entonces la respuesta tiene status 200
    Y "data.locations" tiene exactamente 3 elementos
    Y "data.version" es 5
    Y "data.locations[2].index" es 3

  @happy-path @hu-006-01
  Escenario: Guardar ubicación actualiza lastWizardStep a 2
    Cuando envío PUT /v1/quotes/DAN-2026-00002/locations con 1 ubicación calculable y version 1
    Entonces la respuesta tiene status 200
    Y el campo "metadata.lastWizardStep" del folio en base de datos es 2

# ═══════════════════════════════════════════════════════════════
# HU-006-02: Datos físicos del inmueble
# ═══════════════════════════════════════════════════════════════

  @smoke @critico @hu-006-02
  Escenario: Todos los datos físicos se persisten correctamente
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con una ubicación que tiene:
      | campo            | valor              |
      | address          | Av. Industria 340  |
      | zipCode          | 06600              |
      | constructionType | Tipo 1 - Macizo    |
      | level            | 2                  |
      | constructionYear | 1998               |
    Y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].address" es "Av. Industria 340"
    Y "data.locations[0].zipCode" es "06600"
    Y "data.locations[0].constructionType" es "Tipo 1 - Macizo"
    Y "data.locations[0].level" es 2
    Y "data.locations[0].constructionYear" es 1998

  @edge-case @hu-006-02
  Escenario: Año de construcción en el borde inferior válido (1800)
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con constructionYear 1800 y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].constructionYear" es 1800

# ═══════════════════════════════════════════════════════════════
# HU-006-03: Giro comercial desde catálogo
# ═══════════════════════════════════════════════════════════════

  @happy-path @hu-006-03
  Escenario: Giro con fireKey B-03 se persiste correctamente
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con locationBusinessLine:
      """
      { "description": "Storage warehouse", "fireKey": "B-03" }
      """
    Y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].locationBusinessLine.fireKey" es "B-03"
    Y "data.locations[0].locationBusinessLine.description" es "Storage warehouse"

  @happy-path @hu-006-03
  Escenario: Ubicación sin giro se guarda con validationStatus incomplete
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con una ubicación sin campo "locationBusinessLine" y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].validationStatus" es "incomplete"
    Y "data.locations[0].blockingAlerts" contiene al menos 1 alerta

# ═══════════════════════════════════════════════════════════════
# HU-006-04: Resolución automática de código postal
# ═══════════════════════════════════════════════════════════════

  @smoke @integracion @hu-006-04
  Escenario: CP 06600 resuelve zona, estado, municipio y colonia automáticamente
    Dado que el backend puede consultar core-ohs con GET /v1/zip-codes/06600
    Cuando el frontend ingresa el CP "06600" en el formulario de ubicación
    Entonces el campo "catZone" resuelve a "A"
    Y el campo "state" resuelve a "Ciudad de México"
    Y el campo "municipality" resuelve a "Cuauhtémoc"
    Y el campo "neighborhood" resuelve a "Doctores"
    Y los 4 campos se muestran como read-only en la UI

  @integracion @hu-006-04
  Escenario: CP 03100 resuelve zona B (Benito Juárez)
    Dado que el backend puede consultar core-ohs con GET /v1/zip-codes/03100
    Cuando el frontend ingresa el CP "03100"
    Entonces "catZone" resuelve a "B"
    Y "municipality" resuelve a "Benito Juárez"
    Y "neighborhood" resuelve a "Del Valle"

  @error-path @hu-006-04
  Escenario: CP inexistente 99999 — ubicación guardada como incompleta sin bloquear
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con zipCode "99999" y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].validationStatus" es "incomplete"
    Y "data.locations[0].blockingAlerts" contiene "Código postal no encontrado"
    Y el array de ubicaciones se persiste (guardado exitoso)

  @error-path @integracion @hu-006-04
  Escenario: core-ohs no disponible — mensaje de error sin datos geo resueltos
    Dado que core-ohs retorna 503 para GET /v1/zip-codes/06600
    Cuando el frontend intenta resolver el CP "06600"
    Entonces se muestra un mensaje de error contextual bajo el campo de CP
    Y los campos de estado, municipio y colonia permanecen vacíos
    Y el botón "Guardar" permanece habilitado (guardado parcial permitido)

# ═══════════════════════════════════════════════════════════════
# HU-006-05: Garantías con suma asegurada
# ═══════════════════════════════════════════════════════════════

  @critico @regla-negocio @hu-006-05
  Escenario: Garantía building_fire con insuredAmount 5000000 — calculable
    Cuando envío PUT con guarantees:
      """
      [
        { "guaranteeKey": "building_fire", "insuredAmount": 5000000 },
        { "guaranteeKey": "glass", "insuredAmount": 0 }
      ]
      """
    Y CP "06600" y fireKey "B-03" presentes
    Entonces "data.locations[0].validationStatus" es "calculable"
    Y "data.locations[0].guarantees[0].insuredAmount" es 5000000
    Y "data.locations[0].guarantees[1].insuredAmount" es 0

  @critico @regla-negocio @hu-006-05
  Escenario: Garantía glass con insuredAmount 0 no genera alerta (requiresInsuredAmount false)
    Cuando envío PUT con guarantees incluyendo glass con insuredAmount 0
    Y CP válido y fireKey presentes
    Entonces "data.locations[0].validationStatus" es "calculable"
    Y "data.locations[0].blockingAlerts" no contiene alertas relacionadas con glass

  @critico @regla-negocio @hu-006-05
  Escenario: Garantía building_fire con insuredAmount 0 genera alerta (requiresInsuredAmount true)
    Cuando envío PUT con guarantees incluyendo building_fire con insuredAmount 0
    Y CP "06600" y fireKey "B-03" presentes
    Entonces "data.locations[0].validationStatus" es "incomplete"
    Y "data.locations[0].blockingAlerts" contiene alerta de suma asegurada faltante
    Pero la respuesta tiene status 200 (no bloquea el guardado)

  @error-path @hu-006-05
  Escenario: Garantía con key inválida retorna 400
    Cuando envío PUT con guarantees incluyendo:
      """
      { "guaranteeKey": "invalid_guarantee_key", "insuredAmount": 100000 }
      """
    Entonces la respuesta tiene status 400
    Y "type" es "validationError"
    Y "message" contiene "Clave de garantía inválida: invalid_guarantee_key"
    Y "field" es "locations[0].guarantees"

# ═══════════════════════════════════════════════════════════════
# HU-006-06: Edición PATCH ubicación individual
# ═══════════════════════════════════════════════════════════════

  @smoke @critico @hu-006-06
  Escenario: PATCH edita solo la ubicación index 2 — las demás permanecen intactas
    Dado que el folio "DAN-2026-00001" tiene 3 ubicaciones con indices 1, 2, 3 y version 5
    Cuando envío PATCH /v1/quotes/DAN-2026-00001/locations/2 con body:
      """
      {
        "zipCode": "03100",
        "state": "Ciudad de México",
        "municipality": "Benito Juárez",
        "neighborhood": "Del Valle",
        "city": "Ciudad de México",
        "catZone": "B",
        "version": 5
      }
      """
    Entonces la respuesta tiene status 200
    Y "data.index" es 2
    Y "data.zipCode" es "03100"
    Y "data.catZone" es "B"
    Y "data.version" es 6
    Y la ubicación index 1 permanece sin cambios en base de datos
    Y la ubicación index 3 permanece sin cambios en base de datos

  @critico @hu-006-06
  Escenario: PATCH recalcula validationStatus al actualizar datos de la ubicación
    Dado que la ubicación index 2 está "incomplete" (sin zipCode ni giro)
    Cuando envío PATCH /v1/quotes/DAN-2026-00001/locations/2 con zipCode "06600", fireKey "B-03" y garantía building_fire con insuredAmount 3000000 y version 5
    Entonces "data.validationStatus" es "calculable"
    Y "data.blockingAlerts" es un array vacío

  @error-path @hu-006-06
  Escenario: PATCH con índice inexistente retorna 404
    Dado que el folio "DAN-2026-00001" tiene 3 ubicaciones
    Cuando envío PATCH /v1/quotes/DAN-2026-00001/locations/99 con version 5
    Entonces la respuesta tiene status 404
    Y "type" es "folioNotFound"
    Y "message" contiene "La ubicación con índice 99 no existe en el folio"
    Y "field" es null

  @error-path @hu-006-06
  Escenario: PATCH con zipCode de 4 dígitos retorna 400
    Cuando envío PATCH /v1/quotes/DAN-2026-00001/locations/2 con zipCode "0660" y version 5
    Entonces la respuesta tiene status 400
    Y "type" es "validationError"
    Y "message" es "El código postal debe ser de 5 dígitos"
    Y "field" es "zipCode"

# ═══════════════════════════════════════════════════════════════
# HU-006-07: Resumen de ubicaciones
# ═══════════════════════════════════════════════════════════════

  @happy-path @hu-006-07
  Escenario: GET summary con 2 calculables y 1 incompleta
    Dado que el folio "DAN-2026-00001" tiene 3 ubicaciones:
      | index | locationName    | validationStatus |
      | 1     | Bodega CDMX     | calculable       |
      | 2     | Sucursal Valle  | calculable       |
      | 3     | Almacén Norte   | incomplete       |
    Cuando envío GET /v1/quotes/DAN-2026-00001/locations/summary
    Entonces la respuesta tiene status 200
    Y "data.totalCalculable" es 2
    Y "data.totalIncomplete" es 1
    Y "data.locations" tiene 3 elementos
    Y "data.locations[2].blockingAlerts" no está vacío

  @happy-path @hu-006-07
  Escenario: GET summary de folio sin ubicaciones retorna totales en cero
    Cuando envío GET /v1/quotes/DAN-2026-00002/locations/summary
    Entonces la respuesta tiene status 200
    Y "data.totalCalculable" es 0
    Y "data.totalIncomplete" es 0
    Y "data.locations" es un array vacío

# ═══════════════════════════════════════════════════════════════
# HU-006-08: Guardado parcial sin bloqueo
# ═══════════════════════════════════════════════════════════════

  @critico @regla-negocio @hu-006-08
  Escenario: Ubicación sin zipCode se guarda con status incomplete
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con una ubicación que NO tiene zipCode y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].validationStatus" es "incomplete"
    Y "data.locations[0].blockingAlerts" contiene "Código postal requerido"

  @critico @regla-negocio @hu-006-08
  Escenario: Mezcla calculable + incompleta — ambas persisten sin bloqueo
    Dado que el folio "DAN-2026-00001" tiene version 2
    Cuando envío PUT con 2 ubicaciones:
      | index | locationName   | zipCode | fireKey | garantías                           |
      | 1     | Bodega CDMX    | 06600   | B-03    | building_fire:5000000, glass:0      |
      | 2     | Almacén Norte  | (vacío) | (vacío) | (vacías)                            |
    Y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].validationStatus" es "calculable"
    Y "data.locations[1].validationStatus" es "incomplete"
    Y "data.locations" tiene exactamente 2 elementos

  @critico @regla-negocio @hu-006-08
  Escenario: Ubicación sin garantías se guarda como incompleta
    Cuando envío PUT con una ubicación con CP válido "06600" y giro "B-03" pero sin garantías y version 2
    Entonces la respuesta tiene status 200
    Y "data.locations[0].validationStatus" es "incomplete"
    Y "data.locations[0].blockingAlerts" no está vacío

# ═══════════════════════════════════════════════════════════════
# Validaciones de formulario
# ═══════════════════════════════════════════════════════════════

  @error-path @validacion
  Escenario: locationName vacío retorna 400
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con locationName "" y version 2
    Entonces la respuesta tiene status 400
    Y "type" es "validationError"
    Y "message" contiene "El nombre de la ubicación es obligatorio"
    Y "field" es "locations[0].locationName"

  @error-path @validacion
  Esquema del escenario: Validaciones de campos opcionales pasan solo cuando el valor es inválido
    Dado que el campo "<campo>" tiene el valor "<valor>"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con version 2
    Entonces la respuesta tiene status <status>
    Y el mensaje de error contiene "<mensaje>"

    Ejemplos:
      | campo             | valor  | status | mensaje                            |
      | zipCode           | ABCDE  | 400    | El código postal debe ser de 5 dígitos |
      | zipCode           | 0660   | 400    | El código postal debe ser de 5 dígitos |
      | level             | -1     | 400    | El nivel debe ser un número positivo  |
      | constructionYear  | 1799   | 400    | El año de construcción es inválido    |
      | constructionYear  | 2099   | 400    | El año de construcción es inválido    |

  @error-path @validacion
  Escenario: locationName con 201 caracteres retorna 400
    Cuando envío PUT con locationName de 201 caracteres "A" repetida y version 2
    Entonces la respuesta tiene status 400
    Y "field" es "locations[0].locationName"

  @error-path @validacion
  Escenario: address vacío retorna 400
    Cuando envío PUT con address "" y version 2
    Entonces la respuesta tiene status 400
    Y "message" contiene "La dirección es obligatoria"
    Y "field" es "locations[0].address"

# ═══════════════════════════════════════════════════════════════
# Eliminación implícita vía PUT
# ═══════════════════════════════════════════════════════════════

  @happy-path @regla-negocio
  Escenario: PUT sin una ubicación del folio la elimina implícitamente
    Dado que el folio "DAN-2026-00001" tiene 3 ubicaciones con indices 1, 2, 3 y version 4
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con solo las ubicaciones index 1 y 3 y version 4
    Entonces la respuesta tiene status 200
    Y "data.locations" tiene exactamente 2 elementos
    Y "data.locations" no contiene ninguna ubicación con index 2
    Y "data.version" es 5

  @happy-path @regla-negocio
  Escenario: PUT con array vacío elimina todas las ubicaciones
    Dado que el folio "DAN-2026-00001" tiene 2 ubicaciones y version 3
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con locations [] y version 3
    Entonces la respuesta tiene status 200
    Y "data.locations" es un array vacío
    Y "data.version" es 4

# ═══════════════════════════════════════════════════════════════
# Botón "Continuar →" (UI)
# ═══════════════════════════════════════════════════════════════

  @happy-path @ui
  Escenario: Botón Continuar activo cuando hay al menos 1 ubicación calculable
    Dado que el folio "DAN-2026-00001" tiene 1 ubicación con validationStatus "calculable"
    Cuando el usuario está en la página /quotes/DAN-2026-00001/locations
    Entonces el botón "Continuar →" está habilitado
    Y muestra el texto "Continuar → (1 calculable)"

  @happy-path @ui
  Escenario: Botón Continuar deshabilitado cuando todas las ubicaciones son incompletas
    Dado que el folio "DAN-2026-00001" tiene 2 ubicaciones ambas con validationStatus "incomplete"
    Cuando el usuario está en la página /quotes/DAN-2026-00001/locations
    Entonces el botón "Continuar →" está deshabilitado

# ═══════════════════════════════════════════════════════════════
# Conflicto de versión 409
# ═══════════════════════════════════════════════════════════════

  @critico @concurrencia
  Escenario: PUT con version desactualizada retorna 409
    Dado que el folio "DAN-2026-00001" tiene version actual 5
    Cuando envío PUT /v1/quotes/DAN-2026-00001/locations con version 3
    Entonces la respuesta tiene status 409
    Y "type" es "versionConflict"
    Y "message" es "El folio fue modificado por otro proceso. Recargue para continuar"
    Y "field" es null

  @critico @concurrencia
  Escenario: PATCH con version desactualizada retorna 409
    Dado que el folio "DAN-2026-00001" tiene version actual 8
    Cuando envío PATCH /v1/quotes/DAN-2026-00001/locations/1 con version 6
    Entonces la respuesta tiene status 409
    Y "type" es "versionConflict"

  @critico @concurrencia @ui
  Escenario: UI muestra toast y fuerza recarga cuando el backend retorna 409
    Dado que el usuario guarda una ubicación y el servidor responde 409
    Entonces aparece el toast: "El folio fue modificado. Recargue para continuar"
    Y los datos del folio se recargan (re-fetch automático desde la API)
    Y el formulario se cierra

# ═══════════════════════════════════════════════════════════════
# Autenticación
# ═══════════════════════════════════════════════════════════════

  @seguridad
  Escenario: GET sin credenciales retorna 401
    Dado que el request no incluye el header Authorization
    Cuando envío GET /v1/quotes/DAN-2026-00001/locations
    Entonces la respuesta tiene status 401
    Y "type" es "unauthorized"
    Y "message" es "Credenciales inválidas o ausentes"

  @seguridad
  Escenario: PUT con folio inexistente retorna 404
    Cuando envío PUT /v1/quotes/DAN-2026-99999/locations con credenciales válidas y version 1
    Entonces la respuesta tiene status 404
    Y "type" es "folioNotFound"
    Y "message" contiene "DAN-2026-99999"
```

---

## Notas de implementación para automatización

| ID | Escenario | Nivel recomendado | Framework |
|---|---|---|---|
| GHK-001 | Agregar ubicación calculable | API + Integration | xUnit / Newman |
| GHK-002 | PUT atómico 3 ubicaciones | API | xUnit |
| GHK-003 | PATCH sin afectar otras ubicaciones | API | xUnit |
| GHK-004 | Resolución CP 06600 | Integration | xUnit + core-ohs mock |
| GHK-005 | CP inexistente — guardado parcial | Integration | xUnit + core-ohs mock |
| GHK-006 | Garantía flat-rate insuredAmount 0 | API | xUnit |
| GHK-007 | 409 PUT version desactualizada | API | xUnit |
| GHK-008 | Flujo E2E agregar ubicación calculable | E2E | Playwright |
| GHK-009 | Flujo E2E eliminación con modal | E2E | Playwright |
| GHK-010 | Toast 409 + recarga forzada | E2E | Playwright |
