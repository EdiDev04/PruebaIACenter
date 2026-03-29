# REQ-03: Creación y Apertura de Folio

## Oleada de despliegue: 2 — CRUD Base
## Dependencias: REQ-01 (generación de folio desde core), REQ-02 (persistencia)
## Prioridad: Crítica (punto de entrada para todo el flujo)

---

## Descripción

Implementar la creación de nuevos folios de cotización y la apertura de folios existentes. La creación debe ser idempotente: si se solicita crear un folio que ya existe, se retorna el existente sin duplicar. Este es el punto de entrada obligatorio para todo el flujo del cotizador.

---

## Historias de Usuario

**HU-03.1** — Como usuario del cotizador, quiero crear un nuevo folio para iniciar una cotización de seguro de daños.

**HU-03.2** — Como usuario del cotizador, quiero abrir un folio existente por su número para retomar una cotización en progreso.

**HU-03.3** — Como sistema, quiero que la creación de folio sea idempotente para evitar duplicados ante reintentos del cliente.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/v1/folios` | Crear nuevo folio (idempotente). Consume `GET /v1/folios` del core-mock para obtener el siguiente número secuencial |

---

## Flujo Backend

1. Recibir solicitud de creación
2. Llamar a `core-mock GET /v1/folios` para obtener siguiente `numeroFolio`
3. Verificar si ya existe en MongoDB (idempotencia)
4. Si no existe: crear documento con `quoteStatus: "draft"`, versión 1, metadatos iniciales
5. Retornar el folio creado (o existente)

## Flujo Frontend

1. Pantalla `/cotizador` muestra opciones: "Crear nuevo folio" o "Abrir folio existente"
2. Al crear: llamar POST `/v1/folios`, redirigir a `/quotes/{folio}/general-info`
3. Al abrir: solicitar número de folio, validar existencia, redirigir al flujo

---

## Reglas de negocio

- El formato del folio es `DAN-YYYY-NNNNN` (generado por core-mock)
- Un folio nuevo se crea con `quoteStatus: "draft"` y `version: 1`
- Si el folio ya existe, retornar HTTP 200 con el documento existente (no 409)
- El campo `metadata.createdAt` se fija en la creación y nunca cambia

---

## Criterios de aceptación

```gherkin
Dado que no existe un folio en el sistema
Cuando solicito crear un nuevo folio
Entonces el sistema genera un número secuencial único
Y crea el documento con quoteStatus "draft" y versión 1
Y retorna HTTP 201 con el folio creado

Dado que ya existe un folio con el número generado
Cuando solicito crear con el mismo número
Entonces el sistema retorna HTTP 200 con el folio existente
Y no crea un duplicado

Dado que soy usuario en la pantalla del cotizador
Cuando ingreso un número de folio existente y presiono "Abrir"
Entonces el sistema me redirige al flujo de cotización de ese folio
```

---

## Testabilidad

- **Unit tests**: Lógica de idempotencia, generación de documento inicial
- **Integration tests**: Flujo completo POST → verificar en MongoDB → reintentar y confirmar idempotencia
- **E2E tests**: Crear folio desde la UI, verificar redirección
- **Desplegable**: Sí — primera funcionalidad end-to-end visible
