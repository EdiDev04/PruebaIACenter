namespace Cotizador.Application.DTOs;

public record UpdateLayoutRequest(
    string DisplayMode,
    List<string> VisibleColumns,
    int Version
);
