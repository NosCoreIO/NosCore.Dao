//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace NosCore.Dao.Extensions
{
    public static class TypeExtensions
    {
        public static PropertyInfo[]? FindKey(this Type typeDto)
        {
            var key = new List<object>();
            var pis = typeDto.GetProperties();
            for (var index = 0; (index < pis.Length); index++)
            {
                var pi = pis[index];
                var attrs = pi.GetCustomAttributes(typeof(KeyAttribute), false);
                if (attrs.Length != 1)
                {
                    continue;
                }

                key.Add(pi);
            }

            return key.Count > 0 ? key.Select(s=> (PropertyInfo)s).ToArray() : null;
        }
    }
}