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
        private static IServiceProvider _serviceProvider;
        private static Microsoft.Extensions.Logging.ILogger _logger;
        private static IConfiguration configuration = null;
        private static string SettingsFileName = "";
        private static Settings settings = null;

        private static async Task Main(string[] args)
        {
            await Setup();
            await HandleCommandLineParameters(args);
            EndApplication();
        }

        private static async Task<int> HandleCommandLineParameters(string[] args)
        {
            var parser = new Parser(config =>
            {
                config.CaseInsensitiveEnumValues = true;
                config.HelpWriter = Console.Out;
                config.CaseSensitive = false;
            });

            var result = parser.ParseArguments<NoVerbOptions,
                LetterboxdSyncVerbOptions>(args);

            return await result.MapResult(
                (NoVerbOptions opts) => HandleNoVerbSpecifiedAsync(),
                (LetterboxdSyncVerbOptions opts) => HandleLetterboxdSyncVerbAndReturnExitCode(opts),
                errs => HandleParseErrorAsync(errs)
                );
        }

        private static Task<int> HandleParseErrorAsync(IEnumerable<Error> errs)
        {
            if (errs.IsHelp() || errs.IsVersion())
            {
                // Help or version was requested, do not treat as an error
                return Task.FromResult(0);
            }

            // Check if no verb was selected
            if (errs.Any(e => e is NoVerbSelectedError))
            {
                Console.WriteLine("Error: No verb specified.");
                // Optionally, display help here
                // var helpText = HelpText.AutoBuild(result, h => h, e => e);
                // Console.WriteLine(helpText);
            }

            // Handle other errors or display help
            return Task.FromResult(1);
        }

        private static async Task<int> HandleLetterboxdSyncVerbAndReturnExitCode(LetterboxdSyncVerbOptions options)
        {
            _logger.LogInformation("Verb: LetterboxdSyncVerb");

            ILetterboxdSyncVerb LetterboxdSyncVerb = _serviceProvider.GetService<ILetterboxdSyncVerb>();
            return await LetterboxdSyncVerb.Execute(options);
        }

        private static Task<int> HandleNoVerbSpecifiedAsync()
        {
            Console.WriteLine("Error: No verb specified.");
            // Optionally, display help here
            // var helpText = HelpText.AutoBuild(result, h => h, e => e);
            // Console.WriteLine(helpText);
            return Task.FromResult(1);
        }

        private static Task Setup()
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

            configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile(SettingsFileName, optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>(optional: true)
                .Build();

            settings = new Settings()
            {
                TMDBApiKey = GetConfigValue("APIKeys:TMDBApiKey"),
                LetterboxdWatchlistUrl = GetConfigValue("Letterboxd:LetterboxdWatchlistUrl")
            };

            _serviceProvider = DependencyInjector.GetServiceProvider(configuration, settings);

            LifeAppDatabaseFactory.Setup(GetConfigValue("ConnectionStrings:LifeAppConnectionString"));

            return Task.CompletedTask;
        }

        private static string GetConfigValue(string configPath)
        {
            string configValueString = configuration[configPath];

            if (string.IsNullOrEmpty(configValueString))
            {
                _logger.LogError($"{configPath} is null or empty!");
                Environment.Exit(1);
            }
            return configValueString;
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            _logger.LogError((Exception)e.ExceptionObject, "An unhandled exception occurred!");
            Console.WriteLine(e.ExceptionObject.ToString());
#if DEBUG
            Console.ReadLine();
#endif
            Environment.Exit(1);
        }

        private static void EndApplication()
        {
#if DEBUG
            Console.WriteLine();
            Console.WriteLine("Done!");
            Console.ReadLine();
#endif
        }
    }
}