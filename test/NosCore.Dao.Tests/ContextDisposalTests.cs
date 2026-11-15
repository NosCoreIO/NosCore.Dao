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
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NosCore.Dao.Tests.Database;
using NosCore.Dao.Tests.Database.Entities.SimpleEntities;
using NosCore.Dao.Tests.TestsModels.SimpleModels;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class ContextDisposalTests
    {
        private readonly List<DbContext> _created = new();
        private Dao<SimpleEntity, SimpleDto, int> _dao = null!;

        [TestInitialize]
        public void Setup()
        {
            var builder = new DbContextBuilder();
            _dao = new Dao<SimpleEntity, SimpleDto, int>(
                new Mock<ILogger<Dao<SimpleEntity, SimpleDto, int>>>().Object,
                () =>
                {
                    var context = builder.CreateContext();
                    _created.Add(context);
                    return context;
                });
        }

        [TestMethod]
        public async Task EveryContextTheDaoCreatesIsDisposed()
        {
            await _dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "a" });
            await _dao.TryInsertOrUpdateAsync(new[] { new SimpleDto { Key = 2, Value = "b" } });
            await _dao.FirstOrDefaultAsync(s => s.Key == 1);
            Assert.HasCount(2, _dao.LoadAll().ToList());
            Assert.HasCount(1, _dao.Where(s => s.Key == 2).ToList());
            await _dao.TryDeleteAsync(1);
            await _dao.TryDeleteAsync(new[] { 2 });

            Assert.IsNotEmpty(_created);
            foreach (var context in _created)
            {
                Assert.ThrowsExactly<ObjectDisposedException>(() => _ = context.Model);
            }
        }
    }
}
