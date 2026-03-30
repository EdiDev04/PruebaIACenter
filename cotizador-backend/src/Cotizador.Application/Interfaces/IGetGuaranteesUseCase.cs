using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetGuaranteesUseCase
{
    Task<List<GuaranteeDto>> ExecuteAsync(CancellationToken ct = default);
}
