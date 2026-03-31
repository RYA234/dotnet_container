using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

/// <summary>
/// SELECT *デモのサービスインターフェース
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/SelectStar/internal-design.md</para>
/// <para><strong>責務:</strong> SELECT *（全カラム）と必要カラムのみ指定した場合の転送データ量・コストを比較する</para>
/// </remarks>
public interface ISelectStarService
{
    /// <summary>デモ用データ（デフォルト1万件）をセットアップする。各レコードにBio(10KB)・Preferences(5KB)・ActivityLog(20KB)を含む</summary>
    Task<SetupResponse> SetupAsync();

    /// <summary>SELECT *で全カラムを取得する（大量のデータ転送が発生する）</summary>
    Task<SelectStarResponse> GetAllColumnsAsync();

    /// <summary>必要なカラム（Id, Name, Email）のみ指定して取得する</summary>
    Task<SelectStarResponse> GetSpecificColumnsAsync();
}
