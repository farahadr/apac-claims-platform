namespace ClaimsApi.Features.Claims;

// The claim status flow, in one place. If a move isn't listed here, it's not allowed.
public static class ClaimTransitions
{
    private static readonly Dictionary<ClaimStatus, ClaimStatus[]> Allowed = new()
    {
        [ClaimStatus.Submitted] = [ClaimStatus.Assigned],
        [ClaimStatus.Assigned] = [ClaimStatus.UnderReview],
        [ClaimStatus.UnderReview] = [ClaimStatus.InfoRequested, ClaimStatus.Approved, ClaimStatus.Rejected],
        [ClaimStatus.InfoRequested] = [ClaimStatus.UnderReview],
        [ClaimStatus.Approved] = [ClaimStatus.Settled],
        [ClaimStatus.Rejected] = [], // closed
        [ClaimStatus.Settled] = [],  // closed
    };

    public static bool IsAllowed(ClaimStatus from, ClaimStatus to) => Allowed[from].Contains(to);

    // A message like "Cannot change status from Rejected to Approved. ..." for 400 errors.
    public static string Explain(ClaimStatus from, ClaimStatus to)
    {
        var next = Allowed[from];
        var options = next.Length == 0
            ? $"{from} is a closed status, so it cannot change."
            : $"From {from} the claim can only move to: {string.Join(", ", next)}.";
        return $"Cannot change status from {from} to {to}. {options}";
    }
}
