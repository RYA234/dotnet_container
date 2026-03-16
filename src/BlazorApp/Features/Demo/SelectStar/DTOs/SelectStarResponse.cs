namespace BlazorApp.Features.Demo.DTOs;

public class SelectStarResponse
{
    public long ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public long DataSize { get; set; }
    public string DataSizeLabel { get; set; } = "";
    public double AwsCostEstimate { get; set; }
    public string Sql { get; set; } = "";
    public string Message { get; set; } = "";
    public object Data { get; set; } = new();
}

public class ProfileFull
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Bio { get; set; } = "";
    public string Preferences { get; set; } = "";
    public string ActivityLog { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class ProfileSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
