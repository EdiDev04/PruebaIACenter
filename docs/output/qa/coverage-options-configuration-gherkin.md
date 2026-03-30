# Escenarios Gherkin — SPEC-007: Configuración de Opciones de Cobertura

> **Fuente:** `coverage-options-configuration.spec.md` v1.0 + `coverage-options-configuration.design.md` (APPROVED)
> **Generado:** 2026-03-29
> **Estado:** READY
> **Prerequisito:** `code-quality-report.md` → `QUALITY_GATE: PASSED` ✅
> **Ciclo ASDD:** Lite — tests unitarios diferidos a siguiente iteración

---

## Resumen de cobertura

| # | Capa | HU | Flujo | Escenarios | Etiquetas |
|---|---|---|---|---|---|
| API-01 | API | HU-007-01 | GET coverage-options — folio con configuración | 1 | `@smoke @critico` |
| API-02 | API | HU-007-01 | GET coverage-options — folio sin configuración (defaults) | 1 | `@smoke @regla-negocio` |
| API-03 | API | HU-007-01 | GET coverage-options — folio inexistente | 1 | `@error-path` |
| API-04 | API | HU-007-01 | GET coverage-options — folio con formato inválido | 1 | `@error-path @validacion` |
| API-05 | API | HU-007-01 | GET coverage-options — sin credenciales | 1 | `@seguridad` |
| API-06 | API | HU-007-01 | PUT coverage-options — actualización exitosa | 1 | `@smoke @critico` |
| API-07 | API | HU-007-01 | PUT coverage-options — conflicto de versión (409) | 1 | `@critico @concurrencia` |
| API-08 | API | HU-007-01 | PUT coverage-options — enabledGuarantees vacío | 1 | `@error-path @validacion` |
| API-09 | API | HU-007-04 | PUT coverage-options — key de garantía inválida | 1 | `@error-path @validacion` |
| API-10 | API | HU-007-01 | PUT coverage-options — deducible fuera de rango | 1 | `@error-path @validacion` |
| API-11 | API | HU-007-01 | PUT coverage-options — coaseguro fuera de rango | 1 | `@error-path @validacion` |
| API-12 | API | HU-007-01 | PUT coverage-options — sin credenciales | 1 | `@seguridad` |
| API-13 | API | HU-007-03 | GET catalogs/guarantees — proxy exitoso (14 garantías) | 1 | `@smoke @critico @integracion` |
| API-14 | API | HU-007-03 | GET catalogs/guarantees — core-ohs no disponible (503) | 1 | `@critico @integracion @resiliencia` |
| E2E-01 | UI / Wizard | HU-007-01 + 03 | Carga inicial del formulario con datos existentes | 1 | `@smoke @critico @e2e` |
| E2E-02 | UI / Wizard | HU-007-04 | Warning al deshabilitar garantía usada en ubicaciones | 1 | `@critico @regla-negocio @e2e` |
| E2E-03 | UI / Wizard | HU-007-04 | Cancelar deshabilitación desde el dialog | 1 | `@happy-path @e2e` |
| E2E-04 | UI / Wizard | HU-007-01 | Guardado exitoso con toast de confirmación | 1 | `@smoke @critico @e2e` |
| E2E-05 | UI / Wizard | HU-007-01 | Conflicto de versión — banner ámbar y recarga | 1 | `@critico @concurrencia @e2e` |
| E2E-06 | UI / Wizard | HU-007-03 | Catálogo no disponible — banner de error y reintento | 1 | `@critico @resiliencia @e2e` |

**Total: 20 escenarios** (14 API + 6 E2E UI)

---

## Datos de prueba sintéticos

