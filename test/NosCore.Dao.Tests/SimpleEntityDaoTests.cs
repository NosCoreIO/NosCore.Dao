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
using NosCore.Dao.Tests.Database.Entities.SimpleEntities;
using NosCore.Dao.Tests.TestsModels.SimpleModels;
using Serilog;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class SimpleEntityDaoTests
    {
        private Dao<SimpleEntity, SimpleDto, int> _dao = null!;
        private DbContextBuilder _dbContextBuilder = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbContextBuilder = new DbContextBuilder();
            _dao =
                new Dao<SimpleEntity, SimpleDto, int>(new Mock<ILogger>().Object, _dbContextBuilder.CreateContext);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var simpleDto = new SimpleDto { Key = 8, Value = "test" };
            await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<SimpleEntity>().Add(new SimpleEntity { Key = 8, Value = "test" });
            await otherContext.SaveChangesAsync().ConfigureAwait(false);
            var simpleDto = new SimpleDto { Key = 8, Value = "blabla" };
            await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("blabla", loadAll.First().Value);
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var simpleDtos = new List<SimpleDto>
            {
                new SimpleDto {Key = 8, Value = "blabla"},
                new SimpleDto {Key = 9, Value = "test"}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().OrderBy(s => s.Key).ToList();
            Assert.HasCount(2, loadAll);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("blabla", loadAll.First().Value);
            Assert.AreEqual(9, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
        }

        [TestMethod]
        public async Task CanInsertAndReplaceMultipleDtos()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var simpleDtos = new List<SimpleDto>
            {
                new SimpleDto {Key = 8, Value = "blabla"},
                new SimpleDto {Key = 9, Value = "test"}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().OrderBy(s => s.Key).ToList();
            Assert.HasCount(2, loadAll);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("blabla", loadAll.First().Value);
            Assert.AreEqual(9, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.LoadAll().ToList();
            Assert.HasCount(2, loadAll);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("thisisatest", loadAll.First().Value);
            Assert.AreEqual(9, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
        }

        [TestMethod]
        public async Task AutoIncrementIsWorking()
        {
            var simpleDto = new SimpleDto { Key = 0, Value = "test" };
            var result = await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(1, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);

            Assert.AreEqual(1, result.Key);
            Assert.AreEqual("test", result.Value);
        }

        [TestMethod]
        public async Task CanDelete()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(8).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(0, loadAll);
            Assert.AreEqual(8, deleted.Key);
            Assert.AreEqual("test", deleted.Value);
        }

        [TestMethod]
        public async Task DeleteOnNotFoundReturnNull()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(9).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddRangeAsync(new SimpleEntity { Key = 8, Value = "test" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(new[] { 9, 8 }).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(0, loadAll);
            Assert.AreEqual(2, deleted!.Count());
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeysButSomeMissingObjects()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = (await _dao.TryDeleteAsync(new[] { 9, 8 }).ConfigureAwait(false))!.ToList();
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(0, loadAll);
            Assert.IsNotNull(deleted);
            Assert.AreEqual(1, deleted.Count());
            Assert.AreEqual(8, deleted.First().Key);
            Assert.AreEqual("test", deleted.First().Value);
        }

        [TestMethod]
        public async Task CanUseWhereClause()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.Where(s => s.Key == 9).ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(9, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);
        }

        [TestMethod]
        public async Task FirstOrDefaultReturnObject()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _dao.FirstOrDefaultAsync(s => s.Key == 9).ConfigureAwait(false);
            Assert.IsNotNull(dto);
            Assert.AreEqual(9, dto.Key);
            Assert.AreEqual("test", dto.Value);
        }

        [TestMethod]

        public async Task FirstOrDefaultReturnNullWhenNotFoundObject()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _dao.FirstOrDefaultAsync(s => s.Key == 10).ConfigureAwait(false);
            Assert.IsNull(dto);
        }
    }
}
