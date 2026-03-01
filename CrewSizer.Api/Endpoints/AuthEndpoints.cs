using System.Collections.Concurrent;
using CrewSizer.Api.Models;
using CrewSizer.Api.Services;
using CrewSizer.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CrewSizer.Api.Endpoints;

public static class AuthEndpoints
{
    // Stockage en mémoire des refresh tokens (MVP — migrer en DB plus tard)
    private static readonly ConcurrentDictionary<string, string> RefreshTokens = new();

    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService) =>
        {
            var user = await userManager.FindByNameAsync(request.UserName);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
                return Results.Unauthorized();

            var accessToken = tokenService.GenerateAccessToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();
            RefreshTokens[refreshToken] = user.Id;

            return Results.Ok(new AuthResponse(
                accessToken,
                refreshToken,
                900, // 15 min en secondes
                user.UserName!,
                user.NomComplet));
        }).AllowAnonymous();

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService) =>
        {
            var user = new ApplicationUser
            {
                UserName = request.UserName,
                NomComplet = request.NomComplet,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code, e => e.Description)
                    .ToDictionary(g => g.Key, g => g.ToArray());
                return Results.UnprocessableEntity(new { errors });
            }

            var accessToken = tokenService.GenerateAccessToken(user);
            var refreshToken = tokenService.GenerateRefreshToken();
            RefreshTokens[refreshToken] = user.Id;

            return Results.Ok(new AuthResponse(
                accessToken,
                refreshToken,
                900,
                user.UserName!,
                user.NomComplet));
        }).AllowAnonymous();

        group.MapPost("/refresh", async (
            RefreshRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService) =>
        {
            if (!RefreshTokens.TryRemove(request.RefreshToken, out var userId))
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(userId);
            if (user is null)
                return Results.Unauthorized();

            var accessToken = tokenService.GenerateAccessToken(user);
            var newRefreshToken = tokenService.GenerateRefreshToken();
            RefreshTokens[newRefreshToken] = user.Id;

            return Results.Ok(new AuthResponse(
                accessToken,
                newRefreshToken,
                900,
                user.UserName!,
                user.NomComplet));
        }).AllowAnonymous();

        group.MapPost("/logout", (HttpContext ctx) =>
        {
            // Côté client, supprimer les tokens
            return Results.Ok();
        }).RequireAuthorization();
    }
}
