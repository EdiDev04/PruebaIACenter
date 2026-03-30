using Cotizador.Application.DTOs;
using FluentValidation;

namespace Cotizador.Application.Validators;

public class CalculateRequestValidator : AbstractValidator<CalculateRequest>
{
    public CalculateRequestValidator()
    {
        RuleFor(r => r.Version)
            .GreaterThan(0).WithMessage("La versión es obligatoria");
    }
}
