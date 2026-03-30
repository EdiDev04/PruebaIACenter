using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetLocationsSummaryUseCase
{
    Task<LocationsSummaryResponse> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
