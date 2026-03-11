using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public interface ISelectStarService
{
    Task<SetupResponse> SetupAsync();
    Task<SelectStarResponse> GetAllColumnsAsync();
    Task<SelectStarResponse> GetSpecificColumnsAsync();
}
