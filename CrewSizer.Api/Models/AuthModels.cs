namespace CrewSizer.Api.Models;

public record LoginRequest(string UserName, string Password);
public record RegisterRequest(string UserName, string Password, string? NomComplet);
public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string UserName,
    string? NomComplet);
