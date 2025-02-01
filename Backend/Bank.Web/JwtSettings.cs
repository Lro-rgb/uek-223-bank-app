namespace Bank.Web;

public class JwtSettings
{
    public string? Issuer { get; init; }
    public string? Audience { get; init; }
    public string? PrivateKey { get; init; }
}