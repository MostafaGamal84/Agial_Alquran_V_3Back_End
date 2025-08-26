using Microsoft.EntityFrameworkCore;

using System.Reflection.Metadata.Ecma335;

namespace Orbits.GeneralProject.Core.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> DisableFilter<T>( this IQueryable<T> query ) where T : EntityBase
        {
            if (IsPropertyExists(typeof(T), "inactive"))
                query = query.IgnoreQueryFilters();
            else if (IsPropertyExists(typeof(T), "isdeleted"))
                query = query.IgnoreQueryFilters().Where(x => !x.IsDeleted);
            else
                query =  query;

            return query;

        }
        /// <summary>
        ///check if property name exists in entity like "Inactive" ,"IsDeleted"
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static bool IsPropertyExists( Type type, string propertyName )
        {
            var propery = type
               .GetProperties(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
               | System.Reflection.BindingFlags.DeclaredOnly)
               .Where(x => x.Name.ToLower() == propertyName?.ToLower()).FirstOrDefault();

            return (propery != null) ? true : false;
        }
    }
}
