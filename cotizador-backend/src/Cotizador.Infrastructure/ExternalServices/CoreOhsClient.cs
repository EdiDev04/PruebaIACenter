using System.Net.Http.Json;
using System.Text.Json;
using Cotizador.Application.DTOs;
using Cotizador.Application.Ports;
using Cotizador.Domain.Exceptions;

namespace Cotizador.Infrastructure.ExternalServices;

public class CoreOhsClient : ICoreOhsClient
{
    private readonly HttpClient _httpClient;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CoreOhsClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<SubscriberDto>> GetSubscribersAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<SubscriberDto>>("/v1/subscribers", ct);
    }

    public async Task<AgentDto?> GetAgentByCodeAsync(string code, CancellationToken ct = default)
    {
        try
        {
            return await GetDataAsync<AgentDto>($"/v1/agents?code={Uri.EscapeDataString(code)}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<List<BusinessLineDto>> GetBusinessLinesAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<BusinessLineDto>>("/v1/business-lines", ct);
    }

    public async Task<ZipCodeDto?> GetZipCodeAsync(string zipCode, CancellationToken ct = default)
    {
        try
        {
            return await GetDataAsync<ZipCodeDto>($"/v1/zip-codes/{Uri.EscapeDataString(zipCode)}", ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<ZipCodeValidationDto> ValidateZipCodeAsync(string zipCode, CancellationToken ct = default)
    {
        return await GetDataAsync<ZipCodeValidationDto>($"/v1/zip-codes/validate?zipCode={Uri.EscapeDataString(zipCode)}", ct);
    }

    public async Task<FolioDto> GenerateFolioAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<FolioDto>("/v1/folios/next", ct);
    }

    public async Task<List<RiskClassificationDto>> GetRiskClassificationsAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<RiskClassificationDto>>("/v1/catalogs/risk-classification", ct);
    }

    public async Task<List<GuaranteeDto>> GetGuaranteesAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<GuaranteeDto>>("/v1/catalogs/guarantees", ct);
    }

    public async Task<List<FireTariffDto>> GetFireTariffsAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<FireTariffDto>>("/v1/tariffs/fire", ct);
    }

    public async Task<List<CatTariffDto>> GetCatTariffsAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<CatTariffDto>>("/v1/tariffs/cat", ct);
    }

    public async Task<List<FhmTariffDto>> GetFhmTariffsAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<FhmTariffDto>>("/v1/tariffs/fhm", ct);
    }

    public async Task<List<ElectronicEquipmentFactorDto>> GetElectronicEquipmentFactorsAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<List<ElectronicEquipmentFactorDto>>("/v1/tariffs/electronic-equipment", ct);
    }

    public async Task<CalculationParametersDto> GetCalculationParametersAsync(CancellationToken ct = default)
    {
        return await GetDataAsync<CalculationParametersDto>("/v1/tariffs/calculation-parameters", ct);
    }

    private async Task<T> GetDataAsync<T>(string path, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(path, ct);
        }
        catch (Exception ex)
        {
            throw new CoreOhsUnavailableException($"Error communicating with core-ohs at '{path}'.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"core-ohs returned {(int)response.StatusCode} for '{path}'.");
        }

        using JsonDocument doc = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

        JsonElement dataElement = doc.RootElement.GetProperty("data");
        T result = dataElement.Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException($"Unexpected null response from core-ohs at '{path}'.");
        return result;
    }
}
