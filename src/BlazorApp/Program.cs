using DotNetEnv;
using BlazorApp.Features.Supabase.Services;
using BlazorApp.Features.Demo.Data;
using BlazorApp.Features.Demo.Services;
using BlazorApp.Features.Demo.Entities;
using Microsoft.EntityFrameworkCore;
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

// Demo database configuration (using InMemory for demo purposes)
builder.Services.AddDbContext<DemoDbContext>(options =>
    options.UseInMemoryDatabase("DemoDatabase"));

builder.Services.AddScoped<INPlusOneService, NPlusOneService>();

var app = builder.Build();

// Initialize demo database with sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
    context.Database.EnsureCreated();

    // Seed data if empty
    if (!context.Departments.Any())
    {
        var departments = new List<Department>
        {
            new Department { Name = "開発部" },
            new Department { Name = "営業部" },
            new Department { Name = "人事部" },
            new Department { Name = "総務部" },
            new Department { Name = "マーケティング部" },
            new Department { Name = "経理部" },
            new Department { Name = "法務部" },
            new Department { Name = "カスタマーサポート部" },
            new Department { Name = "企画部" },
            new Department { Name = "品質保証部" }
        };
        context.Departments.AddRange(departments);
        context.SaveChanges();

        var users = new List<User>();
        var names = new[] { "山田太郎", "佐藤花子", "鈴木一郎", "高橋美咲", "田中健太", "伊藤愛", "渡辺翔", "中村結衣", "小林大輔", "加藤さくら" };
        for (int i = 0; i < 100; i++)
        {
            users.Add(new User
            {
                Name = $"{names[i % names.Length]}{i}",
                DepartmentId = (i % 10) + 1,
                Email = $"user{i}@example.com"
            });
        }
        context.Users.AddRange(users);
        context.SaveChanges();

        Console.WriteLine("✓ Demo database initialized with sample data");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Configure path base for /dotnet routing
app.UsePathBase("/dotnet");

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
