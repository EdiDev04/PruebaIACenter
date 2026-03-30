namespace Cotizador.Domain.Constants;

public static class GuaranteeKeys
{
    public const string BuildingFire = "building_fire";
    public const string ContentsFire = "contents_fire";
    public const string CoverageExtension = "coverage_extension";
    public const string CatTev = "cat_tev";
    public const string CatFhm = "cat_fhm";
    public const string DebrisRemoval = "debris_removal";
    public const string ExtraordinaryExpenses = "extraordinary_expenses";
    public const string RentLoss = "rent_loss";
    public const string BusinessInterruption = "business_interruption";
    public const string ElectronicEquipment = "electronic_equipment";
    public const string Theft = "theft";
    public const string CashAndSecurities = "cash_and_securities";
    public const string Glass = "glass";
    public const string IlluminatedSigns = "illuminated_signs";

    public static readonly string[] All = new[]
    {
        BuildingFire, ContentsFire, CoverageExtension, CatTev, CatFhm,
        DebrisRemoval, ExtraordinaryExpenses, RentLoss, BusinessInterruption,
        ElectronicEquipment, Theft, CashAndSecurities, Glass, IlluminatedSigns
    };

    public static readonly string[] NotRequiringInsuredAmount = new[]
    {
        Glass, IlluminatedSigns
    };
}
