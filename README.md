# Life Grid To-Do API 🟩

ASP.NET Core (.NET 10) Web API backend for a "Life Grid" To-Do application. The app visualizes a user's life as a grid of days/weeks, supports daily habits/tasks, journaling, and basic lifetime statistics.

The solution is structured using **Clean Architecture**.

---

## 🏛️ Architecture (Clean Architecture)

Dependencies point inward:

1. **`CalendarLife.Domain`**: entities and core types (no external dependencies)
2. **`CalendarLife.Application`**: use cases (CQRS), validation, interfaces (depends on Domain)
3. **`CalendarLife.Infrastructure`**: persistence/integrations (implements Application interfaces)
4. **`CalendarLife.Api`**: HTTP host (wires everything together)

---

## 🧭 Day model (how days are addressed)

The project models a user's life by an integer day index.

- `DailyRecord.DayIndex` is the **day index** (e.g., `20000` = ~20k days after birth)

The backend is expected to **store only the days the user actually modified** (not pre-create every day).

---

## ✅ Status enum

The Domain defines a 3-state enum (used by the current entities):

- `Pending`
- `Completed`
- `Failed`

Implemented as `DayStatus` in `CalendarLife.Domain/Enums/DayStatus.cs`.

---

## 🗄️ Current Domain model (matches the code)

This section describes the **current code** (not the final EF Core schema). Infrastructure/EF Core mappings are still pending.

### `Users`

From `CalendarLife.Domain.Entities.User`:

| Property     | Type   |
| :----------- | :----- |
| `Id`         | GUID   |
| `Name`       | string |
| `BirthDate`  | DateTime |

Relationships:
- 0/1 `UserSetting`
- Many `DefaultTask`
- Many `DailyRecord`

### `UserSettings`

From `CalendarLife.Domain.Entities.UserSetting` (note: **PK is `Id`**):

| Property          | Type    |
| :---------------- | :------ |
| `Id`              | GUID    |
| `UserId`          | GUID    |
| `SquareSize`      | int     |
| `ShowHelp`        | bool    |
| `SetupCompleted`  | bool    |

### `DefaultTasks`

From `CalendarLife.Domain.Entities.DefaultTask`:

| Property     | Type   |
| :----------- | :----- |
| `Id`         | GUID   |
| `UserId`     | GUID   |
| `Text`       | string |
| `Duration`   | int    |
| `Completed`  | `DayStatus` |

> Note: `Completed` currently uses `DayStatus` in code. If `DefaultTask` is a pure template, this likely becomes removable later.

### `DailyRecords`

From `CalendarLife.Domain.Entities.DailyRecord`:

| Property     | Type   |
| :----------- | :----- |
| `SeqId`      | int    |
| `UserId`     | GUID   |
| `DayIndex`   | int    |
| `Journal`    | string |
| `DayStatus`  | `DayStatus` |

Relationships:
- Many `DailyTask`

> Note: `SeqId` and `DayIndex` both exist in the current code. If they represent the same concept, the model will likely converge to a single field.

### `DailyTasks`

From `CalendarLife.Domain.Entities.DailyTask`:

| Property              | Type   |
| :-------------------- | :----- |
| `Id`                  | GUID   |
| `DailyRecordId`       | GUID   |
| `SourceDefaultTaskId` | GUID?  |
| `Text`                | string |
| `Duration`            | int    |
| `Completed`           | `DayStatus` |

> Note: `Completed` currently uses `DayStatus` in code (3 states).
>
> Also note: `DailyRecordId` is a `Guid`, while `DailyRecord` currently has `SeqId` as its identifier. EF Core persistence/mapping for this relationship is not implemented yet, so the database schema is still a work in progress.

---

## 🚀 Development checklist

### Phase 1: Solution & project structure

- [x] Create solution and projects
- [x] Project references follow Clean Architecture dependency rules

### Phase 2: Domain

- [x] Entities created in `CalendarLife.Domain/Entities`
- [x] `DayStatus` enum created in `CalendarLife.Domain/Enums`

### Phase 3: Application (WIP)

- [ ] Add `Interfaces/` (e.g., `ICalendarLifeDbContext`)
- [ ] Add CQRS (MediatR) commands/queries

### Phase 4: Infrastructure (WIP)

- [ ] Add EF Core + Npgsql packages
- [ ] Implement `DbContext` and mappings
- [ ] Add migrations

### Phase 5: API (WIP)

- [ ] Replace template endpoint (`/weatherforecast`) with real controllers/endpoints
- [ ] Wire Application + Infrastructure via DI

---

*Built with code, caffeine, and Italian dreams.* 🇮🇹☕
