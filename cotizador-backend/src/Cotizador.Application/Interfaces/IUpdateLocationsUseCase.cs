using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IUpdateLocationsUseCase
{
    Task<LocationsResponse> ExecuteAsync(string folioNumber, UpdateLocationsRequest request, CancellationToken ct = default);
}
