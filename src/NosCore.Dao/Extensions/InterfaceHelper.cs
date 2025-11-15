//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;

namespace NosCore.Dao.Extensions
{
    /// <summary>
    /// Helper methods for type and interface operations.
    /// </summary>
    public static class InterfaceHelper
    {
        /// <summary>
        /// Gets all types that implement or inherit from the specified type.
        /// </summary>
        /// <typeparam name="T">The base type or interface to search for</typeparam>
        /// <returns>An enumerable of all matching types</returns>
        public static IEnumerable<Type> GetAllTypesOf<T>()
        {
            var result = new List<Type>();

            foreach (var context in AssemblyLoadContext.All)
            {
                foreach (var assembly in context.Assemblies)
                {
                    try
                    {
                        result.AddRange(assembly.ExportedTypes
                            .Where(t => typeof(T).IsAssignableFrom(t) && !t.IsInterface));
                    }
                    catch
                    {
                        // Some assemblies may fail to load types, skip them
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Removes the specified value from the end of the source string if it exists.
        /// </summary>
        /// <param name="source">The source string</param>
        /// <param name="value">The value to remove from the end</param>
        /// <returns>The trimmed string</returns>
        public static string TrimEnd(this string source, string value)
        {
            return !source.EndsWith(value) ? source : source.Remove(source.LastIndexOf(value, StringComparison.Ordinal));
        }
    }
}
