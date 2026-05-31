using FluentValidation;
using Tibr.Application.Dtos;

namespace Tibr.Application.Dtos.Validators
{
    public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
    {
        public UpdateOrderDtoValidator()
        {
            RuleFor(x => x)
                .Must(x => !string.IsNullOrWhiteSpace(x.PaymentStatus)
                        || !string.IsNullOrWhiteSpace(x.OrderStatus))
                .WithMessage("At least one of PaymentStatus or OrderStatus must be provided.");
        }
    }
}