| Fixture | Campo | Valor |
|---|---|---|
| Folio con opciones configuradas | `folioNumber` | `DAN-2026-00001` |
| Folio sin opciones configuradas (defaults) | `folioNumber` | `DAN-2026-00002` |
| Folio inexistente | `folioNumber` | `DAN-2026-99999` |
| Folio con formato inválido | `folioNumber` | `INVALID-001` |
| Versión actual del folio con config | `version` | `4` |
| Versión desactualizada (conflicto) | `version` | `2` |
| Garantías habilitadas (5 seleccionadas) | `enabledGuarantees` | `["building_fire","contents_fire","cat_tev","theft","glass"]` |
| Todas las garantías (14 habilitadas) | `enabledGuarantees` | `["building_fire","contents_fire","coverage_extension","cat_tev","cat_fhm","debris_removal","extraordinary_expenses","rent_loss","business_interruption","electronic_equipment","theft","cash_and_securities","glass","illuminated_signs"]` |
| Array vacío (inválido) | `enabledGuarantees` | `[]` |
| Key inválida | `guaranteeKey` | `"invalid_key_xyz"` |
| Deducible válido (5%) | `deductiblePercentage` | `0.05` |
| Coaseguro válido (10%) | `coinsurancePercentage` | `0.10` |
| Deducible fuera de rango (mayor a 1) | `deductiblePercentage` | `1.50` |
| Coaseguro fuera de rango (negativo) | `coinsurancePercentage` | `-0.01` |
| Credenciales válidas | `Authorization` | `Basic dXNlcjpwYXNz` |
| Sin credenciales | `Authorization` | (ausente) |
| Garantía con ubicaciones afectadas | `guaranteeKey` | `building_fire` |
| Count de ubicaciones afectadas | `affectedCount` | `3` |

---

## Escenarios API — `GET /v1/quotes/{folio}/coverage-options`

```gherkin
#language: es
Característica: Consulta de opciones de cobertura (GET) — SPEC-007

  El sistema retorna las opciones de cobertura configuradas para un folio.
  Si el folio no tiene opciones configuradas, retorna los valores por defecto
  (todas las 14 garantías habilitadas, deducible 0, coaseguro 0).

  Antecedentes:
    Dado que el sistema tiene la colección de cotizaciones en MongoDB
    Y el folio "DAN-2026-00001" existe con opciones de cobertura:
      | enabledGuarantees     | ["building_fire","contents_fire","cat_tev","theft","glass"] |
      | deductiblePercentage  | 0.05                                                        |
      | coinsurancePercentage | 0.10                                                        |
      | version               | 4                                                           |
    Y el folio "DAN-2026-00002" existe sin opciones de cobertura configuradas


  @smoke @critico
  Escenario: API-01 — GET coverage-options en folio con configuración existente
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío GET /v1/quotes/DAN-2026-00001/coverage-options
    Entonces la respuesta tiene status HTTP 200
    Y el body tiene la estructura { "data": { ... } }
    Y "data.enabledGuarantees" es ["building_fire","contents_fire","cat_tev","theft","glass"]
    Y "data.deductiblePercentage" es 0.05
    Y "data.coinsurancePercentage" es 0.10
    Y "data.version" es 4


  @smoke @regla-negocio
  Escenario: API-02 — GET coverage-options en folio sin configuración retorna defaults
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío GET /v1/quotes/DAN-2026-00002/coverage-options
    Entonces la respuesta tiene status HTTP 200
    Y "data.enabledGuarantees" contiene exactamente 14 claves:
      """
      ["building_fire","contents_fire","coverage_extension","cat_tev","cat_fhm",
       "debris_removal","extraordinary_expenses","rent_loss","business_interruption",
       "electronic_equipment","theft","cash_and_securities","glass","illuminated_signs"]
      """
    Y "data.deductiblePercentage" es 0
    Y "data.coinsurancePercentage" es 0
    Y "data.version" existe y es un entero mayor que 0


  @error-path
  Escenario: API-03 — GET coverage-options en folio inexistente retorna 404
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío GET /v1/quotes/DAN-2026-99999/coverage-options
    Entonces la respuesta tiene status HTTP 404
    Y "type" es "folioNotFound"
    Y "message" contiene "DAN-2026-99999"
    Y "message" está en español


  @error-path @validacion
  Escenario: API-04 — GET coverage-options con formato de folio inválido retorna 400
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío GET /v1/quotes/INVALID-001/coverage-options
    Entonces la respuesta tiene status HTTP 400
    Y "type" es "validationError"
    Y "message" contiene "Formato de folio inválido"
    Y "field" es "folio"


  @seguridad
  Escenario: API-05 — GET coverage-options sin credenciales retorna 401
    Dado que NO incluyo el header Authorization en la solicitud
    Cuando envío GET /v1/quotes/DAN-2026-00001/coverage-options
    Entonces la respuesta tiene status HTTP 401
    Y "type" es "unauthorized"
    Y "message" está en español
```

