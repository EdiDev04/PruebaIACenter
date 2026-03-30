using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetCoverageOptionsUseCase
{
    Task<CoverageOptionsDto> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
