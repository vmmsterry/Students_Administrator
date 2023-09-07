using System.ComponentModel.DataAnnotations;

namespace API_Students.Models.DTOs
{
    public class UserLoginRequest
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}