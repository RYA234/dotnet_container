using System.Text.RegularExpressions;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// ログに含まれる機密情報をマスキングするヘルパークラス。
/// パスワード・APIキー・トークン・シークレットをマスクして安全にログ出力する。
/// </summary>
public static class LogMaskingHelper
{
    /// <summary>マスキング対象のキーワード（大文字小文字を区別しない）</summary>
    private static readonly string[] SensitiveKeys =
    [
        "password", "apikey", "api_key", "token", "secret"
    ];

    /// <summary>
    /// 文字列内の機密情報を "***" にマスキングして返す。
    /// "key=value" または "key: value" の形式を検出してマスキングする。
    /// </summary>
    /// <param name="input">マスキング対象の文字列</param>
    /// <returns>機密情報がマスクされた文字列</returns>
    public static string Mask(string input)
    {
        var result = input;
        foreach (var key in SensitiveKeys)
        {
            // key=value 形式をマスキング（大文字小文字を区別しない）
            result = Regex.Replace(result, $@"(?i)({key})=\S+", "$1=***");
        }
        return result;
    }
}
