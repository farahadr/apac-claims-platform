using ClaimsApi.Data;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Features.Claims;

public static class ClaimEndpoints
{
    // Called from Program.cs. Puts all claim routes under /api/claims.
    public static void MapClaimEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/claims").WithTags("Claims");

        group.MapPost("/", CreateClaim);
        group.MapGet("/", ListClaims);
        group.MapGet("/{id:guid}", GetClaim).WithName("GetClaim");
        group.MapPost("/{id:guid}/assign", AssignClaim);
        group.MapPost("/{id:guid}/status", ChangeStatus);
    }

    // POST /api/claims/{id}/assign  — Submitted → Assigned, and sets the officer.
    private static async Task<IResult> AssignClaim(Guid id, AssignClaimRequest request, ClaimsDbContext db, ClaimEventPublisher publisher)
    {
        var errors = new Dictionary<string, string[]>();
        CheckText(errors, request.Officer, "officer", 100);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var claim = await LoadWithEvents(db, id);
        if (claim is null)
            return Results.NotFound();

        if (!ClaimTransitions.IsAllowed(claim.Status, ClaimStatus.Assigned))
            return TransitionError(claim.Status, ClaimStatus.Assigned);

        var officer = request.Officer!.Trim();
        claim.AssignedOfficer = officer;
        // No login yet, so we record the officer as the person who made the change.
        var ev = RecordStatusChange(db, claim, ClaimStatus.Assigned, officer, note: null);

        await db.SaveChangesAsync();
        publisher.Publish(ClaimStatusChanged.From(claim, ev));
        return Results.Ok(ToDetails(claim));
    }

    // POST /api/claims/{id}/status  — every other move in the status flow.
    private static async Task<IResult> ChangeStatus(Guid id, ChangeStatusRequest request, ClaimsDbContext db, ClaimEventPublisher publisher)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Status is null || !Enum.IsDefined(request.Status.Value))
            errors["status"] = ["Status is required."];
        else if (request.Status == ClaimStatus.Assigned)
            errors["status"] = ["Use POST /api/claims/{id}/assign to assign a claim, so the officer is recorded."];
        CheckText(errors, request.ChangedBy, "changedBy", 100);
        if (request.Note?.Trim().Length > 1000)
            errors["note"] = ["note must be at most 1000 characters."];
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var claim = await LoadWithEvents(db, id);
        if (claim is null)
            return Results.NotFound();

        var to = request.Status!.Value;
        if (!ClaimTransitions.IsAllowed(claim.Status, to))
            return TransitionError(claim.Status, to);

        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        var ev = RecordStatusChange(db, claim, to, request.ChangedBy!.Trim(), note);

        await db.SaveChangesAsync();
        publisher.Publish(ClaimStatusChanged.From(claim, ev));
        return Results.Ok(ToDetails(claim));
    }

    private static Task<Claim?> LoadWithEvents(ClaimsDbContext db, Guid id) =>
        db.Claims.Include(c => c.Events).FirstOrDefaultAsync(c => c.Id == id);

    // Changes the status and writes the matching history row.
    private static ClaimEvent RecordStatusChange(ClaimsDbContext db, Claim claim, ClaimStatus to, string changedBy, string? note)
    {
        var now = DateTime.UtcNow;
        var ev = new ClaimEvent
        {
            Id = Guid.NewGuid(),
            ClaimId = claim.Id,
            FromStatus = claim.Status,
            ToStatus = to,
            ChangedBy = changedBy,
            ChangedAt = now,
            Note = note,
        };
        // Add the event explicitly so EF Core knows it's a new row to INSERT.
        db.ClaimEvents.Add(ev);
        claim.Events.Add(ev);

        claim.Status = to;
        claim.UpdatedAt = now;
        return ev;
    }

    private static IResult TransitionError(ClaimStatus from, ClaimStatus to) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Status change not allowed.",
            detail: ClaimTransitions.Explain(from, to));

    // POST /api/claims
    private static async Task<IResult> CreateClaim(CreateClaimRequest request, ClaimsDbContext db, ClaimEventPublisher publisher)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var now = DateTime.UtcNow;
        var claim = new Claim
        {
            Id = Guid.NewGuid(),
            Type = request.Type!.Value,
            Market = request.Market!.Trim(),
            Description = request.Description!.Trim(),
            EstimatedAmount = request.EstimatedAmount!.Value,
            IncidentDate = request.IncidentDate!.Value,
            Claimant = request.Claimant!.Trim(),
            Status = ClaimStatus.Submitted,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Every claim starts its history with a "Submitted" event.
        var submitted = new ClaimEvent
        {
            Id = Guid.NewGuid(),
            FromStatus = null,
            ToStatus = ClaimStatus.Submitted,
            ChangedBy = claim.Claimant,
            ChangedAt = now,
        };
        claim.Events.Add(submitted);

        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        publisher.Publish(ClaimStatusChanged.From(claim, submitted)); // only after the save worked

        // 201 Created, with a Location header pointing at GET /api/claims/{id}.
        return Results.CreatedAtRoute("GetClaim", new { id = claim.Id }, ToDetails(claim));
    }

    // GET /api/claims?status=Submitted&claimant=Jane
    private static async Task<IResult> ListClaims(ClaimsDbContext db, ClaimStatus? status, string? claimant)
    {
        var query = db.Claims.AsQueryable();

        if (status is not null)
            query = query.Where(c => c.Status == status);
        if (!string.IsNullOrWhiteSpace(claimant))
            query = query.Where(c => c.Claimant == claimant.Trim());

        var claims = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ClaimSummary(
                c.Id, c.Type, c.Market, c.EstimatedAmount, c.Status,
                c.Claimant, c.AssignedOfficer, c.IncidentDate, c.CreatedAt))
            .ToListAsync();

        return Results.Ok(claims);
    }

    // GET /api/claims/{id}
    private static async Task<IResult> GetClaim(Guid id, ClaimsDbContext db)
    {
        var claim = await db.Claims
            .AsNoTracking()
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == id);

        return claim is null ? Results.NotFound() : Results.Ok(ToDetails(claim));
    }

    private static ClaimDetails ToDetails(Claim c) => new(
        c.Id, c.Type, c.Market, c.Description, c.EstimatedAmount, c.Status,
        c.Claimant, c.AssignedOfficer, c.IncidentDate, c.CreatedAt, c.UpdatedAt,
        c.Events
            .OrderBy(e => e.ChangedAt)
            .Select(e => new ClaimEventResponse(e.FromStatus, e.ToStatus, e.ChangedBy, e.ChangedAt, e.Note))
            .ToList());

    // Returns field name -> error messages. Empty means the request is valid.
    // Limits match the database columns in ClaimsDbContext.
    private static Dictionary<string, string[]> Validate(CreateClaimRequest r)
    {
        var errors = new Dictionary<string, string[]>();
        void Add(string field, string message) => errors[field] = [message];

        if (r.Type is null || !Enum.IsDefined(r.Type.Value))
            Add("type", "Type is required (Motor or Property).");

        CheckText(errors, r.Market, "market", 50);
        CheckText(errors, r.Description, "description", 2000);
        CheckText(errors, r.Claimant, "claimant", 100);

        if (r.EstimatedAmount is not { } amount)
            Add("estimatedAmount", "Estimated amount is required.");
        else if (amount <= 0)
            Add("estimatedAmount", "Estimated amount must be greater than 0.");
        else if (decimal.Round(amount, 2) != amount)
            Add("estimatedAmount", "Estimated amount can have at most 2 decimal places.");
        else if (amount >= 10_000_000_000_000_000m) // numeric(18,2) holds 16 digits before the point
            Add("estimatedAmount", "Estimated amount is too large.");

        // Same rule as the database check constraint, but with a friendly message.
        // Uses the UTC date, matching CURRENT_DATE in the database (which runs in UTC).
        if (r.IncidentDate is not { } incidentDate)
            Add("incidentDate", "Incident date is required.");
        else if (incidentDate > DateOnly.FromDateTime(DateTime.UtcNow))
            Add("incidentDate", "Incident date cannot be in the future.");

        return errors;
    }

    // Shared check for required text fields (used by every request).
    private static void CheckText(Dictionary<string, string[]> errors, string? value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors[field] = [$"{field} is required."];
        else if (value.Trim().Length > maxLength)
            errors[field] = [$"{field} must be at most {maxLength} characters."];
    }
}
