# Lesson 2 — Relational Data and the Public Event Catalog

In Lesson 1, you built a real ASP.NET Core MVC request/render cycle. A browser could request `/Events`, `EventsController.Index()` supplied a temporary value, and Razor rendered HTML.

The temporary value at the end of that lesson was:

```csharp
var featuredEventTitle = "Sophomore's Bloodfest";
```

That was enough to prove controller-to-View data flow, but it is not how CEMS can store a real catalog. If the controller must recreate the data on every request, the application has no persistent source of truth.

In this lesson, you will replace that temporary string with relational data stored in **SQL Server LocalDB** and accessed through **Entity Framework Core (EF Core)**. The existing `CampusEvents` project, MVC pipeline, conventional route, `EventsController`, logger dependency, `/Events` entry point, and shared Events navigation remain in place. You are evolving the application you already have, not creating a new one.

The important change is larger than replacing one variable with a database call. Lesson 1 established the **web request/render cycle**. Lesson 2 adds a **persistence cycle** inside that request:

```text
HTTP request
    ↓
controller
    ↓
application data model
    ↓
EF Core
    ↓
SQL Server LocalDB
    ↓
persisted relational rows
    ↓
EF Core materializes C# objects
    ↓
controller passes data to Razor
    ↓
HTML response
```

As you work, keep the boundaries in that diagram separate. The C# entity classes are not the database. EF Core is not the database. A migration is not the database. Razor does not decide which database rows should be public. Each layer has a different responsibility, and much of this lesson is about learning where those responsibilities belong.

## What You Will Build

By the end of this lesson, CEMS will have its first database-backed vertical slice:

- Event, Category, and organizer-ownership data stored relationally;
- an EF Core `ApplicationDbContext`;
- an initial migration named `InitialCemsCatalog`;
- Development-only classroom seed data;
- a public catalog that shows only Published upcoming Events;
- chronological ordering by Event start time;
- a page that displays one persisted Event and its related information;
- configured Event capacity on that page;
- controlled HTTP 404 behavior when a requested Event is unavailable.

The public browsing work completes `FR-SRH-001`, `FR-SRH-002`, and `FR-SRH-003`. `FR-SRH-004` remains partial because current registration availability depends on Registration data that does not exist yet.

## What You Will Learn

After completing the lesson, you should be able to:

1. explain entities, primary keys, foreign keys, navigation properties, and one-to-many relationships;
2. explain what EF Core and a `DbContext` contribute to an MVC application;
3. configure EF Core to use SQL Server LocalDB;
4. create and apply a migration;
5. inspect a migration and relate its schema to C# entity definitions;
6. explain the purpose of Development seed data;
7. trace an asynchronous EF Core query from controller to database and back;
8. explain why public browsing filters and orders data in the query;
9. use a route parameter to identify one stored resource;
10. explain why a missing resource should produce a controlled not-found response;
11. distinguish configured Event capacity from live registration availability;
12. distinguish an EF Core model, a migration, and the actual database schema;
13. explain tracking, eager loading, query execution, and materialization at an introductory level;
14. explain why public-data eligibility belongs in the database query rather than only in Razor.

---

## 1. From Temporary Data to Persistence

Your current `/Events` flow looks like this:

```text
Browser requests /Events
        ↓
conventional routing
        ↓
EventsController.Index()
        ↓
temporary string value
        ↓
Views/Events/Index.cshtml
        ↓
HTML response
```

The MVC part is already real. What changes in this lesson is the source of the Event data.

After persistence is added, the browse flow will become:

```text
Browser requests /Events
        ↓
routing selects EventsController.Index()
        ↓
controller queries ApplicationDbContext
        ↓
EF Core sends the query to SQL Server LocalDB
        ↓
Published upcoming Event rows are returned
        ↓
MVC passes the Event collection to Razor
        ↓
Razor renders HTML
        ↓
HTML is returned to the browser
```

### Why persistence matters

Relational persistence allows the Event data to survive the end of an HTTP request. The controller no longer has to invent the Event title itself. Instead, it asks the database for rows that satisfy CEMS rules.

Some rules can also be protected at the database level. In this lesson:

- every Event must reference an existing Category;
- every Event must reference an organizer;
- start time must be before end time;
- capacity must be greater than zero.

Later lessons will add form validation before invalid data reaches the database. Database constraints and application validation work together.

That distinction matters. Application validation can give a user a helpful message before an invalid write is attempted. Database constraints protect persisted data even when some other path reaches the database—for example, a bug, a script, an administrative tool, or future application code. The database is therefore not merely storage; it is also one of the places where important data invariants can be enforced.

### Relational data in this project

CEMS needs to store related information rather than one large disconnected record.

- Category 1:N Events = Each event has one category : each category may have many events
- Organizer 1:N Events = Each event has one organizer : each organizer may have many events

```text
Category
 	├─ Id (PK)
 	└─ Name

Organizer
 	├─ Id (PK)
 	└─ DisplayName

Event
 	├─ CategoryId (FK)
 	└─ OrganizerId (FK)
```

A **primary key (PK)** identifies one row. A **foreign key (FK)** stores the key of a related row.

```csharp
public class Event
{
	...

	public int CategoryId { get; set; }
	public Category Category { get; set; } = null!;

	public int OrganizerId { get; set; }
	public ApplicationUser Organizer { get; set; } = null!;
}
```

A **navigation property** gives C# code an object-oriented way to work with that relationship.

For example, an Event can store:

```csharp
public int CategoryId { get; set; }
```

and also expose:

```csharp
public Category Category { get; set; } = null!;
```

`CategoryId` is the stored relationship value. `Category` is the related C# object that EF Core can load when needed.

It helps to separate the **relational representation** from the **object-oriented representation**.

The navigation properties are not extra columns containing whole C# objects. The database stores keys and rows. EF Core uses the relationship metadata to connect those rows back into related C# objects when the application requests that related data.

Event  
 	├─ CategoryId (FK)  
 	├─ <s>Category</s>  
 	├─ OrganizerId (FK)  
 	└─ <s>Organizer</s>  

The organizer relationship follows the same general idea. We will begin with its stored foreign-key value, then add its navigation property after the user type has been introduced.

---

## 2. Modeling CEMS Data

Before EF Core can persist CEMS data, the application needs a model that expresses the concepts it cares about. In this lesson those concepts are an Event, its Category, its lifecycle status, and the user reference that owns or organizes it.

An **entity class** is a C# type that EF Core can map to relational data. The class expresses the application's object-oriented view of the concept; EF Core later builds mapping metadata that describes how those properties and relationships correspond to tables, columns, keys, and constraints.

Do not assume that every C# property means “create a column exactly like this.” Some properties are scalar values that map naturally to columns, while navigation properties represent relationships among entities. The purpose of the model is to let C# code work with meaningful application concepts while EF Core handles the relational mapping.

### 2.1 Add `EventStatus`

Open a new file:

**File: `Models/EventStatus.cs`**

```csharp
namespace CampusEvents.Models;

public enum EventStatus
{
    Draft,
    Published,
    Cancelled
}
```

CEMS currently recognizes exactly three Event states:

- `Draft`
- `Published`
- `Cancelled`

An enum makes those states explicit in C#. `EventStatus.Published` communicates much more clearly than a numeric value such as `1`.

### 2.2 Add `ApplicationUser`

Now create the small user model that provides Event ownership.

**File: `Models/ApplicationUser.cs`**

```csharp
namespace CampusEvents.Models;

public class ApplicationUser
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
}
```

The `Id` uses a `string`, which matches the default key shape used by ASP.NET Core Identity. At this point, however, this class is only a persisted ownership reference. You are **not** building registration, login, passwords, roles, or authorization in this lesson.

CEMS needs ownership data before it needs a complete authentication workflow. Keeping the same user identity shape allows later lessons to add authentication capabilities without changing the meaning of `Event.OrganizerId`.

