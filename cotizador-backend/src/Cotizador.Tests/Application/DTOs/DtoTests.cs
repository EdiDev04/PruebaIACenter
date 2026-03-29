using Cotizador.Application.DTOs;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Application.DTOs;

public class DtoTests
{
    [Fact]
    public void SubscriberDto_Should_HoldAllProperties()
    {
        // Arrange & Act
        SubscriberDto dto = new("SUB-001", "María González", "CDMX Central", true);

        // Assert
        dto.Code.Should().Be("SUB-001");
        dto.Name.Should().Be("María González");
        dto.Office.Should().Be("CDMX Central");
        dto.Active.Should().BeTrue();
    }

    [Fact]
    public void FolioDto_Should_HoldFolioNumber()
    {
        // Arrange & Act
        FolioDto dto = new("DAN-2026-00001");

        // Assert
        dto.FolioNumber.Should().Be("DAN-2026-00001");
        dto.FolioNumber.Should().MatchRegex(@"^DAN-\d{4}-\d{5}$");
    }

    [Fact]
    public void CalculationParametersDto_Should_HoldAllFinancialFactors()
    {
        // Arrange & Act
        CalculationParametersDto dto = new(
            ExpeditionExpenses: 0.05m,
            AgentCommission: 0.10m,
            IssuingRights: 0.03m,
            Iva: 0.16m,
            Surcharges: 0.02m,
            EffectiveDate: "2026-01-01");

        // Assert
        dto.ExpeditionExpenses.Should().Be(0.05m);
        dto.AgentCommission.Should().Be(0.10m);
        dto.IssuingRights.Should().Be(0.03m);
        dto.Iva.Should().Be(0.16m);
        dto.Surcharges.Should().Be(0.02m);
        dto.EffectiveDate.Should().Be("2026-01-01");
    }

    [Fact]
    public void ZipCodeValidationDto_Should_IndicateValid_WhenZipCodeExists()
    {
        // Arrange & Act
        ZipCodeValidationDto dto = new(Valid: true, ZipCode: "06600");

        // Assert
        dto.Valid.Should().BeTrue();
        dto.ZipCode.Should().Be("06600");
    }
}
