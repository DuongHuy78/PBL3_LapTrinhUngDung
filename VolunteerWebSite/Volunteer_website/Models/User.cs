using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class User
{
    public string? UserId { get; set; } = null!;

    public string? UserName { get; set; } = null!;

    public string? Password { get; set; } = null!;

    public int? Role { get; set; }

    public string? RandomKey { get; set; }

    public bool IsActive { get; set; }

    public DateOnly? CreateAt { get; set; }
}
