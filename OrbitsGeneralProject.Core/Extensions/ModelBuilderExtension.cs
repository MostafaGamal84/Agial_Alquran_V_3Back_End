using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HandlebarsDotNet;

namespace Orbits.GeneralProject.Core.Extensions
{
    public static class ModelBuilderExtension
    {
        //Reference for solution https://stackoverflow.com/questions/29261734/add-filter-to-all-query-entity-framework
        // https://gist.github.com/haacked/febe9e88354fb2f4a4eb11ba88d64c24
        public static void ApplyGlobalFilters<TInterface>(this ModelBuilder modelBuilder, Expression<Func<TInterface, bool>> expression, string columnName) where TInterface : class
        {

            var entities = modelBuilder.Model.GetEntityTypes().Where(e => e.ClrType.GetInterface(typeof(TInterface).Name) != null && e.FindProperty(columnName) != null).Select(e => e.ClrType);
            foreach (var entity in entities)
            {

                // var newParam = Expression.Parameter(entity, columnName);
                //var newbody = ReplacingExpressionVisitor.Replace(expression.Parameters.Single(), newParam, expression.Body);
                //modelBuilder.Entity(entity).HasQueryFilter(Expression.Lambda(newbody, newParam));

                modelBuilder.Entity(entity).AppendQueryFilter(expression, columnName);
            }
        }

        public static void AppendQueryFilter<T>(this EntityTypeBuilder entityTypeBuilder, Expression<Func<T, bool>> expression, string filterName) where T : class
        {

            var parameterType = Expression.Parameter(entityTypeBuilder.Metadata.ClrType, filterName);

            var expressionFilter = ReplacingExpressionVisitor.Replace(expression.Parameters.Single(), parameterType, expression.Body);

            if (entityTypeBuilder.Metadata.GetQueryFilter() != null)
            {
                var currentQueryFilter = entityTypeBuilder.Metadata.GetQueryFilter();

                var currentExpressionFilter = ReplacingExpressionVisitor.Replace(currentQueryFilter.Parameters.Single(), parameterType, currentQueryFilter.Body);

                expressionFilter = Expression.AndAlso(currentExpressionFilter, expressionFilter);
            }

            var lambdaExpression = Expression.Lambda(expressionFilter, parameterType);
            entityTypeBuilder.HasQueryFilter(lambdaExpression);
        }


        public static T AddIfNotExists<T>(this EntityTypeBuilder entityTypeBuilder, System.Data.Entity.DbSet<T> dbSet, T entity, Expression<Func<T, bool>> predicate = null) where T : class, new()
        {
            var exists = predicate != null ? dbSet.Any(predicate) : dbSet.Any();
            return !exists ? dbSet.Add(entity) : null;
        }

    }
}
