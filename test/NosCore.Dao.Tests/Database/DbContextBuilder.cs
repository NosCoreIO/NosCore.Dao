//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NosCore.Dao.Interfaces;

namespace NosCore.Dao.Tests.Database
{
    public class DbContextBuilder : IDbContextBuilder
    {
        private readonly DbContextOptions _options;

        public DbContextBuilder()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            _options = new DbContextOptionsBuilder<NosCoreContext>()
                .UseSqlite(connection)
                .Options;
        }

        public DbContext CreateContext()
        {
            var dbContext = new NosCoreContext(_options);
            dbContext.Database.EnsureCreated();
            return dbContext;
        }
    }
}