# REQ-04: Gestión de Datos Generales de Cotización

## Oleada de despliegue: 2 — CRUD Base
## Dependencias: REQ-03 (folio existente), REQ-01 (catálogos de suscriptores, agentes)
## Prioridad: Alta

---

## Descripción

Implementar la consulta y guardado de los datos generales de una cotización: datos del asegurado, datos de conducción (suscriptor, oficina), agente asociado, tipo de negocio y clasificación de riesgo. Los catálogos de suscriptores, agentes y clasificación de riesgo se consultan desde el servicio core-mock.

---

## Historias de Usuario

**HU-04.1** — Como usuario del cotizador, quiero capturar los datos del asegurado (`insuredData`: name, taxId) para identificar al cliente de la póliza.

**HU-04.2** — Como usuario del cotizador, quiero seleccionar un suscriptor del catálogo para asignar el underwriter responsable.

**HU-04.3** — Como usuario del cotizador, quiero buscar y seleccionar un agente por clave para asociarlo a la cotización.

**HU-04.4** — Como usuario del cotizador, quiero seleccionar el tipo de negocio y la clasificación de riesgo desde catálogos predefinidos.

**HU-04.5** — Como usuario del cotizador, quiero guardar los datos generales y que el sistema actualice la versión de la cotización.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/v1/quotes/{folio}/general-info` | Consultar datos generales de la cotización |
| PUT | `/v1/quotes/{folio}/general-info` | Guardar/actualizar datos generales |

### Catálogos consumidos del core-mock

| Endpoint core | Uso |
|---------------|-----|
| `GET /v1/subscribers` | Poblar selector de suscriptores |
| `GET /v1/agents` | Buscar agente por clave |
| `GET /v1/catalogs/risk-classification` | Poblar selector de clasificación de riesgo |

---

## Flujo Frontend

1. Ruta: `/quotes/{folio}/general-info`
2. Cargar datos existentes (GET general-info)
3. Cargar catálogos desde core-mock (suscriptores, agentes, clasificación de riesgo)
4. Formulario con validación local (React Hook Form + Zod)
5. Al guardar: PUT general-info con versión actual → actualización parcial

---

## Reglas de negocio

- Los datos generales se guardan como actualización parcial — no afectan ubicaciones ni coberturas
- El PUT requiere enviar la `version` actual; si no coincide → HTTP 409
- El guardado incrementa `version` y actualiza `metadata.updatedAt`
- El suscriptor y agente deben existir en los catálogos del core

---

## Criterios de aceptación

```gherkin
Dado que tengo un folio existente
Cuando consulto los datos generales
Entonces el sistema retorna los datos actuales o vacíos si no se han capturado

Dado que capturo nombre del asegurado, taxId, suscriptor y agente
Cuando guardo los datos generales
Entonces el sistema persiste solo la sección de datos generales
Y incrementa la versión de la cotización
Y actualiza la fecha de última modificación

Dado que intento guardar con una versión desactualizada
Cuando envío el PUT con versión incorrecta
Entonces el sistema responde HTTP 409 Conflict
```

---

## Testabilidad

- **Unit tests**: Validación de campos obligatorios, lógica de actualización parcial
- **Integration tests**: PUT → GET → verificar que solo cambió la sección general
- **E2E tests**: Llenar formulario, guardar, recargar y verificar persistencia
- **Desplegable**: Sí — junto con REQ-03 forma el primer flujo CRUD completo
