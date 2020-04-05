using Microsoft.EntityFrameworkCore;

namespace NosCore.Dao.Interfaces
{
    public interface IDbContextBuilder
    {
        DbContext CreateContext();
    }
}