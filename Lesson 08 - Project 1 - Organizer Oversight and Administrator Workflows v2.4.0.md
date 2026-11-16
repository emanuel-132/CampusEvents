# **Project: Campus Events**
## **Lesson 8 — Organizer Oversight and Administrator Workflows**

Revision: 2026.11.09.1

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Feature Preview](#feature-preview)
- [Design and Coding](#design-and-coding)
	- [Inspect the inherited solution](#inspect-the-inherited-solution)
	- [Add the Organizer Registration projection](#add-the-organizer-registration-projection)
	- [Query registrations through the authorized Event](#query-registrations-through-the-authorized-event)
	- [Render the read-only participant list and management link](#render-the-read-only-participant-list-and-management-link)
	- [Create Administrator Event oversight and cancellation](#create-administrator-event-oversight-and-cancellation)
	- [Manage Organizer membership for existing users](#manage-organizer-membership-for-existing-users)
	- [Define Category input contracts and persistence integrity](#define-category-input-contracts-and-persistence-integrity)
	- [Create, inspect, and apply the Category migration](#create-inspect-and-apply-the-category-migration)
	- [Implement guarded Category commands](#implement-guarded-category-commands)
		- [List and create Categories](#list-and-create-categories)
		- [Edit Categories and exclude the current record](#edit-categories-and-exclude-the-current-record)
		- [Confirm and guard Category deletion](#confirm-and-guard-category-deletion)
	- [Integrate navigation and project-owned styling](#integrate-navigation-and-project-owned-styling)
- [Verification and Validation](#verification-and-validation)
	- [Automated](#automated)
		- [Prepare the inherited HTTP test host and ownership checks](#prepare-the-inherited-http-test-host-and-ownership-checks)
		- [Verify Administrator routes and cancellation](#verify-administrator-routes-and-cancellation)
		- [Verify Organizer membership and Category integrity](#verify-organizer-membership-and-category-integrity)
		- [Execute and interpret the regression suite](#execute-and-interpret-the-regression-suite)
	- [Manual](#manual)
	- [SRS Traceability Matrix Update with Evidence](#srs-traceability-matrix-update-with-evidence)
- [Discussion](#discussion)
	- [Role authorization, ownership, and effective authority](#role-authorization-ownership-and-effective-authority)
		- [Independent roles, combined privileges, and the dual-role exception](#independent-roles-combined-privileges-and-the-dual-role-exception)
	- [Query scope, information exposure, and derived registration state](#query-scope-information-exposure-and-derived-registration-state)
	- [Administrative commands and domain invariants](#administrative-commands-and-domain-invariants)
	- [Least privilege, role assignment, and session state](#least-privilege-role-assignment-and-session-state)
	- [Layered validation and database uniqueness](#layered-validation-and-database-uniqueness)
	- [Referential integrity and controlled deletion](#referential-integrity-and-controlled-deletion)
	- [Verification scope and strength of evidence](#verification-scope-and-strength-of-evidence)
- [Exercises](#exercises)
	- [Exercise: add current Registration counts to Administrator Event oversight](#exercise-add-current-registration-counts-to-administrator-event-oversight)
	- [Exercise: test the dual-role exception and ordinary ownership denial](#exercise-test-the-dual-role-exception-and-ordinary-ownership-denial)
	- [Exercise: test Category input boundaries and normalization](#exercise-test-category-input-boundaries-and-normalization)
	- [Exercise: prove deletion does not damage referenced Events](#exercise-prove-deletion-does-not-damage-referenced-events)
	- [Exercise: compare evidence boundaries](#exercise-compare-evidence-boundaries)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [Authorization architectures and privilege lifecycle](#authorization-architectures-and-privilege-lifecycle)
		- [Roles, claims, policies, and resources](#roles-claims-policies-and-resources)
		- [Permission changes and existing sessions](#permission-changes-and-existing-sessions)
		- [Ownership after privilege removal](#ownership-after-privilege-removal)
	- [String equality, collations, and portable uniqueness strategies](#string-equality-collations-and-portable-uniqueness-strategies)
		- [Equality is a database contract](#equality-is-a-database-contract)
		- [Comparing uniqueness designs](#comparing-uniqueness-designs)
		- [Concurrent requests and the final guarantee](#concurrent-requests-and-the-final-guarantee)
	- [Administrative architecture, scaling, and accountability](#administrative-architecture-scaling-and-accountability)
		- [Organizing administrative modules](#organizing-administrative-modules)
		- [Actor attribution and audits](#actor-attribution-and-audits)
		- [Operational consistency across workflows](#operational-consistency-across-workflows)

# Review of Last Lesson

Lesson 7 established current Registration as a persisted user–Event association, with derived availability, server-authoritative registration eligibility, SQL Server final-seat coordination, and a compatible capacity-edit boundary. Its automated tests use isolated migrated SQL Server databases, a fixed clock, and an authenticated HTTP host; the application retains public search, Category filtering, selectable 10/25/50 pagination, Event lifecycle operations, and registration-aware Draft deletion.

You will use that foundation to let an Organizer inspect current participants for an owned Event and to build Administrator workflows for cross-owner oversight, Organizer-role changes, and Category management. The work applies trusted identity, persisted relationships, and guarded command patterns at three different actor scopes.

# Learning Objectives

By the end of this lesson, you should be able to:

1. **Analyze** a protected operation using authenticated identity, role membership, resource ownership, operation type, and current state.
2. **Design** actor-scoped data retrieval that prevents unauthorized information disclosure while presenting derived relational facts.
3. **Evaluate** privileged state changes against domain invariants without conflating authority with ownership.
4. **Apply** least-privilege principles to assignment and removal of permissions, including the effects of existing sessions.
5. **Design** layered validation that combines helpful application feedback with database-enforced uniqueness and referential integrity.
6. **Distinguish** read-only retrieval, confirmation, and state-changing requests, including their security and failure boundaries.
7. **Evaluate** verification methods by the system boundaries, behavior, and outcomes each examines.
8. **Assess** incremental application changes for accessibility, maintainability, regression safety, and requirements traceability.

# SRS Requirements and Use Cases

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| `UC‑006`, `FR‑REG‑009‑010` | Organizer sees current participants, registration count, and remaining capacity for an owned Event; dual Organizer/Administrator access has the stated Administrator exception. | Add read‑only Registration oversight, owner‑scoped lookup with the explicit dual‑role exception, and relational projections. |
| `UC‑007`, `FR‑ADM‑001‑002` | Administrative functions are restricted to authenticated Administrators; Administrator can grant/remove Organizer access for an existing account. | Add protected Administrator controllers and narrow existing‑user role commands. |
| `UC‑008`, `FR‑ADM‑003‑004`, `FR‑EVT‑010` | Administrator sees all organizers and statuses through bounded 10/25/50 pages and can cancel any eligible Published/not-started Event without deleting it. | Add paginated all-Event oversight (stable start/ID ordering, counted range, 400 invalid size, retained page state) and guarded cross-owner cancellation that preserves Event and owner. |
| `FR‑ADM‑005‑007`, `BR‑CAT‑001‑002` | Administrator can create/rename valid unique Categories and delete only Categories unused by any Event; name uniqueness ignores case. | Add Category forms and guarded mutations, explicit column collation/unique index, and Restrict‑FK‑backed deletion. |
| `BR‑AUT‑001‑004`, `FR‑ACC‑006` | Protected operations require appropriate authenticated privileges; Organizer ownership is enforced except for authorized Administrator scope. | Apply server‑side role and resource checks to each request, retain inherited protection, and exercise role/ownership boundaries through authenticated HTTP requests. |
| `AC‑005‑007`, `AC‑009‑010`, SRS §10.1 | Valid persisted state, access control, controlled errors, and evidence from tests of critical rules. | Inspect persistence after refused operations and extend inherited relational and HTTP verification. |

**Lesson-level acceptance contract:** An Organizer can read current Registration information for an owned Event, and an Organizer who also has Administrator access can use the SRS's cross-owner exception. An Administrator can page through all Events (10/25/50), cancel only eligible Published Events without changing ownership, modify only Organizer membership of existing accounts, and create/rename/delete Categories within their database rules. Unauthorized requests and invalid operations leave protected state unchanged. The protected behavior includes authorized success, controlled refusal, and unchanged persisted records after invalid requests.

# Feature Preview

Start from two Organizers who own different future Events, an ordinary Participant with one current Registration, and an Administrator account. Organizer A selects **Registrations** on an owned Event and sees participant names, email contacts, count, and capacity remaining. Requesting Organizer B's Event ID as Organizer A produces the established not-found response; an account holding both Organizer and Administrator roles may inspect that Event under the SRS exception.

In a separate **Administration** area, an Administrator pages through Draft, Published, and Cancelled Events across organizers using 10/25/50 page sizes. Cancelling a future Published Event changes its status to Cancelled while leaving `OrganizerId` intact. The same interface grants/removes only `Organizer` on an existing account; after the affected user signs out and in, the protected Organizer navigation and endpoints reflect that change. Category forms reject case-variant duplicates and refuse deletion while Events reference the Category.

> **Screenshot placeholder — Organizer Registration oversight**
>
> **Illustration needed:** Owned future Event with two current Registrations, Capacity 4, names and contact data in a semantic list, and remaining capacity 2.
>
> **Expected visible result:** Event title, current registrations 2, remaining capacity 2; no mutation controls on this read-only page.
>
> **Application state:** Development dataset with two persisted Registration rows and signed-in Organizer owner.
>
> ![Expected Organizer Registration oversight showing two participants and two remaining places](images/lesson-08/organizer-registration-oversight.png)

# Design and Coding

## Inspect the inherited solution

Inspect your existing ***Campus Events*** solution before making changes.

Make sure that you staged and commited the previous lesson:

```cmd
git show -s HEAD
```

**Expected output:** should contain "Lesson 07: Implemented and Verified".

Otherwise, build, test, run, stage, and commit:

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
git add .
git commit -m "Lesson 07: Implemented and Verified"
```

Then, create a new branch:

```cmd
git switch -c lesson-08
```

## Add the Organizer Registration projection

Create a response-specific model that contains only the Event fields and participant information required for this screen. Registration counts remain derived from current `Registrations`; they are not duplicated as mutable columns on `Events`.

**File: `src/CampusEvents/ViewModels/OrganizerRegistrationViewModel.cs`**

```csharp
namespace CampusEvents.ViewModels;

public class OrganizerRegistrationViewModel
{
	public int EventId { get; set; }

	public string EventTitle { get; set; } = string.Empty;

	public int Capacity { get; set; }

	public int CurrentRegistrationCount { get; set; }

	public int RemainingCapacity { get; set; }

	public IReadOnlyList<OrganizerRegistrationParticipantViewModel> Participants { get; set; } = [];
}

public class OrganizerRegistrationParticipantViewModel
{
	public string DisplayName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public DateTime RegisteredAt { get; set; }
}
```

This projection records a snapshot of the persisted current associations. Since viewing the page is read-only, it does not change registrations or remaining capacity.

## Query registrations through the authorized Event

Keep this action in the inherited `EventsController` beside other Organizer-owned operations. Resolve the authenticated user on the server, establish the Event authorization boundary, then read the related Registration rows. The SRS permits the cross-owner exception **only when the Organizer also has Administrator access**; the role requirement remains Organizer for this workflow.

**File: `src/CampusEvents/Controllers/EventsController.cs`** — add this action and retain the current controller constructor and injected dependencies:

```csharp
[HttpGet]
[Authorize(Roles = RoleNames.Organizer)]
public async Task<IActionResult> Registrations(int id)
{
	var organizer = await _userManager.GetUserAsync(User);

	if (organizer is null)
	{
		return Challenge();
	}

	var hasAdministratorAccess = User.IsInRole(RoleNames.Administrator);
	var eventInfo = await _context.Events
		.AsNoTracking()
		.Where(eventItem => eventItem.Id == id &&
			(eventItem.OrganizerId == organizer.Id || hasAdministratorAccess))
		.Select(eventItem => new { eventItem.Id, eventItem.Title, eventItem.Capacity })
		.SingleOrDefaultAsync();

	if (eventInfo is null)
	{
		return EventNotFound();
	}

	var participants = await _context.Registrations
		.AsNoTracking()
		.Where(registration => registration.EventId == eventInfo.Id)
		.OrderBy(registration => registration.CreatedAt)
		.ThenBy(registration => registration.Id)
		.Select(registration => new OrganizerRegistrationParticipantViewModel
		{
			DisplayName = registration.User.DisplayName,
			Email = registration.User.Email ?? string.Empty,
			RegisteredAt = registration.CreatedAt
		})
		.ToListAsync();

	var count = participants.Count;
	return View(new OrganizerRegistrationViewModel
	{
		EventId = eventInfo.Id,
		EventTitle = eventInfo.Title,
		Capacity = eventInfo.Capacity,
		CurrentRegistrationCount = count,
		RemainingCapacity = Math.Max(0, eventInfo.Capacity - count),
		Participants = participants
	});
}
```

Keep the existing `using CampusEvents.ViewModels;` if already present, or add it. The Event lookup is the sensitive boundary: an ordinary Organizer's forged Event ID does not produce an authorized record, so the participant projection is never executed for that foreign Event. A dual-role user has the explicit authorized exception. A missing or forbidden Event uses the same `EventNotFound()` behavior adopted in earlier lessons.

**Code checkpoint:** Build the solution and inspect the authorized Event predicate and Registration projection. Confirm that the action produces a response model containing current participants, current count, and remaining capacity derived from persisted associations.

## Render the read-only participant list and management link

**File: `src/CampusEvents/Views/Events/Registrations.cshtml`**

```cshtml
@model CampusEvents.ViewModels.OrganizerRegistrationViewModel
@using CampusEvents.Security

@{
	ViewData["Title"] = "Event Registrations";
}

<h1>Event Registrations</h1>

<h2>@Model.EventTitle</h2>

<dl class="event-meta">
	<div>
		<dt>Capacity</dt>
		<dd>@Model.Capacity</dd>
	</div>
	<div>
		<dt>Current registrations</dt>
		<dd>@Model.CurrentRegistrationCount</dd>
	</div>
	<div>
		<dt>Remaining capacity</dt>
		<dd>@Model.RemainingCapacity</dd>
	</div>
</dl>

@if (Model.Participants.Count == 0)
{
	<p role="status">
		No participants are currently registered
		for this Event.
	</p>
}
else
{
	<section aria-labelledby="participant-list-heading">
		<h2 id="participant-list-heading">
			Current Participants
		</h2>

		<div class="management-list">
			@foreach (var participant in Model.Participants)
			{
				<article class="management-card">
					<h3>@participant.DisplayName</h3>

					<p>
						<strong>Email:</strong>
						@participant.Email
					</p>

					<p>
						<strong>Registered:</strong>
						<time datetime="@participant.RegisteredAt.ToString("yyyy-MM-ddTHH:mm:ss")">
							@participant.RegisteredAt.ToString("f")
						</time>
					</p>
				</article>
			}
		</div>
	</section>
}

<p>
	<a asp-controller="Events" asp-action="Manage">
		Back to My Events
	</a>
</p>

@if (User.IsInRole(RoleNames.Organizer) && User.IsInRole(RoleNames.Administrator))
{
	<p><a asp-controller="Admin" asp-action="Events">Back to Administrator Event Oversight</a></p>
}
```

Add the following link **once, before the status-specific `if` branches**, inside the existing owned-Event `.event-actions` block in `src/CampusEvents/Views/Events/Manage.cshtml`. This ensures Draft, Published, and Cancelled owned Events expose the same read-only destination. Keep the inherited Draft/Published/Cancelled controls and their `@inject TimeProvider Clock` logic intact; do not replace the action block with the old `DateTime.Now` variant.

```cshtml
<a asp-controller="Events" asp-action="Registrations" asp-route-id="@eventItem.Id"
	class="button-secondary">View Registrations</a>
```

**Expected observable checkpoint:** The Organizer's Manage list now reaches a read-only Registration page for owned Draft, Published, and Cancelled Events. An ordinary Organizer requesting another owner's Event ID receives the established not-found response, while a dual-role Organizer/Administrator sees the permitted participant list and can return to Administrator oversight. Compare current count and remaining capacity with the persisted Registration rows and the Event Details snapshot.

## Create Administrator Event oversight and cancellation

Unlike the public Published/upcoming catalog, Administrator oversight includes **every** Event status and owner. Reuse Lesson 3's count, normalized page, stable `(StartDateTime, Id)` order, and database `Skip/Take` pattern, without copying its public eligibility filter. This list accepts 10/25/50 Events per page (default 10), returns a shared HTTP 400 page for an explicitly invalid page size, and preserves page/size through the cancellation POST and redirect. These navigation values never confer cancellation authority.

**File: `src/CampusEvents/ViewModels/AdminEventsViewModel.cs`**

```csharp
using CampusEvents.Models;

namespace CampusEvents.ViewModels;

// Page-level retrieval state; no role or ownership claims are taken from the browser.
public class AdminEventsViewModel
{
	public IReadOnlyList<Event> Events { get; set; } = Array.Empty<Event>();
	public int TotalCount { get; set; }
	public int CurrentPage { get; set; } = 1;
	public int PageSize { get; set; } = 10;
	public int TotalPages { get; set; }
	public IReadOnlyList<int> PageSizeOptions { get; set; } = new[] { 10, 25, 50 };
}
```


The dashboard and Event list have application-wide scope, unlike public discovery or Organizer management. Use a conventional controller with class-level Administrator authorization. Its cancellation command checks persisted state and the injected clock immediately before saving; it neither assigns a different owner nor deletes the Event.

**File: `src/CampusEvents/Controllers/AdminController.cs`**

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminController : Controller
{
	private const int DefaultPageSize = 10;
	private static readonly int[] AllowedPageSizes = { 10, 25, 50 };
	private readonly ApplicationDbContext _context;
	private readonly TimeProvider _timeProvider;

	public AdminController(ApplicationDbContext context, TimeProvider timeProvider)
	{
		_context = context;
		_timeProvider = timeProvider;
	}

	[HttpGet]
	public IActionResult Index() => View();

	[HttpGet]
	public async Task<IActionResult> Events(int? page, int? pageSize)
	{
		// Match the public catalog's input contract, but do NOT apply its public eligibility predicate.
		if (Request.Query.ContainsKey(nameof(pageSize)) &&
			(!pageSize.HasValue || !AllowedPageSizes.Contains(pageSize.Value)))
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			return View("BadRequest", new ErrorPageViewModel
			{
				Title = "Invalid Page Size",
				Message = "Choose a page size of 10, 25, or 50.",
				ReturnUrl = Url.Action(nameof(Events), "Admin"),
				ReturnLabel = "Return to Administrator Event Oversight"
			});
		}

		var selectedPageSize = pageSize.GetValueOrDefault(DefaultPageSize);
		var query = _context.Events.AsNoTracking(); // All owners, all statuses.
		var totalCount = await query.CountAsync();
		var totalPages = (int)Math.Ceiling(totalCount / (double)selectedPageSize);
		var currentPage = Math.Max(1, page.GetValueOrDefault(1));
		if (totalPages == 0) currentPage = 1;
		else if (currentPage > totalPages) currentPage = totalPages;

		var offset = ((long)currentPage - 1) * selectedPageSize;
		if (offset > int.MaxValue)
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			return View("BadRequest", new ErrorPageViewModel
			{
				Title = "Page Range Unavailable",
				Message = "Choose an earlier page in Administrator Event Oversight.",
				ReturnUrl = Url.Action(nameof(Events), "Admin"),
				ReturnLabel = "Return to Administrator Event Oversight"
			});
		}

		var events = await query
			.Include(item => item.Category)
			.Include(item => item.Organizer)
			.OrderBy(item => item.StartDateTime)
			.ThenBy(item => item.Id)
			.Skip((int)offset)
			.Take(selectedPageSize)
			.ToListAsync();

		return View(new AdminEventsViewModel
		{
			Events = events,
			TotalCount = totalCount,
			CurrentPage = currentPage,
			PageSize = selectedPageSize,
			TotalPages = totalPages,
			PageSizeOptions = AllowedPageSizes
		});
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> CancelEvent(int id, int? page, int? pageSize)
	{
		// Navigation state is untrusted and never influences cancellation authorization.
		var returnPage = Math.Max(1, page.GetValueOrDefault(1));
		var returnPageSize = pageSize.HasValue && AllowedPageSizes.Contains(pageSize.Value)
			? pageSize.Value : DefaultPageSize;
		var returnRoute = new { page = returnPage, pageSize = returnPageSize };

		var eventItem = await _context.Events.FirstOrDefaultAsync(item => item.Id == id);
		if (eventItem is null) return NotFound();

		if (eventItem.Status != EventStatus.Published)
		{
			TempData["ErrorMessage"] = "Only a Published Event can be cancelled.";
			return RedirectToAction(nameof(Events), returnRoute);
		}

		if (eventItem.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
		{
			TempData["ErrorMessage"] = "A Published Event can be cancelled only before it starts.";
			return RedirectToAction(nameof(Events), returnRoute);
		}

		eventItem.Status = EventStatus.Cancelled;
		await _context.SaveChangesAsync();
		TempData["StatusMessage"] = $"{eventItem.Title} was cancelled by an Administrator.";
		return RedirectToAction(nameof(Events), returnRoute);
	}
}
```

The accepted Event lifecycle makes Published/not-started the exact cancellation condition. The POST's `page` and `pageSize` values are untrusted navigation state; the action normalizes them for redirect, while the GET clamps them against the new total count. Event-row transaction coordination with concurrent Edit is deliberately deferred to Lesson 9. The check inside the POST is authoritative even if a button was visible during an earlier GET. Because the admin action does not change capacity or create a Registration, it preserves Lesson 7's existing count and final-seat protections; the same persisted status prevents later registration. The action keeps the eligibility rule beside the privileged command; the same Published/not-started condition governs Organizer cancellation.

**File: `src/CampusEvents/Views/Admin/Index.cshtml`**

```cshtml
@{
	ViewData["Title"] = "Administration";
}

<h1>Administration</h1>

<p>
	These product-management operations require Administrator access on the server.
</p>

<div class="management-list">
	<article class="management-card">
		<h2>Event Oversight</h2>
		<p>
			Review all Events and cancel eligible Published Events.
		</p>
		<a asp-controller="Admin" asp-action="Events" class="button-primary">
			Manage Events
		</a>
	</article>

</div>
```

**File: `src/CampusEvents/Views/Admin/Events.cshtml`** — render all Event statuses and ownership without applying the public-catalog filter. Use the injected clock for the visible cancellation condition.

```cshtml
@model CampusEvents.ViewModels.AdminEventsViewModel
@using CampusEvents.Models
@using CampusEvents.Security
@inject TimeProvider Clock

@{
	ViewData["Title"] = "Administrator Event Oversight";
	var now = Clock.GetLocalNow().DateTime;
}

<h1>Administrator Event Oversight</h1>

@if (TempData["StatusMessage"] is string statusMessage)
{
	<p class="status-message" role="status">@statusMessage</p>
}
@if (TempData["ErrorMessage"] is string errorMessage)
{
	<p class="error-message" role="alert">@errorMessage</p>
}

<form asp-controller="Admin" asp-action="Events" method="get" class="event-filter-form"
	aria-label="Administrator Event list page size">
	<div class="event-filter-field">
		<label for="adminPageSize" class="event-filter-label">Events per page</label>
		<select id="adminPageSize" name="pageSize" class="event-filter-control">
			@foreach (var option in Model.PageSizeOptions)
			{
				if (option == Model.PageSize)
				{
					<option value="@option" selected>@option</option>
				}
				else
				{
					<option value="@option">@option</option>
				}
			}
		</select>
	</div>
	<div class="event-filter-actions">
		<button type="submit" class="event-filter-submit">Apply page size</button>
	</div>
</form>

@if (Model.Events.Count == 0)
{
	<p role="status">No Events have been created.</p>
}
else
{
	var firstResult = (Model.CurrentPage - 1) * Model.PageSize + 1;
	var lastResult = firstResult + Model.Events.Count - 1;
	<p class="event-result-count">Showing @firstResult–@lastResult of @Model.TotalCount Events.</p>
	<div class="management-list">
		@foreach (var eventItem in Model.Events)
		{
			<article class="management-card">
				<h2>@eventItem.Title</h2>
				<dl class="event-meta">
					<div><dt>Organizer</dt><dd>@eventItem.Organizer.DisplayName</dd></div>
					<div><dt>Category</dt><dd>@eventItem.Category.Name</dd></div>
					<div><dt>Status</dt><dd>@eventItem.Status</dd></div>
					<div><dt>Location</dt><dd>@eventItem.Location</dd></div>
					<div><dt>Starts</dt><dd>
						<time datetime="@eventItem.StartDateTime.ToString("yyyy-MM-ddTHH:mm:ss")">@eventItem.StartDateTime.ToString("f")</time>
					</dd></div>
				</dl>

				@if (User.IsInRole(RoleNames.Organizer))
				{
					<a asp-controller="Events" asp-action="Registrations" asp-route-id="@eventItem.Id"
						class="button-secondary">View Registrations</a>
				}

				@if (eventItem.Status == EventStatus.Published && eventItem.StartDateTime > now)
				{
					<form asp-controller="Admin" asp-action="CancelEvent" asp-route-id="@eventItem.Id"
						asp-route-page="@Model.CurrentPage" asp-route-pageSize="@Model.PageSize"
						method="post" asp-antiforgery="true" class="event-action-form">
						<button type="submit" class="button-danger">Cancel Event</button>
					</form>
				}
			</article>
		}
	</div>

	<p class="event-page-status">Page @Model.CurrentPage of @Model.TotalPages</p>
	@if (Model.TotalPages > 1)
	{
		<nav class="event-pagination" aria-label="Administrator Event pages">
			<div class="event-pagination-actions">
				@if (Model.CurrentPage > 1)
				{
					<a asp-controller="Admin" asp-action="Events" asp-route-page="1"
						asp-route-pageSize="@Model.PageSize" class="event-page-link">First</a>
					<a asp-controller="Admin" asp-action="Events" asp-route-page="@(Model.CurrentPage - 1)"
						asp-route-pageSize="@Model.PageSize" rel="prev" class="event-page-link">Previous</a>
				}
				@if (Model.CurrentPage < Model.TotalPages)
				{
					<a asp-controller="Admin" asp-action="Events" asp-route-page="@(Model.CurrentPage + 1)"
						asp-route-pageSize="@Model.PageSize" rel="next" class="event-page-link">Next</a>
					<a asp-controller="Admin" asp-action="Events" asp-route-page="@Model.TotalPages"
						asp-route-pageSize="@Model.PageSize" class="event-page-link">Last</a>
				}
			</div>
		</nav>
	}
}

<p><a asp-controller="Admin" asp-action="Index">Back to Administration</a></p>
```

The list covers Draft, Published, and Cancelled Events across organizers **over its full set of pages**, never materializing all Event rows in one request. The visible range and First/Previous/Next/Last links reflect the normalized page. The class-level Administrator requirement already gates the page; the `User.IsInRole(RoleNames.Organizer)` link check adds the second role for the Registrations route. Administrator-only accounts can supervise and cancel but do **not** see links to a route they cannot access. This link rule is not a replacement for endpoint authorization. A dual-role Organizer/Administrator can open any Event's protected Registration list directly from a card. Each eligible Published card supplies an anti-forgery-protected POST; after cancellation, the redirected list displays the Cancelled status and the original Organizer. The POST reloads the Event and checks eligibility again against the current clock.

> **Screenshot placeholder — Administrator Event oversight**
>
> **Illustration needed:** Administrator Event list displaying different owners and statuses, including a cancellation control for a future Published Event.
>
> **Expected visible result:** Cross-owner rows and a controlled status message after cancellation; unchanged owner display.
>
> **Application state:** Administrator session with Draft, Published, and Cancelled Events in the Development database.
>
> ![Expected Administrator Event oversight with cross-owner Event records and eligible cancellation](images/lesson-08/admin-event-oversight.png)


**Expected observable checkpoint:** An Administrator can inspect all statuses across owners through 10/25/50 bounded pages; invalid explicit page sizes return 400, malformed page numbers normalize to the first, and above-range numbers to the last available page. A future Published Event can be cancelled and remains stored as Cancelled with its original `OrganizerId`. An ordinary signed-in user receives Access Denied for `/Admin` and `/Admin/Events`; anonymous requests redirect to Login. A missing Event returns 404; Draft, Cancelled, and already-started Events produce controlled refusal without changing state.

## Manage Organizer membership for existing users

Create a narrow read model for existing accounts. The browser sends only a user ID and the chosen Organizer-membership command; server-side Identity services find that user and modify only the `Organizer` role. The `Administrator` role, account credentials, DisplayName, and Event ownership are outside this form's mutation contract.

**File: `src/CampusEvents/ViewModels/AdminUserViewModel.cs`**

```csharp
namespace CampusEvents.ViewModels;

public class AdminUserViewModel
{
	public string Id { get; set; } = string.Empty;

	public string DisplayName { get; set; } = string.Empty;

	public string Email { get; set; } = string.Empty;

	public bool IsOrganizer { get; set; }
}
```

**File: `src/CampusEvents/Controllers/AdminUsersController.cs`**

```csharp
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminUsersController : Controller
{
	private readonly UserManager<ApplicationUser> _userManager;

	public AdminUsersController(UserManager<ApplicationUser> userManager)
	{
		_userManager = userManager;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var users = await _userManager.Users.AsNoTracking().OrderBy(user => user.Email).ToListAsync();

		var organizers = await _userManager.GetUsersInRoleAsync(RoleNames.Organizer);

		var organizerIds = organizers.Select(user => user.Id).ToHashSet();

		var model = users.Select(user => new AdminUserViewModel
				{
					Id = user.Id, DisplayName = user.DisplayName, Email = user.Email ?? string.Empty,
					IsOrganizer = organizerIds.Contains(user.Id)
				}).ToList();

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> GrantOrganizer(string id)
	{
		var user = await _userManager.FindByIdAsync(id);

		if (user is null)
		{
			return NotFound();
		}

		if (await _userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			TempData["ErrorMessage"] = "This user already has Organizer access.";

			return RedirectToAction(nameof(Index));
		}

		var result = await _userManager.AddToRoleAsync(user, RoleNames.Organizer);

		if (!result.Succeeded)
		{
			TempData["ErrorMessage"] = "Organizer access could not be granted.";

			return RedirectToAction(nameof(Index));
		}

		TempData["StatusMessage"] = $"Organizer access was granted to {user.DisplayName}.";

		return RedirectToAction(nameof(Index));
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> RemoveOrganizer(string id)
	{
		var user = await _userManager.FindByIdAsync(id);

		if (user is null)
		{
			return NotFound();
		}

		if (!await _userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			TempData["ErrorMessage"] = "This user does not currently have Organizer access.";

			return RedirectToAction(nameof(Index));
		}

		var result = await _userManager.RemoveFromRoleAsync(user, RoleNames.Organizer);

		if (!result.Succeeded)
		{
			TempData["ErrorMessage"] = "Organizer access could not be removed.";

			return RedirectToAction(nameof(Index));
		}

		TempData["StatusMessage"] = $"Organizer access was removed from {user.DisplayName}.";

		return RedirectToAction(nameof(Index));
	}
}
```

The two POST operations use anti-forgery, check the existing user's current Organizer membership, and display Identity service errors as controlled feedback. A role change updates persisted membership but does not automatically rewrite every authentication cookie already issued to the affected user. Sign-out/sign-in supplies a deterministic new session for local verification. Existing `Event.OrganizerId` values remain unchanged when Organizer membership is removed.

**File: `src/CampusEvents/Views/AdminUsers/Index.cshtml`**

```cshtml
@model IReadOnlyList<CampusEvents.ViewModels.AdminUserViewModel>

@{
	ViewData["Title"] = "Organizer Access";
}

<h1>Organizer Access</h1>

@if (TempData["StatusMessage"] is string statusMessage)
{
	<p class="status-message" role="status">
		<strong>Success:</strong> @statusMessage
	</p>
}

@if (TempData["ErrorMessage"] is string errorMessage)
{
	<p class="error-message" role="alert">
		@errorMessage
	</p>
}

<div class="management-list">
	@foreach (var user in Model)
	{
		<article class="management-card">
			<h2>@user.DisplayName</h2>

			<p>
				<strong>Email:</strong>
				@user.Email
			</p>

			<p>
				<strong>Organizer access:</strong>
				@(user.IsOrganizer ? "Yes" : "No")
			</p>

			@if (user.IsOrganizer)
			{
				<form asp-controller="AdminUsers" asp-action="RemoveOrganizer" asp-route-id="@user.Id" method="post"
					asp-antiforgery="true">
					<button type="submit" class="button-danger">
						Remove Organizer Access
					</button>
				</form>
			}
			else
			{
				<form asp-controller="AdminUsers" asp-action="GrantOrganizer" asp-route-id="@user.Id" method="post"
					asp-antiforgery="true">
					<button type="submit" class="button-primary">
						Grant Organizer Access
					</button>
				</form>
			}
		</article>
	}
</div>

<p>
	<a asp-controller="Admin" asp-action="Index">
		Back to Administration
	</a>
</p>
```

Add the Organizer Access card inside the existing `.management-list` in `Views/Admin/Index.cshtml`, after the Event Oversight card:

```cshtml
<article class="management-card">
	<h2>Organizer Access</h2>
	<p>Grant or remove Organizer access for existing users.</p>
	<a asp-controller="AdminUsers" asp-action="Index" class="button-primary">Manage Organizer Access</a>
</article>
```

**Expected observable checkpoint:** The dashboard now links to Organizer Access. After granting Organizer to a previously ordinary Participant and signing that account in again, the account can access the Organizer management surface. Removing Organizer and starting a new session denies that access while preserving existing Event ownership. A request with an unknown ID returns 404; duplicate grants/removals produce controlled messages.

## Define Category input contracts and persistence integrity

A Category's name is editable, but its identity and its Event relationships are server-controlled. Use a constrained input model for Create/Edit and a read-only confirmation model for Delete. The SRS requires case-insensitive uniqueness, so give the SQL Server `Name` column explicit case-insensitive comparison semantics and a unique index; the 100-character maximum keeps input and storage bounded.

**File: `src/CampusEvents/ViewModels/CategoryInputModel.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace CampusEvents.ViewModels;

public class CategoryInputModel
{
	[Required]
	[StringLength(100)]
	[Display(Name = "Category name")]
	public string Name { get; set; } = string.Empty;
}
```

**File: `src/CampusEvents/ViewModels/CategoryDeleteViewModel.cs`**

```csharp
namespace CampusEvents.ViewModels;

public class CategoryDeleteViewModel
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public bool IsInUse { get; set; }
}
```

**File: `src/CampusEvents/Data/ApplicationDbContext.cs`** — add the constant inside the class and evolve the existing `Category` mapping. Preserve every inherited Event, Registration, Identity, and database constraint mapping.

```csharp
public const string CategoryNameCollation = "SQL_Latin1_General_CP1_CI_AS";
```

```csharp
modelBuilder.Entity<Category>(entity =>
{
	entity.Property(category => category.Name).IsRequired().HasMaxLength(100).UseCollation(CategoryNameCollation);

	entity.HasIndex(category => category.Name).IsUnique();
});
```

The existing Event→Category relationship still uses `DeleteBehavior.Restrict`. The unique index, rather than a pre-check, is the persisted guarantee that `Music` and `music` cannot coexist. The Category `Name` column receives an explicit collation while other database columns retain their existing settings.

## Create, inspect, and apply the Category migration

Inspect the existing `dbo.Categories` data in SQL Server before changing its constraints. Check case-insensitive duplicates after trimming surrounding whitespace and names longer than the new 100-character storage limit. `DATALENGTH(Name) / 2` measures UTF-16 code units for an `nvarchar` column, including trailing spaces that `LEN` excludes. Use an instructor-approved SQL client on the intended Development database:

```sql
SELECT LTRIM(RTRIM([Name])) COLLATE SQL_Latin1_General_CP1_CI_AS AS CandidateName, COUNT(*) AS MatchingRows
FROM dbo.Categories
GROUP BY LTRIM(RTRIM([Name])) COLLATE SQL_Latin1_General_CP1_CI_AS
HAVING COUNT(*) > 1;

SELECT Id, [Name], DATALENGTH([Name]) / 2 AS NameCodeUnits
FROM dbo.Categories
WHERE DATALENGTH([Name]) / 2 > 100;
```

If either query returns rows, inspect the affected Event relationships, choose deliberate data corrections, and rerun the checks. Preserve referenced Categories and Event associations. From the solution root, create the migration using the established project/startup paths.

```bat
dotnet ef migrations add EnforceCategoryNameUniqueness -p src\CampusEvents -s src\CampusEvents -- --environment Development
```

Review the generated `Up()`, `Down()`, and model snapshot. Expected schema effects are `Categories.Name` as non-null `nvarchar(100)` with `SQL_Latin1_General_CP1_CI_AS` and a unique `IX_Categories_Name` index. The Event→Category restrictive foreign key, Registration relationships, Event constraints, and Identity tables remain intact. If the generated migration disagrees with this expectation, correct the model or migration deliberately before applying it.

```bat
dotnet ef database update -p src\CampusEvents -s src\CampusEvents -- --environment Development
dotnet build .\CampusEvents.slnx
```

**Expected observable checkpoint:** Both preflight queries return no conflicting rows; the reviewed migration applies, the database defines the case-insensitive unique index and 100-character column, and existing Events retain their Category references. Check the generated schema and the application build before proceeding to Category forms.

## Implement guarded Category commands

The inspected migration supplies the Category-name database constraint. Begin with the Administrator-only listing and Create workflow. An application pre-check offers immediate validation feedback, while the unique index resolves conflicting writes that occur between checking and saving.

### List and create Categories

**File: `src/CampusEvents/Controllers/AdminCategoriesController.cs`** — create this controller with the listing, Create actions, and the uniqueness helpers:

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using CampusEvents.Security;
using CampusEvents.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Controllers;

[Authorize(Roles = RoleNames.Administrator)]
public class AdminCategoriesController : Controller
{
	private readonly ApplicationDbContext _context;

	public AdminCategoriesController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		var categories = await _context.Categories.AsNoTracking().OrderBy(category => category.Name).ToListAsync();

		return View(categories);
	}

	[HttpGet]
	public IActionResult Create()
	{
		return View(new CategoryInputModel());
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Create(CategoryInputModel model)
	{
		await ValidateUniqueNameAsync(model);

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		var category = new Category { Name = model.Name.Trim() };

		_context.Categories.Add(category);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException exception) when (IsUniqueCategoryNameViolation(exception))
		{
			_context.Entry(category).State = EntityState.Detached;

			ModelState.AddModelError(nameof(CategoryInputModel.Name), "A Category with this name already exists.");

			return View(model);
		}

		TempData["StatusMessage"] = $"{category.Name} was created.";

		return RedirectToAction(nameof(Index));
	}

	private async Task ValidateUniqueNameAsync(CategoryInputModel model, int? excludedCategoryId = null)
	{
		if (string.IsNullOrWhiteSpace(model.Name))
		{
			return;
		}

		var candidateName = model.Name.Trim();

		var duplicateExists = await _context.Categories.AsNoTracking()
			.AnyAsync(category => (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value) &&
				EF.Functions.Collate(category.Name, ApplicationDbContext.CategoryNameCollation) == candidateName);

		if (duplicateExists)
		{
			ModelState.AddModelError(nameof(CategoryInputModel.Name), "A Category with this name already exists.");
		}
	}

	private static bool IsUniqueCategoryNameViolation(DbUpdateException exception)
	{
		return exception.InnerException is SqlException error && (error.Number == 2601 || error.Number == 2627) &&
			error.Message.Contains("IX_Categories_Name", StringComparison.OrdinalIgnoreCase);
	}

}
```

The GET actions retrieve or present data; Create POST validates the allowlisted name and returns attempted input when invalid. On a recognized unique-index conflict, the controller detaches the failed new entity and renders a field-level error.

Create the two corresponding Razor views now so that the first workflow can be observed in the browser.

**File: `src/CampusEvents/Views/AdminCategories/Index.cshtml`**

```cshtml
@model IReadOnlyList<CampusEvents.Models.Category>

@{
	ViewData["Title"] = "Category Management";
}

<h1>Category Management</h1>

@if (TempData["StatusMessage"] is string statusMessage)
{
	<p class="status-message" role="status">
		<strong>Success:</strong> @statusMessage
	</p>
}

@if (TempData["ErrorMessage"] is string errorMessage)
{
	<p class="error-message" role="alert">
		@errorMessage
	</p>
}

<p>
	<a asp-controller="AdminCategories" asp-action="Create" class="button-primary">
		Create Category
	</a>
</p>

@if (Model.Count == 0)
{
	<p>No Categories have been created.</p>
}
else
{
	<div class="management-list">
		@foreach (var category in Model)
		{
			<article class="management-card">
				<h2>@category.Name</h2>
			</article>
		}
	</div>
}

<p>
	<a asp-controller="Admin" asp-action="Index">
		Back to Administration
	</a>
</p>
```

**File: `src/CampusEvents/Views/AdminCategories/Create.cshtml`**

```cshtml
@model CampusEvents.ViewModels.CategoryInputModel

@{
	ViewData["Title"] = "Create Category";
}

<h1>Create Category</h1>

<form asp-controller="AdminCategories" asp-action="Create" method="post" asp-antiforgery="true">
	<div asp-validation-summary="ModelOnly" class="error-message" role="alert"></div>

	<div class="form-field">
		<label asp-for="Name"></label>
		<input asp-for="Name" />
		<span asp-validation-for="Name" class="field-validation-error"></span>
	</div>

	<div class="event-actions">
		<button type="submit" class="button-primary">
			Create Category
		</button>

		<a asp-controller="AdminCategories" asp-action="Index" class="button-secondary">
			Cancel
		</a>
	</div>
</form>
```

**Expected observable checkpoint:** An Administrator can list and create Categories after the migration has been applied. The list has a clear empty state; a case-variant duplicate returns the Create form with the attempted value and a field error.

### Edit Categories and exclude the current record

Add the following actions **inside the same `AdminCategoriesController` class**. The duplicate query excludes the current Category ID, allowing an unchanged name while rejecting a collision with another Category.

```csharp
	[HttpGet]
	public async Task<IActionResult> Edit(int id)
	{
		var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(category => category.Id == id);

		if (category is null)
		{
			return NotFound();
		}

		return View(new CategoryInputModel { Name = category.Name });
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Edit(int id, CategoryInputModel model)
	{
		var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id);

		if (category is null)
		{
			return NotFound();
		}

		await ValidateUniqueNameAsync(model, excludedCategoryId: category.Id);

		if (!ModelState.IsValid)
		{
			return View(model);
		}

		category.Name = model.Name.Trim();

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException exception) when (IsUniqueCategoryNameViolation(exception))
		{
			_context.Entry(category).State = EntityState.Detached;

			ModelState.AddModelError(nameof(CategoryInputModel.Name), "A Category with this name already exists.");

			return View(model);
		}

		TempData["StatusMessage"] = $"{category.Name} was updated.";

		return RedirectToAction(nameof(Index));
	}
```

A valid Edit changes the name while preserving the Category ID and all Event references. A recognized unique-index conflict returns the attempted name and leaves the persisted row unchanged.

**File: `src/CampusEvents/Views/AdminCategories/Edit.cshtml`**

```cshtml
@model CampusEvents.ViewModels.CategoryInputModel

@{
	ViewData["Title"] = "Edit Category";
}

<h1>Edit Category</h1>

<form method="post" asp-antiforgery="true">
	<div asp-validation-summary="ModelOnly" class="error-message" role="alert"></div>

	<div class="form-field">
		<label asp-for="Name"></label>
		<input asp-for="Name" />
		<span asp-validation-for="Name" class="field-validation-error"></span>
	</div>

	<div class="event-actions">
		<button type="submit" class="button-primary">
			Save Category
		</button>

		<a asp-controller="AdminCategories" asp-action="Index" class="button-secondary">
			Cancel
		</a>
	</div>
</form>
```

After creating the Edit action and view, update each Category card inside the existing `foreach` in `Views/AdminCategories/Index.cshtml`:

```cshtml
<article class="management-card">
	<h2>@category.Name</h2>
	<div class="event-actions">
		<a asp-controller="AdminCategories" asp-action="Edit" asp-route-id="@category.Id" class="button-secondary">Edit</a>
	</div>
</article>
```

**Expected observable checkpoint:** The Category list now links to Edit. An unchanged Category name can be saved, a case-variant conflict returns an error, and the Category's ID and referenced Events remain stable.

### Confirm and guard Category deletion

Add these actions and the named foreign-key conflict helper **inside the `AdminCategoriesController` class**. The confirmation GET checks whether the Category is referenced. The POST checks again before removal and handles a last-moment foreign-key conflict from SQL Server.

```csharp
	[HttpGet]
	public async Task<IActionResult> Delete(int id)
	{
		var model = await _context.Categories.AsNoTracking().Where(category => category.Id == id)
			.Select(category => new CategoryDeleteViewModel
			{
				Id = category.Id,
				Name = category.Name,
				IsInUse = category.Events.Any()
			}).SingleOrDefaultAsync();

		if (model is null)
		{
			return NotFound();
		}

		return View(model);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> DeleteConfirmed(int id)
	{
		var category = await _context.Categories.FirstOrDefaultAsync(category => category.Id == id);

		if (category is null)
		{
			return NotFound();
		}

		var isInUse = await _context.Events.AnyAsync(eventItem => eventItem.CategoryId == category.Id);

		if (isInUse)
		{
			TempData["ErrorMessage"] = "This Category is in use and cannot be deleted.";

			return RedirectToAction(nameof(Index));
		}

		_context.Categories.Remove(category);

		try
		{
			await _context.SaveChangesAsync();
		}
		catch (DbUpdateException exception) when (IsEventCategoryReferenceViolation(exception))
		{
			_context.Entry(category).State = EntityState.Detached;
			TempData["ErrorMessage"] = "This Category is in use and cannot be deleted.";

			return RedirectToAction(nameof(Index));
		}

		TempData["StatusMessage"] = $"{category.Name} was deleted.";

		return RedirectToAction(nameof(Index));
	}

	private static bool IsEventCategoryReferenceViolation(DbUpdateException exception)
	{
		return exception.InnerException is SqlException error && error.Number == 547 &&
			error.Message.Contains("FK_Events_Categories_CategoryId", StringComparison.OrdinalIgnoreCase);
	}
```

SQL Server error numbers identify a constraint category; the named index and foreign-key checks identify the expected CEMS conflict. Inspect the migration to confirm `IX_Categories_Name` and `FK_Events_Categories_CategoryId` are the actual generated names; use the verified names in the helper methods. An Edit conflict detaches the unsuccessful tracked update before returning the attempted form values.

**File: `src/CampusEvents/Views/AdminCategories/Delete.cshtml`**

```cshtml
@model CampusEvents.ViewModels.CategoryDeleteViewModel

@{
	ViewData["Title"] = "Delete Category";
}

<h1>Delete Category</h1>

<div class="confirmation-panel">
	<h2>@Model.Name</h2>

	@if (Model.IsInUse)
	{
		<p class="error-message" role="alert">
			This Category is currently used by one
			or more Events and cannot be deleted.
		</p>
	}
	else
	{
		<p class="destructive-note">
			This action permanently deletes the
			unused Category.
		</p>

		<form asp-controller="AdminCategories" asp-action="DeleteConfirmed" asp-route-id="@Model.Id" method="post"
			asp-antiforgery="true">
			<button type="submit" class="button-danger">
				Permanently Delete Category
			</button>
		</form>
	}

	<p>
		<a asp-controller="AdminCategories" asp-action="Index">
			Keep Category
		</a>
	</p>
</div>
```

Add the Delete link alongside Edit inside the Category card's `.event-actions` in `Views/AdminCategories/Index.cshtml`:

```cshtml
<a asp-controller="AdminCategories" asp-action="Delete" asp-route-id="@category.Id" class="button-danger">Delete</a>
```

Add the Category Management card inside the existing `.management-list` in `Views/Admin/Index.cshtml`, after the Organizer Access card:

```cshtml
<article class="management-card">
	<h2>Categories</h2>
	<p>Create, rename, and delete unused Event Categories.</p>
	<a asp-controller="AdminCategories" asp-action="Index" class="button-primary">Manage Categories</a>
</article>
```

**Expected observable checkpoint:** The dashboard now links to Categories. The Category list links to both Edit and Delete. Delete GET shows the Category and its in-use state; a referenced Category remains stored, and an unused Category is deleted after a valid confirmation POST. Inspect the Event and Category rows after each request.

## Integrate navigation and project-owned styling

Keep the existing public, Participant, Organizer, role-label, and POST-only Logout navigation. Add the Administrator destination in the authenticated navigation of `src/CampusEvents/Views/Shared/_Layout.cshtml`, where `RoleNames` is already imported or available through the layout's existing imports.

```cshtml
@if (User.IsInRole(RoleNames.Administrator))
{
	<li>
		<a class="site-nav-link" asp-controller="Admin" asp-action="Index">
			Administration
		</a>
	</li>
}
```

Reuse the existing `.button-primary`, `.button-secondary`, `.button-danger`, `.event-meta`, `.status-message`, `.error-message`, and form rules. Append these small selectors to `src/CampusEvents/wwwroot/css/site.css` where not already defined:

```css
.management-list {
	display: grid;
	gap: 1rem;
}

.management-card {
	padding: 1rem;
	border: 1px solid currentColor;
	border-radius: 0.35rem;
}

.management-card h2, .management-card h3 {
	margin-top: 0;
}
```

**Expected observable checkpoint:** Navigation reflects the signed-in role, but direct requests still require the server's role and resource authorization. Confirm keyboard access, explicit labels, visible focus, clear feedback, and a readable small-screen layout. Maintain the established MVC/Razor and project-owned CSS design.

**File: `src/CampusEvents/Views/Home/Privacy.cshtml`** — the inherited placeholder describes registrations as not yet implemented. Replace it with a factual classroom-application notice that describes accounts, current Registration data, Organizer-owned participant visibility, and the **dual-role** foreign-owner registration-read exception. Do **not** assert that Administrator-only users can read participant names/emails or invent an institutional retention policy.

```cshtml
@{
	ViewData["Title"] = "Privacy and Application Data";
}

<h1>Privacy and Application Data</h1>

<p>CEMS stores account information, Event details, and current participant registrations for the course application's workflows.</p>
<p>Signed-in participants can review their own current registrations. An Organizer can view participant names and email addresses for owned Events. An Organizer who also has the Administrator role may view registrations for Events owned by other Organizers. An Administrator-only account cannot access that registration-detail endpoint.</p>
<p>Administrative role management and Event oversight are separate operations. This description documents the classroom application's current access behavior; it is not an institutional retention or legal privacy policy.</p>
```

Confirm this copy matches the endpoint authorization model. Review its appropriateness again during Lesson 9 stabilization.

# Verification and Validation

Use the established disposable migrated SQL Server fixture for automated tests and an intentionally seeded Development dataset for manual browser checks. Verification assesses whether the implemented contract behaves as specified; validation assesses whether real users can understand and complete the workflows. Preserve earlier tests for registration reliability, public retrieval, Identity, and Organizer lifecycle behavior.

## Automated

**Test-case inventory — new cases and retained regression boundaries:**

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| New: `OrganizerOversight_OwnerForeignAndDualRoleScopesAreEnforced` (one method) | Own Event, foreign ID, Participant role denial, dual-role exception | HTTP 200/404/Access Denied/200; no foreign participant data in rejected response |
| New: `AdminRoutes_RequireAdministrator` (four cases) | `/Admin`, `/Admin/Events`, `/AdminUsers`, `/AdminCategories` | Authenticated Participant receives Access Denied on each route |
| New: `AdminCancel_FuturePublishedOtherOwnersEvent_RetainsOrganizerId` (one method) | Cross-owner eligible Published cancellation | Redirect; Cancelled persisted; original `OrganizerId` retained |
| New: `AdminCancel_MissingAntiforgeryTokenIsRejected` (one method) | Administrator POST without request-verification token | HTTP 400; Event state and owner unchanged |
| New: `AdminCancel_RejectsInvalidStateAndTime` (four cases) | Draft, Cancelled, exact start, past-start Published | Redirect with error; Event status and owner unchanged |
| New: `OrganizerRole_GrantAndRemoveChangeMembershipOnly` (one method) | Existing user role grant/removal and new sessions | Identity membership and protected route reflect new role; Event ownership unchanged |
| New: `Categories_CaseVariantNamesConflict` (one method) | Create duplicate by case, Edit conflict, own-name Edit | Invalid attempts return form; existing names preserved; unchanged edit redirects |
| New: `CategoryDatabase_UniqueIndexRejectsCaseVariants` (one method) | Direct relational duplicate insert | SQL Server unique-index exception on case-variant name |
| New: `CategoryDelete_ReferencedRefusedAndUnusedSucceeds` (one method) | In-use and unused Category via protected POST | In-use feedback displayed; referenced Category/Event persist; unused Category deleted |
| Retained: public/Identity/Organizer/Registration HTTP tests | Search, 10/25/50 paging, Details, lifecycle, registration and capacity | Accepted HTTP and persisted-state invariants |
| New: `RegistrationLinks_ReflectIndependentRolesAndOwnerScope` | Rendered Organizer Manage links across statuses; Administrator-only suppression; dual-role link plus direct request | Link and destination agree for each principal; no foreign data on refusal |
| New: `AdminEvents_EmptyAndFirstPageBoundaries_AreExplicit` (0/1/10/11) | Empty, one, exact-full, and one-over-full pages | No phantom controls on empty/exact-full pages; Next only for 11 |
| New: `AdminEvents_PaginatesAcrossAllOwnersAndStatuses_WithStablePageState` | 57 all-status/mixed-owner Events, 10/25/50, invalid page size, normalized last page, selected-page cancellation | Bounded HTML cards, expected ranges, 400 errors, preserved `page` and `pageSize` |
| Retained: SQL Server last-seat relational tests | Registration concurrency and Event capacity | Accepted relational assertions and separately controlled lock observation |

**New Lesson 8 integration regression checkpoint (add to the existing HTTP suite):** Run the two new tests in `tests/CampusEvents.Tests/OrganizerEventHttpTests.cs` supplied with the updated Lesson 8 codebase. The first signs in an Organizer, Administrator-only account, and dual-role account in separate cookie clients; it checks rendered owned links for Draft/Published/Cancelled Events, the absence of Administrator-only links, and direct-route permissions. The boundary theory checks 0, 1, 10, and 11 Events. The larger paging test constructs 57 mixed-owner/status Events with identical starts, checks deterministic ID ordering via page content, validates the selected page size and 400 handling, and verifies page state after a cancellation POST. Use the test project's established `AccountsAsync`, `GrantAdministratorAsync`, `SignInAsync`, `FormTokenAsync`, and `IsolatedSqlServerDatabase` helpers; do not substitute a mocked controller call for these rendered-HTML tests.

**Manual checks:** With the Administrator-only session, `/Admin/Events` lists Events but has **no** `View Registrations` link. With a dual-role session, every eligible displayed Event card has that link and its destination is allowed; with an Organizer-only session, owned cards in `/Events/Manage` have it, even when Draft or Cancelled. Browse page sizes 10/25/50, request an invalid explicit size, navigate 57 Events to the last page, and cancel a future Published Event from page 2. Verify the redirect retains `page=2&pageSize=25`, and no participant names/emails appear in unauthorized responses.

### Prepare the inherited HTTP test host and ownership checks

**File: `tests/CampusEvents.Tests/OrganizerEventHttpTests.cs`** — add the following methods **inside the inherited `OrganizerEventHttpTests` class**. They reuse the `FixedClock()`, `CreateHost(...)`, `AccountsAsync(...)`, `EventAsync(...)`, `Browser(...)`, and `SignInAsync(...)` helpers already available there. The test host exercises real routing, authentication, authorization, EF Core, Identity, and Razor rendering against an isolated, migrated SQL Server database.

The existing `AccountsAsync` helper creates two Organizers (`OwnerEmail`, `OtherEmail`) and an ordinary Participant (`ParticipantEmail`). Add a small helper that grants the existing owner account Administrator access; it leaves the existing Organizer membership untouched:

```csharp
private static async Task GrantAdministratorAsync(WebApplicationFactory<Program> host, string email)
{
	using var scope = host.Services.CreateScope();
	var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
	var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
	var adminRole = await roles.FindByNameAsync(RoleNames.Administrator);

	if (adminRole is null)
	{
		var created = await roles.CreateAsync(new IdentityRole(RoleNames.Administrator));
		Assert.True(created.Succeeded);
	}

	var user = await users.FindByEmailAsync(email);
	Assert.NotNull(user);
	var assigned = await users.AddToRoleAsync(user!, RoleNames.Administrator);
	Assert.True(assigned.Succeeded);
}
```

Add `using CampusEvents.Security;` and the existing Identity and DI imports if they are not already present; the inherited class uses `Microsoft.AspNetCore.Mvc.Testing` for `WebApplicationFactory<Program>`. This test establishes the owner, foreign-owner, dual-role exception, and ordinary Participant role boundary through real HTTP requests, not direct controller calls.

```csharp
[Fact]
public async Task OrganizerOversight_OwnerForeignAndDualRoleScopesAreEnforced()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	var categoryId = await CategoryAsync(database);
	var ownedId = await EventAsync(database, categoryId, ownerId, "Owned Oversight", EventStatus.Published);
	var foreignId = await EventAsync(database, categoryId, otherId, "Foreign Oversight", EventStatus.Published);

	await using (var arrange = database.CreateContext())
	{
		var participantId = await arrange.Users.Where(user => user.Email == ParticipantEmail)
			.Select(user => user.Id).SingleAsync();
		arrange.Registrations.Add(new Registration
		{
			UserId = participantId, EventId = foreignId, CreatedAt = FixedClock().GetLocalNow().DateTime
		});
		await arrange.SaveChangesAsync();
	}

	using var owner = Browser(host);
	await SignInAsync(owner, OwnerEmail);
	var owned = await owner.GetAsync($"/Events/Registrations/{ownedId}");
	Assert.Equal(HttpStatusCode.OK, owned.StatusCode);

	var foreign = await owner.GetAsync($"/Events/Registrations/{foreignId}");
	Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
	Assert.DoesNotContain(ParticipantEmail, await foreign.Content.ReadAsStringAsync());

	using var participant = Browser(host);
	await SignInAsync(participant, ParticipantEmail);
	var denied = await participant.GetAsync($"/Events/Registrations/{foreignId}");
	Assert.Equal("/Account/AccessDenied", Location(denied).AbsolutePath);

	await GrantAdministratorAsync(host, OwnerEmail);
	using var dualRole = Browser(host);
	await SignInAsync(dualRole, OwnerEmail);
	var allowed = await dualRole.GetAsync($"/Events/Registrations/{foreignId}");
	Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
	var html = await allowed.Content.ReadAsStringAsync();
	Assert.Contains(ParticipantEmail, html);
	Assert.Contains("Current registrations", html);
}
```

The scenario checks owned and foreign Event visibility, a Participant's denied Organizer route, and the explicit dual-role exception using separate authenticated sessions. Read the returned HTML for the actual participant data and count.

### Verify Administrator routes and cancellation

Begin with role authorization. An authenticated Participant must receive Access Denied at each Administrator entry point:

```csharp
[Theory]
[InlineData("/Admin")]
[InlineData("/Admin/Events")]
[InlineData("/AdminUsers")]
[InlineData("/AdminCategories")]
public async Task AdminRoutes_RequireAdministrator(string path)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	await AccountsAsync(host);
	using var participant = Browser(host);
	await SignInAsync(participant, ParticipantEmail);

	var response = await participant.GetAsync(path);
	Assert.Equal("/Account/AccessDenied", Location(response).AbsolutePath);
}
```

Next, submit an eligible cross-owner cancellation using an authenticated Administrator and a valid anti-forgery token. Read the saved Event from a fresh context to check its status and retained owner:

```csharp
[Fact]
public async Task AdminCancel_FuturePublishedOtherOwnersEvent_RetainsOrganizerId()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, otherId) = await AccountsAsync(host);
	await GrantAdministratorAsync(host, OwnerEmail);
	var categoryId = await CategoryAsync(database);
	var eventId = await EventAsync(database, categoryId, otherId, "Other Owner Published", EventStatus.Published);

	using var admin = Browser(host);
	await SignInAsync(admin, OwnerEmail);
	var token = await FormTokenAsync(admin, "/Admin/Events");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
	var post = await SendFormAsync(admin, $"/Admin/CancelEvent/{eventId}", fields);
	Assert.Equal("/Admin/Events", Location(post).AbsolutePath);

	await using var verify = database.CreateContext();
	var eventItem = await verify.Events.AsNoTracking().SingleAsync(item => item.Id == eventId);
	Assert.Equal(EventStatus.Cancelled, eventItem.Status);
	Assert.Equal(otherId, eventItem.OrganizerId);
}
```

Check request protection separately by submitting the same command without the request-verification token. The pipeline rejects the request with HTTP 400, and the Event remains Published:

```csharp
[Fact]
public async Task AdminCancel_MissingAntiforgeryTokenIsRejected()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (_, otherId) = await AccountsAsync(host);
	await GrantAdministratorAsync(host, OwnerEmail);
	var categoryId = await CategoryAsync(database);
	var eventId = await EventAsync(database, categoryId, otherId, "Token Protected", EventStatus.Published);

	using var admin = Browser(host);
	await SignInAsync(admin, OwnerEmail);
	var fields = new Dictionary<string, string>();
	var refused = await SendFormAsync(admin, $"/Admin/CancelEvent/{eventId}", fields);
	Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

	var saved = await ReadEventAsync(database, eventId);
	Assert.Equal(EventStatus.Published, saved.Status);
	Assert.Equal(otherId, saved.OrganizerId);
}
```

Finally, exercise lifecycle and time-based refusals with valid request-verification tokens. The inherited `EventAsync` helper places Events after the fixed clock. Each case creates an additional eligible Published Event to supply a cancellation form token, then submits the ineligible Event ID. The four `InlineData` values produce four separately discoverable cases:

```csharp
[Theory]
[InlineData("draft")]
[InlineData("cancelled")]
[InlineData("at-start")]
[InlineData("past-start")]
public async Task AdminCancel_RejectsInvalidStateAndTime(string kind)
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	var clock = FixedClock();
	using var host = CreateHost(database, clock);
	var (_, otherId) = await AccountsAsync(host);
	await GrantAdministratorAsync(host, OwnerEmail);
	var categoryId = await CategoryAsync(database);
	var status = kind == "draft" ? EventStatus.Draft : kind == "cancelled" ? EventStatus.Cancelled : EventStatus.Published;
	var id = await EventAsync(database, categoryId, otherId, "Ineligible Admin Cancellation", status);
	var sourceId = await EventAsync(database, categoryId, otherId, "Eligible Token Source", EventStatus.Published);
	if (kind is "at-start" or "past-start")
	{
		var start = clock.GetLocalNow().DateTime;
		await SetStartAsync(database, id, kind == "at-start" ? start : start.AddMinutes(-1));
	}

	using var admin = Browser(host);
	await SignInAsync(admin, OwnerEmail);
	var token = await FormTokenAsync(admin, "/Admin/Events");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
	var response = await SendFormAsync(admin, $"/Admin/CancelEvent/{id}", fields);
	Assert.Equal("/Admin/Events", Location(response).AbsolutePath);

	var saved = await ReadEventAsync(database, id);
	Assert.Equal(status, saved.Status);
	Assert.Equal(otherId, saved.OrganizerId);
	Assert.Equal(EventStatus.Published, (await ReadEventAsync(database, sourceId)).Status);
	var followUp = await admin.GetAsync("/Admin/Events");
	var expectedRefusal = kind is "draft" or "cancelled" ? "Only a Published Event can be cancelled."
		: "A Published Event can be cancelled only before it starts.";
	var feedback = await followUp.Content.ReadAsStringAsync();
	Assert.Contains("role=\"alert\"", feedback);
	Assert.Contains(expectedRefusal, feedback);
}
```

### Verify Organizer membership and Category integrity

The role-management test grants an existing Participant Organizer access, then arranges an Event owned by the promoted account and verifies its access in a fresh authenticated session. After removal, another fresh session loses Organizer access while that Event's owner key, the user's account identity, and Administrator membership remain unchanged.

```csharp
[Fact]
public async Task OrganizerRole_GrantAndRemoveChangeMembershipOnly()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	await AccountsAsync(host);
	await GrantAdministratorAsync(host, OwnerEmail);
	var categoryId = await CategoryAsync(database);

	string participantId;
	string originalDisplayName;
	using (var scope = host.Services.CreateScope())
	{
		var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
		var target = await users.FindByEmailAsync(ParticipantEmail);
		Assert.NotNull(target);
		participantId = target!.Id;
		originalDisplayName = target.DisplayName;
		Assert.False(await users.IsInRoleAsync(target, RoleNames.Administrator));
	}

	using var admin = Browser(host);
	await SignInAsync(admin, OwnerEmail);
	var token = await FormTokenAsync(admin, "/AdminUsers");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
	var grant = await SendFormAsync(admin, $"/AdminUsers/GrantOrganizer/{participantId}", fields);
	Assert.Equal("/AdminUsers", Location(grant).AbsolutePath);
	var ownedId = await EventAsync(database, categoryId, participantId, "Stable Ownership", EventStatus.Draft);

	using var promoted = Browser(host);
	await SignInAsync(promoted, ParticipantEmail);
	Assert.Equal(HttpStatusCode.OK, (await promoted.GetAsync("/Events/Manage")).StatusCode);

	var remove = await SendFormAsync(admin, $"/AdminUsers/RemoveOrganizer/{participantId}", fields);
	Assert.Equal("/AdminUsers", Location(remove).AbsolutePath);
	using var demoted = Browser(host);
	await SignInAsync(demoted, ParticipantEmail);
	Assert.Equal("/Account/AccessDenied", Location(await demoted.GetAsync("/Events/Manage")).AbsolutePath);

	using var verification = host.Services.CreateScope();
	var manager = verification.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
	var participant = await manager.FindByIdAsync(participantId);
	Assert.NotNull(participant);
	Assert.False(await manager.IsInRoleAsync(participant!, RoleNames.Organizer));
	Assert.False(await manager.IsInRoleAsync(participant!, RoleNames.Administrator));
	Assert.Equal(originalDisplayName, participant!.DisplayName);
	Assert.Equal(ParticipantEmail, participant.Email);
	var actingAdmin = await manager.FindByEmailAsync(OwnerEmail);
	Assert.NotNull(actingAdmin);
	Assert.True(await manager.IsInRoleAsync(actingAdmin!, RoleNames.Administrator));
	Assert.Equal(participantId, (await ReadEventAsync(database, ownedId)).OrganizerId);
}
```

The Category tests use the same migrated disposable database and a signed-in Administrator. The unique-name case uses the real HTTP forms, while the delete case inspects the persisted foreign-key relationship after each request.

```csharp
[Fact]
public async Task Categories_CaseVariantNamesConflict()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	await AccountsAsync(host);
	await GrantAdministratorAsync(host, OwnerEmail);
	var academicId = await CategoryAsync(database);

	using var admin = Browser(host);
	await SignInAsync(admin, OwnerEmail);
	var token = await FormTokenAsync(admin, "/AdminCategories/Create");
	var fields = new Dictionary<string, string>
	{
		["Name"] = "Music",
		["__RequestVerificationToken"] = token
	};
	var created = await SendFormAsync(admin, "/AdminCategories/Create", fields);
	Assert.Equal("/AdminCategories", Location(created).AbsolutePath);

	fields["Name"] = "music";
	var duplicate = await SendFormAsync(admin, "/AdminCategories/Create", fields);
	Assert.Equal(HttpStatusCode.OK, duplicate.StatusCode);
	Assert.Contains("already exists", await duplicate.Content.ReadAsStringAsync());

	fields["Name"] = "MUSIC";
	var editConflict = await SendFormAsync(admin, $"/AdminCategories/Edit/{academicId}", fields);
	Assert.Equal(HttpStatusCode.OK, editConflict.StatusCode);
	Assert.Contains("already exists", await editConflict.Content.ReadAsStringAsync());

	fields["Name"] = "Academic";
	var sameName = await SendFormAsync(admin, $"/AdminCategories/Edit/{academicId}", fields);
	Assert.Equal("/AdminCategories", Location(sameName).AbsolutePath);
	await using var verify = database.CreateContext();
	var names = await verify.Categories.AsNoTracking().Select(category => category.Name).ToListAsync();
	Assert.Equal(1, names.Count(name => name.Equals("Music", StringComparison.OrdinalIgnoreCase)));
	Assert.Contains("Academic", names);
}

[Fact]
public async Task CategoryDatabase_UniqueIndexRejectsCaseVariants()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	await using var db = database.CreateContext();
	db.Categories.Add(new Category { Name = "Design" });
	await db.SaveChangesAsync();

	db.Categories.Add(new Category { Name = "design" });
	var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
	var sqlError = Assert.IsType<Microsoft.Data.SqlClient.SqlException>(exception.InnerException);
	Assert.True(sqlError.Number is 2601 or 2627);
	Assert.Contains("IX_Categories_Name", sqlError.Message, StringComparison.OrdinalIgnoreCase);

	await using var verification = database.CreateContext();
	var saved = await verification.Categories.AsNoTracking().Where(category => category.Name == "Design")
		.Select(category => category.Name).ToListAsync();
	Assert.Single(saved);
	Assert.Equal("Design", saved[0]);
}

[Fact]
public async Task CategoryDelete_ReferencedRefusedAndUnusedSucceeds()
{
	await using var database = await IsolatedSqlServerDatabase.CreateAsync();
	using var host = CreateHost(database, FixedClock());
	var (ownerId, _) = await AccountsAsync(host);
	await GrantAdministratorAsync(host, OwnerEmail);
	var referencedId = await CategoryAsync(database);
	var eventId = await EventAsync(database, referencedId, ownerId, "Keep Event", EventStatus.Draft);
	int unusedId;
	await using (var arrange = database.CreateContext())
	{
		var unused = new Category { Name = "Unused Seminar" };
		arrange.Categories.Add(unused);
		await arrange.SaveChangesAsync();
		unusedId = unused.Id;
	}

	using var admin = Browser(host);
	await SignInAsync(admin, OwnerEmail);
	var token = await FormTokenAsync(admin, "/AdminCategories/Create");
	var fields = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
	var refused = await SendFormAsync(admin, $"/AdminCategories/DeleteConfirmed/{referencedId}", fields);
	Assert.Equal("/AdminCategories", Location(refused).AbsolutePath);
	var feedbackResponse = await admin.GetAsync("/AdminCategories");
	Assert.Equal(HttpStatusCode.OK, feedbackResponse.StatusCode);
	var feedbackHtml = await feedbackResponse.Content.ReadAsStringAsync();
	Assert.Contains("This Category is in use and cannot be deleted.", feedbackHtml);

	await using (var verify = database.CreateContext())
	{
		Assert.True(await verify.Categories.AnyAsync(category => category.Id == referencedId));
		Assert.True(await verify.Events.AnyAsync(item => item.Id == eventId && item.CategoryId == referencedId));
	}

	var deleted = await SendFormAsync(admin, $"/AdminCategories/DeleteConfirmed/{unusedId}", fields);
	Assert.Equal("/AdminCategories", Location(deleted).AbsolutePath);
	await using var final = database.CreateContext();
	Assert.False(await final.Categories.AnyAsync(category => category.Id == unusedId));
}
```

Run the seven new `[Fact]` methods and eight parameterized `[Theory]` cases against isolated, migrated SQL Server databases, followed by the inherited regression suite. Inspect the runner's discovered cases and record the execution results.

### Execute and interpret the regression suite

Use the inherited Windows LocalDB endpoint or the configured disposable SQL Server test endpoint `CEMS_TEST_SQLSERVER`. The test fixture must connect to an authorized server and clean up only its own GUID-named database. Follow the inherited environment/identity setup and fixed UTC−04 testing policy.

```bat
dotnet restore .\CampusEvents.slnx
dotnet build .\CampusEvents.slnx --no-restore
dotnet test .\tests\CampusEvents.Tests\CampusEvents.Tests.csproj --no-build --logger "console;verbosity=normal"
```

Record the runner's discovered and executed cases, skips, failures, execution environment, and database endpoint. Diagnose failures by the first incorrect boundary: authentication, anti-forgery, resource lookup, controlled feedback, migration, or persisted rows.

## Manual

Use distinct existing accounts for Organizer A, Organizer B, Administrator, ordinary Participant, and a dual-role Organizer/Administrator. Prepare at least one future Published Event with current Registrations, one Draft, one Cancelled, one already-started Published Event, an unused Category, and an Event-referenced Category. A controlled signed-in browser session and a fresh database query are complementary evidence sources.

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Owned registration view | Organizer A opens Registrations for an owned Event with two rows and Capacity 4. | Names/emails visible to A; current count 2, remaining 2; no write occurs. Observe the page values and persisted row count. |
| Forged ownership | Organizer A requests B's Event ID directly. | Established not-found response; no participant names/emails. URL/response and row check. |
| SRS dual-role exception | Account with Organizer+Administrator accesses another owner's Registration URL. | Allowed view for foreign Event; ordinary Organizer remains denied. |
| Role-protected routes | Anonymous/Participant/Organizer directly visit `/Admin`, `/Admin/Events`, `/AdminUsers`, `/AdminCategories`. | Login for anonymous; Access Denied for authenticated non-admin; no admin data. |
| Cross-owner cancellation | Administrator POSTs with token for future Published Event owned by B. | Status Cancelled; `OrganizerId` unchanged; original Event remains. Before/after row. |
| Ineligible cancellation | Administrator submits Draft, Cancelled, exact-start, and past Published IDs. | Controlled refusal; state and owner unchanged. Fixed-clock evidence where appropriate. |
| Anti-forgery rejection | Authenticated Administrator submits Cancel Event POST without a verification token. | HTTP 400; persisted Event status and `OrganizerId` remain unchanged. |
| Organizer grant and removal | Administrator grants then removes Organizer for an existing Participant; target signs out/in after each. | New session changes protected Organizer access; target identity and existing Event owner keys persist. |
| Category uniqueness | Create `Technology`; attempt `technology`; create `Arts`, rename first to `ARTS`; repeat unchanged-name Edit. | Case-variant conflicts rejected with field message; unchanged-name Edit accepted. SQL index retained. |
| Category deletion | Try Delete GET and direct POST on referenced Category; then delete a genuinely unused one. | GET never deletes; referenced Category and Events remain; unused Category deleted after valid POST. |
| Inherited regressions | Revisit catalog filters/10-25-50 pagination, Cancelled Details, Participant Register/Mine/Cancel, Organizer Edit/Publish/Cancel/Delete. | Earlier behavioral contracts retained with useful feedback and correct persisted state. |
| Accessibility spot check | Keyboard through list links, forms, feedback, and confirmation on narrow viewport. | Focus is visible, controls labeled, status/error text readable and usable without a UI library. |

Record the actual browser URLs, actor roles, SQL Server record IDs, observed outcomes, and diagnosed failures. Enter `Not executed` for any scenario that could not run.

## SRS Traceability Matrix Update with Evidence

Update the working course traceability record after performing the relevant checks. Start with `IMPLEMENTED—UNVERIFIED` for finished code and `PARTIAL` for incomplete acceptance checks. Update each row when you observe its specified verification in your environment. Prior Lesson 7 evidence should retain its original context rather than being relabeled as new Lesson 8 execution.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| `UC-006`, `FR-REG-009`, `FR-REG-010` | Registration projection, authorized Event lookup, read-only Razor view | New owner/foreign/dual-role HTTP cases and manual count: Not executed | IMPLEMENTED—UNVERIFIED | Includes SRS exception for Organizer+Administrator. |
| `FR-ADM-001`, `BR-AUT-003`, `BR-AUT-004` | Three Administrator-protected controller classes | Non-admin/anonymous direct HTTP requests: Not executed | IMPLEMENTED—UNVERIFIED | Verify authorization with real HTTP role/anonymous requests. |
| `FR-ADM-002`, `UC-007` | Existing-user Organizer grant/remove commands | Membership and fresh-session checks: Not executed | IMPLEMENTED—UNVERIFIED | Only Organizer role changes. |
| `FR-ADM-003`, `UC-008` | All-Event Administrator query and complete Razor view | Cross-owner/status HTTP and browser observations: Not executed | IMPLEMENTED—UNVERIFIED | Public and Organizer query scopes preserved. |
| `FR-ADM-004`, `FR-EVT-010`, `UC-008` | Cross-owner cancellation POST with `TimeProvider` | Eligible/ineligible state and unchanged owner tests: Not executed | IMPLEMENTED—UNVERIFIED | Exact-start boundary retained. |
| `FR-ADM-005`, `FR-ADM-006`, `BR-CAT-001` | Category forms, controller pre-check, explicit collation and unique index migration | SQL Server migration/duplicate-name tests: Not executed | IMPLEMENTED—UNVERIFIED | Migration must be inspected/applied to target database. |
| `FR-ADM-007`, `BR-CAT-002` | Confirmation GET, guarded POST, inherited Restrict foreign key | In-use and unused deletion tests: Not executed | IMPLEMENTED—UNVERIFIED | SQL Server FK is final relational backstop. |
| `FR-ACC-006`, `AC-006`, `AC-007` | Participant and Organizer protection retained; Administrator operations added | Cross-role HTTP and ownership matrix: Not executed for Lesson 8 | PARTIAL | Prior evidence must be reconciled before final project-wide status. |
| `AC-005`, `AC-009`, `AC-010` | Database constraints, controlled refusals, new/retained tests | Migration and cumulative suite results: Not executed | PARTIAL | Record observed outcomes for critical business, integrity, and access-control cases. |

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 08: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Discussion

## Role authorization, ownership, and effective authority

Consider the single question, “May this person perform this operation on this resource now?” There are several independent facts in the answer: **identity** (who is authenticated), **role** (which broad privileges the person has), **relationship** (whether the person owns the Event), **operation** (read Registrations or cancel an Event), and **state** (whether a Published Event has already started). No single role flag settles every question.

| Actor and request | Role boundary | Resource/state boundary | Decision |
| --- | --- | --- | --- |
| Administrator-only reads any Event Registrations | Administrator without Organizer | Cannot enter Organizer-only endpoint | Deny |
| Organizer A reads own Event Registrations | Organizer | Owner A | Allow read |
| Organizer A reads Organizer B's Registrations | Organizer | Foreign owner | Not found / refuse |
| Organizer with Administrator also reads B's Registrations | Organizer and Administrator | UC-006 exception | Allow read |
| Participant reads Organizer Registrations | No Organizer role | Ownership irrelevant | Deny |
| Administrator cancels B's future Published Event | Administrator | Published and not started | Allow mutation; retain owner |
| Administrator cancels a Draft | Administrator | Invalid lifecycle transition | Refuse mutation |

This illustrates **effective authority**: the authorization decision is specific to a principal, resource, operation, and state. The server evaluates it from trusted identity and persisted data. Event ownership represents a durable domain relationship; the Organizer role represents a revocable permission. Consequently, revoking Organizer access changes a user's permitted operations while retaining Event ownership.

For example, Organizer A can open the management list and see owned Event IDs. When A types Organizer B's Event ID into the Registration URL, the protected lookup performs a fresh owner test; prior navigation offers no special authority. A dual-role Organizer/Administrator uses the specific exception in `UC-006`, while an Administrator-only account uses the dedicated administration surfaces. Write the authorization rule in terms of *operation plus resource* rather than scattering role checks through unrelated view conditions.

### Independent roles, combined privileges, and the dual-role exception

In CEMS, **a role is a set of permissions, not a rank**. `Administrator` is not a super-role that silently inherits `Organizer`. The Administrator can list and cancel eligible Events across owners and manage Categories/Organizer membership, but Administrator-only cannot use the Organizer-specific registration, Create, Edit, Publish, or Delete endpoints. A user assigned **both** roles receives both independent permission sets, subject to each operation's resource and state rules.

For the specific `UC-006` registration read, the policy is:

```text
authenticated AND Organizer AND (owns this Event OR Administrator)
```

The first part is enforced by `[Authorize(Roles = RoleNames.Organizer)]` on `EventsController.Registrations`. A valid signed-in user is then resolved through Identity. The scoped Event query checks `item.OrganizerId == currentUser.Id || User.IsInRole(RoleNames.Administrator)` **before** loading participant names/email. The `||` here creates a **resource-ownership exception**, not another way around the Organizer role requirement.

Contrast these forms:

```csharp
[Authorize(Roles = RoleNames.Organizer)] // Require Organizer.
// Subsequent Event predicate: owner OR Administrator.
```

```csharp
[Authorize(Roles = "Organizer,Administrator")] // OR between roles: wrong for UC-006.
```

Two separately applied `[Authorize]` requirements normally accumulate (AND), while the comma-delimited roles in **one** attribute mean OR. When `AdminController` already requires Administrator, `@if (User.IsInRole(RoleNames.Organizer))` in the Administrator Event view deliberately shows the Registrations link only to a **dual-role** account. UI link hiding is only discoverability; direct foreign-ID requests must still be refused on the server.

| Principal | Read owned registrations | Read foreign registrations | Organizer edit a foreign Event | Administrator cancel a future Published Event |
| --- | --- | --- | --- | --- |
| Organizer-only | Yes | No (404) | No | No |
| Administrator-only | No (role denial) | No (role denial) | No | Yes |
| Organizer + Administrator | Yes | Yes (`UC-006`) | No | Yes |
| Participant-only | No | No | No | No |

**Privacy boundary:** Registration lists contain participant names and email addresses. The role-plus-resource check enforces the application's *current-request* disclosure rule, but this alone does **not** prove independent separation of duties against someone who can assign the missing role. Lesson 9 evaluates that stronger claim. Do not expand the dual-role registration **read** exception into foreign-owner Organizer mutations.

## Query scope, information exposure, and derived registration state

Public browsing, Organizer management, and Administrator oversight legitimately use different Event queries: `Published AND upcoming`, `OrganizerId == current user` for ordinary Organizer management, and all statuses/owners for Administrator oversight. These are actor-specific information boundaries, not alternate views over an unrestricted query result. In the Registration action, the app first establishes access to the Event, then reads participants. That order makes unauthorized participant details unavailable to the response.

A list of participants contains identifying information such as names and email contacts. The SRS requires current participants; contact email is a deliberate, minimal addition for the Organizer's coordination workflow. Review the project's privacy expectations whenever exposing another field. Keep it on the protected management surface, project only the displayed fields, and render them using encoded Razor expressions. The protected query and server authorization establish the disclosure boundary; navigation then presents the authorized route. Where the business rule explicitly grants the dual-role exception, it is incorporated into that query rather than treated as an ad hoc client-side override.

Current registration count is a **derived fact** from current Registration rows; remaining capacity is computed from Event capacity minus that count. A read model should gather only the fields required by its audience: the Organizer needs participant identity and contact details, while an Administrator summary can use aggregate counts without copying those details into every card. Defining the projection at query time also prevents an accidental N+1 pattern caused by retrieving an Event list and then querying registrations separately for every row. This design derives the displayed value from current associations and keeps registration changes centralized. The page is a point-in-time snapshot: another Participant can register or cancel between two page views. A registration or capacity-change command therefore re-evaluates its own persisted inputs at mutation time. This distinction between displayed state and authoritative command decisions also appears in reservation systems, inventory checks, and ticket allocation. The relationship between such a snapshot and more elaborate reporting projections is explored in [Administrative architecture, scaling, and accountability](#administrative-architecture-scaling-and-accountability).

## Administrative commands and domain invariants

Privilege determines **who may request** an administrative cancellation. The lifecycle determines **which changes are valid**. Even a privileged actor cannot change a Draft to Cancelled through the Published-cancellation command, or cancel an Event at or after its start. A status mutation should preserve the existing Event row and its `OrganizerId`; changing that foreign key merely to satisfy an Organizer action would corrupt domain ownership.

Read-only GETs may display candidate controls. A POST performs the actual write only after server authorization, anti-forgery validation, persisted-state retrieval, and the current-time check. The POST checks role, current Event status, and time independently of the button displayed on the earlier GET. The request at the exact start boundary is refused because eligibility requires `StartDateTime > now`. The shared `TimeProvider` makes the strict start-boundary decision reproducible. The Administrator workflow and Organizer lifecycle use the same state transition, while differing in who is permitted to initiate it.

| Request stage | Responsibility | Example failure behavior |
| --- | --- | --- |
| Authenticate and authorize | Confirm the caller may invoke Administrator commands | Login or Access Denied |
| Resolve resource | Load persisted Event by ID | HTTP 404 for missing ID |
| Check state and time | Enforce Published/not-started invariant | Controlled message, no mutation |
| Save and redirect | Persist valid transition and show feedback | Cancelled persists; owner stays fixed |

## Least privilege, role assignment, and session state

The existing `AdminUsersController.GrantOrganizer` permits an Administrator to assign Organizer membership to an **existing account**, including the Administrator's own account; it does not forbid self-assignment. A fresh sign-in reflects the updated role claims. This is correct for the stated classroom role-management function but must not be mistaken for independently enforced separation of duties; Lesson 9 analyzes the resulting privilege-escalation path and production alternatives.

An administrative role-change form is a narrowly scoped security-sensitive command. Its input should identify an existing account and one narrowly allowed operation. Separate `GrantOrganizer` and `RemoveOrganizer` commands allowlist a single role operation; the server uses the account-management service to modify the role membership. The user object, password, security stamp, and Administrator membership are not bound from the browser.

Identity role membership changes in persistence when an Administrator grants or removes Organizer. An authenticated cookie carries claims issued for a particular session; its refresh and revalidation schedule determines when an existing session sees the changed privileges.

| Moment | Persisted Organizer membership | Effective access in a newly issued session |
| --- | --- | --- |
| Before grant | Absent | Participant-level access |
| After grant | Present | Organizer management access |
| After removal | Absent | Participant-level access; existing Event owner IDs remain unchanged |

For example, grant Organizer access to a Participant, associate a Draft Event with the newly authorized account, then remove Organizer access. The user's new session loses Organizer management permission, while the Event retains the same owner key. End the affected session and sign in again when verifying these transitions. For systems requiring immediate revocation, configure and verify the permitted claim-refresh or session-invalidation window. Additional strategies are compared in [Authorization architectures and privilege lifecycle](#authorization-architectures-and-privilege-lifecycle).

## Layered validation and database uniqueness

Case-insensitive Category-name uniqueness is a **persisted invariant**. Validation before writing provides a precise field error and preserves attempted input, but it observes only the state at query time. Two administrators can both read “Robotics not present”; a correctly configured unique index still allows at most one conflicting value to commit. The explicit SQL Server column collation defines how that index compares `Robotics` and `robotics`, independently of incidental database defaults.

Consider two administrators opening Create with the same new Category name. Each form first checks a database snapshot; either may see the name as available. SQL Server's unique index then serializes the conflicting result at persistence time. The later request receives the recognized index conflict and redisplays its attempted name with a field error.

The layers are complementary: `[Required]` and maximum length constrain the input contract; trimmed values avoid accidental surrounding whitespace; `AnyAsync` with the selected collation offers immediate feedback; and the unique index is the final barrier. An Edit query excludes its own Category ID so retaining the same name is valid. Handle unique-key errors (2601/2627) for the named Category index as expected input conflicts. Classify expected uniqueness conflicts narrowly so other failures follow the normal diagnostics path. More extensive comparison rules and portability options appear in [String equality, collations, and portable uniqueness strategies](#string-equality-collations-and-portable-uniqueness-strategies).

## Referential integrity and controlled deletion

Deleting a Category changes the meaning of any Event that references it. The SRS resolves this by refusing deletion while even one Event uses the Category. Confirmation GET provides context; `DeleteConfirmed` POST rechecks the current relational state before calling `Remove`. The existing Event→Category `Restrict` foreign key is a second, independent guarantee if another transaction inserts a reference between the pre-check and the delete.

| Sequence | Administrator request | Other concurrent request | Integrity outcome |
| --- | --- | --- | --- |
| Initial check | Reads zero referencing Events | — | Deletion appears eligible |
| Interleaving | Prepares Category removal | Adds an Event referring to that Category | Category is now referenced |
| Save | Requests the DELETE | Commits or competes for locks | SQL Server enforces the relationship |
| Response | Handles the expected named FK conflict | Retains a valid Event reference | Both Event and Category remain consistent |

This timeline illustrates why a friendly pre-check and a database constraint solve different parts of the same user workflow.

SQL Server error 547 accompanied by the expected Event–Category constraint name maps to an in-use refusal; other database errors remain available for diagnosis. A useful distinction is **expected business refusal** versus **unexpected infrastructure failure**. Neither should leave a partially applied Category change. Enforce Category deletion in the POST and database, then provide appropriately conditional controls in the view.

## Verification scope and strength of evidence

Match each claim to the system boundary that can demonstrate it. A relational test using a fresh database context checks persisted rows and foreign-key behavior. An authenticated HTTP test exercises the configured request pipeline, including authorization, anti-forgery, routing, model binding, and controller responses. A browser walkthrough reveals whether the rendered form, navigation, and feedback remain usable with real keyboard interaction and a narrow viewport.

For the Organizer Registration page, combine a foreign-ID HTTP request with inspection of the returned response to evaluate protected information scope. For administrative cancellation, test a missing request-verification token as an HTTP 400 and a valid-token, ineligible Event as a controlled lifecycle refusal. Read each Event in a fresh context to confirm its status and retained owner. For Category rules, use a migrated SQL Server database to exercise the actual unique index and restrictive relationship. These complementary observations support a concise requirement-to-test traceability record.

# Exercises

## Exercise: add current Registration counts to Administrator Event oversight

**Purpose and expected outcome:** Extend a read-only cross-owner projection with a derived relational value (Objectives 2 and 8).

**Prerequisites and SRS ties:** Completed Administrator Event list, persisted Registrations, and `FR-ADM-003` across all statuses and organizers.

**Task:** Display the current Registration count on each Administrator Event card. Introduce a compact view-specific row model and project each count in the database query using `Select` and `Registrations.Count()`. Keep the list ordered by start time and Event ID and preserve Administrator-only access.

**Hints or checkpoints:** Compute the count from persisted Registration rows. Project the count in SQL rather than reading an unloaded navigation collection or issuing one separate count query per Event. Retain Category and Organizer text in the new row model.

**Deliverable:** View-model, controller-query, and Razor changes, plus a short explanation of why a derived count belongs in the read projection.

**Completion and verification:** Build and open a known Event card as Administrator. Check its count against the registered rows in a fresh database query, then repeat the direct non-Administrator access check.

## Exercise: test the dual-role exception and ordinary ownership denial

**Purpose and expected outcome:** Design evidence that separates role authorization from resource ownership (Objectives 1, 2, and 7).

**Prerequisites and SRS ties:** Two Organizer-owned Events, a dual-role account, migrated relational fixture; `UC-006`, `BR-AUT-002`, `FR-REG-009`.

**Task:** Extend the guided HTTP case with a second participant Registration and verify the count, remaining capacity, and all displayed contact data for an allowed dual-role lookup. Recheck that an ordinary Organizer still receives the owner-scope refusal.

**Hints or checkpoints:** Use independent authenticated clients or fresh sessions, and verify the denied response does not contain private participant information.

**Deliverable:** An independently authored extension to the HTTP test and a short assertion-to-requirement explanation.

**Completion and verification:** Run the specific test and confirm both status and content boundaries; record the exact observed runner result.

## Exercise: test Category input boundaries and normalization

**Purpose and expected outcome:** Analyze field validation and canonical stored values at the administrative form boundary (Objectives 5–7).

**Prerequisites and SRS ties:** Migrated Category schema, protected Create form, disposable SQL Server fixture; `FR-ADM-005`, `FR-ADM-006`, `BR-CAT-001`.

**Task:** Independently implement HTTP cases for a whitespace-only name, a 101-character name, and a valid name with surrounding spaces. Check the form feedback for invalid submissions and the trimmed stored value for a valid submission. Verify that rejected inputs create no Category rows.

**Hints or checkpoints:** Use authenticated Administrator clients and real anti-forgery tokens. Exercise `[Required]`, `[StringLength(100)]`, and the server-side `Trim()` assignment separately. Re-read the Category rows with a fresh database context.

**Deliverable:** Parameterized or separate HTTP test cases and a compact input/expected-result/observed-result table.

**Completion and verification:** Run the new cases against a migrated relational fixture; verify field-level errors and unchanged row counts for invalid inputs, and the canonical stored name for valid input.

## Exercise: prove deletion does not damage referenced Events

**Purpose and expected outcome:** Demonstrate the difference between a friendly pre-check and a relational integrity backstop (Objectives 5–7).

**Prerequisites and SRS ties:** Event referencing one Category, plus unused Category; `BR-CAT-002`, `FR-ADM-007`.

**Task:** Attempt both direct POST and confirmation-based deletion of the in-use Category; then remove an unused Category. Examine the referenced Event before and after each request.

**Hints or checkpoints:** A GET must never delete. For POSTs, use real anti-forgery tokens and fresh database contexts.

**Deliverable:** A concise HTTP/result/database-state evidence record.

**Completion and verification:** The referenced Category and Event remain unchanged; the unused Category can be removed with the specified authorized workflow.

## Exercise: compare evidence boundaries

**Purpose and expected outcome:** Evaluate what different verification layers demonstrate (Objectives 7 and 8).

**Prerequisites and SRS ties:** Completed Lesson 8 workflows and inherited testing infrastructure; `AC-006`, `AC-010`.

**Task:** Choose one role-restricted command and draw a request-flow diagram. Label which portions are exercised by direct controller invocation, relational integration, authenticated HTTP, and manual browser checks.

**Hints or checkpoints:** Identify middleware authorization, anti-forgery, controller logic, and persistence reads/writes independently.

**Deliverable:** Diagram and a short list of bounded evidence claims.

**Completion and verification:** Map each claim to the relevant request, data, and presentation observation path.

# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| Another Organizer can see private Registrations | Event lookup omits `OrganizerId` / explicit dual-role rule | Inspect the Event predicate, repeat forged-ID HTTP request, verify no participant data in response. |
| Dual-role Organizer gets not found on foreign Event | Owner filter does not incorporate `UC-006` Administrator exception | Check actual dual-role claims and predicate; test ordinary and dual-role identities separately. |
| Administrator list shows only upcoming Published Events | Public catalog query reused for admin | Query all Events without public eligibility filter; compare Draft and Cancelled rows. |
| Administrator cancellation unexpectedly changes the owner | `OrganizerId` was reassigned to reuse an Organizer mutation | Remove reassignment; change only permitted status and inspect the same persisted Event ID. |
| Admin cancellation differs at exact start time | `DateTime.Now` used instead of injected clock | Use `TimeProvider.GetLocalNow().DateTime` consistently and a fixed-time regression. |
| Participant sees Admin navigation or gains access | Role-aware UI treated as security or missing authorization attribute | Check class-level Administrator policy and test direct authenticated requests. |
| Role removal appears ineffective in target browser | Old authenticated cookie still carries previous role claims | End target session, sign in again, verify role membership and protected endpoint. |
| Category duplicate differs only by case but is inserted | Unique index/collation absent or unapplied migration | Inspect schema, apply reviewed migration, repeat conflict against SQL Server. |
| Existing Category cannot keep its own name on Edit | Duplicate check fails to exclude current Category ID | Add exclusion predicate; run unchanged-name and cross-record conflict cases. |
| Category referenced by Event is deleted or Event is cascaded | Restrict relationship modified or only UI link hidden | Reinstate restrictive FK, inspect migration, run direct POST and row-preservation checks. |
| Every database failure reports duplicate name | Broad exception handler replaces narrow SQL Server error checks | Filter uniqueness errors 2601/2627 and FK 547; let other faults surface for diagnosis. |
| New tests fail to compile after controller change | Old test constructs `EventsController` with obsolete parameters | Reuse the accepted host and current constructor/dependencies; avoid creating another fixture. |
| Protected route unexpectedly accepts a Participant | Missing Administrator authorization on endpoint | Send authenticated Participant HTTP request and inspect server authorization and response status. |
| Prior registration-capacity tests regress | Existing RegistrationService or guarded capacity Edit altered accidentally | Revert unrelated changes, run existing SQL Server and HTTP regressions, check final count/capacity invariant. |

# Checking Your Understanding

<details><summary>Why is Organizer role membership alone insufficient to read an Event's current Registration list?</summary>

The role allows entry into Organizer workflows, but an ordinary Organizer may read only owned Event registration data. The server must establish the Event ownership predicate before retrieving participant information. The SRS additionally authorizes the explicit Organizer-plus-Administrator exception.

</details>

<details><summary>Can an Administrator cancel a Published Event after its start or change its OrganizerId in order to cancel it?</summary>

No. Administrator authorization changes who may request the command, not its domain eligibility. The Event must still be Published and not started, and ownership remains persisted unchanged.

</details>

<details><summary>Why is an existing user's Organizer role removed without editing that user's Event records?</summary>

Role membership is current permission state, whereas each Event's OrganizerId records domain ownership. Revoking a privilege limits future access but does not retroactively transfer or destroy domain relationships.

</details>

<details><summary>Why does the UI need a Category duplicate pre-check if the database has a unique index?</summary>

The pre-check produces timely, understandable field feedback; the unique index enforces the invariant even when concurrent requests pass separate pre-checks. Both layers serve different purposes.

</details>

<details><summary>What protects an Event-referenced Category if a reference is inserted after the in-use pre-check?</summary>

The database foreign key with restrictive deletion behavior rejects the deletion at persistence time. A narrow handler can convert the expected SQL Server conflict to controlled feedback.

</details>

<details><summary>What changes when the current registration count is a derived query rather than a stored Event counter?</summary>

Registration create/cancel operations alter the association rows, and every management page derives the count from those authoritative rows. Rendering is still a snapshot, so state-changing commands must make fresh eligibility decisions.

</details>

<details><summary>Which verification approach checks Administrator route authorization, and which checks the persisted cancellation state?</summary>

Send authenticated HTTP requests using Administrator and non-Administrator sessions to check route authorization. Then read the Event through a fresh relational context to check the status change and preserved OrganizerId.

</details>

<details><summary>How would you distinguish a failed anti-forgery check from a business-rule refusal?</summary>

A missing or invalid token is rejected by the request pipeline, commonly with HTTP 400, before the action's domain mutation. An eligible authenticated POST that violates a domain rule reaches the action and produces its controlled refusal; neither should alter database state.

</details>

# Summary

Protected workflows are best understood as decisions over trusted identity, privileges, resource relationships, operations, and current domain state. An authorization role does not replace ownership analysis, and exceptional privileges must be defined explicitly. Actor-scoped retrieval reduces unintended information disclosure, while response-specific projections make derived relational information available without introducing redundant authoritative counters.

Privileged writes must still honor the same domain invariants as ordinary writes. Separating authorization, input contracts, current-state checks, confirmation, and persistence makes these boundaries reviewable. Least-privilege permission changes are narrow operations on current authorization state; already-issued sessions may need refresh or reauthentication before their claims reflect changed privileges.

Reliable data management combines readable application validation with enforceable database uniqueness and foreign keys. Pre-checks improve feedback but do not close concurrency windows, and narrow handling of expected conflicts preserves diagnosis of unexpected errors. Tests should likewise be matched to the boundary they actually exercise: relational state, component decisions, authenticated request behavior, and observable user experience offer complementary evidence. Incremental, traceable changes keep these guarantees understandable across different technology stacks.

# Checkpoint

**Inherited baseline:** CEMS has ASP.NET Core MVC/Razor, EF Core/SQL Server, Identity, the Event/Category/Registration relational model, public Published/upcoming search with 10/25/50 pagination, direct Published/Cancelled Details, Organizer-owned Create/Edit/lifecycle operations, Participant registration and cancellation, `TimeProvider`, persisted count-based capacity, SQL Server final-seat coordination, and isolated relational plus authenticated HTTP tests.

**New in this lesson:**

- `OrganizerRegistrationViewModel` and protected read-only Event Registration projection with explicit dual-role Administrator exception.
- `Views/Events/Registrations.cshtml` and owned-Event **Registrations** management link.
- Administrator dashboard, application-wide Event list, and guarded cross-owner Published cancellation using the existing clock and feedback conventions.
- Existing-user Organizer membership listing and narrowly scoped grant/remove commands through Identity services.
- Category Create/Edit/Delete input and confirmation models, guarded controller actions, and semantic Razor forms.
- Explicit case-insensitive Category-name collation, bounded name length, unique index, and inspected migration while retaining the restrictive Event→Category relationship.
- Role-aware Administrator navigation, reusable management-list styling, and additional relational/authenticated-HTTP regression cases.

**Resulting baseline for next lesson:** CEMS supports Organizer Registration oversight and Administrator Event, role, and Category management. Its accumulated workflows retain server-rendered MVC/Razor, SQL Server relationships, authorization rules, the fixed-clock convention, and the established Registration reliability design.

# Next Lesson Preview

Lesson 9 focuses on stabilizing the cumulative application: review authorization and anti-forgery coverage, reconcile the requirements traceability matrix with executed tests, examine accessibility and query behavior, document setup and migration steps, publish and deploy in an approved environment, smoke-test all actor workflows, and prepare an evidence-based midterm demonstration.

# Appendix

## Authorization architectures and privilege lifecycle

### Roles, claims, policies, and resources

Role-based checks group permissions at the actor level. A claim-based system represents specific assertions about the principal; a policy can combine claims, roles, and predicates; resource-based authorization also considers a particular record and operation. These are related dimensions: a policy may consult the current resource owner rather than replace the entire role system.

In CEMS, the ordinary Organizer Registration decision is an example of a role predicate plus a resource predicate. A broader system might register an authorization handler that receives the resource and operation, avoiding duplicated predicates across many controllers. That indirection has costs: more components and authorization rules that are less visible at the action site. For a small set of actor-specific operations, explicit controller checks remain clear.

| Mechanism | Decision input | Illustrative use |
| --- | --- | --- |
| Role | Broad membership | May enter Administration |
| Claim | Statement carried by the principal | Has a particular institution affiliation |
| Policy | Combined requirements | Requires elevated membership and an approved context |
| Resource-based check | Specific record and operation | May read this Event's Registrations |

The dimensions can be combined. A resource handler might implement the ownership rule for many different protected endpoints, while an outer role check still limits the workflow to Organizers.

### Permission changes and existing sessions

A membership update changes persistent authorization information. For example, a Participant promoted to Organizer may gain access to owned-Event management in a fresh session. If that membership is later removed, their Event records still carry the same durable `OrganizerId`, while a fresh session loses permission to invoke Organizer actions. An Administrator can continue eligible oversight without rewriting the record's creator. The browser may continue to present a cookie with claims issued earlier, and different applications refresh those claims at different times. Options include requiring reauthentication, validating a security stamp or ticket more often, coordinating session invalidation, or consulting a current permission store for sensitive actions. Each changes latency, persistence load, operational complexity, and the time window during which stale privileges could be honored.

For a system with immediate-revocation requirements, define an explicit maximum age for previously issued permission claims. Compare periodic cookie revalidation, server-managed session revocation, and current-permission lookup on sensitive requests. A shorter window may require additional reads or invalidation infrastructure; a longer window increases the time before a privilege removal affects existing sessions. The CEMS classroom path uses sign-out/sign-in to make the effect deterministic without depending on undocumented refresh timing.

### Ownership after privilege removal

An institution may retain historical ownership while shifting responsibility for an Event to a new staff member. Consider three possible governance policies: preserve the original organizer and let Administrators supervise; add a separate responsible-contact association; or explicitly transfer ownership through an authorized domain command. Each policy answers a different question about who created the record, who coordinates it now, and who may modify it.

If ownership transfer becomes a requirement, define which Event states permit reassignment, whether existing Registrations are preserved, and how affected Organizers and Participants are notified. An audit entry could record the acting Administrator, previous and new owners, reason, and timestamp without overwriting the creation history. Retention and privacy rules would govern how long those records remain available. For CEMS, durable `OrganizerId` records and explicit Administrator intervention provide the selected ownership policy.

## String equality, collations, and portable uniqueness strategies

### Equality is a database contract

A human phrase such as “same name ignoring case” must become a precise equality rule in persistence. For a comparison exercise, consider `Music` versus `music`, `Cafe` versus `Café`, and `  Music  ` versus `Music`. Case folding, accent handling, and whitespace normalization answer three different questions. Write down the intended answer for each pair before choosing a collation or normalization rule. SQL Server collation determines character comparison and sorting behavior for the indexed column. The selected `SQL_Latin1_General_CP1_CI_AS` is case-insensitive (`CI`) and accent-sensitive (`AS`): `Music` and `music` conflict, while accent handling is a distinct dimension. Other collations or providers can differ in accent behavior, locale rules, and Unicode handling.

Whitespace also deserves attention. The Category form trims surrounding whitespace for the canonical stored value. String comparison, Unicode normalization, and cultural case-folding are independent choices; a storage model should specify the ones material to its requirements rather than assume that C# string comparisons and SQL Server comparisons always match.

### Comparing uniqueness designs

| Approach | Strength | Trade-off |
| --- | --- | --- |
| Column collation with unique index (CEMS) | Enforces case-insensitive uniqueness directly on the stored name | Provider-specific collation choice; migration must reconcile existing data |
| Separate normalized-name key with unique index | Can make canonical comparison explicit and reusable across queries | Additional persisted or computed field and normalization contract |
| Application-only lowercase pre-check | Simple immediate form feedback | Cannot enforce uniqueness against concurrent writers; affected by provider comparison differences |
| Database-specific expression/computed index | Can support specialized comparisons and searches | Portability, migrations, and query matching become more complex |

The last two database-backed designs are legitimate options under different requirements; none makes a user-facing pre-check sufficient as the sole consistency guarantee. An existing database may contain case-insensitive duplicates, surrounding whitespace, or values longer than the proposed storage limit. Inspect these records and their references before adding the constraint. A deliberate migration should identify conflicts before attempting the constraint change. If `Music` and `music` both exist, review every referencing Event and select a canonical Category before merging records or applying the index. Preserve relationship identities and use a backup or controlled rollback plan before changing an established dataset.

### Concurrent requests and the final guarantee

The selected CEMS design lets the unique index arbitrate competing inserts and the restrictive foreign key arbitrate deletion/reference conflicts. Other systems may add explicit locking, an application-level serialization queue, or a bounded retry policy when the cost of contention justifies it.

| Strategy | Useful when | Engineering trade-off |
| --- | --- | --- |
| Constraint plus controlled conflict response | A conflicting write is rare and users can revise their request | Small implementation, precise persisted invariant |
| Explicit transaction with coordinated locks | Multiple dependent decisions must observe one serialized state | Stronger coordination, potential blocking and deadlocks |
| Bounded retry after transient failures | The operation is safe to repeat and contention is temporary | Requires idempotency rules, retry limits, and clear failure feedback |
| Serialized command processing | Operations need ordered throughput across callers | Additional worker, queue, and operational failure modes |

A duplicate Category name is a semantic conflict: retrying the identical insert will still violate the uniqueness rule. A transient deadlock or connection failure has different recovery characteristics. Choose retry behavior by the failure's meaning, preserve narrow database constraints, and keep the user-facing outcome deterministic.

## Administrative architecture, scaling, and accountability

### Organizing administrative modules

Three conventional controllers keep Event oversight, Organizer access, and Category management independently navigable and easy to test. An MVC Area would group routes, views, and controller discovery under an administrative module; a shared application service would instead centralize a cross-controller domain operation. These solve different problems—application organization versus reusable business behavior—and can be combined if both needs emerge. As the number of administrative functions increases, grouping routes and views within an Area can improve organization without changing authorization rules. A dedicated application service becomes useful when multiple actors need a complex shared command; introducing one solely for a short eligibility check adds abstraction without demonstrating reuse.

An Administrator Event list is broader than the public catalog, not automatically more efficient. **CEMS now paginates it** using `CountAsync` and ordered `Skip/Take` to select at most 10, 25, or 50 Event rows per request, preserving all owners and statuses. This gives bounded application materialization and accessible page navigation; it does **not** make arbitrarily deep offsets free or guarantee a snapshot across separate requests. Purpose-built projections and search remain possible future optimizations, not implemented requirements.

### Actor attribution and audits

A privileged actor may change a resource they do not own. When accountability requirements grow, the system may need to retain **who acted**, **what changed**, and **when**, separately from the Event's Organizer foreign key. An audit record or activity log can represent those facts, and approval workflows can constrain especially sensitive changes. Designing such records raises storage, privacy, retention, and verification questions. In CEMS the cancellation keeps Event ownership stable and communicates the operation through status feedback, giving a clear example of actor/owner separation.

### Operational consistency across workflows

As a system grows, shared domain invariants should be reviewed across all possible writers. Review every writer of a shared invariant, including Organizer actions, Administrator commands, and Participant workflows. CEMS already illustrates this principle through Registration and Capacity Edit coordination; Administrator cancellation must preserve the same Event lifecycle and registration eligibility semantics. Architectural decomposition should follow demonstrated invariant-sharing or scale needs rather than the appearance of enterprise complexity.
