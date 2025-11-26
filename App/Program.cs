using Microsoft.EntityFrameworkCore;
using App.Data;

var builder = WebApplication.CreateBuilder(args);

// OLTP: SQL Server
var oltpConnectionString = builder.Configuration.GetConnectionString("OLTPConnection")
    ?? throw new InvalidOperationException("OLTP connection string not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseOracle(oltpConnectionString));

// OLAP: Oracle
//var olapConnectionString = builder.Configuration.GetConnectionString("OLAPConnection")
//    ?? throw new InvalidOperationException("OLAP connection string not found.");
//builder.Services.AddDbContext<AnalyticsDbContext>(options =>
//    options.UseOracle(olapConnectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
