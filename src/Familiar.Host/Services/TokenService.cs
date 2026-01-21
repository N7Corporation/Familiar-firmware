using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Familiar.Host.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Familiar.Host.Services;

public interface ITokenService
{
    string GenerateToken();
    bool ValidateToken(string token);
}

public class TokenService : ITokenService
{
    private readonly SecurityOptions _options;
    private readonly SymmetricSecurityKey _signingKey;

    public TokenService(IOptions<SecurityOptions> options)
    {
        _options = options.Value;
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Jwt.Key));
    }

    public string GenerateToken()
    {
        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "familiar-user"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _options.Jwt.Issuer,
            audience: _options.Jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.Jwt.ExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _signingKey,
                ValidateIssuer = true,
                ValidIssuer = _options.Jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
