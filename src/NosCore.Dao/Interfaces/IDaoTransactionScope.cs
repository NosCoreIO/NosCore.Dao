//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Threading.Tasks;

namespace NosCore.Dao.Interfaces
{
    /// <summary>
    /// Groups every DAO operation issued on the current async flow into one database
    /// transaction. Nothing is persisted until <see cref="IDaoTransaction.CommitAsync"/>;
    /// disposing without committing rolls everything back.
    /// </summary>
    public interface IDaoTransactionScope
    {
        IDaoTransaction Begin();
    }

    public interface IDaoTransaction : IAsyncDisposable
    {
        Task CommitAsync();
    }
}
