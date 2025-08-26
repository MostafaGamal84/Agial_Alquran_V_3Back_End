using System;
using System.Linq.Expressions;
namespace Orbits.GeneralProject.Core.Infrastructure
{
    public static class Json
    {
        public static string Value(string expression, string path)
        {
            throw new InvalidOperationException($"{nameof(Value)} cannot be called client side");
        }
        //public static LambdaExpression GetExpressionValue<T>( string fieldName,string fieldValue, string path ) //where T : class

        //{
        //    var parameter = Expression.Parameter(typeof(T));
        //    var memberExpression = Expression.Property(parameter, fieldName);
        //    var lambdaExpression = Expression.Lambda(memberExpression, parameter);
        //    //return Json.Value(lambdaExpression, "$.default") == fieldValue;

        //}
        public static bool GetExpressionValue<T>(Expression<Func<T, bool>> expression, string path, string fieldValue) //where T : class

        {

            return Json.Value(expression.ToString(), path) == fieldValue;

        }
    }

    public static class Translate
    {
        public static string Value(int langId, string expression, int translationHeaderId)
        {
            throw new InvalidOperationException($"{nameof(Value)} cannot be called client side");
        }
        //public static LambdaExpression GetExpressionValue<T>( string fieldName,string fieldValue, string path ) //where T : class

        //{
        //    var parameter = Expression.Parameter(typeof(T));
        //    var memberExpression = Expression.Property(parameter, fieldName);
        //    var lambdaExpression = Expression.Lambda(memberExpression, parameter);
        //    //return Json.Value(lambdaExpression, "$.default") == fieldValue;

        //}
        public static bool GetExpressionValue<T>(int langId, Expression<Func<T, bool>> expression, int translationHeaderId, string fieldValue) //where T : class
        {

            return Translate.Value(langId, expression.ToString(), translationHeaderId) == fieldValue;

        }
    }
}
