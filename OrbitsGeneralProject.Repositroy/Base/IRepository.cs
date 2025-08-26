using Orbits.GeneralProject.Repositroy.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Orbits.GeneralProject.Repositroy.Base
{
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Interface for repository. </summary>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public interface IRepository<T> where T : class
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Enables the dynamic filter. </summary>
        ///
        /// <param name="filterName">   Name of the dynamic filter. </param>
        ///-------------------------------------------------------------------------------------------------

        //void EnableFilter(string filterName);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Disables the dynamic filter. </summary>
        ///
        ///-------------------------------------------------------------------------------------------------

        IQueryable<T> DisableFilter();
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Disables the dynamic filter. </summary>
        ///
        ///-------------------------------------------------------------------------------------------------

        IQueryable<T> DisableFilter(string filterName);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Disables the dynamic filter with including navigation property. </summary>
        ///
        ///-------------------------------------------------------------------------------------------------
        IQueryable<T> DisableFilter(params Expression<Func<T, object>>[] includeProperties);
         
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Marks an entity as new. </summary>
        ///
        /// <param name="entity">   The entity to delete. </param>
        ///
        /// <returns>   A T. </returns>
        ///-------------------------------------------------------------------------------------------------

        T Add(T entity);
        List<T> Add(List<T> entities);
        //Task<EntityEntry<T>> AddAsync( T entity );

        Task<T> AddAsync(T entity);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Marks an entity as modified. </summary>
        ///
        /// <param name="entity">   The entity to delete. </param>
        ///
        /// <returns>   A T. </returns>
        ///-------------------------------------------------------------------------------------------------

        T Update(T entity);
        T Update(T entity, Expression<Func<T, object>>[] properties);
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Marks an entity to be removed. </summary>
        ///
        /// <param name="entity">   The entity to delete. </param>
        ///-------------------------------------------------------------------------------------------------

        void Delete(T entity);

        void DeleteRange(IEnumerable<T> entities);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Deletes the given where. </summary>
        ///
        /// <param name="where">    The where. </param>
        ///-------------------------------------------------------------------------------------------------

        void Delete(Expression<Func<T, bool>> where);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Get an entity by int id. </summary>
        ///
        /// <param name="id">   The identifier. </param>
        ///
        /// <returns>   The by identifier. </returns>
        ///-------------------------------------------------------------------------------------------------

        T GetById(long id);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets by identifier. </summary>
        ///
        /// <param name="id">   The identifier. </param>
        ///
        /// <returns>   The by identifier. </returns>
        ///-------------------------------------------------------------------------------------------------

        T GetById(int id);
        Task<T> GetByIdAsync(int id);
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Get an entity using delegate. </summary>
        ///
        /// <param name="where">    The where. </param>
        ///
        /// <returns>   A T. </returns>
        ///-------------------------------------------------------------------------------------------------

        T Get(Expression<Func<T, bool>> where);

        Task<T> GetAsync(Expression<Func<T, bool>> where);
        //IQueryable<T> GetAll( );

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets all entities of type T. </summary>
        /// with option to disable dynamic filter (inactive only)
        /// <returns>   all. </returns>
        ///-------------------------------------------------------------------------------------------------

        IQueryable<T> GetAll(bool disableFilter = false);

        IQueryable<T> Where(Expression<Func<T, bool>> where);


        IQueryable<T> WhereIf(Expression<Func<T, bool>> predicate, bool isValid, bool disableFilter = false);

        /// <summary>
        /// Search in json to find the value that equals your input.
        /// </summary>
        /// <param name="PropretyName">The property name that you search in.</param>
        /// <param name="PropertyValue">The value that you want to search with.</param>
        /// <param name="JsonKey">The json key that you will search into.</param>
        /// <returns></returns>
        //IQueryable<T> EqualJsonBy(string PropretyName, string PropertyValue, JsonKeyEnum JsonKey);

        /// <summary>
        /// Search in json to find the value that equals your input. Search in multiple json keys, by multiple values.
        /// </summary>
        /// <param name="EqualModel">List of property names, values and keys that well search by.</param>
        /// <returns></returns>
        //IQueryable<T> EqualJsonBy(List<EqualModel> equalsList);

        bool Any(Expression<Func<T, bool>> predicate);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        Dictionary<string, string> GetEntitySchema();

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets all entities of type T. </summary>
        ///
        /// <returns>
        ///     An enumerator that allows foreach to be used to process all lists in this collection.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        IEnumerable<T> GetAllList(bool disableFilter = false);

        Task<IEnumerable<T>> GetAllListAsync(bool disableFilter = false);

        int Count(bool disableFilter = false);


        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets entities using delegate. </summary>
        ///
        /// <param name="where">    The where. </param>
        ///
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the manies in this collection.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        IEnumerable<T> GetMany(Expression<Func<T, bool>> where);
        Task<IEnumerable<T>> GetManyAsync(Expression<Func<T, bool>> where);
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Enumerates all include in this collection. </summary>
        ///
        /// <param name="includeProperties">    A variable-length parameters list containing include properties. </param>
        ///
        /// <returns>
        ///     An enumerator that allows foreach to be used to process all include in this collection.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        IEnumerable<T> AllInclude(params Expression<Func<T, object>>[] includeProperties);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Finds the includes in this collection. </summary>
        ///
        /// <param name="predicte">             The predicte. </param>
        /// <param name="includeProperties">    A variable-length parameters list containing include
        ///                                                                      properties. </param>
        ///
        /// <returns>
        ///     An enumerator that allows foreach to be used to process the includes in this collection.
        /// </returns>
        ///-------------------------------------------------------------------------------------------------

        IEnumerable<T> FindByInclude(Expression<Func<T, bool>> predicte, params Expression<Func<T, object>>[] includeProperties);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Paged result. </summary>
        ///
        /// <typeparam name="TKey"> Type of the key. </typeparam>
        /// <param name="take">                 The take. </param>
        /// <param name="skip">                 The skip. </param>
        /// <param name="keySelector">          The key selector. </param>
        /// <param name="sortDirection">        (Optional) The sort direction. </param>
        /// <param name="includeProperties">    A variable-length parameters list containing include
        ///                                                                      properties. </param>
        ///
        /// <returns>   A ListPaged&lt;T&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        //ListPaged<T> PagedResult<TKey>(int take,
        //                                      int skip,
        //                                      Expression<Func<T, TKey>> keySelector,
        //                                      string sortDirection = nameof(SortingDirection.ASC),
        //                                      bool disableFilter = false,
        //                                      params Expression<Func<T, object>>[] includeProperties);

        //ListPaged<T> PagedResult<TKey>(int take, int skip, Expression<Func<T, TKey>> keySelector, string sortDirection = nameof(SortingDirection.ASC), params Expression<Func<T, object>>[] includeProperties);
        //QueryPaged<T> PagedQueryable<TKey>( int take,
        //                                  int skip,
        //                                  Expression<Func<T, TKey>> keySelector,
        //                                  string sortDirection = nameof(SortingDirection.ASC),
        //                                  bool disableFilter = false,
        //                                  params Expression<Func<T, object>> [] includeProperties );
        ListPaged<T> PagedResult<TKey>( int? take,
                                             int? skip,
                                             string keySelector,
                                             //Expression<Func<T, TKey>> keySelector,
                                             string sortDirection = nameof(SortingDirection.ASC),
                                             params Expression<Func<T, object>> [] includeProperties );
        public ListPaged<T> PagedResult<TKey>( int? take,
                                            int? skip,
                                            string keySelector,
                                            //Expression<Func<T, TKey>> keySelector,
                                            string sortDirection = nameof(SortingDirection.ASC),
                                            bool disableFilter = false, 
                                          params Expression<Func<T, object>> [] includeProperties );

        public QueryPaged<T> PagedQueryable<TKey>( int? take,
                                          int? skip,
                                            string keySelector,
                                          //Expression<Func<T, TKey>> keySelector,
                                          string sortDirection = nameof(SortingDirection.ASC),
                                          bool disableFilter = false,
                                          params Expression<Func<T, object>> [] includeProperties );
            

            ListPaged<T> PagedResult<TKey>(int? take, int? skip, 
        //Expression<Func<T, TKey>> keySelector,
        Expression<Func<T, bool>> predicate, string keySelector, string sortDirection = nameof(SortingDirection.ASC), bool disableFilter = false, params Expression<Func<T, object>>[] includeProperties);


        QueryPaged<T> PagedQueryable<TKey>( int? take, int? skip,
              string keySelector,
            //Expression<Func<T, TKey>> keySelector,
            Expression<Func<T, bool>> predicate, string sortDirection = nameof(SortingDirection.ASC), bool disableFilter = false, params Expression<Func<T, object>> [] includeProperties );
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Paged result Async. </summary>
        ///
        /// <typeparam name="TKey"> Type of the key. </typeparam>
        /// <param name="take">                 The take. </param>
        /// <param name="skip">                 The skip. </param>
        /// <param name="keySelector">          The key selector. </param>
        /// <param name="predicate">            The predicate. </param>
        /// <param name="sortDirection">        (Optional) The sort direction. </param>
        /// <param name="includeProperties">    A variable-length parameters list containing include
        ///                                     properties. </param>
        ///
        /// <returns>   A ListPaged&lt;T&gt; </returns>
        ///-------------------------------------------------------------------------------------------------

        Task<ListPaged<T>> PagedResultAsync<TKey>(int? take, int? skip,
             string keySelector,
            //Expression<Func<T, TKey>> keySelector,
            Expression<Func<T, bool>> predicate, string sortDirection = nameof(SortingDirection.ASC), params Expression<Func<T, object>>[] includeProperties);
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets all including. </summary>
        ///
        /// <param name="includeProperties">  A variable-length parameters list containing include properties. </param>
        ///
        /// <returns>   all including. </returns>
        ///-------------------------------------------------------------------------------------------------

        IQueryable<T> GetAllIncluding(Expression<Func<T, object>>[] includeProperties, bool disableFilter = false);
        void UpdateCrossTable(List<T> selectedObj, Expression<Func<T, bool>> ExistingRecords, string entityName);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Updates the cross table. </summary>
        ///
        /// <param name="selectedObj">      The many-to-many relation object. </param>
        /// <param name="ExistingRecords">  Where condition that will be used to get existing records for
        ///                                 cross table. </param>
        /// <param name="entityName">       Name of the entity as it exists in dbContext. </param>
        ///-------------------------------------------------------------------------------------------------

        void UpdateCrossTable(List<T> selectedObj, Expression<Func<T, bool>> ExistingRecords, string entityName, bool isSoftDelete = false, int? currentUser = null, DateTime? currentDate = null);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Execute sql statement. </summary>
        ///
        /// <typeparam name="typ">  The type of return. </typeparam>
        /// <param name="queryStatment">    The Statement . </param>
        /// <param name="parameters">       The Parameter which is included in statement. </param>
        ///
        /// <returns>   return the "typ" which you pass it. </returns>
        ///-------------------------------------------------------------------------------------------------

        List<typ> ExecuteSQLQuery<typ>(string queryStatment, Hashtable parameters) where typ : class;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Execute sql statement. </summary>
        ///
        /// <param name="queryStatment">    The Statement . </param>
        /// <param name="parameters">       The Parameter which is included in statement. </param>
        ///
        ///-------------------------------------------------------------------------------------------------

        void ExecuteSQLQuery(string queryStatment, Hashtable parameters);
        void RefreshEntity(T entity);
        void RefreshEntity(T entity, params Expression<Func<T, object>>[] navigationProperties);

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Execute stored procedure statement and can return multiResult. </summary>
        ///
        /// <param name="storeProcedureName">    The name of stored procedure . </param>
        /// <param name="parameters">       The collection of Parameter for stored procedure. </param>
        /// <param name="lstDtoType">       The list of type for each result which is returned from stored procedure. </param>
        /// <returns>   return the list of IEnumerable. </returns>
        ///-------------------------------------------------------------------------------------------------
        List<IEnumerable> ExecuteStoredProcedure(string storeProcedureName, Hashtable parameters, List<Type> lstDtoType);
    }
}
