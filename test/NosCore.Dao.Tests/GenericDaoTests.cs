//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Tests.Database;
using NosCore.Dao.Tests.TestsModels;
using Serilog;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class GenericDaoTests
    {
        private GenericDao<SimpleEntity, SimpleDto, int> _genericDao = null!;
        private DbContextBuilder _dbContextBuilder = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbContextBuilder = new DbContextBuilder();
            _genericDao =
                new GenericDao<SimpleEntity, SimpleDto, int>(new Mock<ILogger>().Object, _dbContextBuilder);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var simpleDto = new SimpleDto { Key = 8, Value = "test" };
            await _genericDao.TryInsertOrUpdateAsync(simpleDto)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "test");
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            _dbContextBuilder.CreateContext().Set<SimpleEntity>().Add(new SimpleEntity { Key = 8, Value = "test" });
            var simpleDto = new SimpleDto { Key = 8, Value = "blabla" };
            await _genericDao.TryInsertOrUpdateAsync(simpleDto)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var simpleDtos = new List<SimpleDto>
            {
                new SimpleDto {Key = 8, Value = "blabla"},
                new SimpleDto {Key = 9, Value = "test"}
            };

            await _genericDao.TryInsertOrUpdateAsync(simpleDtos)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().OrderBy(s => s.Key).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
            Assert.IsTrue(loadAll.Skip(1).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
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

            await _genericDao.TryInsertOrUpdateAsync(simpleDtos)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().OrderBy(s => s.Key).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
            Assert.IsTrue(loadAll.Skip(1).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _genericDao.LoadAll().ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "thisisatest");
            Assert.IsTrue(loadAll.Skip(1).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task AutoIncrementIsWorking()
        {
            var simpleDto = new SimpleDto { Key = 0, Value = "test" };
            var result = await _genericDao.TryInsertOrUpdateAsync(simpleDto)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 1);
            Assert.IsTrue(loadAll.First().Value == "test");

            Assert.IsTrue(result.Key == 1);
            Assert.IsTrue(result.Value == "test");
        }

        [TestMethod]
        public async Task CanDelete()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _genericDao.TryDeleteAsync(8)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Key == 8);
            Assert.IsTrue(deleted.Value == "test");
        }

        [TestMethod]
        public async Task DeleteOnNotFoundReturnNull()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _genericDao.TryDeleteAsync(9)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddRangeAsync(new SimpleEntity { Key = 8, Value = "test" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _genericDao.TryDeleteAsync(new[] { 9, 8 })!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Count() == 2);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeysButSomeMissingObjects()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>().AddAsync(new SimpleEntity { Key = 8, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = (await _genericDao.TryDeleteAsync(new[] { 9, 8 })!.ConfigureAwait(false)).ToList();
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsNotNull(deleted);
            Assert.IsTrue(deleted.Count() == 1);
            Assert.IsTrue(deleted.First().Key == 8);
            Assert.IsTrue(deleted.First().Value == "test");
        }

        [TestMethod]
        public async Task CanUseWhereClause()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _genericDao.Where(s => s.Key == 9).ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 9);
            Assert.IsTrue(loadAll.First().Value == "test");
        }

        [TestMethod]
        public async Task FirstOrDefaultReturnObject()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _genericDao.FirstOrDefaultAsync(s => s.Key == 9).ConfigureAwait(false);
            Assert.IsNotNull(dto);
            Assert.IsTrue(dto.Key == 9);
            Assert.IsTrue(dto.Value == "test");
        }

        [TestMethod]

        public async Task FirstOrDefaultReturnNullWhenNotFoundObject()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<SimpleEntity>()
                .AddRangeAsync(new SimpleEntity { Key = 8, Value = "thisisatest" }, new SimpleEntity { Key = 9, Value = "test" }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _genericDao.FirstOrDefaultAsync(s => s.Key == 10).ConfigureAwait(false);
            Assert.IsNull(dto);
        }
    }
}
