namespace BlazorApp.Shared.Exceptions;

/// <summary>
/// ビジネスルール違反を表す例外。
/// 技術的には正常だがビジネス上許可できない操作（例: 在庫不足、与信限度超過）に使用する。
/// HTTPステータス: 400 Bad Request
/// </summary>
public class BusinessRuleException : ApplicationException
{
    /// <summary>違反したビジネスルール名（例: "CreditLimitExceeded", "InsufficientInventory"）</summary>
    public string RuleName { get; }

    /// <param name="message">ユーザー向けエラーメッセージ</param>
    /// <param name="ruleName">違反したルール名</param>
    public BusinessRuleException(string message, string ruleName)
        : base(message, "BUSINESS_RULE_VIOLATION", 400,
            new Dictionary<string, object>
            {
                { "ruleName", ruleName }
            })
    {
        RuleName = ruleName;
    }
}
