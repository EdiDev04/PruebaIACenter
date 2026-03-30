using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetZipCodeUseCase
{
    Task<ZipCodeDto?> ExecuteAsync(string zipCode, CancellationToken ct = default);
}
