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