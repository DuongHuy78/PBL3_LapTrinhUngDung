using System.ComponentModel.DataAnnotations;

namespace Volunteer_website.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage = "UserName cannot be blank.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email cannot be blank.")]
        [EmailAddress(ErrorMessage = "Invalid email address! Please provide a valid email.")]
        public string Email { get; set; }
    }
}
