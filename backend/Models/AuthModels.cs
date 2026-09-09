namespace backend.Models;

public record RegisterRequest(string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserResponse(string Id, string Email, int PasskeyCount);