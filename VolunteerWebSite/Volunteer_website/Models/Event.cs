using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class Event
{
    public string? EventId { get; set; } = null!;

    public string? OrgId { get; set; }

    public string TypeEventId { get; set; } = null!;

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateOnly? DayBegin { get; set; }

    public DateOnly? DayEnd { get; set; }

    public string? Location { get; set; }

    public int? TargetMember { get; set; }

    public int? TargetFunds { get; set; }

    public string? ImagePath { get; set; }

    public string? ListImg { get; set; }

    public int? IsActive { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Donation>? Donations { get; set; } = new List<Donation>();

    public virtual Organization? Org { get; set; }

    public virtual ICollection<Registration>? Registrations { get; set; } = new List<Registration>();

    public virtual EventType? TypeEvent { get; set; } = null!;
}
