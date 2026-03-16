namespace BlazorApp.Features.Demo.DTOs;

public class NPlusOneResponse
{
    public long ExecutionTimeMs { get; set; }
    public int SqlCount { get; set; }
    public int DataSize { get; set; }
    public int RowCount { get; set; }
    public List<UserWithDepartment> Data { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class UserWithDepartment
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DepartmentInfo Department { get; set; } = new();
}

public class DepartmentInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
