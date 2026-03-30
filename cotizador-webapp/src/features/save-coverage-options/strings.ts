export const SAVE_COVERAGE_OPTIONS_STRINGS = {
  dialogTitle: 'Confirmar deshabilitación',
  dialogCancel: 'Cancelar',
  dialogConfirm: 'Deshabilitar',
  dialogMessage: (name: string, count: number) =>
    `La cobertura "${name}" está seleccionada en ${count} ubicación(es). Si la deshabilitas, esas ubicaciones quedarán marcadas como incompletas al calcular.`,
  toastSaved: 'Opciones de cobertura guardadas correctamente',
  errorGeneric: 'Error al guardar las opciones. Intenta de nuevo.',
  errorVersionConflict:
    'El folio fue modificado por otro proceso. Recarga la página para ver los datos actualizados.',
  errorCatalogUnavailable:
    'No se pudo cargar el catálogo de garantías. El servicio no está disponible.',
  errorFolioNotFound: 'El folio no existe',
  btnReload: 'Recargar',
};
