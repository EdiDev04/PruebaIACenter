using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.Services;
using Cotizador.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class CalculateQuoteUseCase : ICalculateQuoteUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly ILogger<CalculateQuoteUseCase> _logger;

    public CalculateQuoteUseCase(
        IQuoteRepository repository,
        ICoreOhsClient coreOhsClient,
        ILogger<CalculateQuoteUseCase> logger)
    {
        _repository = repository;
        _coreOhsClient = coreOhsClient;
        _logger = logger;
    }

    public async Task<CalculateResultResponse> ExecuteAsync(
        string folioNumber,
        CalculateRequest request,
        CancellationToken ct = default)
    {
        // 1. Leer PropertyQuote completo
        PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        // 2. Validar versión
        if (quote.Version != request.Version)
            throw new VersionConflictException(folioNumber, request.Version);

        // 3. Leer tarifas en paralelo desde core-ohs
        Task<List<FireTariffDto>> fireTariffsTask = _coreOhsClient.GetFireTariffsAsync(ct);
        Task<List<CatTariffDto>> catTariffsTask = _coreOhsClient.GetCatTariffsAsync(ct);
        Task<List<ElectronicEquipmentFactorDto>> equipFactorsTask = _coreOhsClient.GetElectronicEquipmentFactorsAsync(ct);
        Task<CalculationParametersDto> calcParamsTask = _coreOhsClient.GetCalculationParametersAsync(ct);

        await Task.WhenAll(fireTariffsTask, catTariffsTask, equipFactorsTask, calcParamsTask);

        List<FireTariffDto> fireTariffs = await fireTariffsTask;
        List<CatTariffDto> catTariffs = await catTariffsTask;
        List<ElectronicEquipmentFactorDto> equipFactors = await equipFactorsTask;
        CalculationParametersDto calcParams = await calcParamsTask;

        // 4. Obtener technicalLevel por CP único (batch lookup)
        // RN-009-02b: si EnabledGuarantees es null o vacío se interpreta como "sin filtro activo"
        // (CoverageOptions aún no configurado), para no romper folios en estados tempranos del wizard.
        List<string>? enabledGuaranteesList = quote.CoverageOptions?.EnabledGuarantees;
        HashSet<string>? enabledGuaranteeKeys = (enabledGuaranteesList != null && enabledGuaranteesList.Count > 0)
            ? new HashSet<string>(enabledGuaranteesList)
            : null;

        IEnumerable<string> calculableZipCodes = quote.Locations
            .Where(l => l.ValidationStatus == ValidationStatus.Calculable && !string.IsNullOrWhiteSpace(l.ZipCode))
            .Where(l => enabledGuaranteeKeys == null || l.Guarantees == null ||
                        !l.Guarantees.Any(g => !enabledGuaranteeKeys.Contains(g.GuaranteeKey)))
            .Select(l => l.ZipCode)
            .Distinct();

        Dictionary<string, int> techLevelByZip = new();
        foreach (string zipCode in calculableZipCodes)
        {
            ZipCodeDto? zipData = await _coreOhsClient.GetZipCodeAsync(zipCode, ct);
            if (zipData is not null)
                techLevelByZip[zipCode] = zipData.TechnicalLevel;
        }

        // 5. Calcular primas por ubicación
        var premiumsByLocation = new List<LocationPremium>();

        foreach (Location location in quote.Locations)
        {
            // Una ubicación es incalculable si su ValidationStatus es Incomplete
            // O si (RN-009-02b) alguna de sus garantías ya no está habilitada globalmente en CoverageOptions.
            // El filtro solo aplica cuando EnabledGuarantees tiene al menos un elemento (lista activa).
            bool hasDisabledGuarantee = enabledGuaranteeKeys != null &&
                location.Guarantees != null &&
                location.Guarantees.Any(g => !enabledGuaranteeKeys.Contains(g.GuaranteeKey));

            if (location.ValidationStatus != ValidationStatus.Calculable || hasDisabledGuarantee)
            {
                premiumsByLocation.Add(new LocationPremium
                {
                    LocationIndex = location.Index,
                    LocationName = location.LocationName,
                    NetPremium = 0m,
                    ValidationStatus = ValidationStatus.Incomplete,
                    CoveragePremiums = new List<CoveragePremium>(),
                });
                continue;
            }

            List<CoveragePremium> coveragePremiums = CalculateCoveragePremiums(
                location, fireTariffs, catTariffs, equipFactors, techLevelByZip);

            decimal locationNetPremium = PremiumCalculator.CalculateLocationNetPremium(coveragePremiums);

            premiumsByLocation.Add(new LocationPremium
            {
                LocationIndex = location.Index,
                LocationName = location.LocationName,
                NetPremium = locationNetPremium,
                ValidationStatus = ValidationStatus.Calculable,
                CoveragePremiums = coveragePremiums,
            });
        }

        // 6. Consolidar prima neta
        decimal netPremium = premiumsByLocation
            .Where(p => p.ValidationStatus == ValidationStatus.Calculable)
            .Sum(p => p.NetPremium);

        // 7. Verificar que haya al menos una ubicación calculable
        bool hasCalculable = premiumsByLocation.Any(p => p.ValidationStatus == ValidationStatus.Calculable);
        if (!hasCalculable)
            throw new InvalidQuoteStateException(folioNumber, quote.QuoteStatus, "No hay ubicaciones calculables para ejecutar el cálculo");

        // 8. Calcular prima comercial
        (decimal beforeTax, decimal withTax) = PremiumCalculator.CalculateCommercialPremium(
            netPremium,
            calcParams.ExpeditionExpenses,
            calcParams.AgentCommission,
            calcParams.IssuingRights,
            calcParams.Surcharges,
            calcParams.Iva);

        // 9. Persistir resultado atómico
        await _repository.UpdateFinancialResultAsync(
            folioNumber,
            request.Version,
            netPremium,
            beforeTax,
            withTax,
            premiumsByLocation,
            ct);

        // 10. Retornar respuesta
        List<LocationPremiumDto> locationDtos = premiumsByLocation.Select(p => new LocationPremiumDto(
            p.LocationIndex,
            p.LocationName,
            p.NetPremium,
            p.ValidationStatus,
            p.CoveragePremiums.Select(c => new CoveragePremiumDto(c.GuaranteeKey, c.InsuredAmount, c.Rate, c.Premium)).ToList()
        )).ToList();

        return new CalculateResultResponse(
            netPremium,
            beforeTax,
            withTax,
            locationDtos,
            QuoteStatus.Calculated,
            request.Version + 1
        );
    }

    private List<CoveragePremium> CalculateCoveragePremiums(
        Location location,
        List<FireTariffDto> fireTariffs,
        List<CatTariffDto> catTariffs,
        List<ElectronicEquipmentFactorDto> equipFactors,
        Dictionary<string, int> techLevelByZip)
    {
        FireTariffDto? fireTariff = fireTariffs.FirstOrDefault(f => f.FireKey == location.BusinessLine.FireKey);
        decimal fireRate = fireTariff?.BaseRate ?? 0m;

        if (fireTariff is null)
            _logger.LogWarning("FireKey {FireKey} not found in fire tariffs for location {Index}. Using rate 0.", location.BusinessLine.FireKey, location.Index);

        CatTariffDto? catData = catTariffs.FirstOrDefault(c => c.Zone == location.CatZone);

        techLevelByZip.TryGetValue(location.ZipCode, out int techLevel);

        var coveragePremiums = new List<CoveragePremium>();

        foreach (LocationGuarantee guarantee in location.Guarantees)
        {
            decimal rate = ResolveRate(guarantee.GuaranteeKey, fireRate, catData, equipFactors, techLevel);
            CoveragePremium coverage = PremiumCalculator.CalculateCoveragePremium(guarantee.GuaranteeKey, guarantee.InsuredAmount, rate);
            coveragePremiums.Add(coverage);
        }

        return coveragePremiums;
    }

    private static decimal ResolveRate(
        string guaranteeKey,
        decimal fireRate,
        CatTariffDto? catData,
        List<ElectronicEquipmentFactorDto> equipFactors,
        int techLevel)
    {
        return guaranteeKey switch
        {
            GuaranteeKeys.BuildingFire or GuaranteeKeys.ContentsFire or GuaranteeKeys.CoverageExtension
                => fireRate,

            GuaranteeKeys.CatTev
                => catData?.TevFactor ?? 0m,

            GuaranteeKeys.CatFhm
                => catData?.FhmFactor ?? 0m,

            GuaranteeKeys.DebrisRemoval or GuaranteeKeys.ExtraordinaryExpenses
                => SimplifiedTariffRates.SupplementaryRate,

            GuaranteeKeys.RentLoss or GuaranteeKeys.BusinessInterruption
                => SimplifiedTariffRates.IncomeRate,

            GuaranteeKeys.Theft or GuaranteeKeys.CashAndSecurities
                => SimplifiedTariffRates.SpecialRate,

            GuaranteeKeys.ElectronicEquipment
                => ResolveEquipmentRate(equipFactors, techLevel),

            // Glass e IlluminatedSigns: tarifa plana, PremiumCalculator ignora rate=0
            _ => 0m,
        };
    }

    private static decimal ResolveEquipmentRate(List<ElectronicEquipmentFactorDto> equipFactors, int techLevel)
    {
        ElectronicEquipmentFactorDto? factor = equipFactors.FirstOrDefault(
            e => e.EquipmentClass == SimplifiedTariffRates.DefaultEquipmentClass && e.ZoneLevel == techLevel);

        return factor?.Factor ?? equipFactors.FirstOrDefault()?.Factor ?? 0m;
    }
}
