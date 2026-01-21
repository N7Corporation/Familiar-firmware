namespace Familiar.Host.Options;

public class SecurityOptions
{
    public const string SectionName = "Familiar:Security";

    public string Pin { get; set; } = "1234";
    public JwtOptions Jwt { get; set; } = new();
    public RateLimitingOptions RateLimiting { get; set; } = new();
}

public class JwtOptions
{
    public string Key { get; set; } = "familiar-firmware-secret-key-min-32-chars";
    public string Issuer { get; set; } = "familiar-firmware";
    public string Audience { get; set; } = "familiar-clients";
    public int ExpirationMinutes { get; set; } = 480;
}

public class RateLimitingOptions
{
    public int PermitLimit { get; set; } = 100;
    public int WindowMinutes { get; set; } = 1;
}
