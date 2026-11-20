namespace ClaimsApi.Features.Claims;

public enum ClaimType
{
    Motor,
    Property
}

public enum ClaimStatus
{
    Submitted,
    Assigned,
    UnderReview,
    InfoRequested,
    Approved,
    Rejected,
    Settled
}

public class Claim
{
    public Guid Id { get; set; }
    public ClaimType Type { get; set; }
    public string Market { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal EstimatedAmount { get; set; }
    public ClaimStatus Status { get; set; } = ClaimStatus.Submitted;

    // No real login (see DESIGN.md), so people are stored as plain names.
    public string Claimant { get; set; } = "";
    public string? AssignedOfficer { get; set; }

    // The day the incident happened (no time part). Not the same as
    // CreatedAt, which is when the claim was submitted.
    public DateOnly IncidentDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ClaimEvent> Events { get; set; } = [];
}
