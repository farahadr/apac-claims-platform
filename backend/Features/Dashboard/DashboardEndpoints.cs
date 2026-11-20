using ClaimsApi.Data;
using ClaimsApi.Features.Claims;
using Microsoft.EntityFrameworkCore;

namespace ClaimsApi.Features.Dashboard;

public record DashboardSummary(int OpenClaims, decimal TotalExposure, List<OfficerWorkload> OpenClaimsPerOfficer);

public record OfficerWorkload(string Officer, int OpenClaims);

public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app) =>
        app.MapGet("/api/dashboard/summary", GetSummary).WithTags("Dashboard");

    // GET /api/dashboard/summary — reads only the dashboard table, never Claims.
    private static async Task<DashboardSummary> GetSummary(ClaimsDbContext db)
    {
        // "Open" = anything not finished (Rejected and Settled are closed).
        var open = db.DashboardClaims
            .Where(c => c.Status != ClaimStatus.Rejected && c.Status != ClaimStatus.Settled);

        var count = await open.CountAsync();
        var exposure = await open.SumAsync(c => c.EstimatedAmount);

        var perOfficer = await open
            .GroupBy(c => c.AssignedOfficer)
            .Select(g => new { Officer = g.Key, Count = g.Count() })
            .ToListAsync();

        var workload = perOfficer
            .Select(o => new OfficerWorkload(o.Officer ?? "Unassigned", o.Count))
            .OrderByDescending(o => o.OpenClaims)
            .ToList();

        return new DashboardSummary(count, exposure, workload);
    }
}
