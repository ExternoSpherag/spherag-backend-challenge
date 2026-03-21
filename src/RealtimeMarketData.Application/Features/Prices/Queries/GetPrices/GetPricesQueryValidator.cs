using FluentValidation;

namespace RealtimeMarketData.Application.Features.Prices.Queries.GetPrices;

public sealed class GetPricesQueryValidator : AbstractValidator<GetPricesQuery>
{
    public GetPricesQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("The 'from' value must be lower than or equal to 'to'.");
    }
}