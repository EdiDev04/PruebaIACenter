using Cotizador.Application.DTOs;
using Cotizador.Application.UseCases;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Application.UseCases;

public class LocationMapperTests
{
    // ─── Builders ─────────────────────────────────────────────────────────────

    private static Location BuildFullLocation(int index = 1) =>
        new()
        {
            Index = index,
            LocationName = "Bodega Principal",
            Address = "Av. Industria 340",
            ZipCode = "06600",
            State = "Ciudad de México",
            Municipality = "Cuauhtémoc",
            Neighborhood = "Doctores",
            City = "Ciudad de México",
            ConstructionType = "Tipo 1 - Macizo",
            Level = 2,
            ConstructionYear = 1998,
            CatZone = "A",
            ValidationStatus = ValidationStatus.Calculable,
            BlockingAlerts = new List<string>(),
            BusinessLine = new BusinessLine { Description = "Storage warehouse", FireKey = "B-03" },
            Guarantees = new List<LocationGuarantee>
            {
                new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 5_000_000m },
                new() { GuaranteeKey = GuaranteeKeys.CatTev, InsuredAmount = 3_000_000m }
            }
        };

    private static LocationDto BuildFullLocationDto(int index = 1) =>
        new(
            Index: index,
            LocationName: "Bodega Norte",
            Address: "Calle Real 100",
            ZipCode: "64000",
            State: "Nuevo León",
            Municipality: "Monterrey",
            Neighborhood: "Centro",
            City: "Monterrey",
            ConstructionType: "Tipo 2",
            Level: 3,
            ConstructionYear: 2005,
            LocationBusinessLine: new BusinessLineDto("BL-002", "Office", "A-01", "bajo"),
            Guarantees: new List<LocationGuaranteeDto>
            {
                new(GuaranteeKeys.BuildingFire, 4_000_000m),
                new(GuaranteeKeys.Glass, 0m)
            },
            CatZone: "B",
            BlockingAlerts: new List<string>(),
            ValidationStatus: ValidationStatus.Calculable);

    // ─── ToDto ────────────────────────────────────────────────────────────────

    [Fact]
    public void ToDto_Should_MapAllScalarFields()
    {
        // Arrange
        var location = BuildFullLocation(index: 3);

        // Act
        var dto = LocationMapper.ToDto(location);

        // Assert
        dto.Index.Should().Be(3);
        dto.LocationName.Should().Be("Bodega Principal");
        dto.Address.Should().Be("Av. Industria 340");
        dto.ZipCode.Should().Be("06600");
        dto.State.Should().Be("Ciudad de México");
        dto.Municipality.Should().Be("Cuauhtémoc");
        dto.Neighborhood.Should().Be("Doctores");
        dto.City.Should().Be("Ciudad de México");
        dto.ConstructionType.Should().Be("Tipo 1 - Macizo");
        dto.Level.Should().Be(2);
        dto.ConstructionYear.Should().Be(1998);
        dto.CatZone.Should().Be("A");
    }

    [Fact]
    public void ToDto_Should_MapValidationStatusAndBlockingAlerts()
    {
        // Arrange
        var location = BuildFullLocation();
        location.ValidationStatus = ValidationStatus.Incomplete;
        location.BlockingAlerts = new List<string> { "Código postal requerido", "Giro comercial requerido" };

        // Act
        var dto = LocationMapper.ToDto(location);

        // Assert
        dto.ValidationStatus.Should().Be(ValidationStatus.Incomplete);
        dto.BlockingAlerts.Should().HaveCount(2);
        dto.BlockingAlerts.Should().Contain("Código postal requerido");
    }

    [Fact]
    public void ToDto_Should_MapBusinessLine_WhenBusinessLineIsSet()
    {
        // Arrange
        var location = BuildFullLocation();

        // Act
        var dto = LocationMapper.ToDto(location);

        // Assert
        dto.LocationBusinessLine.Should().NotBeNull();
        dto.LocationBusinessLine!.Description.Should().Be("Storage warehouse");
        dto.LocationBusinessLine.FireKey.Should().Be("B-03");
    }

    [Fact]
    public void ToDto_Should_ReturnNullBusinessLine_WhenBothDescriptionAndFireKeyAreEmpty()
    {
        // Arrange
        var location = BuildFullLocation();
        location.BusinessLine = new BusinessLine { Description = string.Empty, FireKey = string.Empty };

        // Act
        var dto = LocationMapper.ToDto(location);

        // Assert
        dto.LocationBusinessLine.Should().BeNull();
    }

    [Fact]
    public void ToDto_Should_MapAllGuaranteesWithInsuredAmounts()
    {
        // Arrange
        var location = BuildFullLocation();

        // Act
        var dto = LocationMapper.ToDto(location);

        // Assert
        dto.Guarantees.Should().HaveCount(2);
        dto.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.BuildingFire && g.InsuredAmount == 5_000_000m);
        dto.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.CatTev && g.InsuredAmount == 3_000_000m);
    }

    [Fact]
    public void ToDto_Should_MapEmptyGuaranteesAsEmptyList()
    {
        // Arrange
        var location = BuildFullLocation();
        location.Guarantees = new List<LocationGuarantee>();

        // Act
        var dto = LocationMapper.ToDto(location);

        // Assert
        dto.Guarantees.Should().NotBeNull().And.BeEmpty();
    }

    // ─── ToEntity ─────────────────────────────────────────────────────────────

    [Fact]
    public void ToEntity_Should_MapAllScalarFields()
    {
        // Arrange
        var dto = BuildFullLocationDto(index: 2);

        // Act
        var entity = LocationMapper.ToEntity(dto);

        // Assert
        entity.Index.Should().Be(2);
        entity.LocationName.Should().Be("Bodega Norte");
        entity.Address.Should().Be("Calle Real 100");
        entity.ZipCode.Should().Be("64000");
        entity.State.Should().Be("Nuevo León");
        entity.Municipality.Should().Be("Monterrey");
        entity.Neighborhood.Should().Be("Centro");
        entity.City.Should().Be("Monterrey");
        entity.ConstructionType.Should().Be("Tipo 2");
        entity.Level.Should().Be(3);
        entity.ConstructionYear.Should().Be(2005);
        entity.CatZone.Should().Be("B");
    }

    [Fact]
    public void ToEntity_Should_MapBusinessLineFromDto()
    {
        // Arrange
        var dto = BuildFullLocationDto();

        // Act
        var entity = LocationMapper.ToEntity(dto);

        // Assert
        entity.BusinessLine.Should().NotBeNull();
        entity.BusinessLine.Description.Should().Be("Office");
        entity.BusinessLine.FireKey.Should().Be("A-01");
    }

    [Fact]
    public void ToEntity_Should_CreateEmptyBusinessLine_WhenDtoBusinessLineIsNull()
    {
        // Arrange
        var dto = BuildFullLocationDto() with { LocationBusinessLine = null };

        // Act
        var entity = LocationMapper.ToEntity(dto);

        // Assert
        entity.BusinessLine.Should().NotBeNull();
        entity.BusinessLine.Description.Should().BeEmpty();
        entity.BusinessLine.FireKey.Should().BeEmpty();
    }

    [Fact]
    public void ToEntity_Should_MapAllGuaranteesWithInsuredAmounts()
    {
        // Arrange
        var dto = BuildFullLocationDto();

        // Act
        var entity = LocationMapper.ToEntity(dto);

        // Assert
        entity.Guarantees.Should().HaveCount(2);
        entity.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.BuildingFire && g.InsuredAmount == 4_000_000m);
        entity.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.Glass && g.InsuredAmount == 0m);
    }

    // ─── ToSingleResponse ─────────────────────────────────────────────────────

    [Fact]
    public void ToSingleResponse_Should_MapAllFieldsIncludingVersion()
    {
        // Arrange
        var location = BuildFullLocation(index: 1);
        const int version = 7;

        // Act
        var response = LocationMapper.ToSingleResponse(location, version);

        // Assert
        response.Index.Should().Be(1);
        response.LocationName.Should().Be("Bodega Principal");
        response.Address.Should().Be("Av. Industria 340");
        response.ZipCode.Should().Be("06600");
        response.CatZone.Should().Be("A");
        response.ValidationStatus.Should().Be(ValidationStatus.Calculable);
        response.Version.Should().Be(version);
    }

    [Fact]
    public void ToSingleResponse_Should_MapBusinessLine_WhenSet()
    {
        // Arrange
        var location = BuildFullLocation();
        const int version = 3;

        // Act
        var response = LocationMapper.ToSingleResponse(location, version);

        // Assert
        response.LocationBusinessLine.Should().NotBeNull();
        response.LocationBusinessLine!.FireKey.Should().Be("B-03");
    }

    [Fact]
    public void ToSingleResponse_Should_ReturnNullBusinessLine_WhenEmptyBusinessLine()
    {
        // Arrange
        var location = BuildFullLocation();
        location.BusinessLine = new BusinessLine { Description = string.Empty, FireKey = string.Empty };

        // Act
        var response = LocationMapper.ToSingleResponse(location, version: 1);

        // Assert
        response.LocationBusinessLine.Should().BeNull();
    }

    [Fact]
    public void ToSingleResponse_Should_MapGuarantees()
    {
        // Arrange
        var location = BuildFullLocation();

        // Act
        var response = LocationMapper.ToSingleResponse(location, version: 1);

        // Assert
        response.Guarantees.Should().HaveCount(2);
        response.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.BuildingFire);
    }
}
