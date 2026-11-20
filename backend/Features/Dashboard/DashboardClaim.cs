using ClaimsApi.Features.Claims;

namespace ClaimsApi.Features.Dashboard;

// The dashboard's own copy of each claim, built only from Kafka messages.
// One row per claim holding its latest state; the summary is added up from these.
public class DashboardClaim
{
    public Guid ClaimId { get; set; }
    public ClaimStatus Status { get; set; }
    public decimal EstimatedAmount { get; set; }
    public string? AssignedOfficer { get; set; }

    // Time of the last message applied, so old or repeated messages are ignored.
    public DateTime LastChangedAt { get; set; }
}
