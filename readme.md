# CivicLens

A lightweight, robust web platform built to bridge the gap between citizens and municipal administrators. Citizens can report local public infrastructure issues with precise geographic locations and media proof, while local authorities manage triage, field technician routing, and public status tracking.

---

## 🛠️ Tech Stack

* **Backend:** C# ASP.NET Core Web API
* **Database:** PostgreSQL (Entity Framework Core)
* **Frontend:** JavaScript (ES6+), HTML5, CSS3, Leaflet.js
* **Auth:** JSON Web Tokens (JWT) with Role-Based Access Control
* **API Spec:** Swagger / OpenAPI 3.0

---

## 🏗️ System Architecture

```
                       ┌──────────────────────────────┐
                       │    Client Application        │
                       │   (JavaScript / HTML / CSS)  │
                       └──────────────┬───────────────┘
                                      │ REST API (JSON)
                                      ▼
                       ┌──────────────────────────────┐
                       │    Backend Web API           │
                       │    (.NET / ASP.NET Core)     │
                       └──────┬────────────────┬──────┘
                              │                │
            Entity Framework  │                │ Spatial Queries /
                  Core / Npgsql│                │ JSON Responses
                              ▼                ▼
                 ┌──────────────────┐    ┌───────────────────┐
                 │ PostgreSQL / DB  │    │ File/Media Storage│
                 │ (PostGIS enabled)│    │ (Local / Cloud)   │
                 └──────────────────┘    └───────────────────┘
```

---

## 🚀 Key Features

* **Interactive Issue Submission:** Geolocation auto-detection or manual map pin drops for pinpoint accuracy.
* **Status Workflow Engine:** State transitions (`Submitted` → `In Review` → `Assigned` → `In Progress` → `Resolved` → `Closed`).
* **Municipal Admin Dashboard:** Priority queuing, filtering by category/locality, technician assignment, and resolution metrics.
* **Role-Based Access Control:** Distinct roles for Citizens, Department Admins, and Field Technicians.
* **Audit Trail & Logging:** Transparent record of status updates and official municipal response notes.

---

## 💻 Getting Started

### Prerequisites
* [.NET 10.0 SDK](https://dotnet.microsoft.com/)
* [PostgreSQL 15+](https://www.postgresql.org/)

### Local Setup Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/civic-infrastructure-portal.git
   cd civic-infrastructure-portal
   ```

2. **Configure Database Connection:**
   Copy `backend/appsettings.example.json` to
   `backend/appsettings.json`, then update the connection string and
   JWT key. The real `appsettings.json` is ignored so credentials are not committed.

3. **Create the database and apply the schema:**
   ```bash
   createdb civic_lens
   psql -d civic_lens -f backend/scripts/init_schema.sql
   ```

   If PostgreSQL requires a username or host, use the same values configured in
   `appsettings.json`, for example `psql -U postgres -d civic_lens -f backend/scripts/init_schema.sql`.

4. **Apply Database Migrations:**
   ```bash
   dotnet ef database update --project backend
   ```

5. **Run the API Server:**
   ```bash
   dotnet run --project backend
   ```

6. **Access API Documentation:**
   Navigate to `http://localhost:5000/swagger` in your web browser.

7. **Run the frontend:**
   ```bash
   npx serve frontend -l 5500
   ```
   Open `http://localhost:5500`. The frontend uses the API at
   `http://localhost:5080` by default and can be configured with
   `window.CIVIC_API_BASE`.

### API Foundation

Authentication endpoints are available at `/api/v1/auth/register` and `/api/v1/auth/login`.
New users are registered as `Citizen` accounts. Protected issue operations require a
JWT bearer token:

* `POST /api/v1/issues` — citizen issue submission with coordinates and media metadata.
* `GET /api/v1/issues` — public filtering by status, category, and coordinate bounding box.
* `GET /api/v1/issues/{id}` — public issue details and media metadata.
* `PATCH /api/v1/issues/{id}/status` — Admin or FieldTech status updates.
* `POST /api/v1/issues/{id}/assign` — Admin assignment to a FieldTech.

Apply [backend/scripts/init_schema.sql](backend/scripts/init_schema.sql) to a PostgreSQL database with
PostGIS enabled before registering users or submitting issues. Replace the placeholder
JWT key in `backend/appsettings.json` with a randomly generated secret of
at least 32 characters.

The frontend implementation is in [frontend](frontend), with additional local
development instructions in [frontend/README.md](frontend/README.md).

> **Local testing only:** Authentication is currently disabled at the frontend
> and issue write endpoints so the basic website flow can be tested without
> signing in. The original JWT configuration, auth services, and authorization
> attributes remain in the code as comments/unchanged implementation. Restore
> the marked lines in `Program.cs`, `IssuesController.cs`, and `frontend/app.js`
> before exposing the application beyond local development.

---

## 📜 License
This project is open-source and available under the [MIT License](LICENSE).