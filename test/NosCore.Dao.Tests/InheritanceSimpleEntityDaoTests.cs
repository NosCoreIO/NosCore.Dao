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
    public class InheritanceSimpleEntityDaoTests
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
            var simpleDto = new SimpleObject { Key = 8, Value = "test" };
            await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(loadAll, 1);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            otherContext.Set<SimpleEntity>().Add(new SimpleEntity { Key = 8, Value = "test" });
            await otherContext.SaveChangesAsync().ConfigureAwait(false);
            var simpleDto = new SimpleObject { Key = 8, Value = "blabla" };
            await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(loadAll, 1);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("blabla", loadAll.First().Value);
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var simpleDtos = new List<SimpleObject>
            {
                new SimpleObject {Key = 8, Value = "blabla"},
                new SimpleObject {Key = 9, Value = "test"}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().OrderBy(s => s.Key).ToList();
            Assert.HasCount(loadAll, 2);
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

            var simpleDtos = new List<SimpleObject>
            {
                new SimpleObject {Key = 8, Value = "blabla"},
                new SimpleObject {Key = 9, Value = "test"}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().OrderBy(s => s.Key).ToList();
            Assert.HasCount(loadAll, 2);
            Assert.AreEqual(8, loadAll.First().Key);
            Assert.AreEqual("blabla", loadAll.First().Value);
            Assert.AreEqual(9, loadAll.Skip(1).First().Key);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
        }

        [TestMethod]
        public async Task AutoIncrementIsWorking()
        {
            var simpleDto = new SimpleObject { Key = 0, Value = "test" };
            var result = await _dao.TryInsertOrUpdateAsync(simpleDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(loadAll, 1);
            Assert.AreEqual(1, loadAll.First().Key);
            Assert.AreEqual("test", loadAll.First().Value);

            Assert.AreEqual(1, result.Key);
            Assert.AreEqual("test", result.Value);
        }
    }
}
