# RaceDay

**Module:** PROG6212 · **Student:** Bongakonke Makadunyiswe Nsibande (ST10493442)
**Repository:** https://github.com/nsibandebongakonke-png/Raceday-Part2 (Part 3 lives on the `part3-mvc` branch)

RaceDay is a race-event platform for runs, walks and cycle races. **Organisers** create events, set categories, review enrolments and publish results. **Participants** browse events, enrol with a category, and track their results.

> 🎥 **Demo video (unlisted YouTube):** `PASTE YOUR LINK HERE`

---

## Architecture

```
 Browser ──► RaceDay.Web (ASP.NET Core MVC) ──HTTP/JSON──► RaceDay.Api (REST) ──► SQL Server
             • session + role                 • session auth                 └──► Azure Blob Storage
             • NO database / NO Azure code    • business rules, validation        (event banners, profile pictures)
```

| Project | Purpose |
|---|---|
| `RaceDay.Api` | Part 2 REST API. Owns the database (EF Core code-first) and **Azure Blob Storage** (all file handling). Session-based authentication with BCrypt-hashed passwords. Swagger at `/swagger`. |
| `RaceDay.Web` | Part 3 MVC front end. Consumes the API for **all** data. It has no EF Core, no connection string and no Azure SDK, and it never talks to Azure directly. |

### How the MVC app works (for the video)
- **`Services/RaceDayApiClient.cs`** – the only class that talks to the API. One method per endpoint. Converts every failure into a friendly message.
- **Session & roles** – on login the API starts its own session (id + role). The MVC app stores the user's id, role, name and the API session cookie in *its* session (`Infrastructure/SessionUserExtensions.cs`) and forwards the cookie on each API call.
- **`[SessionAuthorize]`** – filter on controllers. Not logged in → redirected to Login; wrong role → Access Denied. Logout clears both sessions and redirects to Login.
- **Controllers** – `Organiser`, `ManageEvents`, `Categories`, `Enrolments`, `Results` (Organiser); `Events`, `MyEnrolments`, `MyResults` (Participant); `Account`, `Profile`, `Home`, `Files` (shared).
- **Images** – the MVC `Files` controller asks the API (`GET /api/files/...`) for the bytes, so the browser never needs Azure access and the container can stay private.

### Azure Blob Storage (in the API)
- `Services/BlobStorageService.cs` uploads, downloads and deletes blobs in a **private** container (`raceday-images`).
- Event banners → `banners/<guid>.<ext>`, profile pictures → `profiles/<guid>.<ext>`. Only the blob name is stored in SQL.
- Uploads are validated: JPG/PNG/GIF/WEBP only, max 5 MB, and the file's real signature is checked.
- Endpoints: `POST /api/events/{id}/banner`, `POST /api/profile/picture`, `GET /api/files/{blobName}`.

---

## Running it

### Option A – everything with ONE command (Docker)
Requires Docker Desktop.
```bash
docker compose up --build
```
| What | URL |
|---|---|
| **RaceDay web app** | http://localhost:5200 |
| API + Swagger | http://localhost:5100/swagger |

This starts SQL Server, **Azurite** (a local Azure Blob Storage emulator), the API and the MVC app. Demo data is created on first start.

### Option B – run ONLY the MVC container (API already running on your machine)
```bash
docker build -t raceday-web ./RaceDay.Web
docker run -d --name raceday-web -p 8080:8080 --add-host=host.docker.internal:host-gateway -e ApiSettings__BaseUrl=http://host.docker.internal:5100 raceday-web
```
Then open http://localhost:8080. (`ApiSettings__BaseUrl` points the container at the API running locally on port 5100.)

### Option C – run locally in Visual Studio
1. Install/enable **Azurite** (included with the Visual Studio *Azure development* workload) **or** configure a real storage account (below).
2. Open `RaceDay.slnx`, set **multiple startup projects**: `RaceDay.Api` and `RaceDay.Web` (http profiles → ports 5100 / 5200).
3. The API creates the `RaceDayApiDb` database on first run (LocalDB) and seeds demo data.

