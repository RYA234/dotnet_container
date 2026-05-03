using DotNetEnv;
using BlazorApp.Features.Supabase.Services;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Features.Demo.TestingTechniques.EquivalencePartitioning.Services;
using BlazorApp.Middleware;
using BlazorApp.Shared.Data;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Load configuration based on environment
var environment = builder.Environment.EnvironmentName;

if (environment == "Production")
{
    // Production: Load from AWS Secrets Manager
    Console.WriteLine("Loading configuration from AWS Secrets Manager...");

    try
    {
        Console.WriteLine("Creating AWS Secrets Manager client...");
        using var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.APNortheast1);
        Console.WriteLine("Client created successfully");

        Console.WriteLine("Requesting secret: ecs/dotnet-container/supabase");
        var supabaseSecretRequest = new GetSecretValueRequest
        {
            SecretId = "ecs/dotnet-container/supabase"
        };

        Console.WriteLine("Calling GetSecretValueAsync...");
        var supabaseSecretResponse = await client.GetSecretValueAsync(supabaseSecretRequest);
        Console.WriteLine($"Secret retrieved, length: {supabaseSecretResponse.SecretString?.Length ?? 0}");

        var supabaseSecret = JsonSerializer.Deserialize<Dictionary<string, string>>(supabaseSecretResponse.SecretString);
        Console.WriteLine($"Secret deserialized, keys: {(supabaseSecret != null ? string.Join(", ", supabaseSecret.Keys) : "none")}");

        if (supabaseSecret != null)
        {
            // Add to configuration
            builder.Configuration["Supabase:Url"] = supabaseSecret["url"];
            builder.Configuration["Supabase:AnonKey"] = supabaseSecret["anon_key"];
            Console.WriteLine($"✓ Supabase configuration loaded: Url={supabaseSecret["url"]}, KeyLength={supabaseSecret["anon_key"].Length}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Warning: Failed to load secrets from AWS Secrets Manager: {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        Console.WriteLine("Continuing with environment variables...");
    }
}
else
{
    // Development: Load from .env file
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        Console.WriteLine("✓ Environment variables loaded from .env file");
    }
    else
    {
        Console.WriteLine($"⚠ .env file not found at: {envPath}");
    }
}

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        // Feature-based folder structure support
        options.ViewLocationFormats.Clear();
        options.ViewLocationFormats.Add("/Features/{1}/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/Features/Shared/Views/{0}.cshtml");
        options.ViewLocationFormats.Add("/Views/Shared/{0}.cshtml");
    });

builder.Services.AddScoped<BlazorApp.Services.ICalculatorService, BlazorApp.Services.CalculatorService>();
builder.Services.AddScoped<BlazorApp.Services.IPricingService, BlazorApp.Services.PricingService>();
builder.Services.AddScoped<BlazorApp.Services.IOrderService, BlazorApp.Services.OrderService>();
builder.Services.AddScoped<ISupabaseService, SupabaseService>();

// N+1デモ用 SQLite — ContentRootPath で絶対パスに変換
var nPlusOneDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "n_plus_one_demo.db");
builder.Configuration["ConnectionStrings:NPlusOneDemo"] = $"Data Source={nPlusOneDbPath};";

// フルスキャンデモ用 SQLite — ContentRootPath で絶対パスに変換
var fullScanDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "full_scan_demo.db");
builder.Configuration["ConnectionStrings:FullScanDemo"] = $"Data Source={fullScanDbPath};";

// SELECT * デモ用 SQLite — ContentRootPath で絶対パスに変換
var selectStarDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "select_star_demo.db");
builder.Configuration["ConnectionStrings:SelectStarDemo"] = $"Data Source={selectStarDbPath};";

// LIKE検索デモ用 SQLite — ContentRootPath で絶対パスに変換
var likeSearchDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "like_search_demo.db");
builder.Configuration["ConnectionStrings:LikeSearchDemo"] = $"Data Source={likeSearchDbPath};";

builder.Services.AddScoped<INPlusOneService, NPlusOneService>();
builder.Services.AddScoped<IFullScanService, FullScanService>();
builder.Services.AddScoped<ISelectStarService, SelectStarService>();
builder.Services.AddScoped<ILikeSearchService, LikeSearchService>();
builder.Services.AddScoped<IValidationDemoService, ValidationDemoService>();
builder.Services.AddScoped<ILoggingDemoService, LoggingDemoService>();

// DB接続デモ用 (SQLite) — ContentRootPath で絶対パスに変換してデプロイ環境でも動作するようにする
var sqliteDbPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "database_connection_demo.db");
var sqliteConnectionString = builder.Configuration.GetConnectionString("DemoSQLite")
    ?.Replace("Data Source=Data/database_connection_demo.db", $"Data Source={sqliteDbPath}")
    ?? $"Data Source={sqliteDbPath};";
builder.Services.AddSingleton<IDbConnectionFactory>(sp =>
    new SqliteConnectionFactory(sqliteConnectionString, sp.GetRequiredService<ILogger<SqliteConnectionFactory>>()));
builder.Services.AddScoped<IDatabaseConnectionDemoService, DatabaseConnectionDemoService>();

// テスト技法デモ
builder.Services.AddScoped<IEquivalencePartitioningService, EquivalencePartitioningService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure path base for /dotnet routing
app.UsePathBase("/dotnet");

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseStaticFiles();
app.UseRouting();

// Health check for ALB (will be served under /dotnet/healthz)
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// Supabase connection test endpoint
app.MapGet("/supabase/test", async (ISupabaseService supabaseService) =>
{
    var result = await supabaseService.TestConnectionAsync();
    return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: 503);
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
