using System.ComponentModel.DataAnnotations;

namespace Volunteer_website.ViewModels
{
    public class Update_ContactModel
    {
        public string? VolunteerId { get; set; }
        public string? UserName { get; set; } 
        public String? Name { get; set; }

        public DateOnly? DateOfBirth { get; set; }
        public bool? Gender { get; set; }
        public String? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public String? Email { get; set; }
       
        public string? Address { get; set; }
        [Display(Name = "Ảnh đại diện")]
        public IFormFile? AvatarFile { get; set; }

        public string? AvatarPath { get; set; } 
    }
}
