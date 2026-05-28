using System.ComponentModel.DataAnnotations;

namespace Core.DTOs;

public class ContactRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MinLength(2, ErrorMessage = "Name is too short")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required")]
    [Phone(ErrorMessage = "Invalid phone format")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message is required")]
    [MinLength(10, ErrorMessage = "Message is too short")]
    public string Message { get; set; } = string.Empty;
}
