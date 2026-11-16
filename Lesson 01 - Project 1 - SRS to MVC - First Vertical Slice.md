# Lesson 1 — SRS, MVC, and the First Vertical Slice

Project #1 is the **Campus Events Management System (CEMS)**. Across the project, you will grow one ASP.NET Core application from a small MVC shell into a working campus-events system.

This lesson begins before any CEMS application exists. You will start with the product requirements, create an ASP.NET Core MVC project, and build the first real vertical slice of the application: a browser request to `/Events` will reach a controller action, pass a value to a Razor View, and return rendered HTML.

The goal is not to build the full event catalog yet. The goal is to understand the path a request follows through a server-rendered MVC application so that later features have a foundation you can reason about.

Lesson 1 therefore concentrates on one small but complete idea:

```text
browser request
    ↓
ASP.NET Core request pipeline
    ↓
routing
    ↓
controller action
    ↓
server-side value
    ↓
Razor View
    ↓
rendered HTML response
```

You will see several ASP.NET Core mechanisms around that flow—service registration, dependency injection, middleware, routing, View selection, Tag Helpers, and configuration. They are related, but they are not interchangeable. A major goal of this lesson is to give each mechanism a clear place in your mental model before later lessons add databases, authentication, validation, and authorization.

## What You Will Build

By the end of this lesson, your application will have:

- an ASP.NET Core MVC project named `CampusEvents`;
- a working conventional MVC route;
- an `EventsController` with an `Index()` action;
- a Razor View at `Views/Events/Index.cshtml`;
- one temporary event title passed from the controller to the View;
- an **Events** link in the shared navigation;
- logging that helps you see when the controller action runs;
- a Git repository with a standard .NET `.gitignore`;
- a final temporary event title of **Library Research Night**.

No database is involved yet. The Events page is a real MVC page, but its data is temporary server-side data rather than persisted CEMS data.

## What You Will Learn

After completing this lesson, you should be able to:

1. explain the purpose of a Software Requirements Specification;
2. distinguish required product behavior from implementation choices;
3. describe a server-rendered multi-page application;
4. trace an HTTP GET request through routing, a controller action, a Razor View, and an HTML response;
5. identify the ASP.NET Core MVC project files that matter to this first request flow;
6. explain service registration, middleware, and dependency injection at an introductory level;
7. pass a simple value from a controller to a Razor View;
8. use conventional routing and MVC Tag Helpers to navigate to `/Events`;
9. use logging or a breakpoint to observe request execution;
10. make a small controller-side change and predict how it affects the rendered page;
11. distinguish service registration, dependency injection, middleware, routing, and endpoint execution at an introductory level;
12. distinguish route selection from MVC View-location conventions;
13. explain the controller/View model contract for a strongly typed Razor View.

---

## 1. From Requirements to a Running Application

Before writing C# code, it is useful to know what you are building and where the rules for the product come from.

### The CEMS SRS

A **Software Requirements Specification**, or **SRS**, describes what the finished system must do. For CEMS, the SRS defines the required product behavior, user types, terminology, business rules, quality expectations, and project scope.

That is different from an implementation decision.

For example:

- “Visitors can browse appropriate campus events” is product behavior.
- “Use ASP.NET Core MVC and Razor Views” is an implementation constraint for this course project.
- “Name the project `CampusEvents`” is an implementation choice established by this lesson.

Keeping those categories separate is important. A framework may make many features possible, but the application should grow because the product requires them, not simply because they are available.

### CEMS terminology

You will see the following terms throughout the project:

| Term | Meaning in CEMS |
|---|---|
| **Visitor** | A person using the application without being authenticated |
| **Participant** | An authenticated user who can register for events |
| **Organizer** | An authenticated user who can manage events they own |
| **Administrator** | An authenticated user with application-wide administrative capabilities |
| **Event** | A scheduled campus activity |
| **Category** | A classification for an event |
| **Registration** | A participant's current reserved place in an event |
| **Draft** | An event that exists but is not yet published publicly |
| **Published** | An event that has been made publicly visible |
| **Cancelled** | A previously published event that is no longer scheduled to occur |

These are product concepts. In this lesson, you will use them as vocabulary only. You will **not** create CEMS `Event`, `Category`, or `Registration` classes yet.

### Requirement identifiers

The SRS uses identifiers to organize requirements. You will encounter groups such as:

- `FR-ACC-*` — accounts and authentication;
- `FR-SRH-*` — event discovery and search;
- `FR-EVT-*` — event management;
- `FR-REG-*` — event registration;
- `FR-ADM-*` — administration;
- `BR-*` — business rules;
- `NFR-*` — non-functional requirements.

