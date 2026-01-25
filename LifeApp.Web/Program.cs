using LifeApp.Web.Data;
using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Interfaces;
using LifeApp.SDK.Repositories;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

var connectionString = builder.Configuration.GetConnectionString("LifeAppConnectionString")
    ?? throw new InvalidOperationException("Connection string 'LifeAppConnectionString' not found.");

builder.Services.AddRazorPages();
builder.Services.AddRadzenComponents();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<ApplicationDBContext>(options => options.UseSqlServer(connectionString));

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
builder.Host.UseNLog();

LifeAppDatabaseFactory.Setup(connectionString);

builder.Services.AddScoped<IOperationResult, DBOperationResult>();
builder.Services.AddScoped<IUnitOfWork, NPocoUnitOfWork>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

var logger = NLog.LogManager.GetCurrentClassLogger();
logger.Debug("init main");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.Run();