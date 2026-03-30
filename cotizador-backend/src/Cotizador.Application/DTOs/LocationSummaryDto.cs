namespace Cotizador.Application.DTOs;

public record LocationSummaryDto(
    int Index,
    string LocationName,
    string ValidationStatus,
    List<string> BlockingAlerts);
