using Microsoft.EntityFrameworkCore.Metadata.Internal;

using Orbits.GeneralProject.BLL.FilterBuilder.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Expression = System.Linq.Expressions.Expression;
using Orbits.GeneralProject.Core.Entities;

namespace Orbits.GeneralProject.BLL.FilterBuilder
{
    /// <summary>   A builder. </summary>
    public static class Builder
    {
        /// <summary>   The contains method. </summary>
        private static MethodInfo containsMethod = typeof(string).GetMethod("Contains", new Type [] { typeof(string) });

        /// <summary>   The trim method. </summary>
        private static MethodInfo trimMethod = typeof(string).GetMethod("Trim", new Type [0]);

        /// <summary>   to lower method. </summary>
        private static MethodInfo toLowerMethod = typeof(string).GetMethod("ToLower", new Type [0]);

        /// <summary>   The starts with method. </summary>
        private static MethodInfo startsWithMethod = typeof(string).GetMethod("StartsWith", new [] { typeof(string) });

        /// <summary>   The ends with method. </summary>
        private static MethodInfo endsWithMethod = typeof(string).GetMethod("EndsWith", new [] { typeof(string) });

        /// <summary>   The expressions. </summary>
        private static readonly Dictionary<Operation, Func<Expression, Expression, Expression>> Expressions;

        /// <summary>   Static constructor. </summary>
        static Builder( )
        {
            Expressions = new Dictionary<Operation, Func<Expression, Expression, Expression>>();
            Expressions.Add(Operation.Equals, ( member, constant ) => Expression.Equal(member, constant));
            Expressions.Add(Operation.NotEquals, ( member, constant ) => Expression.NotEqual(member, constant));
            Expressions.Add(Operation.GreaterThan, ( member, constant ) => Expression.GreaterThan(member, constant));
            Expressions.Add(Operation.GreaterThanOrEquals, ( member, constant ) => Expression.GreaterThanOrEqual(member, constant));
            Expressions.Add(Operation.LessThan, ( member, constant ) => Expression.LessThan(member, constant));
            Expressions.Add(Operation.LessThanOrEquals, ( member, constant ) => Expression.LessThanOrEqual(member, constant));
            Expressions.Add(Operation.Contains, ( member, constant ) => Contains(member, constant));
            Expressions.Add(Operation.NotContains, ( member, constant ) => Expression.Not(Contains(member, constant)));
            Expressions.Add(Operation.StartsWith, ( member, constant ) => Expression.Call(member, startsWithMethod, constant));
            Expressions.Add(Operation.EndsWith, ( member, constant ) => Expression.Call(member, endsWithMethod, constant));
        }
        /// <summary>
        ///check if property name exists in entity like "Inactive" ,"IsDeleted"
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private static bool IsPropertyExists( Type type, string propertyName )
        {
            var propery = type
               .GetProperties(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
               | System.Reflection.BindingFlags.DeclaredOnly)
               .Where(x => x.Name.ToLower() == propertyName?.ToLower()).FirstOrDefault();

            return (propery != null) ? true : false;
        }
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets an expression. </summary>
        ///
        /// <typeparam name="T">    Generic type parameter. </typeparam>
        /// <param name="filter">   Specifies the filter. </param>
        ///
        /// <returns>   The expression. </returns>
        ///-------------------------------------------------------------------------------------------------

