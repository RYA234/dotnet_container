using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// LIKE検索デモのサービスインターフェース
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/LikeSearch/internal-design.md</para>
/// <para><strong>責務:</strong> 前方一致（インデックス有効）と中間一致（フルスキャン）のパフォーマンス差を計測・比較する</para>
/// </remarks>
public interface ILikeSearchService
{
    /// <summary>デモ用データ（デフォルト10万件）をセットアップする</summary>
    Task<SetupResponse> SetupAsync();

    /// <summary>前方一致（LIKE 'keyword%'）で検索する。インデックスが有効になる</summary>
    /// <param name="keyword">検索キーワード</param>
    Task<LikeSearchResponse> SearchPrefixAsync(string keyword);

    /// <summary>中間一致（LIKE '%keyword%'）で検索する。インデックスが無効になりフルスキャンになる</summary>
    /// <param name="keyword">検索キーワード</param>
    Task<LikeSearchResponse> SearchPartialAsync(string keyword);
}
