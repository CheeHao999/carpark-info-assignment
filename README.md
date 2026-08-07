# Technical Documentation & System Setup

Comprehensive technical documentation, setup guide, and test suite verification for the Carpark Information Web API.

---

## Tech Stack

* **Framework**: .NET 10.0 (ASP.NET Core Web API)
* **Database & ORM**: SQLite with Entity Framework Core 10.0
* **CSV Processing**: CsvHelper (33.x)
* **API Documentation**: Swashbuckle / Swagger UI
* **Testing Engine**: xUnit with EF Core In-Memory Database

---

## System Requirements

* **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download) (or .NET 8.0+)
* **Database**: Embedded SQLite (automatically initialized at runtime)
* **Operating System**: Windows 10/11, macOS, or Linux
* **Terminal**: Git Bash, PowerShell, or standard zsh/bash shell
* **API Client**: Web Browser (for Swagger UI) or cURL / Postman

---

## System Operations & Architecture

### 1. Database & ER Design
* **`Carparks` Table**: Primary data entity identified by `CarParkNo` (Primary Key). Indexed on `GantryHeight` and `NightParking` for optimized dynamic range queries.
* **`UserFavorites` Table**: Manages bookmarks linked via foreign key to `Carparks.CarParkNo`. Includes a composite unique index on `(UserId, CarParkNo)` to prevent duplicate entries at the database layer.

## ER Diagram
ER diagram showing the relationship between CARPARK and USER_FAVORITE tables.
Used Markdown Preview Mermaid Support/mermaid.live to preview the ER Diagram.

::: mermaid
erDiagram
    CARPARK ||--o{ USER_FAVORITE : "favorited by"
    CARPARK {
        string car_park_no PK
        string address
        decimal x_coord
        decimal y_coord
        string car_park_type
        string type_of_parking_system
        string short_term_parking
        string free_parking
        boolean night_parking
        decimal gantry_height
        int car_park_decks
        boolean car_park_basement
    }
    USER_FAVORITE {
        int id PK
        string user_id
        string car_park_no FK
        datetime created_at
    }
:::


### 2. Daily Delta Batch Ingestion & Transactional Rollback
* **Ingestion Pipeline**: The batch job processes daily CSV delta files (`hdb-carpark-information-<timestamp>.csv`) uploaded via `POST /api/Carparks/upload` using CsvHelper.
* **Atomic Rollback Safeguard**: Processing runs inside an explicit EF Core database transaction (`BeginTransactionAsync`). If any parsing error or invalid record is encountered mid-stream, `RollbackAsync()` is executed, restoring the database to its exact prior state without leaking partial records.
* **Upsert Logic**: Updates existing carpark details matching `CarParkNo` and inserts new entries seamlessly.

### 3. API & Front-End Integration Plan
* **`POST /api/Carparks/upload`**: Ingests daily delta CSV files.
* **`GET /api/Carparks`**: Accepts dynamic query filters (`FreeParking`, `NightParking`, `VehicleHeight`). Translates directly to SQL via `IQueryable<Carpark>`.
* **`POST /api/Carparks/favorites`**: Bookmarks carparks per user. Returns HTTP `201 Created` on success and `409 Conflict` on duplicates.

---

## How to Launch

1. **Restore & Build**:
dotnet restore
dotnet build

2. **Run Application**:
dotnet run

3. **Access Swagger UI**:
Open your browser and navigate to:
http://localhost:5068/swagger



## Testing

### Automated Unit Tests
Run the xUnit test suite using the .NET CLI:
```bash
dotnet test
```

### Manual Testing via Swagger UI
1. **POST /api/Carparks/upload**
   - Submit a valid CSV file to test ingestion and rollback logic.
   - Verify 200 OK response.

2. **GET /api/Carparks**
   - Apply query filters (FreeParking, NightParking, VehicleHeight) to test dynamic query execution.
   - Verify 200 OK response with filtered results.

3. **POST /api/Carparks/favorites**
   - Send a valid user ID and car park number to test bookmarking.
   - Verify 201 Created response.
   - Send the same request again to test duplicate prevention (should return 409 Conflict).