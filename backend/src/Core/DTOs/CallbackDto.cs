namespace Core.DTOs;

public record CallbackDto(int Id, string Phone, string? Name, DateTime CreatedAt, bool IsProcessed);
