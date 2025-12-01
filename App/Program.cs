using App.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// OLTP DbContext
builder.Services.AddDbContext<App.Data.OltpDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OltpConnection")));

// Seed service
builder.Services.AddScoped<SeedDataService>();

// ETL service
builder.Services.AddScoped<ETLService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var oltpConn = config.GetConnectionString("OltpConnection");
    var olapConn = config.GetConnectionString("OlapConnection");
    return new ETLService(oltpConn!, olapConn!);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy => policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Run seeding and ETL on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var seedService = services.GetRequiredService<SeedDataService>();
        await seedService.SeedAsync();

        var etlService = services.GetRequiredService<ETLService>();
        var logList = new List<string>();
        await etlService.RunETLAsync(logList);

        foreach (var log in logList)
            logger.LogInformation(log);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error running seed or ETL on startup");
        throw; // stop startup if critical
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseCors("AllowLocalhost");

app.Run();
