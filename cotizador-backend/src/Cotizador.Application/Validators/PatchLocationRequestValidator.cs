using Cotizador.Application.DTOs;
using Cotizador.Domain.Constants;
using FluentValidation;

namespace Cotizador.Application.Validators;

public class PatchLocationRequestValidator : AbstractValidator<PatchLocationRequest>
{
    public PatchLocationRequestValidator()
    {
        RuleFor(r => r.Version)
            .GreaterThan(0).WithMessage("La versión es obligatoria");

        When(r => r.LocationName != null, () =>
        {
            RuleFor(r => r.LocationName)
                .NotEmpty().WithMessage("El nombre de la ubicación es obligatorio")
                .MaximumLength(200).WithMessage("El nombre de la ubicación es obligatorio");
        });

        When(r => r.Address != null, () =>
        {
            RuleFor(r => r.Address)
                .NotEmpty().WithMessage("La dirección es obligatoria")
                .MaximumLength(300).WithMessage("La dirección es obligatoria");
        });

        When(r => r.ZipCode != null, () =>
        {
            RuleFor(r => r.ZipCode)
                .Matches(@"^\d{5}$").WithMessage("El código postal debe ser de 5 dígitos");
        });

        When(r => r.Level.HasValue, () =>
        {
            RuleFor(r => r.Level!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("El nivel debe ser un número positivo");
        });

        When(r => r.ConstructionYear.HasValue, () =>
        {
            RuleFor(r => r.ConstructionYear!.Value)
                .InclusiveBetween(1800, DateTime.UtcNow.Year)
                .WithMessage("El año de construcción es inválido");
        });

        When(r => r.Guarantees != null && r.Guarantees.Count > 0, () =>
        {
            RuleForEach(r => r.Guarantees!).ChildRules(guarantee =>
            {
                guarantee.RuleFor(g => g.GuaranteeKey)
                    .Must(key => GuaranteeKeys.All.Contains(key))
                    .WithMessage(g => $"Clave de garantía inválida: {g.GuaranteeKey}");

                guarantee.RuleFor(g => g.InsuredAmount)
                    .GreaterThanOrEqualTo(0m)
                    .WithMessage("La suma asegurada debe ser mayor o igual a 0");
            });
        });
    }
}