`DisplayName` gives CEMS a human-readable organizer name that can later appear with an Event.

`OrganizedEvents` is the collection navigation property for the one-to-many relationship between an `ApplicationUser` and the Events they organize. Initializing it with an empty `List<Event>` keeps the collection ready to use and avoids null checks when Events are added or loaded through Entity Framework Core.

### 2.3 Add `Category`

Create:

**File: `Models/Category.cs`**

```csharp
namespace CampusEvents.Models;

public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Event> Events { get; set; } = new List<Event>();
}
```

`Id` is the Category primary key. `Name` is the label users will see. `Events` is the one-to-many navigation that represents all Events assigned to the Category.

The collection is not one giant column in the Categories table. It represents a relationship that EF Core understands.

The one-to-many relationship will eventually have this shape:

```text
Categories
----------
Id = 1, Name = Academic

Events
------
Id = 10, CategoryId = 1, ...
Id = 11, CategoryId = 1, ...
```

The database expresses the relationship by storing `CategoryId` on each Event row. The `Category.Events` collection is the C# view of that same relationship from the Category side.

**NOTE**: The editor will mark `Event` as undefined until you create the `Event` model (below).

### 2.4 Add the first version of `Event`

Create:

**File: `Models/Event.cs`**

```csharp
namespace CampusEvents.Models;

public class Event
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }

    public int Capacity { get; set; }

    public EventStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public string OrganizerId { get; set; } = string.Empty;

    public ApplicationUser Organizer { get; set; } = null!;
}
```

This model contains the Event information needed at this stage:

- identity;
- title and description;
- location;
- start and end date/time;
- capacity;
- status;
- creation time;
- Category relationship;
- organizer ownership through `OrganizerId`.

There is no Registration collection. Registration belongs to a later stage of the project.

### Build It

Build the project before adding EF Core:

```cmd
dotnet build
```

At this checkpoint, the classes should compile. They are still ordinary C# classes; no database persistence exists yet.

---

## 3. Add EF Core and SQL Server LocalDB

The entity classes describe CEMS concepts, but ordinary C# classes do not know how to create SQL tables, translate LINQ into SQL, send commands to SQL Server, or rebuild related objects from returned rows. **Entity Framework Core (EF Core)** provides that persistence layer.

A useful mental model is:

```text
CEMS entity objects and LINQ
        ↓
ApplicationDbContext
        ↓
EF Core model + query/change services
        ↓
SQL Server provider
        ↓
SQL Server LocalDB
```

Each part has a distinct role:

- the **entity classes** describe CEMS data in C#;
- `ApplicationDbContext` is CEMS's EF Core entry point and contains the model configuration used for persistence;
- **EF Core** uses that model to translate supported LINQ expressions and save operations into database work;
- the **SQL Server provider** knows how EF Core should communicate with SQL Server;
- **SQL Server LocalDB** is the relational database engine that actually stores the rows.

This separation explains why several setup steps are required. Installing EF Core does not create a database. Defining entity classes does not create tables. Registering `ApplicationDbContext` does not apply a schema. Those pieces cooperate, but they are not the same thing.

### 3.1 Install the EF Core packages

From the `CampusEvents` project directory, install the SQL Server provider:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

Add EF Core's design-time package for migrations:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.Design
```

The two packages support different parts of the workflow. `Microsoft.EntityFrameworkCore.SqlServer` supplies the SQL Server database provider used when the application talks to LocalDB. `Microsoft.EntityFrameworkCore.Design` supplies design-time support needed by tooling such as migrations.

Use package versions compatible with the .NET/EF Core version already established. Do not change major framework versions.

Restore and build:

```cmd
dotnet restore
dotnet build
```

### 3.2 Confirm SQL Server LocalDB

This lesson uses **SQL Server LocalDB**. On a Windows development machine with LocalDB installed, you can inspect the default instance with:

```cmd
sqllocaldb info MSSQLLocalDB
```

If Windows reports that sqllocaldb is not recognized, install LocalDB before continuing.

#### 3.2.1 Install with SQL Server Express

Download [Microsoft SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads).

Microsoft's SQL Server Express installer provides LocalDB as a selectable feature. During setup, make sure LocalDB is selected as an installed feature. Alternatively, you may use the standalone installer file named `SqlLocalDB.msi` inside.

After installation, restart your computer and check the `sqllocaldb` command again. Do not continue until the LocalDB installation has completed successfully.

### 3.3 Create `ApplicationDbContext`

`ApplicationDbContext` is where CEMS tells EF Core which entity sets belong to the current persistence model and where explicit mapping rules live.

Think of the context as the application's EF Core working boundary:

```text
Application code
    ↓
ApplicationDbContext
    ├─ Users
    ├─ Categories
    └─ Events
         ↓
model configuration
         ↓
EF Core database operations
```

The context does not contain all database rows inside itself. Its `DbSet<T>` properties are entry points for querying and saving entities. The context also supplies EF Core with relationship and constraint configuration that cannot be inferred—or that CEMS wants to make explicit.

Create the `Data` folder, then create:

**File: `Data/ApplicationDbContext.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("AspNetUsers");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id)
                .HasMaxLength(450)
                .ValueGeneratedNever();

            entity.Property(user => user.DisplayName)
                .IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name)
                .IsRequired();
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(eventItem => eventItem.Title)
                .IsRequired();

            entity.Property(eventItem => eventItem.Description)
                .IsRequired();

            entity.Property(eventItem => eventItem.Location)
                .IsRequired();

            entity.HasOne(eventItem => eventItem.Category)
                .WithMany(category => category.Events)
                .HasForeignKey(eventItem => eventItem.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(eventItem => eventItem.Organizer)
                .WithMany(user => user.OrganizedEvents)
                .HasForeignKey(eventItem => eventItem.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable("Events", table =>
            {
                table.HasCheckConstraint(
                    "CK_Events_StartBeforeEnd",
                    "[StartDateTime] < [EndDateTime]");

                table.HasCheckConstraint(
                    "CK_Events_CapacityPositive",
                    "[Capacity] > 0");
            });
        });
    }
}
```

There are several important pieces here. Read this class as **model configuration**, not as a hand-written SQL schema. EF Core combines conventions with the explicit rules in `OnModelCreating(...)` to build its model of CEMS persistence.

Some things are conventional—for example, an `Id` property is naturally treated as a key in common EF Core models. Other decisions are explicit here because CEMS wants a particular database shape or behavior, such as the `AspNetUsers` table name, the organizer string-key configuration, restricted deletes, and the Event check constraints.

#### `DbSet<T>`

These properties give EF Core query/save entry points for the current entities:

```csharp
public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
public DbSet<Category> Categories => Set<Category>();
public DbSet<Event> Events => Set<Event>();
```

Later, the controller can begin an Event query with:

```csharp
_context.Events
```

That expression does **not** mean “all Events have already been loaded into memory.” It gives EF Core a starting point from which a database query can be composed. Later calls such as `Where(...)`, `OrderBy(...)`, and `Include(...)` can add to that query before execution.

The same `DbSet<T>` can also participate in save operations. In other words, `DbSet<T>` is a persistence entry point for an entity type, not a replacement for a `List<T>` stored inside the application.

#### Event → Category

This relationship:

```csharp
entity.HasOne(eventItem => eventItem.Category)
    .WithMany(category => category.Events)
    .HasForeignKey(eventItem => eventItem.CategoryId)
    .OnDelete(DeleteBehavior.Restrict);
```

means many Events can refer to one Category through `CategoryId`.

#### Event → Organizer

This relationship:

```csharp
entity.HasOne(eventItem => eventItem.Organizer)
    .WithMany(user => user.OrganizedEvents)
    .HasForeignKey(eventItem => eventItem.OrganizerId)
    .OnDelete(DeleteBehavior.Restrict);
