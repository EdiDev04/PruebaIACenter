# REQ-05: Configuración del Layout de Ubicaciones

## Oleada de despliegue: 3 — Gestión de Ubicaciones
## Dependencias: REQ-03 (folio existente)
## Prioridad: Media

---

## Descripción

Implementar la consulta y guardado de la configuración del layout de ubicaciones. El layout define cómo se presenta y organiza la grilla de ubicaciones de riesgo dentro de un folio. Esta configuración persiste como sección independiente de la cotización.

---

## Historias de Usuario

**HU-05.1** — Como usuario del cotizador, quiero configurar el layout de la grilla de ubicaciones para organizar visualmente las propiedades del folio.

**HU-05.2** — Como usuario del cotizador, quiero que la configuración del layout se guarde y persista entre sesiones para no tener que reconfigurar cada vez.

---

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/v1/quotes/{folio}/locations/layout` | Consultar configuración de layout |
| PUT | `/v1/quotes/{folio}/locations/layout` | Guardar configuración de layout |

---

## Flujo Backend

1. GET: Retornar `layoutConfiguration` del documento de cotización
2. PUT: Actualizar solo el campo `layoutConfiguration` (parcial), incrementar versión

## Flujo Frontend

1. Integrado en la pantalla de ubicaciones `/quotes/{folio}/locations`
2. Permite configurar columnas visibles, orden, agrupación u opciones de visualización
3. La configuración se persiste al cambiar

---

## Reglas de negocio

- El layout es una sección independiente — su escritura no afecta ubicaciones ni otros datos
- El PUT requiere `version` actual para versionado optimista
- Si el layout no ha sido configurado, se retorna un layout por defecto

---

## Criterios de aceptación

```gherkin
Dado que tengo un folio sin configuración de layout
Cuando consulto el layout
Entonces el sistema retorna una configuración por defecto

Dado que modifico la configuración del layout
Cuando guardo los cambios
Entonces el sistema persiste solo la sección de layout
Y incrementa la versión de la cotización

Dado que otro usuario modificó la cotización después de mi última lectura
Cuando intento guardar el layout con una versión desactualizada
Entonces el sistema responde HTTP 409 Conflict
```

---

## Testabilidad

- **Unit tests**: Validar layout por defecto, lógica de merge/replace
- **Integration tests**: PUT layout → GET → verificar aislamiento de sección
- **Desplegable**: Sí — funcionalidad aislada, testeable independientemente
