# Campus Events Management System (CEMS)

CEMS is a server-rendered ASP.NET Core MVC/Razor application with Identity,
EF Core, SQL Server, and project-owned CSS.

## Repository layout

- `CampusEvents.slnx`: solution file
- `src/CampusEvents/`: MVC application
- `docs/Project1-Traceability.md`: requirements and acceptance record
*(Note: Automated testing was excluded from this delivery scope).*

## Prerequisites and Development configuration

- **.NET SDK**: 10.0 (Target Framework: `net10.0`)
- **EF Core Tools**: Compatible with project version (`dotnet ef --version`)
- **Database**: SQL Server (or Docker SQL Server / Azure SQL Edge for Mac)

From `src/CampusEvents`, initialize user secrets if necessary and configure
`ConnectionStrings:DefaultConnection` to point to an isolated Development database:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=YOUR_SERVER;Database=CEMS_Dev;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
dotnet user-secrets set "SeedAccounts:Organizer:Email" "organizer@example.com"
dotnet user-secrets set "SeedAccounts:Organizer:Password" "Organizer123!"
dotnet user-secrets set "SeedAccounts:Administrator:Email" "admin@example.com"
dotnet user-secrets set "SeedAccounts:Administrator:Password" "Admin123!"
