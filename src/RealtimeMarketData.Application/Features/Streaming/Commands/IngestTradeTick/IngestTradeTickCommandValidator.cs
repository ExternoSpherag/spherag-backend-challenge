using FluentValidation;

namespace RealtimeMarketData.Application.Features.Streaming.Commands.IngestTradeTick;

public sealed class IngestTradeTickCommandValidator : AbstractValidator<IngestTradeTickCommand>
{
    public IngestTradeTickCommandValidator()
    {
        RuleFor(x => x.Symbol)
            .NotEmpty()
            .MaximumLength(10)
            .Matches("^[a-zA-Z]+$");

        RuleFor(x => x.Price)
            .GreaterThan(0);

        RuleFor(x => x.Quantity)
            .GreaterThan(0);

        RuleFor(x => x.TradeId)
            .GreaterThan(0);

        RuleFor(x => x.TradeTimestamp)
            .NotEqual(default(DateTimeOffset));
    }
}