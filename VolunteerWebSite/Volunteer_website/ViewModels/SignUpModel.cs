using System.ComponentModel.DataAnnotations;

namespace Volunteer_website.ViewModels
{
    public class SignUpModel
    {
        public int VolunteerId { get; set; }

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public bool? Gender { get; set; }

        public string? ImagePath { get; set; }

        public string? Address { get; set; }
        [Required]
        public string? UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [DataType(DataType.Password)]
        public string? ConfirmPassword { get; set; }

    }
}
