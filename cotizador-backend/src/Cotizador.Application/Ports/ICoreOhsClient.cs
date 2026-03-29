using Cotizador.Application.DTOs;

namespace Cotizador.Application.Ports;

public interface ICoreOhsClient
{
    Task<List<SubscriberDto>> GetSubscribersAsync(CancellationToken ct = default);
    Task<AgentDto?> GetAgentByCodeAsync(string code, CancellationToken ct = default);
    Task<List<BusinessLineDto>> GetBusinessLinesAsync(CancellationToken ct = default);
    Task<ZipCodeDto?> GetZipCodeAsync(string zipCode, CancellationToken ct = default);
    Task<ZipCodeValidationDto> ValidateZipCodeAsync(string zipCode, CancellationToken ct = default);
    Task<FolioDto> GenerateFolioAsync(CancellationToken ct = default);
    Task<List<RiskClassificationDto>> GetRiskClassificationsAsync(CancellationToken ct = default);
    Task<List<GuaranteeDto>> GetGuaranteesAsync(CancellationToken ct = default);
    Task<List<FireTariffDto>> GetFireTariffsAsync(CancellationToken ct = default);
    Task<List<CatTariffDto>> GetCatTariffsAsync(CancellationToken ct = default);
    Task<List<FhmTariffDto>> GetFhmTariffsAsync(CancellationToken ct = default);
    Task<List<ElectronicEquipmentFactorDto>> GetElectronicEquipmentFactorsAsync(CancellationToken ct = default);
    Task<CalculationParametersDto> GetCalculationParametersAsync(CancellationToken ct = default);
}
