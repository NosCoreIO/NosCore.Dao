//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Tests.Database;
using NosCore.Dao.Tests.Database.Entities.TphEntities;
using NosCore.Dao.Tests.TestsModels.TphModels;
using Serilog;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class TphEntityDaoTests
    {
        private Dao<TphBaseEntity, ITphDto, int> _dao = null!;
        private DbContextBuilder _dbContextBuilder = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbContextBuilder = new DbContextBuilder();
            _dao = new Dao<TphBaseEntity, ITphDto, int>(new Mock<ILogger>().Object, _dbContextBuilder);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var baseDto = new TphBaseDto() { Key = 7, Value = "test" };
            var result = await _dao.TryInsertOrUpdateAsync(baseDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 7);
            Assert.IsTrue(loadAll.First().Value == "test");
            Assert.IsTrue(result is TphBaseDto);

            var tph1Dto = new Tph1Dto() { Key = 8, Value = "test", SpecificPropertyTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(tph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.Skip(1).First().Key == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
            Assert.IsTrue(result is Tph1Dto);
            Assert.IsTrue((result as Tph1Dto)!.SpecificPropertyTph1 == 1);

            var tph2Dto = new Tph2Dto() { Key = 9, Value = "test", SpecificPropertyTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(tph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.Skip(2).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test");
            Assert.IsTrue(result is Tph2Dto);
            Assert.IsTrue((result as Tph2Dto)!.SpecificPropertyTph2 == 2);
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            var context = _dbContextBuilder.CreateContext();
            context.Set<TphBaseEntity>().Add(new TphBaseEntity { Key = 7, Value = "test" });
            context.Set<TphBaseEntity>().Add(new Tph1Entity() { Key = 8, Value = "test" });
            context.Set<TphBaseEntity>().Add(new Tph2Entity() { Key = 9, Value = "test" });
            await context.SaveChangesAsync().ConfigureAwait(false);

            var baseDto = new TphBaseDto() { Key = 7, Value = "test1" };
            var result = await _dao.TryInsertOrUpdateAsync(baseDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key == 7);
            Assert.IsTrue(loadAll.First().Value == "test1");
            Assert.IsTrue(result is TphBaseDto);

            var tph1Dto = new Tph1Dto() { Key = 8, Value = "test2", SpecificPropertyTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(tph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.Skip(1).First().Key == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test2");
            Assert.IsTrue(result is Tph1Dto);
            Assert.IsTrue((result as Tph1Dto)!.SpecificPropertyTph1 == 1);

            var tph2Dto = new Tph2Dto() { Key = 9, Value = "test3", SpecificPropertyTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(tph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.Skip(2).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test3");
            Assert.IsTrue(result is Tph2Dto);
            Assert.IsTrue((result as Tph2Dto)!.SpecificPropertyTph2 == 2);
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var simpleDtos = new List<TphBaseDto>
            {
                new TphBaseDto {Key = 7, Value = "test1"},
                new Tph1Dto {Key = 8, Value = "test2", SpecificPropertyTph1 = 1},
                new Tph2Dto {Key = 9, Value = "test3", SpecificPropertyTph2 = 2}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().OrderBy(s => s.Key).ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key == 7);
            Assert.IsTrue(loadAll.First().Value == "test1");
            Assert.IsFalse(loadAll.First() is Tph1Entity || loadAll.First() is Tph2Entity);

            Assert.IsTrue(loadAll.Skip(1).First().Key == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test2");
            Assert.IsTrue(loadAll.Skip(1).First() is Tph1Entity);
            Assert.IsTrue((loadAll.Skip(1).First() as Tph1Entity)?.SpecificPropertyTph1 == 1);

            Assert.IsTrue(loadAll.Skip(2).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test3");
            Assert.IsTrue(loadAll.Skip(2).First() is Tph2Entity);
            Assert.IsTrue((loadAll.Skip(2).First() as Tph2Entity)?.SpecificPropertyTph2 == 2);

        }

        [TestMethod]
        public async Task CanInsertAndReplaceMultipleDtos()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<TphBaseEntity>().AddAsync(new TphBaseEntity { Key = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<TphBaseEntity>().AddAsync(new Tph1Entity { Key = 8, Value = "test", SpecificPropertyTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<TphBaseEntity>().AddAsync(new Tph2Entity { Key = 9, Value = "test", SpecificPropertyTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var simpleDtos = new List<TphBaseDto>
            {
                new TphBaseDto {Key = 7, Value = "test1"},
                new Tph1Dto {Key = 8, Value = "test2", SpecificPropertyTph1 = 1},
                new Tph2Dto {Key = 9, Value = "test3", SpecificPropertyTph2 = 2}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().OrderBy(s => s.Key).ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key == 7);
            Assert.IsTrue(loadAll.First().Value == "test1");
            Assert.IsFalse(loadAll.First() is Tph1Entity || loadAll.First() is Tph2Entity);

            Assert.IsTrue(loadAll.Skip(1).First().Key == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test2");
            Assert.IsTrue(loadAll.Skip(1).First() is Tph1Entity);
            Assert.IsTrue((loadAll.Skip(1).First() as Tph1Entity)?.SpecificPropertyTph1 == 1);

            Assert.IsTrue(loadAll.Skip(2).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test3");
            Assert.IsTrue(loadAll.Skip(2).First() is Tph2Entity);
            Assert.IsTrue((loadAll.Skip(2).First() as Tph2Entity)?.SpecificPropertyTph2 == 2);
        }

        [TestMethod]
        public async Task CanDelete()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<TphBaseEntity>().AddAsync(new TphBaseEntity { Key = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<TphBaseEntity>().AddAsync(new Tph1Entity { Key = 8, Value = "test", SpecificPropertyTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<TphBaseEntity>().AddAsync(new Tph2Entity { Key = 9, Value = "test", SpecificPropertyTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(7).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(deleted.Key == 7);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsFalse(deleted is Tph2Dto || deleted is Tph1Dto);

            deleted = await _dao.TryDeleteAsync(8).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(deleted.Key == 8);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is Tph1Dto);
            Assert.IsTrue((deleted as Tph1Dto)!.SpecificPropertyTph1 == 1);

            deleted = await _dao.TryDeleteAsync(9).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Key == 9);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is Tph2Dto);
            Assert.IsTrue((deleted as Tph2Dto)!.SpecificPropertyTph2 == 2);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<TphBaseEntity>().Add(new TphBaseEntity { Key = 7, Value = "test" });
            otherContext.Set<TphBaseEntity>().Add(new Tph1Entity() { Key = 8, Value = "test", SpecificPropertyTph1 = 1});
            otherContext.Set<TphBaseEntity>().Add(new Tph2Entity() { Key = 9, Value = "test", SpecificPropertyTph2 = 2});
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deletedEntities = (await _dao.TryDeleteAsync(new[] { 7, 9, 8 }).ConfigureAwait(false)).ToList();
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.IsTrue(deletedEntities.Count() == 3);
            Assert.IsTrue(!loadAll.Any());

            var deleted = deletedEntities.First(s => s.Key == 7);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsFalse(deleted is Tph2Dto || deleted is Tph1Dto);

            deleted = deletedEntities.First(s => s.Key == 8);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is Tph1Dto);
            Assert.IsTrue((deleted as Tph1Dto)!.SpecificPropertyTph1 == 1);

            deleted = deletedEntities.First(s => s.Key == 9);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is Tph2Dto);
            Assert.IsTrue((deleted as Tph2Dto)!.SpecificPropertyTph2 == 2);
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<TphBaseEntity>().Add(new TphBaseEntity { Key = 7, Value = "test" });
            otherContext.Set<TphBaseEntity>().Add(new Tph1Entity() { Key = 8, Value = "test", SpecificPropertyTph1 = 1 });
            otherContext.Set<TphBaseEntity>().Add(new Tph2Entity() { Key = 9, Value = "test", SpecificPropertyTph2 = 2 });
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.LoadAll().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key == 7);
            Assert.IsTrue(loadAll.First().Value == "test");

            Assert.IsTrue(loadAll.Skip(1).First().Key == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
            Assert.IsTrue(loadAll.Skip(1).First() is Tph1Dto);

            Assert.IsTrue(loadAll.Skip(2).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test");
            Assert.IsTrue(loadAll.Skip(2).First() is Tph2Dto);
        }

        [TestMethod]
        public async Task CanUseWhereClause()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<TphBaseEntity>().Add(new TphBaseEntity { Key = 7, Value = "test" });
            otherContext.Set<TphBaseEntity>().Add(new Tph1Entity() { Key = 8, Value = "test", SpecificPropertyTph1 = 1 });
            otherContext.Set<TphBaseEntity>().Add(new Tph2Entity() { Key = 9, Value = "test", SpecificPropertyTph2 = 2 });
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.Where(s=>s.Key == 7 || s.Key == 8).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 7);
            Assert.IsTrue(loadAll.First().Value == "test");

            Assert.IsTrue(loadAll.Skip(1).First().Key == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
            Assert.IsTrue(loadAll.Skip(1).First() is Tph1Dto);
            Assert.IsTrue((loadAll.Skip(1).First() as Tph1Dto)?.SpecificPropertyTph1 == 1);
        }

        [TestMethod]
        public async Task FirstOrDefaultReturnObject()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<TphBaseEntity>().Add(new TphBaseEntity { Key = 7, Value = "test" });
            otherContext.Set<TphBaseEntity>().Add(new Tph1Entity() { Key = 8, Value = "test", SpecificPropertyTph1 = 1 });
            otherContext.Set<TphBaseEntity>().Add(new Tph2Entity() { Key = 9, Value = "test", SpecificPropertyTph2 = 2 });
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _dao.FirstOrDefaultAsync(s => s.Key == 9).ConfigureAwait(false);
            Assert.IsNotNull(dto);
            Assert.IsTrue(dto.Key == 9);
            Assert.IsTrue(dto.Value == "test");
            Assert.IsTrue(dto is Tph2Dto);
            Assert.IsTrue((dto as Tph2Dto)?.SpecificPropertyTph2 == 2);
        }
    }
}
