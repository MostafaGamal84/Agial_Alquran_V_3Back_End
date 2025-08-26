using System;
using System.Collections.Generic;
using System.Text;

namespace Orbits.GeneralProject.DTO.Paging
{
    /// <summary>   Interface for filtered result request. </summary>
    public interface IFilteredResultRequest : ILimitedResultRequest
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Skip count (beginning of the page). </summary>
        ///
        /// <value> The number of skips. </value>
        ///-------------------------------------------------------------------------------------------------

        int SkipCount { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the search term. </summary>
        ///
        /// <value> The search term. </value>
        ///-------------------------------------------------------------------------------------------------

        string SearchTerm { get; set; }
    }

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   This interface is defined to standardize to request a paged result. </summary>
    ///
    ///-------------------------------------------------------------------------------------------------

    public interface IPagedResultRequest : ILimitedResultRequest
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Skip count (beginning of the page). </summary>
        ///
        /// <value> The number of skips. </value>
        ///-------------------------------------------------------------------------------------------------

        int SkipCount { get; set; }
    }


    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///     This interface is defined to standardize to return a page of items to clients.
    /// </summary>
    ///
    /// <typeparam name="T">    Type of the items in the <see cref="IListResult{T}.Items"/> list. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public interface IPagedResult<T> : IListResult<T>, IHasTotalCount
    {
    }

    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///     This interface is defined to standardize to return a list of items to clients.
    /// </summary>
    ///
    /// <typeparam name="T">    Type of the items in the <see cref="Items"/> list. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    public interface IListResult<T>
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   List of items. </summary>
        ///
        /// <value> The items. </value>
        ///-------------------------------------------------------------------------------------------------

        IReadOnlyList<T> Items { get; set; }
    }

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   This interface is defined to standardize to request a limited result. </summary>
    ///
    ///-------------------------------------------------------------------------------------------------

    public interface ILimitedResultRequest
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Max expected result count. </summary>
        ///
        /// <value> The number of maximum results. </value>
        ///-------------------------------------------------------------------------------------------------

        int MaxResultCount { get; set; }
    }
    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///     This interface is defined to standardize to set "Total Count of Items" to a DTO.
    /// </summary>
    ///
    ///-------------------------------------------------------------------------------------------------

    public interface IHasTotalCount
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Total count of Items. </summary>
        ///
        /// <value> The total number of count. </value>
        ///-------------------------------------------------------------------------------------------------

        int TotalCount { get; set; }
    }

    ///-------------------------------------------------------------------------------------------------
    /// <summary>
    ///     This interface is defined to standardize to set "Total Count of Items" to a DTO for long
    ///     type.
    /// </summary>
    ///
    ///-------------------------------------------------------------------------------------------------

    public interface IHasLongTotalCount
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Total count of Items. </summary>
        ///
        /// <value> The total number of count. </value>
        ///-------------------------------------------------------------------------------------------------

        long TotalCount { get; set; }
    }
}
