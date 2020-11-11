//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Tests.Database;
using NosCore.Dao.Tests.Database.Entities.SimpleEntities;
using NosCore.Dao.Tests.TestsModels.SimpleModels;
using Serilog;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class SimpleWithFkEntityDaoTests
    {
        private Dao<SimpleWithFkEntity, SimpleWithFkDto, int> _dao = null!;
        private Func<DbContext> _dbContextBuilder = null!;

        [TestInitialize]
        public async Task SetupAsync()
        {
            _dbContextBuilder = new DbContextBuilder().CreateContext;
            var init = _dbContextBuilder();
            init.Add(new SimpleEntity()
            {
                Key = 1,
                Value = "test"
            });
            await init.SaveChangesAsync().ConfigureAwait(false);
            _dao =
                new Dao<SimpleWithFkEntity, SimpleWithFkDto, int>(new Mock<ILogger>().Object, _dbContextBuilder);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var simpleDto = new SimpleWithFkDto { Key = 8, Value = "test", Fk = 1 };
            await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "test");
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            var otherContext = _dbContextBuilder();
            otherContext.Set<SimpleWithFkEntity>().Add(new SimpleWithFkEntity { Key = 8, Value = "test", Fk = 1 });
            await otherContext.SaveChangesAsync().ConfigureAwait(false);
            var simpleDto = new SimpleWithFkDto { Key = 8, Value = "blabla", Fk = 1 };
            await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var simpleDtos = new List<SimpleWithFkDto>
            {
                new SimpleWithFkDto {Key = 8, Value = "blabla", Fk = 1},
                new SimpleWithFkDto {Key = 9, Value = "test", Fk = 1}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().OrderBy(s => s.Key).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
            Assert.IsTrue(loadAll.Skip(1).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task CanInsertAndReplaceMultipleDtos()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>().AddAsync(new SimpleWithFkEntity { Key = 8, Value = "thisisatest", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var simpleDtos = new List<SimpleWithFkDto>
            {
                new SimpleWithFkDto {Key = 8, Value = "blabla", Fk = 1},
                new SimpleWithFkDto {Key = 9, Value = "test", Fk = 1}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().OrderBy(s => s.Key).ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "blabla");
            Assert.IsTrue(loadAll.Skip(1).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>()
                .AddRangeAsync(new SimpleWithFkEntity { Key = 8, Value = "thisisatest", Fk = 1 }, new SimpleWithFkEntity { Key = 9, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.LoadAll().ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "thisisatest");
            Assert.IsTrue(loadAll.Skip(1).First().Key == 9);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
        }

        [TestMethod]
        public async Task AutoIncrementIsWorking()
        {
            var simpleDto = new SimpleWithFkDto { Key = 0, Value = "test", Fk = 1 };
            var result = await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 1);
            Assert.IsTrue(loadAll.First().Value == "test");

            Assert.IsTrue(result.Key == 1);
            Assert.IsTrue(result.Value == "test");
        }

        [TestMethod]
        public async Task CanDelete()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>().AddAsync(new SimpleWithFkEntity { Key = 8, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(8).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Key == 8);
            Assert.IsTrue(deleted.Value == "test");
        }

        [TestMethod]
        public async Task DeleteOnNotFoundReturnNull()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>().AddAsync(new SimpleWithFkEntity { Key = 8, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(9).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>().AddRangeAsync(new SimpleWithFkEntity { Key = 8, Value = "test", Fk = 1 }, new SimpleWithFkEntity { Key = 9, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync(new[] { 9, 8 }).ConfigureAwait(false);
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted!.Count() == 2);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeysButSomeMissingObjects()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>().AddAsync(new SimpleWithFkEntity { Key = 8, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = (await _dao.TryDeleteAsync(new[] { 9, 8 }).ConfigureAwait(false))!.ToList();
            var loadAll = _dbContextBuilder().Set<SimpleWithFkEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsNotNull(deleted);
            Assert.IsTrue(deleted.Count() == 1);
            Assert.IsTrue(deleted.First().Key == 8);
            Assert.IsTrue(deleted.First().Value == "test");
        }

        [TestMethod]
        public async Task CanUseWhereClause()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>()
                .AddRangeAsync(new SimpleWithFkEntity { Key = 8, Value = "thisisatest", Fk = 1 }, new SimpleWithFkEntity { Key = 9, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.Where(s => s.Key == 9).ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 9);
            Assert.IsTrue(loadAll.First().Value == "test");
        }

        [TestMethod]
        public async Task FirstOrDefaultReturnObject()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>()
                .AddRangeAsync(new SimpleWithFkEntity { Key = 8, Value = "thisisatest", Fk = 1 }, new SimpleWithFkEntity { Key = 9, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _dao.FirstOrDefaultAsync(s => s.Key == 9).ConfigureAwait(false);
            Assert.IsNotNull(dto);
            Assert.IsTrue(dto.Key == 9);
            Assert.IsTrue(dto.Value == "test");
        }

        [TestMethod]

        public async Task FirstOrDefaultReturnNullWhenNotFoundObject()
        {
            var otherContext = _dbContextBuilder();
            await otherContext.Set<SimpleWithFkEntity>()
                .AddRangeAsync(new SimpleWithFkEntity { Key = 8, Value = "thisisatest", Fk = 1 }, new SimpleWithFkEntity { Key = 9, Value = "test", Fk = 1 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _dao.FirstOrDefaultAsync(s => s.Key == 10).ConfigureAwait(false);
            Assert.IsNull(dto);
        }
    }
}
