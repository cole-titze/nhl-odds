using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApi.Filters;

namespace WebApi.Tests.BusinessLogic.UnitTests.AdminServiceTests;

[TestClass]
public class RequireAdminKeyAttributeTests
{
    private static AuthorizationFilterContext Run(string? configuredKey, string environment, string? headerKey)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [RequireAdminKeyAttribute.ConfigKey] = configuredKey })
            .Build();
        var env = A.Fake<IWebHostEnvironment>();
        A.CallTo(() => env.EnvironmentName).Returns(environment);

        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IConfiguration>(config)
                .AddSingleton(env)
                .BuildServiceProvider(),
        };
        if (headerKey != null)
            http.Request.Headers[RequireAdminKeyAttribute.HeaderName] = headerKey;

        var context = new AuthorizationFilterContext(
            new ActionContext(http, new RouteData(), new ActionDescriptor()), new List<IFilterMetadata>());
        new RequireAdminKeyAttribute().OnAuthorization(context);
        return context;
    }

    [TestMethod]
    public void WithMatchingKey_ShouldAllow()
    {
        Run("secret", Environments.Production, "secret").Result.Should().BeNull();
    }

    [TestMethod]
    public void WithWrongKey_ShouldReturnUnauthorized()
    {
        Run("secret", Environments.Production, "wrong").Result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public void WithMissingHeader_ShouldReturnUnauthorized()
    {
        Run("secret", Environments.Production, null).Result.Should().BeOfType<UnauthorizedResult>();
    }

    [TestMethod]
    public void WithNoKeyConfigured_InProduction_ShouldForbid()
    {
        var result = Run(null, Environments.Production, null).Result;

        result.Should().BeOfType<StatusCodeResult>().Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [TestMethod]
    public void WithNoKeyConfigured_InDevelopment_ShouldAllow()
    {
        Run(null, Environments.Development, null).Result.Should().BeNull();
    }
}