Lesson 1 does not complete any functional `FR-*` requirement. The `/Events` page you create is an architectural first slice, not yet the real persisted event-browsing feature.

That distinction is important for the rest of the project. A vertical slice can be technically real without yet satisfying the full product requirement. In this lesson, the request, controller, View, navigation, and response are real; the Event data is intentionally temporary. Later lessons will evolve this same slice rather than throw it away and start over.

---

## 2. How a Server-Rendered MVC Application Works

CEMS is a **server-rendered multi-page application**, often shortened to **MPA**.

### Server-rendered pages

When a browser requests a page from a server-rendered ASP.NET Core MVC application, the main work happens on the server:

```text
Browser
   ↓ HTTP request
ASP.NET Core MVC application
   ↓ controller/application logic
Razor View renders HTML
   ↓ HTTP response
Browser receives HTML
```

The browser does not receive your C# controller code. The server executes the C# and Razor code, creates HTML, and sends that HTML to the browser.

This gives the server responsibility for the application logic that produces the page. The browser still participates in HTTP, navigation, forms, CSS, and any later client-side behavior, but it does not need your controller source code in order to display a server-rendered page.

### Multi-page navigation

In a multi-page application, navigating to another application URL normally produces another HTTP request.

In Lesson 1, the concrete application URLs you will use are:

```text
/
/Events
```

Moving between those pages causes the browser to make a request, ASP.NET Core to process it, and the server to return a rendered response.

### HTTP request and response

For the Events page, the browser will make a GET request conceptually like this:

```text
GET /Events
```

A successful response will contain HTML:

```text
HTTP status: 200 OK
Content-Type: text/html
Body: rendered HTML
```

The URL `/Events` is **not** the path to a Razor file. The browser does not directly open `Views/Events/Index.cshtml`. Routing and MVC conventions decide which server-side code handles the URL.

### MVC at a practical level

MVC stands for **Model–View–Controller**.

For this lesson, use this practical mental model:

- **Controller** — receives a routed request and decides what application action should occur.
- **Action method** — a public controller method that can handle a request.
- **Model/value** — server-side data supplied to a View.
- **View** — a Razor template that turns server-side values into HTML.

Your first CEMS flow will eventually look like this:

```text
/Events
   ↓
EventsController
   ↓
Index()
   ↓
temporary string value
   ↓
Views/Events/Index.cshtml
   ↓
HTML response
```

The “model” in Lesson 1 is deliberately just a `string`. You do not need a real `Event` class to learn the request/render cycle.

Also keep two different MVC conventions separate:

```text
routing convention
    → decides which controller action handles the request

View-location convention
    → helps MVC locate the Razor View after an action returns View()
```

For `/Events`, routing will eventually select `EventsController.Index()`. Later, when that action returns `View()`, MVC will use the controller/action names to look for `Views/Events/Index.cshtml`. Those are two different stages of the request.

---

## 3. Create and Run the CEMS Project

You are starting with no CEMS application, so the first step is to create a standard ASP.NET Core MVC project.

### Check the installed .NET SDK

Open **Windows Command Prompt** and run:

```cmd
dotnet --version
```

This prints the installed .NET SDK version. Use the SDK version approved for your course and keep using that version throughout the project rather than changing versions between lessons.

### Create the MVC project

Create a project named `CampusEvents`:

```cmd
dotnet new mvc -n CampusEvents
```

Move into the project directory:

```cmd
cd CampusEvents
```

This lesson establishes two names that later code will use:

- **Project name:** `CampusEvents`
- **Root namespace:** `CampusEvents`

### Build before making changes

Compile the generated project:

```cmd
dotnet build
```

A successful build gives you a known-good baseline. If a later change causes a compile error, you know the project was working before your edits.

### Run the generated application

Start the application:

```cmd
dotnet run
```

Use the HTTP or HTTPS URL printed in the console. The port number may differ from one computer to another.

At this point, you are looking at the generated MVC template. It is not yet CEMS-specific, but it proves that the project can build and run.

---

## 4. Tour the Parts of the Project You Need Today

You do not need to memorize every generated file. Focus on the pieces involved in the first request flow.

```text
CampusEvents/
│
├── Controllers/
│   └── HomeController.cs
│
├── Models/
│   └── ErrorViewModel.cs
│
├── Views/
│   ├── Home/
│   ├── Shared/
│   │   └── _Layout.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
│
├── wwwroot/
│
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── CampusEvents.csproj
```

### `Program.cs`

This is the application startup file. It registers services, builds the app, configures the request pipeline, and maps routes.

A useful first mental model is:

```text
startup
│
├─ register services
│     ↓
│  dependency-injection container knows what can be provided
│
├─ build the application
│
├─ configure the request pipeline
│     ↓
│  ordered middleware processes requests
│
└─ map endpoints/routes
      ↓
   requests can reach controller actions
```

