using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetBusinessLinesUseCase
{
    Task<List<BusinessLineDto>> ExecuteAsync(CancellationToken ct = default);
}
