namespace BlazorApp.Shared.Exceptions;

/// <summary>
/// 指定されたリソースが存在しない場合にスローする例外。
/// HTTPステータス: 404 Not Found
/// </summary>
public class NotFoundException : ApplicationException
{
    /// <summary>見つからなかったリソースの種類（例: "User", "Order"）</summary>
    public string ResourceType { get; }

    /// <summary>見つからなかったリソースのID</summary>
    public string ResourceId { get; }

    /// <param name="resourceType">リソースの種類</param>
    /// <param name="resourceId">リソースのID</param>
    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} (ID: {resourceId}) が見つかりません", "NOT_FOUND", 404,
            new Dictionary<string, object>
            {
                { "resourceType", resourceType },
                { "resourceId", resourceId }
            })
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }
}
