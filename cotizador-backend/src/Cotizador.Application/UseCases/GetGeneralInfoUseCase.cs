using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cotizador.Application.UseCases;

public class GetGeneralInfoUseCase : IGetGeneralInfoUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ILogger<GetGeneralInfoUseCase> _logger;

    public GetGeneralInfoUseCase(IQuoteRepository repository, ILogger<GetGeneralInfoUseCase> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<GeneralInfoDto> ExecuteAsync(string folioNumber, CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(GetGeneralInfoUseCase), folioNumber);

        PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        return MapToDto(quote);
    }

    private static GeneralInfoDto MapToDto(PropertyQuote quote) =>
        new(
            new InsuredDataDto(
                quote.InsuredData.Name,
                quote.InsuredData.TaxId,
                quote.InsuredData.Email,
                quote.InsuredData.Phone
            ),
            new ConductionDataDto(
                quote.ConductionData.SubscriberCode,
                quote.ConductionData.OfficeName,
                quote.ConductionData.BranchOffice
            ),
            quote.AgentCode,
            quote.BusinessType,
            quote.RiskClassification,
            quote.Version
        );
}
