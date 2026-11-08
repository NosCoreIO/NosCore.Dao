//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using NosCore.Dao.Tests.Database;
using NosCore.Dao.Tests.Database.Entities.CompositeTphEntities;
using NosCore.Dao.Tests.Database.Entities.SimpleEntities;
using NosCore.Dao.Tests.TestsModels.CompositeModels;
using NosCore.Dao.Tests.TestsModels.SimpleModels;

namespace NosCore.Dao.Tests
{
    [TestClass]
    public class DaoRobustnessTests
    {
        private sealed class FailingSaveInterceptor : SaveChangesInterceptor
        {
            public int FailuresLeft;

            public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                if (Interlocked.Decrement(ref FailuresLeft) >= 0)
                {
                    throw new DbUpdateException("simulated save failure");
                }

                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }
        }

        private sealed class BlockingSaveInterceptor : SaveChangesInterceptor
        {
            public readonly TaskCompletionSource Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public readonly TaskCompletionSource Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public bool Block;

            public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                InterceptionResult<int> result, CancellationToken cancellationToken = default)
            {
                if (Block)
                {
                    Block = false;
                    Entered.SetResult();
                    await Release.Task.ConfigureAwait(false);
                }

                return result;
            }
        }

        private sealed class SelectCounter : DbCommandInterceptor
        {
            public int Selects;

            public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
                CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
            {
                if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref Selects);
                }

                return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
            }

            public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData,
                InterceptionResult<DbDataReader> result)
            {
                if (command.CommandText.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    Interlocked.Increment(ref Selects);
                }

                return base.ReaderExecuting(command, eventData, result);
            }
        }

        private static Dao<SimpleEntity, SimpleDto, int> SimpleDao(DbContextBuilder builder)
        {
            return new Dao<SimpleEntity, SimpleDto, int>(
                new Mock<ILogger<Dao<SimpleEntity, SimpleDto, int>>>().Object, builder.CreateContext);
        }

        private static Dao<CompositeEntity, CompositeDto, (int, int)> CompositeDao(DbContextBuilder builder)
        {
            return new Dao<CompositeEntity, CompositeDto, (int, int)>(
                new Mock<ILogger<Dao<CompositeEntity, CompositeDto, (int, int)>>>().Object, builder.CreateContext);
        }

        private static bool IsDisposed(DbContext context)
        {
            try
            {
                _ = context.Model;
                return false;
            }
            catch (ObjectDisposedException)
            {
                return true;
            }
        }

        [TestMethod]
        public async Task ContextsCreatedOutsideAScopeAreDisposed()
        {
            var builder = new DbContextBuilder();
            var dao = SimpleDao(builder);

            await dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "a" }).ConfigureAwait(false);
            await dao.TryInsertOrUpdateAsync(new List<SimpleDto> { new() { Key = 2, Value = "b" } }).ConfigureAwait(false);
            await dao.FirstOrDefaultAsync(s => s.Key == 1).ConfigureAwait(false);
            Assert.HasCount(2, dao.LoadAll().ToList());
            Assert.HasCount(1, dao.Where(s => s.Key == 2)!.ToList());
            await dao.TryDeleteAsync(1).ConfigureAwait(false);
            await dao.TryDeleteAsync(new[] { 2 }).ConfigureAwait(false);

            var leaked = builder.CreatedContexts.Count(c => !IsDisposed(c));
            Assert.AreEqual(0, leaked);
        }

        [TestMethod]
        public async Task FailedOperationInsideAScopePreventsCommit()
        {
            var failing = new FailingSaveInterceptor();
            var builder = new DbContextBuilder(failing);
            var dao = SimpleDao(builder);
            await dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "item in bag A" }).ConfigureAwait(false);

            var scope = new DaoTransactionScope(builder.CreateContext);
            await using (var transaction = scope.Begin())
            {
                // Moving an item: the delete fails, the insert succeeds. Committing this
                // would leave the item in both places.
                failing.FailuresLeft = 1;
                var deleted = await dao.TryDeleteAsync(1).ConfigureAwait(false);
                Assert.IsNull(deleted);
                var inserted = await dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 2, Value = "item in bag B" }).ConfigureAwait(false);
                Assert.IsNotNull(inserted);

                await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync()).ConfigureAwait(false);
            }

            var rows = builder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, rows);
            Assert.AreEqual(1, rows[0].Key);
        }

        [TestMethod]
        public async Task FailedOperationInsideAScopeDoesNotLeakIntoLaterSaves()
        {
            var failing = new FailingSaveInterceptor();
            var builder = new DbContextBuilder(failing);
            var dao = SimpleDao(builder);
            var scope = new DaoTransactionScope(builder.CreateContext);

            await using (scope.Begin())
            {
                failing.FailuresLeft = 1;
                Assert.IsNull(await dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "failed" }).ConfigureAwait(false));
                await dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 2, Value = "later" }).ConfigureAwait(false);

                // The failed insert must not be silently retried by the next SaveChanges.
                Assert.IsNull(await dao.FirstOrDefaultAsync(s => s.Key == 1).ConfigureAwait(false));
            }
        }

        [TestMethod]
        public async Task BatchUpsertWithDuplicateKeysKeepsTheLastOne()
        {
            var builder = new DbContextBuilder();
            var dao = SimpleDao(builder);

            var result = await dao.TryInsertOrUpdateAsync(new List<SimpleDto>
            {
                new() { Key = 1, Value = "first" },
                new() { Key = 1, Value = "last" },
            }).ConfigureAwait(false);

            Assert.IsTrue(result);
            var rows = builder.CreateContext().Set<SimpleEntity>().ToList();
            Assert.HasCount(1, rows);
            Assert.AreEqual("last", rows[0].Value);
        }

        [TestMethod]
        public async Task CompositeBatchUpsertWithDuplicateKeysKeepsTheLastOne()
        {
            var builder = new DbContextBuilder();
            var dao = CompositeDao(builder);

            var result = await dao.TryInsertOrUpdateAsync(new List<CompositeDto>
            {
                new() { Key1 = 1, Key2 = 2, Value = "first" },
                new() { Key1 = 1, Key2 = 2, Value = "last" },
            }).ConfigureAwait(false);

            Assert.IsTrue(result);
            var rows = builder.CreateContext().Set<CompositeEntity>().ToList();
            Assert.HasCount(1, rows);
            Assert.AreEqual("last", rows[0].Value);
        }

        [TestMethod]
        public async Task BatchUpsertStillInsertsSeveralRowsWithGeneratedKeys()
        {
            var builder = new DbContextBuilder();
            var dao = SimpleDao(builder);

            var result = await dao.TryInsertOrUpdateAsync(new List<SimpleDto>
            {
                new() { Value = "a" },
                new() { Value = "b" },
            }).ConfigureAwait(false);

            Assert.IsTrue(result);
            Assert.HasCount(2, builder.CreateContext().Set<SimpleEntity>().ToList());
        }

        [TestMethod]
        public async Task EmptyCompositeBatchesSucceed()
        {
            var builder = new DbContextBuilder();
            var dao = CompositeDao(builder);

            Assert.IsTrue(await dao.TryInsertOrUpdateAsync(new List<CompositeDto>()).ConfigureAwait(false));
            var deleted = await dao.TryDeleteAsync(new List<(int, int)>()).ConfigureAwait(false);
            Assert.IsNotNull(deleted);
            Assert.IsEmpty(deleted);
        }

        [TestMethod]
        public async Task BatchDeleteQueriesTheRowsOnce()
        {
            var counter = new SelectCounter();
            var builder = new DbContextBuilder(counter);
            var dao = SimpleDao(builder);
            await dao.TryInsertOrUpdateAsync(new List<SimpleDto> { new() { Key = 1 }, new() { Key = 2 } }).ConfigureAwait(false);

            counter.Selects = 0;
            var deleted = await dao.TryDeleteAsync(new[] { 1, 2 }).ConfigureAwait(false);

            Assert.HasCount(2, deleted!.ToList());
            Assert.AreEqual(1, counter.Selects);
            Assert.IsEmpty(builder.CreateContext().Set<SimpleEntity>().ToList());
        }

        [TestMethod]
        public async Task SyncQueriesInsideAScopeWaitForInFlightOperations()
        {
            var blocking = new BlockingSaveInterceptor();
            var builder = new DbContextBuilder(blocking);
            var dao = SimpleDao(builder);
            var scope = new DaoTransactionScope(builder.CreateContext);

            await using (var transaction = scope.Begin())
            {
                blocking.Block = true;
                var insert = Task.Run(() => dao.TryInsertOrUpdateAsync(new SimpleDto { Key = 1, Value = "a" }));
                await blocking.Entered.Task.ConfigureAwait(false);

                var query = Task.Run(() => dao.Where(s => s.Key == 1)!.ToList());
                var finishedEarly = await Task.WhenAny(query, Task.Delay(300)).ConfigureAwait(false) == query;
                blocking.Release.SetResult();

                Assert.IsFalse(finishedEarly, "Where ran on the scope's context while another operation was using it");
                Assert.IsNotNull(await insert.ConfigureAwait(false));
                Assert.HasCount(1, await query.ConfigureAwait(false));
                await transaction.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}
