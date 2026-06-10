using LedGerSystem.Entities;

namespace LedGerSystem.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error)> LoginAsync(string userName, string password);

    Task EnsureBootstrapUserAsync();

    Task<SysUser?> GetByUserNameAsync(string userName);
}
