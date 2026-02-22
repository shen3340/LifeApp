using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Interfaces;
using LifeApp.SDK.Interfaces.Services;
using LifeApp.SDK.Repositories;
using LifeApp.SDK.Services;
using LifeApp.Web.Auth;
using LifeApp.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog.Web;
using Radzen;
using System.ComponentModel.DataAnnotations;

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

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";  // your updated route
        options.LogoutPath = "/logout";

        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogInformation("User signing in: {Name}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },

            OnValidatePrincipal = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Validating authentication cookie for {Name}", context.Principal?.Identity?.Name);
                return Task.CompletedTask;
            },

            OnRedirectToLogin = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogWarning("Redirecting to login. Path: {Path}", context.Request.Path);

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;

        options.Events = new OAuthEvents
        {
            OnRedirectToAuthorizationEndpoint = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Redirecting to Google for authentication. Redirect URI: {RedirectUri}", context.RedirectUri);

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },

            OnTicketReceived = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                var email = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var allowedEmail = builder.Configuration["AdminSettings:AllowedEmail"];

                logger.LogInformation("Google ticket received for {Email}", email);

                if (!string.IsNullOrEmpty(allowedEmail) &&
                    !string.Equals(email, allowedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning("Unauthorized login attempt from {Email}", email);

                    // Reject login
                    context.Fail("Unauthorized email");

                    // Redirect back to signin page with error query
                    context.Response.Redirect("/signin?error=unauthorized");
                    context.HandleResponse(); // Prevents cookie from being issued
                }

                return Task.CompletedTask;
            },

            OnRemoteFailure = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                logger.LogError(context.Failure, "Google remote failure");

                context.Response.Redirect("/signin?error=google");
                context.HandleResponse();

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Logging.ClearProviders();
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
builder.Host.UseNLog();

LifeAppDatabaseFactory.Setup(connectionString);

builder.Services.AddScoped<IOperationResult, DBOperationResult>();
builder.Services.AddScoped<IUnitOfWork, NPocoUnitOfWork>();

builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IMovieGenreService, MovieGenreService>();
builder.Services.AddScoped<IMovieProviderService, MovieProviderService>();
builder.Services.AddScoped<IWishListService, WishListService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapGet("/externallogin", async (HttpContext context, string? returnUrl) =>
{
    returnUrl ??= "/";

    await context.ChallengeAsync(
        GoogleDefaults.AuthenticationScheme,
        new AuthenticationProperties
        {
            RedirectUri = returnUrl
        });
}).AllowAnonymous();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/signed-out");
}).AllowAnonymous();

app.Run();