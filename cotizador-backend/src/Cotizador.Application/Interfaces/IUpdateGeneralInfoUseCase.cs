using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IUpdateGeneralInfoUseCase
{
    Task<GeneralInfoDto> ExecuteAsync(
        string folioNumber,
        UpdateGeneralInfoRequest request,
        CancellationToken ct = default);
}