---

## Escenarios API — `PUT /v1/quotes/{folio}/coverage-options`

```gherkin
#language: es
Característica: Guardado de opciones de cobertura (PUT) — SPEC-007

  El sistema persiste únicamente la sección coverageOptions del folio
  usando versionado optimista. Incrementa la versión en 1 y actualiza
  metadata.lastWizardStep a 3. No modifica ubicaciones existentes.

  Antecedentes:
    Dado que el folio "DAN-2026-00001" existe con version 3 en MongoDB
    Y el folio NO tiene opciones de cobertura configuradas previamente


  @smoke @critico
  Escenario: API-06 — PUT coverage-options con datos válidos actualiza correctamente
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": ["building_fire","contents_fire","cat_tev","theft","glass"],
        "deductiblePercentage": 0.05,
        "coinsurancePercentage": 0.10,
        "version": 3
      }
      """
    Entonces la respuesta tiene status HTTP 200
    Y el body tiene la estructura { "data": { ... } }
    Y "data.enabledGuarantees" es ["building_fire","contents_fire","cat_tev","theft","glass"]
    Y "data.deductiblePercentage" es 0.05
    Y "data.coinsurancePercentage" es 0.10
    Y "data.version" es 4
    Y en MongoDB el campo "coverageOptions.enabledGuarantees" contiene exactamente 5 claves
    Y en MongoDB el campo "metadata.lastWizardStep" es 3
    Y en MongoDB el campo "metadata.updatedAt" es posterior al valor anterior


  @critico @concurrencia
  Escenario: API-07 — PUT coverage-options con versión desactualizada retorna 409
    Dado que el folio "DAN-2026-00001" tiene version 3 en la base de datos
    Y tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": ["building_fire","contents_fire"],
        "deductiblePercentage": 0.10,
        "coinsurancePercentage": 0.05,
        "version": 2
      }
      """
    Entonces la respuesta tiene status HTTP 409
    Y "type" es "versionConflict"
    Y "message" contiene "modificado por otro proceso"
    Y en MongoDB la versión del folio continúa siendo 3
    Y en MongoDB coverageOptions NO fue modificado


  @error-path @validacion
  Escenario: API-08 — PUT coverage-options con enabledGuarantees vacío retorna 400
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": [],
        "deductiblePercentage": 0.05,
        "coinsurancePercentage": 0.10,
        "version": 3
      }
      """
    Entonces la respuesta tiene status HTTP 400
    Y "type" es "validationError"
    Y "message" es "Debe habilitar al menos una garantía"
    Y "field" es "enabledGuarantees"
    Y el folio NO fue modificado en la base de datos


  @error-path @validacion
  Escenario: API-09 — PUT coverage-options con clave de garantía inválida retorna 400
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": ["building_fire","invalid_key_xyz"],
        "deductiblePercentage": 0.05,
        "coinsurancePercentage": 0.10,
        "version": 3
      }
      """
    Entonces la respuesta tiene status HTTP 400
    Y "type" es "validationError"
    Y "message" contiene "Clave de garantía inválida"
    Y "message" contiene "invalid_key_xyz"
    Y "field" es "enabledGuarantees"


  @error-path @validacion
  Escenario: API-10 — PUT coverage-options con deducible mayor a 1 retorna 400
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": ["building_fire","contents_fire"],
        "deductiblePercentage": 1.50,
        "coinsurancePercentage": 0.10,
        "version": 3
      }
      """
    Entonces la respuesta tiene status HTTP 400
    Y "type" es "validationError"
    Y "message" contiene "porcentaje de deducible"
    Y "message" contiene "entre 0 y 1"
    Y "field" es "deductiblePercentage"


  @error-path @validacion
  Escenario: API-11 — PUT coverage-options con coaseguro negativo retorna 400
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": ["building_fire","contents_fire"],
        "deductiblePercentage": 0.05,
        "coinsurancePercentage": -0.01,
        "version": 3
      }
      """
    Entonces la respuesta tiene status HTTP 400
    Y "type" es "validationError"
    Y "message" contiene "porcentaje de coaseguro"
    Y "field" es "coinsurancePercentage"


  @seguridad
  Escenario: API-12 — PUT coverage-options sin credenciales retorna 401
    Dado que NO incluyo el header Authorization en la solicitud
    Cuando envío PUT /v1/quotes/DAN-2026-00001/coverage-options con body:
      """json
      {
        "enabledGuarantees": ["building_fire"],
        "deductiblePercentage": 0.05,
        "coinsurancePercentage": 0.10,
        "version": 3
      }
      """
    Entonces la respuesta tiene status HTTP 401
    Y "type" es "unauthorized"
```

