using Cotizador.Application.DTOs;
using Cotizador.Domain.Constants;
using FluentValidation;

namespace Cotizador.Application.Validators;

public class UpdateCoverageOptionsRequestValidator : AbstractValidator<UpdateCoverageOptionsRequest>
{
    public UpdateCoverageOptionsRequestValidator()
    {
        RuleFor(r => r.EnabledGuarantees)
            .NotEmpty().WithMessage("Debe habilitar al menos una garantía");

        RuleForEach(r => r.EnabledGuarantees)
            .Must(key => GuaranteeKeys.All.Contains(key))
            .WithMessage((_, key) => $"Clave de garantía inválida: {key}");

        RuleFor(r => r.DeductiblePercentage)
            .InclusiveBetween(0m, 1m)
            .WithMessage("El porcentaje de deducible debe estar entre 0 y 1");

        RuleFor(r => r.CoinsurancePercentage)
            .InclusiveBetween(0m, 1m)
            .WithMessage("El porcentaje de coaseguro debe estar entre 0 y 1");

        RuleFor(r => r.Version)
            .GreaterThan(0).WithMessage("La versión es obligatoria");
    }
}
