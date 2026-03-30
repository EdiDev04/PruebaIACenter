# Cotizador de Seguros de Daños

> Reto IA Center — ASDD (Agent Spec-Driven Development)

Sistema de cotización de seguros para pólizas de daños (incendio, CAT, equipo electrónico, falla de maquinaria, etc.). Permite crear folios, registrar información general del riesgo asegurado y calcular primas en base a tarifas del core externo.

---

## Arquitectura

```
 cotizador-webapp
 React 18 + TypeScript + Vite
 Puerto: 5173
       |
       | REST (Basic Auth)
       ↓
 cotizador-backend
 .NET 8 + ASP.NET Core + MongoDB
 Puerto: 5001
       |
       | HTTP (HttpClient)
       ↓
 cotizador-core-mock
 Node.js + Express + TypeScript
 Puerto: 3001
```

---

## Stack tecnológico

| Componente | Tecnología |
|------------|-----------|
| Frontend | React 18 · TypeScript · Redux Toolkit · TanStack Query · Vite |
| Backend | C# · .NET 8 · ASP.NET Core · MongoDB.Driver · FluentValidation |
| Core mock | Node.js · Express · TypeScript · fixtures JSON |
| Testing BE | xUnit · Moq · FluentAssertions |
| Testing FE | Vitest · Testing Library |

---

## Prerrequisitos

- Node.js 20+
- .NET SDK 8
- MongoDB 7+ (local o Docker o Atlas)
- npm 9+

---

## Variables de entorno

### cotizador-webapp — `.env.local`

Crea el archivo copiando `.env.example`:

```bash
cp cotizador-webapp/.env.example cotizador-webapp/.env.local
```

| Variable | Valor por defecto (desarrollo) | Descripción |
|----------|-------------------------------|-------------|
| `VITE_API_URL` | `http://localhost:5001` | URL base del backend |
| `VITE_API_USER` | `admin` | Usuario para Basic Auth |
| `VITE_API_PASSWORD` | `cotizador2026` | Contraseña para Basic Auth |
| `VITE_CORE_MOCK_URL` | `http://localhost:3001` | URL del core mock (referencia) |

### cotizador-backend — `appsettings.Development.json`

```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "cotizador_db"
  },
  "CoreOhs": {
    "BaseUrl": "http://localhost:3001"
  },
  "Auth": {
    "Username": "admin",
    "Password": "cotizador2026"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173"]
  }
}
```

### cotizador-core-mock

| Variable | Valor por defecto | Descripción |
|----------|------------------|-------------|
| `PORT` | `3001` | Puerto HTTP del mock |

---

## Arranque del proyecto

Se necesitan **3 terminales** abiertas en la raíz del repositorio.

### Terminal 1 — cotizador-core-mock

```bash
cd cotizador-core-mock
npm install
npm run dev
# Servicio disponible en: http://localhost:3001
# Verificar: curl http://localhost:3001/health
```

### Terminal 2 — cotizador-backend

```bash
cd cotizador-backend
dotnet restore
dotnet run --project src/Cotizador.API/Cotizador.API.csproj
# Servicio disponible en: http://localhost:5001
```

> Asegúrate de que `appsettings.Development.json` tenga las credenciales y la URL de CORS correctas antes de iniciar.

### Terminal 3 — cotizador-webapp

```bash
cd cotizador-webapp
cp .env.example .env.local   # solo la primera vez
npm install
npm run dev
# Aplicación disponible en: http://localhost:5173
```

### Script unificado (macOS/Linux)

```bash
./start-local.sh
```

### Script unificado (Windows PowerShell)

```powershell
.\start-local.ps1
```

Ambos scripts levantan los 3 servicios en orden, instalan dependencias si es necesario y detienen todo al presionar `Ctrl+C`.

### Opción alternativa — MongoDB con Docker

Si no tienes MongoDB instalado localmente:

```bash
docker run -d --name mongo-cotizador -p 27017:27017 mongo:7
```

---

## Fixtures y datos de prueba

El servicio `cotizador-core-mock` sirve datos estáticos desde `cotizador-core-mock/src/fixtures/`. No requiere base de datos ni configuración adicional.

