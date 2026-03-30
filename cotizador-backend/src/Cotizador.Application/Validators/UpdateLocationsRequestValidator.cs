using Cotizador.Application.DTOs;
using Cotizador.Domain.Constants;
using FluentValidation;

namespace Cotizador.Application.Validators;

public class UpdateLocationsRequestValidator : AbstractValidator<UpdateLocationsRequest>
{
    public UpdateLocationsRequestValidator()
    {
        RuleFor(r => r.Version)
            .GreaterThan(0).WithMessage("La versión es obligatoria");

        RuleFor(r => r.Locations)
            .NotNull().WithMessage("La lista de ubicaciones es requerida");

        When(r => r.Locations != null && r.Locations.Count > 0, () =>
        {
            RuleFor(r => r.Locations)
                .Must(locs => locs.Select(l => l.Index).Distinct().Count() == locs.Count)
                .WithMessage("El índice de ubicación es obligatorio y debe ser único");

            RuleForEach(r => r.Locations).ChildRules(location =>
            {
                location.RuleFor(l => l.Index)
                    .GreaterThanOrEqualTo(1)
                    .WithMessage("El índice de ubicación es obligatorio y debe ser único");

                location.RuleFor(l => l.LocationName)
                    .NotEmpty().WithMessage("El nombre de la ubicación es obligatorio")
                    .MaximumLength(200).WithMessage("El nombre de la ubicación es obligatorio");

                location.RuleFor(l => l.Address)
                    .NotEmpty().WithMessage("La dirección es obligatoria")
                    .MaximumLength(300).WithMessage("La dirección es obligatoria");

                location.When(l => !string.IsNullOrEmpty(l.ZipCode), () =>
                {
                    location.RuleFor(l => l.ZipCode)
                        .Matches(@"^\d{5}$").WithMessage("El código postal debe ser de 5 dígitos");
                });

                location.RuleFor(l => l.Level)
                    .GreaterThanOrEqualTo(0).WithMessage("El nivel debe ser un número positivo");

                location.When(l => l.ConstructionYear != 0, () =>
                {
                    location.RuleFor(l => l.ConstructionYear)
                        .InclusiveBetween(1800, DateTime.UtcNow.Year)
                        .WithMessage("El año de construcción es inválido");
                });

                location.When(l => l.Guarantees != null && l.Guarantees.Count > 0, () =>
                {
                    location.RuleForEach(l => l.Guarantees!).ChildRules(guarantee =>
                    {
                        guarantee.RuleFor(g => g.GuaranteeKey)
                            .Must(key => GuaranteeKeys.All.Contains(key))
                            .WithMessage(g => $"Clave de garantía inválida: {g.GuaranteeKey}");

                        guarantee.RuleFor(g => g.InsuredAmount)
                            .GreaterThanOrEqualTo(0m)
                            .WithMessage("La suma asegurada debe ser mayor o igual a 0");
                    });
                });
            });
        });
    }
}
