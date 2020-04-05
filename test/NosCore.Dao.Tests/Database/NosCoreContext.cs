//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using Microsoft.EntityFrameworkCore;
using NosCore.Dao.Tests.TestsModels;

namespace NosCore.Dao.Tests.Database
{
    public class NosCoreContext : DbContext
    {
        public NosCoreContext(DbContextOptions? options) : base(options)
        {
        }

        public virtual DbSet<SimpleEntity>? SimpleEntities { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SimpleEntity>()
                .Property(e => e.Key)
                .ValueGeneratedOnAdd();
        }
    }
}