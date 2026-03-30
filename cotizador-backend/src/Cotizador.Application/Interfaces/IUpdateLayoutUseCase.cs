using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IUpdateLayoutUseCase
{
    Task<LayoutConfigurationDto> ExecuteAsync(
        string folioNumber,
        UpdateLayoutRequest request,
        CancellationToken ct = default);
}
