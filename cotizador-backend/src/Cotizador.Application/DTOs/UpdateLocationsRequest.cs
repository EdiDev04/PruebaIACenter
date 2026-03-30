namespace Cotizador.Application.DTOs;

public record UpdateLocationsRequest(
    List<LocationDto> Locations,
    int Version);
