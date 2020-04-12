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
using NosCore.Dao.Tests.Database.Entities.CompositeTphEntities;
using NosCore.Dao.Tests.TestsModels.CompositeModels;
using Serilog;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class CompositeEntityDaoTests
    {
        private Dao<CompositeEntity, CompositeDto, (int, int)> _dao = null!;
        private DbContextBuilder _dbContextBuilder = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbContextBuilder = new DbContextBuilder();
            _dao =
                new Dao<CompositeEntity, CompositeDto, (int, int)>(new Mock<ILogger>().Object, _dbContextBuilder);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var compositeDto = new CompositeDto { Key1 = 8, Key2 = 8, Value = "test" };
            await _dao.TryInsertOrUpdateAsync(compositeDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key1 == 8);
            Assert.IsTrue(loadAll.First().Key2 == 8);
            Assert.IsTrue(loadAll.First().Value == "test");
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<CompositeEntity>().Add(new CompositeEntity { Key1 = 8, Key2 = 8, Value = "test" });
            var compositeDto = new CompositeDto { Key1 = 8, Key2 = 8, Value = "blabla" };
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            await _dao.TryInsertOrUpdateAsync(compositeDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key1 == 8);
            Assert.IsTrue(loadAll.First().Key2 == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var compositeDtos = new List<CompositeDto>
            {
                new CompositeDto {Key1 = 8, Key2 = 8, Value = "blabla"},
                new CompositeDto {Key1 = 9, Key2 = 9, Value = "test"}
            };

            await _dao.TryInsertOrUpdateAsync(compositeDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().OrderBy(s => s.Key1).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key1 == 8);
            Assert.IsTrue(loadAll.First().Key2 == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task CanInsertAndReplaceMultipleDtos()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeEntity>().AddAsync(new CompositeEntity { Key1 = 8, Key2 = 8, Value = "thisisatest" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var compositeDtos = new List<CompositeDto>
            {
                new CompositeDto {Key1 = 8,Key2 = 8, Value = "blabla"},
                new CompositeDto {Key1 = 9,Key2 = 9, Value = "test"}
            };

            await _dao.TryInsertOrUpdateAsync(compositeDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().OrderBy(s => s.Key1).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key1 == 8);
            Assert.IsTrue(loadAll.First().Key2 == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeEntity>()
                .AddRangeAsync(new CompositeEntity { Key1 = 8, Key2 = 8, Value = "thisisatest" }, new CompositeEntity { Key2 = 9, Key1 = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.LoadAll().ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key1 == 8);
            Assert.IsTrue(loadAll.First().Key2 == 8);
            Assert.IsTrue(loadAll.First().Value == "thisisatest");
            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task CanDelete()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeEntity>().AddAsync(new CompositeEntity { Key1 = 8, Key2 = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var id = (8, 9);
            var deleted = await _dao.TryDeleteAsync(id).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Key1 == 8);
            Assert.IsTrue(deleted.Key2 == 9);
            Assert.IsTrue(deleted.Value == "test");
        }

        [TestMethod]
        public async Task DeleteOnNotFoundReturnNull()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeEntity>().AddAsync(new CompositeEntity { Key1 = 8, Key2 = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);
            var id = (9, 9);
            var deleted = await _dao.TryDeleteAsync(id).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeEntity>().AddRangeAsync(new CompositeEntity { Key1 = 8, Key2 = 8, Value = "test" }, new CompositeEntity { Key1 = 9, Key2 = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);
            var ids = new List<(int, int)> { (9, 9), (8, 8) };
            var deleted = await _dao.TryDeleteAsync(ids).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Count() == 2);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeysButSomeMissingObjects()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeEntity>().AddAsync(new CompositeEntity { Key1 = 8, Key2 = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);
            var ids = new List<(int, int)> { (9, 9), (8, 8) };
            var deleted = (await _dao.TryDeleteAsync(ids).ConfigureAwait(false)).ToList();
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsNotNull(deleted);
            Assert.IsTrue(deleted.Count() == 1);
            Assert.IsTrue(deleted.First().Key1 == 8);
            Assert.IsTrue(deleted.First().Key2 == 8);
            Assert.IsTrue(deleted.First().Value == "test");
        }
    }
}
