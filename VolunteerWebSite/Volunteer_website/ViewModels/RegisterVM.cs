using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
namespace Volunteer_website.ViewModels
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Username cannot be blank.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password cannot be blank.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password cannot be blank.")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone number cannot be blank.")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email cannot be blank.")]
        [EmailAddress(ErrorMessage = "Invalid Email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Date of Birth is required.")]
        [DataType(DataType.Date)]
        [Column(TypeName = "date")] // Chỉ lưu ngày
        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required.")]
        public bool Gender { get; set; } = true;

        [Required(ErrorMessage = "Address cannot be blank.")]
        public string Address { get; set; }
        public string Name { get; set; }
        public string? ImagePath { get; set; }
        public int Role { get; set; }
        public bool is_active { get; set; } = true;
    }

}
