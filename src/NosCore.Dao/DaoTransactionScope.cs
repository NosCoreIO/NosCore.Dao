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

            // First failure of a DAO operation on the scope. A scope promises all or
            // nothing, so once one operation failed the scope must not commit the rest.
            public Exception? Failure;
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
            return Acquired(holder, fallback);
        }

        // Synchronous twin for the sync query methods, which materialize their results
        // before returning and can therefore hold the lease for the whole query.
        public static Lease LeaseSync(Func<DbContext> fallback)
        {
            var holder = Slot.Value;
            if (holder?.Context == null)
            {
                return new Lease(fallback(), null);
            }

            holder.Lock.Wait();
            return Acquired(holder, fallback);
        }

        private static Lease Acquired(Holder holder, Func<DbContext> fallback)
        {
            var context = holder.Context;
            if (context == null)
            {
                holder.Lock.Release();
                return new Lease(fallback(), null);
            }

            return new Lease(context, holder);
        }

        // Outside a scope the lease owns a context of its own and disposes it.
        public readonly struct Lease(DbContext context, Holder? holder) : IDisposable
        {
            public DbContext Context { get; } = context;

            public bool IsScoped => holder != null;

            public void Fail(Exception exception)
            {
                if (holder == null)
                {
                    return;
                }

                holder.Failure ??= exception;
                // Drop the failed changes so the next SaveChanges on the scope does not
                // silently retry them.
                Context.ChangeTracker.Clear();
            }

            public void Dispose()
            {
                if (holder == null)
                {
                    Context.Dispose();
                    return;
                }

                holder.Lock.Release();
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
            public async Task CommitAsync()
            {
                await holder.Lock.WaitAsync().ConfigureAwait(false);
                try
                {
                    if (holder.Failure != null)
                    {
                        throw new InvalidOperationException(
                            "A DAO operation failed inside this DaoTransactionScope; the transaction is rolled back instead of committed.",
                            holder.Failure);
                    }

                    await transaction.CommitAsync().ConfigureAwait(false);
                }
                finally
                {
                    holder.Lock.Release();
                }
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
