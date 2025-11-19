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
using System.Runtime.CompilerServices;

namespace NosCore.Dao.Extensions
{
    /// <summary>
    /// Extension methods for DbSet to support finding entities by multiple keys.
    /// </summary>
    public static class DbContextFindAllExtensions
    {
        /// <summary>
        /// Converts an enumerable of values into a tuple.
        /// </summary>
        /// <typeparam name="T">The type of values</typeparam>
        /// <param name="values">The values to convert</param>
        /// <returns>A tuple containing all the values</returns>
        public static ITuple GetTuple<T>(this IEnumerable<T> values)
        {
            var enumerable = values.ToArray();
            var genericType = Type.GetType("System.Tuple`" + enumerable.Length) ?? throw new InvalidOperationException();
            var typeArgs = enumerable.Select(_ => typeof(T)).ToArray();
            var specificType = genericType.MakeGenericType(typeArgs);
            var constructorArguments = enumerable.Cast<object>().ToArray();
            return (ITuple)Activator.CreateInstance(specificType, constructorArguments)!;
        }

        /// <summary>
        /// Finds all entities matching any of the provided key values.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <typeparam name="TKey">The key type</typeparam>
        /// <param name="dbSet">The database set</param>
        /// <param name="keyProperty">The key properties</param>
        /// <param name="keyValues">The key values to search for</param>
        /// <returns>A queryable of matching entities</returns>
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
            Expression? WriteKeyQuery(object? key, Expression entity)
            {
                List<Expression> propertiesEqualityExpression;
                if (key is IEnumerable<object> enumerable)
                {
                    propertiesEqualityExpression = enumerable.Select((t, i) => Expression.Equal(Expression.Constant(t), Expression.Property(entity, keyProperty[i].Name))).ToList<Expression>();
                }
                else
                {
                    propertiesEqualityExpression = key!.GetType().GetFields().OrderBy(field => field.MetadataToken)
                        .Select((t, i) => Expression.Equal(
                            Expression.Constant(t.GetValue(key)),
                            Expression.Property(entity, keyProperty[i].Name))).ToList<Expression>();
                }

                Expression? andAlsoExpression = null;
                for (int i = 0; i < propertiesEqualityExpression.Count; i++)
                {
                    andAlsoExpression = i == 0 ? propertiesEqualityExpression.ElementAt(0) : Expression.AndAlso(andAlsoExpression!, propertiesEqualityExpression.ElementAt(i));
                }

                return andAlsoExpression;
            }

            var parameter = Expression.Parameter(typeof(T), "e");
            var listOfChecks = list.Select(s => WriteKeyQuery(s, parameter)).ToList();
            Expression? orElseExpression = null;
            for (var i = 0; i < listOfChecks.Count; i++)
            {
                orElseExpression = i == 0 ? listOfChecks.ElementAt(0) : Expression.OrElse(orElseExpression!, listOfChecks.ElementAt(i)!);
            }

            var lambda = Expression.Lambda<Func<T, bool>>(orElseExpression!, parameter);
            return dbSet.Where(lambda);
        }
    }
}
