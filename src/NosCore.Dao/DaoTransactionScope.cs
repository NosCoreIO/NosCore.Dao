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
        }

        private static readonly AsyncLocal<Holder?> Slot = new();

        public static DbContext? Current => Slot.Value?.Context;

        public static Holder Attach(DbContext context)
        {
            var holder = new Holder { Context = context };
            Slot.Value = holder;
            return holder;
        }
    }

    /// <summary>
    /// Default <see cref="IDaoTransactionScope"/>: every <see cref="Dao{TEntity,TDto,TPk}"/>
    /// call on the current async flow uses the scope's context and transaction.
    /// </summary>
    public sealed class DaoTransactionScope(Func<DbContext> dbContextBuilder) : IDaoTransactionScope
    {
        // Synchronous on purpose: an AsyncLocal written inside an awaited method does not
        // flow back to the caller.
        public IDaoTransaction Begin()
        {
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
                holder.Context = null;
                await transaction.DisposeAsync().ConfigureAwait(false);
                await context.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
