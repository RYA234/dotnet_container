using System.Text.Json;
using BlazorApp.Shared.DTOs;
using BlazorApp.Shared.Exceptions;
using AppException = BlazorApp.Shared.Exceptions.ApplicationException;

namespace BlazorApp.Middleware;

/// <summary>
/// アプリケーション全体の例外をキャッチして JSON レスポンスに変換するミドルウェア。
/// Program.cs で app.UseMiddleware&lt;ExceptionHandlingMiddleware&gt;() として登録する。
///
/// ログレベルの方針:
/// - ValidationException / NotFoundException / BusinessRuleException → Warning（ユーザー起因）
/// - InfrastructureException / その他 → Error（システム起因）
///
/// StackTrace は開発環境のみ付与し、本番環境では省略する。
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation error: {ErrorCode}", ex.ErrorCode);
            await WriteErrorResponse(context, ex.StatusCode, new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode,
                ValidationErrors = ex.Errors,
                Details = ex.Details.Count > 0 ? ex.Details : null,
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : null
            });
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Not found: {ResourceType} ID={ResourceId}", ex.ResourceType, ex.ResourceId);
            await WriteErrorResponse(context, ex.StatusCode, new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode,
                Details = ex.Details,
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : null
            });
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation: {RuleName}", ex.RuleName);
            await WriteErrorResponse(context, ex.StatusCode, new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode,
                Details = ex.Details,
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : null
            });
        }
        catch (InfrastructureException ex)
        {
            _logger.LogError(ex, "Infrastructure error: {Service}", ex.Service);
            await WriteErrorResponse(context, ex.StatusCode, new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode,
                Details = ex.Details,
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : null
            });
        }
        catch (AppException ex)
        {
            _logger.LogError(ex, "Application error: {ErrorCode}", ex.ErrorCode);
            await WriteErrorResponse(context, ex.StatusCode, new ErrorResponse
            {
                Error = ex.Message,
                Code = ex.ErrorCode,
                Details = ex.Details.Count > 0 ? ex.Details : null,
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            await WriteErrorResponse(context, 500, new ErrorResponse
            {
                Error = "内部サーバーエラーが発生しました",
                Code = "INTERNAL_ERROR",
                StackTrace = _env.IsDevelopment() ? ex.StackTrace : null
            });
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, ErrorResponse response)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }
}
