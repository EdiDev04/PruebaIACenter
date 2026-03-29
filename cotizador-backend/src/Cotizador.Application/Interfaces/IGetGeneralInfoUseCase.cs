using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetGeneralInfoUseCase
{
    Task<GeneralInfoDto> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
