using System;
using System.Collections.Generic;

namespace Volunteer_website.Models;

public partial class Admin
{
    public string? AdminId { get; set; } = null!;

    public string? Name { get; set; }

    public string? ImgPath { get; set; }

    public string? Email { get; set; }
}