---

## Escenarios API — `GET /v1/catalogs/guarantees`

```gherkin
#language: es
Característica: Catálogo de garantías via proxy (GET) — SPEC-007

  El backend actúa como proxy entre el frontend y core-ohs.
  Retorna exactamente 14 garantías con los campos: key, name, description,
  category y requiresInsuredAmount. Ante fallo de core-ohs retorna 503.

  Antecedentes:
    Dado que tengo credenciales válidas "Basic dXNlcjpwYXNz"


  @smoke @critico @integracion
  Escenario: API-13 — GET catalogs/guarantees retorna las 14 garantías del catálogo
    Dado que core-ohs está disponible y responde correctamente
    Cuando envío GET /v1/catalogs/guarantees
    Entonces la respuesta tiene status HTTP 200
    Y "data" es un array de exactamente 14 elementos
    Y cada elemento de "data" contiene los campos: "key", "name", "description", "category", "requiresInsuredAmount"
    Y "data" incluye el elemento con "key": "building_fire", "name": "Incendio Edificios", "category": "fire", "requiresInsuredAmount": true
    Y "data" incluye el elemento con "key": "glass", "name": "Vidrios", "category": "special", "requiresInsuredAmount": false
    Y "data" incluye el elemento con "key": "illuminated_signs", "name": "Anuncios Luminosos", "category": "special", "requiresInsuredAmount": false
    Y las categorías presentes son exactamente: "fire" (3 items), "cat" (2 items), "additional" (4 items), "special" (5 items)


  @critico @integracion @resiliencia
  Escenario: API-14 — GET catalogs/guarantees cuando core-ohs no está disponible retorna 503
    Dado que core-ohs no está disponible (timeout o 5xx)
    Cuando envío GET /v1/catalogs/guarantees
    Entonces la respuesta tiene status HTTP 503
    Y "type" es "coreOhsUnavailable"
    Y "message" contiene "Servicio de catálogos no disponible"
    Y "message" está en español
```

---

## Escenarios E2E — Wizard de Opciones de Cobertura (Playwright)

> Escenarios conductuales end-to-end que validan el flujo de usuario en la interfaz.
> Se ejecutan con Playwright en `cotizador-automatization/e2e/specs/`.
> Los escenarios API anteriores se ejecutan con Postman/Newman de forma independiente.

