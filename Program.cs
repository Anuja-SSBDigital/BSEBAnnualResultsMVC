using Microsoft.EntityFrameworkCore;
using BSEBAnnualResultsMVC.Models;
using BSEBAnnualResultsMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// ✅ Logging is already registered by default in ASP.NET Core
// But you can configure it explicitly here:
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

var app = builder.Build();

// ✅ Get logger for Program.cs startup logs
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("Application starting up at {Time}", DateTime.Now);

    // ✅ Optional: Test DB connection at startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.CanConnect())
            logger.LogInformation("Database connection successful.");
        else
            logger.LogWarning("Database connection FAILED. Check connection string.");
    }

    // Middleware
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
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