You do not need the internals of these systems yet. You do need to recognize that each line in `Program.cs` participates in a different part of startup or request handling.

### `Controllers/`

Controllers contain action methods that can respond to routed requests.

### `Views/`

Views are Razor templates that render HTML on the server.

### `Views/Shared/_Layout.cshtml`

The shared layout contains page structure used across multiple Views, including site navigation.

### `wwwroot/`

This folder contains static files such as CSS, JavaScript, and images.

### `appsettings.json` and environment-specific settings

These files hold configuration. Normal non-secret configuration can live here, but passwords, API keys, tokens, and production credentials should not be committed to source control.

### What about `ErrorViewModel`?

The MVC template may include `Models/ErrorViewModel.cs`. That file supports template error handling. It is not a CEMS domain entity.

At this stage, there is still no CEMS `Event`, `Category`, or `Registration` model.

---

## 5. Understand `Program.cs`

Open `Program.cs`.

The exact generated file can vary slightly across .NET versions, but the Lesson 1 baseline is equivalent to the following:

### File: `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register MVC controller and Razor View services.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Use a controlled error path outside Development.
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

app.Run();
```

If the project generated by your course-approved SDK contains small template differences, keep the approved equivalent rather than changing framework versions simply to make the file look identical.

Before reading the individual lines, divide `Program.cs` into three questions:

```text
1. What capabilities does the application need?
   → register services

2. How should an incoming request move through the application?
   → configure middleware

3. Which endpoints can ultimately handle requests?
   → map routes/endpoints
```

This separation will matter later. For example, registering MVC services is not the same operation as defining a route that can reach a controller.

### Service registration

This line is the main service-registration statement you need today:

```csharp
builder.Services.AddControllersWithViews();
```

ASP.NET Core uses a built-in **dependency injection container**. Applications register services during startup, and framework components can receive those services when they are created.

For now, think of service registration as telling ASP.NET Core:

> “This application uses MVC controllers and Razor Views, so make the required framework services available.”

The broader dependency-injection pattern is:

```text
application/framework registers services
        ↓
ASP.NET Core creates an object such as a controller
        ↓
the constructor declares what it needs
        ↓
the DI container supplies compatible registered services
```

Later in the lesson, ASP.NET Core will supply an `ILogger<EventsController>` to your controller through dependency injection. In Lesson 2, the same general mechanism will supply the application's database context. The service changes; the dependency-injection idea remains the same.

### Middleware and the request pipeline

These statements participate in configuring how requests move through the application:

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
```

You do not need to study middleware internals yet. At this level, think of middleware as ordered stages in the application's request-processing pipeline.

Conceptually:

```text
incoming request
    ↓
HTTPS handling
    ↓
static-file handling
    ↓
routing
    ↓
authorization
    ↓
selected endpoint
    ↓
response travels back through the pipeline
```

The exact behavior of each middleware component can become more sophisticated than this diagram suggests, but the important idea is that **order matters**. A request moves through the configured pipeline, and some stages depend on work established by earlier stages.

`UseRouting()` participates in matching the incoming request to a configured endpoint, such as a controller action selected through the application's routing rules.

The template contains `UseAuthorization()`, but CEMS authorization behavior is not implemented in this lesson. You are simply preserving the normal MVC startup pipeline so later authentication and authorization features can evolve from an ordinary MVC application.

### Conventional routing

This route is central to the lesson:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

Do not confuse this with `AddControllersWithViews()`:

```text
AddControllersWithViews()
    → registers MVC capabilities/services

MapControllerRoute(...)
    → defines a conventional way requests can reach controller actions
```

One prepares the framework services; the other participates in endpoint mapping.

The route pattern has three pieces:

```text
controller / action / optional id
```

It also provides defaults:

- controller: `Home`
- action: `Index`
- `id`: optional

That means the root URL:

```text
/
```

can be handled by:

```text
HomeController.Index()
```

#### Attribute routing exists, but CEMS will introduce it later

ASP.NET Core MVC also supports **attribute routing**, where route templates and HTTP-method attributes are placed directly on controllers or actions.

For example, later code can use attributes such as:

```csharp
[Route("organizer/events")]
```

or:

```csharp
[HttpGet("{id:int}/edit")]
```

Lesson 1 will **not** switch to that routing style yet. The generated conventional route gives you one simple rule to trace while you are learning the request/render cycle.

Later, when CEMS gains Organizer workflows with multiple GET and POST operations—Create, Edit, Publish, Cancel, and Delete—attribute routing will be introduced because explicit route definitions will solve a real readability and route-organization problem. At that point you will be able to compare the two styles from experience rather than treating attribute routing as unrelated syntax.