```

means many Events can be organized by one user reference through `OrganizerId`.

The navigation pairs make the relationships convenient to traverse in C#. The foreign-key columns are what preserve the actual references in relational storage.

A **foreign key** protects referential integrity at the database level. Even if normal application code supplies valid IDs, the database constraint prevents an `Event` from being stored with a `CategoryId` or `OrganizerId` that does not correspond to an existing related row. This protects the data from bugs, scripts, manual database changes, or other code paths that might bypass normal application logic.

`DeleteBehavior.Restrict` prevents a referenced Category or organizer row from being silently cascade-deleted along with Events. A **cascade delete** means that deleting one database row automatically causes related dependent rows to be deleted as well. Using `Restrict` requires those relationships to be handled explicitly instead of allowing a delete to remove related data automatically.


#### Database check constraints

The Event table includes:

```text
[StartDateTime] < [EndDateTime]
```

and:

```text
[Capacity] > 0
```

These constraints support the Event timing and positive-capacity business rules in persisted data.

The user table is named `AspNetUsers`. The current class is not yet a complete authenticated Identity user, but this table name and string key give later account work a compatible starting point.

### 3.4 Configure `DefaultConnection`

Open:

**File: `appsettings.Development.json`**

Use the Development configuration below:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CampusEventsDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

This connection string targets the local database `CampusEventsDb` through Windows integrated authentication. It contains no database password.

The connection string tells the configured provider **which database instance and database name to use**.

Production credentials, secret keys, and tokens do not belong in source control.

### 3.5 Register the `DbContext` in `Program.cs`

The entity model and connection string now exist, but application code still needs a standard way to obtain a correctly configured `ApplicationDbContext`. ASP.NET Core dependency injection provides that connection between configuration and consuming code.

The registration will effectively establish this path:

```text
code asks for ApplicationDbContext
        ↓
ASP.NET Core dependency injection
        ↓
uses DefaultConnection
        ↓
configures EF Core with UseSqlServer(...)
        ↓
constructs ApplicationDbContext
```

This is why controllers later request `ApplicationDbContext` in a constructor instead of constructing one manually.

Open:

**File: `Program.cs`**

At this point, before seed logic is added, use:

```csharp
using CampusEvents.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
```

The existing MVC registration remains:

```csharp
builder.Services.AddControllersWithViews();
```

The new registration is:

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
```

This tells ASP.NET Core how to construct `ApplicationDbContext` when application code requests it through dependency injection.

It also keeps configuration out of the controller. `EventsController` should know that it needs CEMS data; it should not need to know how to parse a connection string or configure the SQL Server provider. That separation keeps the controller focused on request behavior.

The conventional route remains unchanged:

```text
{controller=Home}/{action=Index}/{id?}
```

### Build It

```cmd
dotnet build
```

Do not create a migration until this model and configuration compile.

---

## 4. Create the Database with a Migration

At this point, CEMS has C# entity classes and EF Core model configuration, but that does **not** mean SQL Server already contains matching tables. The application model and the physical database schema are separate artifacts.

EF Core migrations provide a versioned bridge between them.

Keep these three states distinct:

```text
1. EF Core model
   C# entities + ApplicationDbContext configuration

2. Migration
   versioned instructions describing a schema change

3. Database schema
   the tables, columns, keys, and constraints that actually exist in SQL Server
```

A migration records how EF Core believes the relational schema should change to match the model at that point in the project. Creating a migration generates project files that describe the schema operations. Applying the migration executes the pending schema changes against the configured database.

The workflow is therefore:

```text
change/build EF Core model
        ↓
dotnet ef migrations add ...
        ↓
inspect generated migration
        ↓
dotnet ef database update ...
        ↓
SQL Server schema changes
```

This distinction becomes important in later lessons. Editing an entity class does not directly alter SQL Server, and generating a migration is not the same action as applying that migration to a database.

### 4.1 Check the EF CLI

```cmd
dotnet ef --version
```

The EF CLI major version should match the EF Core package major version used by the project. If the `ef` tool is not recognized you need to install it first.

For a global installation or update, from a terminal:

```cmd
dotnet tool install --global dotnet-ef
```
or

```cmd
dotnet tool update --global dotnet-ef
```

### 4.2 Create `InitialCemsCatalog`

Because `DefaultConnection` is stored in `appsettings.Development.json`, the EF command must tell the ASP.NET Core startup application to use the Development environment.

Run:

```cmd
dotnet ef migrations add InitialCemsCatalog -- --environment Development
```

The arguments after `--` are forwarded to the ASP.NET Core startup application. That causes Development configuration to load while EF Core creates the design-time `ApplicationDbContext`.

EF Core creates a `Migrations` folder whose generated migration files include the logical name `InitialCemsCatalog`.

Treat generated migrations as schema-change history, not as disposable output. The migration should correspond to the model change you intended. That is why the next step is inspection rather than immediately updating the database.

### 4.3 Inspect the migration

Open the generated migration. Look for evidence of the model you just created:

- `AspNetUsers`;
- `Categories`;
- `Events`;
- primary keys;
- `Events.CategoryId`;
- `Events.OrganizerId`;
- foreign keys to Categories and `AspNetUsers`;
- `CK_Events_StartBeforeEnd`;
- `CK_Events_CapacityPositive`.

A portion of the generated Events-table code should express ideas similar to this:

```csharp
migrationBuilder.CreateTable(
    name: "Events",
    columns: table => new
    {
        Id = table.Column<int>(nullable: false),
        Title = table.Column<string>(nullable: false),
        Description = table.Column<string>(nullable: false),
        Location = table.Column<string>(nullable: false),
        StartDateTime = table.Column<DateTime>(nullable: false),
        EndDateTime = table.Column<DateTime>(nullable: false),
        Capacity = table.Column<int>(nullable: false),
        Status = table.Column<int>(nullable: false),
        CreatedAt = table.Column<DateTime>(nullable: false),
        CategoryId = table.Column<int>(nullable: false),
        OrganizerId = table.Column<string>(nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Events", eventItem => eventItem.Id);
        table.CheckConstraint(
            "CK_Events_CapacityPositive",
            "[Capacity] > 0");
        table.CheckConstraint(
            "CK_Events_StartBeforeEnd",
            "[StartDateTime] < [EndDateTime]");
    });
```

Exact SQL Server annotations and generated type details may vary with the EF Core version used. Inspect the generated migration rather than replacing it with this abbreviated teaching fragment.

When you inspect the migration, translate it back into the concepts you modeled:

```text
entity identity             → primary key
Event.CategoryId            → foreign key
Event.OrganizerId           → foreign key
start-before-end rule       → check constraint
positive-capacity rule      → check constraint
```

The goal is not to memorize generated migration syntax. The goal is to verify that the database change EF Core intends to make matches the CEMS model you intended to build.

### 4.4 Apply the migration

Creating `InitialCemsCatalog` changed the project by adding migration files. The database itself still needs the schema update. `database update` is the step that applies the pending migration to `CampusEventsDb`.

Create the LocalDB schema:

```cmd
dotnet ef database update -- --environment Development
```

Now inspect `CampusEventsDb` with Visual Studio SQL Server Object Explorer or some other SQL Server inspection tool. The VS Code equivalent is:

**SQL Server Extension**  
**Publisher**: Microsoft  
**Extension ID**: ms-mssql.mssql  

It includes an actual Object Explorer where you can inspect servers, databases, tables, views, stored procedures, functions, and other database objects. It also supports table/schema designers, editing table data, query execution, execution plans, schema compare, backup/restore, and database object search.

Install it from a terminal:

```cmd
code --install-extension ms-mssql.mssql
```

or install it from the `Extensions (CTRL+SHIFT+X)` view in VS Code.

After installation, you'll see the MSSQL icon in the VS Code Activity Bar. From there you can add a SQL Server connection and expand it much like:

