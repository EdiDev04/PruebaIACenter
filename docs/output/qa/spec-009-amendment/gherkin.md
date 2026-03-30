# Gherkin Scenarios — SPEC-009 Amendment v1.1
## RN-009-02b: Cruce de `enabledGuarantees` con garantías de ubicación al calcular

**Referencia:** SPEC-009 v1.1 · RN-009-02b · Origen: SPEC-007 RN-007-06, SUP-007-05  
**Generado:** 2026-03-30 · **Agente:** QA Agent

---

```gherkin
#language: es
Característica: Motor de Cálculo — Filtro de Garantías Habilitadas (RN-009-02b)

  Como motor de cálculo
  Quiero cruzar las garantías de cada ubicación contra CoverageOptions.EnabledGuarantees
  Para que una ubicación con garantías deshabilitadas no aporte prima al folio

  Background:
    Dado que existe el folio "DAN-2026-00100" con estado "in_progress" y version 3
    Y los parámetros de cálculo son: expeditionExpenses=0.10, agentCommission=0.05,
      issuingRights=0.02, surcharges=0.01, iva=0.16
    Y las tarifas de incendio para fireKey "A-01" tienen baseRate=0.004
    Y el CP "64000" tiene technicalLevel=2

  # ─────────────────────────────────────────────────────────────────
  # AMEND-009-GH-01: Happy path — ubicación con garantía habilitada
  # ─────────────────────────────────────────────────────────────────
  @smoke @critico @rn-009-02b
  Escenario: AMEND-009-GH-01 — Ubicación con garantía habilitada se calcula normalmente
    # RN-009-02b: si todas las garantías de la ubicación están en EnabledGuarantees, se calcula.
    Dado que el folio "DAN-2026-00100" tiene CoverageOptions con enabledGuarantees=["building_fire","cat_tev"]
    Y la ubicación 1 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:1000000 }, { key:"cat_tev", insuredAmount:500000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 200
    Y la respuesta tiene "data.quoteStatus" igual a "calculated"
    Y "data.premiumsByLocation[0].validationStatus" es "calculable"
    Y "data.premiumsByLocation[0].netPremium" es mayor que 0
    Y "data.premiumsByLocation[0].coveragePremiums" contiene 2 elementos
    Y "data.netPremium" es mayor que 0
    Y "data.version" es 4

  # ─────────────────────────────────────────────────────────────────
  # AMEND-009-GH-02: Garantía deshabilitada → ubicación incomplete
  # ─────────────────────────────────────────────────────────────────
  @critico @rn-009-02b
  Escenario: AMEND-009-GH-02 — Ubicación con garantía no habilitada se trata como incomplete
    # RN-009-02b: cat_tev fue deshabilitado en CoverageOptions. La ubicación 1 lo tiene → incomplete.
    # RN-009-03: no bloquea el cálculo si hay otra ubicación calculable.
    Dado que el folio "DAN-2026-00100" tiene CoverageOptions con enabledGuarantees=["building_fire"]
    Y la ubicación 1 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:1000000 }, { key:"cat_tev", insuredAmount:500000 }]
    Y la ubicación 2 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:2000000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation[0].validationStatus" es "incomplete"
    Y "data.premiumsByLocation[0].netPremium" es 0
    Y "data.premiumsByLocation[0].coveragePremiums" es []
    Y "data.premiumsByLocation[1].validationStatus" es "calculable"
    Y "data.premiumsByLocation[1].netPremium" es mayor que 0
    Y "data.netPremium" es igual a la prima neta de la ubicación 2 únicamente

  # ─────────────────────────────────────────────────────────────────
  # AMEND-009-GH-03: Todas las ubicaciones con garantías deshabilitadas → HTTP 422
  # ─────────────────────────────────────────────────────────────────
  @error-path @rn-009-02b
  Escenario: AMEND-009-GH-03 — Todas las ubicaciones tienen garantías deshabilitadas retorna HTTP 422
    # RN-009-02b + RN-009-04: si la regla deja 0 ubicaciones calculables → InvalidQuoteStateException.
    Dado que el folio "DAN-2026-00100" tiene CoverageOptions con enabledGuarantees=["building_fire"]
    Y la ubicación 1 tiene validationStatus "calculable"
      y garantías [{ key:"cat_tev", insuredAmount:1000000 }, { key:"fhm", insuredAmount:300000 }]
    Y la ubicación 2 tiene validationStatus "calculable"
      y garantías [{ key:"cat_tev", insuredAmount:800000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 422
    Y "error.type" es "invalidQuoteState"
    Y "error.message" contiene "No hay ubicaciones calculables para ejecutar el cálculo"
    Y el folio NO fue modificado (version sigue en 3)

  # ─────────────────────────────────────────────────────────────────
  # AMEND-009-GH-04: EnabledGuarantees vacío → sin filtro, comportamiento original
  # ─────────────────────────────────────────────────────────────────
  @edge-case @rn-009-02b @sin-regresion
  Escenario: AMEND-009-GH-04 — EnabledGuarantees vacío deshabilita el filtro (regla especial)
    # RN-009-02b especifica: si EnabledGuarantees es null o [] → filtro NO aplica.
    # Debe comportarse igual que antes del amendment (solo evalúa validationStatus de BD).
    Dado que el folio "DAN-2026-00100" tiene CoverageOptions con enabledGuarantees=[]
    Y la ubicación 1 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:1000000 }, { key:"cat_tev", insuredAmount:500000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation[0].validationStatus" es "calculable"
    Y "data.premiumsByLocation[0].netPremium" es mayor que 0
    Y "data.netPremium" es mayor que 0

  @edge-case @rn-009-02b @sin-regresion
  Escenario: AMEND-009-GH-04b — CoverageOptions null deshabilita el filtro (regla especial)
    # Si CoverageOptions no fue configurado (null), EnabledGuarantees es null → sin filtro.
    Dado que el folio "DAN-2026-00100" NO tiene CoverageOptions configurado (null)
    Y la ubicación 1 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:1000000 }, { key:"cat_tev", insuredAmount:500000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation[0].validationStatus" es "calculable"
    Y "data.premiumsByLocation[0].netPremium" es mayor que 0

  # ─────────────────────────────────────────────────────────────────
  # AMEND-009-GH-05: Mix — 2 calculables, 1 con garantía deshabilitada
  # ─────────────────────────────────────────────────────────────────
  @smoke @rn-009-02b
  Escenario: AMEND-009-GH-05 — Resultado mixto: 2 calculables, 1 degradada por RN-009-02b
    # RN-009-02b: ubicación 3 tiene "electronic_equipment" fuera de enabledGuarantees.
    # Resultado: locations 1 y 2 calculan, location 3 queda incomplete.
    Dado que el folio "DAN-2026-00100" tiene CoverageOptions con enabledGuarantees=["building_fire","cat_tev"]
    Y la ubicación 1 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:1000000 }]
    Y la ubicación 2 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"cat_tev", insuredAmount:500000 }]
    Y la ubicación 3 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:800000 }, { key:"electronic_equipment", insuredAmount:200000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation" contiene 3 elementos
    Y "data.premiumsByLocation[0].validationStatus" es "calculable"
    Y "data.premiumsByLocation[1].validationStatus" es "calculable"
    Y "data.premiumsByLocation[2].validationStatus" es "incomplete"
    Y "data.premiumsByLocation[2].netPremium" es 0
    Y "data.netPremium" es igual a la suma de primas de ubicaciones 1 y 2
    Y "data.quoteStatus" es "calculated"
    Y "data.version" es 4

  # ─────────────────────────────────────────────────────────────────
  # AMEND-009-GH-06: Ubicación incomplete por BD no es afectada por RN-009-02b
  # ─────────────────────────────────────────────────────────────────
  @edge-case @rn-009-02b
  Escenario: AMEND-009-GH-06 — Ubicación ya incompleta en BD permanece incomplete independientemente
    # La ubicación con validationStatus="incomplete" en BD ya no se calcula (RN-009-02).
    # RN-009-02b agrega un motivo adicional — no debe cambiar el flujo de las ya incompletas.
    Dado que el folio "DAN-2026-00100" tiene CoverageOptions con enabledGuarantees=["building_fire","cat_tev"]
    Y la ubicación 1 tiene validationStatus "incomplete" (sin CP válido)
      y garantías [{ key:"building_fire", insuredAmount:1000000 }]
    Y la ubicación 2 tiene CP "64000", giro fireKey "A-01", validationStatus "calculable"
      y garantías [{ key:"building_fire", insuredAmount:2000000 }]
    Cuando envío POST /v1/quotes/DAN-2026-00100/calculate con body { "version": 3 }
    Entonces la respuesta tiene status 200
    Y "data.premiumsByLocation[0].validationStatus" es "incomplete"
    Y "data.premiumsByLocation[1].validationStatus" es "calculable"
    Y "data.netPremium" es mayor que 0
```