| Fixture | Descripción |
|---------|-------------|
| `agents.json` | Catálogo de agentes con código, nombre y comisión |
| `businessLines.json` | Giros comerciales con `fireKey` para lookup de tarifas |
| `calculationParameters.json` | Parámetros globales: gastos, comisión, derechos, IVA |
| `fireTariffs.json` | Tarifas de incendio indexadas por `fireKey` |
| `catTariffs.json` | Factores CAT (TEV + FHM) indexados por zona |
| `fhmTariffs.json` | Tarifas de fenómenos hidrometeorológicos |
| `electronicEquipmentFactors.json` | Factores por clase de equipo y nivel técnico |
| `guarantees.json` | Catálogo completo de garantías disponibles |
| `riskClassification.json` | Clasificaciones de riesgo |
| `subscribers.json` | Catálogo de suscriptores/asegurados |
| `zipCodes.json` | Códigos postales → zona + nivel técnico |

---

## Ejecutar tests

### Backend

```bash
cd cotizador-backend
dotnet test src/Cotizador.Tests/Cotizador.Tests.csproj
```

### Frontend

```bash
cd cotizador-webapp
npm test
```

---

## Modelo de datos principal

Colección MongoDB: `property_quotes`. Un documento por folio.

```
PropertyQuote
├── folioNumber          string        "DAN-2026-00001"
├── quoteStatus          string        draft | in_progress | calculated
├── version              int           versionado optimista (incrementa en cada escritura)
├── insuredData          object        nombre asegurado, RFC, dirección fiscal
├── conductionData       object        fecha inicio/fin de vigencia, moneda
├── agentCode            string        código del agente (lookup en core-mock)
├── riskClassification   string        clasificación de riesgo
├── businessType         string        tipo de negocio
├── layoutConfiguration  object        número de ubicaciones habilitadas
├── coverageOptions      object        garantías habilitadas para el folio
├── locations[]          array         una entrada por ubicación de riesgo
│   ├── index            int
│   ├── locationName     string
│   ├── zipCode          string        CP de 5 dígitos
│   ├── businessLine     object        giro comercial + fireKey
│   ├── catZone          string        zona CAT (para tarifas TEV/FHM)
│   ├── validationStatus string        calculable | incomplete
│   └── guarantees[]     array         garantías con sumaAsegurada por ubicación
├── netPremium           decimal       prima neta total del folio
├── commercialPremiumBeforeTax decimal  prima comercial sin IVA
├── commercialPremium    decimal       prima comercial con IVA
├── premiumsByLocation[] array         desglose financiero por ubicación
└── metadata             object        timestamps, idempotencyKey, lastWizardStep
```

Documentación extendida: [`docs/output/api/modelo-datos.md`](docs/output/api/modelo-datos.md)

---

## Lógica de cálculo de primas

El motor de cálculo está implementado en `Cotizador.Domain.Services.PremiumCalculator` y es invocado por `CalculateQuoteUseCase`. Opera en 3 fases: **calculabilidad → prima por cobertura → consolidación comercial**.

### 1. Criterios de calculabilidad por ubicación

Una ubicación participa en el cálculo solo si cumple los 3 criterios:

$$\text{calculable} = \text{CP válido} \;\land\; \text{FireKey definida} \;\land\; \text{Garantías} \geq 1$$

Las ubicaciones que no cumplen quedan con `validationStatus = "incomplete"` y `netPremium = 0`. Nunca bloquean el folio.

### 2. Prima neta por cobertura

Para cada garantía de una ubicación calculable:

$$P_{\text{cobertura}} = \text{SumaAsegurada} \times \text{Tasa}$$

La tasa se resuelve por `guaranteeKey` según la siguiente tabla:

| Cobertura (`guaranteeKey`) | Fuente de tasa |
|---|---|
| `building_fire`, `contents_fire`, `coverage_extension` | `fireTariffs[fireKey].baseRate` |
| `cat_tev` | `catTariffs[zone].tevFactor` |
| `cat_fhm` | `catTariffs[zone].fhmFactor` |
| `debris_removal`, `extraordinary_expenses` | Fija: `0.0010` |
| `rent_loss`, `business_interruption` | Fija: `0.0015` |
| `theft`, `cash_and_securities` | Fija: `0.0020` |
| `electronic_equipment` | `electronicEquipmentFactors[equipmentClass=A, zoneLevel=technicalLevel].factor` |
| `glass`, `illuminated_signs` | Tarifa plana (monto fijo, tasa ignorada) |

El `technicalLevel` para equipo electrónico se obtiene del servicio de códigos postales del core (`GET /zip-codes/{cp}`).

