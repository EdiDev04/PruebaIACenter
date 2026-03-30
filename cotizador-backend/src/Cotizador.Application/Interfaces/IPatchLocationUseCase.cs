using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IPatchLocationUseCase
{
    Task<SingleLocationResponse> ExecuteAsync(string folioNumber, int index, PatchLocationRequest request, CancellationToken ct = default);
}
