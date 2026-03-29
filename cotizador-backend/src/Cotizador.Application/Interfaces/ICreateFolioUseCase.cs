using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface ICreateFolioUseCase
{
    Task<(QuoteSummaryDto Dto, bool IsNew)> ExecuteAsync(
        string idempotencyKey,
        string createdBy,
        CancellationToken ct = default);
}
