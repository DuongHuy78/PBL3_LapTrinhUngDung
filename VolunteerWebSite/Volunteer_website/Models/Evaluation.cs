using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class Evaluation
{
    public string EvaluationId { get; set; } = null!;

    public string RegId { get; set; } = null!;

    public bool IsCompleted { get; set; }

    public string? Feedback { get; set; }

    public DateTime EvaluatedAt { get; set; }

    public virtual Registration Reg { get; set; } = null!;
}
