using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class CallbackRequest
{
    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;

    public string? Name { get; set; }
}
