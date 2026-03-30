# Escenarios Gherkin — SPEC-005: Configuración del Layout de Ubicaciones

> **Feature:** `location-layout-configuration`
> **Generado:** 2026-03-29
> **Derivado de:** `.github/specs/location-layout-configuration.spec.md`
> **Cubre:** HU-005-01, HU-005-02 · Criterios §2.1, Reglas §2.2, Validaciones §2.3, Contratos §3.4 y §3.5b

---

```gherkin
#language: es
Feature: Configuración del layout de ubicaciones

  Background:
    Given el folio "DAN-2026-00001" existe en el sistema con version 2
    And el folio tiene ubicaciones con datos completos
    And el usuario está autenticado con Basic Auth "dXNlcjpwYXNz"

  # ─────────────────────────────────────────────
  # FLUJOS FELICES — API Backend
  # ─────────────────────────────────────────────

  @smoke @critico @happy-path
  Scenario: GET layout retorna configuración personalizada cuando ya existe
    Given el folio "DAN-2026-00001" tiene layoutConfiguration persistido:
      | displayMode    | list                                       |
      | visibleColumns | index, locationName, validationStatus       |
      | version        | 2                                          |
    When se envía GET /v1/quotes/DAN-2026-00001/locations/layout
    Then la respuesta tiene status HTTP 200
    And el body contiene el envelope { "data": { ... } }
    And "data.displayMode" es "list"
    And "data.visibleColumns" es ["index", "locationName", "validationStatus"]
    And "data.version" es 2

  @smoke @critico @happy-path
  Scenario: GET layout retorna defaults cuando el folio no tiene configuración previa
    Given el folio "DAN-2026-00002" existe sin layoutConfiguration definida
    When se envía GET /v1/quotes/DAN-2026-00002/locations/layout
    Then la respuesta tiene status HTTP 200
    And "data.displayMode" es "grid"
    And "data.visibleColumns" es ["index", "locationName", "zipCode", "businessLine", "validationStatus"]
    And "data.version" es mayor que 0

  @smoke @critico @happy-path
  Scenario: PUT layout válido actualiza solo layoutConfiguration e incrementa version
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "list",
        "visibleColumns": ["index", "locationName", "validationStatus"],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 200
    And "data.displayMode" es "list"
    And "data.visibleColumns" es ["index", "locationName", "validationStatus"]
    And "data.version" es 3
    And en MongoDB el campo "layoutConfiguration.displayMode" del folio "DAN-2026-00001" es "list"
    And en MongoDB el campo "version" del folio "DAN-2026-00001" es 3
    And en MongoDB el campo "metadata.lastWizardStep" del folio "DAN-2026-00001" es 2
    And en MongoDB el campo "metadata.updatedAt" fue actualizado

  @critico @happy-path
  Scenario: PUT layout no afecta otras secciones del folio (actualización parcial)
    Given el folio "DAN-2026-00001" tiene insuredData, locations y coverageOptions con datos previos
    And el folio tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": ["index", "locationName", "zipCode"],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 200
    And en MongoDB "insuredData" del folio "DAN-2026-00001" permanece sin cambios
    And en MongoDB "locations" del folio "DAN-2026-00001" permanece sin cambios
    And en MongoDB "coverageOptions" del folio "DAN-2026-00001" permanece sin cambios

  @happy-path
  Scenario: PUT layout con todas las columnas válidas disponibles
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": [
          "index", "locationName", "address", "zipCode", "state",
          "municipality", "neighborhood", "city", "constructionType",
          "level", "constructionYear", "businessLine", "guarantees",
          "catZone", "validationStatus"
        ],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 200
    And "data.visibleColumns" contiene exactamente 15 elementos

  @happy-path
  Scenario: PUT layout con una sola columna visible (mínimo permitido)
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": ["index"],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 200
    And "data.visibleColumns" es ["index"]

  @happy-path
  Scenario: GET layout con header X-Correlation-Id propagado en respuesta
    Given el folio "DAN-2026-00001" existe
    When se envía GET /v1/quotes/DAN-2026-00001/locations/layout con header X-Correlation-Id: "abc-123-xyz"
    Then la respuesta tiene status HTTP 200
    And el header de respuesta X-Correlation-Id es "abc-123-xyz"

  # ─────────────────────────────────────────────
  # FLUJOS DE ERROR — API Backend
  # ─────────────────────────────────────────────

  @error-path @critico
  Scenario: GET layout con folio inexistente retorna 404
    Given el folio "DAN-2026-99999" NO existe en el sistema
    When se envía GET /v1/quotes/DAN-2026-99999/locations/layout
    Then la respuesta tiene status HTTP 404
    And el body es:
      """
      {
        "type": "folioNotFound",
        "message": "El folio DAN-2026-99999 no existe",
        "field": null
      }
      """

  @error-path @critico
  Scenario: GET layout con formato de folio inválido retorna 400
    When se envía GET /v1/quotes/FOLIO-INVALIDO/locations/layout
    Then la respuesta tiene status HTTP 400
    And "type" es "validationError"
    And "message" es "Formato de folio inválido. Use DAN-YYYY-NNNNN"
    And "field" es "folio"

  @error-path @critico
  Scenario: PUT con displayMode inválido retorna 400
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "tabla",
        "visibleColumns": ["index", "locationName"],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 400
    And "type" es "validationError"
    And "message" es "Modo de visualización inválido. Valores permitidos: grid, list"
    And "field" es "displayMode"

  @error-path @critico
  Scenario: PUT con visibleColumns vacío retorna 400
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": [],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 400
    And "type" es "validationError"
    And "message" es "Debe seleccionar al menos una columna visible"
    And "field" es "visibleColumns"

  @error-path @critico
  Scenario: PUT con columna inválida retorna 400
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": ["index", "columnaQueNoExiste"],
        "version": 2
      }
      """
    Then la respuesta tiene status HTTP 400
    And "type" es "validationError"
    And "field" es "visibleColumns"

  @error-path @critico
  Scenario: PUT con version desactualizada retorna 409 (conflicto optimista)
    Given el folio "DAN-2026-00001" tiene version 3 en MongoDB
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "list",
        "visibleColumns": ["index", "locationName"],
        "version": 1
      }
      """
    Then la respuesta tiene status HTTP 409
    And el body es:
      """
      {
        "type": "versionConflict",
        "message": "El folio fue modificado por otro proceso. Recargue para continuar",
        "field": null
      }
      """
    And en MongoDB el folio "DAN-2026-00001" mantiene version 3

  @error-path @critico
  Scenario: PUT con folio inexistente retorna 404
    Given el folio "DAN-2026-99999" NO existe en el sistema
    When se envía PUT /v1/quotes/DAN-2026-99999/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": ["index"],
        "version": 1
      }
      """
    Then la respuesta tiene status HTTP 404
    And "type" es "folioNotFound"
    And "message" es "El folio DAN-2026-99999 no existe"

  @error-path @seguridad
  Scenario: GET layout sin autenticación retorna 401
    Given el usuario no envía header Authorization
    When se envía GET /v1/quotes/DAN-2026-00001/locations/layout
    Then la respuesta tiene status HTTP 401
    And "type" es "unauthorized"
    And "message" es "Credenciales inválidas o ausentes"

  @error-path @seguridad
  Scenario: PUT layout sin autenticación retorna 401
    Given el usuario no envía header Authorization
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body válido
    Then la respuesta tiene status HTTP 401
    And "type" es "unauthorized"

  @error-path
  Scenario: PUT con version ausente en el body retorna 400
    Given el folio "DAN-2026-00001" tiene version 2
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con body:
      """
      {
        "displayMode": "grid",
        "visibleColumns": ["index", "locationName"]
      }
      """
    Then la respuesta tiene status HTTP 400
    And "type" es "validationError"
    And "field" es "version"

  # ─────────────────────────────────────────────
  # FLUJOS UI — Frontend React
  # ─────────────────────────────────────────────

  @smoke @ui @happy-path
  Scenario: Toggle entre modo grid y list muestra preview inmediato
    Given el usuario está en la página de ubicaciones del folio "DAN-2026-00001"
    And el layout actual es displayMode "grid"
    When el usuario hace clic en el botón de "Vista de lista"
    Then el LayoutConfigPanel muestra preview en modo lista inmediatamente
    And el botón "Vista de lista" aparece seleccionado
    And el botón "Vista de grilla" aparece deseleccionado
    And el cambio es local hasta que el usuario confirme guardar

  @ui @happy-path
  Scenario: Guardar layout con modo lista persiste el cambio
    Given el usuario está en la página de ubicaciones del folio "DAN-2026-00001"
    And el layout cargado tiene displayMode "grid" y version 2
    When el usuario selecciona "Vista de lista"
    And hace clic en el botón "Guardar"
    Then se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con displayMode "list" y version 2
    And la respuesta 200 invalida la query ['layout', 'DAN-2026-00001']
    And la grilla de ubicaciones se actualiza a modo lista

  @ui @critico
  Scenario: Deseleccionar la última columna es imposible (protección mínimo 1)
    Given el usuario está en la página de ubicaciones del folio "DAN-2026-00001"
    And el selector de columnas muestra solo "index" seleccionada (1 columna visible)
    When el usuario intenta desmarcar el checkbox de "index"
    Then el checkbox de "index" permanece marcado
    And el sistema muestra mensaje "Debe seleccionar al menos una columna visible"
    And el botón "Guardar" permanece deshabilitado

  @ui @critico
  Scenario: Guardar con version desactualizada muestra alerta ámbar con botón Recargar
    Given el usuario está en la página de ubicaciones del folio "DAN-2026-00001"
    And el layout fue cargado con version 1
    And otro proceso actualizó el folio a version 3 mientras el usuario editaba
    When el usuario hace clic en "Guardar"
    Then se recibe respuesta HTTP 409 del servidor
    And aparece una alerta ámbar con el mensaje "El folio fue modificado, recarga para continuar"
    And la alerta contiene el botón "Recargar"
    When el usuario hace clic en "Recargar"
    Then se recarga el layout con la versión actualizada (version 3)
    And la alerta desaparece

  @ui @happy-path
  Scenario: Restaurar predeterminados resetea al estado default
    Given el usuario está en la página de ubicaciones del folio "DAN-2026-00001"
    And el layout actual tiene displayMode "list" y visibleColumns ["index", "validationStatus"]
    When el usuario hace clic en "Restaurar predeterminados"
    Then el LayoutConfigPanel muestra displayMode "grid"
    And las columnas visibles son ["index", "locationName", "zipCode", "businessLine", "validationStatus"]
    And el estado del formulario se resetea a los valores default
    And el cambio es local hasta que el usuario confirme guardar

  @ui @happy-path
  Scenario: Layout persistido recargado correctamente al reabrir la página
    Given el usuario guardó un layout con displayMode "list" y versión 2
    When el usuario cierra la página de ubicaciones y la vuelve a abrir
    Then se ejecuta GET /v1/quotes/DAN-2026-00001/locations/layout
    And el LayoutConfigPanel muestra el modo "list" con las columnas guardadas

  @ui @edge-case
  Scenario: Estado de loading mientras GET layout está en vuelo
    Given el usuario abre la página de ubicaciones del folio "DAN-2026-00001"
    When la query ['layout', 'DAN-2026-00001'] está en estado pending
    Then el LayoutConfigPanel muestra un indicador de carga
    And los controles de configuración están deshabilitados

  @ui @edge-case
  Scenario: Sorts y pageSize son estado local y no se guardan en MongoDB
    Given el usuario está en la página de ubicaciones del folio "DAN-2026-00001"
    When el usuario ordena la grilla por "locationName" descendente
    And cambia el pageSize a 25
    And guarda el layout
    Then se envía PUT con body que solo contiene "displayMode" y "visibleColumns"
    And el body NO contiene "sortBy", "sortDirection" ni "pageSize"
    And al recargar la página el orden vuelve al default

  # ─────────────────────────────────────────────
  # EDGE CASES — Concurrencia y datos límite
  # ─────────────────────────────────────────────

  @edge-case @critico
  Scenario: Dos usuarios guardan el layout simultáneamente — solo el primero tiene éxito
    Given dos clientes A y B cargan el folio "DAN-2026-00001" con version 2
    When el cliente A envía PUT con version 2 y tiene éxito (version → 3)
    And el cliente B envía PUT con version 2 inmediatamente después
    Then el cliente B recibe HTTP 409
    And el folio mantiene la versión del cliente A (version 3)

  @edge-case
  Scenario: PUT con version 0 retorna error de validación
    Given el folio "DAN-2026-00001" existe
    When se envía PUT /v1/quotes/DAN-2026-00001/locations/layout con version 0
    Then la respuesta tiene status HTTP 400
    And "field" es "version"

  @edge-case
  Escenario: Validar displayMode
    Dado que el folio "DAN-2026-00001" existe con version 2
    Cuando se envía PUT con "<displayMode>" y visibleColumns válidas
    Entonces la respuesta tiene status "<status_esperado>"
    Ejemplos:
      | displayMode | status_esperado |
      | grid        | 200             |
      | list        | 200             |
      | tabla       | 400             |
      | Grid        | 400             |
      | LIST        | 400             |
      |             | 400             |
```

