using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain;

public class LocationTests
{
    [Fact]
    public void Location_Should_HaveIncompleteValidationStatus_WhenCreated()
    {
        // Arrange & Act
        Location location = new();

        // Assert
        location.ValidationStatus.Should().Be(ValidationStatus.Incomplete);
    }

    [Fact]
    public void Location_Should_HaveEmptyGuaranteesList_WhenCreated()
    {
        // Arrange & Act
        Location location = new();

        // Assert
        location.Guarantees.Should().NotBeNull();
        location.Guarantees.Should().BeEmpty();
    }

    [Fact]
    public void Location_Should_HaveEmptyBlockingAlerts_WhenCreated()
    {
        // Arrange & Act
        Location location = new();

        // Assert
        location.BlockingAlerts.Should().NotBeNull();
        location.BlockingAlerts.Should().BeEmpty();
    }

    [Fact]
    public void Location_Should_AcceptGuaranteeKeys_WhenAssigned()
    {
        // Arrange
        Location location = new();
        List<LocationGuarantee> guarantees = new()
        {
            new LocationGuarantee { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 0 },
            new LocationGuarantee { GuaranteeKey = GuaranteeKeys.CatTev, InsuredAmount = 0 }
        };

        // Act
        location.Guarantees = guarantees;

        // Assert
        location.Guarantees.Should().HaveCount(2);
        location.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.BuildingFire);
        location.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.CatTev);
    }

    [Fact]
    public void Location_Should_AcceptBusinessLine_WhenAssigned()
    {
        // Arrange
        Location location = new();
        BusinessLine businessLine = new() { Description = "Storage warehouse", FireKey = "B-03" };

        // Act
        location.BusinessLine = businessLine;

        // Assert
        location.BusinessLine.FireKey.Should().Be("B-03");
        location.BusinessLine.Description.Should().Be("Storage warehouse");
    }

    // ─── SPEC-006: Physical fields, calculability inputs (RN-006-01 / RN-006-02) ──

    [Fact]
    public void Location_Should_HaveEmptyDefaultValues_ForAllGeoAndPhysicalFields()
    {
        // Arrange & Act
        Location location = new();

        // Assert — all geo / physical fields must default to empty string or 0
        location.ZipCode.Should().BeEmpty();
        location.State.Should().BeEmpty();
        location.Municipality.Should().BeEmpty();
        location.Neighborhood.Should().BeEmpty();
        location.City.Should().BeEmpty();
        location.CatZone.Should().BeEmpty();
        location.ConstructionType.Should().BeEmpty();
        location.Level.Should().Be(0);
        location.ConstructionYear.Should().Be(0);
        location.Address.Should().BeEmpty();
        location.LocationName.Should().BeEmpty();
    }

    [Fact]
    public void Location_Should_AcceptAllPhysicalFields_WhenAssigned()
    {
        // Arrange
        Location location = new();

        // Act
        location.Index = 3;
        location.LocationName = "Bodega Principal";
        location.Address = "Av. Industria 340";
        location.ZipCode = "06600";
        location.State = "Ciudad de México";
        location.Municipality = "Cuauhtémoc";
        location.Neighborhood = "Doctores";
        location.City = "Ciudad de México";
        location.ConstructionType = "Tipo 1 - Macizo";
        location.Level = 2;
        location.ConstructionYear = 1998;
        location.CatZone = "A";

        // Assert
        location.Index.Should().Be(3);
        location.LocationName.Should().Be("Bodega Principal");
        location.Address.Should().Be("Av. Industria 340");
        location.ZipCode.Should().Be("06600");
        location.State.Should().Be("Ciudad de México");
        location.Municipality.Should().Be("Cuauhtémoc");
        location.Neighborhood.Should().Be("Doctores");
        location.City.Should().Be("Ciudad de México");
        location.ConstructionType.Should().Be("Tipo 1 - Macizo");
        location.Level.Should().Be(2);
        location.ConstructionYear.Should().Be(1998);
        location.CatZone.Should().Be("A");
    }

    [Fact]
    public void Location_Should_AcceptGuaranteeWithInsuredAmount_WhenAssigned()
    {
        // Arrange
        Location location = new();
        var guarantee = new LocationGuarantee
        {
            GuaranteeKey = GuaranteeKeys.BuildingFire,
            InsuredAmount = 5_000_000m
        };

        // Act
        location.Guarantees = new List<LocationGuarantee> { guarantee };

        // Assert
        location.Guarantees.Should().HaveCount(1);
        location.Guarantees[0].InsuredAmount.Should().Be(5_000_000m);
        location.Guarantees[0].GuaranteeKey.Should().Be(GuaranteeKeys.BuildingFire);
    }

    [Fact]
    public void Location_Should_AcceptZeroInsuredAmount_WhenAssigned()
    {
        // Arrange — glass and illuminated_signs accept 0 (RN-006-13)
        Location location = new();
        var glassGuarantee = new LocationGuarantee
        {
            GuaranteeKey = GuaranteeKeys.Glass,
            InsuredAmount = 0m
        };

        // Act
        location.Guarantees = new List<LocationGuarantee> { glassGuarantee };

        // Assert
        location.Guarantees[0].InsuredAmount.Should().Be(0m);
    }

    [Fact]
    public void Location_Should_AllowBlockingAlertsToBeSet_WhenAssigned()
    {
        // Arrange
        Location location = new();
        var alerts = new List<string> { "Código postal requerido", "Giro comercial requerido" };

        // Act
        location.BlockingAlerts = alerts;

        // Assert
        location.BlockingAlerts.Should().HaveCount(2);
        location.BlockingAlerts.Should().Contain("Código postal requerido");
        location.BlockingAlerts.Should().Contain("Giro comercial requerido");
    }

    [Fact]
    public void Location_Should_AllowValidationStatusToBeSetToCalculable_WhenAssigned()
    {
        // Arrange
        Location location = new();

        // Act
        location.ValidationStatus = ValidationStatus.Calculable;

        // Assert
        location.ValidationStatus.Should().Be(ValidationStatus.Calculable);
    }

    [Fact]
    public void Location_Should_AcceptMultipleGuarantees_WithMixedInsuredAmounts()
    {
        // Arrange
        Location location = new();
        var guarantees = new List<LocationGuarantee>
        {
            new() { GuaranteeKey = GuaranteeKeys.BuildingFire, InsuredAmount = 5_000_000m },
            new() { GuaranteeKey = GuaranteeKeys.CatTev, InsuredAmount = 3_000_000m },
            new() { GuaranteeKey = GuaranteeKeys.Glass, InsuredAmount = 0m }
        };

        // Act
        location.Guarantees = guarantees;

        // Assert
        location.Guarantees.Should().HaveCount(3);
        location.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.BuildingFire && g.InsuredAmount == 5_000_000m);
        location.Guarantees.Should().Contain(g => g.GuaranteeKey == GuaranteeKeys.Glass && g.InsuredAmount == 0m);
    }
}
