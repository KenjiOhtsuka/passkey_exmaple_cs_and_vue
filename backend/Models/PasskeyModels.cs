namespace backend.Models;

public record PasskeyLoginOptionsRequest(string? Username);

public record PasskeyRegistrationRequest(string CredentialJson, string? Name);

public record PasskeyLoginRequest(string CredentialJson);

public record PasskeyInfoResponse(
    string Id,
    string Name,
    DateTimeOffset CreatedAt,
    bool IsBackedUp,
    bool IsUserVerified);