### Using a real Azure Storage account
1. Azure Portal → **Storage accounts** → Create → open it → **Security + networking → Access keys** → copy a **connection string**.
2. Local: `cd RaceDay.Api && dotnet user-secrets set "AzureBlob:ConnectionString" "<connection string>"`
3. Docker: create a `.env` file next to `docker-compose.yml` containing `AZURE_STORAGE_CONNECTION_STRING=<connection string>`.

The container is created automatically on the first upload.

### Demo accounts (seeded)
| Role | Email | Password |
|---|---|---|
| Organiser | organiser@raceday.co.za | `Organiser@123` |
| Participant | runner@raceday.co.za | `Participant@123` |

---

## API endpoints
Errors always look like `{ "message": "…" }`. `401` = not logged in, `403` = wrong role / not your event.

| Area | Endpoint | Who |
|---|---|---|
| Auth | `POST /api/auth/register` · `POST /api/auth/login` · `POST /api/auth/logout` · `GET /api/auth/me` | public / any |
| Profile | `GET` / `PUT /api/profile` · `POST /api/profile/picture` | any |
| Events | `GET /api/events` (search, type, from, to, upcomingOnly) · `GET /api/events/{id}` | any |
| | `GET /api/events/mine` · `POST /api/events` · `PUT` / `DELETE /api/events/{id}` · `POST /api/events/{id}/banner` | Organiser (owner) |
| Categories | `GET /api/events/{id}/categories` | any |
| | `POST /api/events/{id}/categories` · `PUT` / `DELETE /api/categories/{id}` | Organiser (owner) |
| Enrolments | `POST /api/enrolments` · `GET /api/enrolments/my` · `PUT /api/enrolments/{id}/cancel` | Participant |
| | `GET /api/events/{id}/enrolments` · `PUT /api/enrolments/{id}/status` | Organiser (owner) |
| Results | `GET /api/events/{id}/results` · `POST /api/results` · `PUT /api/results/{id}` | Organiser (owner) |
| | `GET /api/results/my` (published only, includes total finishers) | Participant |
| Files | `GET /api/files/{blobName}` | any (logged in) |

---

## Part 3 requirements checklist
| # | Requirement | Where |
|---|---|---|
| 1 | MVC consumes the API; no DB access / duplicated logic in MVC | `RaceDay.Web` has no EF/SQL/Azure packages; `Services/RaceDayApiClient.cs` |
| 2 | Register/login via API, session state + role, role-specific navigation, protected pages, logout | `AccountController`, `SessionAuthorizeAttribute`, `_Layout.cshtml` |
| 3 | Organiser: dashboard, events CRUD + banner, categories, enrolments, results | `Organiser`, `ManageEvents`, `Categories`, `Enrolments`, `Results` controllers |
| 4 | Participant: home + filters, event details, enrol, my enrolments, my results ("47th out of 312"), profile + picture | `Events`, `MyEnrolments`, `MyResults`, `Profile` controllers |
| 5 | Azure Blob Storage in the API only | `RaceDay.Api/Services/BlobStorageService.cs` |
| 6 | Docker | `RaceDay.Web/Dockerfile`, `RaceDay.Api/Dockerfile`, `docker-compose.yml` |
| 7 | Sporty theme, Bootstrap, colour-coded statuses, graceful errors, consistent UI | `wwwroot/css/site.css`, `_StatusBadge`, friendly error pages |
| 8 | GitHub + CI | `.github/workflows/dotnet.yml` builds API + MVC (+ Docker images) |

## Database
Code-first with EF Core (`RaceDay.Api/Models/Entities.cs`), created on start-up with `EnsureCreated`. Tables: Users, Roles, Events, Categories, Enrolments, RaceResults. See `RaceDay_ERD.png` and `RaceDay_full.sql` for the original Part 1 design (Part 3 adds `Category.CategoryType`, `Enrolment.Status` values and `RaceResult.Published`).
