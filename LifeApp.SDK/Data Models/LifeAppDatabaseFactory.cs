using Microsoft.Data.SqlClient;
using NPoco;
using NPoco.FluentMappings;

namespace LifeApp.SDK.Data_Models
{
    public class LifeAppDatabaseFactory
    {
        public static DatabaseFactory DbFactory { get; set; }

        public static string ConnectionString
        {
            get
            {
                return DbFactory.GetDatabase().ConnectionString;
            }
        }

        public static void Setup(string connectionString)
        {
            var fluentConfig = FluentMappingConfiguration.Configure(new NPocoModelMappings());

            DbFactory = DatabaseFactory.Config(x =>
            {
                x.UsingDatabase(() => new Database(connectionString, DatabaseType.SqlServer2008, SqlClientFactory.Instance));
                x.WithFluentConfig(fluentConfig);
            });
        }
    }
}