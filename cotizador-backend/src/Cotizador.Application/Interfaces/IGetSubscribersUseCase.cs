using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetSubscribersUseCase
{
    Task<List<SubscriberDto>> ExecuteAsync(CancellationToken ct = default);
}