```gherkin
#language: es
Característica: Opciones de cobertura — Interfaz de usuario (SPEC-007)

  El agente del cotizador configura las opciones de cobertura en el Paso 3
  del wizard (/quotes/{folio}/technical-info). Las 14 garantías se muestran
  agrupadas en 4 categorías con checkboxes. Un warning aparece al deshabilitar
  una garantía ya usada en ubicaciones existentes.

  Antecedentes:
    Dado que el usuario está autenticado en el cotizador
    Y el folio "DAN-2026-00001" existe con 2 ubicaciones que usan "building_fire"
    Y las opciones de cobertura del folio tienen version 4:
      | enabledGuarantees     | ["building_fire","contents_fire","cat_tev","theft","glass"] |
      | deductiblePercentage  | 5.0 (mostrado como %)                                       |
      | coinsurancePercentage | 10.0 (mostrado como %)                                      |
    Y el catálogo de garantías está disponible con las 14 coberturas
    Y el usuario navega a "/quotes/DAN-2026-00001/technical-info"


  @smoke @critico @e2e
  Escenario: E2E-01 — Carga inicial del formulario con datos existentes y catálogo
    Cuando la página "/quotes/DAN-2026-00001/technical-info" termina de cargar
    Entonces el título "Opciones de Cobertura" es visible
    Y el badge contador muestra "5 de 14 habilitadas"
    Y los checkboxes "building_fire", "contents_fire", "cat_tev", "theft", "glass" están marcados
    Y los checkboxes "coverage_extension", "cat_fhm", "debris_removal" están desmarcados
    Y el campo "Deducible (%)" muestra el valor "5.0"
    Y el campo "Coaseguro (%)" muestra el valor "10.0"
    Y la sección "Coberturas de Incendio" es visible con 3 checkboxes
    Y la sección "Catástrofes" es visible con 2 checkboxes
    Y la sección "Coberturas Complementarias" es visible con 4 checkboxes
    Y la sección "Coberturas Especiales" es visible con 5 checkboxes


  @critico @regla-negocio @e2e
  Escenario: E2E-02 — Warning al deshabilitar garantía usada en 2 ubicaciones
    Dado que el formulario está cargado con las opciones existentes
    Cuando el usuario desmarca el checkbox "Incendio Edificios" (building_fire)
    Entonces aparece el dialog "Confirmar deshabilitación"
    Y el dialog muestra el mensaje: "La cobertura «Incendio Edificios» está seleccionada en 2 ubicacion(es). Si la deshabilitas, esas ubicaciones quedarán marcadas como incompletas al calcular."
    Y el dialog tiene un botón "Cancelar" y un botón "Deshabilitar"
    Y el checkbox "Incendio Edificios" permanece marcado mientras el dialog está abierto


  @happy-path @e2e
  Escenario: E2E-03 — Cancelar la deshabilitación desde el dialog no altera el estado
    Dado que el formulario está cargado con las opciones existentes
    Cuando el usuario desmarca el checkbox "Incendio Edificios" (building_fire)
    Y el dialog "Confirmar deshabilitación" aparece
    Y el usuario hace clic en el botón "Cancelar"
    Entonces el dialog se cierra
    Y el checkbox "Incendio Edificios" vuelve a estar marcado
    Y el badge contador continúa mostrando "5 de 14 habilitadas"
    Y NO se realiza ninguna llamada PUT a la API


  @smoke @critico @e2e
  Escenario: E2E-04 — Guardado exitoso muestra toast de confirmación y navega al paso 4
    Dado que el formulario está cargado con las opciones existentes
    Y el usuario cambia el deducible a "8.0"
    Y el usuario desmarca el checkbox "Vidrios" (glass)
    Y el badge muestra "4 de 14 habilitadas"
    Cuando el usuario hace clic en "Guardar y Continuar"
    Entonces el botón muestra un spinner y se deshabilita temporalmente
    Y se envía PUT /v1/quotes/DAN-2026-00001/coverage-options con:
      | enabledGuarantees     | ["building_fire","contents_fire","cat_tev","theft"] |
      | deductiblePercentage  | 0.08                                                 |
      | coinsurancePercentage | 0.10                                                 |
      | version               | 4                                                    |
    Y la respuesta es HTTP 200 con version 5
    Y aparece el toast "Opciones guardadas" con auto-dismiss de 5 segundos
    Y el usuario es redirigido al paso 4 del wizard


  @critico @concurrencia @e2e
  Escenario: E2E-05 — Conflicto de versión (409) muestra banner ámbar con botón Recargar
    Dado que el formulario está cargado con version 4 en estado local
    Y la versión persistida en la base de datos es 5 (modificación concurrente)
    Cuando el usuario hace clic en "Guardar y Continuar"
    Y la API retorna HTTP 409
    Entonces aparece un banner ámbar con el mensaje "El folio fue modificado por otro proceso. Recarga la página para ver los datos actualizados."
    Y el banner tiene un botón "Recargar"
    Y el formulario NO navega al paso 4
    Cuando el usuario hace clic en "Recargar"
    Entonces se realiza GET /v1/quotes/DAN-2026-00001/coverage-options
    Y el formulario se repuebla con los datos actualizados (version 5)


  @critico @resiliencia @e2e
  Escenario: E2E-06 — Catálogo de garantías no disponible muestra banner rojo con Reintentar
    Dado que core-ohs no está disponible
    Cuando el usuario navega a "/quotes/DAN-2026-00001/technical-info"
    Y GET /v1/catalogs/guarantees retorna HTTP 503
    Entonces aparece un banner rojo con el mensaje "No se pudo cargar el catálogo de garantías. El servicio no está disponible."
    Y el banner tiene un botón "Reintentar"
    Y el formulario NO muestra los checkboxes de garantías (no hay catálogo para renderizar)
    Cuando core-ohs se recupera
    Y el usuario hace clic en "Reintentar"
    Entonces GET /v1/catalogs/guarantees se ejecuta nuevamente
    Y las 14 garantías se cargan y los checkboxes aparecen
```

