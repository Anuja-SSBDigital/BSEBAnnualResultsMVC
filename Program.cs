using Microsoft.EntityFrameworkCore;
using BSEBAnnualResultsMVC.Models;
using BSEBAnnualResultsMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add services
builder.Services.AddControllersWithViews();

// ✅ Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs")));

// ✅ Register ResultService
builder.Services.AddScoped<ResultService>();

// ✅ Session MUST be registered in services BEFORE app.Build()
builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// ✅ Get logger for startup logs
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Application starting up at {Time}", DateTime.Now);

    // ✅ Test DB connection at startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.CanConnect())
            logger.LogInformation("Database connection successful.");
        else
            logger.LogWarning("Database connection FAILED. Check connection string.");
    }

    // Middleware pipeline — ORDER MATTERS
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    // ✅ Session MUST come after UseRouting and before UseAuthorization
    app.UseSession();

    app.UseAuthorization();

    // Routing
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Result}/{action=Index}/{id?}"
    );

    logger.LogInformation("Application configured successfully. Listening for requests...");

    app.Run();
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Application failed to start.");
    throw;
}

// ✅ Catch fatal unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
{
    logger.LogCritical("FATAL UNHANDLED EXCEPTION: {Error}", e.ExceptionObject?.ToString());
};