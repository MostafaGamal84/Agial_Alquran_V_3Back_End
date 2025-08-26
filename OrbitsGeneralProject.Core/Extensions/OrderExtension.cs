using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Orbits.GeneralProject.Core.Extensions
{
    public static class OrderQueryableExtensions
    {
        /// <summary>
        /// Add OrderBy Columns Extension for  IQueryable<T>
        ///  Ex: orderby name,industry.name,... // Property,NavigationProperty.Property,....
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static IOrderedQueryable<T> OrderBy<T>( this IQueryable<T> source, string propertyName ) where T : EntityBase
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    var orderBys = propertyName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    IOrderedQueryable<T> orderedQuerybale = source.OrderBy(ToLambda<T>(orderBys[0]));
                    for (int i = 0; i < orderBys.Length; i++)
                    {
                        orderedQuerybale = i == 0
                                         ? source.OrderBy(ToLambda<T>(orderBys[i]))
                                         : orderedQuerybale.ThenBy(ToLambda<T>(orderBys[i]));
                    }
                    return orderedQuerybale;
                }
                else
                {
                    return source.OrderBy(ToLambda<T>("Id"));
                }
            }
            catch (Exception)
            {

                return source.OrderBy(ToLambda<T>("Id"));
            }

        }

        public static IOrderedQueryable<T> OrderByDescending<T>( this IQueryable<T> source, string propertyName )
        {
            try
            {

                //return source.OrderByDescending(ToLambda<T>(propertyName));
                if (!string.IsNullOrWhiteSpace(propertyName))
                {
                    var orderBys = propertyName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    IOrderedQueryable<T> orderedQuerybale = source.OrderBy(ToLambda<T>(orderBys[0]));
                    for (int i = 0; i < orderBys.Length; i++)
                    {
                        orderedQuerybale = i == 0
                                         ? source.OrderByDescending(ToLambda<T>(propertyName))
                                         : orderedQuerybale.ThenByDescending(ToLambda<T>(propertyName));
                    }
                    return orderedQuerybale;
                }
                else
                {
                    return source.OrderByDescending(ToLambda<T>("Id"));
                }
            }
            catch (Exception)
            {

                return source.OrderBy(ToLambda<T>("Id"));
            }
}

        private static Expression BuildPropertyPathExpression( this Expression rootExpression, string propertyPath )
        {
            if (string.IsNullOrWhiteSpace(propertyPath))
                propertyPath = "Id";
            var parts = propertyPath.Split(new [] { '.' }, 2);
            var currentProperty = parts [0];
            var propertyDescription = rootExpression.Type.GetProperty(currentProperty, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (propertyDescription == null)
                throw new KeyNotFoundException($"Cannot find property {rootExpression.Type.Name}.{currentProperty}. The root expression is {rootExpression} and the full path would be {propertyPath}.");

            var propExpr = Expression.Property(rootExpression, propertyDescription);
            if (parts.Length > 1)
                return BuildPropertyPathExpression(propExpr, parts [1]);

            return propExpr;
        }


        private static Expression<Func<T, object>> ToLambda<T>( string propertyName )
        {

            var parameter = Expression.Parameter(typeof(T));
            Expression property = parameter.BuildPropertyPathExpression(propertyName);
            //Expression property = GetMemberExpression(parameter, propertyName);
            ////var property = Expression.Property(parameter, propertyName);
            var propAsObject = Expression.Convert(property, typeof(object));

            return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
        }

    }




}
/////////
///using Azure;
//using Castle.DynamicProxy.Generators.Emitters.SimpleAST;

//using Microsoft.EntityFrameworkCore;

//using System.ComponentModel;
//using System.Linq.Expressions;
//using System.Reflection;

//using Expression = System.Linq.Expressions.Expression;

//namespace DexefGeneralSetting.Core.Extensions
//{
//    public static class OrderQueryableExtensions
//    {
//        private static bool IsPropertyExists( Type type, string propertyName )
//        {
//            var propery = type
//               .GetProperties(System.Reflection.BindingFlags.Public
//                | System.Reflection.BindingFlags.Instance
//               | System.Reflection.BindingFlags.DeclaredOnly)
//               .Where(x => x.Name.ToLower() == propertyName?.ToLower()).FirstOrDefault();

//            return (propery != null) ? true : false;
//        }
//        private static Expression GetExpression( ParameterExpression param, string propertyName = null )
//        {


//            Expression member = GetMemberExpression(param, propertyName);

