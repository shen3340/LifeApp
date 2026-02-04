using LifeApp.SDK.Models;
using NPoco.FluentMappings;

namespace LifeApp.SDK.Data_Models
{
    public class NPocoModelMappings : Mappings
    {
        public NPocoModelMappings()
        {
            For<Movie>().TableName("Movies");
            For<Movie>().PrimaryKey(x => x.Id, true);
            For<Movie>().Columns(x =>
            {
                x.Column(y => y.Genres).Result();
                x.Column(y => y.Providers).Result();
            });

            For<MovieGenre>().TableName("MovieGenres");
            For<MovieGenre>().PrimaryKey(x => x.Id, true);

            For<MovieProvider>().TableName("MovieProviders");
            For<MovieProvider>().PrimaryKey(x => x.Id, true);
        }
    }
}