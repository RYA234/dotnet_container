using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// N+1問題デモのサービスインターフェース
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/n-plus-one-demo/internal-design.md</para>
/// <para><strong>責務:</strong> N+1問題が発生するBad実装と、JOINで解決したGood実装を比較する</para>
/// </remarks>
public interface INPlusOneService
{
    /// <summary>N+1問題あり: ユーザー取得後にループ内で部署情報を個別取得する（1+N回クエリ）</summary>
    Task<NPlusOneResponse> GetUsersBad();

    /// <summary>N+1問題なし: JOINを使って1回のクエリでユーザーと部署情報を一括取得する</summary>
    Task<NPlusOneResponse> GetUsersGood();
}
