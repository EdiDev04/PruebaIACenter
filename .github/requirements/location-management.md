# REQ-06: Gestión de Ubicaciones de Riesgo

## Oleada de despliegue: 3 — Gestión de Ubicaciones
## Dependencias: REQ-03 (folio existente), REQ-05 (layout), REQ-01 (códigos postales, giros)
## Prioridad: Crítica (entrada de datos para el cálculo)

---

## Descripción

Implementar el registro, consulta, edición y resumen de ubicaciones de riesgo dentro de un folio. Cada ubicación representa un inmueble a asegurar con sus datos físicos, giro comercial, coberturas seleccionadas (garantías) y validación de calculabilidad. El sistema debe soportar múltiples ubicaciones por folio y permitir edición granular de una ubicación sin afectar las demás.

---

## Historias de Usuario

**HU-06.1** — Como usuario del cotizador, quiero agregar una o varias ubicaciones de riesgo a mi folio para asegurar múltiples propiedades.

**HU-06.2** — Como usuario del cotizador, quiero capturar datos físicos de cada ubicación: dirección, código postal, tipo constructivo, nivel, año de construcción.

**HU-06.3** — Como usuario del cotizador, quiero seleccionar el giro comercial (`businessLine`) de cada ubicación desde el catálogo de giros (con su `fireKey`).

**HU-06.4** — Como usuario del cotizador, quiero que al ingresar un código postal (`zipCode`), el sistema resuelva automáticamente la `catastrophicZone`, `state`, `municipality` y `neighborhood`.

**HU-06.5** — Como usuario del cotizador, quiero seleccionar las garantías (`guarantees`) activas para cada ubicación.

**HU-06.6** — Como usuario del cotizador, quiero editar una ubicación específica sin afectar las demás ubicaciones del folio.

**HU-06.7** — Como usuario del cotizador, quiero ver un resumen de todas las ubicaciones con su `validationStatus` (`calculable` / `incomplete`).

**HU-06.8** — Como usuario del cotizador, quiero ver alertas sobre ubicaciones incompletas sin que estas bloqueen el guardado del folio ni la gestión de otras ubicaciones.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/v1/quotes/{folio}/locations` | Listar todas las ubicaciones del folio |
| PUT | `/v1/quotes/{folio}/locations` | Guardar/reemplazar el array completo de ubicaciones |
| PATCH | `/v1/quotes/{folio}/locations/{index}` | Editar una ubicación puntual por índice |
| GET | `/v1/quotes/{folio}/locations/summary` | Resumen de ubicaciones con estado de validación |

### Catálogos consumidos del core-mock

| Endpoint core | Uso |
|---------------|-----|
| `GET /v1/business-lines` | Catálogo de giros con `fireKey` |
| `GET /v1/zip-codes/{zipCode}` | Resolver zona, estado, municipio, colonia |
| `POST /v1/zip-codes/validate` | Validar existencia de CP |
| `GET /v1/catalogs/guarantees` | Catálogo de coberturas disponibles |

---

## Modelo de Ubicación — `Location`

```json
{
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
  "businessLine": {
    "description": "Bodega de almacenamiento",
    "fireKey": "B-03"
  },
  "guarantees": ["building_fire", "contents_fire", "cat_tev", "theft"],
  "catastrophicZone": "A",
  "blockingAlerts": [],
  "validationStatus": "calculable"
}
```

---

## Flujo Frontend

1. Ruta: `/quotes/{folio}/locations`
2. Vista de grilla/lista de ubicaciones existentes con estado de validación
3. Botón "Agregar ubicación" → formulario con campos del modelo
4. Al ingresar CP → llamada a core-mock para resolver zona/estado/municipio/colonia
5. Selector de giro → poblar desde catálogo de giros (core-mock)
6. Checkboxes de garantías → poblar desde catálogo de garantías (core-mock)
7. Validación local: marcar ubicación como "incompleta" si falta CP válido, giro o garantías
8. Acción "Editar" por ubicación → edición inline o modal → PATCH al guardar

---

## Reglas de negocio

- Una ubicación es **calculable** solo si tiene: código postal válido, `businessLine.fireKey` y al menos una garantía tarifable
- Una ubicación **incompleta** (`validationStatus: "incomplete"`) genera alerta pero NO bloquea el guardado del folio ni la gestión de otras ubicaciones
- El `validationStatus` se calcula en el backend al persistir (no confiar solo en el frontend)
- El CP resuelve automáticamente: `catastrophicZone`, `state`, `municipality`, `neighborhood` vía core-mock
- El PATCH de una ubicación solo modifica esa ubicación — las demás permanecen intactas
- Toda escritura (PUT/PATCH) incrementa `version` y actualiza `metadata.updatedAt`

---

## Criterios de aceptación

```gherkin
Dado que tengo un folio con 0 ubicaciones
Cuando agrego una ubicación con todos los datos requeridos
Entonces la ubicación se persiste con validationStatus "calculable"
Y la versión del folio se incrementa

Dado que agrego una ubicación sin código postal
Cuando guardo la ubicación
Entonces se persiste con validationStatus "incomplete"
Y se genera una alerta pero el guardado es exitoso

Dado que tengo un folio con 3 ubicaciones
Cuando edito la ubicación con índice 2 vía PATCH
Entonces solo la ubicación 2 se modifica
Y las ubicaciones 1 y 3 permanecen intactas

Dado que ingreso el código postal "06600"
Cuando el sistema consulta el core-mock
Entonces resuelve automáticamente catastrophicZone, state, municipality y neighborhood

Dado que consulto el resumen de ubicaciones
Cuando hay 2 calculables y 1 incompleta
Entonces el resumen muestra el estado de cada una con sus alertas
```

---

## Testabilidad

- **Unit tests**: Lógica de calculabilidad, validación de campos mínimos, resolución de CP
- **Integration tests**: CRUD completo de ubicaciones, verificar aislamiento del PATCH
- **E2E tests**: Agregar ubicación desde UI, editar, verificar resolución de CP
- **Desplegable**: Sí — junto con REQ-03 y REQ-04 forma un flujo de captura funcional
