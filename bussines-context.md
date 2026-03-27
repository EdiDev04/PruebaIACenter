# Contexto del Reto Técnico — Cotizador de Seguros de Daños

## 1. Descripción del dominio

El reto consiste en construir una solución funcional para un **cotizador de seguros de daños a la propiedad** (incendio y riesgos aliados). Este es el tipo de seguro que protege edificios, contenidos y mercancías de empresas con una o varias propiedades (ubicaciones) ante riesgos como incendio, fenómenos naturales, robo, entre otros.

El sistema opera sobre el concepto de **folio**: un expediente de cotización que puede contener múltiples ubicaciones de riesgo, cada una con sus propias características físicas, giro comercial y coberturas. El folio puede existir en estado incompleto (con ubicaciones pendientes) sin bloquear el flujo completo.

---

## 2. Arquitectura de la solución

La solución se divide en **tres bloques**:

| Bloque | Rol |
|---|---|
| `cotizador-danos-web` | Frontend — interfaz de usuario del cotizador |
| `plataforma-danos-backend` | Backend principal — lógica de negocio y cálculo |
| `plataforma-core-ohs` | Servicio de referencia — catálogos, tarifas, agentes, códigos postales y folios (Mock o Stubs) |

### Flujo general entre bloques

```
cotizador-danos-web
      ↕ REST / API
plataforma-danos-backend
      ↕ consultas de referencia
plataforma-core-ohs (catálogos, tarifas, CPs, agentes)
```

---

## 3. Alcance técnico del reto

El reto evalúa las siguientes capacidades:

- Construcción del **backend** (lógica de negocio, cálculo, persistencia)
- Construcción del **frontend** (flujo de captura, visualización de resultados)
- **Integración entre servicios** (`danos-backend` ↔ `core-ohs`)
- **Modelado de datos** (colecciones y dominio)
- **Manejo de reglas de negocio** (calculabilidad por ubicación, derivación de prima comercial)
- **Calidad del código** (estructura limpia, patrones, separación de responsabilidades)
- **Pruebas unitarias**
- **Pruebas automatizadas** (integración / E2E)
- **Documentación técnica y operativa**

---

## 4. Flujo del frontend (`cotizador-danos-web`)

El flujo planteado en el reto es:

1. **Crear o abrir folio** — iniciar una cotización nueva o retomar una existente
2. **Capturar datos generales** — datos del asegurado, agente, conducción y tipo de negocio
3. **Consultar catálogos** — suscriptores, agentes, giros, códigos postales (desde `core-ohs`)
4. **Capturar una o varias ubicaciones** — cada propiedad a asegurar con sus datos físicos y coberturas
5. **Editar una ubicación puntualmente** — modificación granular sin afectar el resto del folio
6. **Visualizar el progreso y estado del folio** — indicador de completitud y validaciones
7. **Configurar opciones de cobertura** — selección de garantías por ubicación
8. **Ejecutar cálculo** — trigger del motor de cálculo sobre el folio
9. **Mostrar resultados**:
   - Prima neta total
   - Prima comercial total
   - Desglose de prima por ubicación
10. **Mostrar alertas de ubicaciones incompletas** — sin bloquear el folio completo

---

## 5. Motor de cálculo — lógica mínima esperada

El proceso de cálculo debe:

1. **Leer la cotización completa** por número de folio
2. **Leer parámetros globales de cálculo** (desde `parametros_calculo`)
3. **Resolver datos técnicos requeridos** por cada ubicación (zona, giro, tipo constructivo, etc.)
4. **Determinar si cada ubicación es calculable o incompleta**
5. **Calcular prima por ubicación** aplicando tarifas técnicas a cada cobertura
6. **Consolidar prima neta total** (suma de primas netas por ubicación)
7. **Derivar prima comercial total** (prima neta + gastos + comisión de agente)
8. **Persistir el resultado financiero** en la colección `cotizaciones_danos`

### Componentes técnicos de cobertura (garantías)

Cada ubicación puede tener activas una o varias de estas coberturas:

| Componente | Descripción |
|---|---|
| Incendio edificios | Cobertura base sobre la construcción |
| Incendio contenidos | Cobertura sobre bienes muebles e inventarios |
| Extensión de cobertura | Riesgos adicionales sobre incendio (daño por agua, explosión, etc.) |
| CAT TEV | Catástrofe — Terremoto, Erupción Volcánica |
| CAT FHM | Catástrofe — Fenómenos Hidrometeorológicos (huracán, inundación) |
| Remoción de escombros | Costos de limpieza post-siniestro |
| Gastos extraordinarios | Erogaciones adicionales derivadas del siniestro |
| Pérdida de rentas | Lucro cesante por inhabilitación del inmueble |
| BI (Business Interruption) | Pérdida de utilidades por interrupción del negocio |
| Equipo electrónico | Cobertura all-risk para equipos electrónicos |
| Robo | Robo con violencia y/o asalto |
| Dinero y valores | Efectivo, cheques, títulos en caja fuerte o en tránsito |
| Vidrios | Rotura accidental de cristales |
| Anuncios luminosos | Daño a letreros y señalética iluminada |

