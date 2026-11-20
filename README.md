# chubb-claims

A small claims platform built for the Chubb take-home assessment. Claimants submit claims; claims officers assign, review and decide them through a fixed status flow (Submitted → Assigned → UnderReview → Approved/Rejected → Settled). Every status change is kept as an audit trail. Each change is also published to Kafka, and a background consumer keeps a dashboard of open claims, total exposure and workload per officer. The backend is a single .NET 10 Web API backed by PostgreSQL.

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

Total: ~7 hours (over the 5-hour cap).

| Phase | Time | Notes |
|---|---|---|
| Reading the brief and planning | ~45 min | The night before, no code |
| Environment setup | ~3h | First time installing and using .NET, Docker, Kafka and Angular; Docker Desktop slow to start on my laptop; package version errors |
| Building | ~4h | Design, data model, endpoints, status rules, Kafka + dashboard, testing, README |

I had not used .NET, Docker, Kafka or Angular before, so much of the
extra time went on setup rather than building. With the tools already
installed, the build itself fit within the cap.

I prioritised a working, tested backend over the frontend, as the brief
suggests.