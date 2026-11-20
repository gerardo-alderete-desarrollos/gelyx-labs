using GelyxLabs.Api;
using GelyxLabs.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GelyxLabs.IntegrationTests;

internal static class TestUserFactory
{
    private const string DefaultPassword = "Test12345678";

    public static async Task<TestUserCredentials> CreateAsync(
        WebApplicationFactory<Program> factory,
        string role = "Usuario",
        bool isActive = true,
        bool isDeleted = false,
        bool lockout = false)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var testRole = await roleManager.FindByNameAsync(role);
        if (testRole is null)
        {
            var roleResult = await roleManager.CreateAsync(new AppRole { Name = role });
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Unable to create test role '{role}': {string.Join(", ", roleResult.Errors.Select(error => error.Description))}");
            }
        }

        var email = $"test.{Guid.NewGuid():N}@example.com";
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "Integration Test User",
            IsActive = isActive,
            IsDeleted = isDeleted
        };

        var createResult = await userManager.CreateAsync(user, DefaultPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to create test user: {string.Join(", ", createResult.Errors.Select(error => error.Description))}");
        }

        var roleResultForUser = await userManager.AddToRoleAsync(user, role);
        if (!roleResultForUser.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to assign test role '{role}': {string.Join(", ", roleResultForUser.Errors.Select(error => error.Description))}");
        }

        if (lockout)
        {
            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
        }

        return new TestUserCredentials(email, DefaultPassword);
    }

    public static async Task SetDeletedAsync(WebApplicationFactory<Program> factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Test user '{email}' was not found.");
        user.IsDeleted = true;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unable to mark test user deleted: {string.Join(", ", updateResult.Errors.Select(error => error.Description))}");
        }
    }

    public static async Task<string> GeneratePasswordResetTokenAsync(
        WebApplicationFactory<Program> factory,
        string email)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email)
            ?? throw new InvalidOperationException($"Test user '{email}' was not found.");
        return await userManager.GeneratePasswordResetTokenAsync(user);
    }
}

internal sealed record TestUserCredentials(string Email, string Password);
