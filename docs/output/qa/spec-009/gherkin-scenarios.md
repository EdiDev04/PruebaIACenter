# Gherkin Scenarios — SPEC-009: Motor de Cálculo de Primas

> **Feature:** `premium-calculation-engine`  
> **Spec:** SPEC-009 | **Estado:** IN_PROGRESS  
> **Generado:** 2026-03-30 | **Agente:** QA Agent

---

```gherkin
#language: es
Característica: Motor de Cálculo de Primas — POST /v1/quotes/{folio}/calculate

  El motor lee el folio persisted, valida ubicaciones, consulta tarifas de core-ohs,
  calcula prima neta por ubicación y devuelve la prima comercial consolidada a nivel folio.

  Contexto:
    Dado que el sistema tiene autenticación Basic Auth activa
    Y el folio "DAN-2026-00001" existe en estado "in_progress" con versión 5
    Y el folio tiene las siguientes ubicaciones:
      | índice | nombre                  | CP    | fireKey | validationStatus | garantías                            |
      | 1      | Bodega Central CDMX     | 06600 | B-03    | calculable       | building_fire, contents_fire, cat_tev, glass, theft |
      | 2      | Sucursal Monterrey      | 64000 | B-01    | calculable       | building_fire, cat_tev               |
      | 3      | Local sin datos         | —     | —       | incomplete       | —                                    |
    Y core-ohs tiene activas las tarifas de incendio, CAT y parámetros de cálculo:
      | fireKey | baseRate |
      | B-03    | 0.00125  |
      | B-01    | 0.00080  |
    Y los parámetros de cálculo son:
      | expeditionExpenses | agentCommission | issuingRights | surcharges | iva  |
      | 0.05               | 0.10            | 0.03          | 0.02       | 0.16 |

  # ─────────────────────────────────────────────────────────────────────────
  # HU-009-01: Ejecutar cálculo exitoso con ubicaciones mixtas
  # ─────────────────────────────────────────────────────────────────────────

  @smoke @critico @HU-009-01
  Escenario: Cálculo exitoso con 2 ubicaciones calculables y 1 incompleta
    Dado que el folio "DAN-2026-00001" tiene 2 ubicaciones calculables y 1 incompleta
    Y la versión actual del folio es 5
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{ "version": 5 }'
    Entonces la respuesta tiene status 200
    Y el body contiene el objeto "data"
    Y "data.netPremium" es mayor que 0
    Y "data.commercialPremiumBeforeTax" es mayor que "data.netPremium"
    Y "data.commercialPremium" es mayor que "data.commercialPremiumBeforeTax"
    Y "data.quoteStatus" es "calculated"
    Y "data.version" es 6
    Y "data.premiumsByLocation" contiene 3 elementos

  @smoke @critico @HU-009-01
  Escenario: La fórmula comercial aplica correctamente los parámetros globales
    Dado que el cálculo produjo una prima neta de 125430.50
    Y los parámetros son expeditionExpenses=0.05, agentCommission=0.10, issuingRights=0.03, surcharges=0.02, iva=0.16
    Cuando evalúo las fórmulas financieras
    Entonces "data.commercialPremiumBeforeTax" es 150516.60
      # Fórmula: 125430.50 × (1 + 0.05 + 0.10 + 0.03 + 0.02) = 125430.50 × 1.20 = 150516.60
    Y "data.commercialPremium" es 174599.26
      # Fórmula: 150516.60 × (1 + 0.16) = 150516.60 × 1.16 = 174599.26

  @error-path @HU-009-01
  Escenario: Folio no existe — 404
    Dado que el folio "DAN-2026-99999" no existe en el sistema
    Cuando envío POST "/v1/quotes/DAN-2026-99999/calculate" con body '{ "version": 1 }'
    Entonces la respuesta tiene status 404
    Y el body contiene '{ "type": "folioNotFound", "message": "El folio DAN-2026-99999 no existe" }'

  @error-path @HU-009-01
  Escenario: Sin autenticación — 401
    Dado que envío la petición sin cabecera Authorization
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{ "version": 5 }'
    Entonces la respuesta tiene status 401
    Y el body contiene '{ "type": "unauthorized", "message": "Credenciales inválidas o ausentes" }'

  @error-path @HU-009-01
  Escenario: Folio con formato inválido — 400
    Cuando envío POST "/v1/quotes/INVALID-FOLIO/calculate" con body '{ "version": 1 }'
    Entonces la respuesta tiene status 400
    Y el body contiene '{ "type": "validationError", "field": "folio", "message": "Formato de folio inválido. Use DAN-YYYY-NNNNN" }'

  @error-path @HU-009-01
  Escenario: version ausente en el body — 400
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{}'
    Entonces la respuesta tiene status 400
    Y el body contiene '{ "type": "validationError", "field": "version", "message": "La versión es obligatoria" }'

  @error-path @HU-009-01
  Escenario: 0 ubicaciones calculables — 422
    Dado que el folio "DAN-2026-00002" existe con versión 2
    Y todas sus ubicaciones tienen validationStatus "incomplete"
    Cuando envío POST "/v1/quotes/DAN-2026-00002/calculate" con body '{ "version": 2 }'
    Entonces la respuesta tiene status 422
    Y el body contiene '{ "type": "invalidQuoteState", "message": "No hay ubicaciones calculables para ejecutar el cálculo" }'
    Y el folio NO fue modificado en base de datos

  # ─────────────────────────────────────────────────────────────────────────
  # HU-009-02: Desglose de prima por ubicación
  # ─────────────────────────────────────────────────────────────────────────

  @smoke @critico @HU-009-02
  Escenario: Ubicación calculable tiene desglose por cobertura
    Dado que el cálculo fue exitoso para "DAN-2026-00001"
    Cuando reviso "data.premiumsByLocation[0]" correspondiente a la Bodega Central CDMX
    Entonces "locationIndex" es 1
    Y "locationName" es "Bodega Central CDMX"
    Y "validationStatus" es "calculable"
    Y "netPremium" es mayor que 0
    Y "coveragePremiums" contiene al menos 1 elemento
    Y cada elemento de "coveragePremiums" tiene los campos "guaranteeKey", "insuredAmount", "rate" y "premium"
    Y cada "premium" = "insuredAmount" × "rate" con 2 decimales de precisión

  @smoke @critico @HU-009-02
  Escenario: Ubicación incompleta aparece en el response con netPremium 0
    Dado que el cálculo fue exitoso para "DAN-2026-00001"
    Cuando reviso "data.premiumsByLocation[2]" correspondiente al Local sin datos
    Entonces "locationIndex" es 3
    Y "locationName" es "Local sin datos"
    Y "validationStatus" es "incomplete"
    Y "netPremium" es 0
    Y "coveragePremiums" es un arreglo vacío []

  @HU-009-02
  Escenario: La prima comercial es un valor consolidado a nivel de folio, no por ubicación
    Dado que el cálculo fue exitoso con 2 ubicaciones calculables
    Cuando reviso el objeto "data"
    Entonces "data.commercialPremium" es un único valor decimal
    Y "data.premiumsByLocation" no contiene campo "commercialPremium" en ningún elemento
    Y "data.netPremium" == suma de "netPremium" de las ubicaciones con validationStatus "calculable"

  # ─────────────────────────────────────────────────────────────────────────
  # HU-009-03: Ubicaciones incompletas no bloquean el cálculo
  # ─────────────────────────────────────────────────────────────────────────

  @smoke @critico @HU-009-03
  Escenario: 2 calculables + 1 incompleta — cálculo procede y genera alerta
    Dado que el folio "DAN-2026-00001" tiene 2 ubicaciones calculables y 1 incompleta
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{ "version": 5 }'
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation" tiene 3 elementos
    Y exactamente 2 elementos tienen "validationStatus": "calculable"
    Y exactamente 1 elemento tiene "validationStatus": "incomplete" y "netPremium": 0
    Y "data.netPremium" es igual a la suma de "netPremium" de las 2 ubicaciones calculables

  @edge-case @HU-009-03
  Escenario: Solo 1 ubicación calculable de 3 — cálculo procede
    Dado que el folio "DAN-2026-00003" tiene 1 ubicación calculable (índice 2) y 2 incompletas
    Y la ubicación 2 tiene fireKey "B-03", CP "06600", garantías ["building_fire"]
    Y la versión actual del folio es 1
    Cuando envío POST "/v1/quotes/DAN-2026-00003/calculate" con body '{ "version": 1 }'
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation[1].validationStatus" es "calculable"
    Y "data.premiumsByLocation[1].netPremium" es mayor que 0
    Y "data.netPremium" == "data.premiumsByLocation[1].netPremium"
    Y los 2 elementos incompletos tienen "netPremium": 0

  # ─────────────────────────────────────────────────────────────────────────
  # HU-009-04: Persistencia atómica y versionado optimista
  # ─────────────────────────────────────────────────────────────────────────

  @smoke @critico @HU-009-04
  Escenario: Resultado financiero persiste sin sobrescribir otras secciones
    Dado que el folio "DAN-2026-00001" tiene insuredData, locations y coverageOptions previos
    Cuando el cálculo es ejecutado exitosamente con versión 5
    Y consulto el folio con GET "/v1/quotes/DAN-2026-00001"
    Entonces "netPremium", "commercialPremiumBeforeTax", "commercialPremium" y "premiumsByLocation" están persistidos
    Y "insuredData" no fue modificado respecto al valor anterior
    Y "locations" no fueron modificadas
    Y "coverageOptions" no fueron modificadas
    Y "quoteStatus" es "calculated"
    Y "version" es 6
    Y "metadata.updatedAt" fue actualizado
    Y "metadata.lastWizardStep" es 4

  @error-path @critico @HU-009-04
  Escenario: Conflicto de versión optimista — 409
    Dado que la versión actual del folio "DAN-2026-00001" en base de datos es 5
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{ "version": 3 }'
    Entonces la respuesta tiene status 409
    Y el body contiene '{ "type": "versionConflict", "message": "El folio fue modificado por otro proceso. Recargue para continuar" }'
    Y el folio NO fue modificado en base de datos (versión sigue siendo 5)

  @edge-case @HU-009-04
  Escenario: Acceso concurrente — segunda solicitud llega después del primer cálculo
    Dado que el folio "DAN-2026-00001" tiene versión 5
    Y el primer request POST .../calculate con version=5 fue exitoso (versión pasa a 6)
    Cuando un segundo request POST .../calculate llega también con version=5
    Entonces la respuesta tiene status 409
    Y el body contiene "versionConflict"

  # ─────────────────────────────────────────────────────────────────────────
  # Integración: core-ohs no disponible
  # ─────────────────────────────────────────────────────────────────────────

  @error-path @critico @integracion
  Escenario: core-ohs no disponible — 503
    Dado que el servicio core-ohs está caído o no responde
    Y el folio "DAN-2026-00001" existe con versión 5 y ubicaciones calculables
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{ "version": 5 }'
    Entonces la respuesta tiene status 503
    Y el body contiene '{ "type": "coreOhsUnavailable", "message": "Servicio de tarifas no disponible, intente más tarde" }'
    Y el folio NO fue modificado en base de datos
    Y el error está registrado en los logs del sistema

  @edge-case @integracion
  Escenario: Timeout parcial en core-ohs durante consulta de tarifas CAT
    Dado que el endpoint GET /v1/tariffs/fire responde normalmente
    Y el endpoint GET /v1/tariffs/cat excede el timeout configurado
    Cuando envío POST "/v1/quotes/DAN-2026-00001/calculate" con body '{ "version": 5 }'
    Entonces la respuesta tiene status 503
    Y el body contiene "coreOhsUnavailable"
    Y ningún dato fue persistido (rollback implícito)

  # ─────────────────────────────────────────────────────────────────────────
  # Contexto: response envelope y contratos
  # ─────────────────────────────────────────────────────────────────────────

  @contrato @critico
  Escenario: Response exitosa tiene envelope { "data": {...} }
    Dado que el cálculo es exitoso
    Cuando reviso la estructura del body de respuesta
    Entonces el body tiene exactamente la clave raíz "data"
    Y "data" contiene "netPremium", "commercialPremiumBeforeTax", "commercialPremium", "premiumsByLocation", "quoteStatus", "version"
    Y no hay otras claves en el nivel raíz del body

  @contrato
  Escenario: Mensajes de error están en español
    Dado que ocurre cualquier error (400, 401, 404, 409, 422, 503)
    Cuando recibo la respuesta de error
    Entonces el campo "message" contiene texto en español
    Y el cuerpo tiene la estructura '{ "type": "...", "message": "...", "field": null|"..." }'
```

