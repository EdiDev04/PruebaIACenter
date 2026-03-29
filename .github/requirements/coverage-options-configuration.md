# REQ-07: Configuración de Opciones de Cobertura

## Oleada de despliegue: 3 — Gestión de Ubicaciones
## Dependencias: REQ-03 (folio existente), REQ-01 (catálogo de garantías)
## Prioridad: Alta (necesaria para el cálculo)

---

## Descripción

Implementar la consulta y guardado de las opciones de cobertura a nivel de cotización. Estas opciones definen la configuración global de coberturas/garantías que aplican al folio, complementando las garantías seleccionadas por ubicación. El catálogo de coberturas disponibles se obtiene del servicio core-mock.

---

## Historias de Usuario

**HU-07.1** — Como usuario del cotizador, quiero configurar las opciones de cobertura del folio para definir qué garantías están disponibles y sus condiciones.

**HU-07.2** — Como usuario del cotizador, quiero consultar las opciones de cobertura ya configuradas para revisarlas o modificarlas.

**HU-07.3** — Como usuario del cotizador, quiero ver el catálogo de garantías disponibles (14 tipos de cobertura) para seleccionar cuáles aplican a mi cotización.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/v1/quotes/{folio}/coverage-options` | Consultar opciones de cobertura |
| PUT | `/v1/quotes/{folio}/coverage-options` | Guardar opciones de cobertura |

### Catálogos consumidos del core-mock

| Endpoint core | Uso |
|---------------|-----|
| `GET /v1/catalogs/guarantees` | Catálogo de las 14 coberturas disponibles |

---

## Coberturas del dominio — Guarantee Keys

| Clave (key) | Descripción |
|-------|-------------|
| `building_fire` | Cobertura base sobre la construcción |
| `contents_fire` | Cobertura sobre bienes muebles e inventarios |
| `coverage_extension` | Riesgos adicionales sobre incendio |
| `cat_tev` | Catástrofe — Terremoto, Erupción Volcánica |
| `cat_fhm` | Catástrofe — Fenómenos Hidrometeorológicos |
| `debris_removal` | Costos de limpieza post-siniestro |
| `extraordinary_expenses` | Erogaciones adicionales por siniestro |
| `rent_loss` | Lucro cesante por inhabilitación |
| `business_interruption` | Pérdida de utilidades por interrupción |
| `electronic_equipment` | All-risk para equipos electrónicos |
| `theft` | Robo con violencia y/o asalto |
| `cash_and_securities` | Efectivo, cheques, títulos |
| `glass` | Rotura accidental de cristales |
| `illuminated_signs` | Daño a letreros y señalética |

---

## Flujo Frontend

1. Ruta: integrado en `/quotes/{folio}/technical-info` o `/quotes/{folio}/terms-and-conditions`
2. Cargar opciones existentes (GET coverage-options)
3. Cargar catálogo de garantías del core-mock
4. Formulario de selección/configuración de opciones
5. Al guardar: PUT coverage-options con versión actual

---

## Reglas de negocio

- Las opciones de cobertura son una sección independiente del documento
- El PUT requiere `version` actual para versionado optimista
- Las opciones de cobertura a nivel de folio complementan las garantías por ubicación
- Al menos las opciones deben reflejar qué coberturas están habilitadas globalmente

---

## Criterios de aceptación

```gherkin
Dado que tengo un folio sin opciones de cobertura configuradas
Cuando consulto las opciones de cobertura
Entonces el sistema retorna un objeto vacío o con valores por defecto

Dado que configuro las opciones de cobertura seleccionando garantías
Cuando guardo las opciones
Entonces el sistema persiste solo la sección de opciones de cobertura
Y incrementa la versión de la cotización

Dado que las opciones de cobertura están configuradas
Cuando consulto las opciones
Entonces el sistema retorna las opciones previamente guardadas
```

---

## Testabilidad

- **Unit tests**: Validar estructura de opciones, lógica de defaults
- **Integration tests**: PUT → GET → verificar aislamiento de sección
- **Desplegable**: Sí — funcionalidad independiente, testeable por separado
