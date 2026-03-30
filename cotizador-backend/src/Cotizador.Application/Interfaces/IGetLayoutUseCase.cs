using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetLayoutUseCase
{
    Task<LayoutConfigurationDto> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
