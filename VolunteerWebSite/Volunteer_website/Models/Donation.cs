using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class Donation
{
    public string DonationId { get; set; } = null!;

    public string? VolunteerId { get; set; }

    public string? EventId { get; set; }

    public decimal? Amount { get; set; }

    public string? Message { get; set; }

    public DateTime? DonationDate { get; set; }

    public virtual Event? Event { get; set; }

    public virtual Volunteer? Volunteer { get; set; }
}
