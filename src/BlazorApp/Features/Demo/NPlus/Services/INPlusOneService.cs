using BlazorApp.Features.Demo.DTOs;

namespace BlazorApp.Features.Demo.Services;

public interface INPlusOneService
{
    Task<NPlusOneResponse> GetUsersBad();
    Task<NPlusOneResponse> GetUsersGood();
}
