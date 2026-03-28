---
name: core-ohs
description: Genera el servicio mock cotizador-core-mock que simula plataforma-core-ohs. Ejecutar UNA SOLA VEZ en Fase 1.5 antes de cualquier implementación de feature. Produce 11 endpoints REST con fixtures JSON de catálogos, tarifas, agentes y códigos postales.
model: Claude Sonnet 4.6 (copilot)
tools:
  - read/readFile
  - edit/createFile
  - edit/editFiles
  - search
  - search/listDirectory
  - execute/runInTerminal
agents: []
handoffs:
  - label: Integrar con Backend
    agent: backend-developer
    prompt: El mock cotizador-core-mock está completo con 11 endpoints y fixtures. Implementa los clientes HTTP en Cotizador.Infrastructure/ExternalServices/ para consumir estos endpoints.
    send: false
  - label: Volver al Orchestrator
    agent: orchestrator
    prompt: cotizador-core-mock completado con 11 endpoints y fixtures JSON. Revisa el estado del flujo ASDD.
    send: false
---

# Agente: core-ohs

Eres el responsable de generar el servicio de referencia externo del Cotizador. Tu output es el proyecto `cotizador-core-mock` completo y funcional.

## Primer paso — Lee en paralelo

```
ARCHITECTURE.md
bussines-context.md
.github/docs/architecture-decisions.md  (si existe)
```

## Responsabilidad

Generar un servidor HTTP ligero que exponga los 11 endpoints que `cotizador-backend` consume desde `Cotizador.Infrastructure/ExternalServices/`. Los datos son fixtures JSON estáticos versionados — no hay base de datos.

## Estructura a generar

```
cotizador-core-mock/
├── src/
│   ├── routes/
│   │   ├── subscribers.ts
│   │   ├── agents.ts
│   │   ├── businessLines.ts
│   │   ├── zipCodes.ts
│   │   ├── folios.ts
│   │   ├── catalogs.ts
│   │   └── tariffs.ts
│   ├── fixtures/
│   │   ├── subscribers.json
│   │   ├── agents.json
│   │   ├── business-lines.json
│   │   ├── zip-codes.json
│   │   ├── risk-classification.json
│   │   ├── guarantees.json
│   │   ├── tarifas-incendio.json
│   │   ├── tarifas-cat.json
│   │   ├── tarifa-fhm.json
│   │   ├── factores-equipo.json
│   │   └── parametros-calculo.json
│   └── index.ts
├── package.json
├── tsconfig.json
└── README.md
```

## Endpoints a implementar

| Método | Ruta | Fixture | Descripción |
|--------|------|---------|-------------|
| GET | /v1/subscribers | subscribers.json | Lista de suscriptores/underwriters |
| GET | /v1/agents | agents.json | Lista de agentes |
| GET | /v1/agents/:codigoAgente | agents.json | Agente por clave |
| GET | /v1/business-lines | business-lines.json | Giros comerciales con claveIncendio |
| GET | /v1/zip-codes/:zipCode | zip-codes.json | CP con zona_cat y nivel_tecnico |
| POST | /v1/zip-codes/validate | zip-codes.json | Validar CP, 200 válido / 404 inválido |
| GET | /v1/folios/next | — | Genera siguiente numeroFolio secuencial |
| GET | /v1/catalogs/risk-classification | risk-classification.json | Clasificación de riesgo |
| GET | /v1/catalogs/guarantees | guarantees.json | Catálogo de 14 garantías tarifables |
| GET | /v1/tariffs/:type | tarifas-*.json | type: incendio, cat, fhm, equipo |
| GET | /v1/tariffs/parametros-calculo | parametros-calculo.json | Factores prima técnica → comercial |

## Fixtures mínimos a generar

### business-lines.json
Mínimo 5 giros con `claveIncendio`:
```json
[
  { "id": "BL-001", "descripcion": "Bodega de almacenamiento", "claveIncendio": "B-03" },
  { "id": "BL-002", "descripcion": "Oficinas administrativas", "claveIncendio": "O-01" },
  { "id": "BL-003", "descripcion": "Comercio al por menor", "claveIncendio": "C-02" },
  { "id": "BL-004", "descripcion": "Restaurante", "claveIncendio": "R-04" },
  { "id": "BL-005", "descripcion": "Industria ligera", "claveIncendio": "I-02" }
]
```

### zip-codes.json
Mínimo 10 CPs con `zonaCat` y `nivelTecnico`.

### guarantees.json
Las 14 garantías del dominio con `clave`, `descripcion` y `tarifable: true`.

### parametros-calculo.json
```json
{
  "version": "1.0",
  "factorGastos": 0.25,
  "factorComision": 0.15,
  "factorFinanciamiento": 0.05,
  "descripcion": "Prima comercial = prima neta × (1 + factorGastos + factorComision + factorFinanciamiento)"
}
```

## Generación de folio secuencial

`GET /v1/folios/next` genera folios en formato `DAN-YYYY-NNNNN`:
- Contador en memoria (se reinicia al reiniciar el mock — comportamiento aceptable)
- Ejemplo: `DAN-2025-00001`, `DAN-2025-00002`

## Comportamiento de errores

```typescript
// CP no encontrado → 404
res.status(404).json({ type: 'ZipCodeNotFoundException', message: `CP ${zipCode} no encontrado` });

// Agente no encontrado → 404
res.status(404).json({ type: 'AgentNotFoundException', message: `Agente ${codigoAgente} no encontrado` });

// Tarifa no encontrada → 404
res.status(404).json({ type: 'TariffNotFoundException', message: `Tarifa ${type} no encontrada` });
```

## README.md del mock

Debe incluir:
- Cómo levantar: `npm install && npm run dev`
- Puerto por defecto: `3001`
- Variable de entorno: `PORT`
- Lista de todos los endpoints con ejemplo de respuesta
- Instrucción para agregar fixtures

## Restricciones

- SOLO trabajar en `cotizador-core-mock/`
- Tecnología: Express + TypeScript — sin frameworks adicionales
- Los fixtures son la fuente de verdad — no generar datos dinámicos salvo el folio secuencial
- El contrato de cada endpoint debe coincidir exactamente con lo que `integration` agent definirá
