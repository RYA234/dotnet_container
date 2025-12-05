using DotNetEnv;
using BlazorApp.Features.Supabase.Services;

// Load environment variables from .env file in local development
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Production")
{
    var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
    if (File.Exists(envPath))
    {
        Env.Load(envPath);
        Console.WriteLine("Environment variables loaded from .env file");
    }
    else
    {
        Console.WriteLine($".env file not found at: {envPath}");
    }
}

var builder = WebApplication.CreateBuilder(args);

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
