using Cotizador.Application.DTOs;
using FluentValidation;

namespace Cotizador.Application.Validators;

public class UpdateGeneralInfoRequestValidator : AbstractValidator<UpdateGeneralInfoRequest>
{
    public UpdateGeneralInfoRequestValidator()
    {
        RuleFor(r => r.InsuredData.Name)
            .NotEmpty().WithMessage("El nombre del asegurado es obligatorio")
            .MaximumLength(200).WithMessage("El nombre del asegurado es obligatorio");

        RuleFor(r => r.InsuredData.TaxId)
            .NotEmpty().WithMessage("El RFC del asegurado es obligatorio y debe tener formato válido")
            .Matches(@"^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$")
                .WithMessage("El RFC del asegurado es obligatorio y debe tener formato válido");

        When(r => !string.IsNullOrEmpty(r.InsuredData.Email), () =>
        {
            RuleFor(r => r.InsuredData.Email)
                .EmailAddress().WithMessage("El correo electrónico no tiene formato válido");
        });

        When(r => !string.IsNullOrEmpty(r.InsuredData.Phone), () =>
        {
            RuleFor(r => r.InsuredData.Phone)
                .MaximumLength(20).WithMessage("El teléfono no tiene formato válido");
        });

        RuleFor(r => r.AgentCode)
            .NotEmpty().WithMessage("Código de agente inválido")
            .Matches(@"^AGT-\d{3}$").WithMessage("Código de agente inválido");

        RuleFor(r => r.ConductionData.SubscriberCode)
            .NotEmpty().WithMessage("El suscriptor es obligatorio")
            .Matches(@"^SUB-\d{3}$").WithMessage("El suscriptor es obligatorio");

        RuleFor(r => r.ConductionData.OfficeName)
            .NotEmpty().WithMessage("La oficina es obligatoria");

        RuleFor(r => r.BusinessType)
            .NotEmpty().WithMessage("El tipo de negocio es obligatorio");

        RuleFor(r => r.RiskClassification)
            .NotEmpty().WithMessage("Clasificación de riesgo inválida");

        RuleFor(r => r.Version)
            .GreaterThan(0).WithMessage("La versión es obligatoria");
    }
}
