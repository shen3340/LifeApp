using LifeApp.SDK.Data_Models;
using LifeApp.Utility.Interfaces.Verbs;
using LifeApp.Utility.Models;
using LifeApp.Utility.Verbs;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace LifeApp.Utility
{
    internal class Program
    {
        private static IServiceProvider _serviceProvider = null;
        private static Microsoft.Extensions.Logging.ILogger _logger;
        private static IConfiguration _configuration = null;
        private static string SettingsFileName = "";

        static async Task Main(string[] args)
        {
            Setup();

            HandleCommandLineParameters(args);

            EndApplication();
        }

        static void Setup()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

            NLog.LogManager.Setup().LoadConfigurationFromFile("NLog.config");

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConsole()
                    .AddNLog()
                    .SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<Program>();

#if DEBUG
            SettingsFileName = "appsettings.json";
#else
            SettingsFileName = "appsettings.json";
#endif

            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>(optional: true)
                .Build();

            _serviceProvider = DependencyInjector.GetServiceProvider(_configuration);

            LifeAppDatabaseFactory.Setup(GetConfigValue("ConnectionStrings:LifeAppConnectionString"));
        }

        static int HandleCommandLineParameters(string[] args)
        {
            Parser parser = new(conf =>
            {
                conf.CaseInsensitiveEnumValues = true;
                conf.HelpWriter = Console.Out;
                conf.CaseSensitive = false;
            });

            Task<int> results = parser.ParseArguments<
                LetterboxdSyncVerbOptions
            >(args)
            .MapResult(
                async opts => await HandleLetterboxdSyncVerbParameters(),
                er => Task.FromResult(1)
            );

            return results.Result;
        }

        static async Task<int> HandleLetterboxdSyncVerbParameters()
        {
            _logger.LogInformation("Verb: LetterboxdSyncVerb");

            ILetterboxdSyncVerb LetterboxdSyncVerb = _serviceProvider.GetService<ILetterboxdSyncVerb>();
            
            await LetterboxdSyncVerb.UpdateMovieWatchlistTables();

            return 0;
        }

        static Task<int> HandleNoVerbSpecifiedAsync()
        {
            Console.WriteLine("Error: No verb specified.");
            // Optionally, display help here
            // var helpText = HelpText.AutoBuild(result, h => h, e => e);
            // Console.WriteLine(helpText);
            return Task.FromResult(1);
        }

        static void EndApplication()
        {
#if DEBUG
            Console.ReadLine();
#endif
            _logger.LogInformation("Application finished.");
        }

        static string GetConfigValue(string configPath)
        {
            string configValueString = _configuration[configPath];

            if (string.IsNullOrEmpty(configValueString))
            {
                _logger.LogError($"{configPath} is null or empty!");
                Environment.Exit(1);
            }
            return configValueString;
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError((Exception)e.ExceptionObject, "An unhandled exception occurred!");
            Console.WriteLine(e.ExceptionObject.ToString());
#if DEBUG
            Console.ReadLine();
#endif
            Environment.Exit(1);
        }
    }
}