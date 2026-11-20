using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClaimsApi.Features.Claims;

// The message we put on the "claim-events" Kafka topic.
// FromStatus is null when the claim was just created.
public record ClaimStatusChanged(
    Guid EventId,
    Guid ClaimId,
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    decimal EstimatedAmount,
    string? AssignedOfficer,
    DateTime ChangedAt)
{
    // Same JSON style as the API: camelCase names, enums as text.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static ClaimStatusChanged From(Claim claim, ClaimEvent ev) => new(
        ev.Id, claim.Id, ev.FromStatus, ev.ToStatus,
        claim.EstimatedAmount, claim.AssignedOfficer, ev.ChangedAt);
}
