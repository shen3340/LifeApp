using LifeApp.SDK.Data_Models;
using LifeApp.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LifeApp.SDK.Interfaces.Services
{
    public interface IMovieProviderService
    {
        public List<MovieProvider> GetAllMovieProviders();

        public IOperationResult BulkInsertMovieProviders(List<MovieProvider> movieProviders);

        public IOperationResult TruncateMovieProviders();
    }
}
