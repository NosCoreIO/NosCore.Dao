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
            var parameter = Expression.Parameter(typeof(T), "e");
            var methodInfo = typeof(List<TKey>).GetMethod("Contains") ?? throw new InvalidOperationException();
            Expression expressionToInject = Expression.Empty();
            var i = 0;
            foreach (var expression in keyProperty.Select(
                composite => Expression.MakeMemberAccess(parameter, composite)))
            {
                if (i == 0)
                {
                    expressionToInject = expression;
                }
                else
                {
                    expressionToInject = Expression.AndAlso(expressionToInject, expression);
                }
            }
            var body = Expression.Call(Expression.Constant(list, typeof(List<TKey>)), methodInfo, expressionToInject);
            var predicateExpression = Expression.Lambda<Func<T, bool>>(body, parameter);
            return dbSet.Where(predicateExpression);
        }
    }
}