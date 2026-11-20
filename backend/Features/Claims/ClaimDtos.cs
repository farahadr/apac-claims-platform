namespace ClaimsApi.Features.Claims;

// What the client sends to create a claim. Fields are nullable so we can
// tell "missing" apart from a real value and return a clear error.
public record CreateClaimRequest(
    ClaimType? Type,
    string? Market,
    string? Description,
    decimal? EstimatedAmount,
    DateOnly? IncidentDate,
    string? Claimant);

// One row in the claims list (no description or history, to keep it light).
public record ClaimSummary(
    Guid Id,
    ClaimType Type,
    string Market,
    decimal EstimatedAmount,
    ClaimStatus Status,
    string Claimant,
    string? AssignedOfficer,
    DateOnly IncidentDate,
    DateTime CreatedAt);

// Full details of one claim, including its status history.
public record ClaimDetails(
    Guid Id,
    ClaimType Type,
    string Market,
    string Description,
    decimal EstimatedAmount,
    ClaimStatus Status,
    string Claimant,
    string? AssignedOfficer,
    DateOnly IncidentDate,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<ClaimEventResponse> Events);

public record ClaimEventResponse(
    ClaimStatus? FromStatus,
    ClaimStatus ToStatus,
    string ChangedBy,
    DateTime ChangedAt);
