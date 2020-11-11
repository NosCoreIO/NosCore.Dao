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
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Key2 == 7);
            Assert.IsTrue(loadAll.First().Value == "test");
            Assert.IsTrue(result is CompositeTphBaseDto);

            var CompositeTph1Dto = new CompositeTph1Dto() { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
            Assert.IsTrue(result is CompositeTph1Dto);
            Assert.IsTrue((result as CompositeTph1Dto)!.SpecificPropertyCompositeTph1 == 1);

            var CompositeTph2Dto = new CompositeTph2Dto() { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.Skip(2).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test");
            Assert.IsTrue(result is CompositeTph2Dto);
            Assert.IsTrue((result as CompositeTph2Dto)!.SpecificPropertyCompositeTph2 == 2);
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
            var loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Key2 == 7);
            Assert.IsTrue(loadAll.First().Value == "test1");
            Assert.IsTrue(result is CompositeTphBaseDto);

            var CompositeTph1Dto = new CompositeTph1Dto() { Key1 = 8, Key2 = 8, Value = "test2", SpecificPropertyCompositeTph1 = 1 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph1Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test2");
            Assert.IsTrue(result is CompositeTph1Dto);
            Assert.IsTrue((result as CompositeTph1Dto)!.SpecificPropertyCompositeTph1 == 1);

            var CompositeTph2Dto = new CompositeTph2Dto() { Key1 = 9, Key2 = 9, Value = "test3", SpecificPropertyCompositeTph2 = 2 };
            result = await _dao.TryInsertOrUpdateAsync(CompositeTph2Dto).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.Skip(2).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test3");
            Assert.IsTrue(result is CompositeTph2Dto);
            Assert.IsTrue((result as CompositeTph2Dto)!.SpecificPropertyCompositeTph2 == 2);
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
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Key2 == 7);
            Assert.IsTrue(loadAll.First().Value == "test1");
            Assert.IsFalse(loadAll.First() is CompositeTph1Entity || loadAll.First() is CompositeTph2Entity);

            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test2");
            Assert.IsTrue(loadAll.Skip(1).First() is CompositeTph1Entity);
            Assert.IsTrue((loadAll.Skip(1).First() as CompositeTph1Entity)?.SpecificPropertyCompositeTph1 == 1);

            Assert.IsTrue(loadAll.Skip(2).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test3");
            Assert.IsTrue(loadAll.Skip(2).First() is CompositeTph2Entity);
            Assert.IsTrue((loadAll.Skip(2).First() as CompositeTph2Entity)?.SpecificPropertyCompositeTph2 == 2);

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
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Key2 == 7);
            Assert.IsTrue(loadAll.First().Value == "test1");
            Assert.IsFalse(loadAll.First() is CompositeTph1Entity || loadAll.First() is CompositeTph2Entity);

            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test2");
            Assert.IsTrue(loadAll.Skip(1).First() is CompositeTph1Entity);
            Assert.IsTrue((loadAll.Skip(1).First() as CompositeTph1Entity)?.SpecificPropertyCompositeTph1 == 1);

            Assert.IsTrue(loadAll.Skip(2).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test3");
            Assert.IsTrue(loadAll.Skip(2).First() is CompositeTph2Entity);
            Assert.IsTrue((loadAll.Skip(2).First() as CompositeTph2Entity)?.SpecificPropertyCompositeTph2 == 2);
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
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(deleted.Key1 == 7);
            Assert.IsTrue(deleted.Key2 == 7);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsFalse(deleted is CompositeTph2Dto || deleted is CompositeTph1Dto);

            deleted = await _dao.TryDeleteAsync((8,8)).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(deleted.Key1 == 8);
            Assert.IsTrue(deleted.Key2 == 8);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is CompositeTph1Dto);
            Assert.IsTrue((deleted as CompositeTph1Dto)!.SpecificPropertyCompositeTph1 == 1);

            deleted = await _dao.TryDeleteAsync((9, 9)).ConfigureAwait(false);
            loadAll = _dbContextBuilder.CreateContext().Set<CompositeTphBaseEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 0);
            Assert.IsTrue(deleted.Key1 == 9);
            Assert.IsTrue(deleted.Key2 == 9);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is CompositeTph2Dto);
            Assert.IsTrue((deleted as CompositeTph2Dto)!.SpecificPropertyCompositeTph2 == 2);
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
            Assert.IsTrue(deletedEntities.Count() == 3);
            Assert.IsTrue(!loadAll.Any());

            var deleted = deletedEntities.First(s => s.Key1 == 7 && s.Key2 == 7);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsFalse(deleted is CompositeTph2Dto || deleted is CompositeTph1Dto);

            deleted = deletedEntities.First(s => s.Key1 == 8 && s.Key2 == 8);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is CompositeTph1Dto);
            Assert.IsTrue((deleted as CompositeTph1Dto)!.SpecificPropertyCompositeTph1 == 1);

            deleted = deletedEntities.First(s => s.Key1 == 9 && s.Key2 == 9);
            Assert.IsTrue(deleted.Value == "test");
            Assert.IsTrue(deleted is CompositeTph2Dto);
            Assert.IsTrue((deleted as CompositeTph2Dto)!.SpecificPropertyCompositeTph2 == 2);
        }

        [TestMethod]
        public async Task CanLoadAll()
        {
            var otherContext = _dbContextBuilder.CreateContext();
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTphBaseEntity { Key1 = 7, Key2 = 7, Value = "test" }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph1Entity { Key1 = 8, Key2 = 8, Value = "test", SpecificPropertyCompositeTph1 = 1 }).ConfigureAwait(false);
            await otherContext.Set<CompositeTphBaseEntity>().AddAsync(new CompositeTph2Entity { Key1 = 9, Key2 = 9, Value = "test", SpecificPropertyCompositeTph2 = 2 }).ConfigureAwait(false);
            await otherContext.SaveChangesAsync().ConfigureAwait(false);

            var loadAll = _dao.LoadAll().ToList();
            Assert.IsTrue(loadAll.Count == 3);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Value == "test");

            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
            Assert.IsTrue(loadAll.Skip(1).First() is CompositeTph1Dto);

            Assert.IsTrue(loadAll.Skip(2).First().Key1 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Key2 == 9);
            Assert.IsTrue(loadAll.Skip(2).First().Value == "test");
            Assert.IsTrue(loadAll.Skip(2).First() is CompositeTph2Dto);
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
            Assert.IsTrue(loadAll.Count == 2);
            Assert.IsTrue(loadAll.First().Key1 == 7);
            Assert.IsTrue(loadAll.First().Key2 == 7);
            Assert.IsTrue(loadAll.First().Value == "test");

            Assert.IsTrue(loadAll.Skip(1).First().Key1 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Key2 == 8);
            Assert.IsTrue(loadAll.Skip(1).First().Value == "test");
            Assert.IsTrue(loadAll.Skip(1).First() is CompositeTph1Dto);
            Assert.IsTrue((loadAll.Skip(1).First() as CompositeTph1Dto)?.SpecificPropertyCompositeTph1 == 1);
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
            Assert.IsTrue(dto.Key1 == 9);
            Assert.IsTrue(dto.Key2 == 9);
            Assert.IsTrue(dto.Value == "test");
            Assert.IsTrue(dto is CompositeTph2Dto);
            Assert.IsTrue((dto as CompositeTph2Dto)?.SpecificPropertyCompositeTph2 == 2);
        }
    }
}