//            //fix for list statement
//            if (propertyName.IndexOf("[") != -1)
//            {
//                var basePropertyName = propertyName.Substring(0, propertyName.IndexOf("["));
//                propertyName = propertyName.Replace(basePropertyName, "").Replace("[", "").Replace("]", "");

//            }

//            string [] chainProperties = propertyName.Split('.');

//            MemberExpression propertyExp = null;

//            for (int i = 0; i < chainProperties.Length; i++)
//                if (i == 0)
//                { ////check if property name exists in entity like "Inactive" ,"IsDeleted"
//                    if (!IsPropertyExists(param.Type, chainProperties [i]))
//                        continue;
//                    else
//                        propertyExp = Expression.Property(param, chainProperties [i]);

//                }
//                else
//                    propertyExp = Expression.Property(propertyExp, chainProperties [i]);

//            return propertyExp;
//        }
//        private static Expression ProcessListStatement( ParameterExpression param, string propertyName = null )
//        {
//            var basePropertyName = propertyName.Substring(0, propertyName.IndexOf("["));
//            var proper = propertyName.Replace(basePropertyName, "").Replace("[", "").Replace("]", "");

//            string probNameLeft = basePropertyName.IndexOf(".") > -1 ? basePropertyName.Split('.') [0] : null;
//            string probNameRight = basePropertyName.IndexOf(".") > -1 ? basePropertyName.Split('.') [1] : null;
//            var type = basePropertyName.IndexOf(".") == -1 ? param.Type.GetProperty(basePropertyName).PropertyType.GetGenericArguments() [0] :
//               param.Type.GetProperty(probNameLeft).PropertyType.GetProperty(probNameRight).PropertyType.GetGenericArguments() [0];

//            ParameterExpression listItemParam = Expression.Parameter(type, "i");
//            var lambda = Expression.Lambda(GetExpression(listItemParam, proper), listItemParam);
//            var member = GetMemberExpression(param, basePropertyName);
//            var tipoEnumerable = typeof(Enumerable);
//            string filterMethodName =/* statement.Operation == Operation.NotContains ? "All" : */"Any";
//            //var anyInfo = tipoEnumerable.GetMethods(BindingFlags.Static | BindingFlags.Public).First(m => m.Name == filterMethodName && m.GetParameters().Count() == 2);
//            //anyInfo = anyInfo.MakeGenericMethod(type);



//            //return Expression.Call(anyInfo, member, lambda);
//            return member;



//        }

//        /// <summary>
//        /// Add OrderBy Columns Extension for  IQueryable<T>
//        ///  Ex: orderby name,industry.name,... // Property,NavigationProperty.Property,....
//        /// </summary>
//        /// <typeparam name="T"></typeparam>
//        /// <param name="source"></param>
//        /// <param name="propertyName"></param>
//        /// <returns></returns>
//        public static IOrderedQueryable<T> OrderBy<T>( this IQueryable<T> source, string propertyName ) where T : EntityBase
//        {
//            try
//            {
//                if (!string.IsNullOrWhiteSpace(propertyName))
//                {
//                    var orderBys = propertyName.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//                    IOrderedQueryable<T> orderedQuerybale = source.OrderBy(ToLambda<T>(orderBys [0]));
//                    for (int i = 0; i < orderBys.Length; i++)
//                    {
//                        orderedQuerybale = i == 0
//                                         ? source.OrderBy(ToLambda<T>(orderBys [i]))
//                                         : orderedQuerybale.ThenBy(ToLambda<T>(orderBys [i]));
//                    }
//                    return orderedQuerybale;
//                }
//                else
//                {
//                    return source.OrderBy(ToLambda<T>("Id"));
//                }
//            }
//            catch (Exception ex)
//            {

//                return source.OrderBy(ToLambda<T>("Id"));
//            }

//        }

//        public static IOrderedQueryable<T> OrderByDescending<T>( this IQueryable<T> source, string propertyName )
//        {
//            try
//            {

//                //return source.OrderByDescending(ToLambda<T>(propertyName));
//                if (!string.IsNullOrWhiteSpace(propertyName))
//                {
//                    var orderBys = propertyName.Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
//                    IOrderedQueryable<T> orderedQuerybale = source.OrderBy(ToLambda<T>(orderBys [0]));
//                    for (int i = 0; i < orderBys.Length; i++)
//                    {
//                        orderedQuerybale = i == 0
//                                         ? source.OrderByDescending(ToLambda<T>(propertyName))
//                                         : orderedQuerybale.ThenByDescending(ToLambda<T>(propertyName));
//                    }
//                    return orderedQuerybale;
//                }
//                else
//                {
//                    return source.OrderByDescending(ToLambda<T>("Id"));
//                }
//            }
//            catch (Exception)
//            {

