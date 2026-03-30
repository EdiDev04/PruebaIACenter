using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface ICalculateQuoteUseCase
{
    Task<CalculateResultResponse> ExecuteAsync(string folioNumber, CalculateRequest request, CancellationToken ct = default);
}
