using Cotizador.Application.DTOs;
using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;

namespace Cotizador.Application.UseCases;

public static class LocationMapper
{
    /// <summary>Mapea una entidad de dominio Location a su DTO de respuesta.</summary>
    public static LocationDto ToDto(Location location) =>
        new(
            location.Index,
            location.LocationName,
            location.Address,
            location.ZipCode,
            location.State,
            location.Municipality,
            location.Neighborhood,
            location.City,
            location.ConstructionType,
            location.Level,
            location.ConstructionYear,
            string.IsNullOrEmpty(location.BusinessLine.Description) && string.IsNullOrEmpty(location.BusinessLine.FireKey)
                ? null
                : new BusinessLineDto(string.Empty, location.BusinessLine.Description, location.BusinessLine.FireKey, string.Empty),
            location.Guarantees
                .Select(g => new LocationGuaranteeDto(g.GuaranteeKey, g.InsuredAmount))
                .ToList(),
            location.CatZone,
            location.BlockingAlerts,
            location.ValidationStatus);

    /// <summary>Mapea un LocationDto de request a una entidad de dominio Location.</summary>
    public static Location ToEntity(LocationDto dto) =>
        new()
        {
            Index = dto.Index,
            LocationName = dto.LocationName,
            Address = dto.Address,
            ZipCode = dto.ZipCode,
            State = dto.State,
            Municipality = dto.Municipality,
            Neighborhood = dto.Neighborhood,
            City = dto.City,
            ConstructionType = dto.ConstructionType,
            Level = dto.Level,
            ConstructionYear = dto.ConstructionYear,
            BusinessLine = dto.LocationBusinessLine is not null
                ? new BusinessLine
                  {
                      Description = dto.LocationBusinessLine.Description,
                      FireKey = dto.LocationBusinessLine.FireKey
                  }
                : new BusinessLine(),
            Guarantees = dto.Guarantees
                .Select(g => new LocationGuarantee { GuaranteeKey = g.GuaranteeKey, InsuredAmount = g.InsuredAmount })
                .ToList(),
            CatZone = dto.CatZone
        };

    /// <summary>Mapea una entidad Location y su versión a SingleLocationResponse (respuesta de PATCH).</summary>
    public static SingleLocationResponse ToSingleResponse(Location location, int version) =>
        new(
            location.Index,
            location.LocationName,
            location.Address,
            location.ZipCode,
            location.State,
            location.Municipality,
            location.Neighborhood,
            location.City,
            location.ConstructionType,
            location.Level,
            location.ConstructionYear,
            string.IsNullOrEmpty(location.BusinessLine.Description) && string.IsNullOrEmpty(location.BusinessLine.FireKey)
                ? null
                : new BusinessLineDto(string.Empty, location.BusinessLine.Description, location.BusinessLine.FireKey, string.Empty),
            location.Guarantees
                .Select(g => new LocationGuaranteeDto(g.GuaranteeKey, g.InsuredAmount))
                .ToList(),
            location.CatZone,
            location.BlockingAlerts,
            location.ValidationStatus,
            version);
}
