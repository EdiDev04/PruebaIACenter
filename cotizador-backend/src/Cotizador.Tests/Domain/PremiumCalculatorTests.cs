using Cotizador.Domain.Constants;
using Cotizador.Domain.Services;
using Cotizador.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain;

public class PremiumCalculatorTests
{
    // ─── CalculateCoveragePremium ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public void CalculateCoveragePremium_Should_ReturnCorrectPremium_ForBuildingFire()
    {
        // Arrange
        const string guaranteeKey = GuaranteeKeys.BuildingFire;
        const decimal insuredAmount = 5_000_000m;
        const decimal rate = 0.00125m;

        // Act
        CoveragePremium result = PremiumCalculator.CalculateCoveragePremium(guaranteeKey, insuredAmount, rate);

        // Assert
        result.Premium.Should().Be(6_250m);
        result.Rate.Should().Be(rate);
        result.GuaranteeKey.Should().Be(guaranteeKey);
        result.InsuredAmount.Should().Be(insuredAmount);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void CalculateCoveragePremium_Should_ReturnCorrectPremium_ForContentsFire()
    {
        // Arrange
        const string guaranteeKey = GuaranteeKeys.ContentsFire;
        const decimal insuredAmount = 3_000_000m;
        const decimal rate = 0.00125m;

        // Act
        CoveragePremium result = PremiumCalculator.CalculateCoveragePremium(guaranteeKey, insuredAmount, rate);

        // Assert
        result.Premium.Should().Be(3_750m);
        result.Rate.Should().Be(rate);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void CalculateCoveragePremium_Should_ReturnFlatPremium_ForGlass()
    {
        // Arrange
        const string guaranteeKey = GuaranteeKeys.Glass;

        // Act
        CoveragePremium result = PremiumCalculator.CalculateCoveragePremium(guaranteeKey, insuredAmount: 0m, rate: 0m);

        // Assert
        result.Premium.Should().Be(SimplifiedTariffRates.FlatPremium);
        result.Rate.Should().Be(0m);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void CalculateCoveragePremium_Should_ReturnFlatPremium_ForIlluminatedSigns()
    {
        // Arrange
        const string guaranteeKey = GuaranteeKeys.IlluminatedSigns;

        // Act
        CoveragePremium result = PremiumCalculator.CalculateCoveragePremium(guaranteeKey, insuredAmount: 0m, rate: 0m);

        // Assert
        result.Premium.Should().Be(SimplifiedTariffRates.FlatPremium);
        result.Rate.Should().Be(0m);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void CalculateCoveragePremium_Should_UseSupplementaryRate_ForDebrisRemoval()
    {
        // Arrange
        const string guaranteeKey = GuaranteeKeys.DebrisRemoval;
        const decimal insuredAmount = 1_000_000m;
        decimal rate = SimplifiedTariffRates.SupplementaryRate;

        // Act
        CoveragePremium result = PremiumCalculator.CalculateCoveragePremium(guaranteeKey, insuredAmount, rate);

        // Assert
        result.Premium.Should().Be(1_000m);
        result.Rate.Should().Be(SimplifiedTariffRates.SupplementaryRate);
    }

    // ─── CalculateLocationNetPremium ──────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public void CalculateLocationNetPremium_Should_ReturnSumOfAllPremiums()
    {
        // Arrange
        var coveragePremiums = new List<CoveragePremium>
        {
            new() { GuaranteeKey = GuaranteeKeys.BuildingFire, Premium = 6_250m },
            new() { GuaranteeKey = GuaranteeKeys.ContentsFire, Premium = 3_750m },
            new() { GuaranteeKey = GuaranteeKeys.DebrisRemoval, Premium = 1_000m },
        };

        // Act
        decimal result = PremiumCalculator.CalculateLocationNetPremium(coveragePremiums);

        // Assert
        result.Should().Be(11_000m);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void CalculateLocationNetPremium_Should_ReturnZero_ForEmptyList()
    {
        // Arrange
        var coveragePremiums = new List<CoveragePremium>();

        // Act
        decimal result = PremiumCalculator.CalculateLocationNetPremium(coveragePremiums);

        // Assert
        result.Should().Be(0m);
    }

    // ─── CalculateCommercialPremium ───────────────────────────────────────────

    [Fact]
    [Trait("Category", "Smoke")]
    public void CalculateCommercialPremium_Should_CalculateCorrectly_WithDefaultParameters()
    {
        // Arrange
        const decimal netPremium = 100_000m;
        const decimal expeditionExpenses = 0.05m;
        const decimal agentCommission = 0.10m;
        const decimal issuingRights = 0.03m;
        const decimal surcharges = 0.02m;
        const decimal iva = 0.16m;
        // loadingFactor = 1 + 0.05 + 0.10 + 0.03 + 0.02 = 1.20
        // beforeTax = 100_000 × 1.20 = 120_000
        // withTax = 120_000 × 1.16 = 139_200

        // Act
        (decimal beforeTax, decimal withTax) = PremiumCalculator.CalculateCommercialPremium(
            netPremium, expeditionExpenses, agentCommission, issuingRights, surcharges, iva);

        // Assert
        beforeTax.Should().Be(120_000m);
        withTax.Should().Be(139_200m);
    }

    [Fact]
    [Trait("Category", "Regression")]
    public void CalculateCommercialPremium_Should_RoundToTwoDecimals()
    {
        // Arrange
        const decimal netPremium = 1m;
        const decimal expeditionExpenses = 0.05m;
        const decimal agentCommission = 0.10m;
        const decimal issuingRights = 0.03m;
        const decimal surcharges = 0.02m;
        const decimal iva = 0.16m;
        // beforeTax = Math.Round(1 × 1.20, 2) = 1.20
        // withTax = Math.Round(1.20 × 1.16, 2) = Math.Round(1.392, 2) = 1.39

        // Act
        (decimal beforeTax, decimal withTax) = PremiumCalculator.CalculateCommercialPremium(
            netPremium, expeditionExpenses, agentCommission, issuingRights, surcharges, iva);

        // Assert
        beforeTax.Should().Be(1.20m);
        withTax.Should().Be(1.39m);
    }
}
