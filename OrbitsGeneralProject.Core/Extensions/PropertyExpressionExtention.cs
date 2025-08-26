using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.Core.Extensions
{
    public static class PropertyExpressionExtention
    {


        public  static LambdaExpression GetLambdaExpression(this string propertyName, Type type)
        {
            var param = Expression.Parameter(type, "x");
            Expression body = param;
            foreach (var member in propertyName.Split('.'))
            {
                body = Expression.PropertyOrField(body, member);
            }
            return Expression.Lambda(body, param);
        }
        public static Expression<Func<T, int>> GetExpression<T>(this string propertyName) where T : EntityBase
        {
            var param = Expression.Parameter(typeof(T), "x");
            Expression body = param;
           
            foreach (var member in propertyName.Split('.'))
            {
                body = Expression.PropertyOrField(body, member);
            }
            return (Expression<Func<T, int>>)Expression.Lambda(body, param);
        }
        
        //private static MemberExpression GetMemberExpression(Expression param, string propertyName)
        //{
        //    if (propertyName.Contains("."))
        //    {
        //        int index = propertyName.IndexOf(".");
        //        var subParam = Expression.Property(param, propertyName.Substring(0, index));
        //        return GetMemberExpression(subParam, propertyName.Substring(index + 1));
        //    }

        //    return Expression.Property(param, propertyName);
        //}
        public static Type getPropertyNameType<T>(this string propertyName) where T : EntityBase
        {
            Type t = typeof(T);
            var prop = t.GetProperties().Where(x => x.Name == propertyName);     
            return prop.GetType();
        }

    }
}
