using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeApp.SDK.Interfaces.Services
{
    public interface IMovieService
    {
        public List<Movie> GetAllMovies();

        public Movie GetMovieByNameAndYear(string movieName, int releaseYear);

        public IOperationResult UpsertMovies(IEnumerable<Movie> movies);

        public IOperationResult BulkDeleteMovies(List<Movie> movies);
    }
}
