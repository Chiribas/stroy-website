namespace Core.DTOs;

public record AuthResponse(string Token, DateTime ExpiresAt);
