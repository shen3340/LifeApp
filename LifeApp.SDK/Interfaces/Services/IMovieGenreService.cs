using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeApp.SDK.Interfaces.Services
{
    public interface IMovieGenreService
    {
        public List<MovieGenre> GetAllMovieGenres();


        public IOperationResult BulkInsertMovieGenres(List<MovieGenre> movieGenres);


        public IOperationResult BulkDeleteMovieGenres(List<MovieGenre> movieGenres);
    }
}
