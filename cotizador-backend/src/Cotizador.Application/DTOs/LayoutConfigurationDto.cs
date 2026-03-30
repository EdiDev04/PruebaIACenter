namespace Cotizador.Application.DTOs;

public record LayoutConfigurationDto(
    string DisplayMode,
    List<string> VisibleColumns,
    int Version
);
