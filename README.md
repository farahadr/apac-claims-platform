# apac-claims-platform

A small claims platform built for the take-home assessment. Claimants submit claims; claims officers assign, review and decide them through a fixed status flow (Submitted → Assigned → UnderReview → Approved/Rejected → Settled). Every status change is kept as an audit trail. Each change is also published to Kafka, and a background consumer keeps a dashboard of open claims, total exposure and workload per officer. The backend is a single .NET 10 Web API backed by PostgreSQL.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

## How to run

```sh
# 1. Start PostgreSQL (port 5433) and Kafka (port 9092)
docker compose up -d

# 2. Wait about 1 minute for Kafka to finish starting

# 3. Run the API
cd backend
dotnet run
```

The API listens on http://localhost:5086. Database tables are created automatically on startup.

On the very first run you may see a `fail: ... SELECT "MigrationId"` log line: it's harmless (EF Core checking for its migrations table before creating it).

### Creating new migrations

Only needed if you change the data model. The repo pins `dotnet-ef` 10.0.4 as a local tool:

```sh
cd backend
dotnet tool restore
dotnet tool run dotnet-ef migrations add <Name> --output-dir Data/Migrations
```

## How to try it

Open [backend/ClaimsApi.http](backend/ClaimsApi.http) in VS Code with the [REST Client](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) extension (or in Visual Studio / Rider), and send the requests in order:

1. **Create a claim**: the new claim's id is picked up automatically for the next requests.
2. **Dashboard summary**: the claim shows as open. The dashboard is updated through Kafka, so allow a second or two.
3. **Assign → UnderReview → Reject**: each step returns the claim with its history.
4. **Dashboard summary** again: the rejected claim no longer counts as open.

The last requests in the file show the validation errors: an invalid status change, and a claim with bad data.

## Endpoints

| Method | Path | What it does |
|---|---|---|
| POST | `/api/claims` | Submit a claim (status `Submitted`) |
| GET | `/api/claims?status=&claimant=` | List claims, newest first, with optional filters |
| GET | `/api/claims/{id}` | One claim with its full status history |
| POST | `/api/claims/{id}/assign` | Assign an officer (Submitted → Assigned) |
| POST | `/api/claims/{id}/status` | Change status; invalid moves return 400 |
| GET | `/api/dashboard/summary` | Open claims, total exposure, open claims per officer |

## More

- [DESIGN.md](DESIGN.md): data model, status flow, REST vs Kafka, and what was left out on purpose
- [AI_JOURNAL.md](AI_JOURNAL.md): how I used AI while building this

## Known limitations

No real login, no optimistic concurrency, and the dashboard can miss a change if Kafka is down for more than about 2 minutes (no outbox yet). See [DESIGN.md → Not building](DESIGN.md#not-building-on-purpose) for details and the planned fixes.

## Time spent

Total: ~9 hours, including ~3 hours of first-time setup (over the
5-hour cap).

| Phase | Time | Notes |
|---|---|---|
| Environment setup | ~3h | First time installing and using .NET, Docker, Kafka and Angular; package version errors; Docker and Kafka slow on my laptop (see below) |
| Backend | ~4h | Design, data model, endpoints, status rules, Kafka + dashboard, testing, README |
| Frontend and final docs | ~2h | Minimal Angular UI (submit form, read-only officer page), built after the backend was complete; dev server slow and lagging with Docker running, needed several laptop restarts; updating DESIGN.md and the journal |

I had not used .NET, Docker, Kafka or Angular before, so a large part
of the extra time went on setup rather than building.

My laptop also has limited memory: Docker's VM had about 1.6 GB, so
Docker Desktop and Kafka were slow to start (one Kafka restart took
~3.5 minutes) until I reduced Kafka's memory settings. Running the
Angular dev server alongside Docker and the backend caused repeated
lag and errors, and I had to restart my laptop several times.

The backend itself took about 4 hours.

I prioritised a working, tested backend first, as the brief suggests,
and added a minimal frontend afterwards. I chose not to extend the
frontend further (claim detail page, "my claims") to limit the overrun.
The backend already supports both.

## How to run the frontend
cd frontend
npm install
ng serve
Then open http://localhost:4200 (the backend must be running).

## Next steps (not built)
- Claim detail page: history (audit trail) and buttons for allowed
  status changes. The backend supports this already.
- "My claims" view for claimants (backend: GET /api/claims?claimant=).
- Outbox pattern for Kafka, concurrency control (see DESIGN.md).
