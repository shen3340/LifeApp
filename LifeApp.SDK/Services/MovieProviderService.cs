using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Interfaces;
using LifeApp.SDK.Interfaces.Services;
using LifeApp.SDK.Models;
using LifeApp.SDK.Repositories;
using Microsoft.Extensions.Logging;

namespace LifeApp.SDK.Services
{
    public class MovieProviderService(IUnitOfWork unitOfWork, IOperationResult result, ILogger<MovieProviderService> logger) : IMovieProviderService
    {
        public IUnitOfWork UnitOfWork { get; set; } = unitOfWork;
        public IOperationResult Result { get; set; } = result;
        private readonly ILogger _logger = logger;

        private NPoco.IDatabase Db => ((NPocoUnitOfWork)UnitOfWork).Db;

        private const int BatchSize = 500; 

        public List<MovieProvider> GetAllMovieProviders()
        {
            Result.Reset();

            List<MovieProvider> movieProviders;

            try
            {
                movieProviders = Db.Fetch<MovieProvider>(
                    "SELECT * FROM MovieProviders");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all MovieProviders");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Fetched {movieProviders.Count} MovieProviders.");

            return movieProviders;
        }

        public IOperationResult BulkInsertMovieProviders(List<MovieProvider> providers)
        {
            Result.Reset();

            if (providers == null || providers.Count == 0)
                return Result;

            try
            {
                UnitOfWork.BeginTransaction();

                for (int movieProviderNum = 0; movieProviderNum < providers.Count; movieProviderNum += BatchSize)
                {
                    var batch = providers.Skip(movieProviderNum).Take(BatchSize).ToList();

                    Db.InsertBulk(batch);
                }

                UnitOfWork.Commit();

                _logger.LogInformation($"Bulk inserted {providers.Count} MovieProviders.");
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error bulk inserting {providers.Count} MovieProviders");
                
                Result.GetException(ex);

                throw;
            }

            return Result;
        }

        public IOperationResult TruncateMovieProviders()
        {
            Result.Reset();

            try
            {
                UnitOfWork.BeginTransaction();

                Db.Execute("TRUNCATE TABLE MovieProviders");

                UnitOfWork.Commit();

                _logger.LogInformation($"Truncated MovieProviders table and reset identity.");
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, "Error truncating MovieProviders");

                Result.GetException(ex);

                throw;
            }

            return Result;
        }
    }
}
