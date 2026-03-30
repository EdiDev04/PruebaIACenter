namespace Cotizador.Application.DTOs;

public record LocationsResponse(
    List<LocationDto> Locations,
    int Version);
