namespace BlazorApp.Features.Supabase.Services;

public interface ISupabaseService
{
    Task<SupabaseConnectionTestResult> TestConnectionAsync();
}

public class SupabaseConnectionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Result { get; set; }
    public string? Error { get; set; }
}
