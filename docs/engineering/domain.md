# Domain specification and consistency contracts

[Engineering index](README.md) · [Русский](domain.ru.md)

**Scope:** implementation of FleetManagement.Core and cooperating modern API writers at V6.3. Normative source is executable code; examples are synthetic. This is not a new feature proposal.

## 1. Data dictionary

| Record | Field | Type / invariant |
| --- | --- | --- |
| Vehicle | Id | int; server allocation independent of plate |
| Vehicle | Plate, Model | nonblank strings; plate unique ignoring case/outer whitespace in Core |
| Vehicle | Mileage, LastServiceMileage | int km; both nonnegative; last service mileage ≤ current |
| Vehicle | LastService | DateTime; date not future relative to validation clock, year ≥ 1900 |
| Vehicle | ServiceIntervalKm | int, 1–1,000,000 |
| Vehicle | ServiceIntervalDays | int, 1–3,650 |
| Driver | Id, Name, License | int ID; nonblank name/license; normalized uniqueness for license in Core |
| Driver | Experience | int years, 0–70 |
| Trip | Id, VehicleId, DriverId | int; referenced vehicle/driver must exist |
| Trip | Origin, Destination | nonblank strings |
| Trip | Start, End | DateTime; End strictly after Start |
| Trip | Distance, Track | int; positive distance; Track represents simulation metadata, not GPS |
| FleetData | Vehicles, Drivers, Trips | collections; no database ownership in Core |

HTTP input DTOs use DateTimeOffset for timestamps and map to UTC. The API's Expected record is optional on supported updates, not an HTTP ETag protocol. Verify additional request constraints in endpoint source/OpenAPI.

## 2. Temporal invariants

Trip occupancy is a half-open interval [start, end). Two distinct assignments overlap exactly when:

~~~text
existing.Start < proposed.End AND proposed.Start < existing.End
~~~

Conflict if they share a vehicle OR driver. A trip ending at 11:00 and another starting at 11:00 do not overlap. Updates exclude their own trip ID. Additional API policy checks reservation and shift availability. The browser simulation clock and server clock are separate.

~~~mermaid
stateDiagram-v2
  [*] --> Scheduled
  Scheduled --> InTransit: now >= start
  InTransit --> Completed: now >= end
~~~

Status is derived, not a mutable lifecycle flag. Only future trips (start > now) can be edited/deleted through the Core rule. Progress is clamped elapsed time divided by total duration; invalid duration yields zero. This is scheduled-time simulation, not observed completion.

## 3. Maintenance decision table

remainingKm = intervalKm − max(0, mileage − lastServiceMileage).  
remainingDays = date(lastService + intervalDays) − date(today).

| Condition (ordered) | Result |
| --- | --- |
| remainingKm ≤ 0 OR remainingDays ≤ 0 | Due |
| otherwise remainingKm ≤ 1000 OR remainingDays ≤ 14 | Soon |
| otherwise | Healthy |

These are operational rules, not mechanical diagnosis. Exact limit values trigger the corresponding status. The Core recommendation excludes Due vehicles and busy resources; orders candidates by lowest mileage then vehicle ID, and drivers by trip count then driver ID. No available pair returns null. Server policies may impose additional constraints.

## 4. Transaction and retry sequence

~~~mermaid
sequenceDiagram
  participant C as Client
  participant A as Authorized API
  participant S as FleetStore
  participant D as Database
  participant H as SignalR
  C->>A: Confirmed write + UUID
  A->>S: Validated identity and request
  S->>D: Acquire provider transaction lock
  S->>D: Read receipt and current records
  alt Existing same fingerprint
    S-->>C: Original response; replay header
  else Different fingerprint or invalid state
    S-->>C: Conflict / validation response
  else New valid operation
    S->>D: Save records, audit, receipt; commit
    S-->>H: Best-effort notification
    S-->>C: Successful response
  end
~~~

Actor/key namespace is shared by supported ordinary and batch writes. Receipt response size limit is 65,536 characters as checked in FleetStore; cap 50,000 entries. Failed validation rolls back and does not consume a key. Deletion protects trip and operational history. Direct SQL bypasses business locking; no multi-writer compatibility is asserted for legacy clients.

## 5. Acceptance cases

| Input | Expected |
| --- | --- |
| Same plate with case/outer whitespace variation | Core rejects duplicate |
| Service mileage greater than current | Reject |
| Driver experience 70 / 71 | Accept boundary / reject |
| Trip end equals start | Reject |
| Adjacent trips on same resources | No interval conflict |
| Overlap, same driver but different vehicle | Reject |
| Edit started trip | Reject |
| Remaining service 1000 km / 14 days | Soon unless Due |
| Identical confirmed write key/body | Historical response, no second mutation |
| Same key, changed body | Conflict |

See [Fleet.cs](../../src/FleetManagement.Core/Fleet.cs), [FleetStore](../../src/FleetManagement.Api/FleetStore.cs), [FleetEndpoints](../../src/FleetManagement.Api/FleetEndpoints.cs), [EF mapping](../../src/FleetManagement.Api/FleetDbContext.cs), [receipt details](../batch-receipts.md), [Core checks](../../tests/FleetManagement.Core.Checks/Program.cs).
