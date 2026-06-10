using LedGerSystem.Entities;
using LedGerSystem.Infrastructure;
using SqlSugar;

namespace LedGerSystem.Services;

public class AuthService(ISqlSugarClient db, IConfiguration configuration) : IAuthService
{
    private const string PendingMarker = "PENDING_SET_ON_FIRST_LOGIN";

    public async Task<(bool Success, string? Error)> LoginAsync(string userName, string password)
    {
        var user = await db.Queryable<SysUser>()
            .FirstAsync(x => x.UserName == userName && x.IsActive);

        if (user is null)
        {
            return (false, "Invalid username or password.");
        }

        if (!PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            return (false, "Invalid username or password.");
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await db.Updateable(user).UpdateColumns(x => new { x.LastLoginAt, x.UpdatedAt }).ExecuteCommandAsync();

        return (true, null);
    }

    public async Task EnsureBootstrapUserAsync()
    {
        var user = await db.Queryable<SysUser>().FirstAsync(x => x.UserName == "shamim");
        if (user is null)
        {
            var (hash, salt) = PasswordHelper.HashPassword(GetBootstrapPassword());
            var now = DateTime.UtcNow;
            await db.Insertable(new SysUser
            {
                UserName = "shamim",
                PasswordHash = hash,
                PasswordSalt = salt,
                DisplayName = "Shamim",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            }).ExecuteCommandAsync();
            return;
        }

        if (user.PasswordHash == PendingMarker || user.PasswordSalt == "PENDING")
        {
            var (hash, salt) = PasswordHelper.HashPassword(GetBootstrapPassword());
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            user.UpdatedAt = DateTime.UtcNow;
            await db.Updateable(user)
                .UpdateColumns(x => new { x.PasswordHash, x.PasswordSalt, x.UpdatedAt })
                .ExecuteCommandAsync();
        }
    }

    public async Task<SysUser?> GetByUserNameAsync(string userName)
    {
        return await db.Queryable<SysUser>().FirstAsync(x => x.UserName == userName && x.IsActive);
    }

    private string GetBootstrapPassword()
    {
        return configuration["Bootstrap:DefaultPassword"] ?? "ChangeMe123!";
    }
}
