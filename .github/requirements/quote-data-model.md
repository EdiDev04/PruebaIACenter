# REQ-02: Modelo de Datos y Persistencia de Cotización

## Oleada de despliegue: 1 — Fundación
## Dependencias: Ninguna (solo decisiones de arquitectura)
## Prioridad: Crítica (bloqueante para todo el backend)

---

## Descripción

Diseñar e implementar el modelo de datos principal `property_quotes` en MongoDB y la capa de persistencia (repositorios). Este modelo es el agregado raíz del sistema y debe soportar actualizaciones parciales por sección, versionado optimista y auditoría de cambios.

---

## Historias de Usuario

**HU-02.1** — Como sistema, quiero persistir la cotización como un documento único en MongoDB para mantener toda la información del folio en un solo agregado.

**HU-02.2** — Como sistema, quiero actualizar secciones de la cotización de forma parcial (datos generales, ubicaciones, coberturas, resultado financiero) sin sobreescribir otras secciones.

**HU-02.3** — Como sistema, quiero implementar versionado optimista para que las ediciones concurrentes sean detectadas y rechazadas con error apropiado.

**HU-02.4** — Como sistema, quiero que cada escritura incremente la versión y actualice `metadata.updatedAt` automáticamente.

---

## Modelo de datos principal — colección `property_quotes`

```
{
  folioNumber: string (PK, unique, formato DAN-YYYY-NNNNN),
  quoteStatus: enum ["draft", "in_progress", "calculated", "finalized"],
  insuredData: { name, taxId, ... },
  conductionData: { subscriber, office, ... },
  agentCode: string,
  riskClassification: string,
  businessType: string,
  layoutConfiguration: object,
  coverageOptions: object,
  locations: [Location],
  netPremium: decimal,
  commercialPremium: decimal,
  premiumsByLocation: [LocationPremium],
  version: integer (starts at 1),
  metadata: {
    createdAt: datetime,
    updatedAt: datetime,
    createdBy: string
  }
}
```

## Secciones de actualización parcial

| Sección | Campos afectados | Endpoint asociado |
|---------|-----------------|-------------------|
| Datos generales | insuredData, conductionData, agentCode, businessType, riskClassification | PUT general-info |
| Layout | layoutConfiguration | PUT locations/layout |
| Ubicaciones | locations[] | PUT/PATCH locations |
| Coberturas | coverageOptions | PUT coverage-options |
| Resultado financiero | netPremium, commercialPremium, premiumsByLocation | POST calculate |

---

## Reglas de negocio

- `folioNumber` es inmutable después de la creación
- Cada operación de escritura DEBE usar `version` como condición de filtro (optimistic locking)
- Si la versión no coincide, responder HTTP 409 Conflict
- La escritura del resultado financiero NO debe sobreescribir otras secciones
- `metadata.updatedAt` se actualiza en toda escritura

---

## Criterios de aceptación

- El modelo soporta todas las secciones del dominio descritas en el contexto de negocio
- Las actualizaciones parciales solo modifican los campos de su sección
- El versionado optimista rechaza escrituras concurrentes con 409
- Toda escritura incrementa `version` y actualiza `metadata.updatedAt`
- Los índices de MongoDB están definidos sobre `folioNumber` (unique)

---

## Testabilidad

- **Unit tests**: Validar entidades de dominio, value objects, reglas de versión
- **Integration tests**: Verificar actualizaciones parciales en MongoDB, confirmar que un update con versión incorrecta falla
- **Desplegable de forma independiente**: No es desplegable sin backend, pero es testeable con unit tests de dominio
