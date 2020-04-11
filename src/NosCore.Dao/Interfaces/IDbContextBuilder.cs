//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace NosCore.Dao.Interfaces
{
    public interface IDbContextBuilder
    {
        DbContext CreateContext();
    }
}