### 3. Prima neta por ubicación

$$P_{\text{ubicación}} = \sum_{i=1}^{n} P_{\text{cobertura}_i}$$

### 4. Prima neta total del folio

Solo se suman las ubicaciones calculables:

$$P_{\text{neta}} = \sum_{\text{ubicaciones calculables}} P_{\text{ubicación}}$$

### 5. Prima comercial

Se aplica un **loading factor** con los parámetros globales del core (`GET /calculation-parameters`):

$$\text{loadingFactor} = 1 + \text{expeditionExpenses} + \text{agentCommission} + \text{issuingRights} + \text{surcharges}$$

Con los parámetros estándar configurados en los fixtures:

$$\text{loadingFactor} = 1 + 0.05 + 0.10 + 0.03 + 0.02 = 1.20$$

$$P_{\text{comercial sin IVA}} = \text{round}(P_{\text{neta}} \times 1.20,\; 2)$$

$$P_{\text{comercial con IVA}} = \text{round}(P_{\text{comercial sin IVA}} \times (1 + 0.16),\; 2)$$

### Ejemplo numérico

| Variable | Valor |
|---|---|
| Suma asegurada edificio | $1,000,000 |
| Tasa incendio (`fireRate`) | 0.0025 |
| Prima incendio | $2,500.00 |
| Prima neta total (1 ubicación) | $2,500.00 |
| Prima comercial sin IVA | $2,500 × 1.20 = **$3,000.00** |
| Prima comercial con IVA | $3,000 × 1.16 = **$3,480.00** |

### Referencia de código fuente

| Clase | Responsabilidad |
|---|---|
| `Cotizador.Domain.Services.PremiumCalculator` | Fórmulas puras de cálculo (sin dependencias externas) |
| `Cotizador.Application.UseCases.CalculateQuoteUseCase` | Orquestación: lee folio, consulta tarifas, itera ubicaciones, persiste |
| `Cotizador.Infrastructure.ExternalServices.CoreOhsClient` | Consulta paralela de tarifas al core-mock |

Documentación extendida: [`docs/output/api/calculo-prima.md`](docs/output/api/calculo-prima.md)

---

## Endpoints disponibles

Todos los endpoints requieren `Authorization: Basic <base64(user:password)>`.

| Método | Endpoint | Descripción |
|--------|----------|-------------|
| `POST` | `/v1/folios` | Crea un nuevo folio de cotización. Requiere header `Idempotency-Key` |
| `GET` | `/v1/quotes/{folio}` | Obtiene el resumen completo de una cotización |
| `GET` | `/v1/quotes/{folio}/general-info` | Obtiene la información general de una cotización |
| `PUT` | `/v1/quotes/{folio}/general-info` | Actualiza la información general de una cotización |
| `GET` | `/v1/quotes/{folio}/state` | Estado y progreso de la cotización — pasos completados, alertas de ubicación y cobertura activa (SPEC-008) |

**Formato de folio:** `DAN-YYYY-NNNNN` (ejemplo: `DAN-2026-00001`)

**Autenticación:** Basic Auth — `admin` / `cotizador2026`

### Contratos API detallados

La documentación completa de contratos (request/response, códigos de error, ejemplos) está en [`docs/output/api/`](docs/output/api/):

| Archivo | Contenido |
|---------|-----------|
| [`README.md`](docs/output/api/README.md) | Índice general de contratos |
| [`contracts-spec-003-004.md`](docs/output/api/contracts-spec-003-004.md) | Contratos de creación de folio y datos generales |
| [`spec-003-004-contracts.md`](docs/output/api/spec-003-004-contracts.md) | Requests/responses con ejemplos JSON |
| [`quote-state-progress-api.md`](docs/output/api/quote-state-progress-api.md) | Contrato de `GET /v1/quotes/{folio}/state` |
| [`premium-calculation-engine-api.md`](docs/output/api/premium-calculation-engine-api.md) | Contrato de `POST /v1/quotes/{folio}/calculate` |
| [`adr-008-quote-state-design.md`](docs/output/api/adr-008-quote-state-design.md) | ADR — diseño de la máquina de estados del folio |
| [`calculo-prima.md`](docs/output/api/calculo-prima.md) | Lógica de cálculo paso a paso con código |
| [`modelo-datos.md`](docs/output/api/modelo-datos.md) | Esquema MongoDB completo |
| [`technical-reference.md`](docs/output/api/technical-reference.md) | Referencia técnica general |

