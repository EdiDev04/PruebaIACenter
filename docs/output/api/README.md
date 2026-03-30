# docs/output/api — Índice de Documentación Técnica

Documentación generada por el agente `tech-docs`. Cada archivo corresponde a uno o más SPECs implementados.

---

## Endpoints documentados

| SPEC | Feature | Endpoint(s) | Archivo |
|---|---|---|---|
| SPEC-003 | Creación de folio | `POST /v1/folios` | [contracts-spec-003-004.md](contracts-spec-003-004.md) |
| SPEC-004 | Información general | `GET /PUT /v1/quotes/{folio}/general-info` | [contracts-spec-003-004.md](contracts-spec-003-004.md) |
| SPEC-005 | Layout de ubicaciones | `GET /PUT /v1/quotes/{folio}/locations/layout` | [technical-reference.md](technical-reference.md) |
| SPEC-006 | Gestión de ubicaciones | `GET /PUT /v1/quotes/{folio}/locations` · `PATCH /v1/quotes/{folio}/locations/{index}` · `GET /v1/quotes/{folio}/locations/summary` | [technical-reference.md](technical-reference.md) |
| SPEC-007 | Opciones de cobertura | `GET /PUT /v1/quotes/{folio}/coverage-options` | [technical-reference.md](technical-reference.md) |
| **SPEC-008** | **Estado y progreso** | **`GET /v1/quotes/{folio}/state`** | **[quote-state-progress-api.md](quote-state-progress-api.md)** |

---

## Architecture Decision Records (ADR)

| ADR | Título | SPEC | Archivo |
|---|---|---|---|
| ADR-008 | Diseño del endpoint de estado del folio | SPEC-008 | [adr-008-quote-state-design.md](adr-008-quote-state-design.md) |

---

## Referencia general

| Documento | Contenido |
|---|---|
| [technical-reference.md](technical-reference.md) | Modelo de datos MongoDB, índices, constantes de garantías, contratos de integración |
| [contracts-spec-003-004.md](contracts-spec-003-004.md) | Auditoría de contratos FE↔BE para SPEC-003 y SPEC-004 |
| [spec-003-004-contracts.md](spec-003-004-contracts.md) | Contratos detallados de integración con core-ohs |

---

## Convenciones

- Todo response exitoso usa envelope: `{ "data": {...} }` (ADR global)
- Mensajes de error siempre en español
- Autenticación: Basic Auth en todos los endpoints bajo `/v1/quotes/`
- Formato de folio: `DAN-YYYY-NNNNN` — validado con regex en cada controller
