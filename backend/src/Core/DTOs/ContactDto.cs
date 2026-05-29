namespace Core.DTOs;

public record ContactDto(int Id, string Name, string Phone, string Message, DateTime CreatedAt, bool IsProcessed);