---

## Estructura del proyecto

```
cotizador-backend/
├── src/
│   ├── Cotizador.API/           # Controllers, middleware, auth
│   ├── Cotizador.Application/   # Casos de uso, DTOs, interfaces
│   ├── Cotizador.Domain/        # Entidades, value objects, reglas de dominio
│   ├── Cotizador.Infrastructure/
│   │   ├── Persistence/         # Repositorios MongoDB
│   │   └── ExternalServices/    # Cliente HTTP para core-ohs
│   └── Cotizador.Tests/         # Tests unitarios

cotizador-core-mock/
└── src/
    ├── fixtures/                # Datos JSON estáticos (tarifas, catálogos)
    └── routes/                  # Endpoints del mock

cotizador-webapp/
└── src/
    ├── app/                     # Store, router, query client
    ├── entities/                # Entidades de dominio FE (folio, catalog)
    ├── features/                # Flujos de usuario (creación, búsqueda)
    ├── pages/                   # Páginas enrutadas
    └── shared/                  # httpClient, componentes compartidos
```

---

## Diseño de pantallas — Google Stitch MCP

El diseño UI del proyecto se generó con **Google Stitch** vía MCP, integrado directamente en el flujo ASDD como la fase de UX (`ux-designer` agent) previa a la implementación frontend.

### Cómo funciona

El agente `ux-designer` analiza el modelo de datos de cada spec, genera un _design spec_ (`Data → UI mapping`) y llama al MCP de Stitch para producir pantallas HTML renderizadas. Cada pantalla pasa por el design system compartido del proyecto antes de ser entregada al `frontend-developer`.

```
spec.md → Data→UI mapping → mcp_stitch_create_project
                           → mcp_stitch_create_design_system
                           → mcp_stitch_generate_screen_from_text  (por pantalla)
                           → mcp_stitch_apply_design_system
                           → HTML/CSS extraído → .github/design-specs/screens/
```

### Proyecto Stitch

| Campo | Valor |
|---|---|
| Modelo IA | `GEMINI_3_FLASH` |
| Config | `.github/design-specs/.stitch-config.json` |

### Pantallas generadas (21 en total)

| Feature | Pantalla | Archivo HTML |
|---------|----------|--------------|
| Creación de folio | Inicio / Home | `screens/folio-creation/home.html` |
| Creación de folio | Confirmación de folio creado | `screens/folio-creation/confirmation.html` |
| Datos generales | Wizard header — Paso 1 | `screens/general-info-management/wizard-header.html` |
| Datos generales | Formulario de datos generales | `screens/general-info-management/formulario-datos-generales.html` |
| Datos generales | Modal conflicto de versión | `screens/general-info-management/modal-conflicto-version.html` |
| Layout de ubicaciones | Panel configuración por defecto | `screens/location-layout-configuration/panel-config-default.html` |
| Layout de ubicaciones | Panel configuración personalizado | `screens/location-layout-configuration/panel-config-personalizado.html` |
| Layout de ubicaciones | Panel error conflicto | `screens/location-layout-configuration/panel-error-conflicto.html` |
| Gestión de ubicaciones | Grilla de ubicaciones | `screens/location-management/grilla-ubicaciones.html` |
| Gestión de ubicaciones | Formulario datos del inmueble | `screens/location-management/formulario-datos-inmueble.html` |
| Gestión de ubicaciones | Formulario de coberturas | `screens/location-management/formulario-coberturas.html` |
| Gestión de ubicaciones | Resumen de validación | `screens/location-management/resumen-validacion.html` |
| Opciones de cobertura | Formulario principal | `screens/coverage-options-configuration/formulario-principal.html` |
| Opciones de cobertura | Warning deshabilitación | `screens/coverage-options-configuration/warning-deshabilitacion.html` |
| Opciones de cobertura | Error conflicto de versión | `screens/coverage-options-configuration/error-conflicto-version.html` |
| Estado y progreso | Wizard layout + progress bar | `screens/quote-state-progress/wizard-layout-progressbar.html` |
| Estado y progreso | Panel alertas de ubicación | `screens/quote-state-progress/panel-location-alerts.html` |
| Resultados | Resumen financiero | `screens/results-display/resumen-financiero.html` |
| Resultados | Desglose por ubicación | `screens/results-display/desglose-ubicaciones.html` |
| Resultados | Panel de alertas incompletas | `screens/results-display/panel-alertas-incompletas.html` |
| Resultados | Estado no calculado | `screens/results-display/estado-no-calculado.html` |

