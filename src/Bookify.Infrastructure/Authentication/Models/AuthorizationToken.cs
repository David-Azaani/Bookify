using System.Text.Json.Serialization;

namespace Bookify.Infrastructure.Authentication.Models;

// to authentication in admin api
public sealed class AuthorizationToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;
}