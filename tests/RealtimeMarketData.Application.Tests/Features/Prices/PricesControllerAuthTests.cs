using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using RealtimeMarketData.Api.Controllers;
using Xunit;

namespace RealtimeMarketData.Application.Tests.Features.Prices;

public sealed class PricesControllerAuthTests
{
    [Fact]
    public void PricesController_ShouldHaveAuthorizeAttribute()
    {
        var attributes = typeof(PricesController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

        attributes.Should().NotBeEmpty("PricesController must be protected by [Authorize]");
    }
}