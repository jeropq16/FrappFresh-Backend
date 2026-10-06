using System.Text.Json;
using FrappFresh.Application.DTOs.Auth;
using FrappFresh.Application.DTOs.Users;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Enums;
using FrappFresh.Infrastructure.Persistence;
using FrappFresh.Tests.Infrastructure;
using FrappFresh.Tests.Validators;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FrappFresh.Tests.Services;

[Collection("MySql")]
public class AuthAndUserServiceTests(MySqlFixture database)
{
    [MySqlFact]
    public async Task RegistrationAndLogin_NormalizeEmail_HashPassword_AndNeverReturnSensitiveFields()
    {
        await using var host = new ApplicationTestHost(database);
        var request = ApplicationValidatorTests.Registration();
        request.Email = "  " + request.Email.ToUpperInvariant() + "  ";
        await using var scope = host.Scope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var registered = await auth.RegisterAsync(request);
        Assert.Equal(UserRole.Customer, registered.User.Role);
        Assert.Equal(request.Email.Trim().ToLowerInvariant(), registered.User.Email);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Users.SingleAsync(x => x.Id == registered.User.Id);
        Assert.NotEqual(request.Password, stored.PasswordHash);
        Assert.DoesNotContain("PasswordHash", JsonSerializer.Serialize(registered));
        Assert.DoesNotContain("TokenHash", JsonSerializer.Serialize(registered));

        var login = await auth.LoginAsync(new LoginRequestDto { Email = request.Email, Password = request.Password });
        Assert.Equal(registered.User.Id, login.User.Id);
        await Assert.ThrowsAsync<ConflictException>(() => auth.RegisterAsync(request));
        await Assert.ThrowsAsync<UnauthorizedException>(() => auth.LoginAsync(new LoginRequestDto { Email = request.Email, Password = "wrong" }));
    }

    [MySqlFact]
    public async Task Auth_RefreshesOnce_AndLogoutPreventsRenewal()
    {
        await using var host = new ApplicationTestHost(database);
        await using var scope = host.Scope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var registered = await auth.RegisterAsync(ApplicationValidatorTests.Registration());
        var first = new RefreshTokenRequestDto { RefreshToken = registered.Tokens.RefreshToken };
        var next = await auth.RefreshAsync(first);
        await Assert.ThrowsAsync<UnauthorizedException>(() => auth.RefreshAsync(first));
        var replacement = new RefreshTokenRequestDto { RefreshToken = next.RefreshToken };
        await auth.LogoutAsync(replacement);
        await Assert.ThrowsAsync<UnauthorizedException>(() => auth.RefreshAsync(replacement));
    }

    [MySqlFact]
    public async Task UserAccess_RejectsAnonymousAndOtherUsers_AndAllowsAdmin()
    {
        await using var host = new ApplicationTestHost(database);
        var user = await host.AddUserAsync();
        var other = await host.AddUserAsync();
        var admin = await host.AddUserAsync(UserRole.Admin);
        await using (var anonymous = host.Scope())
            await Assert.ThrowsAsync<UnauthorizedException>(() => anonymous.ServiceProvider.GetRequiredService<IUserService>().GetMeAsync());
        await using (var customer = host.Scope(user.Id))
        {
            var service = customer.ServiceProvider.GetRequiredService<IUserService>();
            Assert.Equal(user.Id, (await service.GetMeAsync()).Id);
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(other.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAllAsync());
            await Assert.ThrowsAsync<ForbiddenException>(() => service.ChangeRoleAsync(user.Id, new ChangeUserRoleRequestDto { Role = UserRole.Admin }));
        }
        await using var manager = host.Scope(admin.Id);
        Assert.Equal(other.Id, (await manager.ServiceProvider.GetRequiredService<IUserService>().GetByIdAsync(other.Id)).Id);
    }

    [MySqlFact]
    public async Task PasswordChange_RequiresOldPassword_AndRevokesAllRefreshTokens()
    {
        await using var host = new ApplicationTestHost(database);
        var user = await host.AddUserAsync();
        await using var scope = host.Scope(user.Id);
        var sessions = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        var one = await sessions.CreateAsync(user.Id);
        var two = await sessions.CreateAsync(user.Id);
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var request = new ChangePasswordRequestDto
        {
            CurrentPassword = "wrong", NewPassword = "NuevaPassword123!", ConfirmPassword = "NuevaPassword123!"
        };
        await Assert.ThrowsAsync<UnauthorizedException>(() => users.ChangePasswordAsync(request));
        request.CurrentPassword = "PasswordDePrueba123!";
        await users.ChangePasswordAsync(request);
        Assert.Null(await sessions.RotateAsync(one.RefreshToken));
        Assert.Null(await sessions.RotateAsync(two.RefreshToken));
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        await Assert.ThrowsAsync<UnauthorizedException>(() => auth.LoginAsync(new LoginRequestDto { Email = user.Email, Password = request.CurrentPassword }));
        Assert.Equal(user.Id, (await auth.LoginAsync(new LoginRequestDto { Email = user.Email, Password = request.NewPassword })).User.Id);
    }

    [MySqlFact]
    public async Task AdminRoleAndDeactivation_RevokeSessions_AndAdminCannotDisableItself()
    {
        await using var host = new ApplicationTestHost(database);
        var admin = await host.AddUserAsync(UserRole.Admin);
        var customer = await host.AddUserAsync();
        await using var scope = host.Scope(admin.Id);
        var sessions = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();
        var original = await sessions.CreateAsync(customer.Id);
        var users = scope.ServiceProvider.GetRequiredService<IUserService>();
        var updated = await users.ChangeRoleAsync(customer.Id, new ChangeUserRoleRequestDto { Role = UserRole.Admin });
        Assert.Equal(UserRole.Admin, updated.Role);
        Assert.Null(await sessions.RotateAsync(original.RefreshToken));
        await Assert.ThrowsAsync<BusinessRuleException>(() => users.DeleteAsync(admin.Id));
        await Assert.ThrowsAsync<BusinessRuleException>(() => users.ChangeRoleAsync(admin.Id, new ChangeUserRoleRequestDto { Role = UserRole.Customer }));
        await users.DeleteAsync(customer.Id);
        Assert.False((await users.GetByIdAsync(customer.Id)).IsActive);
        await Assert.ThrowsAsync<UnauthorizedException>(() => scope.ServiceProvider.GetRequiredService<IAuthService>()
            .LoginAsync(new LoginRequestDto { Email = customer.Email, Password = "PasswordDePrueba123!" }));
    }

    [MySqlFact]
    public async Task DuplicateProfileEmail_DoesNotPartiallyUpdateUser()
    {
        await using var host = new ApplicationTestHost(database);
        var user = await host.AddUserAsync();
        var other = await host.AddUserAsync();
        await using var scope = host.Scope(user.Id);
        var service = scope.ServiceProvider.GetRequiredService<IUserService>();
        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(user.Id, new UpdateUserRequestDto
        {
            FirstName = "Changed", LastName = "Changed", Email = other.Email, PhoneNumber = "573001234567"
        }));
        Assert.Equal(user.FirstName, (await service.GetMeAsync()).FirstName);
    }
}
