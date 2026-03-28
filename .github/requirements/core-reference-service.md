# REQ-01: Servicio de Referencia Core (Mock)

## Oleada de despliegue: 1 — Fundación
## Dependencias: Ninguna
## Prioridad: Crítica (bloqueante para todo el sistema)

---

## Descripción

Implementar el servicio `cotizador-core-mock` que simula `plataforma-core-ohs`. Este servicio expone endpoints REST con datos de referencia: catálogos, tarifas, agentes, códigos postales y generación secuencial de folios. Es el proveedor de datos maestros para todo el sistema.

---

## Historias de Usuario

**HU-01.1** — Como backend del cotizador, quiero consultar el catálogo de suscriptores para asignar un underwriter a la cotización.

**HU-01.2** — Como backend del cotizador, quiero consultar agentes por clave para asociar un agente a la cotización.

**HU-01.3** — Como backend del cotizador, quiero consultar giros comerciales (business lines) con su `fireKey` para mapear a tarifas técnicas.

**HU-01.4** — Como backend del cotizador, quiero consultar y validar códigos postales para resolver zona catastrófica y nivel técnico de cada ubicación.

**HU-01.5** — Como backend del cotizador, quiero generar folios secuenciales con formato `DAN-YYYY-NNNNN` para identificar cotizaciones de forma única e idempotente.

**HU-01.6** — Como backend del cotizador, quiero consultar catálogos de clasificación de riesgo y garantías para configurar las coberturas disponibles.

**HU-01.7** — Como backend del cotizador, quiero consultar tarifas de incendio, tarifas CAT (TEV/FHM), factores de equipo electrónico y parámetros globales de cálculo para ejecutar el motor de primas.

---

## Endpoints requeridos

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/v1/subscribers` | Catálogo de suscriptores |
| GET | `/v1/agents` | Lista de agentes |
| GET | `/v1/business-lines` | Giros comerciales con `fireKey` |
| GET | `/v1/zip-codes/{zipCode}` | Datos de un código postal (zona, nivel, estado, municipio) |
| POST | `/v1/zip-codes/validate` | Validar existencia de un código postal |
| GET | `/v1/folios` | Generar siguiente folio secuencial |
| GET | `/v1/catalogs/risk-classification` | Catálogo de clasificación de riesgo |
| GET | `/v1/catalogs/guarantees` | Catálogo de garantías disponibles |
| GET | `/v1/tariffs/fire` | Tarifas de incendio por `claveIncendio` |
| GET | `/v1/tariffs/cat` | Factores CAT (TEV y FHM) por zona |
| GET | `/v1/tariffs/fhm` | Cuotas FHM por grupo, zona y condición |
| GET | `/v1/tariffs/electronic-equipment` | Factores de equipo electrónico |
| GET | `/v1/tariffs/calculation-parameters` | Parámetros globales de cálculo |

---

## Datasets mínimos requeridos

- `fire_tariffs` — al menos 5 giros con tasas base
- `cat_tariffs` — al menos 3 zonas (A, B, C) con factores TEV/FHM
- `fhm_tariff` — al menos 3 registros por grupo/zona
- `equipment_factors` — al menos 3 clases con factores
- `zipcode_zone_catalog` — al menos 10 códigos postales con zona y nivel
- `calculation_parameters` — gastos de expedición, comisión de agente, factores de conversión
- Catálogo de suscriptores (`subscribers`) — al menos 3 registros
- Catálogo de agentes (`agents`) — al menos 3 registros
- Catálogo de giros (`business_lines`) — al menos 5 registros con `fireKey`
- Catálogo de garantías (`guarantees`) — las 14 coberturas del dominio
- Catálogo de clasificación de riesgo (`risk_classification`) — al menos 3 niveles

---

## Criterios de aceptación

- Todos los endpoints responden con datos válidos y consistentes entre sí
- Las tarifas son coherentes con los giros y zonas del catálogo de CP
- La generación de folio es secuencial y no repite valores
- Los contratos de respuesta están documentados (JSON schema o ejemplos)
- El servicio arranca de forma independiente sin dependencias externas

---

## Testabilidad

- **Unit tests**: Validar fixtures y coherencia de datos entre catálogos
- **Integration tests**: Verificar que cada endpoint responde HTTP 200 con estructura esperada
- **Desplegable de forma independiente**: Sí — es el primer servicio en levantarse