---

## Datos de prueba de referencia

| Escenario | Campo | Valor válido | Valor inválido | Valor borde |
|-----------|-------|-------------|----------------|-------------|
| GET / PUT | folio | `DAN-2026-00001` | `FOLIO-INVALIDO` | `DAN-2026-00000` |
| PUT | displayMode | `"grid"`, `"list"` | `"tabla"`, `"Grid"`, `""` | — |
| PUT | visibleColumns | `["index", "locationName"]` | `[]`, `["columnaFalsa"]` | `["index"]` (1 elemento) |
| PUT | version | `2` (coincide con BE) | `1` (desactualizada) | `0`, negativos |
| GET defaults | visibleColumns esperadas | `["index","locationName","zipCode","businessLine","validationStatus"]` | — | — |

## Cobertura de criterios de aceptación

| Criterio spec | Escenario Gherkin |
|--------------|-------------------|
| CA-HU-005-01a: GET defaults | `GET layout retorna defaults cuando el folio no tiene configuración previa` |
| CA-HU-005-01b: PUT actualiza y versiona | `PUT layout válido actualiza solo layoutConfiguration e incrementa version` |
| CA-HU-005-01c: 409 conflicto | `PUT con version desactualizada retorna 409` |
| CA-HU-005-02a: Persistencia entre sesiones | `Layout persistido recargado correctamente al reabrir la página` |
| CA-HU-005-02b: Actualización parcial | `PUT layout no afecta otras secciones del folio` |
| RN-005-03: Defaults | `GET layout retorna defaults...` |
| RN-005-04: displayMode enum | `PUT con displayMode inválido retorna 400` · `Validar displayMode` |
| RN-005-05: mínimo 1 columna | `PUT con visibleColumns vacío retorna 400` · `Deseleccionar la última...` |
| RN-005-06: lastWizardStep:2 | `PUT layout válido actualiza solo layoutConfiguration...` |
| RN-005-09: sort no persiste | `Sorts y pageSize son estado local...` |
