# AI Working Journal

1.  Asked AI how to start. It suggested a single .NET backend with modules instead of several services. 
  - I agreed: simpler for a few hours' work, and I can explain how to split it later.

**Choosing the stack**
1. I wanted to use Laragon for Postgres. AI suggested Docker instead so reviewers can run it. 
 - I agreed, because they won't have Laragon.
 - Docker Desktop was slow to start and I was worried about disk space (32 GB free). AI said that was enough and suggested using my own Postgres as a fallback while Docker started. Docker came up in the end, so I used it.
- Mapped Postgres to port 5433 instead of 5432 so it doesn't clash with my local Postgres.

2. I've never used .NET, Spring Boot, Kafka or Angular. My background is TypeScript/HTTP.

3. AI suggested C#/.NET over Spring Boot because C# syntax is close to TypeScript. 
- Accepted: it should make the code easier for me to read and explain.

**Repo and tools**
1. Normally I'd put projects in C:\laragon\www. AI suggested a separate folder since this isn't a Laragon project. 
- Accepted. Used GitHub Desktop + VS Code (my usual workflow) instead of the git commands AI first gave me.

**Package version error**
1. AI's install commands didn't specify versions. NuGet installed EF Core Design v11 and dotnet-ef v11, which don't work with .NET 10 (error NU1202).
- Fixed by pinning both to 10.0.4 to match the EF Core version Npgsql installed. 
**Lesson: always pin package versions to the framework version.**

**Continue Create**
1. Claude Code set cascade delete (deleting a claim deletes its history).
- I asked Claude (chat) to review the change; it flagged that claim history is an audit trail.
- I agreed and had Claude Code change it to Restrict.

2. Reviewed the Claims table: it had CreatedAt but no incident date.
- Added IncidentDate — insurers need when the incident happened (to check coverage), not just when it was reported. 

3. Claude Code built the create/list/get claim endpoints. It returns DTOs instead of database classes. 
- Accepted: the API shape won't change by accident when the database changes. I tested the endpoints myself with REST Client and the browser.

4. I reviewed and kept the list of allowed status transitions
  based on my DESIGN.md flow (e.g. Rejected and Settled are final, a claim can't skip from Submitted to Approved). Claude Code implemented it as one lookup table, so the rules are in one place.
- Accepted that /status refuses "Assigned": a claim should only become Assigned through /assign, which requires an officer name.

5. Claude Code added an optional Note to status changes. 
- Accepted: needed so an officer can say what info is requested or why a claim is rejected.

6. Claude Code suggested adding concurrency protection (two officers editing at once). 
- Decided to defer it for time and documented it in DESIGN.md. Kafka is more important for this brief.

7. Asked Claude Code to add Kafka: publish ClaimStatusChanged after the DB save, a background consumer updates a dashboard table, GET/api/dashboard/summary reads it. 
- REST = what users wait for
- Kafka = dashboard updates in the background.

8. I asked for running totals. Claude Code changed it to one row per claim, because Kafka can deliver a message twice and running totals would double-count. 
- Accepted: duplicates are now harmless (idempotent).

9. Claude Code found that messages can be lost if Kafka is down > 2 min. It suggested an outbox pattern. 
- I deferred it for time and documented it as a known gap in DESIGN.md.

10. Tested the full flow myself with REST Client: created a claim, checked the dashboard (counted in open claims and exposure), assigned it, moved it to UnderReview, rejected it, and checked the dashboard again (it dropped out of the totals). Also tried skipping from Submitted to Approved and got a 400 error, as my transition rules require.

11. Claude Code said it had added the flow requests to ClaimsApi.http, but my file didn't have them.
- Replaced the file with a full test sequence (with the help of Claude chat) that captures the claim id automatically.

12. Sample data used market "UAE", but the brief is about Chubb APAC.
- Changed it to Malaysia.

13. Kafka container was unstable on my machine. Asked Claude Code to fix the docker-compose settings so reviewers get a stable setup.

14. Kafka container was unstable on my machine. Claude Code found the cause: 
- Kafka's default 1 GB memory on a Docker VM with only 1.6 GB.
- Accepted its fix: smaller heap (256 MB), data saved in a volume so it survives restarts, explicit single-node settings. Kafka now restarts cleanly. Startup can still be slow on a low-memory laptop.

15. Claude Code's clean-up command stopped my running API too (it killed every ClaimsApi process by name, not just its own). No data lost. It changed to stopping only processes it started.

16. Claude Code made the app apply database migrations at startup, so reviewers only need docker compose up and dotnet run. 
- Accepted.

17. Asked Claude Code for the README. I filled in the time-spent section.