//                return source.OrderBy(ToLambda<T>("Id"));
//            }
//        }
//        private static bool IsList( string propertyName )
//        {
//            return propertyName.Contains("[") && propertyName.Contains("]");
//        }
//        private static MemberExpression GetMemberExpression( Expression param, string propertyName )
//        {
//            if (propertyName.Contains("."))
//            {
//                int index = propertyName.IndexOf(".");
//                var subParam = Expression.Property(param, propertyName.Substring(0, index));
//                return GetMemberExpression(subParam, propertyName.Substring(index + 1));
//            }

//            return Expression.Property(param, propertyName);
//        }


//        private static Expression BuildPropertyPathExpression( this Expression rootExpression, string propertyPath )
//        {
//            if (string.IsNullOrWhiteSpace(propertyPath))
//                propertyPath = "Id";
//            if (IsList(propertyPath))
//            {
//                var partss = propertyPath.Split(new [] { '.' }, 2);

//                var basePropertyName = propertyPath.Substring(0, propertyPath.IndexOf("["));
//                var propertyName = propertyPath.Replace(basePropertyName, "").Replace("[", "").Replace("]", "");
//                var propertyDescription = rootExpression.Type.GetProperty(partss [0], System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
//                if (propertyDescription == null)
//                    throw new KeyNotFoundException($"Cannot find property {rootExpression.Type.Name}.{propertyName}. The root expression is {rootExpression} and the full path would be {propertyPath}.");

//                string probNameLeft = basePropertyName.IndexOf(".") > -1 ? basePropertyName.Split('.') [0] : null;
//                string probNameRight = basePropertyName.IndexOf(".") > -1 ? basePropertyName.Split('.') [1] : null;

//                var leftPropertyDescription = rootExpression.Type.GetProperty(probNameLeft, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
//                var rightPropertyDescription = leftPropertyDescription.PropertyType.GetProperty(probNameRight, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

//                var type = basePropertyName.IndexOf(".") == -1 ? leftPropertyDescription.PropertyType.GetGenericArguments() [0] :
//                   rightPropertyDescription.PropertyType.GetGenericArguments() [0];

//                ParameterExpression listItemParam = Expression.Parameter(type, "i");

//                var propExpr = Expression.Property(listItemParam, leftPropertyDescription);
//                return BuildPropertyPathExpression(propExpr, partss [1]);

//                //var parts = probNameRight.Split(new [] { '.' }, 2);
//                //if (parts.Length > 1)
//                //    return BuildPropertyPathExpression(propExpr, parts [1]);

//                //return propExpr;


//            }
//            else
//            {
//                var parts = propertyPath.Split(new [] { '.' }, 2);
//                var currentProperty = parts [0];
//                var propertyDescription = rootExpression.Type.GetProperty(currentProperty, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
//                if (propertyDescription == null)
//                    throw new KeyNotFoundException($"Cannot find property {rootExpression.Type.Name}.{currentProperty}. The root expression is {rootExpression} and the full path would be {propertyPath}.");

//                var propExpr = Expression.Property(rootExpression, propertyDescription);
//                if (parts.Length > 1)
//                    return BuildPropertyPathExpression(propExpr, parts [1]);

//                return propExpr;

//            }


//        }


//        private static Expression<Func<T, object>> ToLambda<T>( string propertyName )
//        {
//            try
//            {

//                var parameter = Expression.Parameter(typeof(T));
//                //Expression property = parameter.BuildPropertyPathExpression(propertyName);
//                var param = Expression.Parameter(typeof(T), "x");
//                Expression expression = Expression.Constant(true);
//                Expression expr = null;

//                if (IsList(propertyName))
//                    expr = ProcessListStatement(param, propertyName);
//                else
//                    expr = GetExpression(param, propertyName);
//                Expression property = GetMemberExpression(expr, propertyName.Split(".") [0]);
//                ////var property = Expression.Property(parameter, propertyName);
//                var propAsObject = Expression.Convert(property, typeof(object));

//                return Expression.Lambda<Func<T, object>>(propAsObject, parameter);
//            }
//            catch (Exception ex)
//            {
//                return null;
//            }
//        }

//    }




//}
