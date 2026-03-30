using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IUpdateCoverageOptionsUseCase
{
    Task<CoverageOptionsDto> ExecuteAsync(
        string folioNumber,
        UpdateCoverageOptionsRequest request,
        CancellationToken ct = default);
}
