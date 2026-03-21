using MediatR;
using RealtimeMarketData.Application.Common.Results;

namespace RealtimeMarketData.Application.Features.Streaming.Commands.IngestTradeTick;

public sealed record IngestTradeTickCommand(
    string Symbol,
    decimal Price,
    decimal Quantity,
    DateTimeOffset TradeTimestamp,
    long TradeId) : IRequest<Result>;