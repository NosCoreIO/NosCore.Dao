//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Tests.Database;
using NosCore.Dao.Tests.Database.Entities.SimpleEntities;
using NosCore.Dao.Tests.TestsModels.SimpleModels;
using Microsoft.Extensions.Logging;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class DaoTransactionScopeTests
    {
        private Dao<SimpleEntity, SimpleDto, int> _dao = null!;
        private DbContextBuilder _dbContextBuilder = null!;
        private DaoTransactionScope _scope = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbContextBuilder = new DbContextBuilder();
            _dao = new Dao<SimpleEntity, SimpleDto, int>(
                new Mock<ILogger<Dao<SimpleEntity, SimpleDto, int>>>().Object, _dbContextBuilder.CreateContext);
            _scope = new DaoTransactionScope(_dbContextBuilder.CreateContext);
        }

        [TestMethod]
        public async Task CommittedScopePersistsEveryOperation()
        {
            await using (var transaction = _scope.Begin())
            {
                await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "first" }).ConfigureAwait(false);
                await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 2, Value = "second" }).ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }

            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(2, loadAll);
        }

        [TestMethod]
        public async Task UncommittedScopeRollsEveryOperationBack()
        {
            await using (_scope.Begin())
            {
                await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "first" }).ConfigureAwait(false);
                await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 2, Value = "second" }).ConfigureAwait(false);
            }

            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsEmpty(loadAll);
        }

        [TestMethod]
        public async Task OperationsInsideTheScopeSeeEachOther()
        {
            await using (var transaction = _scope.Begin())
            {
                await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "first" }).ConfigureAwait(false);
                var found = await _dao.FirstOrDefaultAsync(s => s.Key == 1).ConfigureAwait(false);
                Assert.IsNotNull(found);
                Assert.AreEqual("first", found.Value);
                await transaction.CommitAsync().ConfigureAwait(false);
            }
        }

        [TestMethod]
        public async Task OperationsAfterTheScopeUseFreshContexts()
        {
            await using (_scope.Begin())
            {
                await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "rolled back" }).ConfigureAwait(false);
            }

            await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 2, Value = "kept" }).ConfigureAwait(false);

            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(2, loadAll.First().Key);
        }

        [TestMethod]
        public async Task ConcurrentFlowsDoNotShareAScope()
        {
            var second = new DbContextBuilder();
            var secondDao = new Dao<SimpleEntity, SimpleDto, int>(
                new Mock<ILogger<Dao<SimpleEntity, SimpleDto, int>>>().Object, second.CreateContext);

            async Task RolledBackFlow()
            {
                await Task.Yield();
                await using (_scope.Begin())
                {
                    await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "rolled back" }).ConfigureAwait(false);
                }
            }

            async Task PlainFlow()
            {
                await Task.Yield();
                await secondDao.TryInsertOrUpdateAsync(new SimpleDto { Key = 3, Value = "kept" }).ConfigureAwait(false);
            }

            await Task.WhenAll(RolledBackFlow(), PlainFlow()).ConfigureAwait(false);

            Assert.IsEmpty(_dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList());
            Assert.HasCount(1, second.CreateContext().Set<SimpleEntity>().ToList());
        }
    }
}