```text
SQL Server
└── localhost
    └── MyDatabase
        ├── Tables
        │   ├── dbo.Users
        │   └── dbo.Orders
        ├── Views
        ├── Stored Procedures
        ├── Functions
        └── Security
```

At this moment, the schema exists but the CEMS tables do not yet contain the classroom Event records.

---

## 5. Add Development Data

A database-backed catalog is difficult to reason about or verify with an empty database. We will therefore add a small **Development-only classroom dataset** with deliberately different Event states and dates.

The purpose is not merely to make the page look populated. Good seed data should let you test competing cases. If every Event were Published and upcoming, a page that accidentally rendered every row could appear correct. By storing Published, Draft, Cancelled, upcoming, and past records together, the database gives you evidence that the public query is enforcing the intended rules.

Keep the scope of this initializer clear:

```text
Development classroom data ≠ production data provisioning
```

The initializer is intentionally simple because its job is to create a predictable local state for this project stage.

The first seed set includes:

- two Published upcoming Events;
- one Draft upcoming Event;
- one Cancelled upcoming Event;
- one Published past Event;
- three Categories;
- one organizer reference.

This lets you prove that the public query is selecting the correct rows instead of simply rendering every Event.

### 5.1 Create the initializer

Create:

**File: `Data/DbInitializer.cs`**

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public static class DbInitializer
{
    private const string OrganizerId = "seed-organizer-001";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Events.AnyAsync())
        {
            return;
        }

        var organizer = new ApplicationUser
        {
            Id = OrganizerId,
            DisplayName = "Campus Events Office"
        };

        var academic = new Category
        {
            Name = "Academic"
        };

        var social = new Category
        {
            Name = "Social"
        };

        var career = new Category
        {
            Name = "Career"
        };

        context.Users.Add(organizer);
        context.Categories.AddRange(academic, social, career);

        await context.SaveChangesAsync();

        var createdAt = DateTime.Now;

        var libraryStart = DateTime.Today.AddDays(3).AddHours(18);
        var careerStart = DateTime.Today.AddDays(5).AddHours(12);
        var draftStart = DateTime.Today.AddDays(7).AddHours(15);
        var cancelledStart = DateTime.Today.AddDays(9).AddHours(20);
        var pastStart = DateTime.Today.AddDays(-10).AddHours(13);

        var events = new[]
        {
            new Event
            {
                Title = "Library Research Night",
                Description = "An evening workshop on research databases and source evaluation.",
                Location = "Main Library, Room 204",
                StartDateTime = libraryStart,
                EndDateTime = libraryStart.AddHours(2),
                Capacity = 60,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = academic.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Career Networking Lunch",
                Description = "Meet local employers and practice short professional introductions.",
                Location = "Student Center Ballroom",
                StartDateTime = careerStart,
                EndDateTime = careerStart.AddHours(2),
                Capacity = 120,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = career.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Welcome Mixer Planning Draft",
                Description = "A draft event used to demonstrate that Draft Events are not public.",
                Location = "Student Center Patio",
                StartDateTime = draftStart,
                EndDateTime = draftStart.AddHours(2),
                Capacity = 80,
                Status = EventStatus.Draft,
                CreatedAt = createdAt,
                CategoryId = social.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Outdoor Movie Night",
                Description = "A cancelled event retained in the database for lifecycle examples.",
                Location = "Campus Green",
                StartDateTime = cancelledStart,
                EndDateTime = cancelledStart.AddHours(3),
                Capacity = 200,
                Status = EventStatus.Cancelled,
                CreatedAt = createdAt,
                CategoryId = social.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Alumni Panel Archive",
                Description = "A past Published event used to demonstrate upcoming-event filtering.",
                Location = "Business Building Auditorium",
                StartDateTime = pastStart,
                EndDateTime = pastStart.AddHours(2),
                Capacity = 100,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = career.Id,
                OrganizerId = organizer.Id
            }
        };

        context.Events.AddRange(events);

        await context.SaveChangesAsync();
    }
}
```

The initializer begins with:

```csharp
if (await context.Events.AnyAsync())
{
    return;
}
```

That prevents the same classroom dataset from being inserted every time the application starts.

This guard is intentionally coarse: once any Event exists, the initializer assumes the classroom Event dataset has already been established. It is enough for the lesson's controlled Development workflow, but it does not compare and synchronize individual seed records. You will see the consequence later when changing the initializer requires rebuilding the Development database.

It saves the organizer and Categories before the Events because the integer Category IDs are generated when those Category rows are persisted.

The Event dates are relative to the day the seed code runs. So, we shall expect some events to become unavailable as days pass by in the course.

### 5.2 Run the initializer only in Development

Now evolve:

**File: `Program.cs`**

to its Lesson 2 startup form:

```csharp
using CampusEvents.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DbInitializer.SeedAsync(context);
}

app.Run();
```

The new Development block creates a service scope, resolves `ApplicationDbContext`, and calls:

```csharp
await DbInitializer.SeedAsync(context);
```

This is another example of dependency injection: the controller is not the only code that can receive registered services.

### Run It

```cmd
dotnet build
dotnet run
```

On the first Development run against the empty schema, inspect the database and confirm:

- `AspNetUsers` contains `seed-organizer-001`;
- Categories contains Academic, Social, and Career;
- Events contains five rows.

At this point, the database contains Event data, but `/Events` still uses the temporary Lesson 1 string. Persistence has been created first; the web request will use it next.

---

## 6. Query Published Upcoming Events

The database now contains multiple kinds of Event rows, but the public `/Events` endpoint must not simply retrieve all of them. The product rule for normal public browsing is narrower: only **Published upcoming Events** belong in this response.

That rule should be enforced while selecting data, not after broad data has already been loaded into the application.

```text
all stored Events
    ↓
database query applies public eligibility
    ↓
Published + upcoming Events only
    ↓
materialize the eligible result
    ↓
Razor renders that result
```

This is both a correctness boundary and a safer data-access pattern. Querying only the rows needed for the public request avoids unnecessary transfer and in-memory exposure, reduces the amount of work performed for the request, and makes it harder for later presentation code to accidentally reveal rows that were never meant to enter the public result set.

Razor's responsibility is presentation. It should not receive Draft and Cancelled Events and be trusted to hide them correctly.

Now remove the temporary Event-title logic from the public request flow.

The Lesson 1 controller already receives `ILogger<EventsController>`. Keep that dependency and add `ApplicationDbContext`.

Open:

**File: `Controllers/EventsController.cs`**

Replace the Lesson 1 `Index()` implementation with:

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
    private readonly ILogger<EventsController> _logger;
    private readonly ApplicationDbContext _context;

    public EventsController(
        ILogger<EventsController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Loading Published upcoming CEMS events.");

        var now = DateTime.Now;

        var events = await _context.Events
            .AsNoTracking()
            .Include(eventItem => eventItem.Category)
            .Where(eventItem =>
                eventItem.Status == EventStatus.Published &&
                eventItem.StartDateTime > now)
            .OrderBy(eventItem => eventItem.StartDateTime)
            .ToListAsync();

        return View(events);
    }
}
```

The important transition is this:

```text
Lesson 1:
controller creates "Library Research Night"

Lesson 2:
controller asks the database which Events qualify
```

### Read the query from top to bottom

The query starts from:

```csharp
_context.Events
```

`AsNoTracking()` is appropriate because the page is read-only:

```csharp
.AsNoTracking()
```

EF Core can track materialized entities so later changes to those objects can be detected and persisted. This action has no intention of editing the Event objects it reads; it only needs them long enough to render a response. `AsNoTracking()` tells EF Core that change tracking is unnecessary for this query.

The View needs the Category name, so the query loads that relationship:

```csharp
.Include(eventItem => eventItem.Category)
```

