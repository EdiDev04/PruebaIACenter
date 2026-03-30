using Cotizador.Application.DTOs;
using FluentValidation;

namespace Cotizador.Application.Validators;

public class UpdateLayoutRequestValidator : AbstractValidator<UpdateLayoutRequest>
{
    private static readonly HashSet<string> ValidColumns = new(StringComparer.Ordinal)
    {
        "index", "locationName", "address", "zipCode", "state", "municipality",
        "neighborhood", "city", "constructionType", "level", "constructionYear",
        "businessLine", "guarantees", "catZone", "validationStatus"
    };

    private static readonly HashSet<string> ValidDisplayModes = new(StringComparer.Ordinal)
    {
        "grid", "list"
    };

    public UpdateLayoutRequestValidator()
    {
        RuleFor(r => r.DisplayMode)
            .NotEmpty().WithMessage("El modo de visualización es obligatorio")
            .Must(mode => ValidDisplayModes.Contains(mode))
                .WithMessage("Modo de visualización inválido. Valores permitidos: grid, list");

        RuleFor(r => r.VisibleColumns)
            .NotNull().WithMessage("Debe seleccionar al menos una columna visible")
            .Must(cols => cols != null && cols.Count > 0)
                .WithMessage("Debe seleccionar al menos una columna visible");

        RuleForEach(r => r.VisibleColumns)
            .Must(col => ValidColumns.Contains(col))
                .WithMessage((_, col) => $"Columna '{col}' no es válida");

        RuleFor(r => r.Version)
            .GreaterThan(0).WithMessage("La versión es obligatoria");
    }
}
