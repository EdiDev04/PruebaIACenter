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
        List<string> guarantees = new() { GuaranteeKeys.BuildingFire, GuaranteeKeys.CatTev };

        // Act
        location.Guarantees = guarantees;

        // Assert
        location.Guarantees.Should().HaveCount(2);
        location.Guarantees.Should().Contain(GuaranteeKeys.BuildingFire);
        location.Guarantees.Should().Contain(GuaranteeKeys.CatTev);
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
}
