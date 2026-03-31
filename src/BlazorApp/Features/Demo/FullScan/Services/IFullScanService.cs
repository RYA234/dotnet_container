using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// フルスキャンデモのサービスインターフェース
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/FullScan/internal-design.md</para>
/// <para><strong>責務:</strong> インデックスあり/なしの検索パフォーマンス差を計測・比較する</para>
/// </remarks>
public interface IFullScanService
{
    /// <summary>デモ用データ（100万件）をセットアップする</summary>
    Task<SetupResponse> SetupAsync();

    /// <summary>インデックスなしでメールアドレスを検索する（フルスキャン）</summary>
    /// <param name="email">検索するメールアドレス</param>
    Task<FullScanResponse> SearchWithoutIndexAsync(string email);

    /// <summary>Email列にインデックスを作成する</summary>
    Task<SetupResponse> CreateIndexAsync();

    /// <summary>インデックスありでメールアドレスを検索する（インデックス使用）</summary>
    /// <param name="email">検索するメールアドレス</param>
    Task<FullScanResponse> SearchWithIndexAsync(string email);
}