> **Nota técnica:** El reto requiere que el motor calcule prima independiente por cobertura activa en cada ubicación, y que el sistema sepa qué coberturas son calculables (tienen datos suficientes) y cuáles deben alertarse como incompletas.

---

## 6. Modelo de datos

### 6.1 Colecciones de referencia (servidas por `core-ohs`)

| Colección | Contenido |
|---|---|
| `tarifas_incendio` | Tasas base y metadatos técnicos por giro comercial |
| `tarifas_cat` | Factores CAT (TEV y FHM) por zona geográfica |
| `tarifa_fhm` | Cuotas FHM por grupo, zona y condición |
| `factores_equipo` | Factor técnico para equipo electrónico por clase y nivel de zona |
| `catalogos_cp_zonas` | Relación entre código postal, zona CAT y nivel técnico |
| `dim_zona_tev` | Catálogo de zonas para normalización TEV |
| `dim_zona_fhm` | Catálogo de zonas para normalización FHM |
| `parametros_calculo` | Parámetros globales para derivar prima técnica → comercial |

### 6.2 Colección operativa principal

**`cotizaciones_danos`** — captura todo el ciclo de vida de la cotización:
- Datos del folio y asegurado
- Datos de conducción (agente, suscriptor)
- Ubicaciones con sus coberturas
- Resultado financiero (primas neta y comercial)
- Estado y versión del documento

---

## 7. Dominio mínimo esperado

### Entidad `Ubicacion`

```json
{
  "indice": 1,
  "nombreUbicacion": "Bodega Central CDMX",
  "direccion": "Av. Industria 340",
  "codigoPostal": "06600",
  "estado": "Ciudad de México",
  "municipio": "Cuauhtémoc",
  "colonia": "Doctores",
  "ciudad": "Ciudad de México",
  "tipoConstructivo": "Tipo 1 - Macizo",
  "nivel": 2,
  "añoConstruccion": 1998,
  "giro": {
    "descripcion": "Bodega de almacenamiento",
    "claveIncendio": "B-03"
  },
  "garantias": ["incendio_edificios", "incendio_contenidos", "cat_tev", "robo"],
  "zonaCatastrofica": "A",
  "alertasBloquantes": [],
  "estadoValidacion": "calculable"
}
```

### Entidad `Cotizacion`

```json
{
  "numeroFolio": "DAN-2025-00142",
  "estadoCotizacion": "en_proceso",
  "datosAsegurado": { "nombre": "...", "rfc": "..." },
  "datosConduccion": { "suscriptor": "...", "oficina": "..." },
  "codigoAgente": "AGT-001",
  "calificacionRiesgo": "estandar",
  "tipoNegocio": "comercial",
  "configuracionLayout": {},
  "opcionesCobertura": {},
  "ubicaciones": [],
  "primaNeta": 0.0,
  "primaComercial": 0.0,
  "primasPorUbicacion": [],
  "version": 1,
  "metadatos": {
    "creadoEn": "2025-06-01T10:00:00Z",
    "actualizadoEn": "2025-06-01T12:00:00Z",
    "creadoPor": "AGT-001"
  }
}
```

---

## 8. Conceptos clave del negocio

| Término | Definición |
|---|---|
| **Folio** | Expediente único de cotización. Puede estar en progreso. |
| **Ubicación** | Cada inmueble o predio a asegurar dentro del folio. |
| **Giro** | Actividad comercial del riesgo. Determina la tarifa de incendio. |
| **Clave de incendio (`claveIncendio`)** | Código que mapea el giro a la tarifa técnica de incendio. |
| **Zona catastrófica** | Clasificación geográfica de riesgo CAT (A, B, C…). Deriva del CP. |
| **Prima neta** | Costo puro del riesgo: `suma_asegurada × tarifa_técnica`. |
| **Prima comercial** | Prima que se cobra al cliente: `prima_neta + gastos + comisión_agente`. |
| **Prima por ubicación** | Prima neta calculada de forma independiente para cada inmueble. |
| **Calculable** | Ubicación con todos los datos mínimos para ejecutar el cálculo. |
| **Incompleta** | Ubicación con datos faltantes; genera alerta pero no bloquea el folio. |
| **Suscriptor** | Underwriter de la aseguradora que evalúa y acepta el riesgo. |
| **Parámetros globales** | Factores de conversión de prima técnica a prima comercial (gastos, comisiones). |

---

## 9. Relaciones entre conceptos (resumen)

