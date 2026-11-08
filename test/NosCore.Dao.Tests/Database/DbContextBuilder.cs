//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace NosCore.Dao.Tests.Database
{
    public class DbContextBuilder
    {
        private readonly DbContextOptions _options;

        private readonly ConcurrentQueue<DbContext> _created = new();

        public DbContextBuilder(params IInterceptor[] interceptors)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            var builder = new DbContextOptionsBuilder<NosCoreContext>()
                .UseSqlite(connection);
            if (interceptors.Length > 0)
            {
                builder.AddInterceptors(interceptors);
            }

            _options = builder.Options;
        }

        public IEnumerable<DbContext> CreatedContexts => _created;

        public DbContext CreateContext()
        {
            var dbContext = new NosCoreContext(_options);
            dbContext.Database.EnsureCreated();
            _created.Enqueue(dbContext);
            return dbContext;
        }
    }
}