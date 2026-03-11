namespace BlazorApp.Features.Demo.DTOs;

public class FullScanResponse
{
    public long ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public bool HasIndex { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<LargeUserInfo> Data { get; set; } = new();
}

public class LargeUserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class SetupResponse
{
    public bool Success { get; set; }
    public int RowCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public string Message { get; set; } = string.Empty;
}
