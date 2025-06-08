using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class Registration
{
    public string RegId { get; set; } = null!;

    public string? VolunteerId { get; set; }

    public string? EventId { get; set; }

    public string? Status { get; set; }

    public DateOnly? RegisterAt { get; set; }

    public virtual ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();

    public virtual Event? Event { get; set; }

    public virtual Volunteer? Volunteer { get; set; }
}
