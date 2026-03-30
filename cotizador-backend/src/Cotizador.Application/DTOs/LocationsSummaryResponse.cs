namespace Cotizador.Application.DTOs;

public record LocationsSummaryResponse(
    List<LocationSummaryDto> Locations,
    int TotalCalculable,
    int TotalIncomplete,
    int Version);
