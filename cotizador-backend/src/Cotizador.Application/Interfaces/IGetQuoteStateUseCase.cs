using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetQuoteStateUseCase
{
    Task<QuoteStateDto> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
