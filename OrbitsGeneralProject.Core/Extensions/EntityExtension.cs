using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.Core.Extensions
{
    public static class EntityExtension
    {
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, bool IsValid) where T : class
        {
            return IsValid ? source.Where(predicate) : source;
        }
        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, Func<T, bool> predicate, bool IsValid) where T : class
        {
            return IsValid ? source.Where(predicate) : source;
        }

        public static IQueryable<T> DisableFilter<T>( this IQueryable<T> query,DbSet<T> dbset, string filterName ) where T : EntityBase
        {
            if (HasProperty(dbset,nameof(EntityBase.IsDeleted)))
            {

              
                 if (filterName == nameof(EntityBase.IsDeleted))
                    return query.Where(x => x.IsDeleted);
                else
                    return query;

            }
            else
                return query;
        }
        private static bool HasProperty( object
            entity, params string [] properties )
        {
           var entityProperties = entity.GetType().GetProperties();

            foreach (var prop in properties)
            {
                if (string.IsNullOrWhiteSpace(prop))
                    return false;
                if (entityProperties.FirstOrDefault(x=>x.Name == prop) == null)
                    return false;
            }
            return true;
        }
    }
}
