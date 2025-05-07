using System.ComponentModel.DataAnnotations;

namespace Volunteer_website.ViewModels
{
    public class Update_ContactModel
    {
        public string VolunteerId { get; set; }
        public String Name { get; set; } = string.Empty;

     
        public String PhoneNumber { get; set; } = string.Empty;
      
        public String Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Địa chỉ không được để trống")]
        [MinLength(5, ErrorMessage = "Địa chỉ phải có ít nhất 5 ký tự")]
        public string Address { get; set; }

    }
}
