using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Simcag.NotificationService.Tests.Integration;

public sealed class NotificationApiHealthTests : IClassFixture<NotificationApiTestFactory>
{
    private readonly NotificationApiTestFactory _factory;

    public NotificationApiHealthTests(NotificationApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_Health_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Health_Live_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Health_Ready_Returns_200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/ready");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public sealed class NotificationApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
}