The `Category` navigation property describes the relationship in the model, but that does not mean every Event query automatically retrieves Category data. `Include(...)` asks EF Core to **eagerly load** the related Category needed for this result so Razor can use `eventItem.Category.Name` without depending on an additional loading mechanism.

The public browse rule is applied before the data is materialized:

```csharp
.Where(eventItem =>
    eventItem.Status == EventStatus.Published &&
    eventItem.StartDateTime > now)
```

That excludes:

- Draft Events;
- Cancelled Events;
- Published Events whose start time has already passed.

This predicate defines the public data set **before materialization**. That matters beyond presentation. A pattern such as “load every Event and hide the non-public ones in Razor” would over-fetch rows the request does not need, place non-public data in application memory unnecessarily, increase processing and transfer work, and make accidental exposure easier. The query should return the data the public endpoint is allowed and expected to work with.

The remaining Events are ordered chronologically:

```csharp
.OrderBy(eventItem => eventItem.StartDateTime)
```

Finally:

```csharp
.ToListAsync()
```

is the materialization point. Before this call, the LINQ operations describe the database work EF Core should perform. `ToListAsync()` causes the query to be executed and the returned rows to be materialized as C# `Event` objects in a list.

A useful reading is:

```text
_context.Events
    ↓
describe/query Event data

AsNoTracking + Include + Where + OrderBy
    ↓
compose the database query

ToListAsync()
    ↓
execute the query

SQL Server returns matching rows
    ↓
EF Core materializes Event objects
```

Keeping the public filter before materialization means SQL Server performs the filtering and the application receives the bounded result that the page actually needs.

### Why `async` and `await` are useful here

Database work involves I/O. An asynchronous action can wait for database I/O without unnecessarily blocking the request-handling thread for the entire wait.

The action signature:

```csharp
public async Task<IActionResult> Index()
```

and:

```csharp
await ... ToListAsync();
```

form one coherent asynchronous flow.

### Pause and Predict

Before changing the View, inspect the seed data and predict the public result set.

Which initial seeded Events satisfy both conditions?

```text
Status == Published
and
StartDateTime > now
```

Your expected answer should be:

1. Library Research Night
2. Career Networking Lunch

They should appear in that order because their start times are three and five days after the seed date.

### Request/Data Flow

The browse request now works like this:

```text
Browser requests /Events
        ↓
routing selects EventsController.Index()
        ↓
controller queries ApplicationDbContext
        ↓
EF Core translates the LINQ query
        ↓
SQL Server LocalDB filters and orders rows
        ↓
matching Events return as C# objects
        ↓
controller passes the collection to Razor
```

The important boundary is between **query description** and **materialized objects**. EF Core translates the composed LINQ expression into database work; SQL Server evaluates that work over persisted rows; only the matching result is turned back into C# objects for the controller.

The controller is now database-backed, but the existing View still expects `@model string`. The data source has changed, so the presentation contract must change with it. Fix that mismatch next.

---

## 7. Render Persisted Events

Open:

**File: `Views/Events/Index.cshtml`**

Replace the Lesson 1 string View with this first persisted version:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>

@{
    ViewData["Title"] = "Events";
}

<h1>Upcoming Campus Events</h1>

<section aria-labelledby="event-list-heading">
    <h2 id="event-list-heading" class="visually-hidden">Published upcoming events</h2>

    @foreach (var eventItem in Model)
    {
        <article>
            <h3>@eventItem.Title</h3>

            <p><strong>Category:</strong> @eventItem.Category.Name</p>
            <p><strong>Location:</strong> @eventItem.Location</p>
            <p>
                <strong>Starts:</strong>
                <time>@eventItem.StartDateTime.ToString("f")</time>
            </p>
        </article>
    }
</section>
```

The model has changed from:

```cshtml
@model string
```

to:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

The controller returns a collection, and the View now declares a compatible collection model.

This is an MVC contract. `return View(events);` passes an Event collection to Razor, so the View must declare a model type that can receive that collection. The database and EF Core are no longer involved at this point; Razor is working with the C# objects the controller supplied.

Each Event is rendered as an `<article>`, with semantic headings and a `<time>` element for its start date.

At this checkpoint, the Event title is deliberately plain heading text. The page that will display one Event has not been introduced yet, so the catalog does not link to a destination that does not exist.

### Run It

```cmd
dotnet build
dotnet run
```

Request:

```text
/Events
```

### What You Should See

The initial seed should produce:

1. **Library Research Night**
2. **Career Networking Lunch**

You should not see:

- Welcome Mixer Planning Draft;
- Outdoor Movie Night;
- Alumni Panel Archive.

The important evidence is not merely the rendered HTML. Compare the database rows with the page and identify which `Where(...)` condition excludes each omitted Event.

At this point, `/Events` is no longer displaying the temporary Lesson 1 string. It is rendering persisted relational data.

---

## 8. Add an Event Detail Page

The catalog now lists persisted Events. The next product problem is resource identity: when a visitor selects one Event, the server needs a stable way to identify which persisted Event is being requested.

The Event primary key provides that identity. The route can carry the ID to the controller, but the ID alone is **not** enough to decide whether the public request should succeed. The Event must also satisfy the public availability rule for this endpoint.

The desired lookup is therefore:

```text
requested Event ID
        +
Published status
        ↓
one public Event or no public Event
```

This is preferable to first loading any Event by ID and then deciding in the View whether it should be shown.

Next, add the concrete request that loads one Published Event by ID.

The existing conventional route is:

```text
{controller=Home}/{action=Index}/{id?}
```

A request can therefore place an integer in the optional `id` segment, for example:

```text
/Events/Details/1
```

Routing can bind that value to a parameter named `id`.

### 8.1 Add the controller action

Open:

**File: `Controllers/EventsController.cs`**

Add the following action below `Index()`:

```csharp
public async Task<IActionResult> Details(int id)
{
    var eventItem = await _context.Events
        .AsNoTracking()
        .Include(item => item.Category)
        .Include(item => item.Organizer)
        .SingleOrDefaultAsync(item =>
            item.Id == id &&
            item.Status == EventStatus.Published);

    if (eventItem is null)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("EventNotFound");
    }

    return View(eventItem);
}
```

Because this action uses `StatusCodes`, add this namespace to the controller imports:

```csharp
using Microsoft.AspNetCore.Http;
```

The query loads both relationships required by the page:

```csharp
.Include(item => item.Category)
.Include(item => item.Organizer)
```

It also requires:

```csharp
item.Status == EventStatus.Published
```

so a guessed Draft ID is not exposed through this public workflow.

`SingleOrDefaultAsync(...)` returns one matching Event or `null`. Because `Id` is the primary key, more than one matching Event should not exist.

Notice that the predicate combines identity and public eligibility:

```csharp
item.Id == id &&
item.Status == EventStatus.Published
```

The controller is not asking, “Does any Event with this ID exist?” and then separately exposing its status. It is asking, “Is there a Published Event matching this public request?” A nonexistent ID and a non-public Event ID therefore both produce no public resource from this query. That keeps the public endpoint focused on what the visitor is entitled to retrieve.

### 8.2 Create the Razor View

Create:

**File: `Views/Events/Details.cshtml`**

```cshtml
@model CampusEvents.Models.Event

@{
    ViewData["Title"] = Model.Title;
}

<article>
    <header>
        <h1>@Model.Title</h1>
    </header>

    <p>@Model.Description</p>

    <dl>
        <dt>Category</dt>
        <dd>@Model.Category.Name</dd>

        <dt>Location</dt>
        <dd>@Model.Location</dd>

        <dt>Starts</dt>
        <dd><time>@Model.StartDateTime.ToString("f")</time></dd>

        <dt>Ends</dt>
        <dd><time>@Model.EndDateTime.ToString("f")</time></dd>

        <dt>Organizer</dt>
        <dd>@Model.Organizer.DisplayName</dd>

        <dt>Configured capacity</dt>
        <dd>@Model.Capacity</dd>

        <dt>Current registration availability</dt>
        <dd>
            Live availability is not available yet because Registration data
            has not been introduced.
        </dd>
    </dl>
