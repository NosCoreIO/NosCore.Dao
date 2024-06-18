//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyModel;

namespace NosCore.Dao.Extensions
{
    public static class InterfaceHelper
    {
        public static IEnumerable<Type> GetAllTypesOf<T>()
        {
            var platform = Environment.OSVersion.Platform.ToString();
            var runtimeAssemblyNames = DependencyContext.Default?.GetRuntimeAssemblyNames(platform).ToList();

            var result = new List<Type>();
            foreach (var assemblyName in runtimeAssemblyNames ?? [])
            {
                try
                {
                    result.AddRange(Assembly.Load(assemblyName)
                            .ExportedTypes
                        .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface));

                }
                catch (Exception ex)
                {
                   
                }
            }

            return result;
        }

        public static string TrimEnd(this string source, string value)
        {
            return !source.EndsWith(value) ? source : source.Remove(source.LastIndexOf(value, StringComparison.Ordinal));
        }
    }
}