---

## Tabla de datos de prueba por escenario

| Escenario | Variable | Valor concreto |
|---|---|---|
| API-01 | folio | `DAN-2026-00001` |
| API-01 | version esperada | `4` |
| API-01 | enabledGuarantees | `["building_fire","contents_fire","cat_tev","theft","glass"]` |
| API-02 | folio | `DAN-2026-00002` |
| API-02 | count garantías default | `14` |
| API-02 | deductiblePercentage | `0` |
| API-02 | coinsurancePercentage | `0` |
| API-03 | folio | `DAN-2026-99999` |
| API-04 | folio | `INVALID-001` |
| API-06 | folio | `DAN-2026-00001` |
| API-06 | version enviada | `3` |
| API-06 | version esperada en respuesta | `4` |
| API-07 | version enviada (obsoleta) | `2` |
| API-07 | version persistida | `3` |
| API-09 | key inválida | `"invalid_key_xyz"` |
| API-10 | deductiblePercentage | `1.50` |
| API-11 | coinsurancePercentage | `-0.01` |
| API-13 | count garantías | `14` |
| API-13 | categorías | `fire:3, cat:2, additional:4, special:5` |
| E2E-01 | badge | `"5 de 14 habilitadas"` |
| E2E-02 | garantía deshabilitada | `building_fire` — "Incendio Edificios" |
| E2E-02 | count ubicaciones afectadas | `2` |
| E2E-04 | deducible nuevo | `8.0` (0.08 en API) |
| E2E-04 | enabledGuarantees | `["building_fire","contents_fire","cat_tev","theft"]` |
| E2E-05 | version local | `4` |
| E2E-05 | version en DB | `5` |
