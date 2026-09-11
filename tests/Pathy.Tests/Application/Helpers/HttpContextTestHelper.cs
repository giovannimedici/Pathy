using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Pathy.Tests.Application.Helpers;

/// <summary>
/// Creates configured <see cref="IHttpContextAccessor"/> instances for unit tests.
/// </summary>
internal static class HttpContextTestHelper
{
    public static IHttpContextAccessor Create(
        string scheme = "https",
        string host = "pathy.test",
        Guid? userId = null)
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = scheme,
                Host = new HostString(host)
            }
        };

        if (userId.HasValue)
        {
            var claims = new[] { new Claim("sub", userId.Value.ToString()) };
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
        }

        return new HttpContextAccessor { HttpContext = context };
    }
}
