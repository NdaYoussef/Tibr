using FluentValidation;
using Tibr.Application.Dtos;

namespace Tibr.Application.Dtos.Validators
{
    public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
    {
        public CreateOrderDtoValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty()
                .WithMessage("UserId is required.");

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Order must have at least one item.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductId)
                    .NotEmpty()
                    .WithMessage("ProductId is required for each item.");

                item.RuleFor(i => i.Quantity)
                    .InclusiveBetween(1, 10000)
                    .WithMessage("Quantity must be between 1 and 10,000.");
            });
        }
    }
}
