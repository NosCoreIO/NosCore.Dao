//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;

namespace NosCore.Dao.Extensions
{
    public static class DbContextFindAllExtensions
    {
        public static ITuple GetTuple<T>(this IEnumerable<T> values)
        {
            var enumerable = values.ToArray();
            var genericType = Type.GetType("System.Tuple`" + enumerable.Length) ?? throw new InvalidOperationException();
            var typeArgs = enumerable.Select(_ => typeof(T)).ToArray();
            var specificType = genericType.MakeGenericType(typeArgs);
            var constructorArguments = enumerable.Cast<object>().ToArray();
            return (ITuple)Activator.CreateInstance(specificType, constructorArguments);
        }

        public static IQueryable<T> FindAll<T, TKey>(this DbSet<T> dbSet, PropertyInfo[] keyProperty,
            params TKey[] keyValues)
        where T : class
        {
            var list = keyValues.ToList();
            return keyProperty.Length == 1 ? dbSet.FindAll(keyProperty, list) : dbSet.FindAllComposite(keyProperty, list);
        }

        private static IQueryable<T> FindAll<T, TKey>(this DbSet<T> dbSet, PropertyInfo[] keyProperty, List<TKey> list)
            where T : class
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var methodInfo = typeof(List<TKey>).GetMethod("Contains") ?? throw new InvalidOperationException();
            var body = Expression.Call(Expression.Constant(list, typeof(List<TKey>)), methodInfo,
                Expression.MakeMemberAccess(parameter, keyProperty.First()));
            var predicateExpression = Expression.Lambda<Func<T, bool>>(body, parameter);
            return dbSet.Where(predicateExpression);
        }

        private static IQueryable<T> FindAllComposite<T, TKey>(this DbSet<T> dbSet, PropertyInfo[] keyProperty, List<TKey> list)
            where T : class
        {
            string WriteKeyQuery(object? key)
            {
                return string.Join(" and ",
                    key is IEnumerable<object> enumerable
                        ? enumerable.Select((t, i) => $"{{{i}}}={t}")
                        : key!.GetType().GetFields().Select((t, i) => $"{{{i}}}={t.GetValue(key)}"));
            }
            var getValue = string.Join(" or ", list.Select(s => $"({WriteKeyQuery(s)})"));
            var request = string.Format(getValue, keyProperty.Select(s => s.Name).ToArray<object>());
            return dbSet.Where(request);
        }
    }
}