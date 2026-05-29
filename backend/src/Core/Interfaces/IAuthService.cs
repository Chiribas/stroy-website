using Core.DTOs;

namespace Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> AuthenticateAsync(LoginRequest request);
}
