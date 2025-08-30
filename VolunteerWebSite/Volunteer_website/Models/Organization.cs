using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class Organization
{
    public string? OrgId { get; set; } = null!;

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? ImagePath { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Event>? Events { get; set; } = new List<Event>();
}
