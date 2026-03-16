using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public interface IFullScanService
{
    Task<SetupResponse> SetupAsync();
    Task<FullScanResponse> SearchWithoutIndexAsync(string email);
    Task<SetupResponse> CreateIndexAsync();
    Task<FullScanResponse> SearchWithIndexAsync(string email);
}
