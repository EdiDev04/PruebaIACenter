using Cotizador.Application.DTOs;

namespace Cotizador.Application.Interfaces;

public interface IGetAgentByCodeUseCase
{
    Task<AgentDto?> ExecuteAsync(string code, CancellationToken ct = default);
}
