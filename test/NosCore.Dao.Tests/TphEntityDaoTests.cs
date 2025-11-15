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
            _dao = new Dao<TphBaseEntity, ITphDto, int>(new Mock<ILogger>().Object, _dbContextBuilder.CreateContext);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var baseDto = new TphBaseDto() { Key = 7, Value = "test" };
            var result = await _dao.TryInsertOrUpdateAsync(baseDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 1);
            Assert.AreEqual(7, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);
            Assert.IsInstanceOfType(result, typeof(TphBaseDto));

            var tph1Dto = new Tph1Dto() { Key = 8, Value = "test", SpecificPropertyTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(tph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 2);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(result, typeof(Tph1Dto));
            Assert.AreEqual(1, (result as Tph1Dto)!.SpecificPropertyTph1);

            var tph2Dto = new Tph2Dto() { Key = 9, Value = "test", SpecificPropertyTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(tph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key);
            Assert.AreEqual("test", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(result, typeof(Tph2Dto));
            Assert.AreEqual(2, (result as Tph2Dto)!.SpecificPropertyTph2);
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
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(7, loadAll.First().Key);
            Assert.AreEqual("test1", loadAll.First().Value);
            Assert.IsInstanceOfType(result, typeof(TphBaseDto));

            var tph1Dto = new Tph1Dto() { Key = 8, Value = "test2", SpecificPropertyTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(tph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test2", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(result, typeof(Tph1Dto));
            Assert.AreEqual(1, (result as Tph1Dto)!.SpecificPropertyTph1);

            var tph2Dto = new Tph2Dto() { Key = 9, Value = "test3", SpecificPropertyTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(tph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key);
            Assert.AreEqual("test3", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(result, typeof(Tph2Dto));
            Assert.AreEqual(2, (result as Tph2Dto)!.SpecificPropertyTph2);
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
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(7, loadAll.First().Key);
            Assert.AreEqual("test1", loadAll.First().Value);
            Assert.IsFalse(loadAll.First() is Tph1Entity || loadAll.First() is Tph2Entity);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test2", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(Tph1Entity));
            Assert.AreEqual(1, (loadAll.Skip(1).First() as Tph1Entity)?.SpecificPropertyTph1);

            Assert.AreEqual(9, loadAll.Skip(2).First().Key);
            Assert.AreEqual("test3", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(2).First(), typeof(Tph2Entity));
            Assert.AreEqual(2, (loadAll.Skip(2).First() as Tph2Entity)?.SpecificPropertyTph2);

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
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(7, loadAll.First().Key);
            Assert.AreEqual("test1", loadAll.First().Value);
            Assert.IsFalse(loadAll.First() is Tph1Entity || loadAll.First() is Tph2Entity);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test2", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(Tph1Entity));
            Assert.AreEqual(1, (loadAll.Skip(1).First() as Tph1Entity)?.SpecificPropertyTph1);

            Assert.AreEqual(9, loadAll.Skip(2).First().Key);
            Assert.AreEqual("test3", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(2).First(), typeof(Tph2Entity));
            Assert.AreEqual(2, (loadAll.Skip(2).First() as Tph2Entity)?.SpecificPropertyTph2);
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
            Assert.HasCount(loadAll, 2);
            Assert.AreEqual(7, deleted.Key);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsFalse(deleted is Tph2Dto || deleted is Tph1Dto);

            deleted = await _dao.TryDeleteAsync(8).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 1);
            Assert.AreEqual(8, deleted.Key);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(Tph1Dto));
            Assert.AreEqual(1, (deleted as Tph1Dto)!.SpecificPropertyTph1);

            deleted = await _dao.TryDeleteAsync(9).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>().ToList();
            Assert.HasCount(loadAll, 0);
            Assert.AreEqual(9, deleted.Key);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(Tph2Dto));
            Assert.AreEqual(2, (deleted as Tph2Dto)!.SpecificPropertyTph2);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<TphBaseEntity>().Add(new TphBaseEntity { Key = 7, Value = "test" });
            otherContext.Set<TphBaseEntity>().Add(new Tph1Entity() { Key = 8, Value = "test", SpecificPropertyTph1 = 1});
            otherContext.Set<TphBaseEntity>().Add(new Tph2Entity() { Key = 9, Value = "test", SpecificPropertyTph2 = 2});
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deletedEntities = (await _dao.TryDeleteAsync(new[] { 7, 9, 8 }).ConfigureAwait(false))!.ToList();
            var loadAll = _dbContextBuilder.CreateContext().Set<TphBaseEntity>()!.ToList();
            Assert.AreEqual(3, deletedEntities.Count());
            Assert.IsFalse(loadAll.Any());

            var deleted = deletedEntities.First(s => s.Key == 7);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsFalse(deleted is Tph2Dto || deleted is Tph1Dto);

            deleted = deletedEntities.First(s => s.Key == 8);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(Tph1Dto));
            Assert.AreEqual(1, (deleted as Tph1Dto)!.SpecificPropertyTph1);

            deleted = deletedEntities.First(s => s.Key == 9);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(Tph2Dto));
            Assert.AreEqual(2, (deleted as Tph2Dto)!.SpecificPropertyTph2);
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
            Assert.HasCount(loadAll, 3);
            Assert.AreEqual(7, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(Tph1Dto));

            Assert.AreEqual(9, loadAll.Skip(2).First().Key);
            Assert.AreEqual("test", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(2).First(), typeof(Tph2Dto));
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
            Assert.HasCount(loadAll, 2);
            Assert.AreEqual(7, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(Tph1Dto));
            Assert.AreEqual(1, (loadAll.Skip(1).First() as Tph1Dto)?.SpecificPropertyTph1);
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
            Assert.AreEqual(9, dto.Key);
            Assert.AreEqual("test", dto.Value);
            Assert.IsInstanceOfType(dto, typeof(Tph2Dto));
            Assert.AreEqual(2, (dto as Tph2Dto)?.SpecificPropertyTph2);
        }
    }
}
