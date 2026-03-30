# Escenarios Gherkin — SPEC-010: Visualización de Resultados y Alertas

> **Feature:** results-display  
> **Spec:** SPEC-010  
> **Status:** IMPLEMENTED — QUALITY_GATE: PASSED  
> **Generado:** 2026-03-30  
> **Cubre:** HU-010-01 a HU-010-05

---

```gherkin
Feature: Visualización de resultados del cálculo de primas

  Background:
    Given el backend retorna correctamente en GET /v1/quotes/{folio}/state
    And el folio de referencia es "DAN-2026-00010"

  # ─────────────────────────────────────────────────────────────────
  # HU-010-01: Estados de la página según quoteStatus
  # ─────────────────────────────────────────────────────────────────

  Scenario: Estado calculado — tres tarjetas de resumen visibles
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And calculationResult contiene:
      | netPremium              | 125430.50  |
      | commercialPremiumBefore | 145499.38  |
      | commercialPremium       | 173244.26  |
    When el usuario navega a "/quotes/DAN-2026-00010/terms-and-conditions"
    Then se muestra la tarjeta "Prima Neta Total" con valor "$125.430,50"
    And se muestra la tarjeta "Prima Comercial (sin IVA)" con valor "$145.499,38"
    And se muestra la tarjeta "Prima Comercial Total" con valor "$173.244,26"
    And NO se muestra ningún mensaje de invitación a calcular

  Scenario: Estado no calculado con ubicaciones listas — invitación a calcular
    Given el folio "DAN-2026-00010" tiene quoteStatus "in_progress"
    And calculationResult es null
    And readyForCalculation es true
    When el usuario navega a "/quotes/DAN-2026-00010/terms-and-conditions"
    Then se muestra el mensaje "Ejecute el cálculo para ver resultados"
    And se muestra el botón "Calcular cotización"
    And NO se muestran las tarjetas de prima
    And NO se muestra el panel de desglose por ubicación

  Scenario: Estado no calculado sin ubicaciones listas — bloqueo informativo
    Given el folio "DAN-2026-00010" tiene quoteStatus "in_progress"
    And calculationResult es null
    And readyForCalculation es false
    When el usuario navega a "/quotes/DAN-2026-00010/terms-and-conditions"
    Then se muestra el mensaje "Ejecute el cálculo para ver resultados"
    And se muestra el mensaje "No hay ubicaciones calculables"
    And NO se muestra el botón "Calcular cotización"

  Scenario: Formateo de moneda COP con separador de miles y decimales
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And calculationResult.netPremium es 1000000.00
    When el usuario navega a "/quotes/DAN-2026-00010/terms-and-conditions"
    Then la tarjeta "Prima Neta Total" muestra "$1.000.000,00"
    And el formateo usa el locale "es-CO" con currency "COP"

  # ─────────────────────────────────────────────────────────────────
  # HU-010-02: Desglose de prima por ubicación
  # ─────────────────────────────────────────────────────────────────

  Scenario: Desglose por ubicación con dos ubicaciones calculables
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And premiumsByLocation contiene:
      | locationName       | netPremium | status      |
      | Bodega Central     | 75430.50   | calculable  |
      | Oficina Norte      | 50000.00   | calculable  |
    When el usuario ve la sección de desglose
    Then se listan 2 filas de ubicación
    And la fila "Bodega Central" muestra prima neta "$75.430,50" y badge "Calculable"
    And la fila "Oficina Norte" muestra prima neta "$50.000,00" y badge "Calculable"
    And la suma de primas netas por ubicación es "$125.430,50" (igual a netPremium total)

  Scenario: Desglose muestra solo ubicaciones calculables (no incompletas)
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And premiumsByLocation contiene 2 ubicaciones calculables
    And alertLocations contiene 1 ubicación incompleta "Almacén Sur"
    When el usuario ve la sección de desglose principal
    Then la tabla muestra 2 filas
    And "Almacén Sur" NO aparece en la tabla de desglose
    And "Almacén Sur" aparece en el panel de alertas

  # ─────────────────────────────────────────────────────────────────
  # HU-010-03: Acordeón expandible por ubicación (desglose por cobertura)
  # ─────────────────────────────────────────────────────────────────

  Scenario: Expandir acordeón de ubicación muestra coberturas
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And la ubicación "Bodega Central" tiene 4 coberturas:
      | guaranteeKey          | insuredAmount | rate   | premium  |
      | incendio_edificios    | 500000.00     | 0.0020 | 1000.00  |
      | incendio_contenidos   | 300000.00     | 0.0025 | 750.00   |
      | cat_tev               | 200000.00     | 0.0015 | 300.00   |
      | rc_predial            | 100000.00     | 0.0030 | 300.00   |
    When el usuario hace clic en la fila "Bodega Central"
    Then se expande la sub-tabla con 4 filas de cobertura
    And la columna "Garantía" muestra los nombres legibles en español
    And la columna "Suma Asegurada" muestra "$500.000,00" para "incendio_edificios"
    And la columna "Tasa" muestra "0.20%" para rate 0.0020
    And la columna "Prima" muestra "$1.000,00"

  Scenario: Colapsar acordeón oculta la sub-tabla de coberturas
    Given la ubicación "Bodega Central" está expandida
    When el usuario hace clic nuevamente en la fila "Bodega Central"
    Then la sub-tabla de coberturas se oculta
    And solo se muestra el resumen de la ubicación

  Scenario: Múltiples acordeones pueden expandirse de forma independiente
    Given hay 2 ubicaciones calculables "Bodega Central" y "Oficina Norte"
    When el usuario expande "Bodega Central"
    And el usuario expande "Oficina Norte"
    Then ambas sub-tablas de coberturas son visibles simultáneamente

  # ─────────────────────────────────────────────────────────────────
  # HU-010-04: Panel de alertas para ubicaciones incompletas
  # ─────────────────────────────────────────────────────────────────

  Scenario: Panel ámbar de alertas con campos faltantes en español
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And alertLocations contiene la ubicación "Almacén Sur" con missingFields:
      | zipCode                   |
      | businessLine.fireKey      |
    When el usuario ve la pantalla de resultados
    Then se muestra un panel de alerta de color ámbar (warning)
    And el panel indica "Almacén Sur" como ubicación incompleta
    And lista los campos faltantes:
      | Código Postal            |
      | Clave de incendio        |
    And los campos faltantes están en español (no en camelCase)

  Scenario: Botón "Editar ubicación" navega a la pantalla de ubicaciones
    Given el panel de alertas muestra la ubicación incompleta "Almacén Sur"
    When el usuario hace clic en "Editar ubicación"
    Then el usuario es redirigido a "/quotes/DAN-2026-00010/locations"

  Scenario: Cálculo parcial — ubicaciones calculables y alertas coexisten
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And hay 2 ubicaciones calculables y 1 incompleta
    When el usuario ve la pantalla de resultados
    Then las tarjetas de prima se muestran con los totales de las 2 ubicaciones calculables
    And la sección de desglose muestra 2 filas
    And el panel de alertas muestra 1 alerta para la ubicación incompleta
    And NO se bloquea la visualización de resultados por la ubicación incompleta

  Scenario: Sin ubicaciones incompletas — panel de alertas no aparece
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    And alertLocations está vacío
    When el usuario ve la pantalla de resultados
    Then el panel de alertas ámbar NO se renderiza en el DOM

  # ─────────────────────────────────────────────────────────────────
  # HU-010-05: Botón "Recalcular" para re-ejecución del cálculo
  # ─────────────────────────────────────────────────────────────────

  Scenario: Botón "Recalcular" visible en estado calculado
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated"
    When el usuario ve la pantalla de resultados
    Then el botón "Recalcular" es visible
    And el botón "Calcular cotización" NO es visible simultáneamente

  Scenario: Clic en "Recalcular" ejecuta POST /calculate con la versión actual
    Given el folio "DAN-2026-00010" tiene quoteStatus "calculated" y version 5
    When el usuario hace clic en "Recalcular"
    Then se ejecuta POST /v1/quotes/DAN-2026-00010/calculate con body { "version": 5 }
    And se muestra un indicador de carga mientras el cálculo está en progreso
    And al completar, la query ['quote-state', 'DAN-2026-00010'] es invalidada
    And la pantalla se actualiza con los nuevos resultados

  Scenario: Recálculo exitoso actualiza las tarjetas de prima
    Given el folio "DAN-2026-00010" calculado con netPremium 125430.50
    And el usuario modificó una ubicación (prima esperada cambia)
    When el usuario hace clic en "Recalcular"
    And el backend retorna los nuevos resultados con netPremium 98750.00
    Then la tarjeta "Prima Neta Total" se actualiza a "$98.750,00"
    And las filas del desglose reflejan los nuevos valores

  Scenario: Botón "Recalcular" deshabilitado durante la ejecución del cálculo
    Given el usuario hizo clic en "Recalcular"
    And el POST /calculate está en progreso
    When el usuario intenta hacer clic en "Recalcular" nuevamente
    Then el botón está deshabilitado (disabled=true) o muestra estado de carga
    And se previene el envío duplicado
```
