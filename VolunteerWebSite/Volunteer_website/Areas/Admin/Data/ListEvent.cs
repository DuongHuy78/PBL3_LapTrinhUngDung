namespace Volunteer_website.Areas.Admins.Data
{
    public class ListEvent
    {
        public string EventId { get; set; } = null!;

        public string? OrgId { get; set; }

        public string? TypeEvent { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }

        public DateOnly? DayBegin { get; set; }

        public DateOnly? DayEnd { get; set; }

        public string? Location { get; set; }

        public int? TargetMember { get; set; }

        public int? CurrentMember { get; set; }

        public int? TargetFunds { get; set; }

        public Decimal? CurrentFunds { get; set; }

        public int? IsActive { get; set; }

        public string? Status { get; set; }
    }
}
