//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NosCore.Dao.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NosCore.Dao
{
    // The slot stores a mutable holder rather than the context itself: an AsyncLocal
    // written inside DisposeAsync would not reach the caller's flow, but nulling the
    // holder's field does, so operations after the scope fall through to fresh
    // contexts instead of a disposed one. AsyncLocal keeps concurrent flows isolated.
    internal static class AmbientDbContext
    {
        internal sealed class Holder
        {
            public DbContext? Context;
            public readonly SemaphoreSlim Lock = new(1, 1);
        }

        private static readonly AsyncLocal<Holder?> Slot = new();

        public static DbContext? Current => Slot.Value?.Context;

        public static Holder Attach(DbContext context)
        {
            var holder = new Holder { Context = context };
            Slot.Value = holder;
            return holder;
        }

        // Child tasks spawned inside a scope inherit the holder, and EF contexts do not
        // support concurrent use - the lease serializes them. Taken per operation, so
        // sequential awaits pay one uncontended wait each.
        public static async Task<Lease> LeaseAsync(Func<DbContext> fallback)
        {
            var holder = Slot.Value;
            if (holder?.Context == null)
            {
                return new Lease(fallback(), null);
            }

            await holder.Lock.WaitAsync().ConfigureAwait(false);
            var context = holder.Context;
            if (context == null)
            {
                holder.Lock.Release();
                return new Lease(fallback(), null);
            }

            return new Lease(context, holder.Lock);
        }

        public readonly struct Lease(DbContext context, SemaphoreSlim? scopeLock) : IDisposable
        {
            public DbContext Context { get; } = context;

            public void Dispose()
            {
                scopeLock?.Release();
            }
        }
    }

    /// <summary>
    /// Default <see cref="IDaoTransactionScope"/>: every <see cref="Dao{TEntity,TDto,TPk}"/>
    /// call on the current async flow uses the scope's context and transaction. Scopes do
    /// not nest - beginning one while another is active on the flow throws.
    /// </summary>
    public sealed class DaoTransactionScope(Func<DbContext> dbContextBuilder) : IDaoTransactionScope
    {
        // Synchronous on purpose: an AsyncLocal written inside an awaited method does not
        // flow back to the caller.
        public IDaoTransaction Begin()
        {
            if (AmbientDbContext.Current != null)
            {
                throw new InvalidOperationException(
                    "A DaoTransactionScope is already active on this flow; nested scopes are not supported.");
            }

            var context = dbContextBuilder();
            var transaction = context.Database.BeginTransaction();
            var holder = AmbientDbContext.Attach(context);
            return new DaoTransaction(holder, context, transaction);
        }

        private sealed class DaoTransaction(AmbientDbContext.Holder holder, DbContext context, IDbContextTransaction transaction) : IDaoTransaction
        {
            public Task CommitAsync()
            {
                return transaction.CommitAsync();
            }

            public async ValueTask DisposeAsync()
            {
                // Wait for an in-flight leased operation before tearing anything down.
                await holder.Lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    holder.Context = null;
                    await transaction.DisposeAsync().ConfigureAwait(false);
                    await context.DisposeAsync().ConfigureAwait(false);
                }
                finally
                {
                    holder.Lock.Release();
                }
            }
        }
    }
}
