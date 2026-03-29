using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetQuoteSummaryUseCase
{
    Task<QuoteSummaryDto> ExecuteAsync(string folioNumber, CancellationToken ct = default);
}
