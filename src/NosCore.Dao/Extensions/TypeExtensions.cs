//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace NosCore.Dao.Extensions
{
    public static class TypeExtensions
    {
        public static PropertyInfo? FindKey(this Type typeDto)
        {
            var pis = typeDto.GetProperties();
            for (var index = 0; (index < pis.Length); index++)
            {
                var pi = pis[index];
                var attrs = pi.GetCustomAttributes(typeof(KeyAttribute), false);
                if (attrs.Length != 1)
                {
                    continue;
                }

                return pi;
            }

            return null;
        }
    }
}