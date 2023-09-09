using System.ComponentModel.DataAnnotations;

namespace API_Students.Models.DTOs
{
    public class UserRegistrationRequest
    {
        [Required]
        public string? Name { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }
    }
}
