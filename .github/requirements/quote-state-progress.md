# REQ-08: Estado y Progreso de la Cotización

## Oleada de despliegue: 4 — Cálculo y Resultados
## Dependencias: REQ-03 (folio), REQ-06 (ubicaciones)
## Prioridad: Alta

---

## Descripción

Implementar la consulta del estado global de la cotización y el indicador de progreso del folio. El estado refleja cuántas secciones están completas, cuántas ubicaciones son calculables vs incompletas, y si el folio está listo para ejecutar el cálculo. Las alertas de ubicaciones incompletas se presentan de forma informativa sin bloquear el flujo.

---

## Historias de Usuario

**HU-08.1** — Como usuario del cotizador, quiero ver el estado actual de mi cotización (borrador, en proceso, calculada) para saber en qué punto del flujo me encuentro.

**HU-08.2** — Como usuario del cotizador, quiero ver un indicador de progreso que muestre qué secciones del folio están completas y cuáles faltan.

**HU-08.3** — Como usuario del cotizador, quiero ver alertas de ubicaciones incompletas que indiquen qué datos faltan, sin que estas bloqueen la navegación o el guardado de otras secciones.

**HU-08.4** — Como usuario del cotizador, quiero saber cuántas ubicaciones son calculables antes de ejecutar el cálculo para tener expectativas claras.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/v1/quotes/{folio}/state` | Estado completo de la cotización con indicadores de progreso |

---

## Respuesta esperada del endpoint — `QuoteState`

```json
{
  "folioNumber": "DAN-2025-00142",
  "quoteStatus": "in_progress",
  "version": 5,
  "progress": {
    "generalInfo": true,
    "layoutConfiguration": true,
    "locations": true,
    "coverageOptions": false
  },
  "locations": {
    "total": 3,
    "calculable": 2,
    "incomplete": 1,
    "alerts": [
      {
        "index": 3,
        "locationName": "Local sin CP",
        "missingFields": ["zipCode", "businessLine.fireKey"]
      }
    ]
  },
  "readyForCalculation": true
}
```

---

## Flujo Frontend

1. Componente visible en todas las rutas del flujo `/quotes/{folio}/*`
2. Barra o panel lateral de progreso con checkmarks por sección
3. Badge o indicador con conteo de ubicaciones calculables/incompletas
4. Al hacer clic en una alerta de ubicación incompleta → navegar a edición de esa ubicación
5. El estado se consulta al navegar entre secciones (no en polling continuo)

---

## Reglas de negocio

- El estado `quoteStatus` transiciona: `draft` → `in_progress` (al guardar primera sección) → `calculated` (post-cálculo)
- El campo `readyForCalculation` es `true` si hay al menos 1 ubicación calculable
- Las alertas son informativas — nunca bloquean la navegación
- Una ubicación es incompleta si falta: CP válido, `businessLine.fireKey`, o garantías tarifables en `guarantees`
- El progreso por sección es un cálculo derivado (no persiste — se calcula en la consulta)

---

## Criterios de aceptación

```gherkin
Dado que tengo un folio con datos generales y 2 ubicaciones (1 calculable, 1 incompleta)
Cuando consulto el estado del folio
Entonces el sistema retorna progreso con generalInfo: true, locations: true
Y muestra 1 ubicación calculable y 1 incompleta con sus alertas
Y readyForCalculation es true

Dado que tengo un folio sin ninguna sección completa
Cuando consulto el estado
Entonces todos los indicadores de progreso son false
Y quoteStatus es "draft"

Dado que estoy en la pantalla de ubicaciones
Cuando veo una alerta de ubicación incompleta
Entonces puedo hacer clic para navegar a la edición de esa ubicación
Y la alerta no impide guardar ni navegar a otras secciones
```

---

## Testabilidad

- **Unit tests**: Lógica de cálculo de progreso, determinación de calculabilidad
- **Integration tests**: Crear folio con diferentes estados → verificar respuesta del endpoint
- **E2E tests**: Navegar por el flujo y verificar indicadores de progreso
- **Desplegable**: Sí — enriquece el flujo existente sin romper funcionalidad previa
