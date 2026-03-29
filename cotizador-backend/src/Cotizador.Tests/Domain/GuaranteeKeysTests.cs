using Cotizador.Domain.Constants;
using FluentAssertions;
using Xunit;

namespace Cotizador.Tests.Domain;

public class GuaranteeKeysTests
{
    [Fact]
    public void GuaranteeKeys_All_Should_ContainExactly14Keys()
    {
        // Assert
        GuaranteeKeys.All.Should().HaveCount(14);
    }

    [Theory]
    [InlineData(GuaranteeKeys.BuildingFire, "building_fire")]
    [InlineData(GuaranteeKeys.ContentsFire, "contents_fire")]
    [InlineData(GuaranteeKeys.CoverageExtension, "coverage_extension")]
    [InlineData(GuaranteeKeys.CatTev, "cat_tev")]
    [InlineData(GuaranteeKeys.CatFhm, "cat_fhm")]
    [InlineData(GuaranteeKeys.DebrisRemoval, "debris_removal")]
    [InlineData(GuaranteeKeys.ExtraordinaryExpenses, "extraordinary_expenses")]
    [InlineData(GuaranteeKeys.RentLoss, "rent_loss")]
    [InlineData(GuaranteeKeys.BusinessInterruption, "business_interruption")]
    [InlineData(GuaranteeKeys.ElectronicEquipment, "electronic_equipment")]
    [InlineData(GuaranteeKeys.Theft, "theft")]
    [InlineData(GuaranteeKeys.CashAndSecurities, "cash_and_securities")]
    [InlineData(GuaranteeKeys.Glass, "glass")]
    [InlineData(GuaranteeKeys.IlluminatedSigns, "illuminated_signs")]
    public void GuaranteeKeys_Constant_Should_HaveExpectedValue(string constant, string expected)
    {
        // Assert
        constant.Should().Be(expected);
    }

    [Theory]
    [InlineData(GuaranteeKeys.BuildingFire)]
    [InlineData(GuaranteeKeys.ContentsFire)]
    [InlineData(GuaranteeKeys.CoverageExtension)]
    [InlineData(GuaranteeKeys.CatTev)]
    [InlineData(GuaranteeKeys.CatFhm)]
    [InlineData(GuaranteeKeys.DebrisRemoval)]
    [InlineData(GuaranteeKeys.ExtraordinaryExpenses)]
    [InlineData(GuaranteeKeys.RentLoss)]
    [InlineData(GuaranteeKeys.BusinessInterruption)]
    [InlineData(GuaranteeKeys.ElectronicEquipment)]
    [InlineData(GuaranteeKeys.Theft)]
    [InlineData(GuaranteeKeys.CashAndSecurities)]
    [InlineData(GuaranteeKeys.Glass)]
    [InlineData(GuaranteeKeys.IlluminatedSigns)]
    public void GuaranteeKeys_All_Should_ContainEachConstant(string key)
    {
        // Assert
        GuaranteeKeys.All.Should().Contain(key);
    }
}
