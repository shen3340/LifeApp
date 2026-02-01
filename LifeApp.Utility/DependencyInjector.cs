using LifeApp.Utility.Interfaces;
using LifeApp.Utility.Interfaces.Verbs;
using LifeApp.Utility.Models;
using LifeApp.Utility.Verbs;
using LifeApp.SDK.Data_Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using LifeApp.Utility.Handlers;

namespace LifeApp.Utility
{
    internal class DependencyInjector
    {
        public static IServiceProvider GetServiceProvider(IConfiguration configuration, Settings settings)
        {
            ServiceCollection services = new();

            services.AddLogging(configure =>
                configure
                    .AddConsole()
                    .AddNLog()
                    .SetMinimumLevel(LogLevel.Debug));

            // ********************************************
            // **** ---- LifeApp.SDK.Repositories ---- ****
            // ********************************************

            services.AddScoped<LifeApp.SDK.Interfaces.IUnitOfWork, LifeApp.SDK.Repositories.NPocoUnitOfWork>();

            // *********************************************
            // **** ---- LifeApp.SDK.Services ---- ****
            // *********************************************
            services.AddScoped<LifeApp.SDK.Interfaces.Services.IMovieService, LifeApp.SDK.Services.MovieService>();
            services.AddScoped<LifeApp.SDK.Interfaces.Services.IMovieGenreService, LifeApp.SDK.Services.MovieGenreService>();
            services.AddScoped<LifeApp.SDK.Interfaces.Services.IMovieProviderService, LifeApp.SDK.Services.MovieProviderService>();
            // *********************************************
            // **** ---- Services Helpers ---- ****
            // *********************************************
            services.AddScoped<IOperationResult, DBOperationResult>();
            services.AddSingleton<IConfiguration>(configuration);
            services.AddScoped<IMovieSyncHandler, MovieSyncHandler>();
            services.AddSingleton(settings);

            // *********************************************
            // **** ---- LifeApp.Utility Verbs ---- ****
            // *********************************************

            services.AddScoped<ILetterboxdSyncVerb, LetterboxdSyncVerb>();

            return services.BuildServiceProvider();
        }
    }
}