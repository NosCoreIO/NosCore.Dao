using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Internal;
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
            var simpleDto = new SimpleDto() { Key = 8, Value = "test" };
            await _genericDao.TryInsertOrUpdateAsync(simpleDto)!.ConfigureAwait(false);
            var loadAll = _dbContextBuilder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.IsTrue(loadAll.Count == 1);
            Assert.IsTrue(loadAll.First().Key == 8);
            Assert.IsTrue(loadAll.First().Value == "test");
        }

        //can replace

        //can insert dtos

        //auto increment check

        //can delete

        //can not delete unexisting

        //can delete multiple

        //where clause

        //load all

        //FirstOrDefaultAsync clause
    }
}