For now, keep:

```text
{controller=Home}/{action=Index}/{id?}
```

and learn how one conventional route maps URLs to controller actions.


### Trace the generated route

Open `Controllers/HomeController.cs` and locate its `Index()` action. Then look at:

```text
Views/Home/Index.cshtml
```

Request the root page in the browser and trace the flow:

```text
Browser GET /
    ↓
default conventional route
    ↓
controller = Home
    ↓
action = Index
    ↓
HomeController.Index()
    ↓
return View()
    ↓
Views/Home/Index.cshtml
    ↓
Razor renders HTML
    ↓
HTML response returns to browser
```

The browser only knows that it requested `/`. MVC decides which controller action and View are involved.

Notice that two decisions occur:

```text
GET /
    ↓
routing selects HomeController.Index()
    ↓
the action returns View()
    ↓
View-location conventions select Views/Home/Index.cshtml
```

Routing gets the request **to an action**. The View-location convention helps MVC find the Razor template **after the action asks for a View**.

### Check Your Understanding

1. What does `AddControllersWithViews()` make available to the application?
2. How is service registration different from `MapControllerRoute(...)`?
3. What role does `UseRouting()` play at a high level?
4. Why can `/` reach `HomeController.Index()` even though the URL does not contain `Home/Index`?
5. After `HomeController.Index()` returns `View()`, what convention helps MVC locate `Views/Home/Index.cshtml`?

---

## 6. Build the First CEMS Events Page

Now you will add the first CEMS-specific request flow.

### 6.1 Create `EventsController`

A controller class inherits from `Controller`, which gives it access to MVC behavior such as the `View(...)` methods used to return Razor Views.

An **action method** is a public method on a controller that MVC can select to handle a request.

### File: `Controllers/EventsController.cs`

Create the file with this code:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
    private readonly ILogger<EventsController> _logger;

    public EventsController(ILogger<EventsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Handling request for the CEMS Events page.");
        return View();
    }
}
```

Several ideas appear here for the first time.

#### `EventsController : Controller`

The class name ends in `Controller`, which matches MVC controller conventions. Inheriting from `Controller` gives the class MVC controller functionality.

#### Constructor injection

The constructor receives:

```csharp
ILogger<EventsController> logger
```

You did not write `new Logger(...)`. ASP.NET Core creates the controller and supplies a compatible logger through dependency injection.

Connect this back to `Program.cs`:

```text
ASP.NET Core logging services exist
        ↓
MVC creates EventsController
        ↓
constructor requests ILogger<EventsController>
        ↓
dependency injection supplies it
        ↓
