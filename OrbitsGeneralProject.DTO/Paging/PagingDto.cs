using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Orbits.GeneralProject.DTO.Paging
{
     

    /// <summary>   (Serializable) a filtered result request dto. </summary>
    [Serializable]
    public class FilteredResultRequestDto : LimitedResultRequestDto, IFilteredResultRequest
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Skip count (beginning of the page). </summary>
        ///
        /// <value> The number of skips. </value>
        ///-------------------------------------------------------------------------------------------------

        [Range(0, int.MaxValue)]
        public virtual int SkipCount { get; set; } = 0;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the search term. </summary>
        ///
        /// <value> The search term. </value>
        ///-------------------------------------------------------------------------------------------------

        public virtual string SearchTerm { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Gets or sets the filter. </summary>
        ///
        /// <value> The filter. </value>
        ///-------------------------------------------------------------------------------------------------

        public virtual string Filter { get; set; }
        public virtual string Lang { get; set; } = "en";

        public virtual string SortingDirection { get; set; } = "ASC";
        public virtual string SortBy { get; set; } 

    }



    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Simply implements <see cref="ILimitedResultRequest"/>. </summary>
    ///
    ///-------------------------------------------------------------------------------------------------

    public class LimitedResultRequestDto : ILimitedResultRequest
    {
        /// <summary>   Number of maximum results. </summary>
        /// //TODO: Make in Const
        private int maxResultCount = 25;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Max expected result count. </summary>
        ///
        /// <value> The number of maximum results. </value>
        ///-------------------------------------------------------------------------------------------------

        [Range(1, int.MaxValue)]
        public virtual int MaxResultCount
        {
            get
            {
                return maxResultCount;
            }
            set
            {
                maxResultCount = value;
            }
        }
    }
 
    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Simply implements <see cref="IPagedResultRequest"/>. </summary>
    ///
    ///-------------------------------------------------------------------------------------------------

    [Serializable]
    public class PagedResultRequestDto : LimitedResultRequestDto, IPagedResultRequest
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Skip count (beginning of the page). </summary>
        ///
        /// <value> The number of skips. </value>
        ///-------------------------------------------------------------------------------------------------

        [Range(0, int.MaxValue)]
        public virtual int SkipCount { get; set; }
    }

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Implements <see cref="IListResult{T}"/>. </summary>
    ///
    ///
    /// <typeparam name="T">    Type of the items in the <see cref="Items"/> list. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    [Serializable]
    public class ListResultDto<T> : IListResult<T>
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   List of items. </summary>
        ///
        /// <value> The items. </value>
        ///-------------------------------------------------------------------------------------------------

        public IReadOnlyList<T> Items
        {
            get { return _items ?? (_items = new List<T>()); }
            set { _items = value; }
        }

        /// <summary>   The items. </summary>
        private IReadOnlyList<T> _items;

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Creates a new <see cref="ListResultDto{T}"/> object. </summary>
        ///
        ///-------------------------------------------------------------------------------------------------

        public ListResultDto()
        {
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Creates a new <see cref="ListResultDto{T}"/> object. </summary>
        ///
        /// <remarks>   Mahmoud, 2018-02-18. </remarks>
        ///
        /// <param name="items">    List of items. </param>
        ///-------------------------------------------------------------------------------------------------

        public ListResultDto(IReadOnlyList<T> items)
        {
            Items = items;
        }
    }

    ///-------------------------------------------------------------------------------------------------
    /// <summary>   Implements <see cref="IPagedResult{T}"/>. </summary>
    ///
    /// <typeparam name="T">    Type of the items in the <see cref="ListResultDto{T}.Items"/> list. </typeparam>
    ///-------------------------------------------------------------------------------------------------

    [Serializable]
    public class PagedResultDto<T> : ListResultDto<T>, IPagedResult<T>
    {
        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Total count of Items. </summary>
        ///
        /// <value> The total number of count. </value>
        ///-------------------------------------------------------------------------------------------------

        public int TotalCount { get; set; }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Creates a new <see cref="PagedResultDto{T}"/> object. </summary>
        ///
        ///-------------------------------------------------------------------------------------------------

        public PagedResultDto()
        {
        }

        ///-------------------------------------------------------------------------------------------------
        /// <summary>   Creates a new <see cref="PagedResultDto{T}"/> object. </summary>
        ///
        ///
        /// <param name="totalCount">   Total count of Items. </param>
        /// <param name="items">        List of items in current page. </param>
        ///-------------------------------------------------------------------------------------------------

        public PagedResultDto(int totalCount, IReadOnlyList<T> items) : base(items)
        {
            TotalCount = totalCount;
        }
    }
}