```
Folio (cotizacion)
 ├── datosAsegurado
 ├── datosConduccion (agente, suscriptor)
 ├── opcionesCobertura (configuración global)
 └── Ubicaciones[]
       ├── direccion + codigoPostal → zona_cat (desde catalogos_cp_zonas)
       ├── giro.claveIncendio → tarifa_incendio
       ├── zonaCatastrofica → tarifas_cat / tarifa_fhm
       ├── garantias[] → lista de coberturas activas
       └── primaUbicacion → calculada por el motor

Motor de cálculo
 ├── Lee folio completo
 ├── Valida calculabilidad por ubicación
 ├── Aplica tarifas técnicas por cobertura
 ├── Suma → prima neta total
 ├── Aplica parametros_calculo → prima comercial total
 └── Persiste resultado en cotizaciones_danos
```

---

## 10. Notas para el desarrollo

- Las **ubicaciones incompletas** deben mostrar alertas en el frontend pero no impedir guardar o avanzar en el folio.
- El **motor de cálculo** debe poder ejecutarse de forma parcial (solo sobre ubicaciones calculables).
- El **código postal** es el pivot principal para determinar zona de riesgo: debe resolverse contra `catalogos_cp_zonas` para obtener `zona_cat` y `nivel_tecnico`.
- El campo `giro.claveIncendio` es el que conecta la descripción del negocio con la tarifa técnica real.
- Los **parámetros globales** en `parametros_calculo` aplican sobre el total del folio, no por ubicación.
- La **prima comercial** no es un cálculo por ubicación — se deriva de la prima neta total del folio.
- El reto espera que el resultado financiero se **persista**, no solo se muestre en pantalla.
- El sistema debe soportar **múltiples versiones** de una cotización (campo `version` + `metadatos`).

## 11. Alcance funcional obligatorio
### 11.1 Backend
- Cree folios con idempotencia
- Consulte y guarde datod generales de una cotizacion
- Consulte y guarde la configuracion del layout de ubicaciones
- Registre, consulte y edite ubicaciones
- Consulte el estado de la cotizacion
- Consulte y guarde opciones de cobertura
- Ejecute el calculo de prima neta y prima comercial
- Persista el resultado financiero sin sobreescribir otras secciones de la cotizacion
- Maneje versionado optimista en operaciones de edicion
#### 11.1.2 Endpoints backend (minimos)
POST /v1/folios
GET /v1/quotes/{folio}/general-info
PUT /v1/quotes/{folio}/general-info
GET /v1/quotes/{folio}/locations/layout
PUT /v1/quotes/{folio}/locations/layout
GET /v1/quotes/{folio}/locations
PUT /v1/quotes/{folio}/locations
PATCH /v1/quotes/{folio}/locations/{índice}
GET /v1/quotes/{folio}/locations/summary
GET /v1/quotes/{folio}/state
GET /v1/quotes/{folio}/coverage-options
PUT /v1/quotes/{folio}/coverage-options
POST /v1/quotes/{folio}/calculate

### 11.2 Frontend
- Crear o abrir un folio
- Capturar datos generales
- Consultar suscripciones, agentes, giros y codigos postales
- capturar una o varias ubicaciones
- Editar una ubicacion puntual
- visualizar el progreso y estado del folio
- Configurar opciones de cobertura
- Ejecutar el calculo
- Mostrar la prima neta, prima comercial y desgloce por ubicacion
- Mostrar alertas de ubicaciones incompletas sin bloquear completamente el folio
#### 11.2.1 Rutas funcionales
/cotizador
/quotes/{folio}/general-info
/quotes/{folio}/locations
/quotes/{folio}/technical-info
/quotes/{folio}/terms-and-conditions
### 11.3 Reglas de negocio obligatorias
- la cotizacion se identifica por numeroFolio
- el backend debe persistir la cotizacion como agregado principal
- las escrituras deben hacerse por actualizacion parcial
- al editar secciones funcionales, deben incrementarse la version
- debe actualizarse fechaUltimaActualizacion
- el calculo debe guardar primaNeta, primaComercial, primasPorUbicacion en una misma operacion logica
- si una ubicacion esta incompleta, esta ubicacion genera aerta, pero no debe inpedir calcular las demas
- una ubicacion no debe calcularse si no tiene codigo postal valido, giro.claveIncendio o garantias tarifables

### 11.4 Integración con servicios de referencia
El backend debe consumir o simular las siguientes capacidades del servicio core:
- catálogo de suscriptores
- consulta de agente por clave
- consulta de giros
- validación y consulta de código postal
- generación secuencial de folio
- consulta de catálogos de clasificación de riesgo y garantías
- consulta de tarifas y factores técnicos
#### 11.4.1 Endpoints de referencia del servicio core:
GET /v1/subscribers
GET /v1/agents
GET /v1/business-lines
GET /v1/zip-codes/{zipCode}
POST /v1/zip-codes/validate
GET /v1/folios
GET /v1/catalogs/risk-classification
GET /v1/catalogs/guarantees
GET|PUT /v1/tariffs/...

Si no se implementa un servicio real adicional, se acepta un stub, mock server o fixtures versionados siempre que el contrato quede documentado.