//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace NosCore.Dao.Extensions
{
    public static class DbContextFindAllExtensions
    {
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
            var parameter = Expression.Parameter(typeof(T), "e");
            var methodInfo = typeof(List<TKey>).GetMethod("Contains") ?? throw new InvalidOperationException();
            var body = Expression.Call(Expression.Constant(list, typeof(List<TKey>)), methodInfo,
                Expression.MakeMemberAccess(parameter, keyProperty.First()));
            var predicateExpression = Expression.Lambda<Func<T, bool>>(body, parameter);
            return dbSet.Where(predicateExpression);
        }
    }
}