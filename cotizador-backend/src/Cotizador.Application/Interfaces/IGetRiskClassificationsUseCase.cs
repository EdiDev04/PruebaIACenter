using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetRiskClassificationsUseCase
{
    Task<List<RiskClassificationDto>> ExecuteAsync(CancellationToken ct = default);
}