Todos los archivos HTML son la fuente de referencia visual que el `frontend-developer` usó para implementar los componentes React. Los design specs con Data→UI mapping completo están en `.github/design-specs/`.

---

## Specs ASDD generados

El proyecto siguió la metodología **ASDD (Agent Spec-Driven Development)**. Cada feature requirió una spec aprobada antes de cualquier implementación.

| ID | Título | Estado |
|----|--------|--------|
| SPEC-001 | Core Reference Service (Mock) | ✅ IMPLEMENTED |
| SPEC-002 | Quote Data Model and Persistence | ✅ IMPLEMENTED |
| SPEC-003 | Creación y Apertura de Folio | ✅ IMPLEMENTED |
| SPEC-004 | Gestión de Datos Generales de Cotización | ✅ IMPLEMENTED |
| SPEC-005 | Configuración del Layout de Ubicaciones | ✅ IMPLEMENTED |
| SPEC-006 | Gestión de Ubicaciones de Riesgo | ✅ IMPLEMENTED |
| SPEC-007 | Configuración de Opciones de Cobertura | ✅ IMPLEMENTED |
| SPEC-008 | Estado y Progreso de la Cotización | ✅ IMPLEMENTED |
| SPEC-009 | Motor de Cálculo de Primas | ✅ IMPLEMENTED |
| SPEC-010 | Visualización de Resultados y Alertas | ✅ IMPLEMENTED |

Los specs están en `.github/specs/`. Los requerimientos de negocio de origen están en `.github/requirements/`.

---

## Supuestos y limitaciones

| # | Supuesto / Limitación |
|---|----------------------|
| 1 | **Autenticación simplificada**: se usa Basic Auth stateless. No hay JWT, roles ni gestión de sesiones. Credenciales configurables vía `appsettings`. |
| 2 | **core-ohs mockeado**: `cotizador-core-mock` simula `plataforma-core-ohs` con fixtures JSON estáticos. No hay integración con el sistema real. |
| 3 | **Tarifas simplificadas**: las coberturas `debris_removal`, `rent_loss`, `theft`, etc. usan tasas fijas documentadas en lugar de tablas actuariales completas. Cualquier fórmula simplificada está documentada en `docs/output/api/calculo-prima.md`. |
| 4 | **Equipo electrónico**: se aplica siempre `equipmentClass = "A"` como valor por defecto (SUP-009-05 del SPEC-009). |
| 5 | **Ubicaciones incompletas**: no bloquean el cálculo. Generan alertas visibles en el resultado pero el folio avanza a estado `calculated` si existe al menos 1 ubicación calculable. |
| 6 | **Versionado optimista sin transacciones distribuidas**: la consistencia concurrente se garantiza por el campo `version` en MongoDB. Si dos usuarios editan el mismo folio simultáneamente, el segundo recibe HTTP 409 y debe recargar. |
| 7 | **Sin paginación**: los endpoints de listado retornan todos los registros. En producción se requeriría paginación para folios con muchas ubicaciones. |
| 8 | **MongoDB Atlas o local**: el sistema funciona con MongoDB local (`localhost:27017`) o Atlas. La cadena de conexión se configura en `appsettings.Development.json`. |

---

## Decisiones técnicas relevantes

### Regla de dependencia (Clean Architecture)

```
Cotizador.API → Cotizador.Application → Cotizador.Domain
                      ↑
       Cotizador.Infrastructure (Persistence + ExternalServices)
```

`Cotizador.Domain` no referencia ningún otro proyecto. `Cotizador.API` nunca referencia `Infrastructure` directamente — solo la registra en `Program.cs` vía extension methods (`AddInfrastructure`).

### Manejo de errores tipado

Todas las excepciones de dominio están en `Cotizador.Domain/Exceptions/` y son capturadas por `ExceptionHandlingMiddleware`:

| Excepción | HTTP | Cuándo |
|-----------|------|--------|
| `FolioNotFoundException` | 404 | Folio no existe en BD |
| `VersionConflictException` | 409 | Versión desactualizada (optimistic locking) |
| `InvalidQuoteStateException` | 422 | Operación inválida para el estado actual |
| `CoreOhsUnavailableException` | 503 | core-mock no responde |
| `ValidationException` (FluentValidation) | 400 | Request inválido |

