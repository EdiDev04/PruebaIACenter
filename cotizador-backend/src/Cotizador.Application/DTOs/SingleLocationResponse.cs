namespace Cotizador.Application.DTOs;

public record SingleLocationResponse(
    int Index,
    string LocationName,
    string Address,
    string ZipCode,
    string State,
    string Municipality,
    string Neighborhood,
    string City,
    string ConstructionType,
    int Level,
    int ConstructionYear,
    BusinessLineDto? LocationBusinessLine,
    List<LocationGuaranteeDto> Guarantees,
    string CatZone,
    List<string> BlockingAlerts,
    string ValidationStatus,
    int Version);
