using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public interface ILikeSearchService
{
    Task<SetupResponse> SetupAsync();
    Task<LikeSearchResponse> SearchPrefixAsync(string keyword);
    Task<LikeSearchResponse> SearchPartialAsync(string keyword);
}
