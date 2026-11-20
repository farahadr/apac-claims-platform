namespace ClaimsApi.Features.Claims;

// One row per status change, so we keep the full history of a claim.
public class ClaimEvent
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
    public Claim Claim { get; set; } = null!;

    // Null for the very first event (when the claim is submitted).
    public ClaimStatus? FromStatus { get; set; }
    public ClaimStatus ToStatus { get; set; }

    public string ChangedBy { get; set; } = "";
    public DateTime ChangedAt { get; set; }

    // Optional free text, e.g. the reason for a rejection.
    public string? Note { get; set; }
}
