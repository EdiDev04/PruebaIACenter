using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetCoverageOptionsUseCase : IGetCoverageOptionsUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetCoverageOptionsUseCase> _logger;

    public GetCoverageOptionsUseCase(IQuoteRepository repository, ILogger<GetCoverageOptionsUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CoverageOptionsDto> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetCoverageOptionsUseCase), folioNumber);

        var quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        var coverageOptions = quote.CoverageOptions;

        // Return defaults when coverage options have never been configured
        if (coverageOptions is null || coverageOptions.EnabledGuarantees.Count == 0)
        {
            return new CoverageOptionsDto(
                new List<string>(GuaranteeKeys.All),
                0m,
                0m,
                quote.Version
            );
        }

        return new CoverageOptionsDto(
            coverageOptions.EnabledGuarantees,
            coverageOptions.DeductiblePercentage,
            coverageOptions.CoinsurancePercentage,
            quote.Version
        );
    }
}
