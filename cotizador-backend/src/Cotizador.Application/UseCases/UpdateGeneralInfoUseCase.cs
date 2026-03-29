using Cotizador.Application.DTOs;
using Cotizador.Application.Interfaces;
using Cotizador.Application.Ports;
using Cotizador.Application.Settings;
using Cotizador.Domain.Constants;
using Cotizador.Domain.Entities;
using Cotizador.Domain.Exceptions;
using Cotizador.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cotizador.Application.UseCases;

public class UpdateGeneralInfoUseCase : IUpdateGeneralInfoUseCase
{
    private readonly IQuoteRepository _repository;
    private readonly ICoreOhsClient _coreOhsClient;
    private readonly BusinessTypeSettings _businessTypeSettings;
    private readonly ILogger<UpdateGeneralInfoUseCase> _logger;

    public UpdateGeneralInfoUseCase(
        IQuoteRepository repository,
        ICoreOhsClient coreOhsClient,
        IOptions<BusinessTypeSettings> businessTypeSettings,
        ILogger<UpdateGeneralInfoUseCase> logger)
    {
        _repository = repository;
        _coreOhsClient = coreOhsClient;
        _businessTypeSettings = businessTypeSettings.Value;
        _logger = logger;
    }

    public async Task<GeneralInfoDto> ExecuteAsync(
        string folioNumber,
        UpdateGeneralInfoRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Ejecutando {UseCase} para folio {Folio}", nameof(UpdateGeneralInfoUseCase), folioNumber);

        // 1. Validate BusinessType against allowed values (done in use case, not validator)
        if (!_businessTypeSettings.AllowedValues.Contains(request.BusinessType, StringComparer.OrdinalIgnoreCase))
        {
            string allowedList = string.Join(", ", _businessTypeSettings.AllowedValues);
            ValidationFailure failure = new("BusinessType", $"Tipo de negocio inválido. Valores permitidos: {allowedList}");
            throw new ValidationException(new[] { failure });
        }

        // 2. Read folio
        PropertyQuote? quote = await _repository.GetByFolioNumberAsync(folioNumber, ct);
        if (quote is null)
            throw new FolioNotFoundException(folioNumber);

        // 3. Validate agent exists in catalog
        AgentDto? agent = await _coreOhsClient.GetAgentByCodeAsync(request.AgentCode, ct);
        if (agent is null)
        {
            _logger.LogWarning("Agente {AgentCode} no encontrado en catálogo", request.AgentCode);
            throw new InvalidQuoteStateException(
                folioNumber,
                quote.QuoteStatus,
                $"El agente {request.AgentCode} no está registrado en el catálogo");
        }

        // 4. Determine status transition
        string? newStatus = quote.QuoteStatus == QuoteStatus.Draft ? QuoteStatus.InProgress : null;

        // 5. Build value objects
        InsuredData insuredData = new()
        {
            Name = request.InsuredData.Name,
            TaxId = request.InsuredData.TaxId,
            Email = request.InsuredData.Email,
            Phone = request.InsuredData.Phone
        };

        ConductionData conductionData = new()
        {
            SubscriberCode = request.ConductionData.SubscriberCode,
            OfficeName = request.ConductionData.OfficeName,
            BranchOffice = request.ConductionData.BranchOffice
        };

        // 6. Persist
        await _repository.UpdateGeneralInfoAsync(
            folioNumber,
            request.Version,
            insuredData,
            conductionData,
            request.AgentCode,
            request.BusinessType,
            request.RiskClassification,
            newStatus,
            ct);

        // 7. Read updated quote to return version+1
        PropertyQuote? updated = await _repository.GetByFolioNumberAsync(folioNumber, ct);

        _logger.LogInformation("General info actualizada para folio {Folio}", folioNumber);
        return MapToDto(updated!);
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
