# REQ-10: Visualización de Resultados y Alertas

## Oleada de despliegue: 4 — Cálculo y Resultados
## Dependencias: REQ-09 (cálculo ejecutado), REQ-08 (estado y progreso)
## Prioridad: Alta (entregable visible del reto)

---

## Descripción

Implementar la visualización de los resultados del cálculo de primas en el frontend: prima neta total, prima comercial total, desglose por ubicación y desglose por cobertura. Además, mostrar alertas de ubicaciones incompletas de forma clara sin bloquear el flujo. Esta pantalla es el entregable visual principal del cotizador.

---

## Historias de Usuario

**HU-10.1** — Como usuario del cotizador, quiero ver la prima neta total y la prima comercial total de mi cotización después de ejecutar el cálculo.

**HU-10.2** — Como usuario del cotizador, quiero ver el desglose de prima por cada ubicación calculada para entender la contribución de cada propiedad.

**HU-10.3** — Como usuario del cotizador, quiero ver el desglose por cobertura dentro de cada ubicación para entender qué garantías generan más costo.

**HU-10.4** — Como usuario del cotizador, quiero ver las alertas de ubicaciones incompletas con los campos faltantes para poder completarlas y recalcular.

**HU-10.5** — Como usuario del cotizador, quiero que las alertas de ubicaciones incompletas no bloqueen la visualización de resultados de las ubicaciones calculables.

---

## Pantallas y Rutas

| Ruta | Contenido |
|------|-----------|
| `/quotes/{folio}/technical-info` | Resultados del cálculo: primas, desgloses, alertas |

> Nota: La ruta exacta puede ajustarse en la spec técnica. El reto define `/quotes/{folio}/technical-info` como la pantalla de información técnica donde se muestran resultados.

---

## Elementos de UI requeridos

### Resumen financiero (tarjetas o panel principal)
- **Prima Neta Total**: monto consolidado de todas las ubicaciones calculables
- **Prima Comercial Total**: monto con gastos y comisión aplicados
- **Número de ubicaciones calculadas** vs total de ubicaciones

### Desglose por ubicación (tabla o acordeón)
- Nombre de ubicación
- Prima neta de la ubicación
- Estado de validación (calculable / incompleta)
- Expandible: desglose por cobertura

### Desglose por cobertura (dentro de cada ubicación)
- Nombre de la garantía
- Prima calculada para esa cobertura

### Panel de alertas
- Lista de ubicaciones incompletas
- Campos faltantes por cada una
- Acción: enlace para editar la ubicación incompleta
- Estilo visual: informativo (warning), no bloqueante (no error critical)

---

## Flujo Frontend

1. Después de ejecutar cálculo (REQ-09), redirigir o actualizar vista de resultados
2. Consumir los datos ya persistidos en la cotización (GET quote state + datos financieros)
3. Renderizar resumen, desgloses y alertas
4. Botón "Recalcular" para re-ejecutar si se modificaron ubicaciones
5. Enlace "Editar ubicación" en alertas → navegar a `/quotes/{folio}/locations`

---

## Reglas de negocio

- Solo mostrar resultados si el folio ha sido calculado (`quoteStatus: "calculated"`)
- Si no se ha calculado, mostrar mensaje invitando a ejecutar el cálculo
- Las ubicaciones incompletas aparecen en la sección de alertas con detalle de campos faltantes
- Los montos se muestran formateados como moneda (MXN, 2 decimales)
- El desglose por cobertura debe ser trazable: el usuario puede verificar qué tarifa se aplicó

---

## Criterios de aceptación

```gherkin
Dado que ejecuté el cálculo con 2 ubicaciones calculables
Cuando veo la pantalla de resultados
Entonces veo la prima neta total y prima comercial total
Y veo una tabla con el desglose por ubicación (nombre, prima neta, estado)
Y puedo expandir cada ubicación para ver el desglose por cobertura

Dado que hay 1 ubicación incompleta
Cuando veo la pantalla de resultados
Entonces veo un panel de alertas con la ubicación incompleta
Y la alerta indica los campos faltantes
Y puedo hacer clic para ir a editar esa ubicación
Y los resultados de las ubicaciones calculables se muestran normalmente

Dado que el folio no ha sido calculado
Cuando navego a la pantalla de resultados
Entonces veo un mensaje indicando que debe ejecutarse el cálculo
Y un botón para ejecutar el cálculo
```

---

## Testabilidad

- **Unit tests**: Componentes de UI: renderizado de tablas, formato de moneda, panel de alertas
- **Integration tests**: Verificar que la pantalla consume correctamente los datos del backend
- **E2E tests**: Flujo completo: crear folio → ubicaciones → calcular → verificar resultados en pantalla
- **Desplegable**: Sí — completa el flujo end-to-end del cotizador
