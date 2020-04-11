//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using Microsoft.EntityFrameworkCore;
using NosCore.Dao.Tests.Database.Entities.CompositeEntities;
using NosCore.Dao.Tests.Database.Entities.CompositeTphEntities;
using NosCore.Dao.Tests.Database.Entities.SimpleEntities;
using NosCore.Dao.Tests.Database.Entities.TphEntities;

namespace NosCore.Dao.Tests.Database
{
    public class NosCoreContext : DbContext
    {
        public NosCoreContext(DbContextOptions? options) : base(options)
        {
        }

        public virtual DbSet<SimpleEntity>? SimpleEntities { get; set; }

        public virtual DbSet<CompositeEntity>? CompositeEntities { get; set; }

        public virtual DbSet<TphBaseEntity>? TphBaseEntities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SimpleEntity>()
                .Property(e => e.Key)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<CompositeEntity>()
                .HasKey(e => new { e.Key1, e.Key2 });

            modelBuilder.Entity<TphBaseEntity>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<Tph1Entity>("Tph1Entity")
                .HasValue<Tph2Entity>("Tph2Entity");

            modelBuilder.Entity<CompositeTphBaseEntity>()
                .HasKey(e => new {e.Key1, e.Key2});

            modelBuilder.Entity<CompositeTphBaseEntity>()
                .HasDiscriminator<string>("Discriminator")
                .HasValue<CompositeTph1Entity>("CompositeTph1Entity")
                .HasValue<CompositeTph2Entity>("CompositeTph2Entity");

        }
    }
}