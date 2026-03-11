namespace BlazorApp.Features.Demo.DTOs;

public class LikeSearchResponse
{
    public long ExecutionTimeMs { get; set; }
    public int RowCount { get; set; }
    public bool UsesIndex { get; set; }
    public string SearchType { get; set; } = "";
    public string Sql { get; set; } = "";
    public string Keyword { get; set; } = "";
    public string Message { get; set; } = "";
    public List<SearchUserInfo> Data { get; set; } = new();
}

public class SearchUserInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
