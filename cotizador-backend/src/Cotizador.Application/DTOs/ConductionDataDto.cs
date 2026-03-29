namespace Cotizador.Application.DTOs;

public record ConductionDataDto(
    string SubscriberCode,
    string OfficeName,
    string? BranchOffice
);
