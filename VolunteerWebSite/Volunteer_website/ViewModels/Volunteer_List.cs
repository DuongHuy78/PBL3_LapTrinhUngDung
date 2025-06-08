namespace Volunteer_website.ViewModels
{
    public class Volunteer_List
    {
        public string? VolunteerId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateOnly? JoinDate { get; set; }
        public decimal TotalDonations { get; set; }
        public int EventCount { get; set; }
    }
}