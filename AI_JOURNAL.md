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