//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Serilog.Context;

namespace NosCore.Dao.Extensions
{
    public static class InterfaceHelper
    {
        public static IEnumerable<Type> GetAllTypesOf<T>()
        {
            return AssemblyLoadContext.All.SelectMany(x=>x.Assemblies)
                .SelectMany(x=>x.ExportedTypes)
                .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface) ?? [];
        }

        public static string TrimEnd(this string source, string value)
        {
            return !source.EndsWith(value) ? source : source.Remove(source.LastIndexOf(value, StringComparison.Ordinal));
        }
    }
}