        public static Expression<Func<T, bool>> GetExpression<T>( IFilter<T> filter ) where T : class
        {
            var param = Expression.Parameter(typeof(T), "x");
            Expression expression = Expression.Constant(true);
            var connector = FilterStatementConnector.And;
            foreach (var statement in filter.Statements)
            {
                
                Expression expr = null;
                ////check if property name exists in entity like "Inactive" ,"IsDeleted"
                //if (!IsPropertyExists(typeof(T), statement.PropertyName))
                //    continue;
                if (IsList(statement))
                    expr = ProcessListStatement(param, statement);
                else
                    expr = GetExpression(param, statement);

                expression = CombineExpressions(expression, expr, connector);
                connector = statement.Connector;
            }
            return Expression.Lambda<Func<T, bool>>(expression, param);

            //var param = Expression.Parameter(typeof(T), "type");
            //Expression expression = Expression.Constant(true);
            //var connector = FilterStatementConnector.And;
            //Expression expr = null;
            //foreach (var statement in filter.Statements)
            //{
            //    var propertyExp = Expression.Property(param, statement.PropertyName);

            //    if (IsList(statement))
            //    {
            //        expr = ProcessListStatement(param, statement);
            //    }
            //    else
            //    {
            //        if (propertyExp.Type == typeof(string))
            //        {
            //            MethodInfo method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
            //            var someValue = Expression.Constant(statement.Value, typeof(string));
            //            var containsMethodExp = Expression.Call(propertyExp, method, someValue);

            //            expr = Expression.Lambda<Func<T, bool>>(containsMethodExp, param);
            //        }
            //        else
            //        {
            //            var converter = TypeDescriptor.GetConverter(propertyExp.Type);
            //            var result = converter.ConvertFrom(statement.Value);
            //            var someValue = Expression.Convert(Expression.Constant(result), propertyExp.Type);
            //            var containsMethodExp = Expression.Equal(propertyExp, someValue);
            //            expr = Expression.Lambda<Func<T, bool>>(containsMethodExp, param);
            //        }
            //    }

            //    expression = CombineExpressions(expression, expr, connector);
            //    //connector = statement.Connector;
            //}

            //return Expression.Lambda<Func<T, bool>>(expression, param);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets an expression. </summary>
        ///
        /// <param name="param">        The parameter. </param>
        /// <param name="statement">    The statement. </param>
        /// <param name="propertyName"> (Optional)
        ///                             Name of the property. </param>
        ///
        /// <returns>   The expression. </returns>
        ///-------------------------------------------------------------------------------------------------
        /// Create Expression to search in properties inside Entity  or inside its Navigation Property of type TEntity  inside searched entity
        ///Ex:
        ///EmployeeCountries is Navigation Property of type ICollection<EmployeeCountry> and (CountryCurrencyId,EmployeeId) are properties in EmployeeCountry
        ////so we can search in Employee => has many EmployeeCountries => with properties of EmployeeCountry (CountryCurrencyId,EmployeeId) 
        //{"Inactive":{"operator":"contains","value":true}
        //{"Currency.Name":{"operator":"contains","value":"iraqi"}
        // {"EmployeeCountries[CountryCurrency.CurrencyId]":{"operator":"equals","value":7}}
        // {"EmployeeCountries[CountryCurrency.Currency.Name]":{"operator":"equals","value":"Euro"}}
        // {"EmployeeCountries[CountryCurrency.CountryId.Name]":{"operator":"equals","value":"Egypt"}}

        private static Expression GetExpression( ParameterExpression param, IFilterStatement statement, string propertyName = null )
        {

           
            Expression member = GetMemberExpression(param, propertyName ?? statement.PropertyName);
            Expression constant = Expression.Constant(statement.Value);
            //**var propertyExp = Expression.Property(param, statement.PropertyName);
            //--------------

            //fix for list statement
            if (statement.PropertyName.IndexOf("[") != -1)
            {
                var basePropertyName = statement.PropertyName.Substring(0, statement.PropertyName.IndexOf("["));
                statement.PropertyName = statement.PropertyName.Replace(basePropertyName, "").Replace("[", "").Replace("]", "");

            }

            string [] chainProperties = statement.PropertyName.Split('.');

            MemberExpression propertyExp = null;
      
            for (int i = 0; i < chainProperties.Length; i++)
                if (i == 0)
                { ////check if property name exists in entity like "Inactive" ,"IsDeleted"
                    if (!IsPropertyExists(param.Type, chainProperties [i]))
                        continue;
                    else
                      propertyExp = Expression.Property(param, chainProperties [i]);

                }
                else
                    propertyExp = Expression.Property(propertyExp, chainProperties [i]);

            if (propertyExp != null)
            {

                //--------------
                if (propertyExp.Type == typeof(string))
                {
                    if (statement.Value is string)
                    {
                        var trimMemberCall = Expression.Call(member, trimMethod);
                        member = Expression.Call(trimMemberCall, toLowerMethod);
                        var trimConstantCall = Expression.Call(constant, trimMethod);
                        constant = Expression.Call(trimConstantCall, toLowerMethod);
                    }
                }
                else
                {
                    var converter = TypeDescriptor.GetConverter(propertyExp.Type);
                    var result = converter.ConvertFrom(statement.Value);
                    member = propertyExp;
                    // var containsMethodExp = Expression.Equal(param, someValue);
                    constant = Expression.Convert(Expression.Constant(result), propertyExp.Type);

                    statement.Operation = statement.Operation;
                }
                return Expressions [statement.Operation].Invoke(member, constant);

            }
            else
                return Expression.Constant(true);
        }
        private static bool IsListAndStatement( IFilterStatement statement )
        {
            return (statement.PropertyName.Contains("[") && statement.PropertyName.Contains("]") && statement.PropertyName.Contains("."));
        }
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Query if 'statement' is list. </summary>
        ///
        /// <param name="statement">    The statement. </param>
        ///
        /// <returns>   True if list, false if not. </returns>
        ///-------------------------------------------------------------------------------------------------

        private static bool IsList( IFilterStatement statement )
        {
            return statement.PropertyName.Contains("[") && statement.PropertyName.Contains("]");
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Combine expressions. </summary>
        ///
        /// <param name="expr1">        The first expression. </param>
        /// <param name="expr2">        The second expression. </param>
        /// <param name="connector">    The connector. </param>
        ///
        /// <returns>   An Expression. </returns>
        ///-------------------------------------------------------------------------------------------------

        private static Expression CombineExpressions( Expression expr1, Expression expr2, FilterStatementConnector connector )
        {
            return connector == FilterStatementConnector.And ? Expression.AndAlso(expr1, expr2) : Expression.OrElse(expr1, expr2);
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Process the list statement. </summary>
        ///
        /// <param name="param">        The parameter. </param>
        /// <param name="statement">    The statement. </param>
        ///
        /// <returns>   An Expression. </returns>
        ///-------------------------------------------------------------------------------------------------
        /// Create Expression to search in properties inside  Navigation Property of type ICollection<TEntity> all inside searched entity
        /// ///Ex:
        ///EmployeeCountries is Navigation Property of type ICollection<EmployeeCountry> and (CountryCurrencyId,EmployeeId) are properties in EmployeeCountry
        ////so we can search in Employee => has many EmployeeCountries => with properties of EmployeeCountry (CountryCurrencyId,EmployeeId) 
        //{"EmployeeCountries[CountryCurrencyId]":{"operator":"equals","value":29}}
        //{"EmployeeCountries[EmployeeId]":{"operator":"equals","value":2039}}
        //{"Employee.EmployeeCountries[Employee.Id]":{"operator":"equals","value":2039}}

        private static Expression ProcessListStatement( ParameterExpression param, IFilterStatement statement )
        {
            var basePropertyName = statement.PropertyName.Substring(0, statement.PropertyName.IndexOf("["));
            var propertyName = statement.PropertyName.Replace(basePropertyName, "").Replace("[", "").Replace("]", "");

            string probNameLeft = basePropertyName.IndexOf(".")>-1? basePropertyName.Split('.')[0]:null;
            string probNameRight = basePropertyName.IndexOf(".") > -1 ? basePropertyName.Split('.')[1] : null;
            var type = basePropertyName.IndexOf(".")==-1? param.Type.GetProperty(basePropertyName).PropertyType.GetGenericArguments() [0]:
               param.Type.GetProperty(probNameLeft).PropertyType.GetProperty(probNameRight).PropertyType.GetGenericArguments()[0];

            ParameterExpression listItemParam = Expression.Parameter(type, "i");
            var lambda = Expression.Lambda(GetExpression(listItemParam, statement, propertyName), listItemParam);
            var member = GetMemberExpression(param, basePropertyName);
           
            var tipoEnumerable = typeof(Enumerable);
            string filterMethodName = statement.Operation == Operation.NotContains ? "All" : "Any";
            var anyInfo = tipoEnumerable.GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == filterMethodName && m.GetParameters().Count() == 2);
            anyInfo = anyInfo.MakeGenericMethod(type);



            return Expression.Call(anyInfo, member, lambda);
        }

        

       
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Determines if this collection contains a given object. </summary>
        ///
        /// <param name="member">       The member. </param>
        /// <param name="expression">   The expression. </param>
        ///
        /// <returns>   An Expression. </returns>
        ///-------------------------------------------------------------------------------------------------

        private static Expression Contains( Expression member, Expression expression )
        {
            if (expression is ConstantExpression)
            {
                var constant = (ConstantExpression)expression;
                if (constant.Value is IList && constant.Value.GetType().IsGenericType)
                {
                    var type = constant.Value.GetType();
                    var containsInfo = type.GetMethod("Contains", new [] { type.GetGenericArguments() [0] });
                    var contains = Expression.Call(constant, containsInfo, member);
                    return contains;
                }
            }

            return Expression.Call(member, containsMethod, expression);
        }
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets member expression. </summary>
        ///
        /// <param name="param">        The parameter. </param>
        /// <param name="propertyName"> Name of the property. </param>
        ///
        /// <returns>   The member expression. </returns>
        ///-------------------------------------------------------------------------------------------------

        private static MemberExpression GetMemberExpression( Expression param, string propertyName )
        {
            if (propertyName.Contains("."))
            {
                int index = propertyName.IndexOf(".");
                var subParam = Expression.Property(param, propertyName.Substring(0, index));
                return GetMemberExpression(subParam, propertyName.Substring(index + 1));
            }

            return Expression.Property(param, propertyName);
        }
    }
}
