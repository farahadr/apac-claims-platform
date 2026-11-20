# Design Notes

## What I'm building
A claims platform where claimants submit and track claims, and
claims officers assess and decide them.

## Main data
- Claim: id, type (Motor/Property), market, description, estimated amount, status, claimant, assigned officer, dates
- ClaimEvent: history of every status change (who, when, from → to)
- InfoRequest: officer asks a question, claimant answers

## Claim status flow
Submitted → Assigned → UnderReview → Approved → Settled
                          ↓   ↑          
                     InfoRequested      
                          ↓
                       Rejected
(Rules: can't approve a rejected claim, can't settle before approval)

## Key rules
- Claim history (ClaimEvent) is an audit trail: it is never deleted.
  The database blocks deleting a claim that has history (Restrict, not
  cascade). Claims are closed (Rejected/Settled), not deleted.

## Backend: one service
One .NET API, organised by feature. A single service is faster
to build and easier to run; it could be split later if needed.

## REST vs Kafka
- REST: anything a user does and waits for (submit, view, approve)
- Kafka: things that happen after (status changed → update team
  workload and total exposure, send notifications)

## Not building (on purpose)
- Real login: a simple role switch instead
- File uploads, payments, emails
- Multiple markets' different rules
- Concurrency control: two officers updating the same claim at the same moment could both pass the status check. Fix would be optimistic concurrency (PostgreSQL row version). Deferred for time.