namespace Volunteer_website.ViewModels
{
    public class Donate_List
    {
        public int Id { get; set; }
        public string VolunteerName { get; set; } = string.Empty; // Initialize with a default value
        public string? Volunteer_Id { get; set; }
        public string EventId { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? DonationDate { get; set; }
    }
}