---

## Datos de Prueba Sintéticos

| Escenario | Variable | Valor |
|---|---|---|
| Folio principal | `folioNumber` | `DAN-2026-00001` |
| Folio sin calculables | `folioNumber` | `DAN-2026-00002` |
| Folio 1 calculable / 2 incompletas | `folioNumber` | `DAN-2026-00003` |
| Versión correcta | `version` | `5` |
| Versión en conflicto | `version` | `3` |
| fireKey calculable | `fireKey` | `B-03` (baseRate=0.00125) |
| fireKey calculable 2 | `fireKey` | `B-01` (baseRate=0.00080) |
| CP calculable CDMX | `zipCode` | `06600` (zone A, technicalLevel=2) |
| CP calculable MTY | `zipCode` | `64000` (zone B) |
| Monto asegurado edificio | `insuredAmount` | `5,000,000.00` |
| Monto asegurado contenidos | `insuredAmount` | `3,000,000.00` |
| Prima neta esperada | `netPremium` | `125,430.50` |
| Prima comercial sin IVA | `commercialPremiumBeforeTax` | `150,516.60` |
| Prima comercial con IVA | `commercialPremium` | `174,599.26` |
| expensesTotal (suma parámetros) | — | `0.20` (5+10+3+2) |
| IVA | `iva` | `0.16` |

---

## Cobertura por HU

| HU | Happy Path | Error Path | Edge Case | Total |
|---|---|---|---|---|
| HU-009-01 | 2 | 5 | — | 7 |
| HU-009-02 | 3 | — | — | 3 |
| HU-009-03 | 1 | — | 1 | 2 |
| HU-009-04 | 1 | 1 | 1 | 3 |
| Integración core-ohs | — | 1 | 1 | 2 |
| Contratos/envelope | 1 | 1 | — | 2 |
| **Total** | **8** | **8** | **3** | **19** |
