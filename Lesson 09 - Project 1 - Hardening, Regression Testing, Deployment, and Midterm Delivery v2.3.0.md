# **Project — Campus Events**
## **Lesson 9 — Hardening, Regression Testing, Deployment, and Midterm Delivery**

Revision: 2026.11.09.1

# Table of Contents

- [Review of Last Lesson](#review-of-last-lesson)
- [Learning Objectives](#learning-objectives)
- [SRS Requirements and Use Cases](#srs-requirements-and-use-cases)
- [Feature Preview](#feature-preview)
- [Design and Coding](#design-and-coding)
	- [Inspect the accepted repository and establish the stabilization baseline](#inspect-the-accepted-repository-and-establish-the-stabilization-baseline)
	- [Initialize the final traceability working record](#initialize-the-final-traceability-working-record)
	- [Harden the inherited unexpected-error response](#harden-the-inherited-unexpected-error-response)
	- [Review authorization, anti-forgery, and secret boundaries](#review-authorization-anti-forgery-and-secret-boundaries)
	- [Review the catalog, scope-specific queries, and page state](#review-the-catalog-scope-specific-queries-and-page-state)
	- [Coordinate Event lifecycle mutations](#coordinate-event-lifecycle-mutations)
	- [Coordinate participant cancellation with Event edits](#coordinate-participant-cancellation-with-event-edits)
	- [Establish a consistent application-time policy](#establish-a-consistent-application-time-policy)
	- [Finalize the inherited README and repository handoff](#finalize-the-inherited-readme-and-repository-handoff)
	- [Prepare a Release artifact and an approved deployment handoff](#prepare-a-release-artifact-and-an-approved-deployment-handoff)
- [Discussion](#discussion)
	- [Requirements-driven stabilization and bounded change](#requirements-driven-stabilization-and-bounded-change)
	- [Requirements traceability and reproducible evidence](#requirements-traceability-and-reproducible-evidence)
	- [Failure classification, authorization, and defense in depth](#failure-classification-authorization-and-defense-in-depth)
		- [Independent roles are not independent security authorities](#independent-roles-are-not-independent-security-authorities)
	- [Usability, accessibility, and compatibility as system qualities](#usability-accessibility-and-compatibility-as-system-qualities)
	- [Query efficiency, deterministic retrieval, and performance evidence](#query-efficiency-deterministic-retrieval-and-performance-evidence)
	- [Regression testing and confidence across verification boundaries](#regression-testing-and-confidence-across-verification-boundaries)
	- [Release engineering, configuration, and deployment correctness](#release-engineering-configuration-and-deployment-correctness)
	- [End-to-end architectural reasoning and technical defense](#end-to-end-architectural-reasoning-and-technical-defense)
- [Verification and Validation](#verification-and-validation)
	- [Automated](#automated)
		- [Establish the test environment and discover the actual suite](#establish-the-test-environment-and-discover-the-actual-suite)
		- [Retain the controlled last-seat evidence boundary](#retain-the-controlled-last-seat-evidence-boundary)
		- [Verify controlled cross-workflow interleavings](#verify-controlled-cross-workflow-interleavings)
	- [Manual](#manual)
		- [Local hardening and regression](#local-hardening-and-regression)
		- [Accessibility, compatibility, and performance](#accessibility-compatibility-and-performance)
		- [Approved deployment and persistence](#approved-deployment-and-persistence)
		- [Verify the production-safe unexpected-error boundary](#verify-the-production-safe-unexpected-error-boundary)
		- [Review usability, accessibility, and browser compatibility](#review-usability-accessibility-and-browser-compatibility)
		- [Measure the performance contract in its specified context](#measure-the-performance-contract-in-its-specified-context)
		- [Deploy and complete the production smoke test](#deploy-and-complete-the-production-smoke-test)
		- [Rehearse the midterm demonstration and architectural defense](#rehearse-the-midterm-demonstration-and-architectural-defense)
	- [SRS traceability matrix update with evidence](#srs-traceability-matrix-update-with-evidence)
		- [Review, commit, and hand off the verified state](#review-commit-and-hand-off-the-verified-state)
- [Exercises](#exercises)
	- [Exercise: diagnose and repair an authorization regression](#exercise-diagnose-and-repair-an-authorization-regression)
- [Troubleshooting Common Problems](#troubleshooting-common-problems)
- [Checking Your Understanding](#checking-your-understanding)
- [Summary](#summary)
- [Checkpoint](#checkpoint)
- [Next Lesson Preview](#next-lesson-preview)
- [Appendix](#appendix)
	- [Release strategies and delivery automation](#release-strategies-and-delivery-automation)
		- [Manual release, continuous delivery, and continuous deployment](#manual-release-continuous-delivery-and-continuous-deployment)
		- [Immutable artifacts, rollout patterns, and rollback](#immutable-artifacts-rollout-patterns-and-rollback)
	- [Production schema evolution and data recovery](#production-schema-evolution-and-data-recovery)
		- [Reviewable migrations and environment identity](#reviewable-migrations-and-environment-identity)
		- [Expand/contract changes and compatible rollouts](#expandcontract-changes-and-compatible-rollouts)
	- [Production observability and advanced performance diagnosis](#production-observability-and-advanced-performance-diagnosis)
		- [Logs, metrics, traces, and correlation](#logs-metrics-traces-and-correlation)
		- [Latency distributions, query plans, and conditional optimization](#latency-distributions-query-plans-and-conditional-optimization)

# Review of Last Lesson

Lesson 8 extended CEMS from role-scoped participant and organizer workflows into administrative oversight, including bounded 10/25/50 Administrator Event pagination and link visibility that matches the explicit Organizer-plus-Administrator Registrations exception. The `Registrations` endpoint **still** requires Organizer membership; Administrator-only access was not added. You enforced authorization across roles and individual resources, projected current registrations without exposing unrelated participant data, guarded category deletion with relational constraints, and treated a changed role as an authorization-state transition. Its final checkpoint includes the Organizer registration list (with the explicit Organizer-plus-Administrator exception), application-wide Event oversight and cancellation, Organizer role assignment, and case-insensitive Category management.

The accepted solution uses ASP.NET Core MVC/Razor, EF Core with SQL Server, Identity, an injectable `TimeProvider`, and relational and authenticated-HTTP regression tests. Earlier lessons established public search and 10/25/50 pagination, Participant registration with transactional capacity enforcement, and protected Event lifecycle actions. You will stabilize this application, prepare its safe error response and repository handoff, evaluate system qualities, and carry the accepted workflows into the approved release process.

# Learning Objectives

By the end of this lesson, you should be able to:

1. **Evaluate** a completed system against an approved requirements baseline and prioritize narrowly scoped corrections over unnecessary redesign.
2. **Construct** a defensible trace from a requirement through an implementation artifact to bounded, observed acceptance evidence.
3. **Analyze** security and failure-handling boundaries, distinguishing expected refusals from unexpected faults and preserving protected data.
4. **Evaluate** usability, accessibility, compatibility, and data-retrieval behavior using both static inspection and observable user interaction.
5. **Design and interpret** a regression strategy across component, relational, HTTP, concurrency, and manual verification boundaries.
6. **Distinguish** build, publication, deployment, configuration, schema migration, and live operational verification, and explain their risks.
7. **Assess** performance and reliability claims in the context of data volume, test conditions, database persistence, and environment fidelity.
8. **Defend** end-to-end architectural decisions using constraints, control/data flow, explicit trade-offs, and reproducible evidence.

# SRS Requirements and Use Cases

Final CEMS stabilization integrates the previously implemented functional workflows with system-level quality evaluation, release preparation, and acceptance requirements. The requirements below are from the approved Project 1 SRS, version 1.3.3.

| SRS reference | Required behavior / constraint | Lesson contribution |
| --- | --- | --- |
| `UC-001`–`UC-008`; `AC-001`–`AC-007` | All four actors complete the specified public, registration, organizer, and administrator workflows with the approved business rules. | Regress the accepted implementation, including filtered pagination, ownership, capacity, role changes, Category constraints, and controlled failures. |
| `BR-AUT-001`–`BR-AUT-004`; `NFR-SEC-001` | Protected operations enforce effective server authorization independently of navigation visibility. | Audit direct routes, role decisions, ownership filters, and dual-role behavior with positive and negative cases. |
| `NFR-SEC-002`–`NFR-SEC-004` | Protect password verification, state-changing forms, and secret configuration. | Inspect Identity usage, anti-forgery on POST requests, Development secrets, and publication inputs. |
| `NFR-SEC-005`; SRS §11; `AC-009` | Unexpected faults show a generic response; expected validation, login, authorization, missing-resource, and business refusals receive appropriate feedback. | Inspect and, if necessary, complete the inherited exception handler and safe Error action/view; verify failure classifications. |
| `NFR-PER-001` | Normal browse and Details pages complete within approximately two seconds for the specified representative dataset under approved single-user test conditions. | Measure normal browse and Details behavior and record the dataset, environment, durations, and method. |
| `NFR-PER-002`; `AC-001` | Public filter/search/page operations execute on the database before row materialization and retrieve only the selected page. | Audit the inherited composed query and its selectable 10/25/50 page sizes. |
| `NFR-USA-001`–`NFR-USA-004` | Navigation, preserved form input, destructive-action intent, and operation feedback support clear use. | Observe representative workflows in the browser and record findings. |
| `NFR-ACC-001`–`NFR-ACC-003` | Accessible control names, keyboard-operated core workflows, meaningful headings, and understandable pagination state. | Inspect semantic markup and exercise keyboard-based interactions. |
| `NFR-REL-001`–`NFR-REL-003`; `AC-005`; `AC-008` | Committed changes persist; rejected writes do not partially apply; current registrations never exceed capacity through normal workflows. | Regress persisted state, failure atomicity, and established final-seat protections; observe refresh/restart persistence where permitted. |
| `NFR-COM-001`–`NFR-COM-002` | Support at least two course-current major desktop browsers and usable laptop/mobile-width pages. | Complete browser and viewport checks in the approved environment. |
| `AC-010` | Executed automated tests cover critical business behavior, including eligibility/capacity and an authorization-sensitive workflow. | Discover and execute the inherited suite, recording the actual test-case counts and outcomes. |
| `AC-011` | Meet the SRS's required security, usability, accessibility, reliability, and compatibility qualities. | Evaluate the required NFR families through the corresponding security, usability, accessibility, reliability, and compatibility checks. |
| `AC-012` | Deploy to an instructor-approved environment and demonstrate the required end-to-end workflows there. | Prepare a Release artifact, follow the approved deployment/migration procedure, and perform a deployed smoke test. |
| `AC-013` | Document prerequisites, configuration, database preparation, and application/test execution. | Finalize the inherited root `README.md` and maintain a repository traceability record. |

**Lesson-level acceptance contract:** Preserve the accepted CEMS workflows; complete a safe unexpected-error response; critically assess privilege assignment; coordinate Event lifecycle/participant-cancellation writers with the inherited Event-row lock; review Administrator pagination, authorization, query, and interface quality; update setup and traceability documentation; publish a Release artifact; and verify the application in the applicable local and instructor-approved environments. For final-project acceptance, use observed results for each applicable requirement and acceptance criterion.

# Feature Preview

Begin with the accepted Lesson 8 dataset: two Organizers owning different future Events, a Participant who can register for an eligible Published Event, and an Administrator who can cancel other Organizers' eligible Events and manage Categories. CEMS already renders a public Published/upcoming catalog with Title/Location search, Category filtering, selectable page sizes of **10, 25, or 50**, preserved criteria, and First/Previous/Next/Last navigation. The Registration service already guards duplicate requests and the final seat; these form the existing application baseline for the final delivery.

A complete final delivery connects those familiar workflows to a controlled operational result. Consider three representative observations:

1. A Visitor selects 25 results per page, combines search with Category, navigates to the last page, and opens Details. The page retains the chosen criteria, and the database returns only the selected Event rows.
2. A Participant posts a duplicate Registration or an Organizer requests a foreign owned Event. The server returns the established controlled refusal and does not modify protected data. An unrelated unexpected server exception instead reaches a generic error response in a non-Development environment, without disclosing SQL or stack details.
3. An instructor-approved deployed instance opens with the expected schema and configuration. Its public and protected workflows work against persistent database records; the repository provides the setup instructions, release information, regression results, and evidence needed to judge final acceptance.

**Request-to-acceptance workflow:**

```text
approved SRS requirement
    → application/controller/service/database or Razor artifact
    → selected verification procedure
    → observed HTTP/browser/SQL/test result with environment and date
    → traceability status and acceptance decision
```

> **Screenshot placeholder — controlled production error response**
>
> **Illustration needed:** The existing shared-shell generic error page reached after a temporary instructor-controlled fault in a local non-Development run.
>
> **Expected visible result:** A clear heading, neutral recovery message, and route back to the Events catalog; no exception text, server path, or connection detail.
>
> **Application state:** Temporary *local* Production-mode test fault, no production credentials and no deployed fault.
>
> ![Expected generic error page without sensitive technical details](images/lesson-09/generic-error-response.png)

# Design and Coding

## Inspect the inherited solution

Inspect your existing ***Campus Events*** solution before making changes.

Make sure that you staged and commited the previous lesson:

```cmd
git show -s HEAD
```

**Expected output:** should contain "Lesson 08: Implemented and Verified".

Otherwise, build, test, run, stage, and commit:

```cmd
dotnet build
dotnet test
dotnet run --project src/CampusEvents
git add .
git commit -m "Lesson 08: Implemented and Verified"
```

Then, create a new branch:

```cmd
git switch -c lesson-09
```

## Initialize the final traceability working record

The approved SRS provides a requirements traceability matrix and `AC-001` through `AC-013`. Create the repository folder if absent and maintain **`docs/Project1-Traceability.md`**. Carry forward dated records from the Lesson 8 checkpoint with their original provenance. Start with this concise structure:

```markdown
# Project 1 — CEMS traceability and acceptance evidence

## Requirement status legend

- PLANNED: requirement-related implementation is scheduled.
- DEFERRED: requirement-related work has an explicit approved deferral.
- PARTIAL: only part of the required scope is complete or supported by observed results.
- IMPLEMENTED—UNVERIFIED: implementation exists; the relevant observed check is outstanding.
- VERIFIED FOR LESSON SLICE: a bounded behavior has been observed successfully.
- FAILED: an executed check did not meet its expected result.

Use NOT EXECUTED only in the verification/evidence field for an activity that has not run.

## Requirement and acceptance records

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| AC-010 | tests/CampusEvents.Tests/ | NOT EXECUTED | IMPLEMENTED—UNVERIFIED | Record discovery, actual result, and date. |
| AC-012 | Release publication; approved host | NOT EXECUTED | PARTIAL | Host, URL, migration, and live smoke evidence pending. |

## Evidence context

Date: Pending | Environment / dataset: Pending | Procedure / command or scenario: Pending | Observed outcome: Not executed | Commit / repository reference: Pending
```

Preserve inherited records with their originating date and environment, and enter the actual implementation and execution information already available.

**Expected checkpoint:** `docs/Project1-Traceability.md` exists with a requirement/status template consistent with the approved SRS.

## Harden the inherited unexpected-error response

A user who submits an invalid form or lacks authorization has encountered an *expected* refusal. A database outage or unhandled application exception is an *unexpected* fault. The latter must not show exception internals to the browser (`NFR-SEC-005`). Lesson 1 established `Views/Shared/Error.cshtml`; use that shared view for the production-safe error response.

First inspect the existing non-Development branch in `src/CampusEvents/Program.cs`:

```csharp
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}
```

Retain the inherited middleware ordering and centralize unexpected-fault handling in the configured exception middleware. In `src/CampusEvents/Controllers/HomeController.cs`, inspect the existing `Error()` action. If missing or inappropriate, use this response that requires no fault details from request input and does not cache error content:

```csharp
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public IActionResult Error()
{
	Response.StatusCode = StatusCodes.Status500InternalServerError;
	return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
```

Preserve the established route and `ErrorViewModel` return type, correcting only a contract mismatch if one exists. `Activity.Current` comes from `System.Diagnostics`, as in the MVC template controller. Keep the Error action reachable by the exception middleware for both GET and POST failures. `UseExceptionHandler` normally preserves the error status; the explicit assignment states the intended HTTP 500 response.

Keep the inherited shared Error view and `ErrorViewModel` in agreement, and render neutral, navigable content:

**File: `src/CampusEvents/Views/Shared/Error.cshtml`**

```cshtml
@model ErrorViewModel

@{
	ViewData["Title"] = "Something went wrong";
}

<h1>Something went wrong</h1>

<p role="alert">We could not complete your request. Please try again later.</p>

<p><a asp-controller="Events" asp-action="Index">Return to Events</a></p>

@if (Model?.ShowRequestId == true)
{
	<p><strong>Reference:</strong> <code>@Model.RequestId</code></p>
}
```

Keep the controller and shared view consistent on `ErrorViewModel`. An optional request identifier helps correlate a browser error with restricted server diagnostics. Render a neutral message and navigation link while protecting exception messages, stack traces, SQL text, connection strings, and server file paths. Handle `/Account/AccessDenied`, HTTP 404, field-validation, and capacity refusals through their established routes.

**Expected checkpoint:** The configured non-Development handler serves the shared generic Error page with HTTP 500, while normal validation and authorization use their established responses.

## Review authorization, anti-forgery, and secret boundaries

Review protected operations systematically. Role-aware navigation helps users discover permitted features; server authorization enforces access to each operation. Inspect the inherited controller/service decisions and record any correction required by the SRS:

| Operation family | Authoritative server decision | Protected state or information boundary |
| --- | --- | --- |
| Participant Registration creation | Authenticated Participant; eligible Published/upcoming Event, no current duplicate, remaining capacity | Current Registration count and Event capacity/status |
| Participant cancellation | Authenticated Participant; current Registration belongs to the signed-in user and cancellation occurs before Event start | Other users' Registrations and current count on refusal |
| Organizer Create/Edit/Publish/Cancel/Delete | Organizer role, server-assigned owner, persisted ownership, Event status/time/capacity rules | Event owner, lifecycle, capacity, and related rows |
| Organizer participant oversight | **Organizer membership required**; owner-scoped Event query with explicit **dual-role** foreign-owner read exception; Administrator-only is denied | Participant names/emails on foreign Events |
| Administrator Event oversight/cancellation | Administrator role and eligible future Published Event | Original `OrganizerId` and Event relationships |
| Administrator Organizer grant/removal | Administrator role; only `Organizer` membership on an existing account | Account identity and unrelated roles |
| Administrator Category changes | Administrator role, normalized uniqueness and referenced-Category restrictions | Existing Category and Event rows |

Audit direct URLs as well as the rendered menu. In particular, the Administrator-only session should see **no** `View Registrations` link on `/Admin/Events` and must be denied a forged direct request; the dual-role session should see the link and follow it; the Organizer-only session should see it on owned `/Events/Manage` cards, including Draft and Cancelled. Keep anti-forgery and denied-response data checks separate from link-discovery checks. Review every state-changing form's use of POST and anti-forgery validation, including Login/Logout, Event management, Registration, Category, and role-management commands. Enforce anti-forgery validation through the applicable controller/filter configuration and emit tokens in state-changing forms. Keep GET requests read-only. Observe that role changes may require a fresh sign-in for existing cookie claims to reflect the new membership.

Inspect ignored local secrets, connection-string configuration, and Development seed account initialization. Keep `SeedAccounts:Organizer:*` and `SeedAccounts:Administrator:*` credentials confined to approved Development configuration; production secrets come from the deployment platform's protected configuration. Verify tracked files do not contain actual credentials. Correct material defects at their server or configuration boundary and note each change against its requirement.

## Review the catalog, scope-specific queries, and page state

Trace the inherited public `EventsController.Index` query from filter construction through terminal materialization. Its accepted contract is selectable `pageSize` values **10, 25, or 50**, with query filtering, counts, and deterministic ordering before fetching a bounded page. A conceptual sketch of the required sequence is:

```csharp
// Representative extract from the inherited catalog action, after input validation.
var now = _timeProvider.GetLocalNow().DateTime;
IQueryable<Event> query = _context.Events.AsNoTracking()
	.Include(e => e.Category)
	.Where(e => e.Status == EventStatus.Published && e.StartDateTime > now);

if (!string.IsNullOrWhiteSpace(search))
{
	var searchTerm = search.Trim();
	query = query.Where(e => e.Title.Contains(searchTerm) || e.Location.Contains(searchTerm));
}

if (categoryId.HasValue)
{
	query = query.Where(e => e.CategoryId == categoryId.Value);
}

var totalCount = await query.CountAsync();
var pageRows = await query.OrderBy(e => e.StartDateTime).ThenBy(e => e.Id)
	.Skip((int)offset).Take(currentPageSize).ToListAsync();
```

The inherited action validates `currentPageSize`, computes a safe `offset`, and projects the page rows into `EventSearchViewModel` as established in Lesson 3. Inspect parameter normalization, last-page clamping, empty-state behavior, retained search/Category/page-size links, and page-size validation against the accepted implementation and SRS §11. Confirm that `CountAsync` counts the filtered match set, Category data is eagerly available for the page, and row materialization follows database-side filtering and paging.

Review `AdminController.Events` separately using the Lesson 8 `AdminEventsViewModel`: the `CountAsync` query must cover **all statuses and owners**, and only ordered selected rows may be materialized with `Skip((int)offset).Take(pageSize)`. Check 10/25/50, 400 invalid size, normalized empty/over-range pages, first/last links, and POST redirect retention after cancellation. This is a distinct `FR-ADM-003` contract, not the public `FR-SRH-009` filter.

Also review Event Details availability, Participant My Registrations user scope, Organizer Registrations authorized Event-first query, and Administrator cross-owner list with its intentional broader scope. For each, ask whether a query materializes too early, loads unnecessary columns, triggers per-row navigation lookups, or leaks another actor's data. Diagnose an N+1 pattern by tracing repeated per-row database requests. Keep the correction bounded to the identified query behavior.

**Expected checkpoint:** Document query eligibility, database-side filtering and paging, stable ordering, preserved page state, and role-specific data scope. Apply any required bounded correction and inspect the resulting query path.

## Coordinate Event lifecycle mutations

Lesson 6's Publish/Organizer Cancel and Lesson 8's Administrator Cancel correctly check state and time **sequentially**, but they do not yet join the SQL Server Event-row coordination used by Lesson 7's Register/Capacity Edit. A concurrent Edit could alter `StartDateTime` or status between another writer's read and update. This lesson closes that bounded gap; **do not modify Lesson 6/8 snapshots retroactively to claim these protections were already present**.

**File: `src/CampusEvents/Services/EventWriteLock.cs`** — centralize the same SQL Server `UPDLOCK, HOLDLOCK` convention used in `RegistrationService` and inherited `LoadOwnedEventForUpdateAsync`. A transaction must already be open when this helper runs. Keep the SQL parameterized through `FromSqlInterpolated`.

```csharp
using CampusEvents.Data;
using CampusEvents.Models;
using Microsoft.EntityFrameworkCore;

namespace CampusEvents.Services;

public static class EventWriteLock
{
	public static Task<Event?> LoadAsync(ApplicationDbContext context, int eventId)
	{
		return context.Events.FromSqlInterpolated($@"
			SELECT * FROM [Events] WITH (UPDLOCK, HOLDLOCK)
			WHERE [Id] = {eventId}").SingleOrDefaultAsync();
	}
}
```

After adding the helper, remove the duplicate raw SQL from two inherited writers so there is **one documented Event-lock acquisition protocol**:

**File: `src/CampusEvents/Services/RegistrationService.cs`** — replace the `FromSqlInterpolated` Event acquisition inside `TryRegisterAsync` with:

```csharp
var eventItem = await EventWriteLock.LoadAsync(_context, eventId);
```

Do not change its outer transaction, post-lock eligibility checks, duplicate check, capacity recount, unique-index handling, or commit/rollback behavior. This refactor is behavior-preserving for Register, not an alternative to the authoritative transaction.

**File: `src/CampusEvents/Controllers/EventsController.cs`** — replace the inherited `LoadOwnedEventForUpdateAsync` body with:

```csharp
private async Task<Event?> LoadOwnedEventForUpdateAsync(int id, string organizerId)
{
	var eventItem = await EventWriteLock.LoadAsync(_context, id);
	return eventItem?.OrganizerId == organizerId ? eventItem : null;
}
```

This helper must be called inside its existing Edit/DeleteConfirmed transaction. It still performs the trusted owner check **after** acquiring the Event row. Retain `EventNotFound()` for missing and foreign records.

**File: `src/CampusEvents/Controllers/EventsController.cs`** — add `using CampusEvents.Services;`. In Publish and Organizer Cancel, authenticate/resolve the Organizer as before, **then** use the following transaction pattern. Apply the same structure separately to each action, preserving its existing method-level `[HttpPost]`, `[Authorize(Roles = RoleNames.Organizer)]`, and `[ValidateAntiForgeryToken]` annotations. The example is the **complete Publish body**; do not replace `Edit`'s existing lock or remove its validations:

```csharp
[HttpPost]
[Authorize(Roles = RoleNames.Organizer)]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Publish(int id)
{
	var organizer = await _userManager.GetUserAsync(User);
	if (organizer is null) return Challenge();

	await using var transaction = await _context.Database.BeginTransactionAsync();
	var eventItem = await EventWriteLock.LoadAsync(_context, id);
	if (eventItem is null || eventItem.OrganizerId != organizer.Id)
	{
		await transaction.RollbackAsync();
		return EventNotFound();
	}
	if (eventItem.Status != EventStatus.Draft)
	{
		await transaction.RollbackAsync();
		TempData["ErrorMessage"] = "Only a Draft Event can be published.";
		return RedirectToAction(nameof(Manage));
	}
	if (eventItem.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
	{
		await transaction.RollbackAsync();
		TempData["ErrorMessage"] = "A Draft Event can be published only before its start date and time.";
		return RedirectToAction(nameof(Manage));
	}

	eventItem.Status = EventStatus.Published;
	await _context.SaveChangesAsync();
	await transaction.CommitAsync();
	TempData["StatusMessage"] = $"{eventItem.Title} was published.";
	return RedirectToAction(nameof(Manage));
}
```

For **Organizer Cancel**, perform the same authenticated owner check **after locking the Event** and use `Status == Published && StartDateTime > _timeProvider.GetLocalNow().DateTime`; update to `Cancelled`, save, and commit. For missing/foreign resources use `EventNotFound()`; for ineligible status/time use the existing controlled messages and roll back. Never grant cross-owner Organizer cancellation to dual-role accounts: UC-005 remains owner-scoped.

**File: `src/CampusEvents/Controllers/AdminController.cs`** — add `using CampusEvents.Services;` and update only the cancellation POST's decision/persistence block, retaining class-level `[Authorize(Roles = RoleNames.Administrator)]` and the existing `page`/`pageSize` redirect state:

```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
var eventItem = await EventWriteLock.LoadAsync(_context, id);
if (eventItem is null)
{
	await transaction.RollbackAsync();
	return NotFound();
}
if (eventItem.Status != EventStatus.Published)
{
	await transaction.RollbackAsync();
	TempData["ErrorMessage"] = "Only a Published Event can be cancelled.";
	return RedirectToAction(nameof(Events), returnRoute);
}
if (eventItem.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
{
	await transaction.RollbackAsync();
	TempData["ErrorMessage"] = "A Published Event can be cancelled only before it starts.";
	return RedirectToAction(nameof(Events), returnRoute);
}

eventItem.Status = EventStatus.Cancelled;
await _context.SaveChangesAsync();
await transaction.CommitAsync();
TempData["StatusMessage"] = $"{eventItem.Title} was cancelled by an Administrator.";
return RedirectToAction(nameof(Events), returnRoute);
```

Do not accidentally leave the old unlocked Event lookup above this block. The `returnRoute` is the previously normalized `page`/`pageSize` state and does not grant permission. Existing `Edit`/`DeleteConfirmed` already lock Event rows; `RegistrationService.TryRegisterAsync` also locks the Event before recounting. All relevant writers should use the **same row** as coordination point, acquire it before assessing mutable Event state, keep transactions short, and maintain a consistent lock order. `UPDLOCK/HOLDLOCK` is SQL Server-specific; do not claim equivalent behavior from EF Core's in-memory provider or SQLite. Database deadlocks/timeouts remain possible operational failures and should be diagnosed rather than silently converted to business-rule refusals.

**Checkpoint:** With two writers, the second decision sees the first committed state. If Edit moves an Event's start into the past before Publish/Cancel obtains the lock, the latter refuses. If Publish/Cancel locks and commits first, a later Edit sees the new status and applies only its documented current-state rules. Every refusal leaves the persisted Event unchanged.

## Coordinate participant cancellation with Event edits

The Lesson 7 `RegistrationsController.Cancel` first reads its owned Registration and associated Event start time without acquiring the Event lock. A competing Edit can change that start time before the delete commits. Registration cancellation does not increase capacity, but its **time eligibility and ownership** still require an authoritative current-state decision.

**File: `src/CampusEvents/Controllers/RegistrationsController.cs`** — add `using CampusEvents.Services;` (already imported for `RegistrationService`). Keep authentication and anti-forgery. Replace only the body of the current Cancel POST with this complete pattern:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Cancel(int id)
{
	var currentUser = await _userManager.GetUserAsync(User);
	if (currentUser is null) return Challenge();

	// Identify the Event without trusting this preliminary read for final authorization.
	var target = await _context.Registrations.AsNoTracking()
		.Where(item => item.Id == id && item.UserId == currentUser.Id)
		.Select(item => new { item.EventId })
		.SingleOrDefaultAsync();
	if (target is null) return NotFound();

	await using var transaction = await _context.Database.BeginTransactionAsync();
	var eventItem = await EventWriteLock.LoadAsync(_context, target.EventId);
	if (eventItem is null)
	{
		await transaction.RollbackAsync();
		return NotFound();
	}

	var registration = await _context.Registrations.SingleOrDefaultAsync(item =>
		item.Id == id && item.UserId == currentUser.Id && item.EventId == eventItem.Id);
	if (registration is null)
	{
		await transaction.RollbackAsync();
		return NotFound();
	}
	if (eventItem.StartDateTime <= _timeProvider.GetLocalNow().DateTime)
	{
		await transaction.RollbackAsync();
		TempData["ErrorMessage"] = "A Registration can be cancelled only before the Event begins.";
		return RedirectToAction(nameof(Mine));
	}

	_context.Registrations.Remove(registration);
	await _context.SaveChangesAsync();
	await transaction.CommitAsync();
	TempData["StatusMessage"] = "Your Registration was cancelled and the place is available again.";
	return RedirectToAction(nameof(Mine));
}
```

The preliminary lookup only supplies `EventId`; it does not authorize deletion. The transaction acquires the Event lock first, then rechecks that this **current Registration ID** still belongs to the signed-in user and Event, and that the Event has not started. If Edit commits a new start time first, cancellation respects it; if cancellation commits first, subsequent Edit observes the updated Registration count. The existing uniqueness/FK constraints still apply, and a denied cancellation must not delete someone else's row.

## Establish a consistent application-time policy

CEMS currently uses `TimeProvider.GetLocalNow().DateTime`, Razor `datetime-local`, and SQL Server `datetime2` without storing an offset or zone. These model a **wall-clock schedule** that assumes a shared application-local zone; a fixed UTC−04 test clock is a test convention, not proof of an approved deployment policy. Do not infer the intended campus zone from a student machine or a web request.

Before deployment, obtain an instructor/institution-approved zone ID and policy for daylight-saving transitions (ambiguous and nonexistent local times), capture and validate that configuration at startup, and apply one explicit conversion from the injected UTC clock to the approved zone wherever eligibility is evaluated. For example, a central clock abstraction may compute `TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), configuredZone).DateTime`, rather than silently using each host's local zone. The zone ID must be verified on the actual deployment OS; Windows and IANA identifiers and provider behavior can differ. Browser input requires a documented interpretation in the same zone. Do **not** silently reinterpret historical `datetime2` values as UTC or change persisted timestamps without a reviewed data migration.

Add fixed-clock test cases on both sides of an Event's exact start, including a host configured to a *different* time zone from the chosen application zone, and explicit tests for the selected DST policy. If the zone/DST policy has not yet been approved, record the deployment item as **BLOCKED—POLICY UNDECIDED**, not VERIFIED. This is a deployment decision, not a reason to invent an institutional time zone for CEMS.

## Finalize the inherited README and repository handoff

Lesson 1 established a root `README.md`; update it rather than creating a competing README. Keep it practical and concise. Organize the final handoff around the actual repository layout, installed prerequisites, configuration steps, and commands that students can reproduce:

````markdown
# Campus Events Management System (CEMS)

CEMS is a server-rendered ASP.NET Core MVC/Razor application with Identity,
EF Core, SQL Server, project-owned CSS, and an xUnit test project.

## Repository layout

- `CampusEvents.slnx`: solution file
- `src/CampusEvents/`: MVC application
- `tests/CampusEvents.Tests/`: relational and authenticated HTTP tests
- `docs/Project1-Traceability.md`: requirements and acceptance record

## Prerequisites and Development configuration

Record the installed .NET SDK version (`dotnet --info`), target framework,
SQL Server/LocalDB edition and version, and EF Core tools version (`dotnet ef --version`).
Install versions compatible with the project's source and migrations.
From `src/CampusEvents`, initialize user secrets if necessary and configure
`ConnectionStrings:DefaultConnection` to point to an isolated Development database.
Set `SeedAccounts:Organizer:Email`, `SeedAccounts:Organizer:Password`,
`SeedAccounts:Administrator:Email`, and `SeedAccounts:Administrator:Password`
using the Development user-secrets workflow established in Lesson 4.
Document the exact nonsecret configuration steps and keys used by this repository.

## Database and application

From the solution root, in the terminal:

```cmd
cd src\CampusEvents
dotnet ef migrations list
dotnet ef database update
dotnet run
```

After stopping the application with Ctrl+C, return to the solution root:

```cmd
cd ..\..
```

## Automated tests

From the solution root, run:

```cmd
dotnet test .\tests\CampusEvents.Tests\CampusEvents.Tests.csproj
```

## Roles, error handling, and quality checks

Participant access follows account registration; Organizer and Administrator
privileges are assigned through protected administrative/development workflows.
Protected actions enforce server authorization and anti-forgery. The shared Error
page handles unexpected non-Development failures. Follow the project traceability
record for browser, accessibility, security, concurrency, and performance checks.

## Release and deployment

Publish with Release configuration; provide the artifact to the instructor-approved
host with protected configuration and an approved SQL Server migration procedure.
Record the deployed URL, schema version, smoke-test results, and acceptance status
in `docs/Project1-Traceability.md`.

````

Replace generic prerequisite language in the README with the SDK/`TargetFramework` and EF Core tool versions actually used, the SQL Server edition, the established user-secrets commands, and the instructor-approved deployment procedure. Preserve useful inherited instructions. Document safe migration prerequisites, browser-compatibility expectations, and both established final-seat verification procedures. The release section identifies the approved host and its configuration procedure.

## Prepare a Release artifact and an approved deployment handoff

The course SRS specifies an instructor-approved environment but not a hosting provider. Obtain the hosting platform, deployment target, SQL Server location, configuration method, migration authorization/backup policy, and startup/log access procedure from the instructor. Record those details only after supplied; the portable part of the workflow is reproducible publication.

From the solution root, in the terminal:

```cmd
dotnet publish .\src\CampusEvents\CampusEvents.csproj -c Release -o .\artifacts\CampusEvents-Release
```

**Expected result:** The Release output folder contains application deployment files ready for the approved host. Treat the folder as generated output and exclude it from source-control staging.

The application expects `ConnectionStrings:DefaultConnection`. A typical environment-variable representation is `ConnectionStrings__DefaultConnection`, whose *value* must be supplied through the approved host's protected configuration. Set `ASPNETCORE_ENVIRONMENT=Production` using the provider's documented mechanism, not by committing a new production password or connection string. Provision approved production accounts and privileges through the deployment process.

Before migrating the approved target, check database identity, schema history, intended migration list, backup/recovery expectations, and compatibility with the deployed application version. In an approved environment **where the EF Core CLI is permitted and credentials are correctly configured**, these commands illustrate the correct project targeting from the solution root:

```cmd
dotnet ef migrations list --project .\src\CampusEvents\CampusEvents.csproj ^
	--startup-project .\src\CampusEvents\CampusEvents.csproj -- --environment Production
dotnet ef database update --project .\src\CampusEvents\CampusEvents.csproj ^
	--startup-project .\src\CampusEvents\CampusEvents.csproj -- --environment Production
```

Do not run these against a database whose identity or backup policy has not been confirmed. Use an instructor-approved migration script, bundle, or host procedure instead when that is the required mechanism. Preserve existing production records; neither `EnsureCreated` nor dropping and recreating the database is an acceptable shortcut.

**Illustrative release sequence:**

```text
review source and test evidence
    → publish Release artifacts
    → configure the approved host and protected settings
    → verify and apply approved schema changes
    → deploy/start the application
    → perform live browser/HTTP and persistence smoke checks
    → record actual evidence and acceptance status
```

Document the approved host details, database identity verification, migration gate, and startup procedure in the handoff.

# Discussion

## Requirements-driven stabilization and bounded change

When feature development ends, changing additional code is not automatically an improvement. The required goal shifts from adding capability to making the existing behavior dependable under realistic use. The approved SRS supplies an **external acceptance boundary**: it distinguishes mandatory behavior, architectural constraints, and optional future enhancements. The accepted Lesson 8 checkpoint supplies an **internal continuity boundary**: it identifies what must remain operational while changes are made.

For example, a student might notice that the public catalog and Administrator oversight both read Events. Replacing their controllers with a large generic repository may seem tidy, but it risks eliminating the different query eligibility and authorization rules. Visitors see only Published upcoming Events; Administrators intentionally see Events across statuses and owners. A narrow query correction preserves those distinctions. The maintenance cost of a proposed refactor includes newly created failure paths, invalidated contracts, and a broader regression scope.

A practical stabilization decision asks: Which requirement is threatened? Which symptom or static defect demonstrates the risk? What is the smallest correction that restores the contract? Which inherited paths might that change affect? The answer may be code, configuration, documentation, or an additional regression case. This **risk-based change control** preserves the approved MVC/Razor and SQL Server architecture while targeting verified engineering needs.

## Requirements traceability and reproducible evidence

A requirement has several related but distinct representations. `NFR-SEC-001` states that protected operations must be authorized by the server. An `[Authorize]` attribute indicates one implementation mechanism. A protected action that also filters by `OrganizerId` addresses a resource-specific rule. An HTTP test that requests a foreign Event as an ordinary Organizer demonstrates one selected denial path. A deployed browser check confirms that the hosted application exhibits the same denial in its configured environment. Together, these artifacts establish a progressively more complete and reviewable authorization record.

| Evidence level | Engineering question | Representative CEMS observation |
| --- | --- | --- |
| Source/configuration review | Which guards and data paths are implemented? | Controller authorization, form-token filters, bounded query composition. |
| Component/service test | What holds under controlled inputs and database state? | Duplicate Registration and relational constraints. |
| Authenticated HTTP test | How do middleware, identity, routes, and actions interact? | Foreign Organizer request returns an authorized refusal. |
| Browser/manual observation | Can the intended actor complete the workflow? | Keyboard use, validation feedback, retained search criteria. |
| Approved deployment observation | Do the configured host and persistent database support the workflow? | Live login, Registration, restart/persistence, and smoke results. |

A traceability row connects the **requirement**, **implementation artifact**, **procedure**, **observed result**, **execution context**, and **status**. The status communicates which acceptance dimensions the recorded work supports; a reviewer can reproduce the procedure and see precisely which requirement conditions were exercised. Apply the approved acceptance criteria to the complete set of relevant results.

Reproducible evidence records the commit, command or scenario, data volume, clock assumptions, database provider, environment, date, and observed result. Restrict the record to information needed for the claim and redact unnecessary personal data. This makes acceptance decisions auditable and gives a reviewer a clear path to reproduce the stated behavior.

## Failure classification, authorization, and defense in depth

The same application can reject a request without being broken. Consider these outcomes:

| Condition | Correct boundary | Expected result |
| --- | --- | --- |
| Visitor requests a protected capability | Authentication boundary | Login challenge and local return flow when practical |
| Signed-in non-Administrator requests Administrator route | Role authorization | Access Denied, no protected mutation |
| Organizer requests another Organizer's Event | Ownership/resource authorization | Established not-found/denied behavior; no foreign participant disclosure |
| Participant submits a duplicate Registration | Business invariant | Controlled feedback; no second persisted current Registration |
| Request supplies invalid Event fields | Validation boundary | Re-rendered form, useful messages, preserved valid values |
| Unknown Event ID | Resource lookup | HTTP 404 or established not-found page |
| Unexpected application failure | Exception-handling boundary | Generic error response and appropriate server-side diagnostic logging |

These distinctions matter because a broad exception handler should not turn a normal eligibility refusal into a technical 500 response, and a browser-side hidden button must not grant authorization. In CEMS, the effective decision can combine authentication, role, ownership, current Event state, and time. For Organizer oversight, an explicit Administrator-plus-Organizer exception permits an otherwise foreign Event view; ordinary Organizers remain scoped to their own Event IDs. Check the exact SRS allowance rather than generalizing administrator privileges to unrelated workflows.

**Defense in depth** means that separate mechanisms protect separate boundaries: authenticated cookies identify a principal; server-side authorization restricts routes/resources; allowlisted models restrict user-supplied fields; anti-forgery tokens mitigate cross-site request forgery on applicable state-changing forms; and database constraints protect relational integrity even when application pre-checks race. Error pages and production configuration protect information disclosure. These mechanisms complement rather than replace one another. A real security audit verifies both refusal outcomes and unchanged persisted state.

### Independent roles are not independent security authorities

Lesson 8 correctly implements the *current-request* rule for the Organizer registration roster:

```text
authenticated AND Organizer AND (Event owner OR Administrator)
```

An Administrator-only principal is refused. An Organizer can view its own Event's roster. A principal with both Organizer and Administrator can use UC-006's explicit **read** exception for a foreign Event. This valid implementation of **independent, composable role privileges** is not the same as an independently enforced *separation of duties*.

Trace the actual classroom application, not an invented vulnerability: `AdminUsersController` is Administrator-protected; its `GrantOrganizer` command accepts an existing user ID and does **not** forbid targeting the acting Administrator. An Administrator-only user denied `/Events/Registrations/{id}` may grant **its own account** Organizer membership using the administrative user-management workflow. After signing in again so the cookie contains updated role claims, that user holds **both roles** and can view the foreign Event's registration names/emails. This is not a bypass of `[Authorize(Roles = RoleNames.Organizer)]`: the request is authorized **after the principal acquires the missing permission**. It limits the stronger privacy guarantee one might otherwise claim.

| Proposed separation control | What it can prevent | Why it may still fail |
| --- | --- | --- |
| Deny self-grant in `GrantOrganizer` | Direct assignment to the acting account | The Administrator could control another account and use its authority, if privileged account creation, promotion, or credentials are also unrestricted |
| Require another Administrator to approve | Unilateral approval by the same account | Another login is not another independent person; the requester may create, promote, reset, or impersonate it |
| Require two independent approvers | One person acting alone when independent identities and authority are enforced | Compromise/collusion remain possible; the requester must not control approver provisioning, roles, or policy |
| Special role manager/Super-Administrator | Keeps privileged grants outside ordinary Administrator powers | Effective only when ordinary Administrators cannot create/promote that role, reset its credentials, or modify the enforcement mechanism |
| External identity governance | Separates application operations from permission administration | The restricted user must also lack authority over the external provider, deployment code, database permissions, and emergency access |

**Root of trust:** An authorization barrier is not independent if the restricted actor can change the policy, create an approver, give an approver a role, reset or impersonate their credentials, deploy replacement authorization code, or directly modify the role database. Production designs therefore restrict and audit **role assignment and account provisioning**, separate application from security administration, protect deployment/DB control, use independent approvals for sensitive elevation when justified, consider MFA and time-bounded elevation, keep tamper-resistant audit records outside the requester’s control, and define controlled emergency-access procedures. A higher trust authority remains; its powers are constrained, distributed and observed, not eliminated. Two-person control reduces unilateral abuse but cannot guarantee against collusion or compromise.

**Short threat-model exercise:** Compare (i) self-grant, (ii) another-admin approval, (iii) two independent approvers, and (iv) an external identity service. For each, identify the actor allowed to create privileged identities, promote Administrators, reset credentials, alter approval policy, deploy code, and write the role tables. Mark an approach ineffective whenever the requester can control all required approvers directly or indirectly. Then explain why this semester's CEMS slice **documents** that limitation but does **not** implement a Super-Administrator, two-person workflow, or a new privilege system: those would require their own approved requirements, infrastructure, and tests.

## Usability, accessibility, and compatibility as system qualities

Functionally correct HTML can still be difficult or impossible to use. In CEMS, a role-aware menu must make the actor's primary available tasks discoverable (`NFR-USA-001`); rejected form submissions must explain what failed and retain valid values (`NFR-USA-002`); destructive actions must communicate consequence before a deliberate POST (`NFR-USA-003`); and Publish/Register/Cancel outcomes must provide understandable feedback (`NFR-USA-004`). These qualities connect the domain action to the user's understanding of the result.

Semantic structure gives the browser and assistive technology information that visual styling alone does not supply. `<label for="...">` or the equivalent Tag Helper connects a form label to an input; meaningful headings reveal page hierarchy; actual buttons and links offer native keyboard behavior. Search and page-size controls should have accessible names, page controls should expose the current-page state understandably, and a keyboard user should be able to follow links, submit forms, and confirm or decline destructive actions with visible focus.

Combine markup review with rendered interaction: use the keyboard to traverse links, forms, pagination, and confirmation controls in at least two current major desktop browsers. At laptop and mobile viewport widths, inspect long labels, tables, navigation, focus visibility, and action controls for operability and horizontal scrolling. Record browser versions, widths, actions, and outcomes.

## Query efficiency, deterministic retrieval, and performance evidence

The public CEMS catalog has both a **semantic** and a **resource-consumption** contract. Semantically, only Published upcoming Events belong to normal browse results, search and Category filters refine that eligible set, ordering is chronological with a stable ID tie-break, invalid page numbers are normalized, and the selected page size remains 10, 25, or 50. Operationally, `CountAsync()` and the selected `Skip(...).Take(pageSize)` query execute before materialization; this avoids loading the complete matching table into application memory or the browser.

Materialization is the point at which a deferred query is executed and results become concrete objects. Calling `ToListAsync()` before filtering makes the application perform work that belongs in the database, potentially violating `NFR-PER-002`. Applying `Include(...)` deliberately or projecting only necessary fields can also avoid excess round trips and over-fetching. An **N+1** pattern arises when loading N list entries triggers a new related-data query for each entry. It should be diagnosed through actual query behavior, not assumed merely because two or three legitimate queries occur in a Details action.

The two performance requirements examine complementary properties. `NFR-PER-002` concerns database-side filtering, ordering, and bounded page retrieval. The Administrator list now also uses bounded 10/25/50 retrieval under `FR-ADM-003` (a separate all-owner/all-status scope). `NFR-PER-001` specifies approximately two seconds for normal browse and Details pages, up to 500 Events and 2,000 current Registrations, in approved single-user instructor test conditions. Measure repeated requests in that context and record the dataset and method.

## Regression testing and confidence across verification boundaries

Regression testing checks that already accepted behavior survives subsequent change. The CEMS suite has accumulated test cases for catalog eligibility and page boundaries, Identity and authorization, Organizer lifecycle and ownership, Registration eligibility and persistence, transactional capacity, Administrator privileges, and Category constraints. Keep this cumulative coverage rather than reconstructing it during stabilization.

Choose a test boundary according to the behavior under investigation. Relational service tests exercise database uniqueness and duplicate prevention; authenticated HTTP tests exercise cookies, request routing, and authorization responses; browser scenarios exercise usability and task completion. Parameterized methods can produce several discovered test cases. Name the appropriate method, cases, and observed outcomes in the test record.

The final-seat case illustrates complementary concurrency techniques. The inherited automated LocalDB regression exercises competing requests and inspects persisted outcomes; the instructor-controlled SQL Server lock gate deliberately holds database locks to expose the blocked-request schedule. Both relate to the existing transactional Event-row coordination (`UPDLOCK/HOLDLOCK`, authoritative count, capacity decision, insert, commit). Their distinct schedules help explain how transactions protect the shared capacity invariant.

A failed test can reveal an application regression, an unsuitable fixture, a stale assertion, or environment configuration. Diagnosing the cause is part of testing; deleting a failing test or changing its expected result without reconciling the SRS weakens evidence. Reliable test conclusions state the exercised conditions, expected assertion, actual outcome, and relevant fixture assumptions.

## Release engineering, configuration, and deployment correctness

The release lifecycle has several distinct stages. **Build** compiles; **test** evaluates selected claims; **publish** assembles artifacts for hosting; **configure** supplies environment-specific settings and secrets; **migrate** brings a target relational schema into a compatible state; **deploy/start** makes the application execute on the approved host; and **smoke-test** observes essential workflows there. Each stage has a defined output and a corresponding approval or operational handoff.

In CEMS, Development accounts and local SQL Server settings are not production credentials. The approved host must supply its own protected `DefaultConnection`, environment designation, and operational procedure. Migration history is the schema authority. A safe database update targets a verified database with reviewed migrations and backup/recovery preparation. The deployed application's schema and application version must agree, and the runtime needs the host's correct configuration and available dependencies.

These practices transfer to other stacks: the risk lies not in a particular CLI command but in promoting a build across trust and data boundaries. For optional approaches to artifact promotion and rollback, see [Release strategies and delivery automation](#release-strategies-and-delivery-automation). For deeper production schema changes, see [Production schema evolution and data recovery](#production-schema-evolution-and-data-recovery).

## End-to-end architectural reasoning and technical defense

A defensible full-stack explanation follows one request and identifies who owns each decision. For example, a Visitor's search request enters ASP.NET Core routing, reaches the public MVC action, composes a filtered SQL Server query through EF Core, receives bounded Event rows, builds a view-specific model, and returns server-rendered Razor HTML. The browser navigates through ordinary links. A Participant's Registration POST adds the authenticated identity, anti-forgery validation, server-owned decision inputs, temporal eligibility, transactional capacity enforcement, and a confirmation or controlled refusal.

```text
browser request
    → middleware and endpoint routing
    → authentication/authorization boundary
    → action input and current-state checks
    → established service/EF Core data access boundary
    → SQL Server constraints and transactions
    → View Model / Razor or controlled redirect/error
    → browser response and separately recorded evidence
```

For a public read, the MVC action applies eligibility and paging before Razor renders the response. For Registration, the established service owns the SQL Server transaction and capacity decision. Explain each boundary by naming its responsibility, the requirement it supports, and the effect of a missing or misplaced guard. Then connect the observed result to the intended contract. The same reasoning transfers to Project 2, where browser and server responsibilities are distributed differently.

# Verification and Validation

Verification checks whether implementation and integrated behavior meet their contracts; validation checks whether the observed application satisfies the required workflows and intended use. Perform the following on the inherited solution plus the bounded Lesson 9 changes. Keep local, test-database, instructor-controlled, and deployed environments identified separately. Record failures as failures rather than changing an expectation to produce a passing report.

## Automated

Begin with the actual inherited xUnit test project at `tests/CampusEvents.Tests/`. It uses isolated SQL Server relational fixtures and authenticated ASP.NET Core HTTP hosting for appropriate scenarios. Keep relational tests on their inherited SQL Server fixtures, preserve the final-seat transaction design, and include negative-path assertions. The inventory names inherited authored methods and states the parameterized cases defined in their `[InlineData]` attributes. Compare these source-authored cases with the test runner’s discovered cases.

| Test / case | Behavior or boundary | Expected assertion / evidence |
| --- | --- | --- |
| Inherited `Index_Pagination_UsesAllowedSizesAndStablePageBoundaries` (3 parameterized cases) | Allowed 10/25/50 page sizes, deterministic ordering, and first/last page boundaries | Correct totals, page state, selected size, item counts, and tie-breaking order. |
| Inherited `Index_PageNumber_NormalizesAgainstFilteredMatches` (4 parameterized cases) | Missing, zero, negative, and beyond-last page with active search and Category filters | Filtered count and total pages remain correct; current page and returned rows reflect the normalized request. |
| Inherited `Index_NoPublicMatches_NormalizesPageAndReturnsEmptyState` (1 fact case) | Filter finds no public matches and requests a page beyond the available range | Empty result, zero total records/pages, normalized current page 1, selected page size 10, and HTTP 200. |
| Inherited `Index_HandlesMalformedQueryThroughHttp` (3 parameterized cases) | Malformed page/page-size query reaches an HTTP response | Each data case yields its specified status and the corresponding expected rendered outcome. |
| Inherited `Anonymous_ParticipantRequest_RedirectsToLoginWithLocalReturnUrl` | Authentication challenge and local return destination | Anonymous protected request redirects to Login with safe local return target. |
| Inherited `AuthenticatedParticipant_RoleProtectedProbe_IsDenied` | Insufficient role even with authenticated cookie | Protected route refuses access; no privileged data. |
| Inherited `SameUser_CannotRegisterTwice`, `DraftEvent_IsUnavailable`, and `CancelledEvent_IsUnavailable` | Registration uniqueness and eligibility | Duplicate/ineligible attempts create no additional current Registration. |
| Inherited `CompetingRequests_ForLastSeat_OnlyOnePersists` | Coordinated final-seat attempt on SQL Server fixture | One successful Registration, one controlled full result, persisted count no greater than capacity. |
| Inherited `RegisterAndCapacityEdit_CompetingPostsPreserveInvariant` (1 `[Fact]` case) and `RegisterAndCapacityEdit_OrderedWritesPreserveInvariant` (2 parameterized cases) | Concurrent Registration/Capacity edits and both sequential operation orders | Persisted state respects capacity after competing or ordered operations. |
| Inherited `OrganizerOversight_OwnerForeignAndDualRoleScopesAreEnforced` | Owner, foreign Organizer, and Organizer+Administrator exception | Correct authorized projection, foreign refusal, explicit dual-role exception. |
| Inherited `RegistrationLinks_ReflectIndependentRolesAndOwnerScope` | Rendered links for Organizer-only, Administrator-only, dual-role, and direct access | Links align with destination permission for each role; no participant disclosure on refusal. |
| Inherited `AdminEvents_EmptyAndFirstPageBoundaries_AreExplicit` (four theory cases) | 0/1/10/11 Event page boundaries | Correct empty state and pagination links. |
| Inherited `AdminEvents_PaginatesAcrossAllOwnersAndStatuses_WithStablePageState` | 57 mixed-owner/status rows, three page sizes, invalid input, cancellation redirect | Bounded, ordered results and preserved navigation state. |
| Inherited `SeedAsync_IsIdempotentWithEmptyAndPartialDevelopmentData` (three theory cases) | Empty, partial, and existing sample development databases | Five stable seed Event rows without duplication or overwriting existing samples. |
| Inherited `AdminRoutes_RequireAdministrator` (four parameterized cases) | Authenticated Participant requests `/Admin`, `/Admin/Events`, `/AdminUsers`, and `/AdminCategories` | Each authenticated Participant request redirects to `/Account/AccessDenied`. |
| Inherited `AdminCancel_FuturePublishedOtherOwnersEvent_RetainsOrganizerId` | Cross-owner administrative cancellation | Eligible Event becomes Cancelled and keeps its original `OrganizerId`. |
| Inherited `AdminCancel_MissingAntiforgeryTokenIsRejected` | POST without anti-forgery token | HTTP 400 and unchanged persistent Event state. |
| Inherited `OrganizerRole_GrantAndRemoveChangeMembershipOnly` | Narrowly scoped privilege update | Organizer membership changes; identity and unrelated roles remain unchanged. |
| Inherited `CategoryDatabase_UniqueIndexRejectsCaseVariants` and `CategoryDelete_ReferencedRefusedAndUnusedSucceeds` | Database uniqueness and foreign-key deletion restriction | Case-insensitive duplicate refused; referenced Category/Events retained; unused Category removable. |

### Establish the test environment and discover the actual suite

The inherited SQL Server tests use disposable, isolated data and may require LocalDB or the instructor's accepted SQL Server test setup. Confirm that the configured test database is *not* a Development or deployment database containing valuable records. Preserve any fixture-specific environment variables, fixed-clock handling, and connection assumptions documented by Lessons 2–8. Close test-host sessions when no longer in use.

From the solution root, in the terminal:

```cmd
dotnet restore .\CampusEvents.slnx
dotnet build .\CampusEvents.slnx --no-restore
dotnet test .\tests\CampusEvents.Tests\CampusEvents.Tests.csproj --list-tests
dotnet test .\tests\CampusEvents.Tests\CampusEvents.Tests.csproj --no-restore
```

**Expected behavior:** The test runner discovers and executes the inherited fixtures. Record **discovered cases**, **executed cases**, **passed/failed/skipped cases**, and the command outcome from the terminal. Each `[InlineData]` theory case contributes a separate discovered case; retain the authored-method count only when separately relevant.

If a failure occurs, determine whether it is a real application regression, incorrect test data/clock, unavailable SQL Server instance, an unsafe or mismatched database connection, or an outdated test assumption. Inspect the failing assertion and relevant logs without leaking credentials. Rerun the focused case after a justified correction, then rerun the entire project. Preserve the failing-result record until the new actual result supersedes it.

### Retain the controlled last-seat evidence boundary

Use the automated LocalDB competing-request regression to evaluate selected competing outcomes and the final persisted count. For the distinct instructor-controlled SQL Server lock-gate exercise from Lesson 7, retain its original date, environment, test Event, blocked/released requests, and final count when available. Execute any additional contention demonstration only within the instructor-approved isolated test environment; record `NOT EXECUTED` if that separate procedure has not run.

### Verify controlled cross-workflow interleavings

Create focused SQL Server relational tests **in addition to** the inherited last-seat/capacity coverage. Test the actual Lesson 9 code, not just the example pseudocode. A bare `Task.WhenAll` plus assertions is insufficient evidence that a particular lock order occurred; use two separately scoped `ApplicationDbContext` instances/connections, a fixed `TimeProvider`, explicit synchronization gates/hooks around the Event-lock acquisition, and an isolated migrated SQL Server database. If a manual SQL lock gate is used, record the holder transaction, the blocked request, observed block/unblock state, and the released request's persisted outcome. Set a bounded test timeout and always release/rollback in `finally` so a failing test cannot leave locks held.

For each paired operation, run **both orders** (A holds the Event row lock first, then B; reverse the roles) and assert each response **and a fresh committed database read**:

| Concurrent pair | Schedule to force | Required persisted invariant |
| --- | --- | --- |
| Edit changes start / Publish Draft | Edit first vs Publish first | Publish occurs only when locked authoritative Draft/start permits it; no stale publication |
| Edit changes start / Organizer Cancel | Edit first vs Cancel first | Cancel occurs only when Published and not started; original owner retained |
| Edit changes start / Administrator CancelEvent | Edit first vs Admin Cancel first | Same lifecycle rule; Admin route role stays Administrator-only; list page state retained |
| Edit changes start / participant Registration Cancel | Edit first vs Registration Cancel first | The cancellation checks its current owned Registration and locked Event start; a rejected request leaves row present |
| Register vs eligible lifecycle status change | Register first vs cancellation first | New registration only while future Published; final count does not exceed capacity or duplicate a user |

**Suggested synchronization arrangement:** (1) context A starts a transaction and acquires the target Event lock; (2) test signals context B at a deterministic just-before-lock gate, then verifies B attempts the Event-lock query and remains blocked (through a controlled hook/SQL Server lock observation, not elapsed-time guessing); (3) A changes the Event or commits a no-op lock release; (4) release A and await B; (5) verify both HTTP/command outcomes and persisted Event fields/registration rows through a third context. Repeat with B obtaining the lock first. If SQL Server tooling cannot reliably observe a blocked request, label the scenario **NOT EXECUTED** or **INCONCLUSIVE**, rather than claim deterministic coverage.

Capture the test database identity, connection isolation, fixed clock/zone, synchronization point, timeout, state before/after, and observed results. Explain that these tests cover selected interleavings, **not** every possible schedule or proof of linearizability across all code paths. Existing Category FK/unique-index tests and final-seat checks must continue to pass.

## Manual

Work in three phases: establish local hardening and regression behavior using disposable records, evaluate user experience and performance in the specified environments, then validate the instructor-approved deployment. For each scenario, record the actor, database, clock, inputs, expected outcome, actual result, and evidence reference. The scenario matrices provide the complete inventory; the procedures below give additional setup and measurement detail.

### Local hardening and regression

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Generic unexpected-error response | Disposable local non-Development run with verified local SQL connection; inject then remove a temporary `HomeController.Index()` fault, and test `/Home/Error` directly. | HTTP 500 for actual fault **and direct Error route**; generic error page without diagnostic leakage; cleaned source and rebuilt solution. |
| Public catalog eligibility | Visitor; seeded future Published, Draft, and Cancelled Events. Open `/Events`. | Only Published upcoming Events in normal browse; chronological ordering with tie-break; screenshot and known record IDs. |
| Search, filter, and bounded pagination | Enough matching Events for multiple pages; search title/location, select Category, choose 10/25/50, navigate First/Previous/Next/Last. | Criteria and size retained, current page/total indicated, no duplicates or unexpectedly loaded full result set; URLs and request/query evidence. |
| Invalid/out-of-range page and no-results | Request malformed, zero, negative, beyond-last pages and no-match filters. | First/last normalization as SRS §11 specifies, no-results message without page links, criteria preserved; captured paths and page state. |
| Published and Cancelled Details | Open future Published Event and known Cancelled direct URL. | Details include required data/availability; Cancelled record remains visible by direct link and unavailable for registration. |
| Visitor authentication transition | Open Participant route anonymously, authenticate, then logout via protected form. | Login flow with local return when practical; signed-out user cannot reuse protected route; response/redirect trace. |
| Participant Registration and duplicate | Participant on eligible future Published Event with available seat; register, then repeat. | First Registration persists and confirms; duplicate refused with feedback and no extra row. |
| Participant cancellation and persistence | Cancel own future Registration, refresh and revisit; optionally restart/redeploy through approved process. | Registration removed from current count; results persist after refresh and supported restart; before/after rows. |
| Last-seat and capacity guard | Use isolated instructor-controlled test data; normal full-event submission and any separately authorized concurrency demonstration. | Controlled full result, no capacity oversubscription; database count, environment, exact test method recorded. |
| Organizer ownership and lifecycle | Two Organizers owning distinct Events; create/edit Draft, Publish, edit Published, Cancel, delete eligible Draft. | Status/owner remain consistent with rules; direct foreign-ID request refused and no protected mutation. |
| Organizer Registration oversight | Owner reads list; foreign Organizer requests same Event ID; dual-role Organizer+Administrator requests it. | Owner/dual-role exception see correct names/counts; ordinary foreign Organizer receives established refusal; no data leakage. |
| Administrator access, authority, and Event cancellation | Anonymous Visitor and Participant attempt admin URLs; Administrator opens the same routes, lists multi-owner Events, and cancels another owner's future Published Event. | Visitor challenged for login, Participant redirected to Access Denied, Administrator access succeeds; eligible Event Cancelled with original `OrganizerId`; request/SQL evidence. |
| Organizer role grant/removal | Administrator changes `Organizer` membership of existing account; target signs out and in after each. | Fresh login reflects role; unrelated roles/account ID and prior Event owner keys preserved. |
| Category uniqueness and deletion | Administrator attempts case-variant Category name, edit of own unchanged name, in-use deletion, and unused deletion. | Correct validation/uniqueness; referenced Category and Events persist; unused Category may be deleted. |
| Form feedback and protected POSTs | Invalid Event form, capacity below active count, missing anti-forgery token on a safe test POST. | Understandable messages, valid values retained, missing token rejected, persisted rows unchanged. |

### Accessibility, compatibility, and performance

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Navigation and keyboard access | Visitor/Participant/Organizer/Administrator; use Tab, Shift+Tab, Enter, Space through navigation, forms, pagination, and confirmations. | All core tasks possible without mouse-only interaction; meaningful labels, headings, page state, visible focus, operable controls. |
| Responsive and browser coverage | Two current major desktop browsers; representative common mobile and laptop widths. | Core page content usable without horizontal scrolling; record browser versions, widths, and representative screenshot evidence. |
| Requirement-level performance | Up to 500 Events and 2,000 current Registrations; approved single-user instructor environment. Time normal browse and Details with approved tool over repeated requests. | Approximate two-second target evaluated using actual durations, data volume, method, and context; mark pending when unavailable. |

### Approved deployment and persistence

| Scenario | Prerequisites, inputs, and steps | Expected result and evidence |
| --- | --- | --- |
| Deployment startup and schema | Instructor-approved host/database/configuration; publish, approve migration, start application. | Running URL, expected schema, no secret exposure, stable startup; deployment logs and nonsecret configuration record. |
| Deployed smoke and persistence | Live Visitor/Participant/Organizer/Administrator demonstration and one reversible mutation followed by refresh/restart where permitted. | Correct workflows, authorization, feedback, persisted change, and unchanged protected data; timestamped URL/actor/evidence. |

### Verify the production-safe unexpected-error boundary

Use a disposable local application run and an isolated local SQL Server database. **Before introducing a temporary fault or starting the application**, inspect machine/user environment settings and choose a connection string that explicitly targets a disposable *local* database. Confirm that the connection points to this database and that the run has no production credentials, sensitive seed accounts, or production deployment URL. The following example uses LocalDB when installed; substitute an equally isolated local SQL Server test connection if appropriate:

```cmd
set "ConnectionStrings__DefaultConnection=Server=(localdb)\MSSQLLocalDB;Database=CemsL9Err;Trusted_Connection=True"
```

In the local working copy, temporarily add the following statement at the start of `HomeController.Index()` so the Home request triggers an unexpected exception without writing data:

```csharp
// Temporary fault for a disposable local run.
throw new InvalidOperationException("Temporary Lesson 9 fault for verification");
```

Start from the solution root in a local terminal, explicitly selecting the non-Development environment and bypassing launch profiles. Confirm that the connection string still names the disposable local database:

```cmd
set "ConnectionStrings__DefaultConnection=Server=(localdb)\MSSQLLocalDB;Database=CemsL9Err;Trusted_Connection=True"
set ASPNETCORE_ENVIRONMENT=Production
set DOTNET_ENVIRONMENT=Production
dotnet run --project .\src\CampusEvents\CampusEvents.csproj --no-launch-profile
```

Request the Home page at the displayed local URL. In browser developer tools or an approved HTTP client, inspect the **HTTP 500** response, neutral heading, recovery link, and absence of exception text, stack details, SQL, paths, and connection information. Observe normal Access Denied and form-validation behavior through separate safe requests.

Stop the local process, remove the temporary throw, and clear the environment variables in every terminal where they were set. Then rebuild and inspect the controller diff from the solution root:

```cmd
set ASPNETCORE_ENVIRONMENT=
set DOTNET_ENVIRONMENT=
set ConnectionStrings__DefaultConnection=
dotnet build .\CampusEvents.slnx
git diff -- src/CampusEvents/Controllers/HomeController.cs
```

Confirm the source contains no temporary fault and inspect the staged diff before committing or publishing. Record the execution date, safe local environment, response status/content, and cleanup result in the traceability file.

### Review usability, accessibility, and browser compatibility

Move through the matrix as a user rather than a source-code reader. Check primary links in each actor's navigation, a meaningful page heading, associated labels for every sampled form control, visible validation summaries/field messages, and keyboard-usable confirmation forms. Confirm that destructive actions occur only after a deliberate POST, not on GET or ordinary navigation. Check the page's current-number/total state and navigation for the chosen page size. Inspect longer content and administration tables at representative narrow widths; confirm neither content nor required action controls become unreachable.

For `NFR-COM-001`, record **at least two major desktop browsers** and the actual versions used. For `NFR-COM-002`, record the approximate tested viewport widths and whether normal content requires horizontal scrolling. Exercise both selected browsers independently as well as the representative viewport widths.

> **Screenshot placeholder — public pagination and preserved criteria**
>
> **Illustration needed:** Browser catalog with selected Category and search input, page size 25, current/total page state, and a Last/Previous navigation example.
>
> **Expected visible result:** Preserved criteria, readable paging state and visible keyboard focus on an available navigation control.
>
> **Application state:** Development or approved deployment seeded with sufficient matching Published future Events; selected page size 25.
>
> ![Expected public Event catalog with preserved filter, page size, and accessible paging controls](images/lesson-09/pagination-state.png)

### Measure the performance contract in its specified context

`NFR-PER-001` specifies an approximate two-second target under the approved scale and environment. Use the instructor-approved dataset and method. Record the relevant Event and current Registration counts, single-user condition, environment, browser/tool version, URL, repeated durations, and any warm-up or server startup effect. Treat browse and Details as separate measurements. Compare observed typical results against **approximately two seconds**; do not rewrite the SRS target into a different percentile or guarantee.

| Measurement context | Actual value |
| --- | --- |
| Approved environment and date | Pending |
| Event/current Registration counts | Pending |
| Browser or timing instrument | Pending |
| Browse URL and repeated request durations | NOT EXECUTED |
| Details URL and repeated request durations | NOT EXECUTED |
| Interpretation, caveats, and evidence location | Pending |

Record the query-composition review under `NFR-PER-002` and the measured durations, dataset, and environment under `NFR-PER-001`.

### Deploy and complete the production smoke test

After the instructor approves the host, migration method, and configuration mechanism, follow the documented sequence from Design and Coding. Record the **actual** host and URL, date/commit/release artifact, schema state, secret-setting method (not values), and startup outcome. Stop on configuration/migration failure before exercising protected mutations. Do not seed classroom privileged credentials into production by assumption.

Use the **Approved deployment and persistence** matrix together with the relevant local role scenarios. Before testing mutations, confirm the intended accounts and disposable Event/Category records belong to this host; record the live URL, application commit, and current schema. Exercise Visitor, Participant, Organizer, and Administrator requests with distinct accounts and verify the server's role and resource decisions through direct routes as well as rendered navigation.

For each approved mutation, record the actor, target record ID, request outcome, and before/after persisted values. Include a controlled refusal that preserves the protected state, then restore disposable data through permitted application commands. Observe a **persisted** mutation across refresh; when the environment supports a normal restart or redeploy without data loss, recheck that same row afterward for `AC-008`. Run the separate forced SQL Server contention demonstration only in an instructor-approved isolated test environment. Record each executed scenario and its result in the traceability record.

A non-secret deployment evidence record can be maintained as:

| Deployment field | Observed value |
| --- | --- |
| Instructor-approved host and environment | Pending |
| Live URL | Pending |
| Application commit and Release artifact | Pending |
| Target schema/migration check | NOT EXECUTED |
| Startup and smoke-test date | NOT EXECUTED |
| Representative actor workflows and persistent row evidence | NOT EXECUTED |
| Blocking issues or follow-up | Pending |

### Rehearse the midterm demonstration and architectural defense

Prepare an intentional demonstration using the approved accounts, disposable Events, and reproducible scenarios in the manual matrix. Select representative workflows rather than repeating every row. Arrange the presentation around the decisions that explain the application's behavior:

1. **Present the user journey:** Open the filtered public catalog, then authenticate as a Participant and follow one Registration from form submission through stored state and visible availability. Explain the routing, model, query, and transactional boundaries at the point they appear.
2. **Contrast authorization scopes:** Use the Organizer and Administrator scenarios to show resource ownership, the explicit dual-role exception, Administrator-wide oversight, and retained `OrganizerId` after administrative cancellation. Name the trusted server decision for each case.
3. **Explain quality and refusal behavior:** Demonstrate one controlled validation/authorization refusal. Use the recorded isolated non-Development error check to explain the separate unexpected-failure path. Show how a regression case corresponds to the behavior under discussion.
4. **Present release accountability:** Navigate the root README, Release procedure, and traceability record. Identify the actual test execution summary, host configuration boundary, deployed URL and migration state when available, with dates and environments corresponding to the records.

Keep the presentation focused on requirements, visible user behavior, persisted changes, and architectural responsibilities. Use the completed matrix to choose data, account identities, expected outcomes, and safe reversal/cleanup steps for the demonstration.

## SRS traceability matrix update with evidence

Reconcile the complete project state against the approved SRS. Preserve prior valid Lesson 1–8 evidence with its true source/date; append this lesson's observed results as separate records. Use the matrix as the working template, updating each row with its executed procedure, environment, result, and justified status.

| Requirement / Reference | Implementation / Artifact | Verification / Evidence | Status | Notes |
| --- | --- | --- | --- | --- |
| `UC-001`, `FR-SRH-001`–`FR-SRH-009`, `AC-001` | Public `EventsController`, `EventSearchViewModel`, Index/Details Razor, SQL query | Inherited catalog tests; final filter, 10/25/50, boundary and deployed checks: NOT EXECUTED in Lesson 9 | PARTIAL | Reconcile existing observed tests and live browse evidence; do not replace paging with fixed ten rows. |
| `UC-002`, `FR-ACC-001`–`FR-ACC-007`, `AC-002` | Identity controllers, login/registration/Logout, cookie configuration | Inherited Identity HTTP tests; actual cumulative run and deployed login: NOT EXECUTED | PARTIAL | Use generic invalid-login messages and local return behavior. |
| `UC-005`, `FR-EVT-001`–`FR-EVT-011`, `BR-EVT-001`–`BR-EVT-010`, `AC-003` | Event forms, owner-scoped actions, lifecycle transitions | Inherited lifecycle tests; final browser/deployment scenarios: NOT EXECUTED | PARTIAL | Preserve eligible Edit, Publish, Cancel, Draft delete, and time rules. |
| `UC-003`, `UC-004`, `FR-REG-001`–`FR-REG-008`, `AC-004` | Registration service/controller, My Registrations | Inherited service/HTTP tests; final refresh and live test: NOT EXECUTED | PARTIAL | Includes duplicate, status, start-time, and cancellation refusal. |
| `BR-REG-002`–`BR-REG-004`, `BR-EVT-009`, `BR-CAT-001`–`BR-CAT-002`, `AC-005` | SQL Server constraints, Registration transaction, Category migration and guard | Existing relational tests; final execution: NOT EXECUTED | PARTIAL | Record separate forced-contention evidence if performed. |
| `UC-006`, `FR-REG-009`, `FR-REG-010`, `AC-006` | Organizer Registration view and authorized Event-first query | Owner/foreign/dual-role HTTP test; final browser checks: NOT EXECUTED | PARTIAL | Explicit SRS dual-role exception. |
| `UC-007`, `UC-008`, `FR-ADM-001`–`FR-ADM-007`, `AC-007` | Administrator Events, user-role, and Category modules | Existing admin tests; final deployed checks: NOT EXECUTED | PARTIAL | Confirm case-insensitive uniqueness, role refresh, owner retained. |
| `NFR-REL-001`, `AC-008` | EF Core persisted state and migration history | Refresh/restart or approved redeploy persistence: NOT EXECUTED | PARTIAL | Record the database-backed result across refresh or approved restart. |
| SRS §11, `NFR-SEC-005`, `AC-009` | Controlled form/business/authorization responses; shared Error view/handler | Negative tests and non-Development fault check: NOT EXECUTED | PARTIAL | Separate expected refusals from unexpected exceptions. |
| `AC-010` | `tests/CampusEvents.Tests/` cumulative critical tests | Full test run; discovered case and pass/fail counts: NOT EXECUTED | IMPLEMENTED—UNVERIFIED | Actual execution mandatory; include capacity and authorization-sensitive cases. |
| `NFR-SEC-001` | Role and owner guards on protected endpoints | Direct HTTP and browser role/owner tests: NOT EXECUTED | IMPLEMENTED—UNVERIFIED | Enforce server role and ownership checks on every protected route. |
| `NFR-SEC-002` | ASP.NET Core Identity password handling | Framework/config review and authentication tests: NOT EXECUTED | IMPLEMENTED—UNVERIFIED | No plaintext password persistence. |
| `NFR-SEC-003` | Anti-forgery form/tag-helper validation | Missing-token mutation tests/audit: NOT EXECUTED | IMPLEMENTED—UNVERIFIED | Check *all* applicable state-changing forms. |
| `NFR-SEC-004` | User secrets, ignored local configuration, host-provided production settings | Repository and deployment secret review: NOT EXECUTED | PARTIAL | No credential values in source control/evidence. |
| `NFR-PER-001` | Normal public browse/Details routes | Instructor dataset up to 500 Events/2,000 Registrations; elapsed measurements: NOT EXECUTED | PARTIAL | Approximately two seconds under specified single-user conditions. |
| `NFR-PER-002` | Database-composed filtered/paged public query | Query review and SQL/HTTP observation: NOT EXECUTED | IMPLEMENTED—UNVERIFIED | Page size 10/25/50, filtering before materialization. |
| `NFR-USA-001`–`NFR-USA-004` | Role navigation, validation and operation feedback, confirmation forms | Actor/browser task matrix: NOT EXECUTED | PARTIAL | Observe actual usability and clear feedback. |
| `NFR-ACC-001`–`NFR-ACC-003` | Semantic Razor labels/headings and paging controls | Markup + keyboard focus/operation checks: NOT EXECUTED | PARTIAL | Include public page state and keyboard pagination. |
| `NFR-REL-002`, `NFR-REL-003` | Validation/authorization guards and transactional capacity | SQL/HTTP negative cases and final-seat evidence: NOT EXECUTED | PARTIAL | Rejected changes must be atomic; count must not exceed capacity. |
| `NFR-COM-001`, `NFR-COM-002` | Razor responsive pages and project-owned CSS | Two browsers and mobile/laptop viewport check: NOT EXECUTED | PARTIAL | Record versions and widths. |
| `AC-011` | Security, usability, accessibility, reliability, compatibility checks above | Final combined NFR results: NOT EXECUTED | PARTIAL | Do not mark met while required evidence remains missing. |
| `AC-012` | Release artifact and instructor-approved deployment | Host URL, migration, startup, role smoke checks: NOT EXECUTED | PARTIAL | Complete startup and observed workflows on the approved host. |
| `AC-013` | Root `README.md`, repository layout and test/database instructions | README readability, commands/path review: NOT EXECUTED | IMPLEMENTED—UNVERIFIED | Validate actual prerequisites, TargetFramework, and setup steps. |

**Evidence context:** Run `dotnet build` and `dotnet test`, record the test results, and compare the manual inspector output with the expected scenarios. Both are necessary to evidence that requirements have been implemented and verified.

Confirm that your project builds, passes all tests, and runs without errors.

Stage and commit:

```cmd
git add .
git commit -m "Lesson 09: Implemented and Verified"
```

**NOTE:** Never stage or commit source code that does not build, fails a test, or runs with errors.

# Exercises

## Exercise: diagnose and repair an authorization regression

**Purpose and expected outcome:** Practice risk-based corrective maintenance by identifying a server-side authorization regression, implementing a minimal fix, and demonstrating that the correction preserves both the denial path and authorized behavior. This reinforces objectives 1, 2, 3, and 5.

**Prerequisites and SRS ties:** Begin from the accepted Lesson 8 checkpoint and a reviewed working branch or commit, following the established repository workflow. Use an instructor-supplied or locally reproduced **disposable** authorization regression that removes a server guard from one Organizer-owned or Administrator-only operation, never the production deployment. Relevant references include `BR-AUT-002`–`BR-AUT-004`, `NFR-SEC-001`, `AC-005`–`AC-007`, and `AC-009`.

**Task:** Inspect the supplied changed route or action, determine which role/resource boundary is missing, and reproduce the defect with an authorized actor and an actor who must be refused. Correct the narrow guard at the server boundary; preserve existing endpoints, validation, identity, and ownership decisions. Restore expected refusal semantics without exposing foreign data or modifying unrelated workflows.

**Hints or checkpoints:** Start from a direct URL or POST, not the navigation link. Compare the accepted controller guard to the source at the failing commit. A role check and a row-ownership filter answer separate authorization questions. Check whether the current request would still be denied if an unavailable button were manually submitted. Verify unauthorized requests leave the database unchanged.

**Deliverable:** A small reviewed diff; a source/requirement explanation identifying the lost boundary; one focused automated HTTP regression or extension to the existing appropriate test; and a short record of initial failure, correction, and observed result.

**Completion and verification:** Execute the focused test and the cumulative suite, then reproduce both the authorized success and the unauthorized denial with authenticated requests. Record actual method/case discovery and results, response behavior, and unchanged protected state. If the fault cannot be reproduced or the suite cannot run, describe the specific obstacle and do not claim a pass.

**Challenge (optional):** Explain whether replacing the route's response with an application-wide generic 500 page would meet the SRS and why it would be an incorrect repair for an expected authorization refusal.

# Troubleshooting Common Problems

| Symptom | Likely cause / diagnosis | Correction / verification |
| --- | --- | --- |
| Baseline build fails before Lesson 9 edits | Wrong solution root, missing SDK/restore, stale local changes | Confirm `CampusEvents.slnx`, inspect TargetFramework, restore/build from solution root, compare to starting commit. |
| Error handler returns another exception | `Error()` action/view model mismatch, missing view, inappropriate verb/authorization restriction | Reuse `Views/Shared/Error.cshtml`, pass its expected `ErrorViewModel`, keep safe action reachable for re-executed requests; verify HTTP 500 without sensitive details. |
| Error page exposes fault text | Development profile used or technical fields rendered in Razor | Start isolated local non-Development without launch profile; inspect handler and view; remove temporary fault afterward. |
| Expected Access Denied becomes generic 500 | Ordinary authorization refusal incorrectly thrown as an exception | Restore normal Identity/authorization result; keep exception path for unexpected failures only. |
| Non-Administrator can reach admin POST by URL | Missing server authorization or validation on a state-changing action | Restore server role guard and anti-forgery; exercise direct HTTP refusal and unchanged database rows. |
| Hidden action can be invoked anyway | UI visibility incorrectly treated as access control | Apply server role/ownership/state guard to the action itself and test forged requests. |
| A legitimate form submission unexpectedly returns HTTP 400 | Missing/invalid request-verification token, incorrectly configured form, or mismatched anti-forgery settings | Inspect form Tag Helpers and server validation; submit the expected token with the normal POST; retain deliberate missing-token tests as expected HTTP 400 cases. |
| Test suite cannot connect to SQL Server | LocalDB unavailable, incorrect disposable test connection, missing migration | Confirm inherited fixture configuration and test database; never redirect tests to a valuable application database. |
| Full test run reports fewer/more cases than anticipated | Parameterized theory cases, filters, skipped cases, or discovery differences | Run `--list-tests`, examine actual case results, and distinguish methods from individual executions. |
| A final-seat regression unexpectedly fails | Changed fixture data/clock, lost transaction/lock coordination, or real race | Inspect both request outcomes and persisted count; compare inherited service logic before modifying assertions. |
| Public pagination displays wrong results | Hard-coded `Take(10)`, premature materialization, dropped filter/page-size parameters, unstable sort | Preserve 10/25/50 and count/filter/order/Skip/Take; verify First/Last, empty and out-of-range semantics. |
| Performance timing appears inconsistent | Unrepresentative seed data, start-up warm-up, different host, timing method | Measure repeatedly under the stated instructor-approved dataset and environment; report conditions and variation. |
| Release succeeds but deployed app fails | Missing `DefaultConnection`, migration mismatch, runtime version, provider startup configuration | Check approved host configuration, target database migration state, logs with redaction, and artifact compatibility. |
| Production data disappears after deployment | Wrong database target or destructive schema/database reset | Stop deployment, follow instructor's recovery procedure, confirm database identity and backup before future migrations. |
| README commands fail from repository root | Prior lesson paths incorrectly assumed sibling projects | Use `src/CampusEvents/` and `tests/CampusEvents.Tests/`; document command working directory and prerequisites. |
| Recorded traceability status conflicts with dated execution results | Test execution context, requirement scope, or status was entered incorrectly | Compare each status with the recorded command/scenario, date, environment, and outcome; correct the row to match observed results. |

# Checking Your Understanding

<details><summary>Why might a small corrective patch be safer than a broad architectural refactor immediately before acceptance?</summary>

A narrow patch affects fewer existing contracts, keeps the inherited application more stable, and requires a smaller regression scope. A larger refactor is justified only when its benefits and verification cost address a material requirement or diagnosed defect.

</details>

<details><summary>Design the server-side checks needed when an Organizer requests the participant list for a supplied Event ID.</summary>

Require the appropriate authenticated role, load the requested Event from persisted data, and compare its trusted `OrganizerId` with the signed-in user. Apply the SRS's explicit Organizer-plus-Administrator exception when present. Scope the participant projection to the authorized Event and return the established refusal for an ordinary foreign Organizer. Exercise owner, foreign, and dual-role requests through direct HTTP cases.

</details>

<details><summary>How should the application respond when a Category is referenced by Events or a Participant repeats a Registration?</summary>

Treat both as expected business-rule refusals. Return controlled feedback with the established response semantics and keep referenced Category, Event, and current Registration rows unchanged. Use the generic unexpected-error route for unhandled application faults such as unexpected infrastructure failures.

</details>

<details><summary>What evidence differentiates `NFR-PER-002` from `NFR-PER-001`?</summary>

`NFR-PER-002` concerns query structure and bounded database-side search/filter/page retrieval before materialization; inspect code and query/HTTP behavior. `NFR-PER-001` concerns measured normal browse/Details duration against a specified approximate target, representative data volume, single-user conditions, and instructor-approved environment.

</details>

<details><summary>How do the automated final-seat regression and instructor-controlled SQL Server lock gate examine capacity reliability differently?</summary>

The automated relational regression sends selected competing requests and checks both responses and final persisted count. The controlled lock gate holds an Event row lock to expose a blocked-request schedule before releasing it, then checks the final outcome. Together, the two procedures examine selected concurrency paths of the same database-enforced capacity invariant.

</details>

<details><summary>What information should you record when evaluating `AC-010` using the inherited automated suite?</summary>

Record the executed command, date, environment and fixture, discovered test cases, executed and passed/failed/skipped cases, and the critical eligibility/capacity and authorization-sensitive scenarios covered. Compare the observed results with the acceptance criterion and record its supported status.

</details>

<details><summary>Describe the complete release and deployment sequence required to evaluate `AC-012`.</summary>

Build and publish the intended application revision, configure the instructor-approved host with protected settings, verify and apply the approved target database migration, start the application, and exercise the required live end-to-end workflows. Record the version/commit, deployed URL, date, schema state, scenarios, and observed outcomes in the traceability record.

</details>

<details><summary>How would you verify security for a protected operation that is hidden from an ordinary Participant's navigation?</summary>

Submit a direct authenticated request to the protected endpoint as that Participant, observe the expected Access Denied outcome, and check the persisted state for unchanged protected data. Also exercise the permitted actor to confirm that the same operation remains available with the appropriate server-side authorization.

</details>

<details><summary>What sequence would you perform to evaluate persistence across refresh and an approved application restart?</summary>

Create a known valid database-backed change with disposable data, record the affected row, refresh and revisit the page, then use the approved restart procedure and inspect the same row again. Record the database identity, application version, environment, dates, and resulting state.

</details>

<details><summary>Trace the key boundaries for an eligible Participant Registration POST and identify a useful negative test.</summary>

The request passes authentication and anti-forgery, receives trusted user identity, checks the current Event's Published/upcoming status and duplicate state, and executes the inherited atomic capacity decision against SQL Server. The response confirms success or provides a controlled refusal; the authoritative Registration rows determine availability. A competing final-seat attempt should leave at most `Capacity` current rows, with the rejected request creating no partial write.

</details>

# Summary

Final-system engineering requires more than completing the visible feature list. You practiced assessing an implementation against a stable requirements contract, selecting narrow risk-based corrections, and documenting requirements through reproducible engineering work. This approach improves maintainability and makes acceptance decisions reproducible.

You analyzed security as a set of complementary boundaries—identity, authorization, ownership, request validation, protected state changes, and error disclosure—and evaluated quality through semantics, keyboard interaction, compatibility, bounded retrieval, and performance measurement. You also chose among relational, authenticated HTTP, concurrency, browser, and deployed-environment checks according to the specific behavior under evaluation.

A reproducible delivery is a sequence of controlled transitions from source to artifact, configured runtime, compatible persistent data, and observed operational behavior. Reasoning through those transitions, explaining end-to-end responsibilities, and documenting what remains unverified are transferable practices for future systems regardless of their rendering model, programming language, or deployment provider.

# Checkpoint

**Inherited baseline:** At the conclusion of Lesson 8, CEMS uses ASP.NET Core MVC/Razor, Identity, EF Core/SQL Server, and a fixed-clock convention with public filtered 10/25/50 pagination; Participant Registration and SQL Server capacity coordination; Organizer-owned Event/lifecycle and Registration oversight; and Administrator Event, role, and Category management. The Lessons 1–8 baseline includes a paginated all-owner/all-status Administrator Event list, corrected dual-role registration links, and an isolated xUnit project with relational/authenticated HTTP coverage. Lesson 9 adds independent-authority analysis and coordinated cross-workflow mutation tests; **these Lesson 9 changes are not already implemented in the starting repository**.

**New in this lesson:**

- Reviewed the inherited project and preserved its domain, authorization, pagination, transaction, and repository boundaries.
- Reconciled the non-Development `/Home/Error` path with the inherited shared Error view and improved its safe user-facing handling where needed.
- Maintained `docs/Project1-Traceability.md` with requirement-to-artifact references, status fields, and execution-context records.
- Extended the root `README.md` to document the actual solution layout, Development prerequisites, database migrations, testing, release handoff, and production configuration boundaries.
- Added a reproducible Release publication command and documented the instructor-approved deployment, configuration, and migration handoff.
- Prepared a bounded hardening gap register covering protected routes, data queries, browser quality, and operational configuration.

**Resulting baseline for next lesson:** CEMS retains its accepted MVC/Razor and SQL Server workflows, a hardened generic-error response, an updated root README, and a requirements/evidence record. The repository includes a documented Release publication procedure and approved-environment deployment handoff, alongside source code, setup instructions, and traceability records. This stable full-stack project provides a baseline for the next project's different client/server architecture.

# Next Lesson Preview

Project 2 introduces the Campus Student Organizations Portal, whose approved SRS requires a client-rendered SPA with a separate Web API, authentication boundaries suitable for browser and non-browser clients, organization membership workflows, and synchronization concerns. The requirements-driven design, layered authorization, relational correctness, repeatable tests, and evidence discipline practiced in CEMS carry forward while rendering, navigation, and client/server responsibility boundaries change.

# Appendix

## Release strategies and delivery automation

A CEMS Release publication creates a versioned bundle of application assets for review and promotion. Deployment combines that artifact with host configuration, compatible persistent data, application startup, and live validation. Teams can automate these stages while maintaining clear approvals and traceable artifact versions.

### Manual release, continuous delivery, and continuous deployment

A **manual release** may use a checked build, reviewed migration procedure, and operator-triggered deployment. Its strengths are direct control and transparent approval; its costs include repetitive labor and inconsistent execution when steps are undocumented. **Continuous integration** automatically builds/tests proposed changes. **Continuous delivery** keeps a releasable artifact ready while deployment can still require approval. **Continuous deployment** automatically promotes qualifying changes into a target environment. These concepts describe different process stages; continuous integration does not imply automatic production deployment.

| Approach | Typical control | Benefit | Principal trade-off |
| --- | --- | --- | --- |
| Manual reviewed promotion | Operator follows approved checklist and verifies target | Simple to understand; strong human gate | Repetition and possible procedural drift |
| CI build and test | Pipeline executes tests for changes | Early regression feedback; repeatability | Quality depends on meaningful tests and correct fixture isolation |
| Continuous delivery | Approved artifact available for promotion | Predictable handoff, reproducible versions | Still requires environment management and deployment controls |
| Continuous deployment | Automated promotion on configured gates | Low manual release friction | Faster propagation of unnoticed defects if gates/rollback are weak |

For a small, bounded application, a reviewed Release artifact and approved deployment checklist can offer an appropriate balance of simplicity, safety, and repeatability. With frequent releases or multiple environments, automated builds, tests, artifact promotion, and approval gates can reduce procedural drift.

### Immutable artifacts, rollout patterns, and rollback

An **immutable artifact** is built once, identified by a version/commit, and promoted between environments rather than being rebuilt with different source on each machine. Its configuration remains external, allowing the same code to run against different appropriately secured databases. This strengthens provenance: the deployed version can be traced to the code and tests that produced it.

A simple stop/start deployment temporarily replaces the running version. A **rolling deployment** updates instances in stages; **blue/green deployment** prepares an alternate deployment and switches traffic after checks. A **canary** exposes a new version to a small subset of users before wider rollout. These strategies matter mainly when availability, traffic volume, or blast radius justifies their complexity. They can also be combined with automated quality gates rather than viewed as completely different types of systems.

Application rollback is easier when schema changes remain backward compatible. Rolling back a binary does not necessarily undo a destructive database migration. For CEMS, the critical lesson is to know the current artifact, current schema, saved data, and instructor-approved recovery mechanism before deployment.

## Production schema evolution and data recovery

CEMS reaches a database migration boundary when its Release artifact runs against the target SQL Server database. The development migrations created by earlier lessons describe schema history, including Registration integrity and Category uniqueness; their safe application requires more than issuing an update command.

### Reviewable migrations and environment identity

A migration script can be reviewed for the tables, constraints, indexes, and data transformations it changes. An operator must confirm the target database, expected starting migration, desired ending schema, and whether deployment credentials possess only the privileges needed. Some teams use pre-generated SQL scripts or migration bundles so application runtime accounts cannot silently change the schema. Others allow controlled CLI execution. Both can be valid when operational responsibilities are explicit and the resulting database version is verified.

A backup is useful only if its restoration process is known. Database recovery plans consider recent writes, recovery-point objectives, restore permissions, and whether a partially applied migration has left the schema in an intermediate condition. A destructive `DROP DATABASE` is not a recovery strategy for a persistent user-facing system.

### Expand/contract changes and compatible rollouts

Consider renaming an Event column while an old application instance might still be active. A one-step destructive rename can break that instance. An **expand/contract** sequence instead adds a compatible new column, temporarily reads/writes compatible representations, backfills data, switches the application, and removes the obsolete column only after old code no longer depends on it. The stages add complexity but help when old and new versions can overlap during deployment.

| Schema technique | Useful condition | Cost or hazard |
| --- | --- | --- |
| Direct reviewed migration | One coordinated application/schema switch with controlled downtime | Needs verified database target, backup, and strict rollout sequence |
| Idempotent deployment script/bundle | Repeatable operator-controlled promotion | Requires inspection of generated SQL and credentials |
| Expand/contract | Overlapping app versions or near-zero-downtime rollout | Intermediate data mapping and cleanup increase work |
| Backup and restore | Disaster recovery from bad data/schema state | Recovery time and potential loss of changes since backup |

Select a migration strategy according to deployment overlap, tolerance for downtime, database value, and recovery requirements. For CEMS, the approved migration gate protects application/schema compatibility and persistent Event, Category, and Registration data.

## Production observability and advanced performance diagnosis

CEMS exposes browser responses, server-side requests, and SQL Server data operations. **Observability** links these boundaries through diagnostic signals that help identify a failure's cause, locate request latency, and correlate an error with its server-side context while protecting sensitive data.

### Logs, metrics, traces, and correlation

**Logs** describe discrete events and failures; structured logs attach searchable fields such as request ID, route, severity, or safe domain identifiers. **Metrics** summarize aggregate behavior—request counts, error rates, latency distributions, connection usage. **Traces** follow a request across components and time spans. Correlation IDs connect a generic browser error to server-side diagnostics without putting stack traces or connection details on the page. Protect log access and redact passwords, session cookies, connection strings, and unnecessary participant details.

| Signal | Example CEMS diagnostic question | Limitation |
| --- | --- | --- |
| Structured error log | Which route and request reference produced the generic error? | Must redact credentials and private data |
| Request latency metric | Is browse performance typically slow under realistic load? | Aggregate data does not identify a slow SQL statement on its own |
| Database query trace/plan | Does the selected page query scan or sort too many rows? | A plan may differ with query parameters, indexes, and data distribution |
| End-to-end trace | Is time spent in authentication, SQL, rendering, or network I/O? | Instrumentation adds cost and may not include every dependency |

### Latency distributions, query plans, and conditional optimization

The SRS acceptance test examines approximately two seconds for a specified single-user workload. Production performance engineering can additionally study median and high-percentile latencies, concurrent loads, and warm/cold request behavior. Choose the measure according to the operational question while retaining the SRS target for CEMS acceptance.

An index on filtering or ordering columns may reduce the work needed for a paginated query, but it also consumes storage and can slow writes. Execution plans help determine whether an index is actually useful. Caching can reduce repeated database reads, but cached Event status or remaining seats risk becoming stale just when capacity and cancellation decisions matter. A cache suitable for display is not automatically authoritative for a write invariant. Measure first, identify the actual bottleneck, and preserve server/database correctness while considering optimization.

Safe correlation, representative measurement, and database-level diagnosis form an operational improvement loop: observe a symptom, locate its responsible boundary, apply a bounded correction, and measure the resulting behavior.
