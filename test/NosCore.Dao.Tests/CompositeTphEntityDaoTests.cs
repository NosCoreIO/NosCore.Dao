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
using NosCore.Dao.Tests.Database.Entities.CompositeEntities;
using NosCore.Dao.Tests.Database.Entities.CompositeTphEntities;
using NosCore.Dao.Tests.TestsModels.CompositeTphModels;
using Serilog;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class CompositeTphEntityDaoTests
    {
        private Dao<CompositeTphBaseEntity, ICompositeTphDto, (int, int)> _dao = null!;
        private DbContextBuilder _dbContextBuilder = null!;

        [TestInitialize]
        public void Setup()
        {
            _dbContextBuilder = new DbContextBuilder();
            _dao = new Dao<CompositeTphBaseEntity, ICompositeTphDto, (int, int)>(new Mock<ILogger>().Object, _dbContextBuilder.CreateContext);
        }

        [TestMethod]
        public async Task CanInsertDto()
        {
            var baseDto = new CompositeTphBaseDto() { Key1 = 7, Key2 = 7, Value = "test" };
            var result = await _dao.TryInsertOrUpdateAsync(baseDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(7, loadAll.First().Key1);
            Assert.AreEqual(7, loadAll.First().Key2);
            Assert.AreEqual("test", loadAll.First().Value);
            Assert.IsInstanceOfType(result, typeof(CompositeTphBaseDto));

            var CompositeTph1Dto = new CompositeTph1Dto() { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.HasCount(2, loadAll);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(result, typeof(CompositeTph1Dto));
            Assert.AreEqual(1, (result as CompositeTph1Dto)!.SpecificPropertyCompositeTph1);

            var CompositeTph2Dto = new CompositeTph2Dto() { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key1);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key2);
            Assert.AreEqual("test", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(result, typeof(CompositeTph2Dto));
            Assert.AreEqual(2, (result as CompositeTph2Dto)!.SpecificPropertyCompositeTph2);
        }

        [TestMethod]
        public async Task CanReplaceDto()
        {
            var context = _dbContextBuilder.CreateContext();
            context.Set<CompositeTphBaseEntity>().Add(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" });
            context.Set<CompositeTphBaseEntity>().Add(new CompositeTph1Entity() { Key1 = 8, Key2 = 8, Value = "test" });
            context.Set<CompositeTphBaseEntity>().Add(new CompositeTph2Entity() { Key1 = 9, Key2 = 9, Value = "test" });
            await context.SaveChangesAsync().ConfigureAwait(false);

            var baseDto = new CompositeTphBaseDto() { Key1 = 7, Key2 = 7, Value = "test1" };
            var result = await _dao.TryInsertOrUpdateAsync(baseDto).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().OrderBy(x => x.Key1).ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(7, loadAll.First().Key1);
            Assert.AreEqual(7, loadAll.First().Key2);
            Assert.AreEqual("test1", loadAll.First().Value);
            Assert.IsInstanceOfType(result, typeof(CompositeTphBaseDto));

            var CompositeTph1Dto = new CompositeTph1Dto() { Key1 = 8, Key2 = 8, Value = "test2", SpecificPropertyCompositeTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().OrderBy(x => x.Key1).ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key2);
            Assert.AreEqual("test2", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(result, typeof(CompositeTph1Dto));
            Assert.AreEqual(1, (result as CompositeTph1Dto)!.SpecificPropertyCompositeTph1);

            var CompositeTph2Dto = new CompositeTph2Dto() { Key1 = 9, Key2 = 9, Value = "test3", SpecificPropertyCompositeTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().OrderBy(x => x.Key1).ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key1);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key2);
            Assert.AreEqual("test3", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(result, typeof(CompositeTph2Dto));
            Assert.AreEqual(2, (result as CompositeTph2Dto)!.SpecificPropertyCompositeTph2);
        }

        [TestMethod]
        public async Task CanInsertMultipleDtos()
        {
            var simpleDtos = new List<CompositeTphBaseDto>
            {
                new CompositeTphBaseDto {Key1 = 7, Key2 = 7, Value = "test1"},
                new CompositeTph1Dto {Key1 = 8, Key2 = 8,Value = "test2", SpecificPropertyCompositeTph1 = 1},
                new CompositeTph2Dto {Key1 = 9,Key2 = 9, Value = "test3", SpecificPropertyCompositeTph2 = 2}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().OrderBy(s => s.Key1).ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(7, loadAll.First().Key1);
            Assert.AreEqual(7, loadAll.First().Key2);
            Assert.AreEqual("test1", loadAll.First().Value);
            Assert.IsFalse(loadAll.First() is CompositeTph1Entity || loadAll.First() is CompositeTph2Entity);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key2);
            Assert.AreEqual("test2", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(CompositeTph1Entity));
            Assert.AreEqual(1, (loadAll.Skip(1).First() as CompositeTph1Entity)?.SpecificPropertyCompositeTph1);

            Assert.AreEqual(9, loadAll.Skip(2).First().Key1);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key2);
            Assert.AreEqual("test3", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(2).First(), typeof(CompositeTph2Entity));
            Assert.AreEqual(2, (loadAll.Skip(2).First() as CompositeTph2Entity)?.SpecificPropertyCompositeTph2);

        }

        [TestMethod]
        public async Task CanInsertAndReplaceMultipleDtos()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var simpleDtos = new List<CompositeTphBaseDto>
            {
                new CompositeTphBaseDto {Key1 = 7,Key2 = 7, Value = "test1"},
                new CompositeTph1Dto {Key1 = 8,Key2 = 8, Value = "test2", SpecificPropertyCompositeTph1 = 1},
                new CompositeTph2Dto {Key1 = 9,Key2 = 9, Value = "test3", SpecificPropertyCompositeTph2 = 2}
            };

            await _dao.TryInsertOrUpdateAsync(simpleDtos).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().OrderBy(s => s.Key1).ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(7, loadAll.First().Key1);
            Assert.AreEqual(7, loadAll.First().Key2);
            Assert.AreEqual("test1", loadAll.First().Value);
            Assert.IsFalse(loadAll.First() is CompositeTph1Entity || loadAll.First() is CompositeTph2Entity);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key2);
            Assert.AreEqual("test2", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(CompositeTph1Entity));
            Assert.AreEqual(1, (loadAll.Skip(1).First() as CompositeTph1Entity)?.SpecificPropertyCompositeTph1);

            Assert.AreEqual(9, loadAll.Skip(2).First().Key1);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key2);
            Assert.AreEqual("test3", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(2).First(), typeof(CompositeTph2Entity));
            Assert.AreEqual(2, (loadAll.Skip(2).First() as CompositeTph2Entity)?.SpecificPropertyCompositeTph2);
        }

        [TestMethod]
        public async Task CanDelete()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deleted = await _dao.TryDeleteAsync((7, 7)).ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.HasCount(2, loadAll);
            Assert.AreEqual(7, deleted.Key1);
            Assert.AreEqual(7, deleted.Key2);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsFalse(deleted is CompositeTph2Dto || deleted is CompositeTph1Dto);

            deleted = await _dao.TryDeleteAsync((8,8)).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.HasCount(1, loadAll);
            Assert.AreEqual(8, deleted.Key1);
            Assert.AreEqual(8, deleted.Key2);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(CompositeTph1Dto));
            Assert.AreEqual(1, (deleted as CompositeTph1Dto)!.SpecificPropertyCompositeTph1);

            deleted = await _dao.TryDeleteAsync((9, 9)).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.HasCount(0, loadAll);
            Assert.AreEqual(9, deleted.Key1);
            Assert.AreEqual(9, deleted.Key2);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(CompositeTph2Dto));
            Assert.AreEqual(2, (deleted as CompositeTph2Dto)!.SpecificPropertyCompositeTph2);
        }

        [TestMethod]
        public async Task DeleteWorksWithListOfKeys()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var deletedEntities = (await _dao.TryDeleteAsync(new[] { (7, 7), (9,9), (8,8) }).ConfigureAwait(false))!.ToList();
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.AreEqual(3, deletedEntities.Count());
            Assert.IsFalse(loadAll.Any());

            var deleted = deletedEntities.First(s => s.Key1 == 7 && s.Key2 == 7);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsFalse(deleted is CompositeTph2Dto || deleted is CompositeTph1Dto);

            deleted = deletedEntities.First(s => s.Key1 == 8 && s.Key2 == 8);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(CompositeTph1Dto));
            Assert.AreEqual(1, (deleted as CompositeTph1Dto)!.SpecificPropertyCompositeTph1);

            deleted = deletedEntities.First(s => s.Key1 == 9 && s.Key2 == 9);
            Assert.AreEqual("test", deleted.Value);
            Assert.IsInstanceOfType(deleted, typeof(CompositeTph2Dto));
            Assert.AreEqual(2, (deleted as CompositeTph2Dto)!.SpecificPropertyCompositeTph2);
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.LoadAll().OrderBy(x => x.Key1).ToList();
            Assert.HasCount(3, loadAll);
            Assert.AreEqual(7, loadAll.First().Key1);
            Assert.AreEqual(7, loadAll.First().Key2);
            Assert.AreEqual("test", loadAll.First().Value);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key2);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(CompositeTph1Dto));

            Assert.AreEqual(9, loadAll.Skip(2).First().Key1);
            Assert.AreEqual(9, loadAll.Skip(2).First().Key2);
            Assert.AreEqual("test", loadAll.Skip(2).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(2).First(), typeof(CompositeTph2Dto));
        }

        [TestMethod]
        public async Task CanUseWhereClause()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.Where(s => (s.Key1 == 7 && s.Key2 == 7) || (s.Key1 == 8 && s.Key2 == 8)).ToList();
            Assert.HasCount(2, loadAll);
            Assert.AreEqual(7, loadAll.First().Key1);
            Assert.AreEqual(7, loadAll.First().Key2);
            Assert.AreEqual("test", loadAll.First().Value);

            Assert.AreEqual(8, loadAll.Skip(1).First().Key1);
            Assert.AreEqual(8, loadAll.Skip(1).First().Key2);
            Assert.AreEqual("test", loadAll.Skip(1).First().Value);
            Assert.IsInstanceOfType(loadAll.Skip(1).First(), typeof(CompositeTph1Dto));
            Assert.AreEqual(1, (loadAll.Skip(1).First() as CompositeTph1Dto)?.SpecificPropertyCompositeTph1);
        }

        [TestMethod]
        public async Task FirstOrDefaultReturnObject()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var dto = await _dao.FirstOrDefaultAsync(s => s.Key1 == 9 && s.Key2 == 9).ConfigureAwait(false);
            Assert.IsNotNull(dto);
            Assert.AreEqual(9, dto.Key1);
            Assert.AreEqual(9, dto.Key2);
            Assert.AreEqual("test", dto.Value);
            Assert.IsInstanceOfType(dto, typeof(CompositeTph2Dto));
            Assert.AreEqual(2, (dto as CompositeTph2Dto)?.SpecificPropertyCompositeTph2);
        }
    }
}
