using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class EventType
{
    public string TypeEventId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
