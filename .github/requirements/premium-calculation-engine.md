# REQ-09: Motor de Cálculo de Primas

## Oleada de despliegue: 4 — Cálculo y Resultados
## Dependencias: REQ-01 (tarifas y parámetros), REQ-02 (persistencia), REQ-06 (ubicaciones con garantías), REQ-07 (opciones de cobertura)
## Prioridad: Crítica (entrega de valor central del sistema)

---

## Descripción

Implementar el motor de cálculo que procesa la cotización completa para calcular prima neta y prima comercial. El motor lee el folio, determina calculabilidad por ubicación, aplica tarifas técnicas por cobertura a cada ubicación calculable, consolida la prima neta total y deriva la prima comercial aplicando parámetros globales. El resultado financiero se persiste en una sola operación lógica sin sobreescribir otras secciones.

---

## Historias de Usuario

**HU-09.1** — Como usuario del cotizador, quiero ejecutar el cálculo de mi cotización para obtener la prima neta y prima comercial del folio.

**HU-09.2** — Como usuario del cotizador, quiero ver el desglose de prima por ubicación para entender la contribución de cada propiedad.

**HU-09.3** — Como usuario del cotizador, quiero que las ubicaciones incompletas generen alertas pero no impidan calcular las ubicaciones válidas.

**HU-09.4** — Como sistema, quiero persistir el resultado financiero (`netPremium`, `commercialPremium`, `premiumsByLocation`) en una sola operación lógica sin sobreescribir otros datos del folio.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/v1/quotes/{folio}/calculate` | Ejecutar cálculo de primas sobre el folio |

### Datos consumidos del core-mock

| Endpoint core | Uso en cálculo |
|---------------|----------------|
| `GET /v1/tariffs/fire` | Tasa base de incendio por `fireKey` |
| `GET /v1/tariffs/cat` | Factores CAT (TEV/FHM) por zona |
| `GET /v1/tariffs/fhm` | Cuotas FHM por grupo, zona, condición |
| `GET /v1/tariffs/electronic-equipment` | Factor de equipo electrónico |
| `GET /v1/tariffs/calculation-parameters` | Parámetros globales (gastos, comisión) |
| `GET /v1/zip-codes/{zipCode}` | Zona catastrófica y nivel técnico |

---

## Flujo del Motor de Cálculo

1. **Leer cotización completa** por `folioNumber`
2. **Leer parámetros globales** (`calculation_parameters` desde core-mock)
3. **Para cada ubicación**, determinar si es calculable:
   - Tiene código postal válido → resuelve `catastrophicZone`
   - Tiene `businessLine.fireKey` → resuelve tarifa de incendio
   - Tiene al menos una garantía tarifable en `guarantees`
4. **Para cada ubicación calculable**, calcular prima por cobertura:
   - Cada garantía activa → buscar tarifa correspondiente → `suma_asegurada × tarifa_técnica`
   - Sumar todas las primas de coberturas → prima neta de la ubicación
5. **Consolidar prima neta total** = Σ primas netas por ubicación
6. **Derivar prima comercial total** = prima neta + gastos de expedición + comisión de agente (desde `calculation_parameters`)
7. **Persistir resultado**: guardar `netPremium`, `commercialPremium`, `premiumsByLocation` en una operación atómica
8. **Actualizar estado** a `calculated`, incrementar versión

---

## Componentes de cobertura y su tarifa

| Garantía (guarantee key) | Fuente de tarifa |
|----------|------------------|
| `building_fire` | `fire_tariffs` (por `fireKey`) |
| `contents_fire` | `fire_tariffs` (por `fireKey`) |
| `coverage_extension` | `fire_tariffs` (factor adicional) |
| `cat_tev` | `cat_tariffs` (por zona, tipo TEV) |
| `cat_fhm` | `fhm_tariff` (por grupo, zona, condición) |
| `debris_removal` | Porcentaje sobre prima de incendio (desde `calculation_parameters`) |
| `extraordinary_expenses` | Porcentaje sobre prima de incendio |
| `rent_loss` | Tasa sobre suma asegurada |
| `business_interruption` | Tasa sobre suma asegurada BI |
| `electronic_equipment` | `equipment_factors` (por clase y zona) |
| `theft` | Tasa fija o por suma asegurada |
| `cash_and_securities` | Tasa fija o por suma asegurada |
| `glass` | Tasa fija |
| `illuminated_signs` | Tasa fija |

---

## Estructura del resultado financiero — `FinancialResult`

```json
{
  "netPremium": 125000.50,
  "commercialPremium": 156250.63,
  "premiumsByLocation": [
    {
      "index": 1,
      "locationName": "Bodega Central CDMX",
      "netPremium": 85000.30,
      "coverageBreakdown": {
        "building_fire": 45000.00,
        "contents_fire": 25000.00,
        "cat_tev": 10000.30,
        "theft": 5000.00
      },
      "validationStatus": "calculable"
    },
    {
      "index": 2,
      "locationName": "Sucursal Monterrey",
      "netPremium": 40000.20,
      "coverageBreakdown": { ... },
      "validationStatus": "calculable"
    },
    {
      "index": 3,
      "locationName": "Local sin datos",
      "netPremium": 0,
      "coverageBreakdown": {},
      "validationStatus": "incomplete",
      "alerts": ["Missing valid zip code"]
    }
  ]
}
```

---

## Reglas de negocio

- La cotización se identifica por `folioNumber`
- Una ubicación NO se calcula si no tiene: CP válido, `businessLine.fireKey`, o garantías tarifables en `guarantees`
- Las ubicaciones incompletas generan alertas pero no impiden calcular las demás
- `netPremium` = Σ (prima neta por ubicación calculable)
- `commercialPremium` = netPremium + gastos de expedición + comisión de agente (parámetros globales)
- La prima comercial se calcula a nivel de folio, NO por ubicación
- El resultado financiero se persiste sin sobreescribir `insuredData`, `locations`, ni `coverageOptions`
- La operación de persistencia es atómica (una sola operación de escritura)
- Al persistir se actualiza `quoteStatus` a `calculated`, se incrementa `version` y se actualiza `metadata.updatedAt`
- Toda fórmula simplificada debe quedar documentada explícitamente

---

## Criterios de aceptación

```gherkin
Dado que tengo un folio con 2 ubicaciones calculables y 1 incompleta
Cuando ejecuto el cálculo
Entonces el sistema calcula prima neta para las 2 ubicaciones calculables
Y genera alerta para la ubicación incompleta sin bloquear
Y consolida prima neta total = suma de primas por ubicación
Y deriva prima comercial = prima neta + gastos + comisión
Y persiste el resultado financiero sin sobreescribir otras secciones
Y actualiza quoteStatus a "calculated" e incrementa versión

Dado que tengo un folio sin ubicaciones calculables
Cuando ejecuto el cálculo
Entonces el sistema retorna prima neta = 0, prima comercial = 0
Y reporta todas las ubicaciones como incompletas con sus alertas

Dado que la ubicación tiene garantía "building_fire" con fireKey "B-03"
Cuando el motor calcula la prima de esa cobertura
Entonces busca la tarifa en fire_tariffs para "B-03"
Y aplica: suma_asegurada × tarifa_técnica

Dado que ejecuto el cálculo exitosamente
Cuando consulto la cotización
Entonces los campos netPremium, commercialPremium y premiumsByLocation están persistidos
Y los demás campos (insuredData, locations, etc.) no fueron modificados
```

---

## Testabilidad

- **Unit tests**: Lógica de cada cobertura, consolidación, derivación de prima comercial, fórmulas documentadas
- **Integration tests**: Flujo completo: folio con ubicaciones → POST calculate → verificar persistencia atómica
- **E2E tests**: Desde UI: completar ubicaciones → ejecutar cálculo → ver resultados
- **Desplegable**: Sí — funcionalidad de alto valor; junto con REQ-10 completa el flujo principal
