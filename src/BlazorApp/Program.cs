var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<BlazorApp.Services.ICalculatorService, BlazorApp.Services.CalculatorService>();
builder.Services.AddScoped<BlazorApp.Services.IPricingService, BlazorApp.Services.PricingService>();
builder.Services.AddScoped<BlazorApp.Services.IOrderService, BlazorApp.Services.OrderService>();

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

app.Run();