</article>

<p>
    <a asp-controller="Events" asp-action="Index">Back to Events</a>
</p>
```

This page displays the Event's:

- title;
- description;
- Category;
- location;
- start date/time;
- end date/time;
- organizer display name;
- configured capacity.

### Configured capacity is not live availability

Suppose the page displays:

```text
Configured capacity: 60
```

That tells you the Event's maximum participant capacity. It does **not** tell you how many places remain.

Eventually:

```text
remaining capacity
    = configured capacity
    - current registration count
```

The current registration count cannot be calculated yet because the real Registration relationship has not been introduced. For that reason, `FR-SRH-004` is still partial.

The correct approach is to display the configured capacity now and explicitly state that live availability is not available yet. Do not invent a fake remaining-capacity value.

---

## 9. Link the Catalog to Event Details

The destination action and its Razor View now exist. Return to:

**File: `Views/Events/Index.cshtml`**

Inside the `foreach`, replace the plain title heading with:

```cshtml
<h3 class="h4">
    <a asp-controller="Events"
       asp-action="Details"
       asp-route-id="@eventItem.Id">
        @eventItem.Title
    </a>
</h3>
```

The Tag Helpers use the controller, action, and Event ID to generate the link for that Event.

A generated URL can now take the form:

```text
/Events/Details/1
```

### Final `Views/Events/Index.cshtml`

After this change, the full View is:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>

@{
    ViewData["Title"] = "Events";
}

<h1>Upcoming Campus Events</h1>

<section aria-labelledby="event-list-heading">
    <h2 id="event-list-heading" class="visually-hidden">Published upcoming events</h2>

    @foreach (var eventItem in Model)
    {
        <article>
            <h3>
                <a asp-controller="Events"
                   asp-action="Details"
                   asp-route-id="@eventItem.Id">
                    @eventItem.Title
                </a>
            </h3>

            <p><strong>Category:</strong> @eventItem.Category.Name</p>
            <p><strong>Location:</strong> @eventItem.Location</p>
            <p>
                <strong>Starts:</strong>
                <time>@eventItem.StartDateTime.ToString("f")</time>
            </p>
        </article>
    }
</section>
```

Build and run the application, open `/Events`, and select one of the published upcoming Event titles. The request should load the corresponding persisted Event rather than a hard-coded page.

### Details request flow

Now that the concrete action exists, the request can be traced precisely:

```text
GET /Events/Details/{id}
    ↓
route binds {id}
    ↓
EventsController.Details(id)
    ↓
Published Event lookup + Category + Organizer
    ↓
found? ── no ──> 404 EventNotFound View
    │
   yes
    ↓
Views/Events/Details.cshtml
    ↓
HTML response
```

---

## 10. Handle Missing Events

A public resource lookup has two legitimate outcomes: a matching public Event exists, or it does not. “Not found” is therefore an expected HTTP result, not necessarily an application failure.

For this endpoint, the same public outcome is appropriate when:

```text
the requested ID does not exist
or
the requested ID does not identify a Published Event
        ↓
404 Not Found
```

The response should still be useful to a person, but it should also preserve correct HTTP semantics for browsers, automated clients, logs, and diagnostics.

The controller already checks whether the database query returned `null`. Now add the View used for the controlled response.

Create:

**File: `Views/Events/EventNotFound.cshtml`**

```cshtml
@{
    ViewData["Title"] = "Event Not Found";
}

<h1>Event Not Found</h1>

<p>
    The requested published event could not be found.
</p>

<p>
    <a asp-controller="Events" asp-action="Index">Back to Events</a>
</p>
```

When no matching Published Event exists, the action does two things:

```csharp
Response.StatusCode = StatusCodes.Status404NotFound;
return View("EventNotFound");
```

The browser receives a human-readable page **and** the HTTP response status remains 404.

### Run It

Request an ID that does not exist:

```text
/Events/Details/999999
```

### What You Should See

- the application remains running;
- the Event Not Found page appears;
- the response status is 404;
- no null-reference exception is thrown;
- no database details or stack trace are exposed.

A missing resource is an expected web condition. Handling it deliberately is better than allowing `null` to flow into code that assumes an Event exists.

The controlled 404 also avoids revealing an unnecessary distinction between “there is no Event with that ID” and “an Event with that ID exists but is not available through this public endpoint.” The public contract is simply that no matching public Event was found.

### Controller checkpoint

Because `EventsController` has evolved several times, compare your file with this final Lesson 2 version:

**File: `Controllers/EventsController.cs`**

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
    private readonly ILogger<EventsController> _logger;
    private readonly ApplicationDbContext _context;

    public EventsController(
        ILogger<EventsController> logger,
        ApplicationDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Loading Published upcoming CEMS events.");

        var now = DateTime.Now;

        var events = await _context.Events
            .AsNoTracking()
            .Include(eventItem => eventItem.Category)
            .Where(eventItem =>
                eventItem.Status == EventStatus.Published &&
                eventItem.StartDateTime > now)
            .OrderBy(eventItem => eventItem.StartDateTime)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> Details(int id)
    {
        var eventItem = await _context.Events
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Organizer)
            .SingleOrDefaultAsync(item =>
                item.Id == id &&
                item.Status == EventStatus.Published);

        if (eventItem is null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return View("EventNotFound");
        }

        return View(eventItem);
    }
}
```

---

## 11. Guided Practice and Verification

This section gives you evidence that the persistence layer, relationships, filtering, ordering, and error behavior are actually working.

### 11.1 Inspect the migration and schema

Open the `InitialCemsCatalog` migration and the LocalDB schema. Find:

- the Categories primary key;
- the `AspNetUsers` string primary key;
- the Events primary key;
- `Events.CategoryId`;
- `Events.OrganizerId`;
- the foreign key from Event to Category;
- the foreign key from Event to organizer;
- `CK_Events_StartBeforeEnd`;
- `CK_Events_CapacityPositive`.

Then answer:

1. Which column stores the Category relationship?
2. Which column stores the organizer relationship?
3. Which C# navigation properties correspond to those stored foreign-key relationships?
4. Why does a foreign key still matter when application code normally supplies a valid related ID?
5. What invalid Event would `CK_Events_CapacityPositive` reject?
6. Which artifacts came from the EF Core model, which came from the migration, and which now physically exist in LocalDB?

### 11.2 Compare database rows with `/Events`

Open `/Events` and compare it with all rows in the Events table.

Explain why:

- a Published upcoming Event appears;
- the Draft Event does not appear;
- the Cancelled Event does not appear;
- the Published past Event does not appear.

Point to the exact controller code responsible:

```csharp
.Where(eventItem =>
    eventItem.Status == EventStatus.Published &&
    eventItem.StartDateTime > now)
