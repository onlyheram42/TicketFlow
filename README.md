# TicketFlow — Customer Support Ticket System

A full-stack Customer Support Ticket System with a **WPF Desktop Application** as the frontend and **ASP.NET Web API** as the backend, using **MySQL** as the database.

## Tech Stack

| Layer    | Technology                          |
|----------|-------------------------------------|
| Frontend | C# WPF Desktop Application         |
| Backend  | ASP.NET Web API (.NET 8)            |
| Database | MySQL                               |
| ORM      | Dapper (Micro ORM)                  |
| Auth     | JWT Bearer Token Authentication     |
| Docs     | Swagger / Swashbuckle               |

## Project Structure

```
TicketSystem/
├── TicketFlow.Api/           # ASP.NET Web API Backend
│   ├── Controllers/          # API Controllers
│   ├── Data/                 # DB Connection Factory + Seeder
│   ├── DTOs/                 # Request/Response Data Transfer Objects
│   ├── Middleware/            # Global Exception Handling
│   ├── Models/               # Domain Entities
│   ├── Repositories/         # Data Access Layer (Dapper)
│   ├── Services/             # Business Logic Layer
│   └── Database/             # MySQL Init Script
└── TicketFlow.Desktop/       # WPF Desktop Application
```

## How to Run Locally

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL Server](https://dev.mysql.com/downloads/mysql/) (8.0+)

### 1. Set Up the Database

Run the SQL initialization script against your MySQL server:

```bash
mysql -u root -p < TicketFlow.Api/Database/init.sql
```

This creates the `TicketFlowDb` database with all required tables.

### 2. Configure the API

Edit `TicketFlow.Api/TicketFlow.Api/appsettings.json` and update the MySQL connection string with your credentials:

```json
"ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=TicketFlowDb;User=root;Password=YOUR_PASSWORD;"
}
```

### 3. Run the API

```bash
cd TicketFlow.Api
dotnet run --project TicketFlow.Api
```

The API will start and **automatically seed** two default users on first run:

| Username | Password   | Role  |
|----------|------------|-------|
| admin    | Admin@123  | Admin |
| john     | User@123   | User  |

### 4. Open Swagger UI

Navigate to `https://localhost:{port}/swagger` (the URL is printed in the console output).

## API Endpoints

| Method | Endpoint                        | Auth       | Description                         |
|--------|---------------------------------|------------|-------------------------------------|
| POST   | `/api/auth/login`               | None       | Login, returns JWT token            |
| POST   | `/api/tickets`                  | User/Admin | Create a support ticket             |
| GET    | `/api/tickets`                  | User/Admin | List tickets (role-based)           |
| GET    | `/api/tickets/{id}`             | User/Admin | Get ticket details                  |
| PUT    | `/api/tickets/{id}/assign`      | Admin      | Assign ticket to an admin           |
| PUT    | `/api/tickets/{id}/status`      | Admin      | Update ticket status                |
| POST   | `/api/tickets/{id}/comments`    | User/Admin | Add a comment to a ticket           |
| GET    | `/api/tickets/{id}/history`     | User/Admin | Get ticket status history           |
| GET    | `/api/tickets/admins`           | Admin      | List admin users for assignment     |

## Business Rules

- **Ticket Number**: Auto-generated in format `TKT-00001`
- **Status Flow**: `Open` → `In Progress` → `Closed` (strict, no skipping or reopening)
- **Users** can only view and comment on their own tickets
- **Admins** can view all tickets, assign them, change status, and add internal comments
- **Closed tickets** cannot be modified
- **Server time (UTC)** is used for all timestamps
- All admin actions are logged in status history

## Design Decisions

- **Dapper** chosen for lightweight, high-performance data access with full SQL control
- **Repository pattern** separates data access from business logic (SOLID)
- **Service layer** contains all business rule validation
- **Global exception middleware** maps domain exceptions to HTTP status codes
- **BCrypt** for secure password hashing
- **DbSeeder** auto-seeds users on first startup so password hashes are always correct
