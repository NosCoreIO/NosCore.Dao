//  __  _  __    __   ___ __  ___ ___
// |  \| |/__\ /' _/ / _//__\| _ \ __|
// | | ' | \/ |`._`.| \_| \/ | v / _|
// |_|\__|\__/ |___/ \__/\__/|_|_\___|
// -----------------------------------

using System;
using System.Linq.Expressions;

namespace NosCore.Dao.Extensions
{
    /// <summary>
    /// Extension methods for expression manipulation.
    /// </summary>
    public static class ExpressionExtensions
    {
        /// <summary>
        /// Replaces the parameter type in an expression from TFrom to TTo.
        /// </summary>
        /// <typeparam name="TFrom">The source parameter type</typeparam>
        /// <typeparam name="TTo">The target parameter type</typeparam>
        /// <param name="target">The expression to transform</param>
        /// <returns>A new expression with the parameter type replaced</returns>
        public static Expression<Func<TTo, bool>> ReplaceParameter<TFrom, TTo>(
            this Expression<Func<TFrom, bool>> target)
        {
            return (Expression<Func<TTo, bool>>) new WhereReplacerVisitor<TFrom, TTo>().Visit(target);
        }

        private class WhereReplacerVisitor<TFrom, TTo> : ExpressionVisitor
        {
            private readonly ParameterExpression _parameter = Expression.Parameter(typeof(TTo), "c");

            protected override Expression VisitLambda<T>(Expression<T> node)
            {
                // replace parameter here
                return Expression.Lambda(Visit(node.Body) ?? throw new InvalidOperationException(), _parameter);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                // replace parameter member access with new type
                if ((node.Member.DeclaringType == typeof(TFrom)) && node.Expression is ParameterExpression)
                {
                    return Expression.PropertyOrField(_parameter, node.Member.Name);
                }

                return base.VisitMember(node);
            }
        }
    }
}