```

Then identify the code that determines ordering:

```csharp
.OrderBy(eventItem => eventItem.StartDateTime)
```

### 11.3 Follow the relationships on one Event

Open Library Research Night from the catalog.

Identify which information comes from which stored entity:

- title, description, location, times, and capacity → Event;
- Category name → Category through `CategoryId`;
- organizer display name → organizer row through `OrganizerId`.

The browser receives one HTML page, but the server can assemble that page from related relational records.

### 11.4 Small change — add one seed Event

Now make one bounded change using the seed pattern already demonstrated.

Open:

**File: `Data/DbInitializer.cs`**

Add this start-time variable with the existing date variables:

```csharp
var workshopStart = DateTime.Today.AddDays(6).AddHours(16);
```

Add this Event to the `events` array:

```csharp
new Event
{
    Title = "Study Skills Workshop",
    Description = "A practical session on planning, note-taking, and exam preparation.",
    Location = "Learning Commons, Room 110",
    StartDateTime = workshopStart,
    EndDateTime = workshopStart.AddHours(1),
    Capacity = 40,
    Status = EventStatus.Published,
    CreatedAt = createdAt,
    CategoryId = academic.Id,
    OrganizerId = organizer.Id
}
```

### Run It

```cmd
dotnet build
dotnet run
```

### Why a simple restart is not enough

The initializer exits when Events already exist:

```csharp
if (await context.Events.AnyAsync())
{
    return;
}
```

Your existing database has already been seeded, so editing the initializer does not rewrite those stored rows.

For this classroom Development database, stop the application and reset the local database:

```cmd
dotnet ef database drop --force -- --environment Development
```

Reapply the existing migration:

```cmd
dotnet ef database update -- --environment Development
```

Then run the application:

```cmd
dotnet run
```

The database is empty after the reset, so the initializer runs again and builds the six-Event seed set.

### Pause and Predict

Before opening `/Events`, predict the order of the Published upcoming Events.

The expected order is:

1. Library Research Night
2. Career Networking Lunch
3. Study Skills Workshop

The Draft, Cancelled, and Published-past records should still be absent.

### 11.5 Confirm persistence

Refresh `/Events`. Study Skills Workshop remains because it was inserted into LocalDB. `EventsController.Index()` is not recreating it on each request.

This is the core difference from Lesson 1's temporary string.

### 11.6 Verify a valid and invalid Event request

From `/Events`, open one linked Event and verify that its Category, organizer, time information, and configured capacity come from persisted data.

Then request:

```text
/Events/Details/999999
```

Explain the sequence:

1. routing binds `999999` to `id`;
2. the EF Core query finds no matching Published Event;
3. `eventItem` is `null`;
4. the controller sets HTTP 404;
5. `EventNotFound.cshtml` renders a controlled response.

### Final seed-data checkpoint

Your accepted end-of-lesson `Data/DbInitializer.cs` now contains six Events. Compare your file with this final version:

```csharp
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Data;

public static class DbInitializer
{
    private const string OrganizerId = "seed-organizer-001";

    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Events.AnyAsync())
        {
            return;
        }

        var organizer = new ApplicationUser
        {
            Id = OrganizerId,
            DisplayName = "Campus Events Office"
        };

        var academic = new Category { Name = "Academic" };
        var social = new Category { Name = "Social" };
        var career = new Category { Name = "Career" };

        context.Users.Add(organizer);
        context.Categories.AddRange(academic, social, career);

        await context.SaveChangesAsync();

        var createdAt = DateTime.Now;

        var libraryStart = DateTime.Today.AddDays(3).AddHours(18);
        var careerStart = DateTime.Today.AddDays(5).AddHours(12);
        var draftStart = DateTime.Today.AddDays(7).AddHours(15);
        var cancelledStart = DateTime.Today.AddDays(9).AddHours(20);
        var pastStart = DateTime.Today.AddDays(-10).AddHours(13);
        var workshopStart = DateTime.Today.AddDays(6).AddHours(16);

        var events = new[]
        {
            new Event
            {
                Title = "Library Research Night",
                Description = "An evening workshop on research databases and source evaluation.",
                Location = "Main Library, Room 204",
                StartDateTime = libraryStart,
                EndDateTime = libraryStart.AddHours(2),
                Capacity = 60,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = academic.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Career Networking Lunch",
                Description = "Meet local employers and practice short professional introductions.",
                Location = "Student Center Ballroom",
                StartDateTime = careerStart,
                EndDateTime = careerStart.AddHours(2),
                Capacity = 120,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = career.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Study Skills Workshop",
                Description = "A practical session on planning, note-taking, and exam preparation.",
                Location = "Learning Commons, Room 110",
                StartDateTime = workshopStart,
                EndDateTime = workshopStart.AddHours(1),
                Capacity = 40,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = academic.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Welcome Mixer Planning Draft",
                Description = "A draft event used to demonstrate that Draft Events are not public.",
                Location = "Student Center Patio",
                StartDateTime = draftStart,
                EndDateTime = draftStart.AddHours(2),
                Capacity = 80,
                Status = EventStatus.Draft,
                CreatedAt = createdAt,
                CategoryId = social.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Outdoor Movie Night",
                Description = "A cancelled event retained in the database for lifecycle examples.",
                Location = "Campus Green",
                StartDateTime = cancelledStart,
                EndDateTime = cancelledStart.AddHours(3),
                Capacity = 200,
                Status = EventStatus.Cancelled,
                CreatedAt = createdAt,
                CategoryId = social.Id,
                OrganizerId = organizer.Id
            },
            new Event
            {
                Title = "Alumni Panel Archive",
                Description = "A past Published event used to demonstrate upcoming-event filtering.",
                Location = "Business Building Auditorium",
                StartDateTime = pastStart,
                EndDateTime = pastStart.AddHours(2),
                Capacity = 100,
                Status = EventStatus.Published,
                CreatedAt = createdAt,
                CategoryId = career.Id,
                OrganizerId = organizer.Id
            }
        };

        context.Events.AddRange(events);

        await context.SaveChangesAsync();
    }
}
```

---

## 12. Check Your Understanding

Answer these without running the application first.

<details>
<summary>1. What is the difference between <code>CategoryId</code> and <code>Category</code> on an Event?</summary>

`CategoryId` is the foreign-key value stored on the Event row. It identifies which Category the Event belongs to.

`Category` is the navigation property that gives C# code access to the related `Category` object when EF Core loads that relationship.

</details>

<details>
<summary>2. Why does an Event contain both <code>OrganizerId</code> and <code>Organizer</code>?</summary>

`OrganizerId` is the foreign-key value stored on the Event row and identifies the related organizer.

`Organizer` is the navigation property that EF Core can populate with the related `ApplicationUser` object. Together, they provide both the stored relational reference and an object-oriented way to work with the organizer in C#.

</details>

<details>
<summary>3. What does <code>ApplicationDbContext</code> inherit from, and what role does it play in CEMS?</summary>

`ApplicationDbContext` inherits from EF Core's `DbContext`.

It represents CEMS's database context and provides EF Core with the configuration and entry points needed to query and save `ApplicationUser`, `Category`, and `Event` data. It also defines relationships and database constraints for the current model.

</details>

<details>
<summary>4. What is the purpose of the <code>InitialCemsCatalog</code> migration?</summary>

The `InitialCemsCatalog` migration records how EF Core believes the relational database schema should be created to match the C# model.

It creates the initial tables, primary keys, foreign keys, relationships, and check constraints needed for the CEMS catalog.

</details>

<details>
<summary>5. Why are Draft and Cancelled Events stored in the database but absent from normal public browsing?</summary>

Draft and Cancelled Events are valid stored Event records, but they do not satisfy the public browsing rule.

The public query requires an Event to have `EventStatus.Published`, so Draft and Cancelled Events remain in the database for their lifecycle state but are excluded from the public catalog.

</details>

<details>
<summary>6. Why is <code>.OrderBy(eventItem =&gt; eventItem.StartDateTime)</code> part of a product requirement rather than merely a visual preference?</summary>

CEMS requires upcoming Events to appear in chronological order by start time. The ordering therefore determines how the public catalog is supposed to behave, rather than serving only as a presentation choice.

Applying `OrderBy` in the query ensures the Events are returned from the database in the required order.

</details>

<details>
<summary>7. Why does the Index query use <code>.Include(eventItem =&gt; eventItem.Category)</code>?</summary>

The Index View displays each Event's Category name through `eventItem.Category.Name`.

`.Include(eventItem => eventItem.Category)` tells EF Core to load the related Category data along with the Events so that information is available when the View renders the catalog.

</details>

<details>
<summary>8. Why does the one-Event query include both Category and Organizer?</summary>

The Event detail page displays information from both related records: the Category name and the organizer's `DisplayName`.

The query therefore includes both `Category` and `Organizer` so EF Core loads the related objects needed by the View.

</details>

<details>
<summary>9. Why is configured capacity available now while current registration availability is not?</summary>

Configured capacity is stored directly on the Event through its `Capacity` property, so it is already available.

Current registration availability would require Registration data so CEMS could determine how many registrations currently exist and subtract that count from the configured capacity. The Registration relationship has not been introduced yet, so live availability cannot be calculated.

</details>

<details>
<summary>10. What HTTP status should a nonexistent Event request produce, and why is that preferable to an unhandled null-reference exception?</summary>

A nonexistent or unavailable Event request should produce HTTP `404 Not Found`.

A 404 is the appropriate controlled response when the requested resource does not exist. It allows CEMS to display a human-readable not-found page while preserving the correct HTTP status, instead of allowing `null` to reach code that assumes an Event exists and causing an unhandled exception.

</details>

<details>
<summary>11. What is the difference between the EF Core model, the <code>InitialCemsCatalog</code> migration, and the LocalDB schema?</summary>

The EF Core model is the persistence model EF Core builds from the C# entity classes and `ApplicationDbContext` configuration.

`InitialCemsCatalog` is the versioned schema-change description generated from that model for this stage of the project.

The LocalDB schema is the set of tables, columns, keys, and constraints that physically exists after the migration is applied with `dotnet ef database update`.

</details>

<details>
<summary>12. What does <code>ToListAsync()</code> change in the Index query?</summary>

Before `ToListAsync()`, the EF Core LINQ operations describe the database query to perform. `ToListAsync()` executes that query asynchronously and materializes the returned rows as C# `Event` objects.

Because the Published/upcoming predicate appears before this materialization point, SQL Server can restrict the result before those Event objects enter application memory.

</details>

<details>
<summary>13. Why is filtering non-public Events only in Razor a poor design even if the final HTML appears correct?</summary>

Razor is a presentation layer. Loading broad data and hiding rows only during rendering over-fetches data, unnecessarily exposes non-public rows to application memory, increases processing and transfer work, and creates more opportunities for accidental disclosure.

The public eligibility rule belongs in the data query so the controller receives only the rows that this public workflow should operate on.

</details>
---

## 13. Common Mistakes

### `UseSqlServer` is not recognized

Confirm the provider package is installed:

```cmd
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

