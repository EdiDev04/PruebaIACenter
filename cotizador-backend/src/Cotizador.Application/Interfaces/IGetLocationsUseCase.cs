using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetLocationsUseCase
{
    Task<LocationsResponse> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