Formato de error estándar en todos los endpoints:

```json
{ "type": "string", "message": "string", "field": "string | null" }
```

Nunca se expone `StackTrace` ni mensajes internos en la respuesta HTTP.

### Motor de cálculo — invariante de diseño

Las ubicaciones incompletas **nunca bloquean** el cálculo. Si una ubicación no tiene CP válido, giro comercial o garantías con suma asegurada, se marca como `incomplete` y genera una alerta — el folio sigue siendo calculable con las ubicaciones que sí cumplen los criterios. Esto está implementado en `PremiumCalculator` (dominio puro, sin dependencias externas).

### Versionado optimista

Cada documento `PropertyQuote` en MongoDB tiene un campo `Version` que se incrementa en cada escritura. El cliente envía la versión que conoce; si no coincide con la de BD, el backend retorna 409. Esto previene pérdida de datos en edición concurrente sin usar transacciones distribuidas.

### Consulta de tarifas en paralelo

`CalculateQuoteUseCase` consulta los 4 catálogos de tarifas (incendio, CAT, FHM, equipo electrónico) al core-mock en paralelo con `Task.WhenAll`. Esto reduce la latencia del endpoint `/calculate` de forma proporcional al número de tarifas.

### Autenticación Basic Auth

El backend usa Basic Auth stateless con credenciales configurables vía `appsettings`. No hay sesiones ni JWT. La decisión está documentada en la spec (simplificación acordada para el reto).

---

## Estrategia de pruebas

### Niveles de prueba

| Nivel | Framework | Ubicación | Archivos |
|-------|-----------|-----------|---------|
| Unitario Backend | xUnit + Moq + FluentAssertions | `Cotizador.Tests/` | 32 archivos |
| Unitario Frontend | Vitest + Testing Library | `cotizador-webapp/src/__tests__/` | 29 archivos |
| E2E automatizados | Playwright | `cotizador-automatization/e2e/` | 3 flujos |

### Qué cubre cada nivel

**Backend (32 archivos):** casos de uso de cada SPEC, entidades de dominio (`PropertyQuote`, `Location`, `PremiumCalculator`), excepciones de dominio, repositorio MongoDB (con cliente real en memoria), validadores FluentValidation.

**Frontend (29 archivos):** schemas Zod de formularios, hooks de TanStack Query, componentes de UI (LocationRow, FinancialSummary, CoverageAccordion), slices de Redux, integración de páginas (ResultsPage).

**Principio de test de dominio:** `PremiumCalculator` se prueba con datos de entrada directos sin mocks — así se valida la lógica de cálculo en aislamiento total.

### Ejecutar pruebas

```bash
# Backend
cd cotizador-backend
dotnet test src/Cotizador.Tests/Cotizador.Tests.csproj

# Frontend
cd cotizador-webapp
npm test

# E2E (requiere los 3 servicios corriendo)
cd cotizador-automatization
npm run test:e2e
```

---

## Pruebas automatizadas E2E (Playwright)

Los tests E2E están en `cotizador-automatization/` y usan el patrón **Page Object Model**.

### Flujos cubiertos

| Flujo | Archivo | Descripción |
|-------|---------|-------------|
| **Flujo 1** | `flujo1-ciclo-completo.spec.ts` | Ciclo completo: crear folio → datos generales → agregar ubicación calculable → configurar coberturas → calcular → verificar prima en pantalla de resultados |
| **Flujo 3** | `flujo3-conflicto-version.spec.ts` | Edición concurrente con versionado optimista: dos contextos de browser editan el mismo folio; el segundo recibe 409 y se muestra modal de conflicto |
| **Flujo 5** | `flujo5-resultados.spec.ts` | Pantalla de resultados: verifica que prima neta, prima comercial (sin IVA), prima comercial total y tabla de desglose por ubicación se renderizan correctamente |

### Comandos por flujo

```bash
npm run test:e2e               # todos los flujos
npm run test:e2e:flujo1        # ciclo completo
npm run test:e2e:flujo3        # conflicto de versión
npm run test:e2e:flujo5        # visualización de resultados
npm run test:e2e:report        # abrir reporte HTML de Playwright
```
