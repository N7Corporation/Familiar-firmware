using Familiar.Host.Options;
using Familiar.Host.Services;
using Microsoft.Extensions.Options;

namespace Familiar.Host.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", (
            LoginRequest request,
            IOptions<SecurityOptions> options,
            ITokenService tokenService) =>
        {
            if (string.IsNullOrEmpty(request.Pin))
            {
                return Results.BadRequest(new { Error = "PIN is required" });
            }

            if (request.Pin != options.Value.Pin)
            {
                return Results.Unauthorized();
            }

            var token = tokenService.GenerateToken();
            return Results.Ok(new LoginResponse(token, options.Value.Jwt.ExpirationMinutes * 60));
        })
        .WithName("Login")
        .RequireRateLimiting("auth");

        group.MapPost("/refresh", (
            ITokenService tokenService,
            HttpContext context) =>
        {
            // User is already authenticated via JWT middleware
            var token = tokenService.GenerateToken();
            return Results.Ok(new RefreshResponse(token));
        })
        .WithName("RefreshToken")
        .RequireAuthorization();

        group.MapGet("/validate", () =>
        {
            // If we get here, the token is valid (middleware handled it)
            return Results.Ok(new { Valid = true });
        })
        .WithName("ValidateToken")
        .RequireAuthorization();
    }

    public record LoginRequest(string Pin);
    public record LoginResponse(string Token, int ExpiresIn);
    public record RefreshResponse(string Token);
}
