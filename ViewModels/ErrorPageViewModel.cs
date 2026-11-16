namespace CampusEvents.ViewModels;

public class ErrorPageViewModel
{
	public string Title { get; set; } = string.Empty;

	public string Message { get; set; } = string.Empty;

	public string? ReturnUrl { get; set; }

	public string? ReturnLabel { get; set; }
}