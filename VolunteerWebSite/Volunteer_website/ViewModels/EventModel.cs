using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Volunteer_website.ViewModels
{
    [Table("Event")]
    public class EventModel
    {
        public string Event_Id { get; set; } = string.Empty; // Initialize with a default value
        public string Name { get; set; } = string.Empty; // Initialize with a default value

        [Column("description")]
        public string EventDescription { get; set; } = string.Empty; // Initialize with a default value
        public string ImagePath { get; set; } = string.Empty; // Initialize with a default value
        //public virtual ICollection<EventImage> EventImages { get; set; } = new List<EventImage>();
        public DateOnly DateBegin { get; set; }
        public DateOnly DateEnd { get; set; }
        public string Location { get; set; } = string.Empty; // Initialize with a default value
        public string Organization { get; set; } = string.Empty; // Initialize with a default value
        public string OrganizationImagePath { get; set; } = string.Empty; // Initialize with a default value
        public string OrganizationDescription { get; set; } = string.Empty; // Initialize with a default value
        public string OrganizationPhone { get; set; } = string.Empty; // Initialize with a default value
        public string OrganizationEmail { get; set; } = string.Empty; // Initialize with a default value
        public int targetmember { get; set; }
        public int targetfund { get; set; }
        public int currentmember { get; set; }
        public int currentfund { get; set; }
        public string? type { get; set; } // Initialize with a default value
        //public List<EventImage> Images { get; set; } = new List<EventImage>();
        public List<Donate_List> Donations { get; set; } = new List<Donate_List>();

    }

    public class Donation
    {
        public string VolunteerName { get; set; } = string.Empty; // Initialize with a default value
        public Decimal Amount { get; set; }
        public DateTime DonationDate { get; set; }
    }
    
}