and that `Program.cs` contains:

```csharp
using Microsoft.EntityFrameworkCore;
```

### `dotnet ef` is not recognized

Use the course-approved `dotnet-ef` tool version that matches the installed EF Core major version. Do not randomly install a newer incompatible major version.

### The LocalDB connection fails

Confirm the LocalDB instance:

```cmd
sqllocaldb info MSSQLLocalDB
```

and verify that `appsettings.Development.json` contains `DefaultConnection` with:

```text
Server=(localdb)\MSSQLLocalDB
```

In JSON, the backslash appears as `\\`.

### An EF command cannot find `DefaultConnection`

The connection string is in Development configuration. Schema-changing EF commands in this lesson must forward that environment, for example:

```cmd
dotnet ef database update -- --environment Development
```

### The migration is empty

Check that:

- `ApplicationDbContext` derives from `DbContext`;
- its `DbSet` properties exist;
- the SQL Server and Design packages are installed;
- `Program.cs` registers `ApplicationDbContext`.

### Seed insertion fails

Check that each Event satisfies:

```text
StartDateTime < EndDateTime
Capacity > 0
```

Also confirm that its Category and organizer rows exist.

### `/Events` shows Draft or Cancelled Events

The final query must include both:

```csharp
eventItem.Status == EventStatus.Published &&
eventItem.StartDateTime > now
```

Do not load every row and hide unwanted Events only in Razor.

That would move the public-data boundary into the presentation layer. It would also over-fetch rows, increase in-memory data exposure and resource use, and make the page dependent on every rendering path remembering to suppress non-public data. Keep the eligibility predicate in the database query.

### Category data fails in the catalog

The Index query needs:

```csharp
.Include(eventItem => eventItem.Category)
```

because the View reads `eventItem.Category.Name`.

### Organizer data fails on the Event page

The one-Event query needs:

```csharp
.Include(item => item.Organizer)
```

because the page reads the organizer's `DisplayName`.

### The Razor model does not match the controller

The final Index View expects:

```cshtml
@model System.Collections.Generic.IReadOnlyList<CampusEvents.Models.Event>
```

and the controller passes the Event collection returned by `ToListAsync()`.

The Event page expects:

```cshtml
@model CampusEvents.Models.Event
```

and the controller passes one Event object.

### Your new seed Event does not appear after editing `DbInitializer`

The existing database is already populated, so `Events.AnyAsync()` causes the initializer to return. Use the Development database reset workflow from the guided practice when you intentionally want to rebuild the classroom seed set.

---

## 14. What You Built

You started with a controller-created string and ended with a relationally persisted public Event catalog.

More importantly, the application now has several distinct layers that cooperate without becoming the same thing:

```text
C# entity model
    ↓
ApplicationDbContext + EF Core configuration
    ↓
migration history
    ↓
SQL Server LocalDB schema and rows

HTTP request
    ↓
controller composes a bounded EF Core query
    ↓
SQL Server filters/loads persisted data
    ↓
EF Core materializes C# objects
    ↓
Razor renders the supplied model
```

That separation is the foundation for the next lessons. Search and filtering can extend the query before materialization; authentication can later extend the user model; registration can introduce new relational data without replacing the persistence architecture established here.

The completed browse flow is now:

```text
GET /Events
    ↓
EventsController.Index()
    ↓
_context.Events
    ↓
Published + future-start filter
    ↓
OrderBy(StartDateTime)
    ↓
SQL Server LocalDB
    ↓
List<Event>
    ↓
Views/Events/Index.cshtml
    ↓
HTML response
```

Your application now has:

- SQL Server LocalDB configured through `DefaultConnection` in `appsettings.Development.json`;
- EF Core SQL Server and Design packages;
- `EventStatus` with Draft, Published, and Cancelled;
- Category, Event, and `ApplicationUser` persistence models;
- Event → Category and Event → Organizer relationships;
- database constraints for start/end ordering and positive capacity;
- the applied `InitialCemsCatalog` migration;
- a Development-only six-Event seed dataset;
- an asynchronous database-backed `/Events` page;
- Published/upcoming filtering;
- chronological ordering;
- a persisted Event detail page;
- configured capacity display;
- a final catalog link to each available Event;
- controlled 404 behavior for unavailable Event IDs.

The functional requirement state at this point is:

- `FR-SRH-001` — **complete**;
- `FR-SRH-002` — **complete**;
- `FR-SRH-003` — **complete**;
- `FR-SRH-004` — **partial**.

`FR-SRH-004` is partial for one specific reason: the Event page can display configured capacity, but live/current registration availability requires current Registration data that is not part of the application yet.

### Final file-state checklist

Before considering the lesson complete, confirm these material files match the code you built in this chapter:

- `Models/EventStatus.cs`
- `Models/Category.cs`
- `Models/Event.cs`
- `Models/ApplicationUser.cs`
- `Data/ApplicationDbContext.cs`
- `Data/DbInitializer.cs`
- `appsettings.Development.json`
- `Program.cs`
- `Controllers/EventsController.cs`
- `Views/Events/Index.cshtml`
- `Views/Events/Details.cshtml`
- `Views/Events/EventNotFound.cshtml`

The shared Events navigation from Lesson 1 remains unchanged:

```cshtml
<li class="nav-item">
    <a class="nav-link text-dark"
       asp-controller="Events"
       asp-action="Index">Events</a>
</li>
```

At the end of Lesson 2, `/Events` is no longer a temporary teaching page. It is the first real CEMS feature backed by persisted relational data.
