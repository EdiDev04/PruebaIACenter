# Matriz de Riesgos — SPEC-010: Visualización de Resultados y Alertas

> **Feature:** results-display  
> **Spec:** SPEC-010  
> **Clasificación:** ASD (Alto / Medio / Bajo)  
> **Generado:** 2026-03-30

---

## Criterio de clasificación ASD

| Nivel | Definición | Obligatoriedad de mitigación |
|-------|-----------|------------------------------|
| **Alto** | Probabilidad × Impacto ≥ 6, o impacto financiero/dato incorrecto directo | Obligatorio — bloquea release |
| **Medio** | Probabilidad × Impacto 3–5 | Recomendado — registrar deuda técnica |
| **Bajo** | Probabilidad × Impacto 1–2 | Opcional — monitoreo |

Escala: Probabilidad [1=Baja, 2=Media, 3=Alta] · Impacto [1=Bajo, 2=Medio, 3=Alto]

---

## Matriz de Riesgos

| ID | Área | Riesgo | P | I | Score | Nivel | Mitigación |
|----|------|--------|---|---|-------|-------|------------|
| R-01 | Formateo financiero | `formatCurrency` usa locale incorrecto o redondeo erróneo → monto mostrado difiere del calculado | 2 | 3 | 6 | **Alto** | Tests unitarios de `formatCurrency.ts` con casos límite: valores con centavos, millones, cero. Verificar `$1.000.000,00` (punto como sep. miles, coma como decimal) |
| R-02 | Presentación de primas | `netPremium` total no coincide con suma de `premiumsByLocation` → usuario percibe inconsistencia | 2 | 3 | 6 | **Alto** | Test E2E que suma las primas de la tabla y compara contra la tarjeta de resumen — ambas deben coincidir |
| R-03 | Lógica de estado de página | Condición `quoteStatus == 'calculated'` evaluada con typo o comparación estricta falla → estado de pantalla incorrecto (ej: muestra tarjetas cuando quoteStatus es `"Calculated"`) | 1 | 3 | 3 | **Medio** | Test unitario de la lógica de selección de estado con valores exactos del contrato API: `"calculated"`, `"in_progress"`, `"pending"` |
| R-04 | Panel de alertas | `missingFields` con keys camelCase mostrados sin traducción → usuario no entiende qué campo completar | 2 | 2 | 4 | **Medio** | Test de renderizado: `"zipCode"` debe mostrarse como `"Código Postal"` · `"businessLine.fireKey"` como `"Clave de incendio"` |
| R-05 | Acordeón | Componente `CoverageAccordion` no actualiza estado de colapso correctamente en re-render → sub-tabla queda visible cuando debería cerrarse | 2 | 2 | 4 | **Medio** | Test de interacción: togglear acordeón dos veces y verificar visibilidad final |
| R-06 | Invalidación de caché | Al completar recálculo, `invalidateQueries(['quote-state', folio])` no se ejecuta → pantalla muestra datos obsoletos | 1 | 3 | 3 | **Medio** | Test del hook `useCalculateQuote`: verificar que `onSuccess` llama `queryClient.invalidateQueries` con la key correcta |
| R-07 | Botón Recalcular/Calcular | Ambos botones visibles simultáneamente → confusión de flujo para el usuario | 1 | 2 | 2 | **Bajo** | Test de renderizado condicional: cuando `quoteStatus == 'calculated'` solo existe el botón "Recalcular" en el DOM |
| R-08 | Envío duplicado | Clic múltiple en "Recalcular" envía varias peticiones POST → estado de folio inconsistente | 1 | 3 | 3 | **Medio** | Test de interacción: botón deshabilitado mientras `isPending == true`. Verificar `disabled` attribute |
| R-09 | Navegación de alertas | Clic en "Editar ubicación" redirige a ruta incorrecta (ej: sin el folio dinámico) → usuario llega a pantalla equivocada | 1 | 2 | 2 | **Bajo** | Test de integración: verificar href generado es `/quotes/DAN-2026-00010/locations` con el folio del contexto |
| R-10 | Compatibilidad de Intl | `Intl.NumberFormat` no disponible en entorno de prueba (jsdom) → tests fallan con entorno correcto | 2 | 2 | 4 | **Medio** | Configurar polyfill de `Intl` en `vitest.config.ts` o usar `@formatjs/intl-numberformat`. Verificar en `setup.ts` |
| R-11 | Sin datos de cobertura | `LocationPremiumDto.coverages` llega vacío → acordeón expandido muestra tabla vacía sin mensaje | 1 | 1 | 1 | **Bajo** | Test de contorno: expansión de ubicación con `coverages: []` debe mostrar mensaje "Sin coberturas disponibles" o similar |
| R-12 | Responsive / CSS | Panel de alertas o tarjetas de resumen se rompen en viewport móvil (< 768px) → checkout flow inutilizable | 1 | 2 | 2 | **Bajo** | Test visual Playwright en viewport 375×667. Verificar que las tarjetas apilen verticalmente sin overflow horizontal |

---

## Resumen por nivel

| Nivel | Count | IDs |
|-------|-------|-----|
| **Alto** | 2 | R-01, R-02 |
| **Medio** | 5 | R-03, R-04, R-05, R-06, R-08, R-10 |
| **Bajo** | 4 | R-07, R-09, R-11, R-12 |

---

## Riesgos bloqueantes para release

Los siguientes riesgos deben estar mitigados antes de promover a producción:

1. **R-01** — Formateo de moneda incorrecto transmitiría cifras financieras erróneas al usuario. Impacto de negocio directo.
2. **R-02** — Inconsistencia entre total y suma de desglose destruye la confianza del usuario en la herramienta de cotización.

---

## Cobertura actual (34 tests / 6 archivos — SPEC-010)

| Riesgo | Cubierto por tests unitarios existentes | Gap a cubrir |
|--------|----------------------------------------|--------------|
| R-01 | ✅ Tests de `formatCurrency.ts` | Verificar casos de valores negativos y cero |
| R-02 | Parcial — tests de componente aislado | Gap: test E2E de coherencia total vs. suma |
| R-03 | ✅ Tests del hook/lógica de estado | — |
| R-04 | ✅ Tests de `IncompleteAlerts` | — |
| R-05 | ✅ Tests de `CoverageAccordion` | — |
| R-06 | Pendiente verificar en test del hook | Revisar mock de `queryClient` |
| R-08 | ✅ Tests de estado `isPending` | — |
| R-10 | Depende de configuración de Vitest | Verificar `setup.ts` |
