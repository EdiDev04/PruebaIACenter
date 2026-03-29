# cotizador-core-mock

Mock service that simulates `plataforma-core-ohs`. Exposes 13 REST endpoints serving static reference data used by `cotizador-backend`.

## Setup

```bash
npm install
npm run dev      # Development (ts-node, port 3001)
npm run build    # Compile TypeScript → dist/
npm start        # Run compiled build
```

## Environment variables

| Variable | Default | Description |
|---|---|---|
| `PORT` | `3001` | HTTP port |
| `FOLIO_START` | `1` | Starting counter for folio generation (resets on restart) |

## Endpoints

All endpoints propagate the `X-Correlation-Id` header. If the request does not include it, a UUID v4 is generated automatically.

All successful responses use the envelope: `{ "data": ... }`  
All error responses use: `{ "type": "string", "message": "string" }`

| Method | Path | Description |
|---|---|---|
| GET | `/health` | Health check |
| GET | `/v1/subscribers` | List all subscribers/underwriters |
| GET | `/v1/agents` | List all agents (optional `?code=<agentCode>` filter) |
| GET | `/v1/business-lines` | List business lines with `fireKey` |
| GET | `/v1/zip-codes/:zipCode` | Lookup zip code with `catZone` and `technicalLevel` |
| POST | `/v1/zip-codes/validate` | Validate a zip code. Body: `{ "zipCode": "06600" }` |
| GET | `/v1/folios/next` | Generate next sequential folio (`DAN-YYYY-NNNNN`) |
| GET | `/v1/catalogs/risk-classification` | Risk classification catalog |
| GET | `/v1/catalogs/guarantees` | 14 tarifable guarantees |
| GET | `/v1/tariffs/fire` | Fire tariff rates by `fireKey` |
| GET | `/v1/tariffs/cat` | CAT tariff factors by zone (A, B, C) |
| GET | `/v1/tariffs/fhm` | FHM tariff rates by group and zone |
| GET | `/v1/tariffs/electronic-equipment` | Electronic equipment factors |
| GET | `/v1/tariffs/calculation-parameters` | Premium calculation parameters |

## Example requests

```bash
# Subscribers
curl http://localhost:3001/v1/subscribers

# Agent by code
curl "http://localhost:3001/v1/agents?code=AGT-001"

# Zip code lookup
curl http://localhost:3001/v1/zip-codes/06600

# Zip code validation
curl -X POST http://localhost:3001/v1/zip-codes/validate \
  -H "Content-Type: application/json" \
  -d '{"zipCode":"44100"}'

# Next folio
curl http://localhost:3001/v1/folios/next

# Tariffs
curl http://localhost:3001/v1/tariffs/fire
curl http://localhost:3001/v1/tariffs/cat
curl http://localhost:3001/v1/tariffs/fhm
curl http://localhost:3001/v1/tariffs/electronic-equipment
curl http://localhost:3001/v1/tariffs/calculation-parameters

# With correlation ID
curl -H "X-Correlation-Id: my-trace-123" http://localhost:3001/v1/subscribers
```

## Adding or updating fixtures

All static data lives in `src/fixtures/`. Edit the relevant JSON file and restart the service. No code changes required.

**Cross-reference constraints:**
- `businessLines[].fireKey` must exist in `fireTariffs[].fireKey`
- `zipCodes[].catZone` must be one of the zones in `catTariffs[].zone` (A, B, or C)
- `guarantees` must contain exactly 14 records

## Project structure

```
src/
├── fixtures/          # Static JSON data (source of truth)
├── middleware/
│   └── correlationId.ts
├── routes/
│   ├── subscriberRoutes.ts
│   ├── agentRoutes.ts
│   ├── businessLineRoutes.ts
│   ├── zipCodeRoutes.ts
│   ├── folioRoutes.ts
│   ├── catalogRoutes.ts
│   └── tariffRoutes.ts
├── types/
│   └── index.ts
└── index.ts
```
