using LifeApp.Utility.Interfaces.Verbs;
using LifeApp.Utility.Interfaces;
using Microsoft.Extensions.Logging;

namespace LifeApp.Utility.Verbs
{
    public class LetterboxdSyncVerb(
        ILogger<LetterboxdSyncVerb> logger,
        IMovieSyncHandler movieSyncHandler) : ILetterboxdSyncVerb
    {
        private readonly ILogger<LetterboxdSyncVerb> _logger = logger;
        private readonly IMovieSyncHandler _movieSyncHandler = movieSyncHandler;

        public async Task<int> Execute(LetterboxdSyncVerbOptions options)
        {
            try
            {
                _logger.LogInformation("Starting Letterboxd watchlist sync...");

                await _movieSyncHandler.SyncWatchlistAsync();

                _logger.LogInformation("Letterboxd watchlist sync completed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Letterboxd watchlist sync failed.");
                return 1;
            }
        }
    }
}
