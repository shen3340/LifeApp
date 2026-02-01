using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Interfaces;
using LifeApp.SDK.Interfaces.Services;
using LifeApp.SDK.Models;
using LifeApp.SDK.Repositories;
using Microsoft.Extensions.Logging;

namespace LifeApp.SDK.Services
{
    public class MovieService(IUnitOfWork unitOfWork, IOperationResult result, ILogger<MovieService> logger) : IMovieService
    {
        public IUnitOfWork UnitOfWork { get; set; } = unitOfWork;
        public IOperationResult Result { get; set; } = result;
        private readonly ILogger _logger = logger;

        private NPoco.IDatabase Db => ((NPocoUnitOfWork)UnitOfWork).Db;

        private const int BatchSize = 500;

        public List<Movie> GetAllMovies()
        {
            Result.Reset();

            List<Movie> movies;

            try
            {
                movies = Db.Fetch<Movie>(
                    "SELECT * FROM Movies");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all Movies");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Fetched {movies.Count} Movies.");

            return movies;
        }

        public Movie GetMovieByNameAndYear(string movieName, int releaseYear)
        {
            Result.Reset();

            Movie movie;

            try
            {
                movie = Db.SingleOrDefault<Movie>(
                    @"SELECT * FROM Movies 
                      WHERE (MovieName = @0)
                      AND (ReleaseYear = @1)", movieName, releaseYear);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching Movie {movieName} released in {releaseYear}");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Fetched Movie: {movie.MovieName} released in {releaseYear}");

            return movie;
        }

        public IOperationResult BulkInsertMovies(List<Movie> movies)
        {
            Result.Reset();

            if (movies == null || movies.Count == 0)
                return Result;

            try
            {
                UnitOfWork.BeginTransaction();

                for (int movieNum = 0; movieNum < movies.Count; movieNum += BatchSize)
                {
                    var batch = movies.Skip(movieNum).Take(BatchSize).ToList();

                    Db.InsertBulk(batch);
                }

                UnitOfWork.Commit();

                _logger.LogInformation($"Bulk inserted {movies.Count} Movies.");
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error bulk inserting {movies.Count} Movies");

                Result.GetException(ex);

                throw;
            }

            return Result;
        }

        public IOperationResult UpsertMovies(IEnumerable<Movie> movies)
        {
            Result.Reset();

            if (movies == null || !movies.Any())
                return Result;

            try
            {
                UnitOfWork.BeginTransaction();

                foreach (var movie in movies)
                {
                    var existing = Db.FirstOrDefault<Movie>(
                        "WHERE MovieName = @0 AND ReleaseYear = @1",
                        movie.MovieName,
                        movie.ReleaseYear
                    );

                    if (existing == null)
                    {
                        Db.Insert(movie);
                        continue;
                    }

                    if ((existing.Runtime == null || existing.Runtime == 0) &&
                        movie.Runtime.HasValue &&
                        movie.Runtime.Value > 0)
                    {
                        Db.Execute(
                            "UPDATE Movies SET Runtime = @0 WHERE Id = @1",
                            movie.Runtime,
                            existing.Id
                        );
                    }
                }

                UnitOfWork.Commit();
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error upserting {movies.Count()} Movies");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Upserted {movies.Count()} Movies.");

            return Result;
        }

        public IOperationResult BulkDeleteMovies(List<Movie> movies)
        {
            Result.Reset();

            if (movies == null || movies.Count == 0)
                return Result;

            try
            {
                UnitOfWork.BeginTransaction();

                for (int movieNum = 0; movieNum < movies.Count; movieNum += BatchSize)
                {
                    var batch = movies.Skip(movieNum).Take(BatchSize).ToList();

                    foreach (var movie in batch)
                    {
                        Db.Delete(movie);
                    }
                }

                UnitOfWork.Commit();
            }
            catch (Exception ex)
            {
                UnitOfWork.Rollback();

                _logger.LogError(ex, $"Error bulk deleting {movies.Count} Movies");

                Result.GetException(ex);

                throw;
            }
            _logger.LogInformation($"Bulk deleted {movies.Count} Movies.");

            return Result;
        }

    }
}
