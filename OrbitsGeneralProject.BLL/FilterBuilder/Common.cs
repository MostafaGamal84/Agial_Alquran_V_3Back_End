using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.BLL.FilterBuilder
{
    /// <summary>   A common. </summary>
    public static class Common
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   An Expression&lt;T&gt; extension method that composes. </summary>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="first">    The first to act on. </param>
        /// <param name="second">   The second. </param>
        /// <param name="merge">    The merge. </param>
        ///
        /// <returns>   An Expression&lt;T&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        public static Expression<T> Compose<T>(
        this Expression<T> first,
        Expression<T> second,
        Func<Expression, Expression, Expression> merge)
        {
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            var secondBody = RebindParameterVisitor.ReplaceParameters(map, second.Body);
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   An Expression&lt;Func&lt;T,bool&gt;&gt; extension method that ands. </summary>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="first">    The first to act on. </param>
        /// <param name="second">   The second. </param>
        ///
        /// <returns>   An Expression&lt;Func&lt;T,bool&gt;&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        public static Expression<Func<T, bool>> And<T>(
            this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   An Expression&lt;Func&lt;T,bool&gt;&gt; extension method that ors. </summary>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="first">    The first to act on. </param>
        /// <param name="second">   The second. </param>
        ///
        /// <returns>   An Expression&lt;Func&lt;T,bool&gt;&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        public static Expression<Func<T, bool>> Or<T>(
            this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.Or);
        }
    }

    /// <summary>   A rebind parameter visitor. </summary>
    public class RebindParameterVisitor : ExpressionVisitor
    {
        /// <summary>   The map. </summary>
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Constructor. </summary>
        ///
        /// <param name="map">  The map. </param>
        ///-------------------------------------------------------------------------------------------------

        public RebindParameterVisitor(
            Dictionary<ParameterExpression,
            ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Replace parameters. </summary>
        ///
        /// <param name="map">  The map. </param>
        /// <param name="exp">  The exponent. </param>
        ///
        /// <returns>   An Expression. </returns>
        ///-------------------------------------------------------------------------------------------------

        public static Expression ReplaceParameters(
            Dictionary<ParameterExpression,
            ParameterExpression> map,
            Expression exp)
        {
            return new RebindParameterVisitor(map).Visit(exp);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>
        ///     Visits the <see cref="T:System.Linq.Expressions.ParameterExpression" />
        ///     .
        /// </summary>
        ///
        /// <param name="node"> The expression to visit. </param>
        ///
        /// <param name="p">    The ParameterExpression to process. </param>
        ///
        /// <returns>
        ///     The modified expression, if it or any subexpression was modified; otherwise, returns the
        ///     original expression.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        protected override Expression VisitParameter(ParameterExpression p)
        {
            ParameterExpression replacement;
            if (map.TryGetValue(p, out replacement))
            {
                p = replacement;
            }
            return base.VisitParameter(p);
        }
    }
}
