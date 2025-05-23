using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Volunteer_website.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage ="Username cannot be blank.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Password cannot be blank.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
