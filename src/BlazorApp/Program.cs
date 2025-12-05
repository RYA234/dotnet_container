using DotNetEnv;
using BlazorApp.Features.Supabase.Services;
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
        using var client = new AmazonSecretsManagerClient(Amazon.RegionEndpoint.APNortheast1);

        var supabaseSecretRequest = new GetSecretValueRequest
        {
            SecretId = "ecs/dotnet-container/supabase"
        };

        var supabaseSecretResponse = await client.GetSecretValueAsync(supabaseSecretRequest);
        var supabaseSecret = JsonSerializer.Deserialize<Dictionary<string, string>>(supabaseSecretResponse.SecretString);

        if (supabaseSecret != null)
        {
            Environment.SetEnvironmentVariable("Supabase__Url", supabaseSecret["url"]);
            Environment.SetEnvironmentVariable("Supabase__AnonKey", supabaseSecret["anon_key"]);
            Console.WriteLine("✓ Supabase configuration loaded from AWS Secrets Manager");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠ Warning: Failed to load secrets from AWS Secrets Manager: {ex.Message}");
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
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<BlazorApp.Services.ICalculatorService, BlazorApp.Services.CalculatorService>();
builder.Services.AddScoped<BlazorApp.Services.IPricingService, BlazorApp.Services.PricingService>();
builder.Services.AddScoped<BlazorApp.Services.IOrderService, BlazorApp.Services.OrderService>();
builder.Services.AddScoped<ISupabaseService, SupabaseService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Configure path base for /dotnet routing
app.UsePathBase("/dotnet");

app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Health check for ALB (will be served under /dotnet/healthz)
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" }));

// Supabase connection test endpoint
app.MapGet("/supabase/test", async (ISupabaseService supabaseService) =>
{
    var result = await supabaseService.TestConnectionAsync();
    return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: 503);
});

app.Run();