---

## Datos de prueba por escenario

| ID Escenario | enabledGuarantees | Garantías ubicación | validationStatus BD | Resultado esperado |
|---|---|---|---|---|
| AMEND-009-GH-01 | `["building_fire","cat_tev"]` | `building_fire`, `cat_tev` | calculable | calculable, prima > 0 |
| AMEND-009-GH-02 | `["building_fire"]` | `building_fire`, `cat_tev` | calculable | incomplete (degradada por RN-009-02b) |
| AMEND-009-GH-03 | `["building_fire"]` | `cat_tev`, `fhm` (ambas ubics) | calculable | HTTP 422 |
| AMEND-009-GH-04 | `[]` | `building_fire`, `cat_tev` | calculable | calculable (sin filtro) |
| AMEND-009-GH-04b | `null` | `building_fire`, `cat_tev` | calculable | calculable (sin filtro) |
| AMEND-009-GH-05 | `["building_fire","cat_tev"]` | ubic3: `building_fire + electronic_equipment` | calculable | ubic3 → incomplete |
| AMEND-009-GH-06 | `["building_fire","cat_tev"]` | ubic1: garantías ok pero status BD=incomplete | incomplete | sigue incomplete (RN-009-02) |

> **NUNCA usar datos de producción.** Todos los folios, montos y parámetros son sintéticos.
