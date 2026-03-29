using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cotizador.API.Auth;

public class BasicAuthSchemeOptions : AuthenticationSchemeOptions { }

public class BasicAuthHandler : AuthenticationHandler<BasicAuthSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public BasicAuthHandler(
        IOptionsMonitor<BasicAuthSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization header"));
        }

        try
        {
            AuthenticationHeaderValue authHeader = AuthenticationHeaderValue.Parse(Request.Headers.Authorization!);

            if (!string.Equals(authHeader.Scheme, "Basic", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid authentication scheme"));
            }

            byte[] credentialBytes = Convert.FromBase64String(authHeader.Parameter ?? string.Empty);
            string[] credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

            if (credentials.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid credentials format"));
            }

            string username = credentials[0];
            string password = credentials[1];

            string expectedUsername = _configuration["Auth:Username"] ?? string.Empty;
            string expectedPassword = _configuration["Auth:Password"] ?? string.Empty;

            if (username != expectedUsername || password != expectedPassword)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
            }

            Claim[] claims = new[] { new Claim(ClaimTypes.Name, username) };
            ClaimsIdentity identity = new(claims, Scheme.Name);
            ClaimsPrincipal principal = new(identity);
            AuthenticationTicket ticket = new(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to parse Authorization header");
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
        }
    }
}
