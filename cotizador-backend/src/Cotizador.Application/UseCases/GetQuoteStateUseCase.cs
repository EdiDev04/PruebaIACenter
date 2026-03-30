using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetQuoteStateUseCase : IGetQuoteStateUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetQuoteStateUseCase> _logger;

    public GetQuoteStateUseCase(IQuoteRepository repository, ILogger<GetQuoteStateUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<QuoteStateDto> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetQuoteStateUseCase), folioNumber);

        PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        return MapToDto(quote);
    }

    private static QuoteStateDto MapToDto(PropertyQuote quote)
    {
        ProgressDto progress = CalculateProgress(quote);
        LocationsStateDto locationsState = CalculateLocationsState(quote);
        bool readyForCalculation = locationsState.Calculable > 0;
        CalculationResultDto? calculationResult = quote.QuoteStatus == QuoteStatus.Calculated
            ? MapCalculationResult(quote)
            : null;

        return new QuoteStateDto(
            quote.FolioNumber,
            quote.QuoteStatus,
            quote.Version,
            progress,
            locationsState,
            readyForCalculation,
            calculationResult
        );
    }

    private static ProgressDto CalculateProgress(PropertyQuote quote)
    {
        bool generalInfo = !string.IsNullOrWhiteSpace(quote.InsuredData.Name);
        return new(
            GeneralInfo: generalInfo,
            LayoutConfiguration: generalInfo,
            Locations: quote.Locations.Count > 0,
            CoverageOptions: quote.CoverageOptions.EnabledGuarantees.Count > 0
        );
    }

    private static LocationsStateDto CalculateLocationsState(PropertyQuote quote)
    {
        int total = quote.Locations.Count;
        int calculable = quote.Locations.Count(loc => loc.ValidationStatus == ValidationStatus.Calculable);
        int incomplete = total - calculable;

        List<LocationAlertDto> alerts = quote.Locations
            .Where(loc => loc.ValidationStatus == ValidationStatus.Incomplete)
            .Select(loc => new LocationAlertDto(loc.Index, loc.LocationName, loc.BlockingAlerts))
            .ToList();

        return new LocationsStateDto(total, calculable, incomplete, alerts);
    }

    private static CalculationResultDto MapCalculationResult(PropertyQuote quote) =>
        new(
            NetPremium: quote.NetPremium,
            CommercialPremiumBeforeTax: 0m, // Campo pendiente de SPEC-009 (motor de cálculo)
            CommercialPremium: quote.CommercialPremium,
            PremiumsByLocation: quote.PremiumsByLocation
                .Select(lp => new LocationPremiumDto(
                    lp.LocationIndex,
                    lp.LocationName,
                    lp.NetPremium,
                    lp.ValidationStatus,
                    lp.CoveragePremiums
                        .Select(cp => new CoveragePremiumDto(cp.GuaranteeKey, cp.InsuredAmount, cp.Rate, cp.Premium))
                        .ToList()
                ))
                .ToList()
        );
}
