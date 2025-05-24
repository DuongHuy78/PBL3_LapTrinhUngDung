namespace Volunteer_website.ViewModels
{
    public class VolunteerModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // Initialize with a default value\
        public string VolunteerId { get; set; } = string.Empty; // Initialize with a default value
        public string PhoneNumber { get; set; } = string.Empty; // Initialize with a default value
        public string Email { get; set; } = string.Empty; // Initialize with a default value
        public DateOnly? DateOfBirth { get; set; } // Nullable DateOnly

    }
}
