namespace Cotizador.Application.DTOs;

public record PatchLocationRequest(
    string? LocationName,
    string? Address,
    string? ZipCode,
    string? State,
    string? Municipality,
    string? Neighborhood,
    string? City,
    string? ConstructionType,
    int? Level,
    int? ConstructionYear,
    BusinessLineDto? LocationBusinessLine,
    List<LocationGuaranteeDto>? Guarantees,
    string? CatZone,
    int Version);