controller stores and uses the service
```

The controller declares a dependency; it does not take responsibility for constructing the logging infrastructure itself.

The field:

```csharp
private readonly ILogger<EventsController> _logger;
```

stores that service so the action can use it.

#### `Index()`

The method:

```csharp
public IActionResult Index()
```

is the controller action for the Events page.

This line creates a visible diagnostic message when the action runs:

```csharp
_logger.LogInformation("Handling request for the CEMS Events page.");
```

This line asks MVC to render the View associated with the current controller and action:

```csharp
return View();
```

At this moment, the controller contains no database access and no real CEMS Event object.

### 6.2 Create the Razor View

If you request `/Events` before creating the View, MVC can find the controller action but cannot find the corresponding Razor file.

Create the folder `Views/Events` if necessary, then create the View.

### File: `Views/Events/Index.cshtml`

```cshtml
@{
    ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>
    This is the first CEMS Events page. Event information is temporary in
    Lesson 1 and is not loaded from a database.
</p>

<section>
    <h2>Temporary Event Preview</h2>
    <p>Campus Welcome Mixer</p>
    <p>Student Center</p>
</section>
```

When `EventsController.Index()` calls `View()` without naming a View explicitly, MVC uses conventions to look for:

```text
EventsController + Index()
        ↓
Views/Events/Index.cshtml
```

This is a **View-location convention**, not the routing convention that selected `EventsController.Index()` in the first place.

```text
/Events
    ↓ routing
EventsController.Index()
    ↓ return View()
View-location convention
    ↓
Views/Events/Index.cshtml
```

Razor executes on the server. The browser never opens the `.cshtml` file directly.

### Run It

Build the project again:

```cmd
dotnet build
```

Run the application if it is not already running:

```cmd
dotnet run
```

Request:

```text
/Events
```

### What You Should See

The page should include:

```text
Campus Events
Temporary Event Preview
Campus Welcome Mixer
Student Center
```

You should also see the controller log message in the application output:

```text
Handling request for the CEMS Events page.
```

### Why This Matters

You now have a real MVC vertical slice:

```text
GET /Events
   ↓
EventsController.Index()
   ↓
Views/Events/Index.cshtml
   ↓
HTML response
```

The event title is still hard-coded directly in the View. The next step moves that title to the controller so you can see data travel from C# into Razor.

### 6.3 Pass a value from the controller to the View

You are not creating a CEMS `Event` class yet. Instead, use one `string`.

This lets you learn the **controller/View model contract** without introducing domain modeling yet:

```text
controller supplies a value of some type
        ↓
View declares the type it expects
        ↓
the two types must agree
```

For this lesson the contract is intentionally simple:

```text
controller supplies: string
View declares:       @model string
```

Later lessons will keep the same idea while replacing `string` with collections, entities, and page-specific View Models.

### File: `Controllers/EventsController.cs`

Change only the `Index()` action to:

```csharp
public IActionResult Index()
{
    _logger.LogInformation("Handling request for the CEMS Events page.");

    var featuredEventTitle = "Campus Welcome Mixer";

    return View(model: featuredEventTitle);
}
```

The action now creates a value on the server and passes it to the View.

The call:

```csharp
return View(model: featuredEventTitle);
```

returns a View result and supplies the string as the View's model. Here, lowercase `model:` is the name of the C# method parameter receiving `featuredEventTitle`.

Now update the Razor View so it declares the type it expects.

### File: `Views/Events/Index.cshtml`

```cshtml
@model string

@{
    ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>
    This is the first CEMS Events page. Event information is temporary in
    Lesson 1 and is not loaded from a database.
</p>

<section>
    <h2>Temporary Event Preview</h2>
    <p><strong>@Model</strong></p>
    <p>Student Center</p>
</section>
```

The directive:

```cshtml
@model string
```

means that this Razor View expects its model to be a `string`. Lowercase `@model` is a **Razor directive** that declares the model type; it does not receive the model value.

The expression:

```cshtml
@Model
```

reads the string that the controller passed to the View. Uppercase `@Model` is the **C# property** that exposes that value inside the View.

> **Naming note:** `model:`, `@model`, and `@Model` are three different things. Think of this as a small ASP.NET naming hangover—as if the framework designers had one drink too many before choosing three nearly identical labels—or, perhaps more charitably, as the result of naming important framework mechanisms before anyone had enough coffee. Capitalization matters.
>
> - `model:` — C# named argument that supplies the value to `View(...)`
> - `@model` — Razor directive that declares the model type
> - `@Model` — C# property used in the View to access the model value

Follow the **value** from start to finish:

```text
var featuredEventTitle = "Campus Welcome Mixer"
        ↓
return View(model: featuredEventTitle)
        ↓
View's Model property receives the string
        ↓
@Model
        ↓
rendered HTML
```

Separately, `@model string` declares that the View's `Model` property has type `string`. It is not a step through which the value flows.

The controller/View contract can therefore be summarized as:

```text
value:
"Campus Welcome Mixer"

runtime type supplied by controller:
string

type expected by Razor View:
@model string

result:
compatible contract
```

If the controller later supplies a different type, the View must evolve to expect that type. You will see exactly that transition in Lesson 2 when the page stops receiving one temporary string and starts receiving persisted Event data.

### Run It

Save both files and refresh `/Events`.

### What You Should See

The visible title still says:

```text
Campus Welcome Mixer
```

The output looks similar, but the source of the value changed. The controller now supplies the title, and the View decides how to render it.

---

## 7. Add Events to the Site Navigation

Typing `/Events` works, but a normal application should provide navigation.

Open the shared layout and add an Events item beside the existing generated navigation links.

### File: `Views/Shared/_Layout.cshtml`

Add this list item to the main navigation list:

```cshtml
<li class="nav-item">
    <a class="nav-link text-dark"
       asp-controller="Events"
       asp-action="Index">Events</a>
</li>
```

Do not replace the rest of the generated layout.

The attributes:

```cshtml
asp-controller="Events"
asp-action="Index"
```

are MVC Tag Helper attributes. They describe the controller/action target rather than hard-coding a URL string.

Conceptually:

```text
View expresses destination:
EventsController.Index()
        ↓
MVC routing participates in URL generation
        ↓
browser receives an ordinary href
```

That distinction matters. The Razor source expresses the MVC destination; the rendered HTML still contains a normal link that a browser understands.

A useful way to think about it is:

```text
hard-coded URL
    → View assumes the URL shape directly

controller/action Tag Helper
    → View names the MVC destination
      and routing helps generate the URL
```

The generated `_ViewImports.cshtml` normally enables the standard MVC Tag Helpers.

### Run It

Refresh the application and click **Events** in the navigation bar.

### What You Should See

The browser should navigate to the Events page, and the controller log message should appear again.

The link proves that shared navigation, routing, controller selection, and Razor rendering are working together.

---

## 8. Establish Git and Safe Configuration Habits

You now have a meaningful working state worth recording in source control.

Git will appear throughout the project, so use one simple mental model:

```text
working files
    ↓ git add
staging area
    ↓ git commit
repository history
```

`git status` lets you inspect where files currently stand. The staging area lets you choose what will become part of the next commit, and a commit records a project state in repository history.

This lesson does not attempt to teach every Git feature. The goal is to establish a safe, repeatable project workflow before the codebase becomes more complex.

### Create a standard .NET `.gitignore`

From the `CampusEvents` directory, run:

```cmd
dotnet new gitignore
```

This creates a standard ignore file for generated build and IDE artifacts.

### Initialize the repository

Run:

```cmd
git init
```

Stage the current files:

```cmd
git add .
```

Commit the working MVC shell:

```cmd
git commit -m "Create CEMS MVC shell and Events page"
```

### Connect a GitHub remote if your course uses one

If you have been given or have created a GitHub repository for the project, connect it with commands such as:

```cmd
git remote add origin https://github.com/your-account/campus-events.git
git branch -M main
git push -u origin main
```

Replace the example URL with the actual repository URL for your project.

### Configuration safety

The project does not need secret credentials yet, but establish this rule now:

> **Do not commit passwords, production connection strings, API keys, access tokens, or other secrets.**

`appsettings.json` and environment-specific configuration files can contain ordinary non-secret settings. Sensitive values should use the approved configuration approach when they are introduced later.

---

## 9. Guided Practice — Observe the Request and Change the Temporary Event

This practice uses the code you have already built. You are not designing a new feature.

### 9.1 Observe `EventsController.Index()` running

Use either a breakpoint or the existing log message.

#### Option A — Breakpoint

Place a breakpoint inside:

```csharp
public IActionResult Index()
```

Run the application under the debugger and request:

```text
/Events
```

When execution pauses, identify:

- the `EventsController` class;
- the `Index()` action;
- the local `featuredEventTitle` variable;
- the `return View(...)` line.

Continue execution and watch the page render.

#### Option B — Logging

Run the application normally and watch the console while requesting `/Events`.

You should see:

```text
Handling request for the CEMS Events page.
```

The message is evidence that the browser request caused the server-side action to execute.

### 9.2 Trace the current value

Before changing anything, find this controller line:

```csharp
var featuredEventTitle = "Campus Welcome Mixer";
```

Then find this Razor expression:

```cshtml
<strong>@Model</strong>
```

The value flow is:

```text
controller creates string
        ↓
controller passes string through View(model: ...)
        ↓
View's Model property contains the string
        ↓
Razor inserts @Model into HTML
```

Remember: `@model string` declares the type of `Model`; it does not receive the value.

### 9.3 Make the bounded change

Now make the required small change.

### File: `Controllers/EventsController.cs`

Change:

```csharp
var featuredEventTitle = "Campus Welcome Mixer";
```

to:

```csharp
var featuredEventTitle = "Library Research Night";
```

Before refreshing the browser, predict the result. You are changing the controller value, not the Razor markup.

Refresh `/Events`.

### What You Should See

The page should now display:

```text
Library Research Night
```

The View did not need to change because it still renders `@Model`.

This is the final temporary title for Lesson 1.

### Why This Matters

This small change demonstrates an important separation:

- the controller supplies data;
- the View controls presentation;
- Razor combines the two on the server;
- the browser receives the resulting HTML.

---

## 10. Trace `/Events` from Browser to HTML

With the final Lesson 1 value in place, trace the request from beginning to end.

```text
Browser requests /Events
        ↓
ASP.NET Core receives the request
        ↓
Routing selects EventsController.Index()
        ↓
Index() creates/passes "Library Research Night"
        ↓
MVC renders Views/Events/Index.cshtml
        ↓
Razor generates HTML on the server
        ↓
HTML response is returned to the browser
```

Here is what each step means.

### 1. The browser requests `/Events`

The browser sends an HTTP GET request. It does not request a `.cshtml` file and does not execute any C#.

### 2. ASP.NET Core receives the request

The request enters the pipeline configured in `Program.cs`.

### 3. Conventional routing selects the controller and action

The route is:

```text
{controller=Home}/{action=Index}/{id?}
```

For `/Events`, MVC determines:

```text
controller = Events
action = Index
```

That selects:

```text
EventsController.Index()
```

### 4. The action creates and passes the value

The final action contains:

```csharp
public IActionResult Index()
{
    _logger.LogInformation("Handling request for the CEMS Events page.");

    var featuredEventTitle = "Library Research Night";

    return View(model: featuredEventTitle);
}
```

The controller creates the string and passes it to the View.

### 5. MVC locates the Razor View

Because the action returns `View()` without naming a different View, the normal convention points to:

```text
Views/Events/Index.cshtml
```

### 6. Razor generates HTML

The View declares the type of its model with:

```cshtml
@model string
```

and accesses the model value through:

```cshtml
<strong>@Model</strong>
```

Razor runs on the server and inserts `Library Research Night` into the generated HTML.

### 7. The browser receives HTML

The browser receives the rendered response. It does not receive the C# controller source and it does not receive a JSON object.

That is why this action:

```csharp
return View(model: featuredEventTitle);
```

produces an HTML page: it returns a View result, MVC renders the Razor View, and Razor produces HTML.

At this point you can also place the major framework mechanisms in order:

```text
startup:
AddControllersWithViews()
    → MVC services are available

request:
middleware pipeline
    ↓
routing
    ↓
EventsController.Index()

controller construction:
dependency injection supplies ILogger<EventsController>

action result:
return View(model: featuredEventTitle)
    ↓
View-location convention finds Views/Events/Index.cshtml
    ↓
Razor uses the strongly typed model contract
    ↓
HTML response
```

No database participates in this request. The title exists only as a temporary string created during the request.

---

## 11. Check Your Understanding

Answer these questions using the code you just worked with.

<details>
<summary>1. What causes <code>EventsController.Index()</code> to run when the browser requests <code>/Events</code>?</summary>

The request enters the ASP.NET Core pipeline, and the conventional route matches the URL to the `Events` controller and its default `Index` action. MVC then selects `EventsController.Index()` to handle the request.

</details>

<details>
<summary>2. How is <code>AddControllersWithViews()</code> different from <code>MapControllerRoute(...)</code>?</summary>

`AddControllersWithViews()` registers the MVC controller and Razor View services the application needs.

`MapControllerRoute(...)` defines a conventional route pattern that helps incoming requests reach controller actions.

One prepares MVC framework capabilities; the other maps request URLs to endpoints.

</details>

<details>
<summary>3. What does dependency injection contribute when ASP.NET Core creates <code>EventsController</code>?</summary>

`EventsController` declares that it needs an `ILogger<EventsController>` through its constructor. ASP.NET Core creates the controller and uses dependency injection to supply a compatible logger instead of requiring the controller to construct the logging service itself.

</details>

<details>
<summary>4. Why does <code>EventsController</code> inherit from <code>Controller</code>?</summary>

Inheriting from `Controller` gives `EventsController` MVC controller behavior, including access to methods such as `View(...)` that create results for Razor Views.

</details>

<details>
<summary>5. What does <code>return View(model: featuredEventTitle)</code> do conceptually?</summary>

It creates a View result and supplies `featuredEventTitle` as the model value that the Razor View will receive. MVC then locates the appropriate View and renders that model into HTML.

</details>

<details>
<summary>6. What is the difference between lowercase <code>@model</code> and uppercase <code>@Model</code>?</summary>

`@model` is a Razor directive that declares the type the View expects.

`@Model` is the C# property used inside the View to access the actual model value supplied by the controller.

</details>

<details>
<summary>7. In <code>return View(model: featuredEventTitle)</code>, what does the lowercase <code>model:</code> label represent?</summary>

`model:` is a C# named argument. It identifies the `View(...)` method parameter that receives `featuredEventTitle`.

It is separate from Razor's `@model` directive and the View's `@Model` property.

</details>

<details>
<summary>8. What is the controller/View model contract in this lesson?</summary>

The controller supplies a value of type `string`, and the Razor View declares `@model string`.

The supplied runtime value and the type expected by the View therefore agree.

</details>

<details>
<summary>9. How is routing to <code>EventsController.Index()</code> different from MVC locating <code>Views/Events/Index.cshtml</code>?</summary>

Routing decides which controller action handles the incoming request.

After `EventsController.Index()` returns `View()`, MVC uses its View-location conventions to locate the Razor file associated with that controller and action.

These are two different stages of the request.

</details>

<details>
<summary>10. Where does Razor execute: in the browser or on the server?</summary>

Razor executes on the server. It combines the View template with server-side model data and produces HTML.

The browser receives the rendered HTML rather than executing the Razor or C# source.

</details>

<details>
<summary>11. What does the browser ultimately receive from the <code>/Events</code> request?</summary>

The browser receives an HTTP response containing rendered HTML.

It does not receive the controller's C# source code or the Razor processing instructions.

</details>

<details>
<summary>12. Why is the temporary Events page not yet the real CEMS event catalog?</summary>

The MVC request/render cycle is real, but the Event data is still a temporary server-side string created during the request.

No persisted Event model or database-backed catalog exists yet.

</details>

<details>
<summary>13. If you changed the controller string again but left <code>&lt;strong&gt;@Model&lt;/strong&gt;</code> unchanged, what would you expect to happen?</summary>

The page would display the new string supplied by the controller.

The Razor markup would not need to change because it still renders whatever string value is available through `@Model`.

</details>

---

## 12. Common Mistakes

### `/Events` returns a View-not-found error

Check the View path:

```text
Views/Events/Index.cshtml
```

MVC uses the controller and action names to locate the conventional View path.

If the controller action is running—you can see the log message—but MVC reports that the View cannot be found, routing has already done its job. The problem is now in View selection/location, not in the route that selected the action.

### `/Events` returns 404

Check that:

- the class is named `EventsController`;
- it inherits from `Controller`;
- `Index()` is public;
- the default conventional route still exists in `Program.cs`.

### The View reports a model-type error

The View expects:

```cshtml
@model string
```

The controller must therefore pass a string:

```csharp
return View(model: featuredEventTitle);
```

A different model type would require a corresponding View change.

### `@model` and `@Model` look like the same thing

They are different and case-sensitive:

```text
@model   = Razor directive that declares the model type
@Model   = C# property that accesses the model value
```

The `model:` in `View(model: featuredEventTitle)` is yet another use: it is the C# parameter name used to supply that value.

### The Events navigation link does not work

Check the Tag Helper target:

```cshtml
asp-controller="Events"
asp-action="Index"
```

Also make sure the generated MVC Tag Helpers remain enabled through `_ViewImports.cshtml`.

### The local URL uses a different port

Use the URL printed by `dotnet run` or shown by your development environment. Do not assume a fixed port number.

### You expect the browser to receive C#

The browser receives rendered HTML. The C# controller and Razor processing execute on the server.

---

## 13. What You Built

You started with no CEMS application and finished with a working ASP.NET Core MVC vertical slice.

Your application now has:

- the `CampusEvents` project and root namespace;
- MVC controller and Razor View services;
- the conventional route `{controller=Home}/{action=Index}/{id?}`;
- `EventsController.Index()` handling `/Events`;
- built-in logging supplied through dependency injection;
- a `string` passed from the controller to a Razor View;
- `Views/Events/Index.cshtml` rendering the string as HTML;
- shared navigation that links to Events;
- a Git repository and standard `.gitignore`;
- safe configuration habits established before secrets are needed.

The final temporary value is:

```csharp
var featuredEventTitle = "Library Research Night";
```

The Events page is still intentionally simple and non-persistent. It gives you a concrete request/render cycle that later project work can extend.

The main architecture you should carry forward is:

```text
Program.cs startup
    ├─ register services
    ├─ build/configure middleware
    └─ map routes

GET /Events
    ↓
routing selects EventsController.Index()
    ↓
DI has supplied the controller's dependencies
    ↓
controller supplies a string model
    ↓
View-location convention finds Index.cshtml
    ↓
Razor renders the model into HTML
    ↓
browser receives the response
```

The route style itself will evolve later when the Organizer workflows create a real need for explicit attribute routes. The request/controller/View principles learned here will remain the same.

### Final code checkpoint

Use the following snapshots to confirm your Lesson 1 code state.

#### `Program.cs`

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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

app.Run();
```

#### `Controllers/EventsController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CampusEvents.Controllers;

public class EventsController : Controller
{
    private readonly ILogger<EventsController> _logger;

    public EventsController(ILogger<EventsController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        _logger.LogInformation("Handling request for the CEMS Events page.");

        var featuredEventTitle = "Library Research Night";

        return View(model: featuredEventTitle);
    }
}
```

#### `Views/Events/Index.cshtml`

```cshtml
@model string

@{
    ViewData["Title"] = "Events";
}

<h1>Campus Events</h1>

<p>
    This is the first CEMS Events page. Event information is temporary in
    Lesson 1 and is not loaded from a database.
</p>

<section>
    <h2>Temporary Event Preview</h2>
    <p><strong>@Model</strong></p>
    <p>Student Center</p>
</section>
```

#### `Views/Shared/_Layout.cshtml` — Events navigation addition

```cshtml
<li class="nav-item">
    <a class="nav-link text-dark"
       asp-controller="Events"
       asp-action="Index">Events</a>
</li>
```

At this point, you should be able to explain the full path from a browser request for `/Events` to the HTML displayed on the screen. That request/render cycle is the foundation you will build on as CEMS gains real data and additional behavior.
