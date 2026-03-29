# REQ-WIZARD-SESSION: Persistencia de sesión del wizard de cotización

### Descripción: 

Como usuario del cotizador, quiero que al recargar la página o navegar de vuelta a un folio en proceso, el wizard retome en el último paso guardado y recupere los datos de formulario no guardados, para no perder mi progreso de trabajo.

### Criterios de aceptación:

Al abrir un folio existente, el wizard se posiciona en el paso correspondiente a la última sección guardada exitosamente (metadatos.ultimoPasoWizard).
Si el usuario recarga la página con un formulario parcialmente llenado (sin guardar), los valores del formulario se restauran desde sessionStorage.
Tras un guardado exitoso de cualquier sección, el buffer de sessionStorage para ese paso se limpia.
El campo metadatos.ultimoPasoWizard se actualiza automáticamente en cada operación de escritura del backend sin necesidad de un endpoint adicional.
Al cerrar la pestaña y reabrir, el wizard se posiciona en el último paso guardado (los datos de formulario no guardados se pierden — esto es aceptable).