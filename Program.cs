using Microsoft.EntityFrameworkCore;
using BSEBAnnualResultsMVC.Models;
using BSEBAnnualResultsMVC.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();

// ✅ Register DbContext
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs")));

// ✅ 🔥 THIS LINE WAS MISSING (MAIN ERROR FIX)
builder.Services.AddScoped<ResultService>();

var app = builder.Build();

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

app.Run();