namespace Volunteer_website.ViewModels
{
    public class Volunteer_List
    {
        public string VolunteerId { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public DateOnly? JoinDate { get; set; }
    }
}
