using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Interfaces;
using LifeApp.SDK.Interfaces.Services;
using LifeApp.SDK.Models;
using LifeApp.SDK.Repositories;
using Microsoft.Extensions.Logging;

namespace LifeApp.SDK.Services
{
    public class MovieGenreService(IUnitOfWork unitOfWork, IOperationResult result, ILogger<MovieGenreService> logger) : IMovieGenreService
    {
        public IUnitOfWork UnitOfWork { get; set; } = unitOfWork;
        public IOperationResult Result { get; set; } = result;
        private readonly ILogger _logger = logger;

        private NPoco.IDatabase Db => ((NPocoUnitOfWork)UnitOfWork).Db;

        private const int BatchSize = 500;

        public List<MovieGenre> GetAllMovieGenres()
        {
            Result.Reset();

            List<MovieGenre> movieGenres;

            try
            {
                movieGenres = Db.Fetch<MovieGenre>(
                    "SELECT * FROM MovieGenres");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all MovieGenres");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Fetched {movieGenres.Count} MovieGenres.");

            return movieGenres;
        }

        public IOperationResult BulkInsertMovieGenres(List<MovieGenre> genres)
        {
            Result.Reset();

            if (genres == null || genres.Count == 0)
                return Result;

            try
            {
                UnitOfWork.BeginTransaction();

                for (int movieGenreNum = 0; movieGenreNum < genres.Count; movieGenreNum += BatchSize)
                {
                    var batch = genres.Skip(movieGenreNum).Take(BatchSize).ToList();

                    Db.InsertBulk(batch);
                }

                UnitOfWork.Commit();

                _logger.LogInformation($"Bulk inserted {genres.Count} MovieGenres.");
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error bulk inserting {genres.Count} MovieGenres");

                Result.GetException(ex);

                throw;
            }

            return Result;
        }

        public IOperationResult BulkDeleteMovieGenres(List<MovieGenre> genres)
        {
            Result.Reset();

            if (genres == null || genres.Count == 0)
                return Result;

            try
            {
                UnitOfWork.BeginTransaction();

                for (int movieGenreNum = 0; movieGenreNum < genres.Count; movieGenreNum += BatchSize)
                {
                    var batch = genres.Skip(movieGenreNum).Take(BatchSize).ToList();

                    Db.Delete(batch);
                }

                UnitOfWork.Commit();
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error bulk deleting {genres.Count} MovieGenres");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Bulk deleted {genres.Count} MovieGenres.");

            return Result;
        }

    }
}
